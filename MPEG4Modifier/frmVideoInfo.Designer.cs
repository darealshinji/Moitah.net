namespace JDP {
	partial class frmVideoInfo {
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
			this.btnWriteFrameList = new System.Windows.Forms.Button();
			this.txtInfo = new System.Windows.Forms.TextBox();
			this.btnSaveQuantMatrix = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnWriteFrameList
			// 
			this.btnWriteFrameList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnWriteFrameList.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnWriteFrameList.Location = new System.Drawing.Point(8, 394);
			this.btnWriteFrameList.Name = "btnWriteFrameList";
			this.btnWriteFrameList.Size = new System.Drawing.Size(120, 23);
			this.btnWriteFrameList.TabIndex = 1;
			this.btnWriteFrameList.Text = "Write Frame List...";
			this.btnWriteFrameList.Click += new System.EventHandler(this.btnWriteFrameList_Click);
			// 
			// txtInfo
			// 
			this.txtInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.txtInfo.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.txtInfo.Location = new System.Drawing.Point(8, 8);
			this.txtInfo.Multiline = true;
			this.txtInfo.Name = "txtInfo";
			this.txtInfo.ReadOnly = true;
			this.txtInfo.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.txtInfo.Size = new System.Drawing.Size(436, 378);
			this.txtInfo.TabIndex = 0;
			// 
			// btnSaveQuantMatrix
			// 
			this.btnSaveQuantMatrix.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnSaveQuantMatrix.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnSaveQuantMatrix.Location = new System.Drawing.Point(136, 394);
			this.btnSaveQuantMatrix.Name = "btnSaveQuantMatrix";
			this.btnSaveQuantMatrix.Size = new System.Drawing.Size(136, 23);
			this.btnSaveQuantMatrix.TabIndex = 2;
			this.btnSaveQuantMatrix.Text = "Save Quant Matrix...";
			this.btnSaveQuantMatrix.Click += new System.EventHandler(this.btnSaveQuantMatrix_Click);
			// 
			// frmVideoInfo
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(452, 425);
			this.Controls.Add(this.btnSaveQuantMatrix);
			this.Controls.Add(this.btnWriteFrameList);
			this.Controls.Add(this.txtInfo);
			this.Name = "frmVideoInfo";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Video Information";
			this.Load += new System.EventHandler(this.frmVideoInfo_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnWriteFrameList;
		private System.Windows.Forms.TextBox txtInfo;
        private System.Windows.Forms.Button btnSaveQuantMatrix;
	}
}