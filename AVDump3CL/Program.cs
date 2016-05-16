﻿using AVDump3Lib.Information;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing;
using AVDump3Lib.Reporting;
using AVDump3Lib.Settings;
using AVDump3Lib.Settings.CLArguments;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3CL {
	class Program {

		static void Main(string[] args) {
			if(args.Length == 0) {
				args = new string[] {
					"--Conc=2:G:/,2",
					"--BSize=8:8",
                    //"--Consumers=CRC32, ED2K, MD4, MD5, SHA1, SHA384, SHA512, TTH, TIGER",
                    "--Consumers=MD5, SHA1, MD4, TIGER",
                    //@"G:\Software\en_visual_studio_enterprise_2015_with_update_2_x86_x64_dvd_8510142.iso",
                    "G:/Video",
				};
			}
			var moduleManagemant = IniModules();
			var pathsToProcess = ProcessCommandlineArguments(moduleManagemant.GetModule<AVD3SettingsModule>(), args);

			var clModule = moduleManagemant.GetModule<AVD3CLModule>();
			clModule.Process(pathsToProcess);
		}
		private static AVD3ModuleManagement IniModules() {
			var moduleManagament = new AVD3ModuleManagement();
			moduleManagament.LoadModules(AppDomain.CurrentDomain.BaseDirectory);
			moduleManagament.LoadModuleFromType(typeof(AVD3InformationModule));
			moduleManagament.LoadModuleFromType(typeof(AVD3ProcessingModule));
			moduleManagament.LoadModuleFromType(typeof(AVD3ReportingModule));
			moduleManagament.LoadModuleFromType(typeof(AVD3SettingsModule));
			moduleManagament.LoadModuleFromType(typeof(AVD3CLModule));
			moduleManagament.InitializeModules();
			return moduleManagament;
		}

		private static string[] ProcessCommandlineArguments(AVD3SettingsModule settingsModule, string[] arguments) {
			var unnamedArgs = new List<string>();
			var clManagement = new CLManagement();
			clManagement.SetUnnamedParamHandler(arg => unnamedArgs.Add(arg));

			var argGroups = settingsModule.RaiseCommandlineRegistration().ToArray();
			clManagement.RegisterArgGroups(argGroups);

			if(!clManagement.ParseArgs(arguments)) return null;

			return unnamedArgs.ToArray();
		}


	}
}
