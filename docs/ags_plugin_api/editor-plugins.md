# Editor plugins

An editor plugin is a .NET assembly that extends the AGS Editor's UI and
behaviour. It is never shipped with a game — anything a *game* needs at run time
must be an [engine plugin](engine/index.html) instead.

Editor plugins require AGS 3.0 or later, and can be written in any .NET
language.

## Naming and placement

The editor scans its own directory at startup for `AGS.Plugin.*.dll`. Your
assembly's filename **must** begin with `AGS.Plugin.`. Place it in the AGS
Editor folder.

## What the editor requires of you

1. Reference `AGS.Types.dll` from the AGS installation directory. You may also
   reference `AGS.Controls.dll`, `AGS.Native.dll` and
   `AGS.CScript.Compiler.dll`.
2. Provide exactly one **public** class implementing
   `AGS.Types.IAGSEditorPlugin`.
3. Give that class a constructor taking a single `AGS.Types.IAGSEditor`
   argument. The editor constructs your class and hands you this object; it is
   your entire channel to the editor.
4. Put a `RequiredAGSVersion` attribute on that class, naming the lowest AGS
   version your plugin works with. `3.0.0.0` is fine for basic functionality;
   raise it if you use anything newer. Many `IAGSEditor` members document the
   version that introduced them.

## Components

Functionality is organised into **components**. A component covers one area —
the editor itself has a Characters component, a Fonts component, and so on. Your
plugin will usually need exactly one.

Implement `AGS.Types.IEditorComponent`, do your setup (tree items, menu entries)
in its constructor, and register it with `IAGSEditor.AddComponent`. Once added,
a component cannot be removed.

## Handling game data with care

`IAGSEditor.CurrentGame` gives you the loaded game. You can change it, and you
can break it: AGS makes assumptions about its data, particularly about item IDs.
Do not add or remove items unless you are certain of the consequences.

## Not to be confused with

The C++ `IAGSEditor` in `agsplugin.h` is a completely different interface for
native plugins. See the [note on the home page](index.md).

## Reference

- [Editor plugin API reference](api/index.md)
