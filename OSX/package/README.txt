BUILDING YOUR AGS GAME FOR MACOS

This is a self-contained Xcode project. The AGS engine is already compiled
into Frameworks/AGSKit.xcframework, so nothing here builds the engine from
source and you do not need the AGS source tree.


STEPS

1. Copy this whole folder to a Mac with Xcode installed.

2. Unpack the frameworks. In Terminal, from this folder:

       sh unpack-frameworks.sh

   The frameworks in Frameworks/ ship as .zip archives because a macOS
   framework's internal symlinks do not survive the Windows machine the Editor
   runs on. This script restores them and clears the quarantine flag that a
   downloaded copy carries. DO THIS BEFORE OPENING XCODE -- Xcode resolves the
   framework references when a build starts, so a first build against the
   still-zipped frameworks fails. (Run it through "sh"; the executable bit is
   also lost on the Windows trip.)

3. Put your game's data files in Resources/. The AGS Editor does this for you
   when it exports the project; otherwise copy everything from your game
   project's Compiled/Data/ folder -- the .ags file, any numbered split
   resource files, audio.vox, speech.vox and acsetup.cfg. Do not copy
   winsetup.exe or any .dll, which are Windows-only.

4. Open AGSGame.xcodeproj and set your game's identity. The Editor writes
   AGSGame.xcconfig for you; if you are doing this by hand, edit that file and
   set PRODUCT_NAME, PRODUCT_BUNDLE_IDENTIFIER and MARKETING_VERSION. Then pick
   your team under the target's Signing & Capabilities tab.

5. Product > Archive, then Distribute App. For distribution outside the App
   Store choose Developer ID, which signs and notarizes the app so macOS will
   open it on other people's machines.

To check your work before archiving, just Product > Run. The shared scheme
already turns off Xcode's document-versions debug flag; without that, Xcode
passes "-NSDocumentRevisionsDebugMode YES" on launch and the engine mistakes
the trailing YES for a game path ("Unable to determine game data"). The built
.app is unaffected either way.


REPLACING THE ICON

The app icon is built from ags.iconset, which holds the default AGS icon at
every required size. Replace those PNGs with your own, keeping the file names
and pixel sizes exactly as they are, and Xcode compiles them into the bundle's
ags.icns on the next build.


SIGNING AND ENTITLEMENTS

AGSGame.entitlements disables library validation:

       <key>com.apple.security.cs.disable-library-validation</key>
       <true/>

This is required, not optional, for a normal Developer ID build.
SDL2.framework is the official prebuilt SDL release, signed by the SDL team
rather than by you. Under the hardened runtime, library validation refuses to
load any library whose Team ID differs from the app's, so without this key the
game fails at launch with "different Team IDs". Disabling it is fine for
Developer ID and notarizes without trouble.

Submitting to the Mac App Store instead. The App Store does not allow the
library-validation exception, so you must remove that key and re-sign SDL2 (and
any plugin dylibs) with your own identity, then add the sandbox:

       codesign --force --sign "Developer ID Application: Your Name (TEAMID)" \
         AGSGame.app/Contents/Frameworks/SDL2.framework

       <key>com.apple.security.app-sandbox</key>
       <true/>
       <key>com.apple.security.device.usb</key>
       <true/>

Note that sandboxing changes where your game's saved games and configuration
are written, so test saves and restores after turning it on.

Plugins. Engine plugins are loaded at runtime as lib<name>.dylib. With library
validation disabled they load regardless of who signed them; if you later
re-enable it for the App Store, re-sign each plugin with your identity the same
way as SDL2 above.


ADDING AN ENGINE PLUGIN FROM SOURCE

plugin_registration.cpp is where built-in plugins are registered, and the
commented-out block in it shows the shape. Add the plugin's source files to the
target, then fill in a pl_register_builtin_plugin call. The plugin API header
is in include/plugin/agsplugin.h, already on the target's header search path.


WHAT IS IN HERE

  AGSGame.xcodeproj              The Xcode project. One target, AGSGame.
  AGSGame.xcconfig               Game name, bundle id and version. Rewritten by
                                 the Editor.
  Info.plist                     Bundle metadata. Reads its values from the
                                 build settings above.
  AGSGame.entitlements           Disables library validation so the prebuilt
                                 SDL2 loads. See above.
  Frameworks/AGSKit.xcframework  The AGS engine, prebuilt for Intel and Apple
                                 Silicon.
  Frameworks/SDL2.framework      SDL2, embedded into the bundle and re-signed
                                 on copy.
  Resources/                     Your game's data files.
  ags.iconset                    The app icon sources.
  plugin_registration.cpp        Registration point for built-in engine
                                 plugins.
  include/                       Public AGS headers, for plugin sources you add.
