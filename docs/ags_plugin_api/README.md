# AGS Plugin API documentation

Generates a browsable API reference for both kinds of AGS plugin:

- **Engine plugins** — native DLLs built against `Engine/plugin/agsplugin.h`.
- **Editor plugins** — .NET assemblies built against `AGS.Types` and friends.

Output is a self-contained static site in `_site/`. Nothing here is committed;
everything it produces is gitignored.

## Prerequisites

- **Doxygen 1.9 or newer** on `PATH` — `winget install DimitriVanHeesch.Doxygen`
- **.NET SDK 8 or newer**, plus the **.NET 8 runtime** (DocFX 2.78 targets it;
  a newer SDK alone is not enough). Check with `dotnet --list-runtimes`.
- **A prior build of the editor's C# assemblies.** DocFX reads compiled
  assemblies, not project files. Build `Solutions/AGS.Editor.Full.sln` for the
  complete site, or any solution producing `AGS.Types.dll` for most of it.
  `AGS.Native.dll` additionally needs the MSVC C++/CLI toolchain; without it
  the build warns and omits that one section.

## Usage

```powershell
pwsh -NoProfile -File ./build.ps1                     # build into _site/
pwsh -NoProfile -File ./build.ps1 -Configuration Debug
pwsh -NoProfile -File ./build.ps1 -Serve              # build, then serve locally
pwsh -NoProfile -File ./verify.ps1                    # assert the site is sane
```

## Keeping it accurate

`Engine/plugin/agsplugin.h` is now the source of truth for the engine plugin
API. Doxygen runs with `WARN_AS_ERROR = FAIL_ON_WARNINGS`, so a new member added
without a documentation comment fails this build.

The manual's `EnginePluginRun-timeAPI` and `EnginePluginDesign-timeAPI` pages in
the separate `ags-manual-source` repository now duplicate this content and are
candidates to be replaced with a pointer here. That is a separate change in a
separate repository.
