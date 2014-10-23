// ****************************************************************************
// 
// LineDrop
// Copyright (C) 2005  Moitah (moitah@yahoo.com)
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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Text;

namespace Moitah {
	public class frmLineDrop : System.Windows.Forms.Form {
		private System.Windows.Forms.GroupBox grpOutputLineEnds;
		private System.Windows.Forms.RadioButton rbCR;
		private System.Windows.Forms.RadioButton rbLF;
		private System.Windows.Forms.RadioButton rbCRLF;
		private System.Windows.Forms.GroupBox grpOptions;
		private System.Windows.Forms.CheckBox chkOneAtEnd;
		private System.Windows.Forms.CheckBox chkRemoveDupes;
		private System.ComponentModel.Container components = null;

		public frmLineDrop() {
			InitializeComponent();

			// Add any constructor code here
		}

		protected override void Dispose( bool disposing ) {
			if( disposing ) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.grpOutputLineEnds = new System.Windows.Forms.GroupBox();
			this.rbCR = new System.Windows.Forms.RadioButton();
			this.rbLF = new System.Windows.Forms.RadioButton();
			this.rbCRLF = new System.Windows.Forms.RadioButton();
			this.grpOptions = new System.Windows.Forms.GroupBox();
			this.chkOneAtEnd = new System.Windows.Forms.CheckBox();
			this.chkRemoveDupes = new System.Windows.Forms.CheckBox();
			this.grpOutputLineEnds.SuspendLayout();
			this.grpOptions.SuspendLayout();
			this.SuspendLayout();
			// 
			// grpOutputLineEnds
			// 
			this.grpOutputLineEnds.Controls.Add(this.rbCR);
			this.grpOutputLineEnds.Controls.Add(this.rbLF);
			this.grpOutputLineEnds.Controls.Add(this.rbCRLF);
			this.grpOutputLineEnds.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.grpOutputLineEnds.Location = new System.Drawing.Point(8, 4);
			this.grpOutputLineEnds.Name = "grpOutputLineEnds";
			this.grpOutputLineEnds.Size = new System.Drawing.Size(172, 84);
			this.grpOutputLineEnds.TabIndex = 0;
			this.grpOutputLineEnds.TabStop = false;
			this.grpOutputLineEnds.Text = "Output Line Ends:";
			// 
			// rbCR
			// 
			this.rbCR.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbCR.Location = new System.Drawing.Point(8, 60);
			this.rbCR.Name = "rbCR";
			this.rbCR.Size = new System.Drawing.Size(72, 16);
			this.rbCR.TabIndex = 2;
			this.rbCR.Text = "CR (Mac)";
			// 
			// rbLF
			// 
			this.rbLF.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbLF.Location = new System.Drawing.Point(8, 40);
			this.rbLF.Name = "rbLF";
			this.rbLF.Size = new System.Drawing.Size(72, 16);
			this.rbLF.TabIndex = 1;
			this.rbLF.Text = "LF (Unix)";
			// 
			// rbCRLF
			// 
			this.rbCRLF.Checked = true;
			this.rbCRLF.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbCRLF.Location = new System.Drawing.Point(8, 20);
			this.rbCRLF.Name = "rbCRLF";
			this.rbCRLF.Size = new System.Drawing.Size(120, 16);
			this.rbCRLF.TabIndex = 0;
			this.rbCRLF.TabStop = true;
			this.rbCRLF.Text = "CR+LF (Windows)";
			// 
			// grpOptions
			// 
			this.grpOptions.Controls.Add(this.chkOneAtEnd);
			this.grpOptions.Controls.Add(this.chkRemoveDupes);
			this.grpOptions.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.grpOptions.Location = new System.Drawing.Point(8, 92);
			this.grpOptions.Name = "grpOptions";
			this.grpOptions.Size = new System.Drawing.Size(172, 64);
			this.grpOptions.TabIndex = 1;
			this.grpOptions.TabStop = false;
			this.grpOptions.Text = "Options:";
			// 
			// chkOneAtEnd
			// 
			this.chkOneAtEnd.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.chkOneAtEnd.Location = new System.Drawing.Point(10, 40);
			this.chkOneAtEnd.Name = "chkOneAtEnd";
			this.chkOneAtEnd.Size = new System.Drawing.Size(138, 16);
			this.chkOneAtEnd.TabIndex = 1;
			this.chkOneAtEnd.Text = "One line end at file end";
			// 
			// chkRemoveDupes
			// 
			this.chkRemoveDupes.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.chkRemoveDupes.Location = new System.Drawing.Point(10, 20);
			this.chkRemoveDupes.Name = "chkRemoveDupes";
			this.chkRemoveDupes.Size = new System.Drawing.Size(154, 16);
			this.chkRemoveDupes.TabIndex = 0;
			this.chkRemoveDupes.Text = "Remove duplicate line ends";
			// 
			// frmLineDrop
			// 
			this.AllowDrop = true;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.ClientSize = new System.Drawing.Size(188, 164);
			this.Controls.Add(this.grpOptions);
			this.Controls.Add(this.grpOutputLineEnds);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "frmLineDrop";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "LineDrop";
			this.Load += new System.EventHandler(this.frmLineDrop_Load);
			this.DragDrop += new System.Windows.Forms.DragEventHandler(this.frmLineDrop_DragDrop);
			this.Closed += new System.EventHandler(this.frmLineDrop_Closed);
			this.DragEnter += new System.Windows.Forms.DragEventHandler(this.frmLineDrop_DragEnter);
			this.grpOutputLineEnds.ResumeLayout(false);
			this.grpOptions.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		[STAThread]
		static void Main() {
			Application.Run(new frmLineDrop());
		}

		private void frmLineDrop_Load(object sender, System.EventArgs e) {
			LoadConfig();
		}

		private void frmLineDrop_Closed(object sender, System.EventArgs e) {
			SaveConfig();
		}

		private void frmLineDrop_DragEnter(object sender, System.Windows.Forms.DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				e.Effect = DragDropEffects.Copy;
			}
		}

		private void frmLineDrop_DragDrop(object sender, System.Windows.Forms.DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				if (_overwriteWarning) {
					if (MessageBox.Show("Convert all dropped files and folders?  All files will be " +
						"overwritten!", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) !=
						DialogResult.Yes)
					{
						return;
					}
				}

				string[] drops = (string[])e.Data.GetData(DataFormats.FileDrop);

				InitVars();
				
				for (int i = 0; i < drops.Length; i++) {
					string drop = drops[i];

					if (Directory.Exists(drop)) {
						ConvertDirectory(drop);
					}
					else if (File.Exists(drop)) {
						ConvertFile(drop);
					}
				}

				MessageBox.Show(String.Format("Converted {0} files, there were {1} errors.",
					_successCount, _errorCount), "Done", MessageBoxButtons.OK,
					MessageBoxIcon.Information);
			}
		}

		// ********************************************************************************

		byte[] _lineEnd;
		bool _oneAtEnd, _removeDupes, _overwriteWarning;
		int _successCount, _errorCount;

		private void LoadConfig() {
			string path = Path.Combine(Application.StartupPath, "config.txt");
			StreamReader sr;
			string line, name, val;
			string[] lineSplit;

			_overwriteWarning = true;

			try {
				sr = new StreamReader(path, Encoding.ASCII);

				while ((line = sr.ReadLine()) != null) {
					lineSplit = line.Split(new char[] { ':' }, 2);
					if (lineSplit.Length == 2) {
						name = lineSplit[0].Trim();
						val = lineSplit[1].Trim();

						switch (name.ToLower()) {
							case "overwritewarning":
								SetBool(ref _overwriteWarning, val);
								break;
						}
					}
				}

				sr.Close();
			}
			catch {
			}
		}

		private void SetBool(ref bool myBool, string val) {
			val = val.ToLower();
			if (val == "true") {
				myBool = true;
			}
			else if (val == "false") {
				myBool = false;
			}
		}

		private void SaveConfig() {
			string path = Path.Combine(Application.StartupPath, "config.txt");
			StreamWriter sw;

			try {
				sw = new StreamWriter(path, false, Encoding.ASCII);

				sw.WriteLine("OverwriteWarning: {0}", BoolToString(_overwriteWarning));

				sw.Close();
			}
			catch {
			}
		}

		private string BoolToString(bool myBool) {
			return myBool ? "true" : "false";
		}

		private void InitVars() {
			if (rbLF.Checked) {
				_lineEnd = new byte[] { 10 };
			}
			else if (rbCR.Checked) {
				_lineEnd = new byte[] { 13 };
			}
			else {
				_lineEnd = new byte[] { 13, 10 };
			}
			_oneAtEnd = chkOneAtEnd.Checked;
			_removeDupes = chkRemoveDupes.Checked;
			_successCount = 0;
			_errorCount = 0;
		}

		private void ConvertDirectory(string dir) {
			string[] files = Directory.GetFiles(dir);
			string[] subDirs = Directory.GetDirectories(dir);
			int i;

			for (i = 0; i < files.Length; i++) {
				ConvertFile(files[i]);
			}

			for (i = 0; i < subDirs.Length; i++) {
				ConvertDirectory(subDirs[i]);
			}
		}

		private void ConvertFile(string path) {
			try {
				LineEndConverter.ConvertFile(path, _lineEnd, _oneAtEnd, _removeDupes);
				_successCount++;
			}
			catch {
				_errorCount++;
			}
		}
	}
}