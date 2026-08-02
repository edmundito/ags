# Building your AGS game for macOS

This is a self-contained Xcode project. The AGS engine is already compiled
into `Frameworks/AGSKit.xcframework`, so nothing here builds the engine from
source and you do not need the AGS source tree.

## Steps

1. Copy this whole folder to a Mac with Xcode installed.

2. Put your game's data files in `Resources/`. The AGS Editor does this for
   you when it exports the project; otherwise copy everything from your game
   project's `Compiled/Data/` folder -- the `.ags` file, any numbered split
   resource files, `audio.vox`, `speech.vox` and `acsetup.cfg`. Do not copy
   `winsetup.exe` or any `.dll`, which are Windows-only.

3. Open `mygame.xcodeproj` and set your game's identity. The Editor writes
   `mygame.xcconfig` for you; if you are doing this by hand, edit that file
   and set `PRODUCT_NAME`, `PRODUCT_BUNDLE_IDENTIFIER` and
   `MARKETING_VERSION`. Then pick your team under the target's
   **Signing & Capabilities** tab.

4. **Product > Archive**, then **Distribute App**. For distribution outside
   the App Store choose *Developer ID*, which signs and notarizes the app so
   macOS will open it on other people's machines.

To check your work before archiving, just **Product > Run**.

## Replacing the icon

The app icon is built from `ags.iconset`, which holds the default AGS icon at
every required size. Replace those PNGs with your own, keeping the file names
and pixel sizes exactly as they are, and Xcode compiles them into the bundle's
`ags.icns` on the next build.

## Signing and entitlements

`mygame.entitlements` is deliberately empty. A Developer ID build with the
hardened runtime needs no entitlements, and every entitlement you add is
something Apple's notarization service looks at more closely.

Two cases where you do need to add keys:

**Submitting to the Mac App Store.** Add the sandbox:

```xml
<key>com.apple.security.app-sandbox</key>
<true/>
<key>com.apple.security.device.usb</key>
<true/>
```

Note that sandboxing changes where your game's saved games and configuration
are written, so test saves and restores after turning it on.

**Shipping a plugin binary you cannot re-sign.** Engine plugins are loaded at
runtime as `lib<name>.dylib`. Xcode signs everything it copies into the bundle
with your certificate, so plugins you build yourself are fine. A prebuilt
plugin from someone else will fail the hardened runtime's library validation
unless you either re-sign it yourself:

```sh
codesign --force --sign "Developer ID Application: Your Name (TEAMID)" libyourplugin.dylib
```

or, as a last resort, weaken the runtime by adding:

```xml
<key>com.apple.security.cs.disable-library-validation</key>
<true/>
```

Re-signing is the better option.

## Adding an engine plugin from source

`plugin_registration.cpp` is where built-in plugins are registered, and the
commented-out block in it shows the shape. Add the plugin's source files to
the target, then fill in a `pl_register_builtin_plugin` call. The plugin API
header is in `include/plugin/agsplugin.h`, already on the target's header
search path.

## What is in here

| Path | What it is |
| --- | --- |
| `mygame.xcodeproj` | The Xcode project. One target, `mygame`. |
| `mygame.xcconfig` | Game name, bundle id and version. Rewritten by the Editor. |
| `Info.plist` | Bundle metadata. Reads its values from the build settings above. |
| `mygame.entitlements` | Empty by default. See above. |
| `Frameworks/AGSKit.xcframework` | The AGS engine, prebuilt for Intel and Apple Silicon. |
| `Frameworks/SDL2.framework` | SDL2, embedded into the bundle and re-signed on copy. |
| `Resources/` | Your game's data files. |
| `ags.iconset` | The app icon sources. |
| `plugin_registration.cpp` | Registration point for built-in engine plugins. |
| `include/` | Public AGS headers, for plugin sources you add. |
