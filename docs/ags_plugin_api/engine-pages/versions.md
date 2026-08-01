@page engine_plugin_versions Interface versions

# Engine plugin interface versions

IAGSEngine::version tells a plugin which interface version the running engine
provides. Every method, event and constant in this reference is tagged with the
version that introduced it. Check `version` before calling anything newer than
the minimum your plugin requires — an older engine simply does not have the
method, and calling it is undefined behaviour, not a graceful failure.

The current version is `30`, defined as `PLUGIN_API_VERSION` in
`Engine/plugin/agsplugin.cpp`.

## Is the interface version related to the engine version?

**No.** They are independent counters. The interface version is a single
integer bumped by hand whenever something is added to the plugin API, and it
carries no information about which AGS release you are running on. There is no
formula converting one to the other, and a plugin must never try to derive one
from the other — use `IAGSEngine::version` for interface capability and
`IAGSEngine::GetEngineVersion` for the engine's own version string.

The mapping is only discoverable from history. For the versions added since AGS
went open source, the first release containing each bump was:

| Interface version | First AGS release | Bumped |
|---|---|---|
| 25 | 3.6.0.6 | 2021-04-15 |
| 26 | 3.6.0.49 | 2022-07-17 |
| 27 | 3.6.1.12 | 2023-11-13 |
| 28 | 3.6.1.12 | 2023-11-15 |
| 29 | 3.6.2.1 | 2024-05-20 |
| 30 | 3.6.2.5 | 2024-12-20 |

Versions 27 and 28 landed two days apart and shipped in the same release, which
is the clearest illustration of why the two numbers cannot be correlated: an
engine version does not imply an interface version, or the reverse.

Versions 1–24 predate this history and have no recorded release mapping. In
practice any engine you will meet today provides at least version 26.

## What each version added

Entries marked *(Windows only)* are compiled out on other platforms.

| Version | Added |
|---|---|
| 1 | `AbortGame`, `GetEngineVersion`, `RegisterScriptFunction`, `GetScreen`; `GetWindowHandle`, `GetDirectDraw2`, `GetBitmapSurface` *(Windows only)* |
| 2 | `RequestEventHook`, `GetSavedData` |
| 3 | `GetVirtualScreen`, `DrawText`, `GetScreenDimensions`, `GetRawBitmapSurface`, `ReleaseBitmapSurface`, `GetMousePosition`; events `AGSE_KEYPRESS`, `AGSE_MOUSECLICK`, `AGSE_POSTSCREENDRAW` |
| 4 | `GetCurrentRoom`, `GetNumBackgrounds`, `GetCurrentBackground`, `GetBackgroundScene`, `GetBitmapDimensions`; event `AGSE_PRESCREENDRAW` |
| 5 | `FWrite`, `FRead`, `DrawTextWrapped`, `SetVirtualScreen`, `LookupParserWord`, `BlitBitmap`, `PollSystem`; events `AGSE_SAVEGAME`, `AGSE_RESTOREGAME` |
| 6 | `GetNumCharacters`, `GetCharacter`, `GetGameOptions`, `GetPalette`, `SetPalette`; events `AGSE_PREGUIDRAW`, `AGSE_LEAVEROOM`, `AGSE_ENTERROOM`, `AGSE_TRANSITIONIN`, `AGSE_TRANSITIONOUT` |
| 7 | `GetPlayerCharacter`, `RoomToViewport`, `ViewportToRoom`, `GetNumObjects`, `GetObject`, `GetSpriteGraphic`, `CreateBlankBitmap`, `FreeBitmap` |
| 8 | `GetRoomMask` |
| 9 | `GetViewFrame`, `GetWalkbehindBaseline`, `GetScriptFunctionAddress`, `GetBitmapTransparentColor`, `GetAreaScaling`, `IsGamePaused` |
| 10 | `GetRawPixelColor` |
| 11 | `GetSpriteWidth`, `GetSpriteHeight`, `GetTextExtent`, `PrintDebugConsole`, `PlaySoundChannel`, `IsChannelPlaying`; the `MASK_REGIONS` room mask |
| 12 | `MarkRegionDirty`, `GetMouseCursor`, `GetRawColorComponents`, `MakeRawColorPixel`, `GetFontType`, `CreateDynamicSprite`, `DeleteDynamicSprite`, `IsSpriteAlphaBlended`; events `AGSE_FINALSCREENDRAW`, `AGSE_TRANSLATETEXT` |
| 13 | `UnrequestEventHook`, `BlitSpriteTranslucent`, `BlitSpriteRotated`; event `AGSE_SCRIPTDEBUG`; the `AGS_EngineDebugHook` and `AGS_EngineShutdown` exports |
| 14 | `DisableSound`, `CanRunScriptFunctionNow`, `CallGameScriptFunction`; `GetDirectSound` *(Windows only)* |
| 15 | `NotifySpriteUpdated`, `SetSpriteAlphaBlended`, `QueueGameScriptFunction`, `RegisterManagedObject`, `AddManagedObjectReader`, `RegisterUnserializedObject` |
| 16 | `GetManagedObjectAddressByKey`, `GetManagedObjectKeyByAddress` |
| 17 | `CreateScriptString` |
| 18 | `IncrementManagedObjectRefCount`, `DecrementManagedObjectRefCount`, `SetMousePosition`, `SimulateMouseClick`, `GetMovementPathWaypointCount`, `GetMovementPathLastWaypoint`, `GetMovementPathWaypointLocation`, `GetMovementPathWaypointSpeed`; event `AGSE_SPRITELOAD` |
| 19 | `GetGraphicsDriverID` |
| 20 | Render-stage events pass the live `IDirect3DDevice9` as their data value under the D3D9 driver |
| 21 | Event `AGSE_PRERENDER` |
| 22 | `IsRunningUnderDebugger`, `BreakIntoDebugger`, `GetPathToFileInCompiledFolder` |
| 23 | `ReplaceFontRenderer`, the `AGS_EngineInitGfx` export; `GetDirectInputKeyboard`, `GetDirectInputMouse` *(Windows only)* |
| 24 | Events `AGSE_PRESAVEGAME`, `AGSE_POSTRESTOREGAME` |
| 25 | `GetRenderStageDesc`, and the `AGSRenderStageDesc` / `AGSRenderMatrixes` structs |
| 26 | `GetGameInfo`, `ReplaceFontRenderer2`, `NotifyFontUpdated`; event `AGSE_POSTROOMDRAW`; the `AGSGameInfo` struct and `IAGSFontRenderer2` interface |
| 27 | `ResolveFilePath`, deprecating `GetPathToFileInCompiledFolder` |
| 28 | `OpenFileStream`, `GetFileStreamByHandle`, and the `IAGSStream` interface |
| 29 | `Log` |
| 30 | `CreateDynamicArray`, `GetDynamicArrayLength`, `GetDynamicArraySize` |

## Deprecated and removed

| What | Status |
|---|---|
| `AGSE_AUDIODECODE` | No longer supported. The engine never raises it. |
| `GetPathToFileInCompiledFolder` | Unsafe, deprecated since version 27. Use `ResolveFilePath`. |
| `PrintDebugConsole` | The in-game debug console was removed in AGS 3.6.1. The function now writes to the engine log; prefer `Log`. |
| `GetDirectDraw2`, `GetBitmapSurface`, `GetScreen` | DirectDraw-era, usable only under the long-obsolete DX5 driver. |
