#!/bin/sh
# Converts a square PNG into a macOS .icns using tools that ship with macOS.
#
#     sh make-icon.sh icon.png out.icns
#
# icon.png should be 1024x1024. sign.sh calls this automatically when an
# icon.png is present next to it.
set -e
SRC="${1:-icon.png}"
OUT="${2:-ags.icns}"

[ -f "$SRC" ] || { echo "make-icon.sh: $SRC not found" >&2; exit 1; }

WORK="$(mktemp -d)/ags.iconset"
mkdir -p "$WORK"
for size in 16 32 128 256 512; do
    sips -z "$size" "$size"             "$SRC" --out "$WORK/icon_${size}x${size}.png"    >/dev/null
    sips -z "$((size*2))" "$((size*2))" "$SRC" --out "$WORK/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$WORK" -o "$OUT"
rm -rf "$(dirname "$WORK")"
echo "wrote $OUT"
