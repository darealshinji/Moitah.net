namespace VDTimer {
	partial class frmTimeInput {
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
			this.dtpStop = new System.Windows.Forms.DateTimePicker();
			this.dtpStart = new System.Windows.Forms.DateTimePicker();
			this.lblStart = new System.Windows.Forms.Label();
			this.lblStop = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.lblComment = new System.Windows.Forms.Label();
			this.txtComment = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// dtpStop
			// 
			this.dtpStop.CustomFormat = "yyyy-MM-dd  HH:mm:ss";
			this.dtpStop.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.dtpStop.Location = new System.Drawing.Point(68, 40);
			this.dtpStop.Name = "dtpStop";
			this.dtpStop.Size = new System.Drawing.Size(135, 21);
			this.dtpStop.TabIndex = 3;
			// 
			// dtpStart
			// 
			this.dtpStart.CustomFormat = "yyyy-MM-dd  HH:mm:ss";
			this.dtpStart.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.dtpStart.Location = new System.Drawing.Point(68, 12);
			this.dtpStart.Name = "dtpStart";
			this.dtpStart.Size = new System.Drawing.Size(135, 21);
			this.dtpStart.TabIndex = 1;
			// 
			// lblStart
			// 
			this.lblStart.AutoSize = true;
			this.lblStart.Location = new System.Drawing.Point(8, 16);
			this.lblStart.Name = "lblStart";
			this.lblStart.Size = new System.Drawing.Size(49, 13);
			this.lblStart.TabIndex = 0;
			this.lblStart.Text = "Start At:";
			// 
			// lblStop
			// 
			this.lblStop.AutoSize = true;
			this.lblStop.Location = new System.Drawing.Point(8, 44);
			this.lblStop.Name = "lblStop";
			this.lblStop.Size = new System.Drawing.Size(47, 13);
			this.lblStop.TabIndex = 2;
			this.lblStop.Text = "Stop At:";
			// 
			// btnOK
			// 
			this.btnOK.Location = new System.Drawing.Point(55, 100);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(48, 23);
			this.btnOK.TabIndex = 6;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(111, 100);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(64, 23);
			this.btnCancel.TabIndex = 7;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// lblComment
			// 
			this.lblComment.AutoSize = true;
			this.lblComment.Location = new System.Drawing.Point(8, 72);
			this.lblComment.Name = "lblComment";
			this.lblComment.Size = new System.Drawing.Size(56, 13);
			this.lblComment.TabIndex = 4;
			this.lblComment.Text = "Comment:";
			// 
			// txtComment
			// 
			this.txtComment.Location = new System.Drawing.Point(68, 68);
			this.txtComment.Name = "txtComment";
			this.txtComment.Size = new System.Drawing.Size(152, 21);
			this.txtComment.TabIndex = 5;
			// 
			// frmTimeInput
			// 
			this.AcceptButton = this.btnOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(231, 134);
			this.Controls.Add(this.txtComment);
			this.Controls.Add(this.lblComment);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.lblStop);
			this.Controls.Add(this.lblStart);
			this.Controls.Add(this.dtpStop);
			this.Controls.Add(this.dtpStart);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "frmTimeInput";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Time Input";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DateTimePicker dtpStop;
		private System.Windows.Forms.DateTimePicker dtpStart;
		private System.Windows.Forms.Label lblStart;
		private System.Windows.Forms.Label lblStop;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label lblComment;
		private System.Windows.Forms.TextBox txtComment;
	}
}