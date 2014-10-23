namespace JDP {
	partial class frmSettings {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.grpGeneral = new System.Windows.Forms.GroupBox();
			this.chkAutoCorrectFilenames = new System.Windows.Forms.CheckBox();
			this.chkPreserveHTOA = new System.Windows.Forms.CheckBox();
			this.txtWriteOffset = new System.Windows.Forms.TextBox();
			this.lblWriteOffset = new System.Windows.Forms.Label();
			this.grpFLAC = new System.Windows.Forms.GroupBox();
			this.txtFLACCompressionLevel = new System.Windows.Forms.TextBox();
			this.lblFLACCompressionLevel = new System.Windows.Forms.Label();
			this.chkFLACVerify = new System.Windows.Forms.CheckBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.grpWavPack = new System.Windows.Forms.GroupBox();
			this.txtWVExtraMode = new System.Windows.Forms.TextBox();
			this.chkWVExtraMode = new System.Windows.Forms.CheckBox();
			this.rbWVVeryHigh = new System.Windows.Forms.RadioButton();
			this.rbWVHigh = new System.Windows.Forms.RadioButton();
			this.rbWVNormal = new System.Windows.Forms.RadioButton();
			this.rbWVFast = new System.Windows.Forms.RadioButton();
			this.grpGeneral.SuspendLayout();
			this.grpFLAC.SuspendLayout();
			this.grpWavPack.SuspendLayout();
			this.SuspendLayout();
			// 
			// grpGeneral
			// 
			this.grpGeneral.Controls.Add(this.chkAutoCorrectFilenames);
			this.grpGeneral.Controls.Add(this.chkPreserveHTOA);
			this.grpGeneral.Controls.Add(this.txtWriteOffset);
			this.grpGeneral.Controls.Add(this.lblWriteOffset);
			this.grpGeneral.Location = new System.Drawing.Point(8, 4);
			this.grpGeneral.Name = "grpGeneral";
			this.grpGeneral.Size = new System.Drawing.Size(256, 112);
			this.grpGeneral.TabIndex = 0;
			this.grpGeneral.TabStop = false;
			this.grpGeneral.Text = "General";
			// 
			// chkAutoCorrectFilenames
			// 
			this.chkAutoCorrectFilenames.Location = new System.Drawing.Point(12, 68);
			this.chkAutoCorrectFilenames.Name = "chkAutoCorrectFilenames";
			this.chkAutoCorrectFilenames.Size = new System.Drawing.Size(232, 36);
			this.chkAutoCorrectFilenames.TabIndex = 3;
			this.chkAutoCorrectFilenames.Text = "Preprocess with filename corrector if unable to locate audio files";
			this.chkAutoCorrectFilenames.UseVisualStyleBackColor = true;
			// 
			// chkPreserveHTOA
			// 
			this.chkPreserveHTOA.AutoSize = true;
			this.chkPreserveHTOA.Location = new System.Drawing.Point(12, 48);
			this.chkPreserveHTOA.Name = "chkPreserveHTOA";
			this.chkPreserveHTOA.Size = new System.Drawing.Size(229, 17);
			this.chkPreserveHTOA.TabIndex = 2;
			this.chkPreserveHTOA.Text = "Preserve HTOA for gaps appended output";
			this.chkPreserveHTOA.UseVisualStyleBackColor = true;
			// 
			// txtWriteOffset
			// 
			this.txtWriteOffset.Location = new System.Drawing.Point(136, 20);
			this.txtWriteOffset.Name = "txtWriteOffset";
			this.txtWriteOffset.Size = new System.Drawing.Size(56, 21);
			this.txtWriteOffset.TabIndex = 1;
			this.txtWriteOffset.Text = "0";
			// 
			// lblWriteOffset
			// 
			this.lblWriteOffset.AutoSize = true;
			this.lblWriteOffset.Location = new System.Drawing.Point(10, 24);
			this.lblWriteOffset.Name = "lblWriteOffset";
			this.lblWriteOffset.Size = new System.Drawing.Size(118, 13);
			this.lblWriteOffset.TabIndex = 0;
			this.lblWriteOffset.Text = "Write offset (samples):";
			// 
			// grpFLAC
			// 
			this.grpFLAC.Controls.Add(this.txtFLACCompressionLevel);
			this.grpFLAC.Controls.Add(this.lblFLACCompressionLevel);
			this.grpFLAC.Controls.Add(this.chkFLACVerify);
			this.grpFLAC.Location = new System.Drawing.Point(8, 120);
			this.grpFLAC.Name = "grpFLAC";
			this.grpFLAC.Size = new System.Drawing.Size(256, 76);
			this.grpFLAC.TabIndex = 1;
			this.grpFLAC.TabStop = false;
			this.grpFLAC.Text = "FLAC";
			// 
			// txtFLACCompressionLevel
			// 
			this.txtFLACCompressionLevel.Location = new System.Drawing.Point(144, 20);
			this.txtFLACCompressionLevel.Name = "txtFLACCompressionLevel";
			this.txtFLACCompressionLevel.Size = new System.Drawing.Size(28, 21);
			this.txtFLACCompressionLevel.TabIndex = 1;
			this.txtFLACCompressionLevel.Text = "5";
			// 
			// lblFLACCompressionLevel
			// 
			this.lblFLACCompressionLevel.AutoSize = true;
			this.lblFLACCompressionLevel.Location = new System.Drawing.Point(10, 24);
			this.lblFLACCompressionLevel.Name = "lblFLACCompressionLevel";
			this.lblFLACCompressionLevel.Size = new System.Drawing.Size(124, 13);
			this.lblFLACCompressionLevel.TabIndex = 0;
			this.lblFLACCompressionLevel.Text = "Compression level (0-8):";
			// 
			// chkFLACVerify
			// 
			this.chkFLACVerify.AutoSize = true;
			this.chkFLACVerify.Location = new System.Drawing.Point(12, 48);
			this.chkFLACVerify.Name = "chkFLACVerify";
			this.chkFLACVerify.Size = new System.Drawing.Size(54, 17);
			this.chkFLACVerify.TabIndex = 2;
			this.chkFLACVerify.Text = "Verify";
			this.chkFLACVerify.UseVisualStyleBackColor = true;
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(204, 308);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(60, 23);
			this.btnOK.TabIndex = 3;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			// 
			// grpWavPack
			// 
			this.grpWavPack.Controls.Add(this.txtWVExtraMode);
			this.grpWavPack.Controls.Add(this.chkWVExtraMode);
			this.grpWavPack.Controls.Add(this.rbWVVeryHigh);
			this.grpWavPack.Controls.Add(this.rbWVHigh);
			this.grpWavPack.Controls.Add(this.rbWVNormal);
			this.grpWavPack.Controls.Add(this.rbWVFast);
			this.grpWavPack.Location = new System.Drawing.Point(8, 200);
			this.grpWavPack.Name = "grpWavPack";
			this.grpWavPack.Size = new System.Drawing.Size(256, 100);
			this.grpWavPack.TabIndex = 2;
			this.grpWavPack.TabStop = false;
			this.grpWavPack.Text = "WavPack";
			// 
			// txtWVExtraMode
			// 
			this.txtWVExtraMode.Location = new System.Drawing.Point(132, 68);
			this.txtWVExtraMode.Name = "txtWVExtraMode";
			this.txtWVExtraMode.Size = new System.Drawing.Size(28, 21);
			this.txtWVExtraMode.TabIndex = 5;
			this.txtWVExtraMode.Text = "0";
			// 
			// chkWVExtraMode
			// 
			this.chkWVExtraMode.AutoSize = true;
			this.chkWVExtraMode.Location = new System.Drawing.Point(12, 68);
			this.chkWVExtraMode.Name = "chkWVExtraMode";
			this.chkWVExtraMode.Size = new System.Drawing.Size(112, 17);
			this.chkWVExtraMode.TabIndex = 4;
			this.chkWVExtraMode.Text = "Extra mode (1-6):";
			this.chkWVExtraMode.UseVisualStyleBackColor = true;
			this.chkWVExtraMode.CheckedChanged += new System.EventHandler(this.chkWVExtraMode_CheckedChanged);
			// 
			// rbWVVeryHigh
			// 
			this.rbWVVeryHigh.AutoSize = true;
			this.rbWVVeryHigh.Location = new System.Drawing.Point(88, 40);
			this.rbWVVeryHigh.Name = "rbWVVeryHigh";
			this.rbWVVeryHigh.Size = new System.Drawing.Size(71, 17);
			this.rbWVVeryHigh.TabIndex = 3;
			this.rbWVVeryHigh.Text = "Very High";
			this.rbWVVeryHigh.UseVisualStyleBackColor = true;
			// 
			// rbWVHigh
			// 
			this.rbWVHigh.AutoSize = true;
			this.rbWVHigh.Location = new System.Drawing.Point(88, 20);
			this.rbWVHigh.Name = "rbWVHigh";
			this.rbWVHigh.Size = new System.Drawing.Size(46, 17);
			this.rbWVHigh.TabIndex = 1;
			this.rbWVHigh.Text = "High";
			this.rbWVHigh.UseVisualStyleBackColor = true;
			// 
			// rbWVNormal
			// 
			this.rbWVNormal.AutoSize = true;
			this.rbWVNormal.Checked = true;
			this.rbWVNormal.Location = new System.Drawing.Point(12, 40);
			this.rbWVNormal.Name = "rbWVNormal";
			this.rbWVNormal.Size = new System.Drawing.Size(58, 17);
			this.rbWVNormal.TabIndex = 2;
			this.rbWVNormal.TabStop = true;
			this.rbWVNormal.Text = "Normal";
			this.rbWVNormal.UseVisualStyleBackColor = true;
			// 
			// rbWVFast
			// 
			this.rbWVFast.AutoSize = true;
			this.rbWVFast.Location = new System.Drawing.Point(12, 20);
			this.rbWVFast.Name = "rbWVFast";
			this.rbWVFast.Size = new System.Drawing.Size(46, 17);
			this.rbWVFast.TabIndex = 0;
			this.rbWVFast.Text = "Fast";
			this.rbWVFast.UseVisualStyleBackColor = true;
			// 
			// frmSettings
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(272, 340);
			this.Controls.Add(this.grpWavPack);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.grpFLAC);
			this.Controls.Add(this.grpGeneral);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "frmSettings";
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Advanced Settings";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmSettings_FormClosing);
			this.Load += new System.EventHandler(this.frmSettings_Load);
			this.grpGeneral.ResumeLayout(false);
			this.grpGeneral.PerformLayout();
			this.grpFLAC.ResumeLayout(false);
			this.grpFLAC.PerformLayout();
			this.grpWavPack.ResumeLayout(false);
			this.grpWavPack.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox grpGeneral;
		private System.Windows.Forms.CheckBox chkPreserveHTOA;
		private System.Windows.Forms.TextBox txtWriteOffset;
		private System.Windows.Forms.Label lblWriteOffset;
		private System.Windows.Forms.GroupBox grpFLAC;
		private System.Windows.Forms.TextBox txtFLACCompressionLevel;
		private System.Windows.Forms.Label lblFLACCompressionLevel;
		private System.Windows.Forms.CheckBox chkFLACVerify;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.GroupBox grpWavPack;
		private System.Windows.Forms.RadioButton rbWVVeryHigh;
		private System.Windows.Forms.RadioButton rbWVHigh;
		private System.Windows.Forms.RadioButton rbWVNormal;
		private System.Windows.Forms.RadioButton rbWVFast;
		private System.Windows.Forms.CheckBox chkWVExtraMode;
		private System.Windows.Forms.TextBox txtWVExtraMode;
		private System.Windows.Forms.CheckBox chkAutoCorrectFilenames;

	}
}