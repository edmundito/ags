# Editor Plugin API Reference

Generated from the AGS Editor's own .NET assemblies. An editor plugin may
reference any of them, but only `AGS.Types` is required.

See [Editor plugins](../editor-plugins.md) for what the editor requires of your
plugin before any of this is reachable.

## Where to start

| Type | Why |
|---|---|
| <xref:AGS.Types.IAGSEditorPlugin> | Your main class implements this. The editor finds it by scanning for it. |
| <xref:AGS.Types.IAGSEditor> | Passed to your main class's constructor. Your entire channel to the editor. |
| <xref:AGS.Types.IEditorComponent> | One area of functionality. Register it with `IAGSEditor.AddComponent`. |
| <xref:AGS.Types.IGUIController> | Menus, panes, dialogs. Reached via `IAGSEditor.GUIController`. |
| <xref:AGS.Types.IGame> | The loaded game. Reached via `IAGSEditor.CurrentGame`. Change it carefully. |

## Assemblies

- **AGS.Types** — the interfaces and the game data model. Required.
- **AGS.Controls** — reusable WinForms controls the editor itself uses, so a
  plugin's UI can match the host.
- **AGS.Native** — the C++/CLI bridge to the native `Common/` layer: sprite
  files, room files, game data. Present only if the site was generated from a
  build that included it.
- **AGS.CScript.Compiler** — the editor-side C# script tooling, separate from
  the native compiler the editor actually ships games with.

> [!NOTE]
> `AGS.Types.IAGSEditor` is not the C++ `IAGSEditor` in `agsplugin.h`. Those are
> unrelated interfaces that happen to share a name. See the
> [home page](../index.md).
