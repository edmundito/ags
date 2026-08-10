# Building your AGS game for macOS (prebuilt app)

This folder is a **prebuilt macOS app**. The AGS engine is already compiled into
`AGSGame.app.zip`, so nothing here builds from source, you do not need the AGS
source tree, and you do not need the full Xcode IDE -- only the **Xcode Command
Line Tools** (`xcode-select --install`), which provide `codesign`, `notarytool`,
`stapler`, `sips` and `iconutil`.

Because the engine binary is identical for every game, all you do on the Mac is
inject your game data and re-sign the app with your own certificate.

## Steps

1. Copy this whole folder to a Mac.

2. Put your signing identity into `sign.sh` (edit the variables near the top), or
   keep them in a `signing.env` file next to `sign.sh` so they carry across every
   game you build:

   ```sh
   SIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)"
   # optional, for notarization:
   APPLE_ID="you@example.com"
   TEAM_ID="TEAMID"
   NOTARY_PASSWORD="app-specific-password"
   ```

   Find your identity string with `security find-identity -v -p codesigning`.

3. In Terminal, from this folder:

   ```sh
   sh sign.sh
   ```

   This unpacks the prebuilt app (restoring the internal symlinks that a
   downloaded copy loses), injects the game data from `Resources/`, patches the
   bundle's name, id and version from `game.env` (written by the AGS Editor),
   converts an optional icon, then signs inside-out and -- if you set the notary
   variables -- notarizes and staples. The result is `<your game>.app`.

To check your work first, just double-click the produced `.app`, or run it from
Terminal.

## Replacing the icon

Drop a square `icon.png` (1024x1024 recommended) next to `sign.sh`; it is
converted to `ags.icns` and baked into the bundle automatically before signing.
Without one, the default AGS icon is used. You can also run the converter by
hand:

```sh
sh make-icon.sh icon.png ags.icns
```

## Signing and entitlements

`AGSGame.entitlements` disables **library validation**:

```xml
<key>com.apple.security.cs.disable-library-validation</key>
<true/>
```

This is required, not optional, for a normal Developer ID build. `SDL2.framework`
inside the bundle is the official prebuilt SDL release, signed by the SDL team
rather than by you. Under the hardened runtime, library validation refuses to
load any library whose Team ID differs from the app's, so without this key the
game fails at launch with *"different Team IDs"*. Disabling it is fine for
Developer ID and notarizes without trouble.

`SDL2.framework` does not need re-signing per game -- it is the same binary every
time. `sign.sh` re-signs it anyway (harmless, byte-identical result) so the whole
bundle carries a consistent, timestamped signature.

**Submitting to the Mac App Store instead.** The App Store does not allow the
library-validation exception, so remove that key from `AGSGame.entitlements`,
re-sign SDL2 (and any plugin dylibs) with your own identity, and add the sandbox:

```xml
<key>com.apple.security.app-sandbox</key>
<true/>
<key>com.apple.security.device.usb</key>
<true/>
```

Note that sandboxing changes where your game's saved games and configuration are
written, so test saves and restores after turning it on.

## Notarization

Distributing to other people's Macs requires notarization, which needs a paid
Apple Developer account. Set `APPLE_ID`, `TEAM_ID` and `NOTARY_PASSWORD` (an
app-specific password from appleid.apple.com) and `sign.sh` submits and staples
for you. Without notarization the app runs only on the machine that signed it --
Gatekeeper blocks it elsewhere.

## Plugins

Engine plugins are loaded at runtime as `lib<name>.dylib`. Drop any such `.dylib`
next to `sign.sh`; it is copied into `AGSGame.app/Contents/Frameworks/` and signed
with your identity. With library validation disabled they load regardless of who
originally signed them.

## What is in here

| Path | What it is |
| --- | --- |
| `AGSGame.app.zip` | The prebuilt AGS engine app, universal (Intel + Apple Silicon), unsigned. Stays zipped until unpacked on the Mac. |
| `AGSGame.entitlements` | Disables library validation so the prebuilt SDL2 loads. See above. |
| `sign.sh` | Assembles game data into the app, patches identity, signs and notarizes. |
| `make-icon.sh` | Converts `icon.png` to `ags.icns`. Called by `sign.sh` when an icon is present. |
| `game.env` | Game name, bundle id and version. Written by the AGS Editor. |
| `Resources/` | Your game's data files. Written by the AGS Editor. |
