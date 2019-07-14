using System;
using System.Collections.Generic;
using System.IO;
using AGS.CScript.Compiler;
using AGS.Types;

namespace AGS.Editor 
{
    public class AGSCompiler
    {
        public delegate void PreCompileGameHandler(PreCompileGameEventArgs evArgs);
        public event PreCompileGameHandler PreCompileGame;
        public delegate void ExtraOutputCreationStepHandler(bool miniExeForDebug);
        public event ExtraOutputCreationStepHandler ExtraOutputCreationStep;
        public delegate void ExtraCompilationStepHandler(CompileMessages errors);
        public event ExtraCompilationStepHandler ExtraCompilationStep;

        private AGSEditor _agsEditor;
        private Game _game;

        private static readonly string[] _scriptAPIVersionMacros;
        private static readonly string[] _scriptCompatLevelMacros;

        public AGSCompiler(AGSEditor agsEditor, Game game)
        {
            _agsEditor = agsEditor;
            _game = game;
        }

        static AGSCompiler()
        {
            _scriptAPIVersionMacros = new string[Enum.GetNames(typeof(ScriptAPIVersion)).Length];
            foreach (ScriptAPIVersion v in Enum.GetValues(typeof(ScriptAPIVersion)))
            {
                if (v == ScriptAPIVersion.Highest)
                    continue; // don't enlist "Highest" constant
                _scriptAPIVersionMacros[(int)v] = "SCRIPT_API_" + v.ToString();
            }
            _scriptCompatLevelMacros = new string[Enum.GetNames(typeof(ScriptAPIVersion)).Length];
            foreach (ScriptAPIVersion v in Enum.GetValues(typeof(ScriptAPIVersion)))
            {
                if (v == ScriptAPIVersion.Highest)
                    continue; // don't enlist "Highest" constant
                _scriptCompatLevelMacros[(int)v] = "SCRIPT_COMPAT_" + v.ToString();
            }
        }

        public CompileMessages CompileGame(bool forceRebuild, bool createMiniExeForDebug)
        {
            CompileMessages compileMessages = new CompileMessages();

            Utilities.EnsureStandardSubFoldersExist();

            if (PreCompileGame != null)
            {
                PreCompileGameEventArgs evArgs = new PreCompileGameEventArgs(forceRebuild);
                evArgs.Errors = compileMessages;

                PreCompileGame(evArgs);

                if (!evArgs.AllowCompilation)
                {
                    return compileMessages;
                }
            }

            RunPreCompilationChecks(compileMessages);

            if (!compileMessages.HasErrors)
            {
                CompileMessage result = (CompileMessage)BusyDialog.Show("Please wait while your scripts are compiled...", new BusyDialog.ProcessingHandler(CompileScripts), new CompileScriptsParameters(compileMessages, forceRebuild));
                if (result != null)
                {
                    compileMessages.Add(result);
                }
                else if (!compileMessages.HasErrors)
                {
                    if (createMiniExeForDebug)
                    {
                        CreateMiniEXEForDebugging(compileMessages);
                    }
                    else
                    {
                        CreateCompiledFiles(compileMessages, forceRebuild);
                    }
                }
            }

            return compileMessages;
        }

        private void RunPreCompilationChecks(CompileMessages errors)
        {
            if ((_game.LipSync.Type == LipSyncType.PamelaVoiceFiles) &&
                (_game.Settings.SpeechStyle == SpeechStyle.Lucasarts))
            {
                errors.Add(new CompileError("Voice lip-sync cannot be used with Lucasarts-style speech"));
            }

            if ((_game.Settings.EnhancedSaveGames) &&
                (_game.Settings.SaveGameFileExtension == string.Empty))
            {
                errors.Add(new CompileError("Enhanced Save Games are enabled but no file extension is specified"));
            }

            if (_game.PlayerCharacter == null)
            {
                errors.Add(new CompileError("No character has been set as the player character"));
            }
            else if (_game.FindRoomByID(_game.PlayerCharacter.StartingRoom) == null)
            {
                errors.Add(new CompileError("The game is set to start in room " + _game.PlayerCharacter.StartingRoom + " which does not exist"));
            }

            if (_game.Settings.ColorDepth == GameColorDepth.Palette)
            {
                if (_game.DefaultSetup.GraphicsDriver == GraphicsDriver.D3D9)
                    errors.Add(new CompileError("Direct3D graphics driver does not support 256-colour games"));
                else if (_game.DefaultSetup.GraphicsDriver == GraphicsDriver.OpenGL)
                    errors.Add(new CompileError("OpenGL graphics driver does not support 256-colour games"));
            }

            if ((_game.Settings.ColorDepth == GameColorDepth.Palette) &&
                (_game.Settings.RoomTransition == RoomTransitionStyle.CrossFade))
            {
                errors.Add(new CompileError("You cannot use the CrossFade room transition with 256-colour games"));
            }

            if ((_game.Settings.DialogOptionsGUI < 0) ||
                (_game.Settings.DialogOptionsGUI >= _game.RootGUIFolder.GetAllItemsCount()))
            {
                if (_game.Settings.DialogOptionsGUI != 0)
                {
                    errors.Add(new CompileError("Invalid GUI number set for Dialog Options GUI"));
                }
            }

            foreach (Character character in _game.RootCharacterFolder.AllItemsFlat)
            {
                View view = _game.FindViewByID(character.NormalView);
                if (view == null)
                {
                    errors.Add(new CompileError("Character " + character.ID + " (" + character.RealName + ") has invalid normal view."));
                }
                else
                {
                    EnsureViewHasAtLeast4LoopsAndAFrameInLeftRightLoops(view);
                }
            }

            Dictionary<string, View> viewNames = new Dictionary<string, View>();
            EnsureViewNamesAreUnique(_game.RootViewFolder, viewNames, errors);

            foreach (AudioClip clip in _game.RootAudioClipFolder.GetAllAudioClipsFromAllSubFolders())
            {
                if (!File.Exists(clip.CacheFileName))
                {
                    errors.Add(new CompileError("Audio file missing for " + clip.ScriptName + ": " + clip.CacheFileName));
                }
            }
        }

        private void EnsureViewHasAtLeast4LoopsAndAFrameInLeftRightLoops(View view)
        {
            bool viewModified = false;
            while (view.Loops.Count < 4)
            {
                view.Loops.Add(new ViewLoop(view.Loops.Count));
                viewModified = true;
            }

            if (view.Loops[1].Frames.Count < 1)
            {
                view.Loops[1].Frames.Add(new ViewFrame(0));
            }
            if (view.Loops[2].Frames.Count < 1)
            {
                view.Loops[2].Frames.Add(new ViewFrame(0));
            }
            if (viewModified)
            {
                view.NotifyClientsOfUpdate();
            }
        }

        private void EnsureViewNamesAreUnique(ViewFolder folder, Dictionary<string, View> viewNames, CompileMessages errors)
        {
            foreach (ViewFolder subFolder in folder.SubFolders)
            {
                EnsureViewNamesAreUnique(subFolder, viewNames, errors);
            }

            foreach (View view in folder.Views)
            {
                if (!string.IsNullOrEmpty(view.Name))
                {
                    if (viewNames.ContainsKey(view.Name.ToLower()))
                    {
                        errors.Add(new CompileError("There are two or more views with the same name '" + view.Name + "'"));
                    }
                    else
                    {
                        viewNames.Add(view.Name.ToLower(), view);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a mini-exe that only contains the GAME.DTA file,
        /// in order to improve compile speed.
        /// All other files will be sourced from the game folder.
        /// </summary>
        private void CreateMiniEXEForDebugging(CompileMessages errors)
        {
            IBuildTarget target = BuildTargetsInfo.FindBuildTargetByName(BuildTargetDebug.DEBUG_TARGET_NAME);

            var buildNames = Factory.AGSEditor.CurrentGame.WorkspaceState.GetLastBuildGameFiles();
            string oldName;
            if (buildNames.TryGetValue(target.Name, out oldName))
            {
                if (!string.IsNullOrWhiteSpace(oldName) && oldName != Factory.AGSEditor.BaseGameFileName)
                    target.DeleteMainGameData(oldName);
            }

            target.Build(errors, false);
            if (ExtraOutputCreationStep != null)
            {
                ExtraOutputCreationStep(true);
            }

            buildNames[target.Name] = Factory.AGSEditor.BaseGameFileName;
            Factory.AGSEditor.CurrentGame.WorkspaceState.SetLastBuildGameFiles(buildNames);
        }

        private void CreateCompiledFiles(CompileMessages errors, bool forceRebuild)
        {
            try
            {
                BusyDialog.Show("Please wait while your game is created...", new BusyDialog.ProcessingHandler(CreateCompiledFiles), new CompileScriptsParameters(errors, forceRebuild));
            }
            catch (Exception ex)
            {
                errors.Add(new CompileError("Unexpected error: " + ex.Message));
            }
        }

        private object CreateCompiledFiles(object parameter)
        {
            CompileScriptsParameters parameters = (CompileScriptsParameters)parameter;
            CompileMessages errors = parameters.Errors;
            bool forceRebuild = parameters.RebuildAll;

            // TODO: This is also awkward, we call Cleanup for active targets to make sure
            // that in case they changed the game binary name an old one gets removed.
            // Also please see the comment about build steps below.
            var buildNames = Factory.AGSEditor.CurrentGame.WorkspaceState.GetLastBuildGameFiles();
            foreach (IBuildTarget target in BuildTargetsInfo.GetSelectedBuildTargets())
            {
                string oldName;
                if (!buildNames.TryGetValue(target.Name, out oldName)) continue;
                if (!string.IsNullOrWhiteSpace(oldName) && oldName != Factory.AGSEditor.BaseGameFileName)
                    target.DeleteMainGameData(oldName);
            }

            IBuildTarget targetDataFile = BuildTargetsInfo.FindBuildTargetByName(BuildTargetsInfo.DATAFILE_TARGET_NAME);
            targetDataFile.Build(errors, forceRebuild); // ensure that data file is built first
            if (ExtraOutputCreationStep != null)
            {
                ExtraOutputCreationStep(false);
            }

            // TODO: As of now the build targets other than DataFile and Debug do DEPLOYMENT rather than BUILDING
            // (BuildTargetDebug, - which is never used right here, - seem to combine both operations:
            // building and preparing game to run under Windows).
            // This is why the BuildTargetDataFile is called explicitly at the start.
            // And that is why the rest must be called AFTER the ExtraOutputCreationStep.
            //
            // Possible solution that could improve situation could be to develop some kind of a BuildStep interface,
            // having BuildTargets providing their build steps of corresponding type and execution order.
            foreach (IBuildTarget target in BuildTargetsInfo.GetSelectedBuildTargets())
            {
                if (target != targetDataFile) target.Build(errors, forceRebuild);
                buildNames[target.Name] = Factory.AGSEditor.BaseGameFileName;
            }
            Factory.AGSEditor.CurrentGame.WorkspaceState.SetLastBuildGameFiles(buildNames);
            return null;
        }

        private object CompileScripts(object parameter)
        {
            CompileScriptsParameters parameters = (CompileScriptsParameters)parameter;
            CompileMessages errors = parameters.Errors;
            CompileMessage errorToReturn = null;
            _agsEditor.RegenerateScriptHeader(null);
            List<Script> headers = _agsEditor.GetInternalScriptHeaders();

            try
            {
                Script dialogScripts = CompileDialogs(errors, parameters.RebuildAll);

                _game.ScriptsToCompile = new ScriptsAndHeaders();

                foreach (Script script in _agsEditor.GetInternalScriptModules())
                {
                    CompileScript(script, headers, errors, false);
                    _game.ScriptsToCompile.Add(new ScriptAndHeader(null, script));
                }

                foreach (ScriptAndHeader scripts in _game.RootScriptFolder.AllItemsFlat)
                {
                    headers.Add(scripts.Header);
                    CompileScript(scripts.Script, headers, errors, false);
                    _game.ScriptsToCompile.Add(scripts);
                }

                CompileScript(dialogScripts, headers, errors, false);
                _game.ScriptsToCompile.Add(new ScriptAndHeader(null, dialogScripts));
            }
            catch (CompileMessage ex)
            {
                errorToReturn = ex;
            }

            if (ExtraCompilationStep != null)
            {
                ExtraCompilationStep(errors);
            }

            return errorToReturn;
        }

        private Script CompileDialogs(CompileMessages errors, bool rebuildAll)
        {
            DialogScriptConverter dialogConverter = new DialogScriptConverter();
            string dialogScriptsText = dialogConverter.ConvertGameDialogScripts(_game, errors, rebuildAll);
            Script dialogScripts = new Script(Script.DIALOG_SCRIPTS_FILE_NAME, dialogScriptsText, false);
            Script globalScript = _game.RootScriptFolder.GetScriptByFileName(Script.GLOBAL_SCRIPT_FILE_NAME, true);
            if (!System.Text.RegularExpressions.Regex.IsMatch(globalScript.Text, @"function\s+dialog_request\s*\("))
            {
                // A dialog_request must exist in the global script, otherwise
                // the dialogs script fails to load at run-time
                globalScript.Text += Environment.NewLine + "function dialog_request(int param) {" + Environment.NewLine + "}";
            }
            return dialogScripts;
        }


        /// <summary>
		/// Preprocesses and then compiles the script using the supplied headers.
		/// </summary>
		public void CompileScript(Script script, List<Script> headers, CompileMessages errors, bool isRoomScript)
        {
            IPreprocessor preprocessor = CompilerFactory.CreatePreprocessor(Types.Version.AGS_EDITOR_VERSION);
            DefineMacrosAccordingToGameSettings(preprocessor);

            List<string> preProcessedCode = new List<string>();
            foreach (Script header in headers)
            {
                preProcessedCode.Add(preprocessor.Preprocess(header.Text, header.FileName));
            }

            preProcessedCode.Add(preprocessor.Preprocess(script.Text, script.FileName));

#if DEBUG
            // TODO: REMOVE BEFORE DISTRIBUTION
            /*			if (true)
                        {
                            string wholeScript = string.Join("\n", preProcessedCode.ToArray());
                            IScriptCompiler compiler = CompilerFactory.CreateScriptCompiler();
                            CompileResults output = compiler.CompileScript(wholeScript);
                            preprocessor.Results.AddRange(output);
                        }*/
#endif

            if (preprocessor.Results.Count > 0)
            {
                foreach (AGS.CScript.Compiler.Error error in preprocessor.Results)
                {
                    CompileError newError = new CompileError(error.Message, error.ScriptName, error.LineNumber);
                    if (errors == null)
                    {
                        throw newError;
                    }
                    errors.Add(newError);
                }
            }
            else
            {
                Factory.NativeProxy.CompileScript(script, preProcessedCode.ToArray(), _game, isRoomScript);
            }
        }

        private void DefineMacrosAccordingToGameSettings(IPreprocessor preprocessor)
        {
            preprocessor.DefineMacro("AGS_NEW_STRINGS", "1");
            preprocessor.DefineMacro("AGS_SUPPORTS_IFVER", "1");
            if (_game.Settings.DebugMode)
            {
                preprocessor.DefineMacro("DEBUG", "1");
            }
            if (_game.Settings.EnforceObjectBasedScript)
            {
                preprocessor.DefineMacro("STRICT", "1");
            }
            if (_game.Settings.LeftToRightPrecedence)
            {
                preprocessor.DefineMacro("LRPRECEDENCE", "1");
            }
            if (_game.Settings.EnforceNewStrings)
            {
                preprocessor.DefineMacro("STRICT_STRINGS", "1");
            }
            if (_game.Settings.EnforceNewAudio)
            {
                preprocessor.DefineMacro("STRICT_AUDIO", "1");
            }
            if (!_game.Settings.UseOldCustomDialogOptionsAPI)
            {
                preprocessor.DefineMacro("NEW_DIALOGOPTS_API", "1");
            }
            // Define Script API level macros
            foreach (ScriptAPIVersion v in Enum.GetValues(typeof(ScriptAPIVersion)))
            {
                if (v == ScriptAPIVersion.Highest)
                    continue; // skip Highest constant
                if (v > _game.Settings.ScriptAPIVersionReal)
                    continue;
                preprocessor.DefineMacro(_scriptAPIVersionMacros[(int)v], "1");
            }
            foreach (ScriptAPIVersion v in Enum.GetValues(typeof(ScriptAPIVersion)))
            {
                if (v == ScriptAPIVersion.Highest)
                    continue; // skip Highest constant
                if (v < _game.Settings.ScriptCompatLevelReal)
                    continue;
                preprocessor.DefineMacro(_scriptCompatLevelMacros[(int)v], "1");
            }
        }

        private class CompileScriptsParameters
        {
            internal CompileMessages Errors { get; set; }
            internal bool RebuildAll { get; set; }

            internal CompileScriptsParameters(CompileMessages errors, bool rebuildAll)
            {
                this.Errors = errors;
                this.RebuildAll = rebuildAll;
            }
        }
    }
}
