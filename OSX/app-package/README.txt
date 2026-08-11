BUILDING YOUR AGS GAME FOR MACOS (PREBUILT APP)

This folder is a prebuilt macOS app. The AGS engine is already compiled into
the shipped .app.zip, so nothing here builds from source, you do not need the
AGS source tree, and you do not need the full Xcode IDE -- only the Xcode
Command Line Tools ("xcode-select --install"), which provide codesign,
notarytool, stapler, sips and iconutil.

Because the engine binary is identical for every game, all you do on the Mac is
inject your game data.


BUILD THE APP

1. Copy this whole folder to a Mac.

2. In Terminal, from this folder:

       sh make-app.sh

   This unpacks the prebuilt app (restoring the internal symlinks a downloaded
   copy loses), injects the game data from Resources/, stamps the bundle's
   name, id and version from game.env (written by the AGS Editor), and converts
   an optional icon. The result is <your game>.app.

   With no signing identity set it is ad-hoc signed, which is fine to run and
   test on this Mac. To give it to other people, set an identity (see below).

3. Double-click the produced .app, or run it from Terminal, to check it.


REPLACING THE ICON

Drop a square icon.png (1024x1024 recommended) next to make-app.sh; it is
converted to ags.icns and baked into the bundle automatically. Without one, the
default AGS icon is used. You can also run the converter by hand:

       sh make-icon.sh icon.png ags.icns


SIGNING AND DISTRIBUTION (OPTIONAL)

You only need this to give the app to OTHER PEOPLE -- an unsigned/ad-hoc app
runs fine on the Mac that built it.

Put your Developer ID into make-app.sh (the variables near the top), or keep
them in a signing.env file next to it so they carry across every game:

       SIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)"
       # for notarization (needed to open on other people's Macs):
       APPLE_ID="you@example.com"
       TEAM_ID="TEAMID"
       NOTARY_PASSWORD="app-specific-password"

Find your identity with "security find-identity -v -p codesigning". With
SIGN_IDENTITY set, make-app.sh signs with it; with the notary variables also
set, it notarizes and staples so macOS opens the app on other machines.
Notarization needs a paid Apple Developer account. Without it, Gatekeeper
blocks the app on any machine other than the one that signed it.

Entitlements. AGSGame.entitlements disables library validation:

       <key>com.apple.security.cs.disable-library-validation</key>
       <true/>

This is required for a Developer ID build. SDL2.framework inside the bundle is
the official prebuilt SDL release, signed by the SDL team rather than by you.
Under the hardened runtime, library validation refuses to load a library whose
Team ID differs from the app's, so without this key the game fails at launch
with "different Team IDs". Disabling it is fine for Developer ID and notarizes
without trouble. SDL2.framework does not need re-signing per game -- it is the
same binary every time, and make-app.sh signs it along with the app.

Mac App Store instead. The App Store does not allow the library-validation
exception, so remove that key from AGSGame.entitlements, and add the sandbox:

       <key>com.apple.security.app-sandbox</key>
       <true/>
       <key>com.apple.security.device.usb</key>
       <true/>

Sandboxing changes where saved games and configuration are written, so test
saves and restores after turning it on.


PLUGINS

Engine plugins are loaded at runtime as lib<name>.dylib. Drop any such .dylib
next to make-app.sh; it is copied into AGSGame.app/Contents/Frameworks/ and
signed along with the app.


WHAT IS IN HERE

  <your game>.app.zip   The prebuilt AGS engine app, universal (Intel + Apple
                        Silicon), unsigned. Stays zipped until unpacked on the
                        Mac.
  make-app.sh           Builds your game into a .app: injects data, icon,
                        identity; signs if you provide an identity.
  make-icon.sh          Converts icon.png to ags.icns. Called by make-app.sh
                        when an icon is present.
  AGSGame.entitlements  Disables library validation so the prebuilt SDL2 loads.
                        See above.
  game.env              Game name, bundle id and version. Written by the AGS
                        Editor.
  Resources/            Your game's data files. Written by the AGS Editor.
