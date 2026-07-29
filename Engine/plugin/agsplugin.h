//=============================================================================
//
// Adventure Game Studio (AGS)
//
// Copyright (C) 1999-2011 Chris Jones and 2011-2026 various contributors
// The full list of copyright holders can be found in the Copyright.txt
// file, which is part of this source code distribution.
//
// The AGS source code is provided under the Artistic License 2.0.
// A copy of this license can be found in the file License.txt and at
// https://opensource.org/license/artistic-2-0/
//
//=============================================================================
//
// AGS Plugin interface header file.
//
// #define THIS_IS_THE_PLUGIN beforehand if including from the plugin.
//
//=============================================================================
/**
 * @file
 * Native AGS engine plugin interface.
 */
#ifndef _AGS_PLUGIN_H
#define _AGS_PLUGIN_H

#include <stddef.h> // for size_t
#include <stdint.h>

// If the plugin isn't using DDraw, don't require the headers
#ifndef DIRECTDRAW_VERSION
/** Opaque DirectDraw interface pointer. */
typedef void *LPDIRECTDRAW2;
/** Opaque DirectDraw surface interface pointer. */
typedef void *LPDIRECTDRAWSURFACE2;
#endif

#ifndef DIRECTSOUND_VERSION
/** Opaque DirectSound interface pointer. */
typedef void *LPDIRECTSOUND;
#endif

#ifndef DIRECTINPUT_VERSION
/** Opaque DirectInput device interface pointer. */
typedef void *LPDIRECTINPUTDEVICE;
#endif

// If the user isn't using Allegro or WinGDI, define the BITMAP into something
#if !defined(ALLEGRO_H) && !defined(_WINGDI_) && !defined(BITMAP_DEFINED)
/** Opaque bitmap type. */
typedef void BITMAP;
#endif

// If not using windows.h, define HWND
#if !defined(_WINDOWS_)
/** Opaque window-handle type. */
typedef void *HWND;
#endif

// This file is distributed as part of the Plugin API docs, so
// ensure that WINDOWS_VERSION is defined (if applicable)
#if defined(_WIN32)
  #undef WINDOWS_VERSION
  /** Indicates a Windows plugin build. */
  #define WINDOWS_VERSION
#endif

// DOS engine doesn't know about stdcall / neither does Linux version
#if !defined (_WIN32)
  #define __stdcall
#endif

#ifndef int32
/** Signed 32-bit integer type. */
typedef int int32;
#endif

/** Declares an engine interface function. */
#define AGSIFUNC(type) virtual type __stdcall

/**
 * @name Room area mask types
 * Passed to IAGSEngine::GetRoomMask.
 * @{
 */
/** Walkable-area mask. */
#define MASK_WALKABLE   1
/** Walk-behind-area mask. */
#define MASK_WALKBEHIND 2
/** Hotspot-area mask. */
#define MASK_HOTSPOT    3
/** @since Interface version 11 */
#define MASK_REGIONS    4
/** @} */

// **** WARNING: DO NOT ALTER THESE CLASSES IN ANY WAY!
// **** CHANGING THE ORDER OF THE FUNCTIONS OR ADDING ANY VARIABLES
// **** WILL CRASH THE SYSTEM.

/**
 * An RGB color in the game's current palette.
 *
 * Obtain the 256-color palette with IAGSEngine::GetPalette.
 *
 * @see IAGSEngine::GetPalette
 */
struct AGSColor {
  /** Red component. */ unsigned char r;
  /** Green component. */ unsigned char g;
  /** Blue component. */ unsigned char b;
  /** Structure padding. */ unsigned char padding;
};

/**
 * The game's current state, similar to the text script @c game variables.
 *
 * Obtain this structure with IAGSEngine::GetGameOptions.
 *
 * @see IAGSEngine::GetGameOptions
 */
struct AGSGameOptions {
  /** Player's current score. */ int32 score;
  /** Last cursor mode used by ProcessClick. */ int32 usedmode;
  /** Positive while in a cutscene or similar state. */ int32 disabled_user_interface;
  /** Obsolete timer. */ int32 gscript_timer;
  /** Whether debug mode is active. */ int32 debug_mode;
  /** Obsolete global variables. */ int32 globalvars[50];
  /** Time remaining for auto-removed messages. */ int32 messagetime;
  /** Last used inventory item. */ int32 usedinv;
  /** Top displayed inventory item. */ int32 inv_top;
  /** Number of displayed inventory items. */ int32 inv_numdisp;
  /** Internal inventory-display bookkeeping; not documented by the manual. */ int32 inv_numorder;
  /** Internal inventory-display bookkeeping; not documented by the manual. */ int32 inv_numinline;
  /** Text removal speed. */ int32 text_speed;
  /** Default inventory-window background color. */ int32 sierra_inv_color;
  /** Talking-animation speed. */ int32 talkanim_speed;
  /** Inventory item width set by SetInvDimensions. */ int32 inv_item_wid;
  /** Inventory item height set by SetInvDimensions. */ int32 inv_item_hit;
  /** Outline-font shadow color. */ int32 speech_text_shadow;
  /** Sierra-style speech portrait-side setting. */ int32 swap_portrait_side;
  /** Text window for Sierra-style speech. */ int32 speech_textwindow_gui;
  /** Delay before following characters change room. */ int32 follow_change_room_timer;
  /** Maximum possible score. */ int32 totalscore;
  /** Controls how Display windows are skipped. */ int32 skip_display;
  /** Backward-compatibility setting. */ int32 no_multiloop_repeat;
  /** Whether the room's on_call function has finished. */ int32 roomscript_finished;
  /** Inventory item selected by the player. */ int32 used_inv_on;
  /** Whether voice speech has no text-window background. */ int32 no_textbg_when_voice;
  /** Maximum dialog-options text window width. */ int32 max_dialogoption_width;
  /** Whether hi-color fade-in is instant. */ int32 no_hicolor_fadein;
  /** Whether background speech uses game speed. */ int32 bgspeech_game_speed;
  /** Whether background speech stays displayed during DisplaySpeech. */ int32 bgspeech_stay_on_display;
  /** Whether "&10" is omitted when calculating text display time. */ int32 unfactor_speech_from_textlength;
  /** Milliseconds before a track loops. */ int32 mp3_loop_before_end;
  /** Music-volume reduction during speech. */ int32 speech_music_drop;
  /** Whether the game is between StartCutscene and EndCutscene. */ int32 in_cutscene;
  /**
   * Whether the player is skipping a cutscene.
   *
   * Plugin code that updates game state must still run while this is set;
   * skipping such updates while the player fast-forwards through a cutscene
   * will desync the game.
   */
  int32 fast_forward;
  /** Current room width. */ int32 room_width;
  /** Current room height. */ int32 room_height;
};

// AGSCharacter.flags
/** Character flag. */
#define CHF_NOSCALING       1
/** Character flag. */
#define CHF_FIXVIEW         2     // between SetCharView and ReleaseCharView
/** Character flag. */
#define CHF_NOINTERACT      4
/** Character flag. */
#define CHF_NODIAGONAL      8
/** Character flag. */
#define CHF_ALWAYSIDLE      0x10
/** Character flag. */
#define CHF_NOLIGHTING      0x20
/** Character flag. */
#define CHF_NOTURNING       0x40
/** Character flag. */
#define CHF_NOWALKBEHINDS   0x80

/**
 * The state of a game character, mostly identical to the @c character[] text
 * script variable.
 *
 * Obtain this structure with IAGSEngine::GetCharacter. View numbers stored in
 * this structure are one less than the numbers shown in the editor and used
 * in the text script.
 *
 * @see IAGSEngine::GetCharacter
 */
struct AGSCharacter {
  /** Default view. */ int32 defview;
  /** Talking view. */ int32 talkview;
  /** Current view. */ int32 view;
  /** Current room. */ int32 room;
  /** Previous room. */ int32 prevroom;
  /** Horizontal position. */ int32 x;
  /** Vertical position. */ int32 y;
  /** Wait state. */ int32 wait;
  /** Character flags. */ int32 flags;
  /** Character being followed. */ short following;
  /** Follow information. */ short followinfo;
  /** Idle view; its loop is chosen randomly. */ int32 idleview;
  /** Seconds idle before playing an animation. */ short idledelay;
  /** Remaining idle time. */ short idleleft;
  /** Character transparency. */ short transparency;
  /** Character baseline. */ short baseline;
  /** Active inventory item. */ int32 activeinv;
  /** Talking text color. */ int32 talkcolor;
  /** Thinking view. */ int32 thinkview;
  /** Reserved fields. */ int32 reserved[2];
  /** Vertical walk speed. */ short walkspeed_y;
  /** Picture vertical offset. */ short pic_yoffs;
  /** Elevation. */ int32 z;
  /** Reserved fields. */ int32 reserved2[5];
  /** Current loop. */ short loop;
  /** Current frame. */ short frame;
  /**
   * Walking state.
   *
   * When greater than 0, this is the movement-path @p pathId to pass to the
   * GetMovementPath* family of functions.
   *
   * @see IAGSEngine::GetMovementPathWaypointCount
   * @see IAGSEngine::GetMovementPathLastWaypoint
   * @see IAGSEngine::GetMovementPathWaypointLocation
   * @see IAGSEngine::GetMovementPathWaypointSpeed
   */
  short walking;
  /** Animation state. */ short animating;
  /** Walking speed. */ short walkspeed;
  /** Animation speed. */ short animspeed;
  /** Inventory quantities by item. */ short inv[301];
  /** Current action horizontal coordinate. */ short actx;
  /** Current action vertical coordinate. */ short acty;
  /** Character name. */ char name[40];
  /** Script name. */ char scrname[20];
  /** Whether the character is enabled. */ char on;
};

// AGSObject.flags
/** Object flag. */
#define OBJF_NOINTERACT 1     // not clickable
/** Object flag. */
#define OBJF_NOWALKBEHINDS 2  // ignore walk-behinds

/**
 * The state of an object in the current room.
 *
 * Obtain this structure with IAGSEngine::GetObject. Object state structures
 * are reallocated whenever the room changes, so do not keep the returned
 * pointer after the function ends.
 *
 * @see IAGSEngine::GetObject
 */
struct AGSObject {
  /** Horizontal position. */ int32 x;
  /** Vertical position. */ int32 y;
  /** Current transparency setting. */ int32 transparent;
  /** Reserved fields. */ int32 reserved[4];
  /** Sprite slot number. */ short num;
  /** Baseline; non-positive uses the Y coordinate. */ short baseline;
  /** Animation view. */ short view;
  /** Animation loop. */ short loop;
  /** Animation frame. */ short frame;
  /** Wait state. */ short wait;
  /**
   * Movement state.
   *
   * When greater than 0, this is the movement-path @p pathId to pass to the
   * GetMovementPath* family of functions.
   *
   * @see IAGSEngine::GetMovementPathWaypointCount
   * @see IAGSEngine::GetMovementPathLastWaypoint
   * @see IAGSEngine::GetMovementPathWaypointLocation
   * @see IAGSEngine::GetMovementPathWaypointSpeed
   */
  short moving;
  /** Whether animation is active. */ char cycling;
  /** Overall animation speed. */ char overall_speed;
  /** Whether the object is enabled. */ char on;
  /** Object flags. */ char flags;
};

// AGSViewFrame.flags
/** View-frame flag. */
#define FRAF_MIRRORED  1  // flipped left to right

/**
 * A frame in a game view.
 *
 * Obtain this structure with IAGSEngine::GetViewFrame.
 *
 * @see IAGSEngine::GetViewFrame
 */
struct AGSViewFrame {
  /** Sprite slot number. */ int32 pic;
  /** Horizontal offset. */ short xoffs;
  /** Vertical offset. */ short yoffs;
  /** Frame speed. */ short speed;
  /** Frame flags. */ int32 flags;
  /** Sound played when this frame is reached. */ int32 sound;
  /** Reserved fields. */ int32 reserved_for_future[2];
};

// AGSMouseCursor.flags
/** Mouse-cursor flag. */
#define MCF_ANIMATEMOVE 1
/** Mouse-cursor flag. */
#define MCF_DISABLED    2
/** Mouse-cursor flag. */
#define MCF_STANDARD    4
/** Mouse-cursor flag. */
#define MCF_ONLYANIMOVERHOTSPOT 8

/**
 * Details of a mouse cursor mode.
 *
 * Obtain this read-only structure with IAGSEngine::GetMouseCursor. Do not
 * modify its members directly; use the corresponding script functions so the
 * game state is updated properly.
 *
 * @see IAGSEngine::GetMouseCursor
 */
struct AGSMouseCursor {
  /** Sprite slot number. */ int32 pic;
  /** Horizontal hotspot coordinate. */ short hotx;
  /** Vertical hotspot coordinate. */ short hoty;
  /** Animation view, or -1. */ short view;
  /** Cursor-mode name. */ char name[10];
  /** @c MCF_* flags. */ char flags;
};

// GetFontType font types
/** Invalid font type. */
#define FNT_INVALID 0
/** SCI font type. */
#define FNT_SCI     1
/** TrueType font type. */
#define FNT_TTF     2

// PlaySoundChannel sound types
/** Wave sound type. */
#define PSND_WAVE       1
/** Streaming MP3 sound type. */
#define PSND_MP3STREAM  2
/** Static MP3 sound type. */
#define PSND_MP3STATIC  3
/** Streaming Ogg sound type. */
#define PSND_OGGSTREAM  4
/** Static Ogg sound type. */
#define PSND_OGGSTATIC  5
/** MIDI sound type. */
#define PSND_MIDI       6
/** MOD sound type. */
#define PSND_MOD        7

/**
 * Callback implemented by the plugin for a script-managed object.
 *
 * The engine calls these methods to release, identify, and save the object.
 */
class IAGSScriptManagedObject {
public:
  /**
   * Called by the engine when the object's reference count reaches zero.
   *
   * @param address  Address of the object.
   * @param force    Whether the engine is forcing disposal.
   * @return 1 to remove the object from memory, or 0 to leave it.
   */
  virtual int Dispose(void *address, bool force) = 0;
  /**
   * Called by the engine to identify the object's type.
   *
   * @return The type name of the object.
   */
  virtual const char *GetType() = 0;
  /**
   * Called by the engine when it serializes the managed object.
   *
   * @param address  Address of the object.
   * @param buffer   Buffer receiving the serialized data.
   * @param bufsize  Size of @p buffer in bytes.
   * @return Number of bytes written to @p buffer.
   */
  virtual int Serialize(void *address, char *buffer, int bufsize) = 0;
protected:
  IAGSScriptManagedObject() {};
  ~IAGSScriptManagedObject() {};
};

/**
 * Callback implemented by the plugin to restore a script-managed object.
 *
 * The engine calls this method while loading serialized managed objects.
 */
class IAGSManagedObjectReader {
public:
  /**
   * Called by the engine to restore a serialized managed object.
   *
   * @param key             Object key supplied by the engine.
   * @param serializedData  Serialized object data.
   * @param dataSize        Size of @p serializedData in bytes.
   */
  virtual void Unserialize(int key, const char *serializedData, int dataSize) = 0;
protected:
  IAGSManagedObjectReader() {};
  ~IAGSManagedObjectReader() {};
};

/**
 * Callback implemented by the plugin to render a font.
 *
 * The engine calls these methods when it needs to load, measure, validate, or
 * render text using the plugin's font renderer.
 */
class IAGSFontRenderer {
public:
  /**
   * Called by the engine to load a font.
   *
   * @param fontNumber  Font slot to load.
   * @param fontSize    Requested font size.
   * @return @c true if the font was loaded; otherwise @c false.
   */
  virtual bool LoadFromDisk(int fontNumber, int fontSize) = 0;
  /**
   * Called by the engine to release a loaded font.
   *
   * @param fontNumber  Font slot to release.
   */
  virtual void FreeMemory(int fontNumber) = 0;
  /**
   * Called by the engine to determine whether a font supports extended
   * characters.
   *
   * @param fontNumber  Font slot to query.
   * @return @c true if the font supports extended characters; otherwise
   *         @c false.
   */
  virtual bool SupportsExtendedCharacters(int fontNumber) = 0;
  /**
   * Called by the engine to measure text width in a font.
   *
   * @param text        Text to measure.
   * @param fontNumber  Font slot to use.
   * @return Width of @p text.
   */
  virtual int GetTextWidth(const char *text, int fontNumber) = 0;
  /**
   * Called by the engine to measure text height in a font.
   *
   * @param text        Text to measure.
   * @param fontNumber  Font slot to use.
   * @return Height of @p text.
   */
  virtual int GetTextHeight(const char *text, int fontNumber) = 0;
  /**
   * Called by the engine to render text into a bitmap.
   *
   * @param text         Text to render.
   * @param fontNumber   Font slot to use.
   * @param destination  Destination bitmap.
   * @param x            Horizontal destination coordinate.
   * @param y            Vertical destination coordinate.
   * @param colour       Text color.
   */
  virtual void RenderText(const char *text, int fontNumber, BITMAP *destination, int x, int y, int colour) = 0;
  /**
   * Called by the engine to adjust a text baseline for a font.
   *
   * @param ycoord      Coordinate to adjust.
   * @param fontNumber  Font slot to use.
   */
  virtual void AdjustYCoordinateForFont(int *ycoord, int fontNumber) = 0;
  /**
   * Called by the engine to make text valid for a font.
   *
   * @param text        Text to validate and adjust.
   * @param fontNumber  Font slot to use.
   */
  virtual void EnsureTextValidForFont(char *text, int fontNumber) = 0;
protected:
  IAGSFontRenderer() = default;
  ~IAGSFontRenderer() = default;
};

/**
 * Extended font-renderer callback implemented by the plugin.
 *
 * The engine calls these methods after obtaining an IAGSFontRenderer2
 * implementation.
 *
 * @note This class declares no `public:` section, so its members are
 *       private in this header (the default for @c class). A plugin's
 *       implementation must still override them as public methods; the
 *       engine calls them through the base pointer regardless of the
 *       access specifier used here.
 */
class IAGSFontRenderer2 : public IAGSFontRenderer {
  /**
   * Called by the engine to obtain the renderer interface version.
   *
   * @return The renderer interface version.
   */
  virtual int GetVersion() = 0;
  /**
   * Called by the engine to obtain the renderer's name.
   *
   * @return Renderer name.
   */
  virtual const char *GetRendererName() = 0;
  /**
   * Called by the engine to obtain a font's name.
   *
   * @param fontNumber  Font slot to query.
   * @return Font name.
   */
  virtual const char *GetFontName(int fontNumber) = 0;
  /**
   * Called by the engine to obtain a font's height.
   *
   * @param fontNumber  Font slot to query.
   * @return Font height.
   */
  virtual int GetFontHeight(int fontNumber) = 0;
  /**
   * Called by the engine to obtain a font's line spacing.
   *
   * @param fontNumber  Font slot to query.
   * @return Font line spacing.
   */
  virtual int GetLineSpacing(int fontNumber) = 0;
protected:
  IAGSFontRenderer2() = default;
  ~IAGSFontRenderer2() = default;
};

/**
 * Transformation matrices for the current render stage.
 *
 * Obtained as part of AGSRenderStageDesc through
 * IAGSEngine::GetRenderStageDesc. The matrix format is the internal format of
 * the renderer in use; the software renderer fills these arrays with zeroes.
 *
 * @see IAGSEngine::GetRenderStageDesc
 */
struct AGSRenderMatrixes {
  /** World transformation matrix, with 16 floating-point values. */
  float WorldMatrix[16];
  /** View transformation matrix, with 16 floating-point values. */
  float ViewMatrix[16];
  /** Projection transformation matrix, with 16 floating-point values. */
  float ProjMatrix[16];
};

/**
 * Description of the current render stage.
 *
 * Before passing this structure to IAGSEngine::GetRenderStageDesc, a plugin
 * must fill @c Version with the interface version it requires.
 *
 * @see IAGSEngine::GetRenderStageDesc
 */
struct AGSRenderStageDesc {
  /** Interface version requested by the plugin; must be filled before use. */
  int Version;
  /** World, view, and projection matrices for 3D rendering. */
  AGSRenderMatrixes Matrixes;
};

/**
 * Descriptive information about the game.
 *
 * Before passing this structure to IAGSEngine::GetGameInfo, a plugin must fill
 * @c Version with the interface version it requires.
 *
 * @see IAGSEngine::GetGameInfo
 */
struct AGSGameInfo {
  /** Interface version requested by the plugin; must be filled before use. */
  int Version;
  /** Human-readable game title, corresponding to the @c Game.Name property. */
  char GameName[50];
  /** Unique string identifying the game. */
  char Guid[40];
  /** Deprecated game identifier, used if @c Guid is empty. */ int UniqueId;
};

/**
 * @name File stream open modes
 * Passed to IAGSEngine::OpenFileStream.
 * @{
 */
/** Opens an existing file and fails if it does not exist. */
#define AGSSTREAM_FILE_OPEN         1
/** Opens an existing file or creates it if it does not exist. */
#define AGSSTREAM_FILE_CREATE       2
/** Creates a new file, completely overwriting any existing file. */
#define AGSSTREAM_FILE_CREATEALWAYS 3
/** @} */

/**
 * @name File stream access modes
 * Returned by IAGSStream::GetMode.
 * @{
 */
/** Supports read operations only. */
#define AGSSTREAM_MODE_READ  0x01
/** Supports write operations only. */
#define AGSSTREAM_MODE_WRITE 0x02
/** Supports both read and write operations. */
#define AGSSTREAM_MODE_READWRITE (AGSSTREAM_MODE_READ | AGSSTREAM_MODE_WRITE)
/** Supports seeking. */
#define AGSSTREAM_MODE_SEEK  0x04
/** @} */

// Stream seek origins
// Seek from the beginning of a stream (towards positive offset)
/** Stream seek origin. */
#define AGSSTREAM_SEEK_SET 0
// Seek from the current position (towards positive or negative offset)
/** Stream seek origin. */
#define AGSSTREAM_SEEK_CUR 1
// Seek from the end of a stream (towards negative offset)
/** Stream seek origin. */
#define AGSSTREAM_SEEK_END 2

/**
 * Stream implementation supplied by the engine to a plugin.
 *
 * The engine implements this interface and hands streams to the plugin. The
 * caller owns the returned stream from IAGSEngine::OpenFileStream and must
 * release it with Dispose(). A stream from IAGSEngine::GetFileStreamByHandle
 * is owned by the engine and must not be disposed.
 *
 * @see IAGSEngine::OpenFileStream
 * @see IAGSEngine::GetFileStreamByHandle
 */
class IAGSStream {
public:
  /**
   * Gets the supported stream operations.
   *
   * @return A combination of @c AGSSTREAM_MODE_* flags, or 0 for an invalid
   *         or non-functional stream.
   */
  virtual int    GetMode() const = 0;
  /**
   * Gets an optional diagnostic description of the stream source.
   *
   * @return A file path, resource name, or other source description.
   */
  virtual const char *GetPath() const = 0;
  /**
   * Tests whether the current position is at the end of the stream.
   *
   * Unlike the C @c feof function, this does not wait for a read attempt past
   * the end and returns @c true when position equals length.
   *
   * @return @c true at the end of the stream; otherwise @c false.
   */
  virtual bool   EOS() const = 0;
  /**
   * Gets and clears the previous I/O error state.
   *
   * @return @c true if a prior I/O operation failed; otherwise @c false.
   */
  virtual bool   GetError() const = 0;
  /** @return Total stream length in bytes. */
  virtual int64_t GetLength() const = 0;
  /** @return Current stream position. */
  virtual int64_t GetPosition() const = 0;

  /**
   * Reads bytes into a buffer.
   *
   * @param buffer  Buffer receiving the data.
   * @param len     Number of bytes to read.
   * @return Number of bytes read.
   */
  virtual size_t Read(void *buffer, size_t len) = 0;
  /**
   * Reads one byte, following the C @c fgetc convention.
   *
   * @return The unsigned byte packed in an @c int32_t on success, or -1 at
   *         end of stream or on error.
   */
  virtual int32_t ReadByte() = 0;
  /**
   * Writes bytes from a buffer.
   *
   * @param buffer  Buffer containing the data.
   * @param len     Number of bytes to write.
   * @return Number of bytes written.
   */
  virtual size_t Write(const void *buffer, size_t len) = 0;
  /**
   * Writes one byte, following the C @c fputc convention.
   *
   * @param b  Byte to write.
   * @return The written unsigned byte packed in an @c int32_t on success, or
   *         -1 on failure.
   */
  virtual int32_t WriteByte(uint8_t b) = 0;
  /**
   * Seeks to an offset from an @c AGSSTREAM_SEEK_* origin.
   *
   * @param offset  Offset from @p origin.
   * @param origin  @c AGSSTREAM_SEEK_SET, @c AGSSTREAM_SEEK_CUR, or
   *                @c AGSSTREAM_SEEK_END.
   * @return New stream position, or -1 on error.
   */
  virtual int64_t Seek(int64_t offset, int origin) = 0;
  /**
   * Flushes buffered data to the underlying device.
   *
   * @return @c true on success; otherwise @c false.
   */
  virtual bool   Flush() = 0;
  /**
   * Flushes and closes the stream.
   *
   * Usually callers use Dispose() instead, which also deletes the stream
   * object.
   */
  virtual void   Close() = 0;

  /**
   * Closes and deallocates the stream object.
   *
   * The pointer is invalid after this call. Dispose only a stream owned by the
   * caller; see IAGSEngine::OpenFileStream and
   * IAGSEngine::GetFileStreamByHandle.
   *
   * @see IAGSEngine::OpenFileStream
   * @see IAGSEngine::GetFileStreamByHandle
   */
  virtual void   Dispose() = 0;

protected:
  IAGSStream() = default;
  ~IAGSStream() = default;
};


// Plugin events
//
// Below are interface 3 and later
/** Plugin event. */
#define AGSE_KEYPRESS        0x01
/** Plugin event. */
#define AGSE_MOUSECLICK      0x02
/** Plugin event. */
#define AGSE_POSTSCREENDRAW  0x04
// Below are interface 4 and later
/** Plugin event. */
#define AGSE_PRESCREENDRAW   0x08
// Below are interface 5 and later
/** Plugin event. */
#define AGSE_SAVEGAME        0x10
/** Plugin event. */
#define AGSE_RESTOREGAME     0x20
// Below are interface 6 and later
/** Plugin event. */
#define AGSE_PREGUIDRAW      0x40
/** Plugin event. */
#define AGSE_LEAVEROOM       0x80
/** Plugin event. */
#define AGSE_ENTERROOM       0x100
/** Plugin event. */
#define AGSE_TRANSITIONIN    0x200
/** Plugin event. */
#define AGSE_TRANSITIONOUT   0x400
// Below are interface 12 and later
/** Plugin event. */
#define AGSE_FINALSCREENDRAW 0x800
/** Plugin event. */
#define AGSE_TRANSLATETEXT   0x1000
// Below are interface 13 and later
/** Plugin event. */
#define AGSE_SCRIPTDEBUG     0x2000
// AGSE_AUDIODECODE is no longer supported
/** Plugin event. */
#define AGSE_AUDIODECODE     0x4000
// Below are interface 18 and later
/** Plugin event. */
#define AGSE_SPRITELOAD      0x8000
// Below are interface 21 and later
/** Plugin event. */
#define AGSE_PRERENDER       0x10000
// Below are interface 24 and later
/** Plugin event. */
#define AGSE_PRESAVEGAME     0x20000
/** Plugin event. */
#define AGSE_POSTRESTOREGAME 0x40000
// Below are interface 26 and later
/** Plugin event. */
#define AGSE_POSTROOMDRAW    0x80000
/** Plugin event. */
#define AGSE_TOOHIGH         0x100000

/**
 * @name Log message levels
 * Passed to IAGSEngine::Log.
 * @{
 */
/** Disables logging. */
#define AGSLOG_LEVEL_NONE   0
/** Alert-level message. */
#define AGSLOG_LEVEL_ALERT  1
/** Fatal-error message. */
#define AGSLOG_LEVEL_FATAL  2
/** Error-level message. */
#define AGSLOG_LEVEL_ERROR  3
/** Warning-level message. */
#define AGSLOG_LEVEL_WARN   4
/** Informational message. */
#define AGSLOG_LEVEL_INFO   5
/** Debug-level message. */
#define AGSLOG_LEVEL_DEBUG  6
/** @} */


/**
 * Engine interface supplied to a plugin at run time.
 *
 * A plugin receives this interface during AGS_EngineStartup and calls its
 * methods to interact with the running game.
 */
class IAGSEngine {
public:
  /**
   * The interface version provided by the running engine.
   *
   * Check this before calling any method introduced after the minimum version
   * your plugin requires. Each method's documentation records the version that
   * introduced it under @c \@since.
   */
  int32 version;
  /** Used internally by the engine. Do not modify. */
  int32 pluginId;

public:
  /**
   * Aborts the game with an error message, in the same way as the script
   * command AbortGame.
   *
   * @param reason  Text shown to the player in the error dialog.
   * @since Interface version 1
   */
  AGSIFUNC(void) AbortGame (const char *reason);
  /**
   * Gets the engine version.
   *
   * @return The full engine build number, such as "2.51.400".
   * @since Interface version 1
   */
  AGSIFUNC(const char*) GetEngineVersion ();
  /**
   * Registers a function that game script can then call by name.
   *
   * Call this from AGS_EngineStartup. The script header declaring the function
   * must be registered separately at design time, via the editor interface.
   *
   * @param name     Name the script will use to call the function.
   * @param address  Pointer to your function.
   * @since Interface version 1
   */
  AGSIFUNC(void) RegisterScriptFunction (const char *name, void *address);
#ifdef WINDOWS_VERSION
  /**
   * Gets the main game window handle.
   *
   * @return The main game window handle.
   * @note Windows only.
   * @since Interface version 1
   */
  AGSIFUNC(HWND) GetWindowHandle();
  /**
   * Gets the main DirectDraw interface used by the game.
   *
   * This is supported only when the player uses the DX5 graphics driver.
   *
   * @return The main IDirectDraw2 interface.
   * @since Interface version 1
   */
  AGSIFUNC(LPDIRECTDRAW2) GetDirectDraw2 ();
  /**
   * Gets the DirectDraw surface associated with a bitmap.
   *
   * This is supported only when the player uses the DX5 graphics driver. The
   * bitmap must be an Allegro BITMAP, not a Win32 GDI BITMAP.
   *
   * The bitmap argument is the Allegro bitmap whose surface is retrieved.
   * @return The associated IDirectDrawSurface2 interface.
   * @since Interface version 1
   */
  AGSIFUNC(LPDIRECTDRAWSURFACE2) GetBitmapSurface (BITMAP *);
#endif
  /**
   * Gets the main screen bitmap.
   *
   * This is supported only when the player uses the DX5 graphics driver.
   *
   * @return The main screen Allegro BITMAP.
   * @since Interface version 1
   */
  AGSIFUNC(BITMAP *) GetScreen ();

  // *** BELOW ARE INTERFACE VERSION 2 AND ABOVE ONLY
  /**
   * Requests callbacks when a specified event occurs.
   *
   * The engine calls the plugin's AGS_EngineOnEvent function for future
   * occurrences of the requested event.
   *
   * @param event  Event to request.
   * @since Interface version 2
   */
  AGSIFUNC(void) RequestEventHook (int32 event);
  /**
   * Gets plugin data saved by the editor.
   *
   * @param buffer   Buffer that receives the saved data.
   * @param bufsize  Size of @p buffer in bytes.
   * @return The number of bytes used.
   * @since Interface version 2
   */
  AGSIFUNC(int)  GetSavedData (char *buffer, int32 bufsize);

  // *** BELOW ARE INTERFACE VERSION 3 AND ABOVE ONLY
  /**
   * Gets the virtual screen bitmap.
   *
   * With the Software renderer this is the game's virtual screen. With a
   * texture-based renderer it is an initially transparent stage screen that
   * is rendered after the plugin finishes drawing.
   *
   * @return The virtual screen Allegro BITMAP.
   * @since Interface version 3
   */
  AGSIFUNC(BITMAP *) GetVirtualScreen ();
  /**
   * Draws text on the current virtual screen.
   *
   * The coordinates use actual screen coordinates, unlike text script
   * commands, which use 320-scale coordinates.
   *
   * @param x      Horizontal screen coordinate.
   * @param y      Vertical screen coordinate.
   * @param font   Game font number.
   * @param color  Text color.
   * @param text   Text to draw.
   * @since Interface version 3
   */
  AGSIFUNC(void) DrawText (int32 x, int32 y, int32 font, int32 color, char *text);
  /**
   * Gets the game's native screen dimensions.
   *
   * Any output parameter may be NULL. Color depth is reported in bits.
   *
   * @param width     Receives the native width, or NULL.
   * @param height    Receives the native height, or NULL.
   * @param coldepth  Receives the color depth, or NULL.
   * @since Interface version 3
   */
  AGSIFUNC(void) GetScreenDimensions (int32 *width, int32 *height, int32 *coldepth);
  /**
   * Gets a bitmap's raw pixel surface.
   *
   * This locks the bitmap; call ReleaseBitmapSurface when finished. It cannot
   * be used with the real screen bitmap.
   *
   * The bitmap argument is the bitmap whose surface is locked.
   * @return Pointer to the bitmap's linear memory surface.
   * @since Interface version 3
   */
  AGSIFUNC(unsigned char**) GetRawBitmapSurface (BITMAP *);
  /**
   * Releases a bitmap locked with GetRawBitmapSurface.
   *
   * The bitmap argument is the bitmap whose locked surface is released.
   * @since Interface version 3
   */
  AGSIFUNC(void) ReleaseBitmapSurface (BITMAP *);
  /**
   * Gets the current mouse position.
   *
   * The coordinates are real screen-size coordinates.
   *
   * @param x  Receives the horizontal coordinate.
   * @param y  Receives the vertical coordinate.
   * @since Interface version 3
   */
  AGSIFUNC(void) GetMousePosition (int32 *x, int32 *y);

  // *** BELOW ARE INTERFACE VERSION 4 AND ABOVE ONLY
  /**
   * Gets the current room number.
   *
   * @return The current room number.
   * @since Interface version 4
   */
  AGSIFUNC(int)  GetCurrentRoom ();
  /**
   * Gets the number of background frames in the current room.
   *
   * @return The number of background frames.
   * @since Interface version 4
   */
  AGSIFUNC(int)  GetNumBackgrounds ();
  /**
   * Gets the currently displayed background frame.
   *
   * @return The frame number; 0 is the default frame.
   * @since Interface version 4
   */
  AGSIFUNC(int)  GetCurrentBackground ();
  /**
   * Gets a background frame bitmap.
   *
   * The frame argument is the background frame number; 0 is the default.
   * @return The background frame Allegro BITMAP.
   * @since Interface version 4
   */
  AGSIFUNC(BITMAP *) GetBackgroundScene (int32);
  /**
   * Gets a bitmap's dimensions and color depth.
   *
   * Any output parameter may be NULL.
   *
   * @param bmp       Bitmap to inspect.
   * @param width     Receives the width in pixels, or NULL.
   * @param height    Receives the height in pixels, or NULL.
   * @param coldepth  Receives the color depth, or NULL.
   * @since Interface version 4
   */
  AGSIFUNC(void) GetBitmapDimensions (BITMAP *bmp, int32 *width, int32 *height, int32 *coldepth);

  // *** BELOW ARE INTERFACE VERSION 5 AND ABOVE ONLY
  /**
   * Writes bytes to an AGS-provided file handle.
   *
   * @param out_buf  Buffer to write.
   * @param len      Number of bytes to write.
   * @param fhandle  File handle supplied by AGS.
   * @return The number of bytes actually written.
   * @since Interface version 5
   */
  AGSIFUNC(int)  FWrite (void *out_buf, int32 len, int32 fhandle);
  /**
   * Reads bytes from an AGS-provided file handle.
   *
   * @param in_buf   Buffer to receive data.
   * @param len      Number of bytes to read.
   * @param fhandle  File handle supplied by AGS.
   * @return The number of bytes actually read.
   * @since Interface version 5
   */
  AGSIFUNC(int)  FRead (void *in_buf, int32 len, int32 fhandle);
  /**
   * Draws wrapped text on the current virtual screen.
   *
   * @param x      Horizontal screen coordinate.
   * @param y      Vertical screen coordinate.
   * @param width  Maximum text width in pixels.
   * @param font   Game font number.
   * @param color  Text color.
   * @param text   Text to draw.
   * @since Interface version 5
   */
  AGSIFUNC(void) DrawTextWrapped (int32 x, int32 y, int32 width, int32 font, int32 color, const char *text);
  /**
   * Sets the current virtual screen for drawing.
   *
   * Restore the previous virtual screen before returning from the plugin
   * function.
   *
   * The bitmap argument is used as the virtual screen.
   * @since Interface version 5
   */
  AGSIFUNC(void) SetVirtualScreen (BITMAP *);
  /**
   * Looks up a word in the parser dictionary.
   *
   * Synonyms have the same word number.
   *
   * @param word  Word to look up.
   * @return The word number, or -1 if the word is not in the dictionary.
   * @since Interface version 5
   */
  AGSIFUNC(int)  LookupParserWord (const char *word);
  /**
   * Draws a bitmap on the current virtual screen.
   *
   * @param x       Horizontal screen coordinate.
   * @param y       Vertical screen coordinate.
   * The bitmap argument is the bitmap to draw.
   * @param masked  Whether transparent pixels are skipped.
   * @since Interface version 5
   */
  AGSIFUNC(void) BlitBitmap (int32 x, int32 y, BITMAP *, int32 masked);
  /**
   * Updates mouse input, plugin events, and music decoding.
   *
   * Call this from plugin functions that run for more than half a second, or
   * when they need mouse and keypress events.
   *
   * @since Interface version 5
   */
  AGSIFUNC(void) PollSystem ();

  // *** BELOW ARE INTERFACE VERSION 6 AND ABOVE ONLY
  /**
   * Gets the number of characters in the game.
   *
   * Valid character IDs range from 0 to the return value minus 1.
   *
   * @return The number of characters in the current game.
   * @since Interface version 6
   */
  AGSIFUNC(int)  GetNumCharacters ();
  /**
   * Gets a reference to the specified character structure.
   *
   * This structure is documented in the header file, but is mostly identical
   * to the @c character[] text script variable. Beware when modifying
   * character state structures; for example, altering X and Y while the
   * character is moving. View numbers stored here are one less than the
   * numbers shown in the editor and used in the text script.
   *
   * The character-ID argument identifies the character.
   * @return The character state structure.
   * @since Interface version 6
   */
  AGSIFUNC(AGSCharacter*) GetCharacter (int32);
  /**
   * Gets a reference to the game structure.
   *
   * This is similar to the @c game text script variables. The
   * @c AGSGameOptions.fast_forward member is set to 1 while a cut-scene is
   * being skipped. Operations that update game state must still be performed,
   * or skipping the cut-scene will desync the game.
   *
   * @return The game state structure.
   * @since Interface version 6
   */
  AGSIFUNC(AGSGameOptions*) GetGameOptions ();
  /**
   * Gets a reference to the current palette.
   *
   * This is an array of 256 @c AGSColor structures. In a game with animating
   * backgrounds, the palette can change quite often, so only rely on it
   * remaining consistent for the length of the function.
   *
   * @return The current palette.
   * @since Interface version 6
   */
  AGSIFUNC(AGSColor*) GetPalette();
  /**
   * Updates the palette.
   *
   * Sets the current screen palette to @p palette, which must be an array of
   * 256 @c AGSColor structures. Elements from @p start to @p finish are
   * updated. Pass 0 for @p start and 255 for @p finish to update the entire
   * palette.
   *
   * @param start    First palette element to update.
   * @param finish   Last palette element to update.
   * The palette argument is an array of 256 colors to use.
   * @since Interface version 6
   */
  AGSIFUNC(void) SetPalette (int32 start, int32 finish, AGSColor*);

  // *** BELOW ARE INTERFACE VERSION 7 AND ABOVE ONLY
  /**
   * Gets the current player character.
   *
   * @return The number of the current player character.
   * @since Interface version 7
   */
  AGSIFUNC(int)  GetPlayerCharacter ();
  /**
   * Adjusts coordinates to the main viewport.
   *
   * Converts room coordinates into current viewport coordinates. Input
   * coordinates use the 320x200-style scheme that the text script uses and
   * that object and character coordinates are stored as. The resulting
   * coordinates are real resolution-sized coordinates within the current
   * viewable area of the screen. Check them against the screen size to
   * determine whether they are currently visible.
   *
   * @param x  Horizontal coordinate to convert.
   * @param y  Vertical coordinate to convert.
   * @since Interface version 7
   */
  AGSIFUNC(void) RoomToViewport (int32 *x, int32 *y);
  /**
   * Adjusts coordinates from the main viewport, ignoring viewport bounds.
   *
   * Converts viewport coordinates into room coordinates. Input coordinates are
   * screen resolution scale coordinates within the current viewable area of
   * the room. The resulting coordinates are those stored in character and
   * object structures and used in the text script.
   *
   * @param x  Horizontal coordinate to convert.
   * @param y  Vertical coordinate to convert.
   * @since Interface version 7
   */
  AGSIFUNC(void) ViewportToRoom (int32 *x, int32 *y);
  /**
   * Gets the number of objects in the current room.
   *
   * @return The number of objects in the current room.
   * @since Interface version 7
   */
  AGSIFUNC(int)  GetNumObjects ();
  /**
   * Gets a reference to the specified object structure.
   *
   * Object state pointers are not constant throughout the game. The
   * structures are reallocated whenever the room changes, so do not keep a
   * pointer returned by this function after the function ends.
   *
   * The object-number argument identifies the object.
   * @return The specified object's state structure.
   * @since Interface version 7
   */
  AGSIFUNC(AGSObject*) GetObject (int32);
  /**
   * Gets a sprite graphic.
   *
   * This automatically loads the sprite from disk into the sprite cache when
   * it is not already present. The sprite cache may destroy any sprite from
   * memory at any time; never keep a pointer returned by this function. Call
   * it again for each operation on the bitmap.
   *
   * The sprite-slot argument identifies the sprite.
   * @return The bitmap for the specified sprite slot.
   * @since Interface version 7
   */
  AGSIFUNC(BITMAP *) GetSpriteGraphic (int32);
  /**
   * Creates a new blank bitmap.
   *
   * The bitmap is @p width by @p height pixels at @p coldep-bit color and is
   * initially filled with the transparent color for that color depth. It
   * remains in memory until explicitly freed with FreeBitmap. Always check
   * the return value to avoid crashes. To draw on the bitmap, use
   * GetRawBitmapSurface, or SetVirtualScreen followed by a drawing command
   * such as DrawText or BlitBitmap. Use this command sparingly, because each
   * bitmap uses the engine's available memory.
   *
   * @param width   Bitmap width in pixels.
   * @param height  Bitmap height in pixels.
   * @param coldep  Color depth in bits.
   * @return The new bitmap, or NULL if it could not be created.
   * @since Interface version 7
   */
  AGSIFUNC(BITMAP *) CreateBlankBitmap (int32 width, int32 height, int32 coldep);
  /**
   * Frees a created bitmap.
   *
   * Only use this with bitmaps created by CreateBlankBitmap. Using it with an
   * AGS system bitmap, including one obtained with GetVirtualScreen, GetScreen
   * or GetSpriteGraphic, will crash the engine.
   *
   * The bitmap argument is the bitmap to free.
   * @since Interface version 7
   */
  AGSIFUNC(void) FreeBitmap (BITMAP *);

  // *** BELOW ARE INTERFACE VERSION 8 AND ABOVE ONLY
  /**
   * Gets one of the room area masks.
   *
   * @p which is one of @c MASK_WALKABLE, @c MASK_WALKBEHIND, @c MASK_HOTSPOT,
   * or @c MASK_REGIONS. The walk-behind mask is the same size as the room.
   * The walkable area, hotspot, and region masks may be smaller, depending on
   * the Mask Resolution set in the editor. @c MASK_REGIONS requires interface
   * version 11. All masks have 8-bit color depth.
   * Area masks are reallocated when the player changes rooms, so do not keep a
   * returned pointer longer than needed.
   *
   * The mask argument selects the requested mask.
   * @return The requested mask for the current room.
   * @since Interface version 8
   */
  AGSIFUNC(BITMAP *) GetRoomMask(int32);

  // *** BELOW ARE INTERFACE VERSION 9 AND ABOVE ONLY
  /**
   * Gets a particular view frame.
   *
   * @p view is the number from the editor, rather than the view number minus
   * 1. With interface version 9, invalid @p view or @p loop values, or an
   * invalid @p frame, cause AGS to exit with an error message.
   *
   * @param view   View number from the editor.
   * @param loop   Loop number.
   * @param frame  Frame number.
   * @return The specified view frame structure.
   * @since Interface version 9
   */
  AGSIFUNC(AGSViewFrame *) GetViewFrame(int32 view, int32 loop, int32 frame);
  /**
   * Gets the walk-behind baseline of a specific area.
   *
   * The baseline is always in character-resolution, so its maximum value is
   * 200 in every resolution.
   *
   * @param walkbehind  Walk-behind area number.
   * @return The area's baseline.
   * @since Interface version 9
   */
  AGSIFUNC(int)    GetWalkbehindBaseline(int32 walkbehind);
  /**
   * Gets the address of a script function.
   *
   * @p funcName identifies an exported text script function. It is
   * @c FunctionName^N for a global function and @c StructName::FunctionName^N
   * for a member function; @c StructName::get_AttributeName or
   * @c StructName::set_AttributeName for an attribute; and
   * @c StructName::geti_AttributeName or @c StructName::seti_AttributeName
   * for an indexed attribute. The @c ^N suffix specifies the expected number
   * of arguments. It may be omitted only when the script function has no
   * variants. Attribute getters and setters do not need @c ^N because their
   * argument lists are exact. The result must be cast to the correct function
   * type before calling it; failure to do so will corrupt the stack and likely
   * crash the engine. Only use this to obtain addresses of documented AGS
   * script functions.
   *
   * @param funcName  Name of the exported script function.
   * @return The function address, or NULL if the function does not exist.
   * @since Interface version 9
   */
  AGSIFUNC(void *) GetScriptFunctionAddress(const char * funcName);
  /**
   * Gets the transparent color of a bitmap.
   *
   * This is 0 for 256-color bitmaps and RGB (255, 0, 255) for hi-color
   * bitmaps.
   *
   * The bitmap argument is the bitmap to inspect.
   * @return The transparent pixel color.
   * @since Interface version 9
   */
  AGSIFUNC(int)    GetBitmapTransparentColor(BITMAP *);
  /**
   * Gets the character scaling level at a particular point.
   *
   * The walkable area scaling level ranges from 5 to 200 and specifies the
   * scaling percentage set in the editor. Coordinates are room coordinates.
   *
   * @param x  Horizontal room coordinate.
   * @param y  Vertical room coordinate.
   * @return The walkable area scaling level.
   * @since Interface version 9
   */
  AGSIFUNC(int)    GetAreaScaling (int32 x, int32 y);
  /**
   * Is equivalent to the text script function.
   *
   * @return Non-zero if the game is paused, or 0 if it is not paused.
   * @since Interface version 9
   */
  AGSIFUNC(int)    IsGamePaused();

  // *** BELOW ARE INTERFACE VERSION 10 AND ABOVE ONLY
  /**
   * Gets the raw pixel value for an AGS Editor color.
   *
   * Converts the color to the appropriate raw pixel color for the current
   * graphics mode.
   *
   * In 8-bit color the value is returned unchanged. In 16-bit color it is
   * returned unchanged unless it is less than 32, in which case it is
   * converted to the appropriate locked color from the editor. In 15-bit
   * color it is converted to the appropriate 15-bit RGB pixel value.
   *
   * @param color  Color specified in the AGS Editor.
   * @return The appropriate raw pixel color.
   * @since Interface version 10
   */
  AGSIFUNC(int)    GetRawPixelColor (int32 color);

  // *** BELOW ARE INTERFACE VERSION 11 AND ABOVE ONLY
  /**
   * Gets a sprite's width.
   *
   * This is faster than retrieving the sprite from the sprite cache and using
   * GetBitmapDimensions.
   *
   * The sprite-number argument identifies the sprite.
   * @return The sprite width in pixels.
   * @since Interface version 11
   */
  AGSIFUNC(int)    GetSpriteWidth (int32);
  /**
   * Gets a sprite's height.
   *
   * This is faster than retrieving the sprite from the sprite cache and using
   * GetBitmapDimensions.
   *
   * The sprite-number argument identifies the sprite.
   * @return The sprite height in pixels.
   * @since Interface version 11
   */
  AGSIFUNC(int)    GetSpriteHeight (int32);
  /**
   * Gets a string's dimensions in a game font.
   *
   * Either output pointer may be NULL when only the other dimension is needed.
   *
   * @param font    Game font number.
   * @param text    Text to measure.
   * @param width   Receives the width in pixels, or NULL.
   * @param height  Receives the height in pixels, or NULL.
   * @since Interface version 11
   */
  AGSIFUNC(void)   GetTextExtent (int32 font, const char *text, int32 *width, int32 *height);
  /**
   * Prints a message to the engine log.
   *
   * This formerly printed to the in-game debug console, which was removed in
   * AGS 3.6.1. Prefer Log.
   *
   * @param text  Message to print.
   * @since Interface version 11
   */
  AGSIFUNC(void)   PrintDebugConsole (const char *text);
  /**
   * Plays a sound on a sound channel.
   *
   * @p volume ranges from 0 to 255 and @p loop is 0 or 1. @p soundType
   * determines which sound driver loads the file. Normally, use
   * AudioClip.Play through GetScriptFunctionAddress instead.
   *
   * Sound types are PSND_WAVE for uncompressed WAV, PSND_MP3STREAM or
   * PSND_MP3STATIC for MP3, PSND_OGGSTREAM or PSND_OGGSTATIC for OGG,
   * PSND_MIDI for MIDI, and PSND_MOD for MOD/XM/S3M. Only one MIDI file and
   * one MOD/XM file can play at a time; generally, avoid these sound types
   * unless their restrictions are understood.
   *
   * @param channel    Sound channel number.
   * @param soundType  Sound driver used to load the file.
   * @param volume     Volume from 0 to 255.
   * @param loop       0 or 1.
   * @param filename   Sound filename.
   * @since Interface version 11
   */
  AGSIFUNC(void)   PlaySoundChannel (int32 channel, int32 soundType, int32 volume, int32 loop, const char *filename);
  /**
   * Checks whether a sound channel is in use.
   *
   * Equivalent to the text script function of the same name.
   *
   * @param channel  Sound channel number.
   * @return 1 if the channel is in use; otherwise, 0.
   * @since Interface version 11
   */
  AGSIFUNC(int)    IsChannelPlaying (int32 channel);

  // *** BELOW ARE INTERFACE VERSION 12 AND ABOVE ONLY
  /**
   * Marks a virtual-screen region as dirty.
   *
   * The rectangle uses screen coordinates. Call this after modifying the
   * virtual screen through its raw bitmap surface so AGS repaints that area;
   * drawing functions such as DrawText and BlitBitmap do this automatically.
   *
   * @param left    Left screen coordinate.
   * @param top     Top screen coordinate.
   * @param right   Right screen coordinate.
   * @param bottom  Bottom screen coordinate.
   * @since Interface version 12
   */
  AGSIFUNC(void)   MarkRegionDirty(int32 left, int32 top, int32 right, int32 bottom);
  /**
   * Gets the details of a cursor mode.
   *
   * The returned structure is read-only. Use script functions such as
   * Mouse.ChangeModeGraphic to modify cursor details.
   *
   * @param cursor  Cursor mode.
   * @return The mouse cursor structure.
   * @since Interface version 12
   */
  AGSIFUNC(AGSMouseCursor *) GetMouseCursor(int32 cursor);
  /**
   * Gets the components of a raw pixel color.
   *
   * Any output pointer may be NULL. Alpha has meaning only for 32-bit pixels;
   * it is 0 for all other color depths.
   *
   * @param coldepth  Color depth.
   * @param color     Raw color from a bitmap surface or GetRawPixelColor.
   * @param red       Receives the red component from 0 to 255, or NULL.
   * @param green     Receives the green component from 0 to 255, or NULL.
   * @param blue      Receives the blue component from 0 to 255, or NULL.
   * @param alpha     Receives the alpha component from 0 to 255, or NULL.
   * @since Interface version 12
   */
  AGSIFUNC(void)   GetRawColorComponents(int32 coldepth, int32 color, int32 *red, int32 *green, int32 *blue, int32 *alpha);
  /**
   * Creates a raw pixel color from components.
   *
   * @p alpha is meaningful only for 32-bit alpha-channel bitmaps; use 0 for
   * bitmaps without an alpha channel.
   *
   * @param coldepth  Color depth.
   * @param red       Red value from 0 to 255.
   * @param green     Green value from 0 to 255.
   * @param blue      Blue value from 0 to 255.
   * @param alpha     Alpha value.
   * @return The raw pixel value for the requested color.
   * @since Interface version 12
   */
  AGSIFUNC(int)    MakeRawColorPixel(int32 coldepth, int32 red, int32 green, int32 blue, int32 alpha);
  /**
   * Gets a font's type.
   *
   * @param fontNum  Font number.
   * @return FNT_INVALID for an invalid font, FNT_SCI for an SCI font, or
   *         FNT_TTF for a TTF font.
   * @since Interface version 12
   */
  AGSIFUNC(int)    GetFontType(int32 fontNum);
  /**
   * Creates a dynamic sprite.
   *
   * Dynamic sprites are not controlled by the AGS Sprite Cache and remain in
   * memory until DeleteDynamicSprite is called. Their bitmap surfaces may be
   * written directly.
   *
   * @param coldepth  Color depth.
   * @param width     Sprite width.
   * @param height    Sprite height.
   * @return The sprite slot on success, or 0 if the sprite could not be
   *         created.
   * @since Interface version 12
   */
  AGSIFUNC(int)    CreateDynamicSprite(int32 coldepth, int32 width, int32 height);
  /**
   * Deletes a dynamic sprite.
   *
   * The sprite can no longer be used in the game.
   *
   * @param slot  Previously created dynamic sprite slot.
   * @since Interface version 12
   */
  AGSIFUNC(void)   DeleteDynamicSprite(int32 slot);
  /**
   * Checks whether a sprite has an alpha channel.
   *
   * Only 32-bit bitmaps can have an alpha channel; this returns 0 for all
   * other color depths. Alpha cannot be ignored when modifying a sprite with
   * an alpha channel because the default value of 0 is fully invisible.
   *
   * @param slot  Sprite slot.
   * @return 1 if the sprite has an alpha channel; otherwise, 0.
   * @since Interface version 12
   */
  AGSIFUNC(int)    IsSpriteAlphaBlended(int32 slot);

  // *** BELOW ARE INTERFACE VERSION 13 AND ABOVE ONLY
  /**
   * Stops event callbacks for this plugin.
   *
   * This can improve performance when notification of the event is no longer
   * required.
   *
   * @param event  Event to stop receiving.
   * @see RequestEventHook
   * @since Interface version 13
   */
  AGSIFUNC(void)   UnrequestEventHook(int32 event);
  /**
   * Draws a translucent bitmap on the current virtual screen.
   *
   * Pixels in the bitmap's transparent color are skipped completely.
   *
   * @param x      Horizontal position.
   * @param y      Vertical position.
   * The sprite argument is the bitmap to draw.
   * @param trans  Translucency from 0 (fully transparent) to 255 (fully
   *               opaque).
   * @since Interface version 13
   */
  AGSIFUNC(void)   BlitSpriteTranslucent(int32 x, int32 y, BITMAP *, int32 trans);
  /**
   * Draws a bitmap on the current virtual screen, rotated around its center.
   *
   * Pixels in the bitmap's transparent color are skipped completely. The
   * bitmap is positioned at (@p x, @p y) then rotated by @p angle; 128 is
   * 180 degrees and 64 is 90 degrees.
   *
   * @param x      Horizontal position.
   * @param y      Vertical position.
   * The sprite argument is the bitmap to draw.
   * @param angle  Rotation from 0 to 255.
   * @since Interface version 13
   */
  AGSIFUNC(void)   BlitSpriteRotated(int32 x, int32 y, BITMAP *, int32 angle);

  // *** BELOW ARE INTERFACE VERSION 14 AND ABOVE ONLY
#ifdef WINDOWS_VERSION
  /**
   * Gets the main DirectSound interface used by the engine.
   *
   * The returned pointer is not AddRef'd. This is supported only on Windows.
   * Returns NULL when sound is disabled or when the WaveOut driver is in use.
   *
   * @return The IDirectSound interface, or NULL.
   * @since Interface version 14
   */
  AGSIFUNC(LPDIRECTSOUND) GetDirectSound();
#endif
  /**
   * Disables the AGS sound system completely.
   *
   * Use this when the plugin takes over all audio functionality.
   *
   * @since Interface version 14
   */
  AGSIFUNC(void)   DisableSound();
  /**
   * Tests whether a game script function may run now.
   *
   * @return 1 if no script is running; otherwise, 0.
   * @see CallGameScriptFunction
   * @see QueueGameScriptFunction
   * @since Interface version 14
   */
  AGSIFUNC(int)    CanRunScriptFunctionNow();
  /**
   * Runs a user-defined game script function.
   *
   * Call this only when CanRunScriptFunctionNow returns 1; it fails while a
   * script is running. Use QueueGameScriptFunction to defer the call until
   * the script engine is available.
   *
   * @param name          Script function name.
   * @param globalScript  1 for the global script, or 0 for the room script.
   * @param numArgs       Number of arguments, from 0 to 3.
   * @param arg1          First 32-bit integer argument.
   * @param arg2          Second 32-bit integer argument.
   * @param arg3          Third 32-bit integer argument.
   * @return 0 on success, or a non-zero value on failure.
   * @see CanRunScriptFunctionNow
   * @see QueueGameScriptFunction
   * @since Interface version 14
   */
  AGSIFUNC(int)    CallGameScriptFunction(const char *name, int32 globalScript, int32 numArgs, intptr_t arg1 = 0, intptr_t arg2 = 0, intptr_t arg3 = 0);

  // *** BELOW ARE INTERFACE VERSION 15 AND ABOVE ONLY
  /**
   * Notifies the engine that a sprite was updated by the plugin.
   *
   * Call this after changing a dynamic sprite so the screen reflects the
   * change.
   *
   * @param slot  Sprite slot number.
   * @since Interface version 15
   */
  AGSIFUNC(void)   NotifySpriteUpdated(int32 slot);
  /**
   * Enables or disables alpha blending for a dynamic 32-bit sprite.
   *
   * Do not use this with sprites of lower color depths or sprites created in
   * the AGS Editor; the results are unpredictable.
   *
   * @param slot            Dynamic 32-bit sprite slot number.
   * @param isAlphaBlended  1 to enable the alpha channel, or 0 to disable it.
   * @see IsSpriteAlphaBlended
   * @since Interface version 15
   */
  AGSIFUNC(void)   SetSpriteAlphaBlended(int32 slot, int32 isAlphaBlended);
  /**
   * Runs or queues a user-defined game script function.
   *
   * The function runs immediately when no script is running; otherwise it is
   * queued until the current script finishes.
   *
   * @param name          Script function name.
   * @param globalScript  1 for the global script, or 0 for the room script.
   * @param numArgs       Number of arguments, from 0 to 2.
   * @param arg1          First 32-bit integer argument.
   * @param arg2          Second 32-bit integer argument.
   * @see CanRunScriptFunctionNow
   * @see CallGameScriptFunction
   * @since Interface version 15
   */
  AGSIFUNC(void)   QueueGameScriptFunction(const char *name, int32 globalScript, int32 numArgs, intptr_t arg1 = 0, intptr_t arg2 = 0);
  /**
   * Registers a new managed object with the script engine.
   *
   * The object begins with a reference count of zero and may then be returned
   * to the script. The callback object must outlive the managed object.
   *
   * @param object    Address of the managed object.
   * @param callback  Object that implements its managed-object operations.
   * @return The managed pool key from interface version 16; its earlier value
   *         is not normally needed.
   * @since Interface version 15
   */
  AGSIFUNC(int)    RegisterManagedObject(void *object, IAGSScriptManagedObject *callback);
  /**
   * Registers a reader that deserializes a managed object type.
   *
   * Call this before loading any save game containing the type. The engine
   * retains @p typeName rather than copying it, so it must not be temporary
   * or local storage.
   *
   * @param typeName  Managed object type name.
   * @param reader    Reader that deserializes that type.
   * @see RegisterUnserializedObject
   * @since Interface version 15
   */
  AGSIFUNC(void)   AddManagedObjectReader(const char *typeName, IAGSManagedObjectReader *reader);
  /**
   * Re-registers a managed object read from a save game.
   *
   * Call this from the managed-object reader's deserialization routine, using
   * the key supplied by the engine to that reader.
   *
   * @param key       Engine-supplied managed pool key.
   * @param object    Address of the reconstructed object.
   * @param callback  Object that implements its managed-object operations.
   * @see AddManagedObjectReader
   * @since Interface version 15
   */
  AGSIFUNC(void)   RegisterUnserializedObject(int key, void *object, IAGSScriptManagedObject *callback);

  // *** BELOW ARE INTERFACE VERSION 16 AND ABOVE ONLY
  /**
   * Gets a managed object's address from its managed pool key.
   *
   * This can link related objects after deserialization.
   *
   * @param key  Managed pool key.
   * @return The managed object address, or NULL for an invalid key.
   * @see GetManagedObjectKeyByAddress
   * @since Interface version 16
   */
  AGSIFUNC(void*)  GetManagedObjectAddressByKey(int key);
  /**
   * Gets a managed object's managed pool key from its address.
   *
   * This can link related objects after deserialization. It is slow, so avoid
   * using it where possible.
   *
   * @param address  Managed object address.
   * @return The managed pool key, or -1 if the object is not in the pool.
   * @see GetManagedObjectAddressByKey
   * @since Interface version 16
   */
  AGSIFUNC(int)    GetManagedObjectKeyByAddress(void *address);

  // *** BELOW ARE INTERFACE VERSION 17 AND ABOVE ONLY
  /**
   * Creates a new script String containing a copy of text.
   *
   * The new String is added to the managed object pool and is freed
   * automatically when it has no script references. Because the text is
   * copied, the caller may free the source after this call.
   *
   * @param fromText  Text to copy into the new String.
   * @return The new script String.
   * @since Interface version 17
   */
  AGSIFUNC(const char*) CreateScriptString(const char *fromText);

  // *** BELOW ARE INTERFACE VERSION 18 AND ABOVE ONLY
  /**
   * Increments a managed object's reference count.
   *
   * Increment the reference count before storing a managed-object handle
   * between script function calls; otherwise the engine may dispose the
   * object early. Balance this with DecrementManagedObjectRefCount when the
   * stored handle is no longer needed.
   *
   * @param address  Address of the managed object.
   * @return The new reference count.
   * @see DecrementManagedObjectRefCount
   * @since Interface version 18
   */
  AGSIFUNC(int)    IncrementManagedObjectRefCount(void *address);
  /**
   * Decrements a managed object's reference count.
   *
   * Use this to balance IncrementManagedObjectRefCount after storing a
   * managed-object handle between script function calls. Adjusting reference
   * counts incorrectly can leak memory or crash the game.
   *
   * @param address  Address of the managed object.
   * @return The new reference count.
   * @see IncrementManagedObjectRefCount
   * @since Interface version 18
   */
  AGSIFUNC(int)    DecrementManagedObjectRefCount(void *address);
  /**
   * Sets the mouse cursor position.
   *
   * Coordinates use the real screen resolution, not script coordinates.
   *
   * @param x  Horizontal screen coordinate.
   * @param y  Vertical screen coordinate.
   * @since Interface version 18
   */
  AGSIFUNC(void)   SetMousePosition(int32 x, int32 y);
  /**
   * Simulates a mouse click.
   *
   * @p button is 1 for left, 2 for right, or 3 for middle; the effect is the
   * same as the player clicking that button.
   *
   * @param button  Mouse button to click.
   * @since Interface version 18
   */
  AGSIFUNC(void)   SimulateMouseClick(int32 button);
  /**
   * Gets the number of waypoints on a movement path.
   *
   * The movement-path functions inspect character and object movement. Obtain
   * @p pathId from AGSCharacter.walking or AGSObject.moving when it is
   * greater than 0.
   *
   * @param pathId  Movement path identifier.
   * @return The number of waypoints.
   * @since Interface version 18
   */
  AGSIFUNC(int)    GetMovementPathWaypointCount(int32 pathId);
  /**
   * Gets the last waypoint passed on a movement path.
   *
   * The starting point is waypoint 0. The destination is one less than the
   * waypoint count, but is not returned because the move finishes upon
   * reaching it.
   *
   * @param pathId  Movement path identifier.
   * @return The last waypoint passed.
   * @since Interface version 18
   */
  AGSIFUNC(int)    GetMovementPathLastWaypoint(int32 pathId);
  /**
   * Gets a waypoint's coordinates on a movement path.
   *
   * This performs no error checking; supply a valid path ID and waypoint.
   *
   * @param pathId    Movement path identifier.
   * @param waypoint  Waypoint index.
   * @param x         Receives the horizontal coordinate.
   * @param y         Receives the vertical coordinate.
   * @since Interface version 18
   */
  AGSIFUNC(void)   GetMovementPathWaypointLocation(int32 pathId, int32 waypoint, int32 *x, int32 *y);
  /**
   * Gets the speed from a waypoint to the next one.
   *
   * The horizontal and vertical speeds are 32-bit fixed-point values: the high
   * 16 bits are the whole part and the low 16 bits are the fractional part.
   *
   * @param pathId    Movement path identifier.
   * @param waypoint  Waypoint index.
   * @param xSpeed    Receives the horizontal speed.
   * @param ySpeed    Receives the vertical speed.
   * @since Interface version 18
   */
  AGSIFUNC(void)   GetMovementPathWaypointSpeed(int32 pathId, int32 waypoint, int32 *xSpeed, int32 *ySpeed);

  // *** BELOW ARE INTERFACE VERSION 19 AND ABOVE ONLY
  /**
   * Gets the current graphics driver identifier.
   *
   * The identifier is "D3D9" for the Direct3D driver or "DX5" for the
   * DirectDraw driver. Do not call this from AGS_EngineStartup, because the
   * graphics system has not initialized at that point.
   *
   * @return The graphics driver identifier.
   * @since Interface version 19
   */
  AGSIFUNC(const char*) GetGraphicsDriverID();

  // *** BELOW ARE INTERFACE VERSION 22 AND ABOVE ONLY
  /**
   * Tests whether the game runs under the AGS Editor debugger.
   *
   * @return Non-zero when the debugger is running; otherwise, 0.
   * @see BreakIntoDebugger
   * @since Interface version 22
   */
  AGSIFUNC(int)    IsRunningUnderDebugger();
  /**
   * Requests a break into the AGS Editor debugger.
   *
   * The break occurs when the next script line runs, not immediately. This
   * works only when IsRunningUnderDebugger returns non-zero.
   *
   * @see IsRunningUnderDebugger
   * @since Interface version 22
   */
  AGSIFUNC(void)   BreakIntoDebugger();
  /**
   * Gets a path for a file in the Compiled folder.
   *
   * Writes the path to the caller-supplied @p buffer, which must be at least
   * MAX_PATH bytes. Under the debugger it writes "Compiled/fileName";
   * otherwise it copies @p fileName. This function is deprecated since
   * interface version 27; prefer ResolveFilePath.
   *
   * @param fileName  File name to resolve.
   * @param buffer    Buffer that receives the path.
   * @since Interface version 22
   */
  AGSIFUNC(void)   GetPathToFileInCompiledFolder(const char* fileName, char* buffer);

  // *** BELOW ARE INTERFACE VERSION 23 AND ABOVE ONLY
#ifdef WINDOWS_VERSION
  /**
   * Gets the DirectInput keyboard device.
   *
   * This is supported only on Windows. The returned pointer is not AddRef'd,
   * so do not release it.
   *
   * @return The keyboard IDirectInputDevice interface.
   * @since Interface version 23
   */
  AGSIFUNC(LPDIRECTINPUTDEVICE) GetDirectInputKeyboard();
  /**
   * Gets the DirectInput mouse device.
   *
   * This is supported only on Windows. The returned pointer is not AddRef'd,
   * so do not release it.
   *
   * @return The mouse IDirectInputDevice interface.
   * @since Interface version 23
   */
  AGSIFUNC(LPDIRECTINPUTDEVICE) GetDirectInputMouse();
#endif
  /**
   * Replaces the renderer for a font.
   *
   * Keep the previous renderer returned by this method so it may be chained
   * or restored later. Prefer ReplaceFontRenderer2 for new renderers.
   *
   * @param fontNumber   Font number whose renderer to replace.
   * @param newRenderer  Replacement renderer.
   * @return The previous renderer.
   * @since Interface version 23
   */
  AGSIFUNC(IAGSFontRenderer*) ReplaceFontRenderer(int fontNumber, IAGSFontRenderer* newRenderer);

  // *** BELOW ARE INTERFACE VERSION 25 AND ABOVE ONLY
  /**
   * Gets the current render-stage description.
   *
   * The plugin must fill @c desc->Version before this call. It tells the
   * engine which structure fields it may assign; use 25 or higher. The
   * matrixes use the current renderer's internal format, are zero with the
   * Software renderer, and are useful only during a render-stage event.
   *
   * @param desc  Structure to receive the render-stage description.
   * @since Interface version 25
   */
  AGSIFUNC(void)  GetRenderStageDesc(AGSRenderStageDesc* desc);

  // *** BELOW ARE INTERFACE VERSION 26 AND ABOVE ONLY
  /**
   * Gets the game's description.
   *
   * The plugin must fill @c ginfo->Version before this call. It tells the
   * engine which structure fields it may assign; use 26 or higher.
   *
   * @param ginfo  Structure to receive the game information.
   * @since Interface version 26
   */
  AGSIFUNC(void)  GetGameInfo(AGSGameInfo* ginfo);
  /**
   * Replaces the renderer for a font with an extended renderer.
   *
   * The previous renderer remains a base-interface pointer for compatibility.
   * Use ReplaceFontRenderer to restore an old renderer.
   *
   * @param fontNumber   Font number whose renderer to replace.
   * @param newRenderer  Replacement extended renderer.
   * @return The previous renderer.
   * @see ReplaceFontRenderer
   * @since Interface version 26
   */
  AGSIFUNC(IAGSFontRenderer*) ReplaceFontRenderer2(int fontNumber, IAGSFontRenderer2* newRenderer);
  /**
   * Notifies the engine that a custom font changed.
   *
   * The engine recalculates metrics through the active renderer and redraws
   * the game GUI.
   *
   * @param fontNumber  Updated font number.
   * @see ReplaceFontRenderer2
   * @since Interface version 26
   */
  AGSIFUNC(void)  NotifyFontUpdated(int fontNumber);

  // *** BELOW ARE INTERFACE VERSION 27 AND ABOVE ONLY
  /**
   * Resolves a script path to a system path as File.Open does.
   *
   * Pass NULL for @p buf to receive the required path length in bytes.
   *
   * @param script_path  Script path to resolve.
   * @param buf          Output buffer, or NULL to query its required length.
   * @param buf_len      Size of @p buf in bytes.
   * @return Bytes written, required length when @p buf is NULL, or 0 on
   *         failure.
   * @since Interface version 27
   */
  AGSIFUNC(size_t) ResolveFilePath(const char *script_path, char *buf, size_t buf_len);

  // *** BELOW ARE INTERFACE VERSION 28 AND ABOVE ONLY
  /**
   * Opens a data stream for a script path.
   *
   * @p file_mode is an @c AGSSTREAM_FILE_* value and @p work_mode is a
   * combination of @c AGSSTREAM_MODE_* flags. The returned stream belongs to
   * the caller and must be released with IAGSStream::Dispose.
   *
   * @param script_path  Script path to open.
   * @param file_mode    File open mode.
   * @param work_mode    Requested stream work modes.
   * @return The new stream, or NULL on failure.
   * @see GetFileStreamByHandle
   * @since Interface version 28
   */
  AGSIFUNC(IAGSStream*) OpenFileStream(const char *script_path, int file_mode, int work_mode);
  /**
   * Gets the stream identified by an engine-supplied file handle.
   *
   * Use it only for the duration of the event callback that supplied the
   * handle. Ownership is not transferred: do not close or dispose the stream,
   * or the engine will fail.
   *
   * @param fhandle  Stream handle received in an event callback.
   * @return The associated stream, or NULL for an invalid handle.
   * @see OpenFileStream
   * @since Interface version 28
   */
  AGSIFUNC(IAGSStream*) GetFileStreamByHandle(int32 fhandle);

  // *** BELOW ARE INTERFACE VERSION 29 AND ABOVE ONLY
  /**
   * Writes a formatted message to the engine log.
   *
   * @p level is one of the @c AGSLOG_LEVEL_* constants. Formatting follows
   * the standard C @c printf convention.
   *
   * @param level  Log message level.
   * @param fmt    Format string.
   * @since Interface version 29
   */
  AGSIFUNC(void)  Log(int level, const char *fmt, ...);

  // *** BELOW ARE INTERFACE VERSION 30 AND ABOVE ONLY
  /**
   * Creates a dynamic array for use by game script.
   *
   * For an array of managed handles, pass true for @p is_managed_type and use
   * @c sizeof(int32) as @p elem_size. This declaration must be correct or the
   * engine cannot release handle references. Increment every handle's
   * reference count before writing it into the array, or it may be disposed
   * before the array.
   *
   * @param elem_count       Number of elements.
   * @param elem_size        Size of each element in bytes.
   * @param is_managed_type  Whether elements are managed handles.
   * @return The element array, or NULL if creation fails or arguments are
   *         invalid.
   * @see IncrementManagedObjectRefCount
   * @since Interface version 30
   */
  AGSIFUNC(void*) CreateDynamicArray(size_t elem_count, size_t elem_size, bool is_managed_type);
  /**
   * Gets a dynamic array's number of elements.
   *
   * @param arr  Dynamic array received from the engine or CreateDynamicArray.
   * @return The number of elements.
   * @see CreateDynamicArray
   * @since Interface version 30
   */
  AGSIFUNC(size_t) GetDynamicArrayLength(const void *arr);
  /**
   * Gets a dynamic array's total capacity in bytes.
   *
   * @param arr  Dynamic array received from the engine or CreateDynamicArray.
   * @return The total capacity in bytes.
   * @see CreateDynamicArray
   * @since Interface version 30
   */
  AGSIFUNC(size_t) GetDynamicArraySize(const void *arr);
};


/**
 * The editor-to-plugin interface, passed to AGS_EditorStartup when the AGS
 * Editor loads a native plugin at design time.
 *
 * @note This is **not** the same as @c AGS.Types.IAGSEditor, the C# interface
 *       used by managed editor plugins. The two are unrelated despite the
 *       shared name. If you are writing a .NET plugin, you want that one.
 */
class IAGSEditor {
public:
  /** Editor interface version. */ int32 version;
  /** Used internally by the editor; do not modify. */ int32 pluginId;

public:
  /**
   * Gets the window handle of the AGS Editor's main frame.
   *
   * @return The main editor frame's window handle.
   * @since Interface version 1
   */
  AGSIFUNC(HWND) GetEditorHandle ();
  /**
   * Gets the window handle of the current active window.
   *
   * Use this handle as a parent for message boxes, so currently displayed
   * dialog boxes remain modal.
   *
   * @return The current active window's handle.
   * @since Interface version 1
   */
  AGSIFUNC(HWND) GetWindowHandle ();
  /**
   * Adds script declarations to the built-in script header compiled into every
   * game script.
   *
   * The editor retains a reference to @p header rather than copying it; do not
   * overwrite or destroy the string after registering it.
   *
   * @param header  Script header text to register.
   * @since Interface version 1
   */
  AGSIFUNC(void) RegisterScriptHeader (const char *header);
  /**
   * Unregisters a script header that was previously registered.
   *
   * @param header  The same pointer passed to RegisterScriptHeader.
   * @since Interface version 1
   */
  AGSIFUNC(void) UnregisterScriptHeader (const char *header);
};


#ifdef THIS_IS_THE_PLUGIN

#ifdef WINDOWS_VERSION
/** Plugin export declaration. */
#define DLLEXPORT extern "C" __declspec(dllexport)
#else
// MAC VERSION: compile with -fvisibility=hidden
// gcc -dynamiclib -std=gnu99 agsplugin.c -fvisibility=hidden -o agsplugin.dylib
/** Plugin export declaration. */
#define DLLEXPORT extern "C" __attribute__((visibility("default")))
#endif

/**
 * Gets the user-friendly plugin name during design time.
 *
 * @return A static string containing the plugin description.
 */
DLLEXPORT const char * AGS_GetPluginName(void);
/**
 * Initializes the plugin when the editor adds it to a game at design time.
 *
 * @return 0 on success; any other value on failure, after which the editor
 *         stops communicating with the plugin until the user starts it again.
 */
DLLEXPORT int    AGS_EditorStartup (IAGSEditor *);
/**
 * Shuts down the plugin when the editor removes it from a game at design time.
 *
 * Unregister anything registered during AGS_EditorStartup.
 * This callback has no return value.
 */
DLLEXPORT void   AGS_EditorShutdown (void);
/**
 * Opens the optional plugin properties UI at design time.
 *
 * This callback has no return value.
 */
DLLEXPORT void   AGS_EditorProperties (HWND);
/**
 * Saves design-time plugin data into the current game file.
 *
 * @return The number of bytes used in the supplied buffer, up to and including
 *         its size.
 */
DLLEXPORT int    AGS_EditorSaveGame (char *, int);
/**
 * Loads design-time plugin data from the current game file.
 *
 * The supplied buffer is freed when this function returns; copy data that
 * must persist elsewhere.
 * This callback has no return value.
 */
DLLEXPORT void   AGS_EditorLoadGame (char *, int);
/**
 * Initializes the plugin when the game engine loads at run time.
 *
 * Register script functions and request event hooks here.
 * This callback has no return value.
 */
DLLEXPORT void   AGS_EngineStartup (IAGSEngine *);
/**
 * Shuts down the plugin just before the game engine exits at run time.
 *
 * This callback has no return value.
 */
DLLEXPORT void   AGS_EngineShutdown (void);
/**
 * Handles a requested engine event at run time.
 *
 * @return 0 to continue normal event processing, or 1 to prevent other
 *         plugin and text-script handlers from being called.
 */
DLLEXPORT intptr_t AGS_EngineOnEvent (int, intptr_t);
/**
 * Handles a script-debug event at run time.
 *
 * @return 1 if the debug event was handled, or 0 to allow other plugins to
 *         handle it.
 */
DLLEXPORT int    AGS_EngineDebugHook(const char *, int, int);
/**
 * Runs immediately before graphics-driver initialization at run time.
 *
 * This callback has no return value.
 */
DLLEXPORT void   AGS_EngineInitGfx(const char* driverID, void *data); 
// Export this to let engine verify that this is a compatible AGS Plugin;
// exact return value is not essential, but should be non-zero for consistency.
/**
 * Marks the plugin as compatible with engine interface version 5 and later.
 *
 * This export is used in both design time and run time to recognize a valid
 * plugin, but AGS never calls it.
 *
 * @return Any non-zero value; the exact value does not matter.
 */
DLLEXPORT int    AGS_PluginV2 ();

#endif // THIS_IS_THE_PLUGIN

#endif // _AGS_PLUGIN_H
