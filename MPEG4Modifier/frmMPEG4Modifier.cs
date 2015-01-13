// --------------------------------------------------------------------------------
// Copyright (c) 2004 J.D. Purcell
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// --------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace JDP {
	internal partial class frmMPEG4Modifier : Form {
		private static string[] _args;
		private bool _updatingUI;
		private string _sourcePath, _destPath;
		private AVIModifier _aviMod;
		private MPEG4FrameModifier _mp4Mod;
		private frmWait _waitForm;
		private Exception _workEx;
		private bool _showingAutoUD;
		private List<MPEG4UserData> _regUDList;
		private List<MPEG4UserData> _autoUDList;

		public frmMPEG4Modifier(string[] args) {
			InitializeComponent();
			Program.SetFontAndScaling(this);
			_args = args;
			_updatingUI = false;
		}

		private void SetupForm(bool isVideoLoaded) {
			_updatingUI = true;

			grpAspectRatio.Enabled = isVideoLoaded;
			grpPackedBitstream.Enabled = isVideoLoaded;
			grpUserData.Enabled = isVideoLoaded;
			grpInterlacing.Enabled = isVideoLoaded;
			btnSave.Enabled = isVideoLoaded;
			btnVideoInfo.Enabled = isVideoLoaded;

			if (!isVideoLoaded) {
				if (_aviMod != null) {
					_aviMod.Close();
					_aviMod = null;
				}
				_mp4Mod = null;
				_regUDList = null;
				_autoUDList = null;
				_showingAutoUD = false;
				txtSourcePath.Text = String.Empty;
				rbPAR_VGA_1_1.Checked = true;
				txtPAR_Width.Text = String.Empty;
				txtPAR_Height.Text = String.Empty;
				txtDAR_Width.Text = String.Empty;
				txtDAR_Height.Text = String.Empty;
				lblIsPacked.Text = "...";
				chkChangePacking.Text = "Unpack";
				chkChangePacking.Checked = false;
				chkAutoUD.Checked = true;
				chkAutoUD.Enabled = false;
				lstUserData.Items.Clear();
				lblIsInterlaced.Text = "...";
				rbTFF.Checked = true;
			}

			_updatingUI = false;
		}

		private void LoadSource(string path) {
			SetupForm(false);
			_sourcePath = path;
			txtSourcePath.Text = path;

			// Load video
			_waitForm = new frmWait("Loading", "Loading video, please wait...",
				new Thread(LoadSourceThread));
			_waitForm.ShowDialog(this);
			_waitForm.Dispose();
			_waitForm = null;

			if (_workEx != null) {
				SetupForm(false);
				MessageBox.Show(this, _workEx.Message, "Error", MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}
			else if (_aviMod.WasStopped) {
				SetupForm(false);
				return;
			}

			SetupForm(true);

			_updatingUI = true;

			// Aspect Ratio
			switch (_mp4Mod.PARInfo.Type) {
				case PARType.VGA_1_1:
					rbPAR_VGA_1_1.Checked = true;
					break;
				case PARType.PAL_4_3:
					rbPAR_PAL_4_3.Checked = true;
					break;
				case PARType.NTSC_4_3:
					rbPAR_NTSC_4_3.Checked = true;
					break;
				case PARType.PAL_16_9:
					rbPAR_PAL_16_9.Checked = true;
					break;
				case PARType.NTSC_16_9:
					rbPAR_NTSC_16_9.Checked = true;
					break;
				case PARType.Custom: {
					double par = (double)_mp4Mod.PARInfo.Width / (double)_mp4Mod.PARInfo.Height;
					double rar = (double)_mp4Mod.FrameWidth / (double)_mp4Mod.FrameHeight;
					double dar = par * rar;

					rbPAR_Custom.Checked = true;
					txtPAR_Width.Text = _mp4Mod.PARInfo.Width.ToString();
					txtPAR_Height.Text = _mp4Mod.PARInfo.Height.ToString();

					txtDAR_Width.Text = dar.ToString("0.###");
					txtDAR_Height.Text = "1";

					break;
				}
			}

			// Packed Bitstream
			lblIsPacked.Text = _mp4Mod.IsPacked ? "Yes" : "No";
			chkChangePacking.Text = _mp4Mod.IsPacked ? "Unpack" : "Pack";
			chkChangePacking.Enabled = _mp4Mod.IsPacked || _mp4Mod.ContainsBVOPs;

			// User Data
			_regUDList = _mp4Mod.UserDataList;
			_autoUDList = _mp4Mod.SuggestedUserData();
			UDListBoxItems = _regUDList;

			// Interlacing
			bool isInt = _mp4Mod.IsInterlaced;
			lblIsInterlaced.Text = isInt ? "Yes" : "No";
			rbTFF.Enabled = isInt;
			rbBFF.Enabled = isInt;
			if (isInt) {
				(_mp4Mod.TopFieldFirst ? rbTFF : rbBFF).Checked = true;
			}

			_updatingUI = false;
		}

		private void LoadSourceThread() {
			_workEx = null;
			try {
				_aviMod = new AVIModifier(_sourcePath);
				_mp4Mod = new MPEG4FrameModifier();
				_aviMod.FrameModifier = _mp4Mod;
				_mp4Mod.VideoModifier = _aviMod;

				_aviMod.ProgressCallback = _waitForm.SetProgress;
				_aviMod.Preview();
			}
			catch (Exception ex) {
				_workEx = ex;
			}
			_waitForm.FinishedWork();
		}

		private void BeginLoadSource(string path) {
			Thread thread = new Thread(BeginLoadSourceThread);
			thread.Start((object)path);
		}

		private void BeginLoadSourceThread(object path) {
			BeginInvoke((MethodInvoker)delegate() {
				LoadSource((string)path);
			});
		}

		private void SaveDest(string path) {
			bool wasStopped;

			_destPath = path;

			// Aspect Ratio
			try {
				_mp4Mod.PARInfo = GetMPEG4PARFromForm();
			}
			catch {
				MessageBox.Show(this, "The aspect ratio you entered is invalid.", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			// User Data
			_mp4Mod.UserDataList = UDListBoxItems;

			// Packed Bitstream
			_mp4Mod.NewIsPacked = _mp4Mod.IsPacked ^ chkChangePacking.Checked;

			// Interlacing
			_mp4Mod.TopFieldFirst = rbTFF.Checked;

			// Save video
			_waitForm = new frmWait("Saving", "Saving video, please wait...",
				new Thread(SaveDestThread));
			_waitForm.ShowDialog(this);
			_waitForm.Dispose();
			_waitForm = null;
			wasStopped = _aviMod.WasStopped;

			SetupForm(false);

			if (_workEx != null) {
				MessageBox.Show(this, _workEx.Message, "Error", MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}

			if (!wasStopped) {
				MessageBox.Show(this, "Video has been saved successfully.", "Done",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		}

		private void SaveDestThread() {
			_workEx = null;
			try {
				_aviMod.ProgressCallback = _waitForm.SetProgress;
				_aviMod.Write(_destPath);
			}
			catch (Exception ex) {
				_workEx = ex;
			}
			_waitForm.FinishedWork();
		}

		// DAR = Display Aspect Ratio
		// PAR = Pixel Aspect Ratio
		// RAR = Resolution Aspect Ratio

		// D = R * P
		// P = D / R
		// R = D / P

		private MPEG4PAR GetMPEG4PARFromForm() {
			RadioButton rb;
			string tag, val;
			MPEG4PAR mp4PAR = new MPEG4PAR();

			foreach (Control c in grpAspectRatio.Controls) {
				if ((rb = c as RadioButton) != null) {
					if (rb.Checked) {
						tag = (string)rb.Tag;
						val = tag.Substring(4);
						if (tag.StartsWith("PAR")) {
							// PAR
							if (val == "Custom") {
								double par = Double.Parse(txtPAR_Width.Text) /
									Double.Parse(txtPAR_Height.Text);
								mp4PAR.SetCustomPAR(par);
							}
							else {
								mp4PAR.Type = (PARType)UInt32.Parse(val);
							}
						}
						else {
							// DAR
							double dar, rar, par;
							if (val == "Custom") {
								dar = Double.Parse(txtDAR_Width.Text) /
									Double.Parse(txtDAR_Height.Text);
							}
							else {
								string[] split = val.Split(':');
								dar = Double.Parse(split[0]) / Double.Parse(split[1]);
							}
							rar = (double)_mp4Mod.FrameWidth / (double)_mp4Mod.FrameHeight;
							par = dar / rar;
							mp4PAR.SetCustomPAR(par);
						}
						break;
					}
				}
			}

			return mp4PAR;
		}

		private List<MPEG4UserData> UDListBoxItems {
			get {
				List<MPEG4UserData> udList = new List<MPEG4UserData>();
				foreach (MPEG4UserData item in lstUserData.Items) {
					udList.Add(item);
				}
				return udList;
			}
			set {
				lstUserData.Items.Clear();
				foreach (MPEG4UserData item in value) {
					lstUserData.Items.Add(item);
				}
			}
		}

		private void SwitchUserData() {
			bool showAutoUD = chkChangePacking.Checked && chkAutoUD.Checked;

			if (_showingAutoUD) {
				_autoUDList = UDListBoxItems;
			}
			else {
				_regUDList = UDListBoxItems;
			}

			UDListBoxItems = showAutoUD ? _autoUDList : _regUDList;
			_showingAutoUD = showAutoUD;
		}

		private void frmMPEG4Modifier_Load(object sender, EventArgs e) {
			SetupForm(false);
		}

		private void frmMPEG4Modifier_Shown(object sender, EventArgs e) {
			// If a valid path was passed as a command-line argument, load it
			if (_args.Length == 1) {
				if (File.Exists(_args[0])) {
					LoadSource(_args[0]);
				}
			}
		}

		private void btnBrowseSource_Click(object sender, EventArgs e) {
			using (OpenFileDialog fileDlg = new OpenFileDialog()) {
				fileDlg.Title = "Source Video";
				fileDlg.Filter = "AVI Files (*.avi;*.divx)|*.avi;*.divx";
				if (_sourcePath != null) {
					fileDlg.InitialDirectory = Path.GetDirectoryName(_sourcePath);
				}

				if (fileDlg.ShowDialog(this) != DialogResult.OK) return;

				LoadSource(fileDlg.FileName);
			}
		}

		private void txtSourcePath_DragEnter(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void txtSourcePath_DragDrop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				BeginLoadSource(files[0]);
			}
		}

		private void btnSave_Click(object sender, EventArgs e) {
			using (SaveFileDialog fileDlg = new SaveFileDialog()) {
				fileDlg.Title = "Destination Video";
				fileDlg.Filter = "AVI Files (*.avi;*.divx)|*.avi;*.divx";
				fileDlg.InitialDirectory = Path.GetDirectoryName(_destPath ?? _sourcePath);
				fileDlg.FileName = Path.GetFileName(_sourcePath);

				if (fileDlg.ShowDialog(this) != DialogResult.OK) return;

				SaveDest(fileDlg.FileName);
			}
		}

		private void btnVideoInfo_Click(object sender, EventArgs e) {
			using (frmVideoInfo infoForm = new frmVideoInfo(_mp4Mod)) {
				infoForm.ShowDialog(this);
			}
		}

		private void chkUnpack_CheckedChanged(object sender, EventArgs e) {
			if (_updatingUI) return;

			_mp4Mod.NewIsPacked = _mp4Mod.IsPacked ^ chkChangePacking.Checked;
			_autoUDList = _mp4Mod.SuggestedUserData();
			chkAutoUD.Enabled = chkChangePacking.Checked;
			SwitchUserData();
		}

		private void chkAutoUD_CheckedChanged(object sender, EventArgs e) {
			if (_updatingUI) return;

			SwitchUserData();
		}

		private void btnUDEdit_Click(object sender, EventArgs e) {
			int index = lstUserData.SelectedIndex;

			if (index != -1) {
				using (frmUserData udForm = new frmUserData("Edit")) {
					MPEG4UserData ud = (MPEG4UserData)lstUserData.SelectedItem;
					udForm.UserDataString = ud.ToString();

					if (udForm.ShowDialog(this) != DialogResult.OK) return;

					ud.SetString(udForm.UserDataString);
					lstUserData.Items[index] = ud;
				}
			}
		}

		private void btnUDAdd_Click(object sender, EventArgs e) {
			using (frmUserData udForm = new frmUserData("Add")) {
				if (udForm.ShowDialog(this) != DialogResult.OK) return;

				MPEG4UserData ud = new MPEG4UserData();
				ud.UserData = Encoding.ASCII.GetBytes(udForm.UserDataString);
				lstUserData.Items.Add(ud);
			}
		}

		private void btnUDRemove_Click(object sender, EventArgs e) {
			int index = lstUserData.SelectedIndex;

			if (index != -1) {
				lstUserData.Items.RemoveAt(index);
			}
		}

		private void frmMPEG4Modifier_DoubleClick(object sender, EventArgs e) {
			if (_workEx != null) {
				MessageBox.Show(this, _workEx.StackTrace, "Stack Trace of Last Error",
					MessageBoxButtons.OK, MessageBoxIcon.None);
			}
		}

		private void btnAbout_Click(object sender, EventArgs e) {
			string text = String.Format("MPEG4 Modifier v{1}{0}Copyright {2} J.D. Purcell{0}{3}",
				Environment.NewLine,
				VersionInfo.DisplayVersion,
				VersionInfo.CopyrightYears,
				VersionInfo.Website);
			MessageBox.Show(this, text, "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}
