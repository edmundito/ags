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

  popd
  echo "done!"
}

function create_proj_archive {
  set -e
  echo "creating project archive..."
  pushd "${SCRIPT_DIR}"
  version=$(ags_version)

  if [[ ! -d "${PACKAGE_DIR}/Frameworks/AGSKit.xcframework" ]]; then
    echo "error: package/Frameworks/AGSKit.xcframework is missing, run 'osx-build.sh build_release' first." >&2
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
    -h|--help)
       usage
       break ;;
    *)
       break ;;
  esac
done
