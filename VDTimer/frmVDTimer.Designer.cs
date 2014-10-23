namespace VDTimer {
	partial class frmVDTimer {
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
			this.grpSchedule = new System.Windows.Forms.GroupBox();
			this.btnClear = new System.Windows.Forms.Button();
			this.btnEdit = new System.Windows.Forms.Button();
			this.btnRemove = new System.Windows.Forms.Button();
			this.btnAdd = new System.Windows.Forms.Button();
			this.lvwTimes = new System.Windows.Forms.ListView();
			this.chStartTime = new System.Windows.Forms.ColumnHeader();
			this.chStopTime = new System.Windows.Forms.ColumnHeader();
			this.chComment = new System.Windows.Forms.ColumnHeader();
			this.chkEnableTimer = new System.Windows.Forms.CheckBox();
			this.lblCaptureWindowFoundDesc = new System.Windows.Forms.Label();
			this.lblCaptureWindowFound = new System.Windows.Forms.Label();
			this.grpSchedule.SuspendLayout();
			this.SuspendLayout();
			// 
			// grpSchedule
			// 
			this.grpSchedule.Controls.Add(this.btnClear);
			this.grpSchedule.Controls.Add(this.btnEdit);
			this.grpSchedule.Controls.Add(this.btnRemove);
			this.grpSchedule.Controls.Add(this.btnAdd);
			this.grpSchedule.Controls.Add(this.lvwTimes);
			this.grpSchedule.Location = new System.Drawing.Point(8, 4);
			this.grpSchedule.Name = "grpSchedule";
			this.grpSchedule.Size = new System.Drawing.Size(428, 159);
			this.grpSchedule.TabIndex = 0;
			this.grpSchedule.TabStop = false;
			this.grpSchedule.Text = "Schedule";
			// 
			// btnClear
			// 
			this.btnClear.Location = new System.Drawing.Point(208, 124);
			this.btnClear.Name = "btnClear";
			this.btnClear.Size = new System.Drawing.Size(56, 23);
			this.btnClear.TabIndex = 4;
			this.btnClear.Text = "Clear";
			this.btnClear.UseVisualStyleBackColor = true;
			this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
			// 
			// btnEdit
			// 
			this.btnEdit.Location = new System.Drawing.Point(72, 124);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.Size = new System.Drawing.Size(52, 23);
			this.btnEdit.TabIndex = 2;
			this.btnEdit.Text = "Edit";
			this.btnEdit.UseVisualStyleBackColor = true;
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
			// 
			// btnRemove
			// 
			this.btnRemove.Location = new System.Drawing.Point(132, 124);
			this.btnRemove.Name = "btnRemove";
			this.btnRemove.Size = new System.Drawing.Size(68, 23);
			this.btnRemove.TabIndex = 3;
			this.btnRemove.Text = "Remove";
			this.btnRemove.UseVisualStyleBackColor = true;
			this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
			// 
			// btnAdd
			// 
			this.btnAdd.Location = new System.Drawing.Point(12, 124);
			this.btnAdd.Name = "btnAdd";
			this.btnAdd.Size = new System.Drawing.Size(52, 23);
			this.btnAdd.TabIndex = 1;
			this.btnAdd.Text = "Add";
			this.btnAdd.UseVisualStyleBackColor = true;
			this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
			// 
			// lvwTimes
			// 
			this.lvwTimes.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.chStartTime,
            this.chStopTime,
            this.chComment});
			this.lvwTimes.FullRowSelect = true;
			this.lvwTimes.GridLines = true;
			this.lvwTimes.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvwTimes.HideSelection = false;
			this.lvwTimes.LabelWrap = false;
			this.lvwTimes.Location = new System.Drawing.Point(12, 20);
			this.lvwTimes.Name = "lvwTimes";
			this.lvwTimes.Size = new System.Drawing.Size(404, 96);
			this.lvwTimes.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.lvwTimes.TabIndex = 0;
			this.lvwTimes.UseCompatibleStateImageBehavior = false;
			this.lvwTimes.View = System.Windows.Forms.View.Details;
			// 
			// chStartTime
			// 
			this.chStartTime.Text = "Start Time";
			this.chStartTime.Width = 124;
			// 
			// chStopTime
			// 
			this.chStopTime.Text = "Stop Time";
			this.chStopTime.Width = 124;
			// 
			// chComment
			// 
			this.chComment.Text = "Comment";
			this.chComment.Width = 152;
			// 
			// chkEnableTimer
			// 
			this.chkEnableTimer.AutoSize = true;
			this.chkEnableTimer.Location = new System.Drawing.Point(8, 170);
			this.chkEnableTimer.Name = "chkEnableTimer";
			this.chkEnableTimer.Size = new System.Drawing.Size(87, 17);
			this.chkEnableTimer.TabIndex = 1;
			this.chkEnableTimer.Text = "Enable Timer";
			this.chkEnableTimer.UseVisualStyleBackColor = true;
			this.chkEnableTimer.CheckedChanged += new System.EventHandler(this.chkEnableTimer_CheckedChanged);
			// 
			// lblCaptureWindowFoundDesc
			// 
			this.lblCaptureWindowFoundDesc.Location = new System.Drawing.Point(236, 172);
			this.lblCaptureWindowFoundDesc.Name = "lblCaptureWindowFoundDesc";
			this.lblCaptureWindowFoundDesc.Size = new System.Drawing.Size(176, 13);
			this.lblCaptureWindowFoundDesc.TabIndex = 2;
			this.lblCaptureWindowFoundDesc.Text = "VirtualDub Capture Window Found:";
			this.lblCaptureWindowFoundDesc.Visible = false;
			// 
			// lblCaptureWindowFound
			// 
			this.lblCaptureWindowFound.Location = new System.Drawing.Point(412, 172);
			this.lblCaptureWindowFound.Name = "lblCaptureWindowFound";
			this.lblCaptureWindowFound.Size = new System.Drawing.Size(24, 13);
			this.lblCaptureWindowFound.TabIndex = 3;
			this.lblCaptureWindowFound.Text = "...";
			this.lblCaptureWindowFound.Visible = false;
			// 
			// frmVDTimer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(444, 192);
			this.Controls.Add(this.lblCaptureWindowFound);
			this.Controls.Add(this.lblCaptureWindowFoundDesc);
			this.Controls.Add(this.chkEnableTimer);
			this.Controls.Add(this.grpSchedule);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "frmVDTimer";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "VDTimer";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmVDTimer_FormClosed);
			this.Load += new System.EventHandler(this.frmVDTimer_Load);
			this.grpSchedule.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox grpSchedule;
		private System.Windows.Forms.Button btnEdit;
		private System.Windows.Forms.Button btnRemove;
		private System.Windows.Forms.Button btnAdd;
		private System.Windows.Forms.ListView lvwTimes;
		private System.Windows.Forms.ColumnHeader chStartTime;
		private System.Windows.Forms.ColumnHeader chStopTime;
		private System.Windows.Forms.Button btnClear;
		private System.Windows.Forms.CheckBox chkEnableTimer;
		private System.Windows.Forms.Label lblCaptureWindowFoundDesc;
		private System.Windows.Forms.ColumnHeader chComment;
		private System.Windows.Forms.Label lblCaptureWindowFound;

	}
}

