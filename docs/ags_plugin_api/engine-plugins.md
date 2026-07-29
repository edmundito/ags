# Engine plugins

An engine plugin is a plain native DLL. It has no managed dependencies and
must ship alongside any game that uses it.

## Naming and placement

The editor scans its own directory at startup for files matching `ags*.dll` and
lists what it finds. The filename **must** begin with `ags`. Place the plugin in
the directory containing `AGSEditor.exe`, not in your game folder.

Other platforms use the `libags*.so` convention, in per-platform directories
relative to the editor: `Linux/lib32` and `Linux/lib64`, and
`Android/plugins/<abi>/`. The web port does not support plugins.

## The two modes

A plugin is loaded in one of two modes:

- **Design time** — loaded by the editor. The editor calls `AGS_EditorStartup`,
  passing a C++ `IAGSEditor`. Use this to register script headers so the
  compiler knows about your plugin's script API.
- **Run time** — loaded by the engine when the game runs. The engine calls
  `AGS_EngineStartup`, passing an `IAGSEngine`. This is where the real work
  happens: registering script functions, hooking events, drawing.

## Entry points

Define `THIS_IS_THE_PLUGIN` before including `agsplugin.h` to get the exported
declarations. The full set is in the API reference; the ones you almost always
need are:

| Export | Mode | Purpose |
|---|---|---|
| `AGS_GetPluginName` | both | Identifies the plugin. |
| `AGS_PluginV2` | both | Must be exported and return non-zero, so the engine can verify compatibility. |
| `AGS_EditorStartup` | design | Receives `IAGSEditor`. |
| `AGS_EditorShutdown` | design | Cleanup. |
| `AGS_EngineStartup` | run | Receives `IAGSEngine`. |
| `AGS_EngineShutdown` | run | Cleanup. |
| `AGS_EngineOnEvent` | run | Called for events you requested via `RequestEventHook`. |

## Interface versions

`IAGSEngine::version` tells you which interface version the running engine
provides. Every method in the reference is tagged with the version that
introduced it. **Check `version` before calling anything newer than the minimum
you require**, or you will call into an engine that has no such method.

## Portability

Plugins are **not portable**. A game using your plugin cannot be built for a
platform you have not supplied a plugin binary for. Binaries follow the engine's
own compilers: MSVC on Windows, GCC on Linux, Clang elsewhere.

## Reference

- [Engine plugin API reference](engine/index.html)
