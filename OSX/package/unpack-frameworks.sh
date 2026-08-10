#!/bin/sh
# Unpacks the framework archives shipped with this project.
#
# The frameworks are stored as .zip files because a macOS framework relies on
# internal symlinks that do not survive being copied on Windows, where the AGS
# Editor assembles this project. Run this once on your Mac before opening the
# project in Xcode:
#
#     sh unpack-frameworks.sh
#
# Invoking it through "sh" means it does not need the executable bit, which is
# also lost on the Windows trip. ditto restores the symlinks that a plain unzip
# would flatten.
set -e

cd "$(dirname "$0")"

# Files that came through Windows and a download arrive quarantined, which makes
# macOS distrust the scripts and libraries. Clear it for the whole project so
# Gatekeeper does not get in the way. Ignore failure on systems without xattr.
xattr -dr com.apple.quarantine . 2>/dev/null || true

cd Frameworks

for zip in *.zip; do
    [ -e "$zip" ] || continue
    name="${zip%.zip}"
    if [ -e "$name" ]; then
        echo "$name already unpacked"
        continue
    fi
    echo "unpacking $name"
    ditto -x -k "$zip" .
done

# Clear quarantine again, now that the frameworks the zips carried are on disk.
cd ..
xattr -dr com.apple.quarantine . 2>/dev/null || true

echo "frameworks ready - open mygame.xcodeproj and build"
