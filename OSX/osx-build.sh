#!/bin/bash
set -e
#   Exit immediately if a simple command exits with a non-zero status, unless
# the command that fails is part of an until or  while loop, part of an
# if statement, part of a && or || list, or if the command's return status
# is being inverted using !.

# Builds the AGS engine as a framework and assembles the exportable Xcode
# project template in OSX/package. The AGS Editor ships the result, so a game
# author only has to open the project on a Mac and archive it.
#
# Must run on macOS: it needs xcodebuild and hdiutil.

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
PACKAGE_DIR="${SCRIPT_DIR}/package"
BUILD_DIR="${SCRIPT_DIR}/package/build"
LIBSRC_DIR="${SCRIPT_DIR}/../libsrc"
TAR_CMD="bsdtar"
command -v bsdtar >/dev/null 2>&1 || { TAR_CMD="tar" ; }

function require_macos {
  if [[ "$(uname -s)" != "Darwin" ]]; then
    echo "error: this script builds an Xcode project and only runs on macOS." >&2
    exit 1
  fi
}

function ags_version {
  awk -F"[ \"]+" '{ if ($1=="#define" && $2=="ACI_VERSION_STR") { print $3; exit } }' \
    "${SCRIPT_DIR}/../Common/ac/def_version.h"
}

# Extracts SDL2.framework from the disk image that libsrc/download.sh fetches.
# download.sh only downloads the .dmg; nothing else in the tree unpacks it.
function extract_sdl2_framework {
  set -e
  local dmg="${LIBSRC_DIR}/SDL2-Framework.dmg"
  local mnt

  if [[ -d "${LIBSRC_DIR}/SDL2.framework" ]]; then
    echo "SDL2.framework: already extracted, delete it to redo this."
    return
  fi
  if [[ ! -f "${dmg}" ]]; then
    echo "error: ${dmg} is missing, run libsrc/download.sh first." >&2
    exit 1
  fi

  mnt="$(mktemp -d)"
  hdiutil attach -nobrowse -noverify -quiet -mountpoint "${mnt}" "${dmg}"
  cp -R "${mnt}/SDL2.framework" "${LIBSRC_DIR}/"
  hdiutil detach -quiet "${mnt}"
  rmdir "${mnt}"
}

function prepare_package {
  set -e
  require_macos
  echo "doing preparations..."
  pushd "${SCRIPT_DIR}"

  # Library sources for AGSKit, and the SDL2 framework it links against.
  "${LIBSRC_DIR}/download.sh"
  extract_sdl2_framework

  # SDL2 ships inside the game bundle, so the template needs its own copy.
  rm -rf "${PACKAGE_DIR}/Frameworks/SDL2.framework"
  cp -R "${LIBSRC_DIR}/SDL2.framework" "${PACKAGE_DIR}/Frameworks/"

  # plugin_registration.cpp includes this. Copied rather than duplicated in
  # the repo so it cannot drift from the engine's own copy.
  mkdir -p "${PACKAGE_DIR}/include/plugin"
  cp "${SCRIPT_DIR}/../Engine/plugin/agsplugin.h" "${PACKAGE_DIR}/include/plugin/"

  popd
  echo "done!"
}

function build_release_framework {
  set -e
  require_macos
  echo "building AGSKit framework..."
  pushd "${SCRIPT_DIR}"

  if [[ ! -d "${LIBSRC_DIR}/SDL2.framework" ]]; then
    echo "error: libsrc/SDL2.framework is missing, run 'osx-build.sh prepare' first." >&2
    exit 1
  fi

  # Make sure the SDL2 framework is staged in the template. prepare puts it
  # here too, but a repeated build_release finds it already zipped away, so
  # refresh it to keep this step runnable on its own.
  rm -rf "${PACKAGE_DIR}/Frameworks/SDL2.framework"
  cp -R "${LIBSRC_DIR}/SDL2.framework" "${PACKAGE_DIR}/Frameworks/"

  rm -rf "${BUILD_DIR}"
  xcodebuild -project xcode/AGSKit/AGSKit.xcodeproj \
    -scheme AGSKit \
    -configuration Release \
    -derivedDataPath "${BUILD_DIR}/dd" \
    ARCHS="x86_64 arm64" \
    ONLY_ACTIVE_ARCH=NO \
    CODE_SIGNING_ALLOWED=NO \
    build

  # AGSKit is a static framework (MACH_O_TYPE = staticlib), so it links into
  # the game's executable and never needs signing of its own. The xcframework
  # wrapper keeps the layout stable if a slice is ever split out per arch.
  rm -rf "${PACKAGE_DIR}/Frameworks/AGSKit.xcframework"
  xcodebuild -create-xcframework \
    -framework "${BUILD_DIR}/dd/Build/Products/Release/AGSKit.framework" \
    -output "${PACKAGE_DIR}/Frameworks/AGSKit.xcframework"

  pack_frameworks

  popd
  echo "done!"
}

# Frameworks are stored in the template as zip archives, not as loose
# directories. A macOS framework relies on internal symlinks (SDL2 ->
# Versions/Current/SDL2 and so on), and those symlinks do not survive being
# unpacked on Windows and copied file-by-file by the Editor. Shipping the
# frameworks zipped keeps them opaque until they reach the Mac, where the
# Xcode project (or unpack-frameworks.sh) restores them with their symlinks
# intact. ditto is used because it preserves symlinks and resource forks.
function pack_frameworks {
  set -e
  pushd "${PACKAGE_DIR}/Frameworks"
  for fw in SDL2.framework AGSKit.xcframework; do
    if [[ -d "${fw}" ]]; then
      rm -f "${fw}.zip"
      ditto -c -k --keepParent "${fw}" "${fw}.zip"
      rm -rf "${fw}"
    fi
  done
  popd
}

function build_app {
  set -e
  require_macos
  echo "building AGSGame.app..."
  pushd "${SCRIPT_DIR}"

  # The package project links these; build_release must have produced them.
  if [[ ! -f "${PACKAGE_DIR}/Frameworks/AGSKit.xcframework.zip" ]]; then
    echo "error: run 'osx-build.sh build_release' first." >&2
    exit 1
  fi

  # Unzip the frameworks so xcodebuild can link against them (build_release
  # leaves them zipped for the Windows Editor).
  ( cd "${PACKAGE_DIR}/Frameworks"
    for fw in SDL2.framework.zip AGSKit.xcframework.zip; do
      [[ -e "${fw}" ]] && { rm -rf "${fw%.zip}"; ditto -x -k "${fw}" .; }
    done )

  local appbuild="${SCRIPT_DIR}/app-build"
  rm -rf "${appbuild}"
  xcodebuild -project "${PACKAGE_DIR}/AGSGame.xcodeproj" \
    -scheme AGSGame \
    -configuration Release \
    -derivedDataPath "${appbuild}/dd" \
    ARCHS="x86_64 arm64" \
    ONLY_ACTIVE_ARCH=NO \
    CODE_SIGNING_ALLOWED=NO \
    build

  local app="${appbuild}/dd/Build/Products/Release/AGSGame.app"
  [[ -d "${app}" ]] || { echo "error: AGSGame.app not produced." >&2; exit 1; }

  # Ship the app zipped so its internal symlinks survive the Windows Editor;
  # sign.sh restores them with ditto on the Mac. See OSX/app-package/.
  mkdir -p "${SCRIPT_DIR}/app-package"
  rm -f "${SCRIPT_DIR}/app-package/AGSGame.app.zip"
  ditto -c -k --keepParent "${app}" "${SCRIPT_DIR}/app-package/AGSGame.app.zip"
  rm -rf "${appbuild}"

  popd
  echo "done!"
}

function create_app_archive {
  set -e
  echo "creating app archive..."
  pushd "${SCRIPT_DIR}"
  version=$(ags_version)

  if [[ ! -f "${SCRIPT_DIR}/app-package/AGSGame.app.zip" ]]; then
    echo "error: app-package/AGSGame.app.zip is missing, run 'osx-build.sh build_app' first." >&2
    exit 1
  fi

  ${TAR_CMD} -f "../AGS-${version}-macos-app.zip" -acv --strip-components 1 app-package
  popd
  echo "done!"
}

function create_proj_archive {
  set -e
  echo "creating project archive..."
  pushd "${SCRIPT_DIR}"
  version=$(ags_version)

  if [[ ! -f "${PACKAGE_DIR}/Frameworks/AGSKit.xcframework.zip" ]]; then
    echo "error: package/Frameworks/AGSKit.xcframework.zip is missing, run 'osx-build.sh build_release' first." >&2
    exit 1
  fi

  # Build leftovers are not part of what ships.
  rm -rf "${BUILD_DIR}"

  ${TAR_CMD} -f "../AGS-${version}-macos-proj.zip" -acv --strip-components 1 package
  popd
  echo "done!"
}

function usage
{
   echo "macOS build script."
   echo
   echo "Builds the AGS engine as AGSKit.xcframework and assembles the"
   echo "exportable Xcode project template in OSX/package."
   echo
   echo "Syntax: osx-build.sh [options]"
   echo "options:"
   echo "  prepare          downloads library sources, unpacks SDL2 and"
   echo "                   copies the shipped headers into the template"
   echo "  build_release    builds AGSKit universal and wraps it as an"
   echo "                   xcframework inside the template"
   echo "  archive_project  zips the template as AGS-<version>-macos-proj.zip"
   echo "  build_app        builds the prebuilt AGSGame.app and stages it,"
   echo "                   zipped, into the app-bundle template"
   echo "  archive_app      zips the app template as AGS-<version>-macos-app.zip"
   echo "  -h --help  prints this message."
   echo
}

if [[ $# -eq 0 ]]; then
    usage
    exit
fi

while : ; do
  case "$1" in
    prepare)
       prepare_package
       shift 1 ;;
    build_release)
       build_release_framework
       shift 1 ;;
    archive_project)
       create_proj_archive
       shift 1 ;;
    build_app)
       build_app
       shift 1 ;;
    archive_app)
       create_app_archive
       shift 1 ;;
    -h|--help)
       usage
       break ;;
    *)
       break ;;
  esac
done
