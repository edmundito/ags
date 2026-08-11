#!/bin/sh
# Builds your AGS game into a macOS .app.
#
# Run this once on a Mac (Xcode Command Line Tools are enough - no full Xcode):
#
#     sh make-app.sh
#
# It unpacks the prebuilt engine app, injects your game data, patches the
# bundle's identity, and converts an optional icon. Signing and notarization
# are OPTIONAL and only needed to give the app to other people (or a store) --
# without a signing identity the app is ad-hoc signed and runs on this Mac.
set -e
cd "$(dirname "$0")"

# ---- signing (optional) ----------------------------------------------------
# Leave SIGN_IDENTITY empty to just build a runnable app for this Mac.
# To distribute, set it to your Developer ID, or keep these in a signing.env
# next to this script (reusable across every game). Find the identity with:
#     security find-identity -v -p codesigning
SIGN_IDENTITY="${SIGN_IDENTITY:-}"
# For notarization (needed for other people's Macs). Leave APPLE_ID empty to skip.
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
#    unzip would flatten. The archive is named after your game; the bundle
#    inside it is always AGSGame.app, which we rename to your game's name.
if [ ! -d "$APP" ]; then
    ZIP=$(ls -1 *.app.zip 2>/dev/null | head -1)
    [ -n "$ZIP" ] || { echo "error: no .app.zip found next to make-app.sh" >&2; exit 1; }
    ditto -x -k "$ZIP" .
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

# 6. Sign the bundle, inside-out (nested code first, the app last). With a
#    Developer ID this produces a distributable app; without one it is ad-hoc
#    signed so it runs on this Mac only. Ad-hoc cannot use a secure timestamp.
if [ -n "$SIGN_IDENTITY" ]; then
    IDENTITY="$SIGN_IDENTITY"; TIMESTAMP="--timestamp"
    echo "signing $APP with: $IDENTITY"
else
    IDENTITY="-"; TIMESTAMP="--timestamp=none"
    echo "no SIGN_IDENTITY set: ad-hoc signing (runs on this Mac only; set an identity to distribute)"
fi
for nested in "$APP/Contents/Frameworks/"*; do
    [ -e "$nested" ] || continue
    codesign --force --options runtime $TIMESTAMP --sign "$IDENTITY" "$nested"
done
codesign --force --options runtime $TIMESTAMP \
    --entitlements AGSGame.entitlements --sign "$IDENTITY" "$APP"
codesign --verify --deep --strict "$APP"
echo "built $APP"

# 7. Optional notarize + staple (needs a real identity and notary credentials).
if [ -n "$SIGN_IDENTITY" ] && [ -n "$APPLE_ID" ] && [ -n "$TEAM_ID" ] && [ -n "$NOTARY_PASSWORD" ]; then
    ditto -c -k --keepParent "$APP" "$APP_NAME-notarize.zip"
    xcrun notarytool submit "$APP_NAME-notarize.zip" \
        --apple-id "$APPLE_ID" --team-id "$TEAM_ID" --password "$NOTARY_PASSWORD" --wait
    xcrun stapler staple "$APP"
    rm -f "$APP_NAME-notarize.zip"
    echo "notarized and stapled $APP"
else
    echo "skipped notarization (set SIGN_IDENTITY + APPLE_ID/TEAM_ID/NOTARY_PASSWORD to enable)"
fi
