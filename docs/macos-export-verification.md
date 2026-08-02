# Verifying the macOS export template on a Mac

Manual test plan for `OSX/package` (the exportable Xcode project) and
`OSX/osx-build.sh` (the script that assembles it). Everything here needs a Mac
with Xcode 15 or newer; none of it can be checked from Windows, which is why
it is written down rather than automated.

You need a built AGS game to test with — any game's `Compiled/Data` folder,
produced by the Editor on Windows and copied across.

---

## 1. Assemble the template

```sh
git clone <your fork> ags && cd ags
git checkout edmunditor/macOS-export

cd OSX
./osx-build.sh prepare build_release
```

`prepare` downloads the library sources, unpacks `SDL2.framework` out of the
SDL disk image, and copies `Engine/plugin/agsplugin.h` into the template.
`build_release` compiles the engine and wraps it as an xcframework. Expect the
compile to take several minutes the first time.

**What to check**

| Check | Command | Expected |
| --- | --- | --- |
| Both frameworks landed | `ls package/Frameworks` | `AGSKit.xcframework` and `SDL2.framework` |
| Engine is universal | `lipo -archs package/Frameworks/AGSKit.xcframework/macos-*/AGSKit.framework/AGSKit` | `x86_64 arm64` |
| Engine is a static library | `file package/Frameworks/AGSKit.xcframework/macos-*/AGSKit.framework/AGSKit` | `current ar archive` — a static framework, which is why it never needs signing of its own |
| Plugin header copied | `ls package/include/plugin` | `agsplugin.h` |
| Deployment target | `otool -l package/Frameworks/AGSKit.xcframework/macos-*/AGSKit.framework/AGSKit \| grep -A3 LC_BUILD_VERSION \| head` | `minos 10.13` |

If `prepare` fails on the checksum step, that is `libsrc/download.sh` doing
its own verification — the downloads are bad or partial, not the script.

---

## 2. Build a game

```sh
cp /path/to/YourGame/Compiled/Data/* OSX/package/Resources/

xcodebuild -project OSX/package/mygame.xcodeproj -scheme mygame \
  -configuration Release -derivedDataPath /tmp/mygame-dd \
  CODE_SIGNING_ALLOWED=NO build

open /tmp/mygame-dd/Build/Products/Release/MyGame.app
```

Do not copy `winsetup.exe` or any `.dll` — those are Windows-only. The Editor
filters them out; by hand, you have to.

**The pbxproj was written by hand and has never been compiled**, so the first
thing to learn is simply whether Xcode accepts it. Open
`OSX/package/mygame.xcodeproj` in Xcode: no file in the navigator should be
red, and the project should open without a parse error.

Then, in order of what would be most surprising if wrong:

| Check | Command | Expected |
| --- | --- | --- |
| The xcconfig is actually applied | `xcodebuild -project OSX/package/mygame.xcodeproj -scheme mygame -showBuildSettings \| grep -E '^ +(PRODUCT_NAME\|PRODUCT_BUNDLE_IDENTIFIER\|MARKETING_VERSION) '` | `MyGame`, `com.mystudio.mygame`, `1.0`. Empty or wrong means the base configuration reference is not wired up, and the Editor would have no way to stamp a game's identity. |
| Game data reached the bundle, flat | `ls /tmp/mygame-dd/Build/Products/Release/MyGame.app/Contents/Resources` | Your `.ags`, `acsetup.cfg`, any `.vox` and split `.001` files — all directly in `Resources`, not in a subfolder. Plus `ags.icns` and `PrivacyInfo.xcprivacy`. |
| The icon compiled | same listing | `ags.icns` present, proving Xcode's iconset rule fired on `ags.iconset` |
| App is universal | `lipo -archs .../MyGame.app/Contents/MacOS/MyGame` | `x86_64 arm64` |
| SDL2 is embedded | `ls .../MyGame.app/Contents/Frameworks` | `SDL2.framework` |
| **The game runs** | `open MyGame.app` | The game starts and is playable |
| Saves work | save and reload in-game | Confirms nothing about the bundle layout broke the save path |

The "game runs" line is the one that matters most. It is the only check that
proves the flat copy into `Resources` is where the engine actually looks:
`AGSMacInitPaths` (`Engine/platform/osx/alplmac.mm`) chdirs to the bundle's
`Resources` directory, and `FindGameData` (`Engine/main/engine.cpp`) then
scans that one directory, non-recursively, for any `.ags` file.

---

## 3. Deliberate behaviors, not bugs

Two things are supposed to happen. Confirm them rather than assuming they are
defects if you trip over them.

**An empty `Resources/` fails the build with a readable message.**

```sh
mkdir -p /tmp/stash && mv OSX/package/Resources/*.ags /tmp/stash/
xcodebuild -project OSX/package/mygame.xcodeproj -scheme mygame \
  -configuration Release -derivedDataPath /tmp/mygame-dd \
  CODE_SIGNING_ALLOWED=NO build
mv /tmp/stash/*.ags OSX/package/Resources/
```

Expected: the build fails with `error: no .ags game data found in
.../Resources`. A silently-built app that crashes at launch would be worse.

**Renaming the product works.** Edit `mygame.xcconfig` and set
`PRODUCT_NAME = SomethingElse`, rebuild, and you should get
`SomethingElse.app` with the same contents and `CFBundleName` matching. This
is exactly what the Editor will do in Part 2, so it has to work.

```sh
/usr/libexec/PlistBuddy -c 'Print :CFBundleName' \
  /tmp/mygame-dd/Build/Products/Release/SomethingElse.app/Contents/Info.plist
```

---

## 4. Signing, if you have an identity

Everything above runs with signing disabled. With a Developer ID certificate,
do one real end-to-end pass, since signing is the whole reason the export
exists.

In Xcode: set your team in `mygame.xcconfig` (`DEVELOPMENT_TEAM`) or in the
target's Signing & Capabilities tab, then **Product > Archive**, then
**Distribute App > Developer ID**.

```sh
codesign -dv --entitlements - /path/to/exported/MyGame.app
```

Expected: `flags=0x10000(runtime)` — the hardened runtime is on — and an empty
entitlements dict. The template ships no entitlements on purpose; a Developer
ID build needs none, and every entitlement added is one more thing
notarization looks at.

Then confirm the notarized app opens on a Mac that has never seen it before,
ideally one of a different architecture than the one you built on.

---

## 5. Archiving

```sh
cd OSX && ./osx-build.sh archive_project
unzip -l ../AGS-*-macos-proj.zip | head -30
```

Expected entries: `mygame.xcodeproj/project.pbxproj`, `mygame.xcconfig`,
`Frameworks/AGSKit.xcframework/Info.plist`,
`Frameworks/SDL2.framework/SDL2`, `include/plugin/agsplugin.h`, and
`Resources/` — carrying whatever you left in it, so clear it first if you do
not want your test game inside the archive. There should be no `build/`
directory; `archive_project` removes it.

This zip is what CI will publish and what the Editor installer will unpack, so
its layout is the contract that Part 2's `BuildTargetMacOS` depends on.

---

## What to report back

For each numbered section: pass, or the exact command and the error. Build
failures from `xcodebuild` are most useful with the surrounding lines — the
first error, not the summary at the end.
