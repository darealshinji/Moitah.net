// ****************************************************************************
// 
// CUE Tools
// Copyright (C) 2006-2007  Moitah (moitah@yahoo.com)
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace JDP {
	public partial class frmCUETools : Form {
		public frmCUETools() {
			InitializeComponent();
		}

		private void btnBrowseInput_Click(object sender, EventArgs e) {
			OpenFileDialog fileDlg = new OpenFileDialog();
			DialogResult dlgRes;

			fileDlg.Title = "Input CUE Sheet";
			fileDlg.Filter = "CUE Sheets (*.cue)|*.cue";

			dlgRes = fileDlg.ShowDialog();
			if (dlgRes == DialogResult.OK) {
				txtInputPath.Text = fileDlg.FileName;
			}
		}

		private void btnBrowseOutput_Click(object sender, EventArgs e) {
			SaveFileDialog fileDlg = new SaveFileDialog();
			DialogResult dlgRes;

			fileDlg.Title = "Output CUE Sheet";
			fileDlg.Filter = "CUE Sheets (*.cue)|*.cue";

			dlgRes = fileDlg.ShowDialog();
			if (dlgRes == DialogResult.OK) {
				txtOutputPath.Text = fileDlg.FileName;
			}
		}

		private void btnConvert_Click(object sender, EventArgs e) {
			if ((_workThread != null) && (_workThread.IsAlive)) {
				_workClass.Stop();
			}
			else {
				if (!CheckWriteOffset()) return;
				StartConvert();
			}
		}

		private void btnBatch_Click(object sender, EventArgs e) {
			if (rbDontGenerate.Checked) {
				MessageBox.Show("Batch mode cannot be used with the output path set manually.",
					"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			FolderBrowserDialog folderDialog = new FolderBrowserDialog();
			folderDialog.Description = "Select the folder containing the CUE sheets you want to convert.  Subfolders will be included automatically.";
			folderDialog.ShowNewFolderButton = false;
			if (folderDialog.ShowDialog() == DialogResult.OK) {
				if (!CheckWriteOffset()) return;
				AddDirToBatch(folderDialog.SelectedPath);
				StartConvert();
			}
		}

		private void btnFilenameCorrector_Click(object sender, EventArgs e) {
			if ((_fcForm == null) || _fcForm.IsDisposed) {
				_fcForm = new frmFilenameCorrector();
				CenterSubForm(_fcForm);
				_fcForm.Show();
			}
			else {
				_fcForm.Activate();
			}
		}

		private void btnSettings_Click(object sender, EventArgs e) {
			using (frmSettings settingsForm = new frmSettings()) {
				settingsForm.WriteOffset = _writeOffset;
				settingsForm.PreserveHTOA = _preserveHTOA;
				settingsForm.AutoCorrectFilenames = _autoCorrectFilenames;
				settingsForm.FLACCompressionLevel = _flacCompressionLevel;
				settingsForm.FLACVerify = _flacVerify;
				settingsForm.WVCompressionMode = _wvCompressionMode;
				settingsForm.WVExtraMode = _wvExtraMode;

				CenterSubForm(settingsForm);
				settingsForm.ShowDialog();

				_writeOffset = settingsForm.WriteOffset;
				_preserveHTOA = settingsForm.PreserveHTOA;
				_autoCorrectFilenames = settingsForm.AutoCorrectFilenames;
				_flacCompressionLevel = settingsForm.FLACCompressionLevel;
				_flacVerify = settingsForm.FLACVerify;
				_wvCompressionMode = settingsForm.WVCompressionMode;
				_wvExtraMode = settingsForm.WVExtraMode;
			}
		}

		private void btnAbout_Click(object sender, EventArgs e) {
			string msg = String.Format("CUE Tools v1.9.1{0}Copyright 2006-2007 Moitah{0}" +
				"http://www.moitah.net/", Environment.NewLine);

			MessageBox.Show(msg, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void PathTextBox_DragEnter(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop) && !((TextBox)sender).ReadOnly) {
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void PathTextBox_DragDrop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files.Length == 1) {
					((TextBox)sender).Text = files[0];
				}
			}
		}

		private void txtInputPath_TextChanged(object sender, EventArgs e) {
			UpdateOutputPath();
		}

		private void rbCreateSubdirectory_CheckedChanged(object sender, EventArgs e) {
			UpdateOutputPath();
		}

		private void rbAppendFilename_CheckedChanged(object sender, EventArgs e) {
			UpdateOutputPath();
		}

		private void rbCustomFormat_CheckedChanged(object sender, EventArgs e) {
			UpdateOutputPath();
		}

		private void txtCreateSubdirectory_TextChanged(object sender, EventArgs e) {
			UpdateOutputPath();
		}

		private void txtAppendFilename_TextChanged(object sender, EventArgs e) {
			UpdateOutputPath();
		}

		private void txtCustomFormat_TextChanged(object sender, EventArgs e) {
			UpdateOutputPath();
		}

		private void frmCUETools_Load(object sender, EventArgs e) {
			_batchPaths = new List<string>();
			LoadSettings();
			UpdateOutputPath();
		}

		private void frmCUETools_FormClosed(object sender, FormClosedEventArgs e) {
			SaveSettings();
		}


		// ********************************************************************************


		frmFilenameCorrector _fcForm;
		List<string> _batchPaths;
		string[] _charMap;
		bool _preserveHTOA, _usePregapForFirstTrackInSingleFile, _flacVerify, _autoCorrectFilenames;
		int _writeOffset, _flacCompressionLevel, _wvCompressionMode, _wvExtraMode;
		Thread _workThread;
		CUESheet _workClass;

		private void StartConvert() {
			string pathIn, pathOut, outDir, extension;
			bool outputExists, outputAudio;
			CUESheet cueSheet;
			CUEStyle cueStyle;

			try {
				_workThread = null;
				if (_batchPaths.Count != 0) {
					txtInputPath.Text = _batchPaths[0];
				}
				pathIn = txtInputPath.Text;
				outputAudio = !chkNoAudioOutput.Checked;

				if (!File.Exists(pathIn)) {
					throw new Exception("Input CUE Sheet not found.");
				}

				cueSheet = new CUESheet(pathIn, _autoCorrectFilenames);

				cueStyle = SelectedCUEStyle;

				if (rbFLAC.Checked) {
					extension = ".flac";
				}
				else if (rbWavPack.Checked) {
					extension = ".wv";
				}
				else {
					extension = ".wav";
				}

				cueSheet.WriteOffset = _writeOffset;

				BuildCharMap(chkRemoveSpecial.Checked, txtSpecialExceptions.Text);
				GenerateFilenames(cueSheet, txtSingleFilenameFormat.Text, txtTrackFilenameFormat.Text,
					extension, chkReplaceSpaces.Checked, chkKeepOriginalFilenames.Checked);
				UpdateOutputPath(cueSheet.Artist, cueSheet.Title);
				pathOut = txtOutputPath.Text;
				outDir = Path.GetDirectoryName(pathOut);

				outputExists = File.Exists(pathOut);
				if (outputAudio) {
					if (cueStyle == CUEStyle.SingleFile) {
						outputExists |= File.Exists(Path.Combine(outDir, cueSheet.SingleFilename));
					}
					else {
						if ((cueStyle == CUEStyle.GapsAppended) && _preserveHTOA) {
							outputExists |= File.Exists(Path.Combine(outDir, cueSheet.HTOAFilename));
						}
						for (int i = 0; i < cueSheet.TrackCount; i++) {
							outputExists |= File.Exists(Path.Combine(outDir, cueSheet.TrackFilenames[i]));
						}
					}
				}
				if (outputExists) {
					DialogResult dlgRes = MessageBox.Show("One or more output file already exists, " +
						"do you want to overwrite?", "Overwrite?", (_batchPaths.Count == 0) ?
						MessageBoxButtons.YesNo : MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

					if (dlgRes == DialogResult.Cancel) {
						_batchPaths.Clear();
					}
					if (dlgRes != DialogResult.Yes) {
						goto SkipConversion;
					}
				}

				cueSheet.PreserveHTOA = _preserveHTOA;
				cueSheet.UsePregapForFirstTrackInSingleFile = _usePregapForFirstTrackInSingleFile && !outputAudio;
				cueSheet.FLACCompressionLevel = _flacCompressionLevel;
				cueSheet.FLACVerify = _flacVerify;
				cueSheet.WVCompressionMode = _wvCompressionMode;
				cueSheet.WVExtraMode = _wvExtraMode;

				if (!Directory.Exists(outDir)) {
					Directory.CreateDirectory(outDir);
				}

				cueSheet.Write(pathOut, cueStyle);

				if (outputAudio) {
					object[] p = new object[3];

					_workThread = new Thread(WriteAudioFilesThread);
					_workClass = cueSheet;

					p[0] = cueSheet;
					p[1] = outDir;
					p[2] = cueStyle;

					SetupControls(true);
					_workThread.Start(p);
				}
				else {
					ShowFinishedMessage(cueSheet.PaddedToFrame);
				}
			}
			catch (Exception ex) {
				if (!ShowErrorMessage(ex)) {
					_batchPaths.Clear();
				}
			}

		SkipConversion:
			if ((_workThread == null) && (_batchPaths.Count != 0)) {
				_batchPaths.RemoveAt(0);
				if (_batchPaths.Count == 0) {
					ShowBatchDoneMessage();
				}
				else {
					StartConvert();
				}
			}
		}

		private void WriteAudioFilesThread(object o) {
			object[] p = (object[])o;

			CUESheet cueSheet = (CUESheet)p[0];
			string outDir = (string)p[1];
			CUEStyle cueStyle = (CUEStyle)p[2];

			try {
				cueSheet.WriteAudioFiles(outDir, cueStyle, new SetStatus(this.SetStatus));
				this.Invoke((MethodInvoker)delegate() {
					if (_batchPaths.Count == 0) SetupControls(false);
					ShowFinishedMessage(cueSheet.PaddedToFrame);
				});
			}
			catch (StopException) {
				_batchPaths.Clear();
				this.Invoke((MethodInvoker)delegate() {
					SetupControls(false);
					MessageBox.Show("Conversion was stopped.", "Stopped", MessageBoxButtons.OK,
						MessageBoxIcon.Exclamation);
				});
			}
			catch (Exception ex) {
				this.Invoke((MethodInvoker)delegate() {
					if (_batchPaths.Count == 0) SetupControls(false);
					if (!ShowErrorMessage(ex)) {
						_batchPaths.Clear();
						SetupControls(false);
					}
				});
			}

			if (_batchPaths.Count != 0) {
				_batchPaths.RemoveAt(0);
				this.BeginInvoke((MethodInvoker)delegate() {
					if (_batchPaths.Count == 0) {
						SetupControls(false);
						ShowBatchDoneMessage();
					}
					else {
						StartConvert();
					}
				});
			}
		}

		public void SetStatus(string status) {
			this.BeginInvoke((MethodInvoker)delegate() {
				txtStatus.Text = status;
			});
		}

		private void SetupControls(bool running) {
			grpCUEPaths.Enabled = !running;
			grpOutputPathGeneration.Enabled = !running;
			grpAudioOutput.Enabled = !running;
			grpOutputStyle.Enabled = !running;
			grpAudioFilenames.Enabled = !running;
			btnAbout.Enabled = !running;
			btnSettings.Enabled = !running;
			btnFilenameCorrector.Enabled = !running;
			btnBatch.Enabled = !running;
			btnConvert.Text = running ? "Stop" : "Convert";
			txtStatus.Text = String.Empty;
		}

		private bool ShowErrorMessage(Exception ex) {
			DialogResult dlgRes = MessageBox.Show(ex.Message, "Error", (_batchPaths.Count == 0) ? 
				MessageBoxButtons.OK : MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
			return (dlgRes == DialogResult.OK);
		}

		private void ShowFinishedMessage(bool warnAboutPadding) {
			if (_batchPaths.Count != 0) {
				return;
			}
			if (warnAboutPadding) {
				MessageBox.Show("One or more input file doesn't end on a CD frame boundary.  " +
					"The output has been padded where necessary to fix this.  If your input " +
					"files are from a CD source, this may indicate a problem with your files.",
					"Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			MessageBox.Show("Conversion was successful!", "Done", MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}

		private void ShowBatchDoneMessage() {
			MessageBox.Show("Batch conversion is complete!", "Done", MessageBoxButtons.OK,
				MessageBoxIcon.Information);
		}

		private bool CheckWriteOffset() {
			if ((_writeOffset == 0) || chkNoAudioOutput.Checked) {
				return true;
			}

			DialogResult dlgRes = MessageBox.Show("Write offset setting is non-zero which " +
				"will cause some samples to be discarded.  You should only use this setting " +
				"to make temporary files for burning.  Are you sure you want to continue?",
				"Write offset is enabled", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
			return (dlgRes == DialogResult.Yes);
		}

		private void AddDirToBatch(string dir) {
			string[] files = Directory.GetFiles(dir, "*.cue");
			string[] subDirs = Directory.GetDirectories(dir);
			_batchPaths.AddRange(files);
			foreach (string subDir in subDirs) {
				AddDirToBatch(subDir);
			}
		}

		private void LoadSettings() {
			SettingsReader sr = new SettingsReader("CUE Tools", "settings.txt");
			string val;

			val = sr.Load("OutputPathGeneration");
			if (val != null) {
				try {
					SelectedOutputPathGeneration = (OutputPathGeneration)Int32.Parse(val);
				}
				catch { }
			}

			val = sr.Load("OutputSubdirectory");
			if (val != null) {
				txtCreateSubdirectory.Text = val;
			}

			val = sr.Load("OutputFilenameSuffix");
			if (val != null) {
				txtAppendFilename.Text = val;
			}

			val = sr.Load("OutputCustomFormat");
			if (val != null) {
				txtCustomFormat.Text = val;
			}

			val = sr.Load("NoAudioOutput");
			if (val != null) {
				chkNoAudioOutput.Checked = (val != "0");
			}

			val = sr.Load("OutputAudioFormat");
			if (val != null) {
				try {
					SelectedOutputAudioFormat = (OutputAudioFormat)Int32.Parse(val);
				}
				catch { }
			}

			val = sr.Load("CUEStyle");
			if (val != null) {
				try {
					SelectedCUEStyle = (CUEStyle)Int32.Parse(val);
				}
				catch { }
			}

			val = sr.Load("KeepOriginalFilenames");
			if (val != null) {
				chkKeepOriginalFilenames.Checked = (val != "0");
			}

			val = sr.Load("SingleFilenameFormat");
			if (val != null) {
				txtSingleFilenameFormat.Text = val;
			}

			val = sr.Load("TrackFilenameFormat");
			if (val != null) {
				txtTrackFilenameFormat.Text = val;
			}

			val = sr.Load("RemoveSpecialCharacters");
			if (val != null) {
				chkRemoveSpecial.Checked = (val != "0");
			}

			val = sr.Load("SpecialCharactersExceptions");
			if (val != null) {
				txtSpecialExceptions.Text = val;
			}

			val = sr.Load("ReplaceSpaces");
			if (val != null) {
				chkReplaceSpaces.Checked = (val != "0");
			}

			val = sr.Load("WriteOffset");
			if (val != null) {
				if (!Int32.TryParse(val, out _writeOffset)) _writeOffset = 0;
			}

			val = sr.Load("PreserveHTOA");
			_preserveHTOA = (val != null) ? (val != "0") : true;

			val = sr.Load("UsePregapForFirstTrackInSingleFile");
			_usePregapForFirstTrackInSingleFile = (val != null) ? (val != "0") : false;

			val = sr.Load("AutoCorrectFilenames");
			_autoCorrectFilenames = (val != null) ? (val != "0") : false;

			val = sr.Load("FLACCompressionLevel");
			if ((val == null) || !Int32.TryParse(val, out _flacCompressionLevel) ||
				(_flacCompressionLevel < 0) || (_flacCompressionLevel > 8))
			{
				_flacCompressionLevel = 5;
			}

			val = sr.Load("FLACVerify");
			_flacVerify = (val != null) ? (val != "0") : false;

			val = sr.Load("WVCompressionMode");
			if ((val == null) || !Int32.TryParse(val, out _wvCompressionMode) ||
				(_wvCompressionMode < 0) || (_wvCompressionMode > 3))
			{
				_wvCompressionMode = 1;
			}

			val = sr.Load("WVExtraMode");
			if ((val == null) || !Int32.TryParse(val, out _wvExtraMode) ||
				(_wvExtraMode < 0) || (_wvExtraMode > 6))
			{
				_wvExtraMode = 0;
			}
		}

		private void SaveSettings() {
			SettingsWriter sw = new SettingsWriter("CUE Tools", "settings.txt");

			sw.Save("OutputPathGeneration", ((int)SelectedOutputPathGeneration).ToString());

			sw.Save("OutputSubdirectory", txtCreateSubdirectory.Text);

			sw.Save("OutputFilenameSuffix", txtAppendFilename.Text);

			sw.Save("OutputCustomFormat", txtCustomFormat.Text);

			sw.Save("NoAudioOutput", chkNoAudioOutput.Checked ? "1" : "0");

			sw.Save("OutputAudioFormat", ((int)SelectedOutputAudioFormat).ToString());

			sw.Save("CUEStyle", ((int)SelectedCUEStyle).ToString());

			sw.Save("KeepOriginalFilenames", chkKeepOriginalFilenames.Checked ? "1" : "0");

			sw.Save("SingleFilenameFormat", txtSingleFilenameFormat.Text);

			sw.Save("TrackFilenameFormat", txtTrackFilenameFormat.Text);

			sw.Save("RemoveSpecialCharacters", chkRemoveSpecial.Checked ? "1" : "0");

			sw.Save("SpecialCharactersExceptions", txtSpecialExceptions.Text);

			sw.Save("ReplaceSpaces", chkReplaceSpaces.Checked ? "1" : "0");

			sw.Save("WriteOffset", _writeOffset.ToString());

			sw.Save("PreserveHTOA", _preserveHTOA ? "1" : "0");

			sw.Save("UsePregapForFirstTrackInSingleFile", _usePregapForFirstTrackInSingleFile ? "1" : "0");

			sw.Save("AutoCorrectFilenames", _autoCorrectFilenames ? "1" : "0");

			sw.Save("FLACCompressionLevel", _flacCompressionLevel.ToString());

			sw.Save("FLACVerify", _flacVerify ? "1" : "0");

			sw.Save("WVCompressionMode", _wvCompressionMode.ToString());

			sw.Save("WVExtraMode", _wvExtraMode.ToString());

			sw.Close();
		}

		private void GenerateFilenames(CUESheet cueSheet, string singleFormat, string trackFormat, string extension, bool replaceSpaces, bool keepOriginal) {
			List<string> find, replace;
			string filename;
			int iTrack;

			find = new List<string>();
			replace = new List<string>();

			find.Add("%D"); // 0: Album artist
			find.Add("%C"); // 1: Album title
			find.Add("%N"); // 2: Track number
			find.Add("%A"); // 3: Track artist
			find.Add("%T"); // 4: Track title

			replace.Add(EmptyStringToNull(CleanseString(cueSheet.Artist)));
			replace.Add(EmptyStringToNull(CleanseString(cueSheet.Title)));
			replace.Add(null);
			replace.Add(null);
			replace.Add(null);

			if (keepOriginal && cueSheet.HasSingleFilename) {
				cueSheet.SingleFilename = Path.ChangeExtension(cueSheet.SingleFilename, extension);
			}
			else {
				filename = ReplaceMultiple(singleFormat, find, replace);
				if (filename == null) {
					filename = "Range";
				}
				if (replaceSpaces) {
					filename = filename.Replace(' ', '_');
				}
				filename += extension;

				cueSheet.SingleFilename = filename;
			}

			for (iTrack = -1; iTrack < cueSheet.TrackCount; iTrack++) {
				bool htoa = (iTrack == -1);

				if (keepOriginal && htoa && cueSheet.HasHTOAFilename) {
					cueSheet.HTOAFilename = Path.ChangeExtension(cueSheet.HTOAFilename, extension);
				}
				else if (keepOriginal && !htoa && cueSheet.HasTrackFilenames) {
					cueSheet.TrackFilenames[iTrack] = Path.ChangeExtension(
						cueSheet.TrackFilenames[iTrack], extension);
				}
				else {
					string trackStr = htoa ? "01.00" : String.Format("{0:00}", iTrack + 1);
					string artist = cueSheet.Tracks[htoa ? 0 : iTrack].Artist;
					string title = htoa ? "(HTOA)" : cueSheet.Tracks[iTrack].Title;

					replace[2] = trackStr;
					replace[3] = EmptyStringToNull(CleanseString(artist));
					replace[4] = EmptyStringToNull(CleanseString(title));

					filename = ReplaceMultiple(trackFormat, find, replace);
					if (filename == null) {
						filename = replace[2];
					}
					if (replaceSpaces) {
						filename = filename.Replace(' ', '_');
					}
					filename += extension;

					if (htoa) {
						cueSheet.HTOAFilename = filename;
					}
					else {
						cueSheet.TrackFilenames[iTrack] = filename;
					}
				}
			}
		}

		private void BuildOutputPathFindReplace(string inputPath, string format, List<string> find, List<string> replace) {
			int i, j, first, last, maxFindLen;
			string range;
			string[] rangeSplit;
			List<string> tmpFind = new List<string>();
			List<string> tmpReplace = new List<string>();

			i = 0;
			last = 0;
			while (i < format.Length) {
				if (format[i++] == '%') {
					j = i;
					while (j < format.Length) {
						char c = format[j];
						if (((c < '0') || (c > '9')) && (c != '-') && (c != ':')) {
							break;
						}
						j++;
					}
					range = format.Substring(i, j - i);
					if (range.Length != 0) {
						rangeSplit = range.Split(new char[] { ':' }, 2);
						if (Int32.TryParse(rangeSplit[0], out first)) {
							if (rangeSplit.Length == 1) {
								last = first;
							}
							if ((rangeSplit.Length == 1) || Int32.TryParse(rangeSplit[1], out last)) {
								tmpFind.Add("%" + range);
								tmpReplace.Add(EmptyStringToNull(GetDirectoryElements(Path.GetDirectoryName(inputPath), first, last)));
							}
						}
					}
					i = j;
				}
			}

			// Sort so that longest find strings are first, so when the replacing is done the
			// longer strings are checked first.  This avoids problems with overlapping find
			// strings, for example if one of the strings is "%1" and another is "%1:3".
			maxFindLen = 0;
			for (i = 0; i < tmpFind.Count; i++) {
				if (tmpFind[i].Length > maxFindLen) {
					maxFindLen = tmpFind[i].Length;
				}
			}
			for (j = maxFindLen; j >= 1; j--) {
				for (i = 0; i < tmpFind.Count; i++) {
					if (tmpFind[i].Length == j) {
						find.Add(tmpFind[i]);
						replace.Add(tmpReplace[i]);
					}
				}
			}

			find.Add("%F");
			replace.Add(Path.GetFileNameWithoutExtension(inputPath));
		}

		private string GetDirectoryElements(string dir, int first, int last) {
			string[] dirSplit = dir.Split(Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar);
			int count = dirSplit.Length;

			if ((first == 0) && (last == 0)) {
				first = 1;
				last = count;
			}

			if (first < 0) first = (count + 1) + first;
			if (last < 0) last = (count + 1) + last;

			if ((first < 1) && (last < 1)) {
				return String.Empty;
			}
			else if ((first > count) && (last > count)) {
				return String.Empty;
			}
			else {
				int i;
				StringBuilder sb = new StringBuilder();

				if (first < 1) first = 1;
				if (first > count) first = count;
				if (last < 1) last = 1;
				if (last > count) last = count;

				if (last >= first) {
					for (i = first; i <= last; i++) {
						sb.Append(dirSplit[i - 1]);
						sb.Append(Path.DirectorySeparatorChar);
					}
				}
				else {
					for (i = first; i >= last; i--) {
						sb.Append(dirSplit[i - 1]);
						sb.Append(Path.DirectorySeparatorChar);
					}
				}

				return sb.ToString(0, sb.Length - 1);
			}
		}

		private CUEStyle SelectedCUEStyle {
			get {
				if (rbGapsAppended.Checked)	 return CUEStyle.GapsAppended;
				if (rbGapsPrepended.Checked) return CUEStyle.GapsPrepended;
				if (rbGapsLeftOut.Checked)	 return CUEStyle.GapsLeftOut;
											 return CUEStyle.SingleFile;
			}
			set {
				switch (value) {
					case CUEStyle.SingleFile:	 rbSingleFile.Checked = true; break;
					case CUEStyle.GapsAppended:	 rbGapsAppended.Checked = true; break;
					case CUEStyle.GapsPrepended: rbGapsPrepended.Checked = true; break;
					case CUEStyle.GapsLeftOut:	 rbGapsLeftOut.Checked = true; break;
				}
			}
		}

		private OutputPathGeneration SelectedOutputPathGeneration {
			get {
				if (rbCreateSubdirectory.Checked) return OutputPathGeneration.CreateSubdirectory;
				if (rbAppendFilename.Checked)	  return OutputPathGeneration.AppendFilename;
				if (rbCustomFormat.Checked)		  return OutputPathGeneration.CustomFormat;
												  return OutputPathGeneration.Disabled;
			}
			set {
				switch (value) {
					case OutputPathGeneration.CreateSubdirectory: rbCreateSubdirectory.Checked = true; break;
					case OutputPathGeneration.AppendFilename:	  rbAppendFilename.Checked = true; break;
					case OutputPathGeneration.CustomFormat:		  rbCustomFormat.Checked = true; break;
					case OutputPathGeneration.Disabled:			  rbDontGenerate.Checked = true; break;
				}
			}
		}

		private OutputAudioFormat SelectedOutputAudioFormat {
			get {
				if (rbFLAC.Checked)    return OutputAudioFormat.FLAC;
				if (rbWavPack.Checked) return OutputAudioFormat.WavPack;
									   return OutputAudioFormat.WAV;
			}
			set {
				switch (value) {
					case OutputAudioFormat.FLAC:    rbFLAC.Checked = true; break;
					case OutputAudioFormat.WavPack: rbWavPack.Checked = true; break;
					case OutputAudioFormat.WAV:     rbWAV.Checked = true; break;
				}
			}
		}

		private void CenterSubForm(Form form) {
			int centerX, centerY, formX, formY;
			Rectangle formRect, maxRect;

			centerX = ((Left * 2) + Width ) / 2;
			centerY = ((Top  * 2) + Height) / 2;
			formX   = ((Left * 2) + Width  - form.Width ) / 2;
			formY   = ((Top  * 2) + Height - form.Height) / 2;

			formRect = new Rectangle(formX, formY, form.Width, form.Height);
			maxRect = Screen.GetWorkingArea(new Point(centerX, centerY));

			if (formRect.Right > maxRect.Right) {
				formRect.X -= formRect.Right - maxRect.Right;
			}
			if (formRect.Bottom > maxRect.Bottom) {
				formRect.Y -= formRect.Bottom - maxRect.Bottom;
			}
			if (formRect.X < maxRect.X) {
				formRect.X = maxRect.X;
			}
			if (formRect.Y < maxRect.Y) {
				formRect.Y = maxRect.Y;
			}

			form.Location = formRect.Location;
		}

		private void UpdateOutputPath() {
			UpdateOutputPath("Artist", "Album");
		}

		private void UpdateOutputPath(string artist, string album) {
			txtOutputPath.ReadOnly = !rbDontGenerate.Checked;
			btnBrowseOutput.Enabled = rbDontGenerate.Checked;

			if (!rbDontGenerate.Checked) {
				if (rbCustomFormat.Checked) {
					BuildCharMap(chkRemoveSpecial.Checked, txtSpecialExceptions.Text);
				}
				txtOutputPath.Text = GenerateOutputPath(artist, album);
			}
		}

		private string GenerateOutputPath(string artist, string album) {
			string pathIn, pathOut, dir, file, ext;

			pathIn = txtInputPath.Text;
			pathOut = String.Empty;

			if ((pathIn.Length != 0) && File.Exists(pathIn)) {
				dir = Path.GetDirectoryName(pathIn);
				file = Path.GetFileNameWithoutExtension(pathIn);
				ext = Path.GetExtension(pathIn);

				if (rbCreateSubdirectory.Checked) {
					pathOut = Path.Combine(Path.Combine(dir, txtCreateSubdirectory.Text), file + ext);
				}
				else if (rbAppendFilename.Checked) {
					pathOut = Path.Combine(dir, file + txtAppendFilename.Text + ext);
				}
				else if (rbCustomFormat.Checked) {
					string format = txtCustomFormat.Text;
					List<string> find = new List<string>();
					List<string> replace = new List<string>();
					bool rs = chkReplaceSpaces.Checked;

					find.Add("%D");
					find.Add("%C");
					replace.Add(EmptyStringToNull(CleanseString(rs ? artist.Replace(' ', '_') : artist)));
					replace.Add(EmptyStringToNull(CleanseString(rs ? album.Replace(' ', '_') : album)));
					BuildOutputPathFindReplace(pathIn, format, find, replace);

					pathOut = ReplaceMultiple(format, find, replace);
					if (pathOut == null) pathOut = String.Empty;
				}
			}

			return pathOut;
		}

		private string EmptyStringToNull(string s) {
			return ((s != null) && (s.Length == 0)) ? null : s;
		}

		private void BuildCharMap(bool disallowSpecial, string specialExceptions) {
			System.Collections.BitArray allowed = new System.Collections.BitArray(256, true);
			char[] invalid = Path.GetInvalidFileNameChars();
			int i;

			if (disallowSpecial) {
				byte[] exceptions = CUESheet.Encoding.GetBytes(specialExceptions);

				for (i = 0; i <= 255; i++) {
					allowed[i] = ((i >= 'a') && (i <= 'z')) ||
								 ((i >= 'A') && (i <= 'Z')) ||
								 ((i >= '0') && (i <= '9')) ||
								 (i == ' ') || (i == '_');
				}

				for (i = 0; i < exceptions.Length; i++) {
					allowed[exceptions[i]] = true;
				}
			}

			for (i = 0; i < invalid.Length; i++) {
				allowed[invalid[i]] = false;
			}

			_charMap = new string[256];
			for (i = 0; i <= 255; i++) {
				if (allowed[i]) {
					_charMap[i] = CUESheet.Encoding.GetString( new byte[] { (byte)i } );
				}
				else {
					_charMap[i] = String.Empty;
				}
			}
		}

		private string CleanseString(string s) {
			StringBuilder sb = new StringBuilder();
			byte[] b = CUESheet.Encoding.GetBytes(s);

			for (int i = 0; i < b.Length; i++) {
				sb.Append(_charMap[b[i]]);
			}

			return sb.ToString();
		}

		private string ReplaceMultiple(string s, List<string> find, List<string> replace) {
			if (find.Count != replace.Count) {
				throw new ArgumentException();
			}
			StringBuilder sb;
			int iChar, iFind;
			string f;
			bool found;

			sb = new StringBuilder();

			for (iChar = 0; iChar < s.Length; iChar++) {
				found = false;
				for (iFind = 0; iFind < find.Count; iFind++) {
					f = find[iFind];
					if ((f.Length <= (s.Length - iChar)) && (s.Substring(iChar, f.Length) == f)) {
						if (replace[iFind] == null) {
							return null;
						}
						sb.Append(replace[iFind]);
						iChar += f.Length - 1;
						found = true;
						break;
					}
				}

				if (!found) {
					sb.Append(s[iChar]);
				}
			}

			return sb.ToString();
		}
	}

	enum OutputPathGeneration {
		CreateSubdirectory,
		AppendFilename,
		CustomFormat,
		Disabled
	}

	enum OutputAudioFormat {
		WAV,
		FLAC,
		WavPack
	}
}