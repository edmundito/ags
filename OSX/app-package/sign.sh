#!/bin/sh
# Assembles and signs your AGS game as a macOS .app.
#
# Run this once on a Mac (Xcode Command Line Tools are enough - no full Xcode):
#
#     sh sign.sh
#
# It unpacks the prebuilt app, injects your game data, patches the bundle's
# identity, converts an optional icon, then signs (and optionally notarizes).
set -e
cd "$(dirname "$0")"

# ---- your signing identity -------------------------------------------------
# Fill these in, or keep them in a signing.env next to this script (reusable
# across every game you build). Find the identity string with:
#     security find-identity -v -p codesigning
SIGN_IDENTITY="${SIGN_IDENTITY:-Developer ID Application: YOUR NAME (TEAMID)}"
# For notarization (optional). Leave APPLE_ID empty to skip notarizing.
APPLE_ID="${APPLE_ID:-}"
TEAM_ID="${TEAM_ID:-}"
NOTARY_PASSWORD="${NOTARY_PASSWORD:-}"   # app-specific password
[ -f signing.env ] && . ./signing.env
# ---------------------------------------------------------------------------

. ./game.env   # GAME_NAME, APP_NAME, BUNDLE_ID, APP_VERSION (written by the Editor)

APP="$APP_NAME.app"

# Downloaded/Windows-copied files arrive quarantined; clear it so Gatekeeper
# and codesign do not balk. Ignore failure where xattr is unavailable.
xattr -dr com.apple.quarantine . 2>/dev/null || true

# 1. Unpack the prebuilt app. ditto restores the internal symlinks a plain
#    unzip would flatten.
if [ ! -d "$APP" ]; then
    ditto -x -k AGSGame.app.zip .
    [ "$APP" = "AGSGame.app" ] || mv AGSGame.app "$APP"
fi

# 2. Inject game data.
rsync -a --delete Resources/ "$APP/Contents/Resources/"

# 3. Optional icon: convert icon.png -> ags.icns before signing (sealed resource).
if [ -f icon.png ]; then
    sh make-icon.sh icon.png "$APP/Contents/Resources/ags.icns"
fi

# 4. Optional plugins: any .dylib next to this script goes into the bundle.
for dylib in *.dylib; do
    [ -e "$dylib" ] || continue
    cp "$dylib" "$APP/Contents/Frameworks/"
done

# 5. Patch the bundle's identity.
PLIST="$APP/Contents/Info.plist"
set_plist() { /usr/libexec/PlistBuddy -c "Set :$1 $2" "$PLIST" 2>/dev/null \
    || /usr/libexec/PlistBuddy -c "Add :$1 string $2" "$PLIST"; }
set_plist CFBundleName "$GAME_NAME"
set_plist CFBundleDisplayName "$GAME_NAME"
set_plist CFBundleIdentifier "$BUNDLE_ID"
set_plist CFBundleShortVersionString "$APP_VERSION"

# 6. Sign inside-out: nested code first, the app last with entitlements.
for nested in "$APP/Contents/Frameworks/"*; do
    [ -e "$nested" ] || continue
    codesign --force --options runtime --timestamp --sign "$SIGN_IDENTITY" "$nested"
done
codesign --force --options runtime --timestamp \
    --entitlements AGSGame.entitlements --sign "$SIGN_IDENTITY" "$APP"
codesign --verify --deep --strict "$APP"
echo "signed $APP"

# 7. Optional notarize + staple.
if [ -n "$APPLE_ID" ] && [ -n "$TEAM_ID" ] && [ -n "$NOTARY_PASSWORD" ]; then
    ditto -c -k --keepParent "$APP" "$APP_NAME-notarize.zip"
    xcrun notarytool submit "$APP_NAME-notarize.zip" \
        --apple-id "$APPLE_ID" --team-id "$TEAM_ID" --password "$NOTARY_PASSWORD" --wait
    xcrun stapler staple "$APP"
    rm -f "$APP_NAME-notarize.zip"
    echo "notarized and stapled $APP"
else
    echo "skipped notarization (set APPLE_ID/TEAM_ID/NOTARY_PASSWORD to enable)"
fi
