﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AGS.Editor
{
	class CommandLineOptions
	{
		private bool _compileAndExit;
		private string _projectPath;

		private void Parse(string[] args)
		{
			_compileAndExit = false;

			foreach (string arg in args)
			{
				if (arg.ToLower() == "/compile")
				{
					_compileAndExit = true;
					StdConsoleWriter.Enable();
				}
				else if (arg.StartsWith("/") || arg.StartsWith("-"))
				{
					Factory.GUIController.ShowMessage("Invalid command line argument " + arg, MessageBoxIcon.Warning);
				}
				else
				{
					if (!File.Exists(arg))
					{
						_compileAndExit = false;
						Factory.GUIController.ShowMessage("Unable to load the game '" + arg + "' because it does not exist", MessageBoxIcon.Warning);
					}
					else
					{
						_projectPath = arg;
					}
				}
			}
			if (string.IsNullOrEmpty(_projectPath)) _compileAndExit = false;
		}

		public CommandLineOptions(string[] args)
		{
			Parse(args);
		}

		public bool CompileAndExit 
		{
			get { return _compileAndExit; }
		}

		public string ProjectPath
		{
			get { return _projectPath; }
		}
	}
}
