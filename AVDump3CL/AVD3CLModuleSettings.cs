﻿using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AVDump3CL {
	public class AVD3CLModuleSettings {
		public FileDiscoverySettings FileDiscovery { get; }
		public ProcessingSettings Processing { get; }
		public ReportingSettings Reporting { get; }
		public DisplaySettings Display { get; }
		public DiagnosticsSettings Diagnostics { get; }

		public bool UseNtfsAlternateStreams { get; set; }

		public AVD3CLModuleSettings() {
			FileDiscovery = new FileDiscoverySettings();
			Processing = new ProcessingSettings();
			Reporting = new ReportingSettings();
			Display = new DisplaySettings();
			Diagnostics = new DiagnosticsSettings();
		}
	}

	public class FileExtensionsSetting {
		public bool Allow { get; set; }
		public ReadOnlyCollection<string> Items { get; set; }

		public FileExtensionsSetting() {
			Allow = false;
			Items = Array.AsReadOnly(new string[0]);
		}
	}
	public class FileDiscoverySettings : SettingsObject, ICLConvert {
		[CLNames("R")]
		public SettingsProperty RecursiveProperty { get; }
		public bool Recursive {
			get => (bool)GetValue(RecursiveProperty);
			set => SetValue(RecursiveProperty, value);
		}

		[CLNames("PLPath")]
		public SettingsProperty ProcessedLogPathProperty { get; }
		public string ProcessedLogPath {
			get => (string)GetValue(ProcessedLogPathProperty);
			set => SetValue(ProcessedLogPathProperty, value);
		}

		[CLNames("SLPath")]
		public SettingsProperty SkipLogPathProperty { get; }
		public string SkipLogPath {
			get => (string)GetValue(SkipLogPathProperty);
			set => SetValue(SkipLogPathProperty, value);
		}

		[CLNames("DLPath")]
		public SettingsProperty DoneLogPathProperty { get; }
		public string DoneLogPath {
			get => (string)GetValue(DoneLogPathProperty);
			set => SetValue(DoneLogPathProperty, value);
		}

		[CLNames("Conc")]
		public SettingsProperty ConcurrentProperty { get; }
		public PathPartitions Concurrent {
			get => (PathPartitions)GetValue(ConcurrentProperty);
			set => SetValue(ConcurrentProperty, value);
		}

		[CLNames("WExts")]
		public SettingsProperty WithExtensionsProperty { get; }
		public FileExtensionsSetting WithExtensions {
			get => (FileExtensionsSetting)GetValue(WithExtensionsProperty);
			set => SetValue(WithExtensionsProperty, value);
		}

		public FileDiscoverySettings() {
			Name = "FileDiscovery";
			ResourceManager = Lang.ResourceManager;
			RecursiveProperty = Register(nameof(Recursive), false);
			ProcessedLogPathProperty = Register<string>(nameof(ProcessedLogPath), null);
			SkipLogPathProperty = Register<string>(nameof(SkipLogPath), null);
			DoneLogPathProperty = Register<string>(nameof(DoneLogPath), null);
			ConcurrentProperty = Register(nameof(Concurrent), new PathPartitions(1, new PathPartition[0]));
			WithExtensionsProperty = Register(nameof(WithExtensions), new FileExtensionsSetting() { Allow = true });
		}

		string ICLConvert.ToCLString(SettingsProperty property, object obj) {
			if(property == WithExtensionsProperty) {
				var value = (FileExtensionsSetting)obj;
				return (value.Allow ? "" : "-") + string.Join(",", value.Items);

			} else if(property == ConcurrentProperty) {
				var value = (PathPartitions)obj;
				return value.ConcurrentCount + (value.Partitions.Count > 0 ? ":" : "") + string.Join(",", value.Partitions.Select(x => x.Path + "," + x.ConcurrentCount));
			}

			return obj == null ? "<null>" : obj.ToString();
		}

		object ICLConvert.FromCLString(SettingsProperty property, string str) {
			if(property == WithExtensionsProperty) {
				var value = new FileExtensionsSetting { Allow = str.Length != 0 && str[0] != '-' };
				if(!value.Allow) str = str.Substring(1);
				value.Items = Array.AsReadOnly(str.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
				return value;

			} else if(property == ConcurrentProperty) {
				var raw = str.Split(new char[] { ':' }, 2);

				return new PathPartitions(
					int.Parse(raw[0]),
					from item in (raw.Length > 1 ? raw[1].Split(';') : new string[0])
					let parts = item.Split(',')
					select new PathPartition(parts[0], int.Parse(parts[1]))
				);
			} else if(property == DoneLogPathProperty) {
				SkipLogPath = str;
				ProcessedLogPath = str;
			}

			return Convert.ChangeType(str, property.ValueType);
		}
	}

	public class ProcessingSettings : SettingsObject, ICLConvert {
		[CLNames("BLength")]
		public SettingsProperty BufferLengthProperty { get; }
		public int BufferLength {
			get => (int)GetValue(BufferLengthProperty);
			set => SetValue(BufferLengthProperty, value);
		}

		public SettingsProperty ProducerMinReadLengthProperty { get; }
		public int ProducerMinReadLength {
			get => (int)GetValue(ProducerMinReadLengthProperty);
			set => SetValue(ProducerMinReadLengthProperty, value);
		}
		public SettingsProperty ProducerMaxReadLengthProperty { get; }
		public int ProducerMaxReadLength {
			get => (int)GetValue(ProducerMaxReadLengthProperty);
			set => SetValue(ProducerMaxReadLengthProperty, value);
		}


		[CLNames("PBExit")]
		public SettingsProperty PauseBeforeExitProperty { get; }
		public bool PauseBeforeExit {
			get => (bool)GetValue(PauseBeforeExitProperty);
			set => SetValue(PauseBeforeExitProperty, value);
		}

		[CLNames("Cons")]
		public SettingsProperty ConsumersProperty { get; }
		public IReadOnlyCollection<string> Consumers {
			get => (IReadOnlyCollection<string>)GetValue(ConsumersProperty);
			set => SetValue(ConsumersProperty, value);
		}

		public SettingsProperty PrintAvailableSIMDsProperty { get; }
		public bool PrintAvailableSIMDs {
			get => (bool)GetValue(PrintAvailableSIMDsProperty);
			set => SetValue(PrintAvailableSIMDsProperty, value);
		}

		public ProcessingSettings() {
			Name = "Processing";
			ResourceManager = Lang.ResourceManager;
			BufferLengthProperty = Register(nameof(BufferLength), 64 << 20);
			ProducerMinReadLengthProperty = Register(nameof(ProducerMinReadLength), 1 << 20);
			ProducerMaxReadLengthProperty = Register(nameof(ProducerMaxReadLength), 8 << 20);
			ConsumersProperty = Register(nameof(Consumers), Array.Empty<string>());
			PrintAvailableSIMDsProperty = Register(nameof(PrintAvailableSIMDs), false);
			PauseBeforeExitProperty = Register(nameof(PauseBeforeExit), false);
		}

		string ICLConvert.ToCLString(SettingsProperty property, object obj) {
			if(property == BufferLengthProperty || property == ProducerMinReadLengthProperty || property == ProducerMaxReadLengthProperty) {
				var value = (int)obj;
				return (value >> 20).ToString();

			} else if(property == ConsumersProperty) {
				var lst = (IReadOnlyCollection<string>)obj;
				//A bit odd at first, but with this we make Consumers==null the special case (i.e. list the consumers)
				return obj != null ? (lst.Count == 0 ? null : string.Join(",", lst)) : "";
			}
			return obj == null ? "<null>" : obj.ToString();
		}

		object ICLConvert.FromCLString(SettingsProperty property, string str) {
			if(property == BufferLengthProperty || property == ProducerMinReadLengthProperty || property == ProducerMaxReadLengthProperty) {
				return int.Parse(str) << 20;

			} else if(property == ConsumersProperty) {
				if(str != null && str.Length == 0) return null;
				//See ToCLString
				return Array.AsReadOnly((str ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray());
			}
			return Convert.ChangeType(str, property.ValueType);
		}
	}


	public class ReportingSettings : SettingsObject, ICLConvert {

		public SettingsProperty PrintHashesProperty { get; }
		public bool PrintHashes {
			get => (bool)GetValue(PrintHashesProperty);
			set => SetValue(PrintHashesProperty, value);
		}

		public SettingsProperty PrintReportsProperty { get; }
		public bool PrintReports {
			get => (bool)GetValue(PrintReportsProperty);
			set => SetValue(PrintReportsProperty, value);
		}

		public SettingsProperty ReportsProperty { get; }
		public ReadOnlyCollection<string> Reports {
			get => (ReadOnlyCollection<string>)GetValue(ReportsProperty);
			set => SetValue(ReportsProperty, value);
		}

		[CLNames("RDir")]
		public SettingsProperty ReportDirectoryProperty { get; }
		public string ReportDirectory {
			get => (string)GetValue(ReportDirectoryProperty);
			set => SetValue(ReportDirectoryProperty, value);
		}

		public SettingsProperty ReportFileNameProperty { get; }
		public string ReportFileName {
			get => (string)GetValue(ReportFileNameProperty);
			set => SetValue(ReportFileNameProperty, value);
		}

		[CLNames("EDPath")]
		public SettingsProperty ExtensionDifferencePathProperty { get; }
		public string ExtensionDifferencePath {
			get => (string)GetValue(ExtensionDifferencePathProperty);
			set => SetValue(ExtensionDifferencePathProperty, value);
		}

		public SettingsProperty CRC32ErrorProperty { get; }
		public (string Path, string Pattern) CRC32Error {
			get => ((string, string))GetValue(CRC32ErrorProperty);
			set => SetValue(CRC32ErrorProperty, value);
		}

		public ReportingSettings() {
			Name = "Reporting";
			ResourceManager = Lang.ResourceManager;

			PrintHashesProperty = Register(nameof(PrintHashes), false);
			PrintReportsProperty = Register(nameof(PrintReports), false);
			ReportsProperty = Register(nameof(Reports), Array.AsReadOnly(new string[0]));
			ReportDirectoryProperty = Register(nameof(ReportDirectory), Environment.CurrentDirectory);
			ReportFileNameProperty = Register(nameof(ReportFileName), "<FileName>.<ReportName>.<ReportFileExtension>");
			ExtensionDifferencePathProperty = Register(nameof(ExtensionDifferencePath), default(string));
			CRC32ErrorProperty = Register(nameof(CRC32Error), (default(string), "(?i)<CRC32>"));
		}

		string ICLConvert.ToCLString(SettingsProperty property, object obj) {
			if(property == ReportsProperty) {
				var lst = (ReadOnlyCollection<string>)obj;
				//A bit odd at first, but with this we make Reports==null the special case (i.e. list the consumers)
				return obj != null ? (lst.Count == 0 ? null : string.Join(",", lst)) : "";
			}
			return obj == null ? "<null>" : obj.ToString();
		}

		object ICLConvert.FromCLString(SettingsProperty property, string str) {
			if(property == ReportsProperty) {
				if(str != null && str.Length == 0) return null;
				//See ToCLString
				return Array.AsReadOnly((str ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray());

			} else if(property == CRC32ErrorProperty) {
				var parts = str.Split(':');
				var retVal = parts.Length == 1 ? (parts[0], (((string, string))property.DefaultValue).Item2) : (parts[0], parts[1]);

				Regex.IsMatch("12345678", retVal.Item2.Replace("<CRC32>", "12345678")); //Throw Early on invalid Regex

				return retVal;

			}

			return Convert.ChangeType(str, property.ValueType);
		}
	}

	public class DisplaySettings : SettingsObject {
		public SettingsProperty HideBuffersProperty { get; }
		public bool HideBuffers {
			get => (bool)GetValue(HideBuffersProperty);
			set => SetValue(HideBuffersProperty, value);
		}

		public SettingsProperty HideFileProgressProperty { get; }
		public bool HideFileProgress {
			get => (bool)GetValue(HideFileProgressProperty);
			set => SetValue(HideFileProgressProperty, value);
		}

		public SettingsProperty HideTotalProgressProperty { get; }
		public bool HideTotalProgress {
			get => (bool)GetValue(HideTotalProgressProperty);
			set => SetValue(HideTotalProgressProperty, value);
		}

		public SettingsProperty ShowDisplayJitterProperty { get; }
		public bool ShowDisplayJitter {
			get => (bool)GetValue(ShowDisplayJitterProperty);
			set => SetValue(ShowDisplayJitterProperty, value);
		}

		public SettingsProperty ForwardConsoleCursorOnlyProperty { get; }
		public bool ForwardConsoleCursorOnly {
			get => (bool)GetValue(ForwardConsoleCursorOnlyProperty);
			set => SetValue(ForwardConsoleCursorOnlyProperty, value);
		}

		public DisplaySettings() {
			Name = "Display";
			ResourceManager = Lang.ResourceManager;

			HideBuffersProperty = Register(nameof(HideBuffers), false);
			HideFileProgressProperty = Register(nameof(HideFileProgress), false);
			HideTotalProgressProperty = Register(nameof(HideTotalProgress), false);
			ShowDisplayJitterProperty = Register(nameof(ShowDisplayJitter), false);
			ForwardConsoleCursorOnlyProperty = Register(nameof(ForwardConsoleCursorOnly), false);
		}
	}

	public class NullStreamTestSettings {
		public NullStreamTestSettings(int streamCount, long streamLength, int parallelStreamCount) {
			StreamCount = streamCount;
			StreamLength = streamLength;
			ParallelStreamCount = parallelStreamCount;
		}

		public int StreamCount { get; }
		public long StreamLength { get; }
		public int ParallelStreamCount { get; internal set; }
	}


	public class DiagnosticsSettings : SettingsObject, ICLConvert {
		public SettingsProperty SaveErrorsProperty { get; }
		public bool SaveErrors {
			get => (bool)GetValue(SaveErrorsProperty);
			set => SetValue(SaveErrorsProperty, value);
		}

		public SettingsProperty SkipEnvironmentElementProperty { get; }
		public bool SkipEnvironmentElement {
			get => (bool)GetValue(SkipEnvironmentElementProperty);
			set => SetValue(SkipEnvironmentElementProperty, value);
		}

		public SettingsProperty IncludePersonalDataProperty { get; }
		public bool IncludePersonalData {
			get => (bool)GetValue(IncludePersonalDataProperty);
			set => SetValue(IncludePersonalDataProperty, value);
		}

		public SettingsProperty ErrorDirectoryProperty { get; }
		public string ErrorDirectory {
			get => (string)GetValue(ErrorDirectoryProperty);
			set => SetValue(ErrorDirectoryProperty, value);
		}

		public SettingsProperty NullStreamTestProperty { get; }
		public NullStreamTestSettings NullStreamTest {
			get => (NullStreamTestSettings)GetValue(NullStreamTestProperty);
			set => SetValue(NullStreamTestProperty, value);
		}

		public DiagnosticsSettings() {
			Name = "Diagnostics";
			ResourceManager = Lang.ResourceManager;

			SaveErrorsProperty = Register(nameof(SaveErrors), false);
			SkipEnvironmentElementProperty = Register(nameof(SkipEnvironmentElement), false);
			IncludePersonalDataProperty = Register(nameof(IncludePersonalData), false);
			ErrorDirectoryProperty = Register(nameof(ErrorDirectory), Environment.CurrentDirectory);
			NullStreamTestProperty = Register<NullStreamTestSettings>(nameof(NullStreamTest), null);

		}

		public string ToCLString(SettingsProperty property, object obj) {
			if(property == NullStreamTestProperty) {
				var nullStreamTestSettings = (NullStreamTestSettings)obj;

				return nullStreamTestSettings == null ? "" :
					nullStreamTestSettings.StreamCount +
					":" + nullStreamTestSettings.StreamLength +
					":" + nullStreamTestSettings.ParallelStreamCount;
			}
			return obj == null ? "<null>" : obj.ToString();
		}

		public object FromCLString(SettingsProperty property, string str) {
			if(property == NullStreamTestProperty) {
				var args = str.Split(':');
				return args.Length == 0 ? null :
					new NullStreamTestSettings(
						int.Parse(args[0]),
						long.Parse(args[1]) * (1 << 20),
						int.Parse(args[2])
					);
			}
			return Convert.ChangeType(str, property.ValueType);
		}
	}

}
