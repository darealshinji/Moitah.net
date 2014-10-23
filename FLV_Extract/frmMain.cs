using System;
using System.Threading;
using System.Windows.Forms;

namespace JDP {
	public partial class frmMain : Form {
		Thread _statusThread;

		public frmMain() {
			InitializeComponent();
			Program.SetFontAndScaling(this);
		}

		private void LoadSettings() {
			SettingsReader sr = new SettingsReader("FLV Extract", "settings.txt");
			string val;

			if ((val = sr.Load("ExtractVideo")) != null) {
				chkVideo.Checked = (val != "0");
			}
			if ((val = sr.Load("ExtractTimeCodes")) != null) {
				chkTimeCodes.Checked = (val != "0");
			}
			if ((val = sr.Load("ExtractAudio")) != null) {
				chkAudio.Checked = (val != "0");
			}
		}

		private void SaveSettings() {
			SettingsWriter sw = new SettingsWriter("FLV Extract", "settings.txt");

			sw.Save("ExtractVideo", chkVideo.Checked ? "1" : "0");
			sw.Save("ExtractTimeCodes", chkTimeCodes.Checked ? "1" : "0");
			sw.Save("ExtractAudio", chkAudio.Checked ? "1" : "0");

			sw.Close();
		}

		private void btnAbout_Click(object sender, EventArgs e) {
			MessageBox.Show(this, String.Format("FLV Extract v{1}{0}Copyright 2006-2012 J.D. Purcell{0}" +
				"http://www.moitah.net/", Environment.NewLine, General.Version), "About",
				MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void frmMain_DragEnter(object sender, DragEventArgs e) {
			if ((_statusThread != null) && _statusThread.IsAlive) return;

			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void frmMain_DragDrop(object sender, DragEventArgs e) {
			if ((_statusThread != null) && _statusThread.IsAlive) return;

			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
				_statusThread = new Thread(delegate() {
					Invoke((MethodInvoker)delegate() {
						using (frmStatus statusForm = new frmStatus(paths,
							chkVideo.Checked, chkAudio.Checked, chkTimeCodes.Checked))
						{
							bool topMost = TopMost;
							TopMost = false;
							statusForm.ShowDialog(this);
							TopMost = topMost;
						}
					});
				});
				_statusThread.Start();
			}
		}

		private void frmMain_Load(object sender, EventArgs e) {
			LoadSettings();
		}

		private void frmMain_FormClosed(object sender, FormClosedEventArgs e) {
			SaveSettings();
		}
	}
}
