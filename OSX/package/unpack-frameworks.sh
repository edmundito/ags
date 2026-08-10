#!/bin/sh
# Unpacks the framework archives shipped with this project.
#
# The frameworks are stored as .zip files because a macOS framework relies on
# internal symlinks that do not survive being copied on Windows, where the AGS
# Editor assembles this project. On the Mac they must be unpacked before the
# app can link against them. The Xcode project does this automatically as its
# first build phase; run this script by hand if you want them unpacked now, or
# if you are building outside Xcode.
#
# ditto restores the symlinks that plain unzip would flatten.
set -e

cd "$(dirname "$0")/Frameworks"

for zip in *.zip; do
    [ -e "$zip" ] || continue
    name="${zip%.zip}"
    if [ ! -e "$name" ]; then
        echo "unpacking $name"
        ditto -x -k "$zip" .
    fi
done

echo "frameworks ready"
