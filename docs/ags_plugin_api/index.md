# AGS Plugin API

AGS supports two unrelated kinds of plugin. They share a name and nothing else —
different languages, different loading mechanisms, different interfaces.

## Engine plugins

Native DLLs (`ags*.dll` on Windows, `libags*.so` elsewhere) that extend what a
*game* can do. They include a single header, `Engine/plugin/agsplugin.h`, and
ship with the game at run time. They are loaded by both the editor
(design time) and the engine (run time).

- [Engine plugin API](engine/index.html) — overview, interface versions and the
  full reference, generated from `agsplugin.h`

## Editor plugins

.NET assemblies (`AGS.Plugin.*.dll`) that extend the *editor*. They reference
the editor's own libraries and are never shipped with a game.

- [Editor plugin overview](editor-plugins.md)
- [Editor plugin API reference](api/index.md)

## A warning about the name `IAGSEditor`

There are **two different interfaces named `IAGSEditor`**, and they have nothing
to do with each other:

| | Language | Where | What it is |
|---|---|---|---|
| `IAGSEditor` | C++ | `agsplugin.h` | Handed to a *native* plugin's `AGS_EditorStartup` when the editor loads it at design time. Four methods. |
| `AGS.Types.IAGSEditor` | C# | `AGS.Types.dll` | Handed to a *managed* plugin's constructor. The main entry point for editor plugins. |

If you are writing a C# plugin, you want the second one.
