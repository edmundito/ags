# AGS Engine Plugin API

An engine plugin is a plain native DLL. It has no managed dependencies and must
ship alongside any game that uses it.

This reference is generated from `Engine/plugin/agsplugin.h`, which is the
single header a plugin includes and the source of truth for this API.

If you are looking to extend the *editor* rather than a game, you want the
[.NET editor plugin API](../editor-plugins.html) instead.

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
  passing an IAGSEditor. Use this to register script headers so the compiler
  knows about your plugin's script API.
- **Run time** — loaded by the engine when the game runs. The engine calls
  `AGS_EngineStartup`, passing an IAGSEngine. This is where the real work
  happens: registering script functions, hooking events, drawing.

@note The IAGSEditor documented here is the C++ interface for native plugins.
      It is unrelated to `AGS.Types.IAGSEditor`, the C# interface used by
      managed editor plugins, despite the shared name.

## Entry points

Define `THIS_IS_THE_PLUGIN` before including `agsplugin.h` to get the exported
declarations. The ones you almost always need:

| Export | Mode | Purpose |
|---|---|---|
| `AGS_GetPluginName` | both | Identifies the plugin. |
| `AGS_PluginV2` | both | Must be exported and return non-zero, so the engine can verify compatibility. |
| `AGS_EditorStartup` | design | Receives IAGSEditor. |
| `AGS_EditorShutdown` | design | Cleanup. |
| `AGS_EngineStartup` | run | Receives IAGSEngine. |
| `AGS_EngineShutdown` | run | Cleanup. |
| `AGS_EngineOnEvent` | run | Called for events you requested via IAGSEngine::RequestEventHook. |

## Interface versions

IAGSEngine::version tells you which interface version the running engine
provides. Every method, event and constant in this reference is tagged with the
version that introduced it.

**Check `version` before calling anything newer than the minimum you require**,
or you will call into an engine that has no such method.

The interface version is *not* related to the AGS version — they are
independent counters. See @ref engine_plugin_versions for what each version
added and which release it first shipped in.

## Portability

Plugins are **not portable**. A game using your plugin cannot be built for a
platform you have not supplied a plugin binary for. Binaries follow the
engine's own compilers: MSVC on Windows, GCC on Linux, Clang elsewhere.

## Where to start

- IAGSEngine — the run-time interface, and the bulk of this API
- IAGSEditor — the design-time interface
- @ref engine_plugin_versions — what each interface version added
- IAGSStream, IAGSFontRenderer, IAGSScriptManagedObject — interfaces your
  plugin implements or receives
