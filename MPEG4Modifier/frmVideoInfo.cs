using System;
using System.IO;
using System.Windows.Forms;

namespace JDP {
	partial class frmVideoInfo : Form {
		private MPEG4FrameModifier _mp4Mod;

		public frmVideoInfo(MPEG4FrameModifier mp4Mod) {
			InitializeComponent();
			Program.SetFontAndScaling(this);
			_mp4Mod = mp4Mod;
		}

		private void frmVideoInfo_Load(object sender, EventArgs e) {
			txtInfo.Text = _mp4Mod.GenerateStats();
			txtInfo.SelectionStart = 0;
			btnSaveQuantMatrix.Visible = _mp4Mod.HasCustomIntraAndInterQuantMatrices;
		}

		private void btnWriteFrameList_Click(object sender, EventArgs e) {
			using (SaveFileDialog fileDlg = new SaveFileDialog()) {
				fileDlg.Title = "Write Frame List";
				fileDlg.Filter = "Text Files (*.txt)|*.txt";

				if (fileDlg.ShowDialog(this) != DialogResult.OK) return;

				try {
					using (StreamWriter sw = new StreamWriter(fileDlg.FileName)) {
						_mp4Mod.DumpFrameList(sw);
					}

					MessageBox.Show(this, "Done writing frame list.", "Done",
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch {
					MessageBox.Show(this, "Unable to write frame list.", "Error",
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void btnSaveQuantMatrix_Click(object sender, EventArgs e) {
			using (SaveFileDialog fileDlg = new SaveFileDialog()) {
				fileDlg.Title = "Save Quantization Matrix";
				fileDlg.Filter = "Custom Quantization Matrix (*.cqm)|*.cqm";

				if (fileDlg.ShowDialog(this) != DialogResult.OK) return;

				try {
					using (FileStream fs = new FileStream(fileDlg.FileName, FileMode.Create, FileAccess.Write)) {
						fs.Write(_mp4Mod.CustomIntraAndInterQuantMatrices, 0, 128);
					}

					MessageBox.Show(this, "Done saving quantization matrix.", "Done",
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				catch {
					MessageBox.Show(this, "Unable to save quantization matrix.", "Error",
						MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}
	}
}
