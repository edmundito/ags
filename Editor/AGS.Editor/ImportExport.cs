using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using AGS.Editor.Utils;
using AGS.Types;

namespace AGS.Editor
{
    public class ImportExport
    {
        private const string MODULE_FILE_SIGNATURE = "AGSScriptModule\0";
		private const uint MODULE_FILE_TRAILER = 0xb4f76a65;
        private const uint MODULE_FILE_SECTION = 0xb4f76a66;
        private const string CHARACTER_FILE_SIGNATURE = "AGSCharacter";
		private const string SINGLE_RUN_SCRIPT_TAG = "$$SINGLE_RUN_SCRIPT$$";

        private const string GUI_XML_ROOT_NODE = "ExportedGUI";
        private const string GUI_XML_VERSION_ATTRIBUTE = "Version";
        // TODO: split XML versions for base object (GUI/Character) and
        // nested items (like sprites) to allow converting from future versions
        // if only nested items format changed (when still possible)
        // TODO: find a way to make version number maintenance more convenient,
        // because right now it's very easy to forget updating these!
        // Maybe think about this when implementing a uniform method to import/export every project item
        // *  1: Initial
        // *  2: Sprites have "real resolution"
        private const int GUI_XML_CURRENT_VERSION = 2;
        private const string GUI_XML_PALETTE_NODE = "Palette";
        private const string GUI_XML_SPRITES_NODE = "UsedSprites";
        private const string GUI_XML_SPRITE_NODE = "SpriteData";

        private const string CHARACTER_XML_ROOT_NODE = "ExportedCharacter";
        private const string CHARACTER_XML_VERSION_ATTRIBUTE = "Version";
        // *  1: Initial
        // *  2: Sprites have "real resolution"
        private const int CHARACTER_XML_CURRENT_VERSION = 2;
        private const string CHARACTER_XML_PALETTE_NODE = "Palette";
        private const string CHARACTER_XML_VIEWS_NODE = "Views";

        private const string GUI_XML_SPRITE_NUMBER = "Number";
        private const string GUI_XML_SPRITE_COLOR_DEPTH = "ColorDepth";
        private const string GUI_XML_SPRITE_ALPHA_CHANNEL = "AlphaChannel";
        private const string GUI_XML_SPRITE_WIDTH = "Width";
        private const string GUI_XML_SPRITE_HEIGHT = "Height";
        private const string GUI_XML_SPRITE_RESOLUTION = "Resolution";

        private static int SPRITE_FLAG_HI_RES = NativeConstants.SPF_HIRES;
        private static int SPRITE_FLAG_ALPHA_CHANNEL = NativeConstants.SPF_ALPHACHANNEL;
        private static int EDITOR_DAT_LATEST_FILE_VERSION = 7;
        private static Dictionary<string, ImageFormat> ImageFileTypes = new Dictionary<string, ImageFormat>();

        static ImportExport()
        {
            ImageFileTypes.Add(".bmp", ImageFormat.Bmp);
            ImageFileTypes.Add(".gif", ImageFormat.Gif);
            ImageFileTypes.Add(".jpg", ImageFormat.Jpeg);
            ImageFileTypes.Add(".png", ImageFormat.Png);
            ImageFileTypes.Add(".tif", ImageFormat.Tiff);
        }

        public static void ExportBitmapToFile(string fileName, Bitmap bmp)
        {
            try
            {
                string extension = System.IO.Path.GetExtension(fileName).ToLower();
                if (ImportExport.ImageFileTypes.ContainsKey(extension))
                {
                    bmp.Save(fileName, ImportExport.ImageFileTypes[extension]);
                }
                else
                {
                    Factory.GUIController.ShowMessage("Invalid file extension '" + extension + "'. Format not recognised.", System.Windows.Forms.MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                Factory.GUIController.ShowMessage("There was an error exporting the file. The error message was: '" + ex.Message + "'. Please try again", System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }

        public static void ExportPaletteToFile(string filename)
        {
            if (filename.ToLower().EndsWith(".pal"))
            {
                FileStream fs = new FileStream(filename, FileMode.Create);
                byte[] paletteData = Factory.AGSEditor.CurrentGame.GetPaletteAsRawPAL();
                fs.Write(paletteData, 0, paletteData.Length);
                fs.Close();
            }
            else if (filename.ToLower().EndsWith(".bmp"))
            {
                Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format8bppIndexed);
                ColorPalette pal = bmp.Palette;
                for (int i = 0; i < Factory.AGSEditor.CurrentGame.Palette.Length; i++)
                {
                    pal.Entries[i] = Factory.AGSEditor.CurrentGame.Palette[i].Colour;
                }
                // The palette needs to be re-set onto the bitmap to force it
                // to update its internal storage of the colours
                bmp.Palette = pal;
                bmp.Save(filename, ImageFormat.Bmp);
            }
            else
            {
                throw new AGSEditorException("Invalid file format requested for export. Supports PAL and BMP.");
            }
        }

        public static PaletteEntry[] ImportPaletteFromFile(string filename)
        {
            PaletteEntry[] newPalette = new PaletteEntry[256];

            if (filename.ToLower().EndsWith(".pal"))
            {
                byte[] rawPalette = new byte[768];
                FileStream fs = new FileStream(filename, FileMode.Open);
                fs.Read(rawPalette, 0, rawPalette.Length);
                fs.Close();

                for (int i = 0; i < newPalette.Length; i++)
                {
                    newPalette[i] = new PaletteEntry(i, Color.FromArgb(rawPalette[i * 3] * 4, rawPalette[i * 3 + 1] * 4, rawPalette[i * 3 + 2] * 4));
                }
            }
            else if (filename.ToLower().EndsWith(".bmp"))
            {
                Bitmap bmp = new Bitmap(filename);
                if (!bmp.IsIndexed())
                {
                    throw new AGSEditorException("Selected bitmap file is not a 256-colour bitmap. Only 8-bit images can have a palette imported.");
                }
                for (int i = 0; i < newPalette.Length; i++)
                {
                    newPalette[i] = new PaletteEntry(i, bmp.Palette.Entries[i]);
                }
                bmp.Dispose();
            }
            else
            {
                throw new AGSEditorException("Invalid file format requested for import. Supports PAL and BMP.");
            }

            return newPalette;
        }

        private static bool CreateScriptsForInteraction(string itemName, Script script, Interactions interactions, CompileMessages errors)
        {
            bool convertedSome = false;

            for (int i = 0; i < interactions.ImportedScripts.Length; i++)
            {
                if ((interactions.ImportedScripts[i] != null) &&
                    (interactions.ImportedScripts[i].Length > 0))
                {
					if (interactions.ImportedScripts[i].IndexOf(SINGLE_RUN_SCRIPT_TAG) >= 0)
					{
						// just a single Run Script -- don't wrap it in an outer
						// function since there's no need to
						interactions.ScriptFunctionNames[i] = interactions.ImportedScripts[i].Replace(SINGLE_RUN_SCRIPT_TAG, string.Empty).Replace("();", string.Empty).Trim();
						convertedSome = true;
					}
					else
					{
						string funcName = itemName + "_" + interactions.FunctionSuffixes[i];
						string funcScriptLine = "function " + funcName + "()";
						interactions.ScriptFunctionNames[i] = funcName;
						if (script.Text.IndexOf(funcScriptLine) >= 0)
						{
							errors.Add(new CompileWarning("Function " + funcName + " already exists in script and could not be created"));
						}
						else
						{
							script.Text += Environment.NewLine + funcScriptLine + Environment.NewLine;
							script.Text += "{" + Environment.NewLine + interactions.ImportedScripts[i] + "}" + Environment.NewLine;
							convertedSome = true;
						}
					}
                    interactions.ImportedScripts[i] = null;
                }
            }

            return convertedSome;
        }

        public static void CreateInteractionScripts(Game game, CompileMessages errors)
        {
            Script theScript = game.RootScriptFolder.GetScriptByFileName(Script.GLOBAL_SCRIPT_FILE_NAME, true);
            foreach (InventoryItem item in game.RootInventoryItemFolder.AllItemsFlat)
            {
                if (item.Name.Length < 1)
                {
					item.Name = "iInventory" + item.ID;
                }
				CreateScriptsForInteraction(item.Name, theScript, item.Interactions, errors);
            }
            foreach (Character character in game.RootCharacterFolder.AllItemsFlat)
            {
                if (character.ScriptName.Length < 1)
                {
					character.ScriptName = "cCharacter" + character.ID;
                }
				CreateScriptsForInteraction(character.ScriptName, theScript, character.Interactions, errors);
            }
        }

        public static bool CreateInteractionScripts(Room room, CompileMessages errors)
        {
            Script theScript = room.Script;
            bool convertedSome = CreateScriptsForInteraction("room", theScript, room.Interactions, errors);

            foreach (RoomHotspot hotspot in room.Hotspots)
            {
				if (hotspot.Name.Length < 1)
                {
                    hotspot.Name = "hHotspot" + hotspot.ID;
                }
                convertedSome |= CreateScriptsForInteraction(hotspot.Name, theScript, hotspot.Interactions, errors);
            }
            foreach (RoomObject obj in room.Objects)
            {
                if (obj.Name.Length < 1)
                {
					obj.Name = "oObject" + obj.ID;
                }
				convertedSome |= CreateScriptsForInteraction(obj.Name, theScript, obj.Interactions, errors);
            }
            foreach (RoomRegion region in room.Regions)
            {
                string useName = "region" + region.ID;
                convertedSome |= CreateScriptsForInteraction(useName, theScript, region.Interactions, errors);
            }
            return convertedSome;
        }

        public static CompileMessages ImportOldEditorDatFile(string fileName, Game game, Dictionary<int, Sprite> spriteList)
        {
            CompileMessages importErrors = new CompileMessages();
            string editorDatFilename = Path.Combine(Path.GetDirectoryName(fileName), "editor.dat");
            BinaryReader reader = new BinaryReader(new FileStream(editorDatFilename, FileMode.Open));
            string fileSig = Encoding.ASCII.GetString(reader.ReadBytes(14));
            if (fileSig != "AGSEditorInfo\0")
            {
                throw new AGS.Types.InvalidDataException("This is not a valid AGS game file.");
            }
            int version = reader.ReadInt32();
            if (version != EDITOR_DAT_LATEST_FILE_VERSION)
            {
                throw new AGS.Types.InvalidDataException("This game is from an unsupported version of AGS. This editor can only import games saved with AGS 2.72.");
            }
            game.RootScriptFolder.Clear();

            Script globalScript, scriptHeader;
            ReadGlobalScriptAndScriptHeader(reader, game, out globalScript, out scriptHeader);

            game.RootSpriteFolder = ImportSpriteFolders(reader, spriteList, importErrors);

            ImportRoomList(game, reader, fileName, importErrors);

            if (reader.ReadInt32() != 1)
            {
                throw new AGS.Types.InvalidDataException("Error in game files: invalid header for script modules");
            }

            int moduleCount = reader.ReadInt32();
            for (int i = 0; i < moduleCount; i++)
            {
                string author = ReadNullTerminatedString(reader);
                string description = ReadNullTerminatedString(reader);
                string name = ReadNullTerminatedString(reader);
                string moduleVersion = ReadNullTerminatedString(reader);

                int scriptLength = reader.ReadInt32();
                string moduleScript = Encoding.Default.GetString(reader.ReadBytes(scriptLength)); // always ANSI
                reader.ReadByte();  // discard the null terminating byte

                scriptLength = reader.ReadInt32();
                string moduleHeader = Encoding.Default.GetString(reader.ReadBytes(scriptLength)); // always ANSI
                reader.ReadByte();  // discard the null terminating byte

                int uniqueKey = reader.ReadInt32();

                ScriptAndHeader scripts = new ScriptAndHeader(
                        new Script("Module" + i + ".ash", moduleHeader, name, description, author, moduleVersion, uniqueKey, true),
                        new Script("Module" + i + ".asc", moduleScript, name, description, author, moduleVersion, uniqueKey, false));
                game.RootScriptFolder.Items.Add(scripts);

                reader.ReadInt32(); ////int permissions = reader.ReadInt32();
                reader.ReadInt32(); ////int weAreOwner = reader.ReadInt32();
            }

            game.RootScriptFolder.Items.Add(new ScriptAndHeader(scriptHeader, globalScript));            

            // Ensure that all .asc/.ash files are saved
            foreach (Script script in game.RootScriptFolder.AllScriptsFlat)
            {
                script.Modified = true;
            }

            int voxFilesListLength = reader.ReadInt32();
            reader.ReadBytes(voxFilesListLength); // skip over vox file list

            // The final portion of the file contains state data
            // for COM plugins, but since we no longer support these,
            // we can ignore it and finish here.

            reader.Close();
            return importErrors;
        }

        public static List<Script> AddImportedScriptModule(ScriptModuleDef module)
        {
            string author = module.Author;
            string description = module.Description;
            string name = module.Name;
            string version = module.Version;
            string scriptText = module.Script;
            string headerText = module.Header;
            int uniqueKey = module.UniqueKey;

            List<Script> scriptsImported = new List<Script>();
            Script header = new Script(null, headerText, name, description, author, version, uniqueKey, true);
            Script mainScript = new Script(null, scriptText, name, description, author, version, uniqueKey, false);
            scriptsImported.Add(header);
            scriptsImported.Add(mainScript);
            return scriptsImported;
        }

        public static List<Script> ImportScriptModule(string fileName, Encoding defEncoding)
        {
            BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            string fileSig = Encoding.ASCII.GetString(reader.ReadBytes(16));
            if (fileSig != MODULE_FILE_SIGNATURE)
            {
                reader.Close();
                throw new AGS.Types.InvalidDataException("This is not a valid AGS script module.");
            }
            if (reader.ReadInt32() != 1)
            {
                reader.Close();
                throw new AGS.Types.InvalidDataException("This module requires a newer version of AGS.");
            }

            Encoding enc = defEncoding;

            var bAuthor = ReadNullTerminatedStringAsBytes(reader);
            var bDescription = ReadNullTerminatedStringAsBytes(reader);
            var bName = ReadNullTerminatedStringAsBytes(reader);
            var version = ReadNullTerminatedString(reader);

            int scriptLength = reader.ReadInt32();
            var scriptBytes = reader.ReadBytes(scriptLength);
            reader.ReadByte();  // discard null terminator

            int headerLength = reader.ReadInt32();
            var headerBytes = reader.ReadBytes(headerLength);
            reader.ReadByte();  // discard null terminator

            int uniqueKey = reader.ReadInt32();
            reader.ReadInt32(); // permissions (obselete)
            reader.ReadInt32(); // owner (obsolete)

            uint section = reader.ReadUInt32();
            // extension 1
            int encodingCP = 0;
            if (section == MODULE_FILE_SECTION)
            {
                encodingCP = reader.ReadInt32(); // text encoding hint
            }
            reader.Close();

            try
            {
                if (encodingCP > 0)
                    enc = Encoding.GetEncoding(encodingCP);
            }
            catch (ArgumentException) { }

            string author = enc.GetString(bAuthor);
            string description = enc.GetString(bDescription);
            string name = enc.GetString(bName);
            string script = enc.GetString(scriptBytes);
            string header = enc.GetString(headerBytes);

            ScriptModuleDef module;
            module.Author = author;
            module.Description = description;
            module.Name = name;
            module.Version = version;
            module.Script = script;
            module.Header = header;
            module.UniqueKey = uniqueKey;

            return AddImportedScriptModule(module);
        }


        public static void ExportScriptModule(Script header, Script script, string fileName, Encoding defEncoding)
        {
            Encoding enc = defEncoding;
            BinaryWriter writer = new BinaryWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write));
            writer.Write(Encoding.ASCII.GetBytes(MODULE_FILE_SIGNATURE));
            writer.Write((int)1);  // version

            WriteStringTerminated(script.Author, enc, writer);
            WriteStringTerminated(script.Description, enc, writer);
            WriteStringTerminated(script.Name, enc, writer);
            WriteStringTerminated(script.Version, enc, writer);

            WriteStringLongTerminated(script.Text, enc, writer);

            WriteStringLongTerminated(header.Text, enc, writer);

            writer.Write((int)script.UniqueKey);
            writer.Write((int)0);  // Permissions (obselete)
            writer.Write((int)0);  // We are owner (obselete)

            // format extension 1
            writer.Write((uint)MODULE_FILE_SECTION);
            writer.Write(defEncoding.CodePage);

            // end of format
            writer.Write((uint)MODULE_FILE_TRAILER);
            writer.Close();
        }

        private static void ReadGlobalScriptAndScriptHeader(BinaryReader reader, Game game, out Script globalScript, out Script scriptHeader)
        {
            // NOTE: old game format: texts are always ANSI/ASCII
            int scriptHeaderLength = reader.ReadInt32() - 1;
            string scriptHeaderText = Encoding.Default.GetString(reader.ReadBytes(scriptHeaderLength));
            string extraScript = "// Automatically converted interaction variables" + Environment.NewLine;
            foreach (OldInteractionVariable var in game.OldInteractionVariables)
            {
                extraScript += "import int " + var.ScriptName + ";" + Environment.NewLine;
            }
            scriptHeaderText += extraScript;
            scriptHeader = new Script(Script.GLOBAL_HEADER_FILE_NAME, scriptHeaderText, true);
            reader.ReadByte(); // skip the null terminator

            int globalScriptLength = reader.ReadInt32() - 1;
            string globalScriptText = Encoding.Default.GetString(reader.ReadBytes(globalScriptLength));
            extraScript = "// Automatically converted interaction variables" + Environment.NewLine;
            foreach (OldInteractionVariable var in game.OldInteractionVariables)
            {
                extraScript += string.Format("int {0} = {1};{2}export {0};{2}", var.ScriptName, var.Value, Environment.NewLine);
            }
            globalScriptText = extraScript + globalScriptText;
            globalScript = new Script(Script.GLOBAL_SCRIPT_FILE_NAME, globalScriptText, false);
            reader.ReadByte(); // skip the null terminator
        }

		private static string ReadNullTerminatedString(BinaryReader reader)
 		{
			return Encoding.Default.GetString(ReadNullTerminatedStringAsBytes(reader, 0));
		}

		private static string ReadNullTerminatedString(BinaryReader reader, int fixedLength)
		{
			return Encoding.Default.GetString(ReadNullTerminatedStringAsBytes(reader, fixedLength));
		}

        private static byte[] ReadNullTerminatedStringAsBytes(BinaryReader reader, int fixedLength = 0)
        {
			List<byte> bytes = new List<byte>(100);
			int bytesToRead = fixedLength;
			byte thisChar;
			while ((thisChar = reader.ReadByte()) != 0)
			{
				bytes.Add(thisChar);
				bytesToRead--;

				if ((fixedLength > 0) && (bytesToRead < 1))
				{
					break;
				}
			}
			if (bytesToRead > 0)
			{
				reader.ReadBytes(bytesToRead - 1);
			}
			return bytes.ToArray();
		}

        // TODO: merge with the similar methods in DataFileWriter, maybe make a class with Encoder
        /// Writes a null-terminated string using given encoding
        private static void WriteStringTerminated(string text, Encoding enc, BinaryWriter writer)
        {
            writer.Write(enc.GetBytes(text));
            writer.Write((byte)0);
        }

        /// Writes a null-terminated string, prepending with 32-bit length value (in bytes)
        private static void WriteStringLongTerminated(string text, Encoding enc, BinaryWriter writer)
        {
            writer.Write(enc.GetByteCount(text));
            writer.Write(enc.GetBytes(text));
            writer.Write((byte)0);
        }

		private static int GetRoomNumberFromFileName(string fileName)
		{
			string roomFile = fileName.Substring(4);
			roomFile = roomFile.Substring(0, roomFile.IndexOf("."));
			int roomNumber = -1;
			if (Int32.TryParse(roomFile, out roomNumber))
			{
				roomFile = "room" + roomNumber + ".crm";
				if (string.Compare(roomFile, fileName, true) == 0)
				{
					return roomNumber;
				}
			}
			return -1;
		}

        private static void ImportRoomList(Game game, BinaryReader reader, string fullPathToGameFiles, CompileMessages importErrors)
        {
            Dictionary<int, UnloadedRoom> rooms = new Dictionary<int, UnloadedRoom>();
            foreach (string roomFileFullPath in Utilities.GetDirectoryFileList(Path.GetDirectoryName(fullPathToGameFiles), "room*.crm"))
            {
                int roomNumber = GetRoomNumberFromFileName(Path.GetFileName(roomFileFullPath));

				if (roomNumber >= 0)
                {
                    try
                    {
                        string roomScript = Factory.NativeProxy.LoadRoomScript(roomFileFullPath);
                        if (roomScript != null)
                        {
                            StreamWriter sw = new StreamWriter(roomFileFullPath.Substring(0, roomFileFullPath.Length - 4) + ".asc", false, Encoding.Default);
                            sw.Write(roomScript);
                            sw.Close();
                        }
                    }
                    catch (AGSEditorException ex)
                    {
                        importErrors.Add(new CompileError("There was an error saving the script for room " + roomNumber + ": " + ex.Message));
                    }

                    UnloadedRoom newUnloadedRoom = new UnloadedRoom(roomNumber);
                    rooms.Add(roomNumber, newUnloadedRoom);
                    game.RootRoomFolder.Items.Add(newUnloadedRoom);
                }
                else
                {
                    importErrors.Add(new CompileWarning("The room file '" + roomFileFullPath + "' does not have a recognised name and will not be part of the game."));
                }
            }

            game.RootRoomFolder.Sort(false);

            int roomCount = reader.ReadInt32();
            for (int i = 0; i < roomCount; i++)
            {
                string roomDescription = ReadNullTerminatedString(reader);
                if (rooms.ContainsKey(i))
                {
                    rooms[i].Description = roomDescription;
                }
            }
        }

        private static SpriteFolder ImportSpriteFolders(BinaryReader reader, Dictionary<int,Sprite> spriteList, CompileMessages importErrors)
        {
            const string DUMMY_FOLDER_NAME = "$$TEMPNAME";
            Dictionary<int, SpriteFolder> spriteFolders = new Dictionary<int, SpriteFolder>();
            int spriteFolderCount = reader.ReadInt32();
            // First, create an object for all of them (since folder 2
            // might have folder 15 as its parent, all objects need
            // to be there from the start).
            for (int i = 0; i < spriteFolderCount; i++)
            {
                spriteFolders.Add(i, new SpriteFolder(DUMMY_FOLDER_NAME));
            }
            for (int i = 0; i < spriteFolderCount; i++)
            {
                int numItems = reader.ReadInt32();
                for (int j = 0; j < 240; j++)
                {
                    int spriteNum = reader.ReadInt16();
                    if ((j < numItems) && (spriteNum >= 0))
                    {
                        if (spriteList.ContainsKey(spriteNum))
                        {
                            spriteFolders[i].Sprites.Add(spriteList[spriteNum]);
                            spriteList.Remove(spriteNum);
                        }
                        else
                        {
                            importErrors.Add(new CompileWarning("Sprite " + spriteNum + " not found whilst importing the sprite folder list"));
                        }
                    }
                }
                int parentFolder = reader.ReadInt16();
                if (parentFolder >= 0)
                {
                    if (!spriteFolders.ContainsKey(parentFolder))
                    {
                        throw new AGS.Types.InvalidDataException("Invalid sprite folder structure found: folder " + i + " has parent " + parentFolder);
                    }
                    spriteFolders[parentFolder].SubFolders.Add(spriteFolders[i]);
                }
				string folderName = ReadNullTerminatedString(reader, 30);
                if (folderName.Contains("\0"))
                {
                    folderName = folderName.TrimEnd('\0');
                }
                spriteFolders[i].Name = folderName;
            }
            return spriteFolders[0];
        }

        private static void WriteZeros(BinaryWriter writer, int numberOfZeros)
        {
            if (numberOfZeros == 4)
            {
                writer.Write((int)0);
            }
            else if (numberOfZeros > 0)
            {
                byte[] zeros = new byte[numberOfZeros];
                for (int i = 0; i < numberOfZeros; i++)
                {
                    zeros[i] = 0;
                }
                writer.Write(zeros);
            }
        }

        public static void ExportCharacterNewFormat(Character character, string fileName, Game game)
        {
            Utilities.TryDeleteFile(fileName);
            XmlTextWriter writer = new XmlTextWriter(fileName, game.TextEncoding);
            writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"" + game.TextEncoding.WebName + "\"");
            writer.WriteComment("AGS Exported Character file. DO NOT EDIT THIS FILE BY HAND, IT IS GENERATED AUTOMATICALLY BY THE AGS EDITOR.");
            writer.WriteStartElement(CHARACTER_XML_ROOT_NODE);
            writer.WriteAttributeString(CHARACTER_XML_VERSION_ATTRIBUTE, CHARACTER_XML_CURRENT_VERSION.ToString());

            character.ToXml(writer);

            writer.WriteStartElement(CHARACTER_XML_VIEWS_NODE);

            Dictionary<int, object> spritesWritten = new Dictionary<int, object>();

            if (character.NormalView > 0)
            {
                writer.WriteStartElement("NormalView");
                WriteNewStyleView(writer, game.FindViewByID(character.NormalView), spritesWritten);
                writer.WriteEndElement();
            }
            if (character.SpeechView > 0)
            {
                writer.WriteStartElement("SpeechView");
                WriteNewStyleView(writer, game.FindViewByID(character.SpeechView), spritesWritten);
                writer.WriteEndElement();
            }
            if (character.IdleView > 0)
            {
                writer.WriteStartElement("IdleView");
                WriteNewStyleView(writer, game.FindViewByID(character.IdleView), spritesWritten);
                writer.WriteEndElement();
            }
            if (character.ThinkingView > 0)
            {
                writer.WriteStartElement("ThinkingView");
                WriteNewStyleView(writer, game.FindViewByID(character.ThinkingView), spritesWritten);
                writer.WriteEndElement();
            }
            if (character.BlinkingView > 0)
            {
                writer.WriteStartElement("BlinkingView");
                WriteNewStyleView(writer, game.FindViewByID(character.BlinkingView), spritesWritten);
                writer.WriteEndElement();
            }
            
            writer.WriteEndElement();

            game.WritePaletteToXML(writer);

            writer.WriteEndElement();
            writer.Close();
        }

        /// <summary>
        /// Export a 2.72-compatible CHA file. This code is horrid, but it's
        /// backwards compatible!!
        /// </summary>
        public static void ExportCharacter272(Character character, string fileName, Game game)
        {
            BinaryWriter writer = new BinaryWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write));
            writer.Write(Encoding.ASCII.GetBytes(CHARACTER_FILE_SIGNATURE));
            writer.Write((int)6);
            for (int i = 0; i < 256; i++)
            {
                writer.Write((byte)(game.Palette[i].Colour.R / 4));
                writer.Write((byte)(game.Palette[i].Colour.G / 4));
                writer.Write((byte)(game.Palette[i].Colour.B / 4));
                writer.Write((byte)0);
            }

            WriteZeros(writer, 4);
            writer.Write(character.SpeechView);
            WriteZeros(writer, 4);
            writer.Write(character.StartingRoom);
            WriteZeros(writer, 4);
            writer.Write(character.StartX);
            writer.Write(character.StartY);
            WriteZeros(writer, 12);
            writer.Write(character.IdleView);
            WriteZeros(writer, 12);
            writer.Write(character.SpeechColor);
            writer.Write(character.ThinkingView);
            writer.Write((short)character.BlinkingView);
            WriteZeros(writer, 42);
            writer.Write((short)character.MovementSpeed);
            writer.Write((short)character.AnimationDelay);
            WriteZeros(writer, 606);
            writer.Write(Encoding.Default.GetBytes(character.RealName)); // always ANSI
            WriteZeros(writer, 40 - character.RealName.Length);
            string scriptName = character.ScriptName;
            if (scriptName.StartsWith("c"))
            {
                scriptName = scriptName.Substring(1).ToUpperInvariant();
            }
            writer.Write(Encoding.Default.GetBytes(scriptName)); // always ANSI
            WriteZeros(writer, 20 - scriptName.Length);
            writer.Write((short)0);

            try
            {
                WriteOldStyleView(writer, game.FindViewByID(character.NormalView));

                if (character.SpeechView > 0)
                {
                    WriteOldStyleView(writer, game.FindViewByID(character.SpeechView));
                }
                if (character.IdleView > 0)
                {
                    WriteOldStyleView(writer, game.FindViewByID(character.IdleView));
                }
                if (character.ThinkingView > 0)
                {
                    WriteOldStyleView(writer, game.FindViewByID(character.ThinkingView));
                }
                if (character.BlinkingView > 0)
                {
                    WriteOldStyleView(writer, game.FindViewByID(character.BlinkingView));
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                writer.Close();
                Utilities.TryDeleteFile(fileName);
                throw ex;
            }
        }

        public static Character ImportCharacterNew(string fileName, Game game)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(fileName);
            }
            catch (XmlException ex)
            {
                throw new AGS.Types.InvalidDataException("This does not appear to be a valid AGS Character file." + Environment.NewLine + Environment.NewLine + ex.Message, ex);
            }

            if (doc.DocumentElement.Name != CHARACTER_XML_ROOT_NODE)
            {
                throw new AGS.Types.InvalidDataException("Not a valid AGS Character file.");
            }
            string xml_version = SerializeUtils.GetAttributeString(doc.DocumentElement, CHARACTER_XML_VERSION_ATTRIBUTE);
            int version_num;
            if (!Int32.TryParse(xml_version, out version_num) || version_num > CHARACTER_XML_CURRENT_VERSION)
            {
                throw new AGS.Types.InvalidDataException("This file requires a newer version of AGS to import it.");
            }

            Character newChar = new Character(doc.DocumentElement.FirstChild);
            EnsureCharacterScriptNameIsUnique(newChar, game);

            // Clear any existing event handler function names
            for (int i = 0; i < newChar.Interactions.ScriptFunctionNames.Length; i++)
            {
                newChar.Interactions.ScriptFunctionNames[i] = string.Empty;
            }

            PaletteEntry[] palette = game.ReadPaletteFromXML(doc.DocumentElement);
            SpriteFolder newFolder = new SpriteFolder(newChar.ScriptName + "Import");

            Dictionary<int, int> spriteMapping = new Dictionary<int, int>();
            XmlNode viewsNode = doc.DocumentElement.SelectSingleNode("Views");

            if (newChar.NormalView > 0)
            {
                newChar.NormalView = ReadAndAddNewStyleView(viewsNode.SelectSingleNode("NormalView"), game, spriteMapping, palette, newFolder);
            }
            if (newChar.SpeechView > 0)
            {
                newChar.SpeechView = ReadAndAddNewStyleView(viewsNode.SelectSingleNode("SpeechView"), game, spriteMapping, palette, newFolder);
            }
            if (newChar.IdleView > 0)
            {
                newChar.IdleView = ReadAndAddNewStyleView(viewsNode.SelectSingleNode("IdleView"), game, spriteMapping, palette, newFolder);
            }
            if (newChar.ThinkingView > 0)
            {
                newChar.ThinkingView = ReadAndAddNewStyleView(viewsNode.SelectSingleNode("ThinkingView"), game, spriteMapping, palette, newFolder);
            }
            if (newChar.BlinkingView > 0)
            {
                newChar.BlinkingView = ReadAndAddNewStyleView(viewsNode.SelectSingleNode("BlinkingView"), game, spriteMapping, palette, newFolder);
            }

            if (spriteMapping.Count > 0)
            {
                game.RootSpriteFolder.SubFolders.Add(newFolder);
                game.RootSpriteFolder.NotifyClientsOfUpdate();
            }
            game.NotifyClientsViewsUpdated();
            return newChar;
        }

        private static void EnsureCharacterScriptNameIsUnique(Character character, Game game)
        {
            string scriptNameBase = character.ScriptName;
            int suffix = 0;
            while (game.IsScriptNameAlreadyUsed(character.ScriptName, character))
            {
                suffix++;
                character.ScriptName = scriptNameBase + suffix;
            }
        }

        /// <summary>
        /// Import a 2.72-compatible CHA file. This code is horrid, but it's
        /// backwards compatible!!
        /// </summary>
        public static Character ImportCharacter272(string fileName, Game game)
        {
            BinaryReader reader = new BinaryReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            string fileSig = Encoding.ASCII.GetString(reader.ReadBytes(12));
            if (fileSig != CHARACTER_FILE_SIGNATURE)
            {
                reader.Close();
                throw new AGS.Types.InvalidDataException("This is not a valid AGS character file.");
            }
            int fileVersion = reader.ReadInt32();
            if ((fileVersion < 5) || (fileVersion > 6))
            {
                reader.Close();
                throw new AGS.Types.InvalidDataException("This character file is not supported by this version of AGS.");
            }

            Color []palette = new Color[256];
            for (int i = 0; i < 256; i++)
            {
                int r = reader.ReadByte();
                int g = reader.ReadByte();
                int b = reader.ReadByte();
                reader.ReadByte();
                palette[i] = Color.FromArgb(r * 4, g * 4, b * 4);
            }

            Character character = new Character();
            reader.ReadInt32();
            character.SpeechView = reader.ReadInt32();
            reader.ReadInt32();
            character.StartingRoom = reader.ReadInt32();
            reader.ReadInt32();
            character.StartX = reader.ReadInt32();
            character.StartY = reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            character.IdleView = reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            reader.ReadInt32();
            character.SpeechColor = reader.ReadInt32();
            character.ThinkingView = reader.ReadInt32();
            character.BlinkingView = reader.ReadInt16();
            reader.ReadInt16();
            reader.ReadBytes(40);
            character.MovementSpeed = reader.ReadInt16();
            character.AnimationDelay = reader.ReadInt16();
            reader.ReadBytes(606);
			character.RealName = ReadNullTerminatedString(reader, 40);
			character.ScriptName = ReadNullTerminatedString(reader, 20);
            if (character.ScriptName.Length > 0)
            {
                character.ScriptName = "c" + Char.ToUpperInvariant(character.ScriptName[0]) + character.ScriptName.Substring(1).ToLowerInvariant();

                EnsureCharacterScriptNameIsUnique(character, game);
            }
            reader.ReadInt16();

            string viewNamePrefix = character.ScriptName;
            if (viewNamePrefix.StartsWith("c")) viewNamePrefix = viewNamePrefix.Substring(1);

            SpriteFolder folder = new SpriteFolder(character.ScriptName + "Sprites");
            character.NormalView = ReadAndAddView(viewNamePrefix + "Walk", reader, game, folder, palette);
            if (character.SpeechView > 0)
            {
                character.SpeechView = ReadAndAddView(viewNamePrefix + "Talk", reader, game, folder, palette);
            }
            else
            {
                character.SpeechView = 0;
            }

            if (character.IdleView > 0)
            {
                character.IdleView = ReadAndAddView(viewNamePrefix + "Idle", reader, game, folder, palette);
            }
            else
            {
                character.IdleView = 0;
            }

            if ((character.ThinkingView > 0) && (fileVersion >= 6))
            {
                character.ThinkingView = ReadAndAddView(viewNamePrefix + "Think", reader, game, folder, palette);
            }
            else
            {
                character.ThinkingView = 0;
            }

            if ((character.BlinkingView > 0) && (fileVersion >= 6))
            {
                character.BlinkingView = ReadAndAddView(viewNamePrefix + "Blink", reader, game, folder, palette);
            }
            else
            {
                character.BlinkingView = 0;
            }

            reader.Close();

            if (folder.Sprites.Count > 0 || folder.SubFolders.Count > 0)
            {
                game.RootSpriteFolder.SubFolders.Add(folder);
                game.RootSpriteFolder.NotifyClientsOfUpdate();
            }
            game.NotifyClientsViewsUpdated();

            return character;
        }

        private static void WriteNewStyleView(XmlTextWriter writer, View view, Dictionary<int, object> spritesWritten)
        {
            view.ToXml(writer);

            foreach (ViewLoop loop in view.Loops)
            {
                foreach (ViewFrame frame in loop.Frames)
                {
                    if (!spritesWritten.ContainsKey(frame.Image))
                    {
                        WriteSpriteToXML(frame.Image, writer);
                        spritesWritten.Add(frame.Image, null);
                    }
                }
            }
        }

        private static void WriteOldStyleView(BinaryWriter writer, View view)
        {
            if (view.Loops.Count > 16)
            {
                throw new AGS.Types.InvalidDataException("This view has more than 16 loops and cannot be exported in a format compatible with previous AGS versions.");
            }
            foreach (ViewLoop loop in view.Loops)
            {
                if (loop.Frames.Count > 20)
                {
                    throw new AGS.Types.InvalidDataException("This view has a loop with more than 20 frames and cannot be exported in a format compatible with previous AGS versions.");
                }
            }

			short loopCount = Math.Min((short)view.Loops.Count, (short)16);
            writer.Write(loopCount);
            foreach (ViewLoop loop in view.Loops)
            {
				short frameCount = (short)loop.Frames.Count;
				if (loop.RunNextLoop)
				{
					frameCount++;
				}
				frameCount = Math.Min(frameCount, (short)20);
                writer.Write(frameCount);
            }
            WriteZeros(writer, (16 - view.Loops.Count) * 2);
            WriteZeros(writer, 16 * 4 + 2);

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if ((i < view.Loops.Count) && (j < view.Loops[i].Frames.Count))
                    {
                        ViewFrame frame = view.Loops[i].Frames[j];
                        writer.Write(frame.Image);
                        writer.Write((int)0);
                        writer.Write((short)frame.Delay);
                        WriteZeros(writer, 2);
                        writer.Write((frame.Flipped) ? 1 : 0);
                        writer.Write(frame.Sound);
                        WriteZeros(writer, 8);
                    }
					else if ((i < view.Loops.Count) &&
							 (j == view.Loops[i].Frames.Count) &&
							 (view.Loops[i].RunNextLoop))
					{
						// Write the "Run Next Loop" flag
						writer.Write((int)-1);
						writer.Write((int)0);
						writer.Write((short)0);
						WriteZeros(writer, 2);
						writer.Write(0);
						writer.Write(0);
						WriteZeros(writer, 8);
					}
					else
					{
						WriteZeros(writer, 28);
					}
                }
            }

            foreach (ViewLoop loop in view.Loops)
            {
                foreach (ViewFrame frame in loop.Frames)
                {
                    WriteOldStyleViewFrame(writer, frame);
                }

				if (loop.RunNextLoop)
				{
					writer.Write((int)200);
				}
            }
        }

        private static int ReadAndAddView(string viewName, BinaryReader reader, Game game, SpriteFolder folder, Color[] palette)
        {
            View newView = ReadOldStyleView(reader, game, folder, palette);
            newView.ID = game.FindAndAllocateAvailableViewID();
            newView.Name = viewName;
            game.RootViewFolder.Views.Add(newView);
            return newView.ID;
        }

        private static int ReadAndAddNewStyleView(XmlNode parentOfViewNode, Game game, Dictionary<int, int> spriteMapping, PaletteEntry[] palette, SpriteFolder newFolder)
        {
            View newView = new View(parentOfViewNode.SelectSingleNode("View"));
            newView.ID = game.FindAndAllocateAvailableViewID();
            ImportSpritesFromXML(parentOfViewNode, palette, newFolder, spriteMapping);
            UpdateViewWithSpriteMapping(newView, spriteMapping);
            EnsureViewNameIsUnique(newView, game);
            game.RootViewFolder.Views.Add(newView);
            return newView.ID;
        }

        private static void EnsureViewNameIsUnique(View view, Game game)
        {
            string scriptNameBase = view.Name; 
            int suffix = 0;
            while (game.IsScriptNameAlreadyUsed(view.Name.ToUpperInvariant(), view))
            {
                suffix++;
                view.Name = scriptNameBase + suffix;
            }
        }

        private static void UpdateViewWithSpriteMapping(View view, Dictionary<int, int> spriteMapping)
        {
            foreach (ViewLoop loop in view.Loops)
            {
                foreach (ViewFrame frame in loop.Frames)
                {
                    if (spriteMapping.ContainsKey(frame.Image))
                    {
                        frame.Image = spriteMapping[frame.Image];
                    }
                }
            }
        }

        private static View ReadOldStyleView(BinaryReader reader, Game game, SpriteFolder folder, Color[] palette)
        {
            View view = new View();
            int i;
            int numLoops = reader.ReadInt16();
            int[] numFrames = new int[numLoops];
            for (i = 0; i < numLoops; i++)
            {
                view.AddNewLoop();
                numFrames[i] = reader.ReadInt16();
            }
            if (numLoops < 16)
            {
                reader.ReadBytes((16 - numLoops) * 2);
            }
            reader.ReadBytes(16 * 4 + 2);
            for (i = 0; i < 16; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    ViewFrame frame = new ViewFrame();
                    frame.ID = j;
                    frame.Image = reader.ReadInt32();
                    reader.ReadInt32();
                    frame.Delay = reader.ReadInt16();
                    reader.ReadBytes(2);
                    frame.Flipped = (reader.ReadInt32() == 1);
                    frame.Sound = reader.ReadInt32();
                    reader.ReadBytes(8);
                    if ((i < numLoops) && (j < numFrames[i]))
                    {
                        view.Loops[i].Frames.Add(frame);
                    }
                }
            }

            foreach (ViewLoop loop in view.Loops)
            {
				foreach (ViewFrame frame in loop.Frames)
				{
					Sprite newSprite = ReadOldStyleViewFrame(reader, loop, frame, palette);
					if (newSprite == null)
					{
						break;
					}
					folder.Sprites.Add(newSprite);
					frame.Image = newSprite.Number;
				}
            }
            return view;
        }

        private static void WriteOldStyleViewFrame(BinaryWriter writer, ViewFrame frame)
        {
            Bitmap bmp = Factory.NativeProxy.GetSpriteBitmap(frame.Image);
            int colDepth = GetColorDepthForPixelFormat(bmp.PixelFormat);
            writer.Write(colDepth);
            int spriteFlags = 0;
            // TODO: why we are not saving resolution flags?
            if (bmp.PixelFormat == PixelFormat.Format32bppArgb)
            {
                spriteFlags |= SPRITE_FLAG_ALPHA_CHANNEL;
            }
            writer.Write((byte)spriteFlags);
            writer.Write(bmp.Width);
            writer.Write(bmp.Height);

            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            int memoryAddress = bmpData.Scan0.ToInt32();
            for (int y = 0; y < bmp.Height; y++)
            {
                byte[] line = new byte[bmp.Width * ((colDepth + 1) / 8)];
                Marshal.Copy(new IntPtr(memoryAddress), line, 0, line.Length);
                writer.Write(line);
                memoryAddress += bmpData.Stride;
            }
            bmp.UnlockBits(bmpData);

            bmp.Dispose();
        }

        private static Sprite ReadOldStyleViewFrame(BinaryReader reader, ViewLoop loop, ViewFrame frame, Color[] palette)
        {
            int colDepth = reader.ReadInt32();
            if (colDepth == 200)
            {
                loop.RunNextLoop = true;
                loop.Frames.Remove(frame);
                return null;
            }
            int spriteFlags = reader.ReadByte();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            byte[] spriteData = reader.ReadBytes(width * height * ((colDepth + 1) / 8));

            Sprite newSprite = ImportSpriteFromRawData(colDepth, width, height, (spriteFlags & SPRITE_FLAG_ALPHA_CHANNEL) != 0, spriteData, palette);
            // Old style format did not contain a flag for "varied resolution" so we should assume it is always varied (and this does not matter much anyway)
            newSprite.Resolution = Utilities.FixupSpriteResolution(((spriteFlags & SPRITE_FLAG_HI_RES) != 0) ? SpriteImportResolution.HighRes : SpriteImportResolution.LowRes);
            return newSprite;
        }

        private static PixelFormat GetPixelFormatForColorDepth(int depth)
        {
            switch (depth) 
            {
                case 8:
                    return PixelFormat.Format8bppIndexed;
                case 15:
                    return PixelFormat.Format16bppRgb555;
                case 16:
                    return PixelFormat.Format16bppRgb565;
                case 24:
                    return PixelFormat.Format24bppRgb;
                case 32:
                    return PixelFormat.Format32bppArgb;
            }
            return PixelFormat.Undefined;
        }

        private static int GetColorDepthForPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.Format8bppIndexed:
                    return 8;
                case PixelFormat.Format16bppRgb555:
                    return 15;
                case PixelFormat.Format16bppRgb565:
                    return 16;
                case PixelFormat.Format24bppRgb:
                    return 24;
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppRgb:
                    return 32;
            }
            throw new AGSEditorException("Invalid pixel format: " + format.ToString());
        }

        public static GUI ImportGUIFromFile(string fileName, Game game)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fileName);

            if (doc.DocumentElement.Name != GUI_XML_ROOT_NODE)
            {
                throw new AGS.Types.InvalidDataException("Not a valid AGS GUI file.");
            }
            string xml_version = SerializeUtils.GetAttributeString(doc.DocumentElement, GUI_XML_VERSION_ATTRIBUTE);
            int version_num;
            if (!Int32.TryParse(xml_version, out version_num) || version_num > GUI_XML_CURRENT_VERSION)
            {
                throw new AGS.Types.InvalidDataException("This file requires a newer version of AGS to import it.");
            }

            // First import the GUI itself
            GUI newGui;
            if (doc.DocumentElement.FirstChild.FirstChild.Name == NormalGUI.XML_ELEMENT_NAME)
            {
                newGui = new NormalGUI(doc.DocumentElement.FirstChild);
            }
            else
            {
                newGui = new TextWindowGUI(doc.DocumentElement.FirstChild);
            }

            AdjustScriptNamesToEnsureEverythingIsUnique(newGui, game);

            // Now load any sprites it contains
            PaletteEntry[] palette = game.ReadPaletteFromXML(doc.DocumentElement);
            SpriteFolder newFolder = new SpriteFolder(newGui.Name + "Import");
            Dictionary<int, int> spriteMapping = ImportSpritesFromXML(doc.DocumentElement.SelectSingleNode(GUI_XML_SPRITES_NODE), palette, newFolder);

            // If sprites are present then update sprite references in GUI and controls
            if (newGui.BackgroundImage > 0)
            {
                newGui.BackgroundImage = spriteMapping[newGui.BackgroundImage];
            }

            if (newGui.BackgroundImage < 0)
                newGui.BackgroundImage = 0;

            foreach (GUIControl control in newGui.Controls)
            {
                control.UpdateSpritesWithMapping(spriteMapping);
            }

            // Finally add new sprite folder to the project root
            if (spriteMapping.Count > 0)
            {
                game.RootSpriteFolder.SubFolders.Add(newFolder);
                game.RootSpriteFolder.NotifyClientsOfUpdate();
            }
            return newGui;
        }

        private static void AdjustScriptNamesToEnsureEverythingIsUnique(GUI gui, Game game)
        {
            while (game.IsScriptNameAlreadyUsed(gui.Name, gui))
            {
                gui.Name += "2";
            }
            foreach (GUIControl control in gui.Controls)
            {
                while (game.IsScriptNameAlreadyUsed(control.Name, control))
                {
                    control.Name += "2";
                }
            }
        }

        public static void ExportGUIToFile(GUI gui, string fileName, Game game)
        {
            Utilities.TryDeleteFile(fileName);
            XmlTextWriter writer = new XmlTextWriter(fileName, game.TextEncoding);
			writer.WriteProcessingInstruction("xml", "version=\"1.0\" encoding=\"" + game.TextEncoding.WebName + "\"");
			writer.WriteComment("AGS Exported GUI file. DO NOT EDIT THIS FILE BY HAND, IT IS GENERATED AUTOMATICALLY BY THE AGS EDITOR.");
            writer.WriteStartElement(GUI_XML_ROOT_NODE);
            writer.WriteAttributeString(GUI_XML_VERSION_ATTRIBUTE, GUI_XML_CURRENT_VERSION.ToString());

            gui.ToXml(writer);

            writer.WriteStartElement(GUI_XML_SPRITES_NODE);
            ExportAllSpritesOnGUI(gui, writer);
            writer.WriteEndElement();

            game.WritePaletteToXML(writer);

            writer.WriteEndElement();
            writer.Close();
        }

        private static void ExportAllSpritesOnGUI(GUI gui, XmlTextWriter writer)
        {
            WriteSpriteToXML(gui.BackgroundImage, writer);

            Dictionary<int, object> spritesWritten = new Dictionary<int, object>();
            foreach (GUIControl control in gui.Controls)
            {
                foreach (int spriteNumber in control.GetSpritesUsed())
                {
                    if (!spritesWritten.ContainsKey(spriteNumber))
                    {
                        WriteSpriteToXML(spriteNumber, writer);
                        spritesWritten.Add(spriteNumber, null);
                    }
                }
            }
            
        }

        private static void WriteSpriteToXML(int spriteNumber, XmlTextWriter writer)
        {
            if (spriteNumber > 0)
            {
                Sprite sprite = Factory.AGSEditor.CurrentGame.RootSpriteFolder.FindSpriteByID(spriteNumber, true);
                if (sprite == null)
                {
                    throw new AGSEditorException("Sprite not found: " + spriteNumber);
                }

                writer.WriteStartElement(GUI_XML_SPRITE_NODE);
                writer.WriteAttributeString(GUI_XML_SPRITE_NUMBER, spriteNumber.ToString());

                Bitmap bmp = Factory.NativeProxy.GetSpriteBitmap(spriteNumber);
                int colDepth = GetColorDepthForPixelFormat(bmp.PixelFormat);
                writer.WriteAttributeString(GUI_XML_SPRITE_COLOR_DEPTH, colDepth.ToString());
                writer.WriteAttributeString(GUI_XML_SPRITE_ALPHA_CHANNEL, (bmp.PixelFormat == PixelFormat.Format32bppArgb).ToString());
                writer.WriteAttributeString(GUI_XML_SPRITE_WIDTH, bmp.Width.ToString());
                writer.WriteAttributeString(GUI_XML_SPRITE_HEIGHT, bmp.Height.ToString());
                writer.WriteAttributeString(GUI_XML_SPRITE_RESOLUTION, sprite.Resolution.ToString());

                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                int memoryAddress = bmpData.Scan0.ToInt32();
                for (int y = 0; y < bmp.Height; y++)
                {
                    byte[] line = new byte[bmp.Width * ((colDepth + 1) / 8)];
                    Marshal.Copy(new IntPtr(memoryAddress), line, 0, line.Length);
                    writer.WriteBase64(line, 0, line.Length);
                    memoryAddress += bmpData.Stride;
                }
                bmp.UnlockBits(bmpData);

                bmp.Dispose();

                writer.WriteEndElement();
            }
        }

        private static Sprite ImportSpriteFromRawData(int colDepth, int width, int height, bool hasAlpha, byte[] spriteData, Color[] palette)
        {
            Bitmap bmp = new Bitmap(width, height, GetPixelFormatForColorDepth(colDepth));
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bmp.PixelFormat);
            int memoryAddress = bmpData.Scan0.ToInt32();
            for (int y = 0; y < height; y++)
            {
                int lineSpan = width * ((colDepth + 1) / 8);
                Marshal.Copy(spriteData, lineSpan * y, new IntPtr(memoryAddress), lineSpan);
                memoryAddress += bmpData.Stride;
            }
            bmp.UnlockBits(bmpData);

            if (bmp.IsIndexed())
            {
                ColorPalette pal = bmp.Palette;
                for (int i = 0; i < palette.Length; i++)
                {
                    pal.Entries[i] = palette[i];
                }
                // The palette needs to be re-set onto the bitmap to force it
                // to update its internal storage of the colours
                bmp.Palette = pal;
            }

            Sprite newSprite = Factory.NativeProxy.CreateSpriteFromBitmap(bmp, SpriteImportTransparency.LeaveAsIs, 0, true, false, hasAlpha);
            bmp.Dispose();
            return newSprite;
        }

        private static Dictionary<int, int> ImportSpritesFromXML(XmlNode spritesNode, PaletteEntry[] paletteEntries, SpriteFolder folderToAddTo)
        {
            Dictionary<int, int> spriteNumberMapping = new Dictionary<int, int>();
            ImportSpritesFromXML(spritesNode, paletteEntries, folderToAddTo, spriteNumberMapping);
            return spriteNumberMapping;
        }

        private static void ImportSpritesFromXML(XmlNode spritesNode, PaletteEntry[] paletteEntries, SpriteFolder folderToAddTo, Dictionary<int, int> spriteNumberMapping)
        {
            Color[] palette = new Color[paletteEntries.Length];
            for (int i = 0; i < paletteEntries.Length; i++)
            {
                palette[i] = paletteEntries[i].Colour;
            }

            List<Sprite> newSprites = new List<Sprite>();

            foreach (XmlNode childNode in spritesNode.ChildNodes)
            {
                if (childNode.Name == GUI_XML_SPRITE_NODE)
                {
                    int spriteNumber = SerializeUtils.GetAttributeInt(childNode, GUI_XML_SPRITE_NUMBER);
                    int colDepth = SerializeUtils.GetAttributeInt(childNode, GUI_XML_SPRITE_COLOR_DEPTH);
                    bool hasAlphaChannel = bool.Parse(SerializeUtils.GetAttributeString(childNode, GUI_XML_SPRITE_ALPHA_CHANNEL));
                    int width = SerializeUtils.GetAttributeInt(childNode, GUI_XML_SPRITE_WIDTH);
                    int height = SerializeUtils.GetAttributeInt(childNode, GUI_XML_SPRITE_HEIGHT);
                    SpriteImportResolution resolution = (SpriteImportResolution)Enum.Parse(typeof(SpriteImportResolution), SerializeUtils.GetAttributeString(childNode, GUI_XML_SPRITE_RESOLUTION));
                    byte[]spriteData = Convert.FromBase64String(childNode.InnerText);

                    Sprite newSprite = ImportSpriteFromRawData(colDepth, width, height, hasAlphaChannel, spriteData, palette);
                    newSprite.Resolution = Utilities.FixupSpriteResolution(resolution);

                    newSprites.Add(newSprite);
                    spriteNumberMapping.Add(spriteNumber, newSprite.Number);
                    folderToAddTo.Sprites.Add(newSprite);
                }
            }

            Factory.NativeProxy.SpriteResolutionsChanged(newSprites.ToArray());
        }
    }
}
