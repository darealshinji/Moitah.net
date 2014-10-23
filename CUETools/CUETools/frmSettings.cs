using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace JDP {
	public partial class frmSettings : Form {
		int _writeOffset, _flacCompressionLevel, _wvCompressionMode, _wvExtraMode;
		bool _preserveHTOA, _autoCorrectFilenames, _flacVerify;

		public frmSettings() {
			InitializeComponent();
		}

		private void frmSettings_Load(object sender, EventArgs e) {
			txtWriteOffset.Text = _writeOffset.ToString();
			chkPreserveHTOA.Checked = _preserveHTOA;
			chkAutoCorrectFilenames.Checked = _autoCorrectFilenames;
			txtFLACCompressionLevel.Text = _flacCompressionLevel.ToString();
			chkFLACVerify.Checked = _flacVerify;
			if (_wvCompressionMode == 0) rbWVFast.Checked = true;
			if (_wvCompressionMode == 1) rbWVNormal.Checked = true;
			if (_wvCompressionMode == 2) rbWVHigh.Checked = true;
			if (_wvCompressionMode == 3) rbWVVeryHigh.Checked = true;
			chkWVExtraMode.Checked = (_wvExtraMode != 0);
			chkWVExtraMode_CheckedChanged(null, null);
			if (_wvExtraMode != 0) txtWVExtraMode.Text = _wvExtraMode.ToString();
		}

		private void frmSettings_FormClosing(object sender, FormClosingEventArgs e) {
			if (!Int32.TryParse(txtWriteOffset.Text, out _writeOffset)) {
				MessageBox.Show("Invalid write offset setting.", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				e.Cancel = true;
				return;
			}
			_preserveHTOA = chkPreserveHTOA.Checked;
			_autoCorrectFilenames = chkAutoCorrectFilenames.Checked;
			if (!Int32.TryParse(txtFLACCompressionLevel.Text, out _flacCompressionLevel) ||
				(_flacCompressionLevel < 0) || (_flacCompressionLevel > 8))
			{
				MessageBox.Show("Invalid compression level setting.", "Error",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				e.Cancel = true;
				return;
			}
			_flacVerify = chkFLACVerify.Checked;
			if      (rbWVFast.Checked)	   _wvCompressionMode = 0;
			else if (rbWVHigh.Checked)	   _wvCompressionMode = 2;
			else if (rbWVVeryHigh.Checked) _wvCompressionMode = 3;
			else						   _wvCompressionMode = 1;
			if (!chkWVExtraMode.Checked) {
				_wvExtraMode = 0;
			}
			else {
				if (!Int32.TryParse(txtWVExtraMode.Text, out _wvExtraMode) ||
					(_wvExtraMode < 1) || (_wvExtraMode > 6))
				{
					MessageBox.Show("Invalid extra mode setting.", "Error",
						MessageBoxButtons.OK, MessageBoxIcon.Error);
					e.Cancel = true;
					return;
				}
			}
		}

		public int WriteOffset {
			get { return _writeOffset; }
			set { _writeOffset = value; }
		}

		public bool PreserveHTOA {
			get { return _preserveHTOA; }
			set { _preserveHTOA = value; }
		}

		public bool AutoCorrectFilenames {
			get { return _autoCorrectFilenames; }
			set { _autoCorrectFilenames = value; }
		}

		public int FLACCompressionLevel {
			get { return _flacCompressionLevel; }
			set { _flacCompressionLevel = value; }
		}

		public bool FLACVerify {
			get { return _flacVerify; }
			set { _flacVerify = value; }
		}

		public int WVCompressionMode {
			get { return _wvCompressionMode; }
			set { _wvCompressionMode = value; }
		}

		public int WVExtraMode {
			get { return _wvExtraMode; }
			set { _wvExtraMode = value; }
		}

		private void chkWVExtraMode_CheckedChanged(object sender, EventArgs e) {
			if (chkWVExtraMode.Checked) {
				txtWVExtraMode.Enabled = true;
				txtWVExtraMode.Text = "1";
			}
			else {
				txtWVExtraMode.Text = "0";
				txtWVExtraMode.Enabled = false;
			}
		}
	}
}