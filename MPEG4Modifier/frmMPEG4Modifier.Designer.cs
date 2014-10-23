namespace JDP {
	partial class frmMPEG4Modifier {
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
			this.grpUserData = new System.Windows.Forms.GroupBox();
			this.chkAutoUD = new System.Windows.Forms.CheckBox();
			this.btnUDEdit = new System.Windows.Forms.Button();
			this.btnUDRemove = new System.Windows.Forms.Button();
			this.btnUDAdd = new System.Windows.Forms.Button();
			this.lstUserData = new System.Windows.Forms.ListBox();
			this.chkChangePacking = new System.Windows.Forms.CheckBox();
			this.lblPackedBitstream = new System.Windows.Forms.Label();
			this.grpPackedBitstream = new System.Windows.Forms.GroupBox();
			this.lblIsPacked = new System.Windows.Forms.Label();
			this.btnAbout = new System.Windows.Forms.Button();
			this.rbBFF = new System.Windows.Forms.RadioButton();
			this.btnSave = new System.Windows.Forms.Button();
			this.rbTFF = new System.Windows.Forms.RadioButton();
			this.lblFieldOrder = new System.Windows.Forms.Label();
			this.lblIsInterlaced = new System.Windows.Forms.Label();
			this.grpAspectRatio = new System.Windows.Forms.GroupBox();
			this.rbDAR_185_1 = new System.Windows.Forms.RadioButton();
			this.lblDAR = new System.Windows.Forms.Label();
			this.lblPAR = new System.Windows.Forms.Label();
			this.txtDAR_Height = new System.Windows.Forms.TextBox();
			this.txtDAR_Width = new System.Windows.Forms.TextBox();
			this.rbDAR_235_1 = new System.Windows.Forms.RadioButton();
			this.rbDAR_Custom = new System.Windows.Forms.RadioButton();
			this.rbDAR_16_9 = new System.Windows.Forms.RadioButton();
			this.rbDAR_4_3 = new System.Windows.Forms.RadioButton();
			this.lblDAR_Colon = new System.Windows.Forms.Label();
			this.txtPAR_Height = new System.Windows.Forms.TextBox();
			this.txtPAR_Width = new System.Windows.Forms.TextBox();
			this.lblPAR_Colon = new System.Windows.Forms.Label();
			this.rbPAR_NTSC_16_9 = new System.Windows.Forms.RadioButton();
			this.rbPAR_Custom = new System.Windows.Forms.RadioButton();
			this.rbPAR_PAL_16_9 = new System.Windows.Forms.RadioButton();
			this.rbPAR_PAL_4_3 = new System.Windows.Forms.RadioButton();
			this.rbPAR_NTSC_4_3 = new System.Windows.Forms.RadioButton();
			this.rbPAR_VGA_1_1 = new System.Windows.Forms.RadioButton();
			this.btnBrowseSource = new System.Windows.Forms.Button();
			this.lblInterlaced = new System.Windows.Forms.Label();
			this.lblSource = new System.Windows.Forms.Label();
			this.grpInterlacing = new System.Windows.Forms.GroupBox();
			this.btnVideoInfo = new System.Windows.Forms.Button();
			this.txtSourcePath = new System.Windows.Forms.TextBox();
			this.grpUserData.SuspendLayout();
			this.grpPackedBitstream.SuspendLayout();
			this.grpAspectRatio.SuspendLayout();
			this.grpInterlacing.SuspendLayout();
			this.SuspendLayout();
			// 
			// grpUserData
			// 
			this.grpUserData.Controls.Add(this.chkAutoUD);
			this.grpUserData.Controls.Add(this.btnUDEdit);
			this.grpUserData.Controls.Add(this.btnUDRemove);
			this.grpUserData.Controls.Add(this.btnUDAdd);
			this.grpUserData.Controls.Add(this.lstUserData);
			this.grpUserData.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.grpUserData.Location = new System.Drawing.Point(284, 116);
			this.grpUserData.Name = "grpUserData";
			this.grpUserData.Size = new System.Drawing.Size(148, 140);
			this.grpUserData.TabIndex = 5;
			this.grpUserData.TabStop = false;
			this.grpUserData.Text = "User Data";
			// 
			// chkAutoUD
			// 
			this.chkAutoUD.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.chkAutoUD.Location = new System.Drawing.Point(12, 20);
			this.chkAutoUD.Name = "chkAutoUD";
			this.chkAutoUD.Size = new System.Drawing.Size(56, 16);
			this.chkAutoUD.TabIndex = 0;
			this.chkAutoUD.Text = "Auto";
			this.chkAutoUD.CheckedChanged += new System.EventHandler(this.chkAutoUD_CheckedChanged);
			// 
			// btnUDEdit
			// 
			this.btnUDEdit.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnUDEdit.Location = new System.Drawing.Point(12, 108);
			this.btnUDEdit.Name = "btnUDEdit";
			this.btnUDEdit.Size = new System.Drawing.Size(52, 23);
			this.btnUDEdit.TabIndex = 2;
			this.btnUDEdit.Text = "Edit";
			this.btnUDEdit.Click += new System.EventHandler(this.btnUDEdit_Click);
			// 
			// btnUDRemove
			// 
			this.btnUDRemove.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnUDRemove.Location = new System.Drawing.Point(108, 108);
			this.btnUDRemove.Name = "btnUDRemove";
			this.btnUDRemove.Size = new System.Drawing.Size(28, 23);
			this.btnUDRemove.TabIndex = 4;
			this.btnUDRemove.Text = "-";
			this.btnUDRemove.Click += new System.EventHandler(this.btnUDRemove_Click);
			// 
			// btnUDAdd
			// 
			this.btnUDAdd.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnUDAdd.Location = new System.Drawing.Point(72, 108);
			this.btnUDAdd.Name = "btnUDAdd";
			this.btnUDAdd.Size = new System.Drawing.Size(28, 23);
			this.btnUDAdd.TabIndex = 3;
			this.btnUDAdd.Text = "+";
			this.btnUDAdd.Click += new System.EventHandler(this.btnUDAdd_Click);
			// 
			// lstUserData
			// 
			this.lstUserData.Location = new System.Drawing.Point(12, 44);
			this.lstUserData.Name = "lstUserData";
			this.lstUserData.Size = new System.Drawing.Size(124, 56);
			this.lstUserData.TabIndex = 1;
			// 
			// chkChangePacking
			// 
			this.chkChangePacking.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.chkChangePacking.Location = new System.Drawing.Point(12, 44);
			this.chkChangePacking.Name = "chkChangePacking";
			this.chkChangePacking.Size = new System.Drawing.Size(68, 20);
			this.chkChangePacking.TabIndex = 2;
			this.chkChangePacking.Text = "Unpack";
			this.chkChangePacking.CheckedChanged += new System.EventHandler(this.chkUnpack_CheckedChanged);
			// 
			// lblPackedBitstream
			// 
			this.lblPackedBitstream.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblPackedBitstream.Location = new System.Drawing.Point(12, 20);
			this.lblPackedBitstream.Name = "lblPackedBitstream";
			this.lblPackedBitstream.Size = new System.Drawing.Size(96, 16);
			this.lblPackedBitstream.TabIndex = 0;
			this.lblPackedBitstream.Text = "Packed Bitstream:";
			// 
			// grpPackedBitstream
			// 
			this.grpPackedBitstream.Controls.Add(this.lblIsPacked);
			this.grpPackedBitstream.Controls.Add(this.chkChangePacking);
			this.grpPackedBitstream.Controls.Add(this.lblPackedBitstream);
			this.grpPackedBitstream.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.grpPackedBitstream.Location = new System.Drawing.Point(284, 40);
			this.grpPackedBitstream.Name = "grpPackedBitstream";
			this.grpPackedBitstream.Size = new System.Drawing.Size(148, 72);
			this.grpPackedBitstream.TabIndex = 4;
			this.grpPackedBitstream.TabStop = false;
			this.grpPackedBitstream.Text = "Packed Bitstream";
			// 
			// lblIsPacked
			// 
			this.lblIsPacked.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblIsPacked.Location = new System.Drawing.Point(108, 20);
			this.lblIsPacked.Name = "lblIsPacked";
			this.lblIsPacked.Size = new System.Drawing.Size(28, 16);
			this.lblIsPacked.TabIndex = 1;
			this.lblIsPacked.Text = "Yes";
			this.lblIsPacked.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// btnAbout
			// 
			this.btnAbout.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnAbout.Location = new System.Drawing.Point(472, 100);
			this.btnAbout.Name = "btnAbout";
			this.btnAbout.Size = new System.Drawing.Size(80, 23);
			this.btnAbout.TabIndex = 9;
			this.btnAbout.Text = "About";
			this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
			// 
			// rbBFF
			// 
			this.rbBFF.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbBFF.Location = new System.Drawing.Point(12, 84);
			this.rbBFF.Name = "rbBFF";
			this.rbBFF.Size = new System.Drawing.Size(84, 20);
			this.rbBFF.TabIndex = 4;
			this.rbBFF.Text = "Bottom First";
			// 
			// btnSave
			// 
			this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnSave.Location = new System.Drawing.Point(472, 44);
			this.btnSave.Name = "btnSave";
			this.btnSave.Size = new System.Drawing.Size(80, 23);
			this.btnSave.TabIndex = 7;
			this.btnSave.Text = "Save...";
			this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
			// 
			// rbTFF
			// 
			this.rbTFF.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbTFF.Location = new System.Drawing.Point(12, 64);
			this.rbTFF.Name = "rbTFF";
			this.rbTFF.Size = new System.Drawing.Size(68, 20);
			this.rbTFF.TabIndex = 3;
			this.rbTFF.Text = "Top First";
			// 
			// lblFieldOrder
			// 
			this.lblFieldOrder.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblFieldOrder.Location = new System.Drawing.Point(12, 48);
			this.lblFieldOrder.Name = "lblFieldOrder";
			this.lblFieldOrder.Size = new System.Drawing.Size(68, 16);
			this.lblFieldOrder.TabIndex = 2;
			this.lblFieldOrder.Text = "Field Order:";
			// 
			// lblIsInterlaced
			// 
			this.lblIsInterlaced.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblIsInterlaced.Location = new System.Drawing.Point(76, 20);
			this.lblIsInterlaced.Name = "lblIsInterlaced";
			this.lblIsInterlaced.Size = new System.Drawing.Size(24, 16);
			this.lblIsInterlaced.TabIndex = 1;
			this.lblIsInterlaced.Text = "Yes";
			this.lblIsInterlaced.TextAlign = System.Drawing.ContentAlignment.TopRight;
			// 
			// grpAspectRatio
			// 
			this.grpAspectRatio.Controls.Add(this.rbDAR_185_1);
			this.grpAspectRatio.Controls.Add(this.lblDAR);
			this.grpAspectRatio.Controls.Add(this.lblPAR);
			this.grpAspectRatio.Controls.Add(this.txtDAR_Height);
			this.grpAspectRatio.Controls.Add(this.txtDAR_Width);
			this.grpAspectRatio.Controls.Add(this.rbDAR_235_1);
			this.grpAspectRatio.Controls.Add(this.rbDAR_Custom);
			this.grpAspectRatio.Controls.Add(this.rbDAR_16_9);
			this.grpAspectRatio.Controls.Add(this.rbDAR_4_3);
			this.grpAspectRatio.Controls.Add(this.lblDAR_Colon);
			this.grpAspectRatio.Controls.Add(this.txtPAR_Height);
			this.grpAspectRatio.Controls.Add(this.txtPAR_Width);
			this.grpAspectRatio.Controls.Add(this.lblPAR_Colon);
			this.grpAspectRatio.Controls.Add(this.rbPAR_NTSC_16_9);
			this.grpAspectRatio.Controls.Add(this.rbPAR_Custom);
			this.grpAspectRatio.Controls.Add(this.rbPAR_PAL_16_9);
			this.grpAspectRatio.Controls.Add(this.rbPAR_PAL_4_3);
			this.grpAspectRatio.Controls.Add(this.rbPAR_NTSC_4_3);
			this.grpAspectRatio.Controls.Add(this.rbPAR_VGA_1_1);
			this.grpAspectRatio.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.grpAspectRatio.Location = new System.Drawing.Point(8, 40);
			this.grpAspectRatio.Name = "grpAspectRatio";
			this.grpAspectRatio.Size = new System.Drawing.Size(268, 216);
			this.grpAspectRatio.TabIndex = 3;
			this.grpAspectRatio.TabStop = false;
			this.grpAspectRatio.Text = "Aspect Ratio";
			// 
			// rbDAR_185_1
			// 
			this.rbDAR_185_1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbDAR_185_1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.rbDAR_185_1.Location = new System.Drawing.Point(156, 84);
			this.rbDAR_185_1.Name = "rbDAR_185_1";
			this.rbDAR_185_1.Size = new System.Drawing.Size(96, 20);
			this.rbDAR_185_1.TabIndex = 13;
			this.rbDAR_185_1.Tag = "DAR 185:100";
			this.rbDAR_185_1.Text = "1.85:1";
			// 
			// lblDAR
			// 
			this.lblDAR.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblDAR.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblDAR.Location = new System.Drawing.Point(156, 20);
			this.lblDAR.Name = "lblDAR";
			this.lblDAR.Size = new System.Drawing.Size(96, 16);
			this.lblDAR.TabIndex = 10;
			this.lblDAR.Text = "Display AR:";
			this.lblDAR.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// lblPAR
			// 
			this.lblPAR.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblPAR.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblPAR.Location = new System.Drawing.Point(16, 20);
			this.lblPAR.Name = "lblPAR";
			this.lblPAR.Size = new System.Drawing.Size(96, 16);
			this.lblPAR.TabIndex = 0;
			this.lblPAR.Text = "Pixel AR:";
			this.lblPAR.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// txtDAR_Height
			// 
			this.txtDAR_Height.Location = new System.Drawing.Point(212, 184);
			this.txtDAR_Height.Name = "txtDAR_Height";
			this.txtDAR_Height.Size = new System.Drawing.Size(40, 20);
			this.txtDAR_Height.TabIndex = 18;
			// 
			// txtDAR_Width
			// 
			this.txtDAR_Width.Location = new System.Drawing.Point(160, 184);
			this.txtDAR_Width.Name = "txtDAR_Width";
			this.txtDAR_Width.Size = new System.Drawing.Size(40, 20);
			this.txtDAR_Width.TabIndex = 16;
			// 
			// rbDAR_235_1
			// 
			this.rbDAR_235_1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbDAR_235_1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.rbDAR_235_1.Location = new System.Drawing.Point(156, 106);
			this.rbDAR_235_1.Name = "rbDAR_235_1";
			this.rbDAR_235_1.Size = new System.Drawing.Size(96, 20);
			this.rbDAR_235_1.TabIndex = 14;
			this.rbDAR_235_1.Tag = "DAR 235:100";
			this.rbDAR_235_1.Text = "2.35:1";
			// 
			// rbDAR_Custom
			// 
			this.rbDAR_Custom.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbDAR_Custom.Location = new System.Drawing.Point(156, 160);
			this.rbDAR_Custom.Name = "rbDAR_Custom";
			this.rbDAR_Custom.Size = new System.Drawing.Size(96, 20);
			this.rbDAR_Custom.TabIndex = 15;
			this.rbDAR_Custom.Tag = "DAR Custom";
			this.rbDAR_Custom.Text = "Custom";
			// 
			// rbDAR_16_9
			// 
			this.rbDAR_16_9.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbDAR_16_9.Location = new System.Drawing.Point(156, 62);
			this.rbDAR_16_9.Name = "rbDAR_16_9";
			this.rbDAR_16_9.Size = new System.Drawing.Size(96, 20);
			this.rbDAR_16_9.TabIndex = 12;
			this.rbDAR_16_9.Tag = "DAR 16:9";
			this.rbDAR_16_9.Text = "16:9";
			// 
			// rbDAR_4_3
			// 
			this.rbDAR_4_3.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbDAR_4_3.Location = new System.Drawing.Point(156, 40);
			this.rbDAR_4_3.Name = "rbDAR_4_3";
			this.rbDAR_4_3.Size = new System.Drawing.Size(96, 20);
			this.rbDAR_4_3.TabIndex = 11;
			this.rbDAR_4_3.Tag = "DAR 4:3";
			this.rbDAR_4_3.Text = "4:3";
			// 
			// lblDAR_Colon
			// 
			this.lblDAR_Colon.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblDAR_Colon.Location = new System.Drawing.Point(200, 188);
			this.lblDAR_Colon.Name = "lblDAR_Colon";
			this.lblDAR_Colon.Size = new System.Drawing.Size(12, 16);
			this.lblDAR_Colon.TabIndex = 17;
			this.lblDAR_Colon.Text = ":";
			this.lblDAR_Colon.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// txtPAR_Height
			// 
			this.txtPAR_Height.Location = new System.Drawing.Point(72, 184);
			this.txtPAR_Height.Name = "txtPAR_Height";
			this.txtPAR_Height.Size = new System.Drawing.Size(40, 20);
			this.txtPAR_Height.TabIndex = 9;
			// 
			// txtPAR_Width
			// 
			this.txtPAR_Width.Location = new System.Drawing.Point(20, 184);
			this.txtPAR_Width.Name = "txtPAR_Width";
			this.txtPAR_Width.Size = new System.Drawing.Size(40, 20);
			this.txtPAR_Width.TabIndex = 7;
			// 
			// lblPAR_Colon
			// 
			this.lblPAR_Colon.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblPAR_Colon.Location = new System.Drawing.Point(60, 188);
			this.lblPAR_Colon.Name = "lblPAR_Colon";
			this.lblPAR_Colon.Size = new System.Drawing.Size(12, 16);
			this.lblPAR_Colon.TabIndex = 8;
			this.lblPAR_Colon.Text = ":";
			this.lblPAR_Colon.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// rbPAR_NTSC_16_9
			// 
			this.rbPAR_NTSC_16_9.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbPAR_NTSC_16_9.Location = new System.Drawing.Point(16, 128);
			this.rbPAR_NTSC_16_9.Name = "rbPAR_NTSC_16_9";
			this.rbPAR_NTSC_16_9.Size = new System.Drawing.Size(96, 20);
			this.rbPAR_NTSC_16_9.TabIndex = 5;
			this.rbPAR_NTSC_16_9.Tag = "PAR 5";
			this.rbPAR_NTSC_16_9.Text = "16:9 NTSC";
			// 
			// rbPAR_Custom
			// 
			this.rbPAR_Custom.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbPAR_Custom.Location = new System.Drawing.Point(16, 160);
			this.rbPAR_Custom.Name = "rbPAR_Custom";
			this.rbPAR_Custom.Size = new System.Drawing.Size(96, 20);
			this.rbPAR_Custom.TabIndex = 6;
			this.rbPAR_Custom.Tag = "PAR Custom";
			this.rbPAR_Custom.Text = "Custom";
			// 
			// rbPAR_PAL_16_9
			// 
			this.rbPAR_PAL_16_9.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbPAR_PAL_16_9.Location = new System.Drawing.Point(16, 106);
			this.rbPAR_PAL_16_9.Name = "rbPAR_PAL_16_9";
			this.rbPAR_PAL_16_9.Size = new System.Drawing.Size(96, 20);
			this.rbPAR_PAL_16_9.TabIndex = 4;
			this.rbPAR_PAL_16_9.Tag = "PAR 4";
			this.rbPAR_PAL_16_9.Text = "16:9 PAL";
			// 
			// rbPAR_PAL_4_3
			// 
			this.rbPAR_PAL_4_3.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbPAR_PAL_4_3.Location = new System.Drawing.Point(16, 62);
			this.rbPAR_PAL_4_3.Name = "rbPAR_PAL_4_3";
			this.rbPAR_PAL_4_3.Size = new System.Drawing.Size(96, 20);
			this.rbPAR_PAL_4_3.TabIndex = 2;
			this.rbPAR_PAL_4_3.Tag = "PAR 2";
			this.rbPAR_PAL_4_3.Text = "4:3 PAL";
			// 
			// rbPAR_NTSC_4_3
			// 
			this.rbPAR_NTSC_4_3.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbPAR_NTSC_4_3.Location = new System.Drawing.Point(16, 84);
			this.rbPAR_NTSC_4_3.Name = "rbPAR_NTSC_4_3";
			this.rbPAR_NTSC_4_3.Size = new System.Drawing.Size(96, 20);
			this.rbPAR_NTSC_4_3.TabIndex = 3;
			this.rbPAR_NTSC_4_3.Tag = "PAR 3";
			this.rbPAR_NTSC_4_3.Text = "4:3 NTSC";
			// 
			// rbPAR_VGA_1_1
			// 
			this.rbPAR_VGA_1_1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.rbPAR_VGA_1_1.Location = new System.Drawing.Point(16, 40);
			this.rbPAR_VGA_1_1.Name = "rbPAR_VGA_1_1";
			this.rbPAR_VGA_1_1.Size = new System.Drawing.Size(96, 20);
			this.rbPAR_VGA_1_1.TabIndex = 1;
			this.rbPAR_VGA_1_1.Tag = "PAR 1";
			this.rbPAR_VGA_1_1.Text = "Square Pixels";
			// 
			// btnBrowseSource
			// 
			this.btnBrowseSource.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnBrowseSource.Location = new System.Drawing.Point(472, 8);
			this.btnBrowseSource.Name = "btnBrowseSource";
			this.btnBrowseSource.Size = new System.Drawing.Size(80, 23);
			this.btnBrowseSource.TabIndex = 2;
			this.btnBrowseSource.Text = "Browse...";
			this.btnBrowseSource.Click += new System.EventHandler(this.btnBrowseSource_Click);
			// 
			// lblInterlaced
			// 
			this.lblInterlaced.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblInterlaced.Location = new System.Drawing.Point(12, 20);
			this.lblInterlaced.Name = "lblInterlaced";
			this.lblInterlaced.Size = new System.Drawing.Size(64, 16);
			this.lblInterlaced.TabIndex = 0;
			this.lblInterlaced.Text = "Interlaced:";
			// 
			// lblSource
			// 
			this.lblSource.AutoSize = true;
			this.lblSource.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.lblSource.Location = new System.Drawing.Point(8, 12);
			this.lblSource.Name = "lblSource";
			this.lblSource.Size = new System.Drawing.Size(64, 13);
			this.lblSource.TabIndex = 0;
			this.lblSource.Text = "AVI Source:";
			// 
			// grpInterlacing
			// 
			this.grpInterlacing.Controls.Add(this.rbBFF);
			this.grpInterlacing.Controls.Add(this.rbTFF);
			this.grpInterlacing.Controls.Add(this.lblFieldOrder);
			this.grpInterlacing.Controls.Add(this.lblIsInterlaced);
			this.grpInterlacing.Controls.Add(this.lblInterlaced);
			this.grpInterlacing.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.grpInterlacing.Location = new System.Drawing.Point(440, 144);
			this.grpInterlacing.Name = "grpInterlacing";
			this.grpInterlacing.Size = new System.Drawing.Size(112, 112);
			this.grpInterlacing.TabIndex = 6;
			this.grpInterlacing.TabStop = false;
			this.grpInterlacing.Text = "Interlacing";
			// 
			// btnVideoInfo
			// 
			this.btnVideoInfo.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnVideoInfo.Location = new System.Drawing.Point(472, 72);
			this.btnVideoInfo.Name = "btnVideoInfo";
			this.btnVideoInfo.Size = new System.Drawing.Size(80, 23);
			this.btnVideoInfo.TabIndex = 8;
			this.btnVideoInfo.Text = "Video Info";
			this.btnVideoInfo.Click += new System.EventHandler(this.btnVideoInfo_Click);
			// 
			// txtSourcePath
			// 
			this.txtSourcePath.AllowDrop = true;
			this.txtSourcePath.Location = new System.Drawing.Point(76, 8);
			this.txtSourcePath.Name = "txtSourcePath";
			this.txtSourcePath.ReadOnly = true;
			this.txtSourcePath.Size = new System.Drawing.Size(388, 20);
			this.txtSourcePath.TabIndex = 1;
			this.txtSourcePath.DragDrop += new System.Windows.Forms.DragEventHandler(this.txtSourcePath_DragDrop);
			this.txtSourcePath.DragEnter += new System.Windows.Forms.DragEventHandler(this.txtSourcePath_DragEnter);
			// 
			// frmMPEG4Modifier
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(560, 264);
			this.Controls.Add(this.grpUserData);
			this.Controls.Add(this.grpPackedBitstream);
			this.Controls.Add(this.btnAbout);
			this.Controls.Add(this.btnSave);
			this.Controls.Add(this.grpAspectRatio);
			this.Controls.Add(this.btnBrowseSource);
			this.Controls.Add(this.lblSource);
			this.Controls.Add(this.grpInterlacing);
			this.Controls.Add(this.btnVideoInfo);
			this.Controls.Add(this.txtSourcePath);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "frmMPEG4Modifier";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "MPEG4 Modifier";
			this.Load += new System.EventHandler(this.frmMPEG4Modifier_Load);
			this.Shown += new System.EventHandler(this.frmMPEG4Modifier_Shown);
			this.DoubleClick += new System.EventHandler(this.frmMPEG4Modifier_DoubleClick);
			this.grpUserData.ResumeLayout(false);
			this.grpPackedBitstream.ResumeLayout(false);
			this.grpAspectRatio.ResumeLayout(false);
			this.grpAspectRatio.PerformLayout();
			this.grpInterlacing.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.GroupBox grpUserData;
		private System.Windows.Forms.CheckBox chkAutoUD;
		private System.Windows.Forms.Button btnUDEdit;
		private System.Windows.Forms.Button btnUDRemove;
		private System.Windows.Forms.Button btnUDAdd;
		private System.Windows.Forms.ListBox lstUserData;
		private System.Windows.Forms.CheckBox chkChangePacking;
		private System.Windows.Forms.Label lblPackedBitstream;
		private System.Windows.Forms.GroupBox grpPackedBitstream;
		private System.Windows.Forms.Label lblIsPacked;
		private System.Windows.Forms.Button btnAbout;
		private System.Windows.Forms.RadioButton rbBFF;
		private System.Windows.Forms.Button btnSave;
		private System.Windows.Forms.RadioButton rbTFF;
		private System.Windows.Forms.Label lblFieldOrder;
		private System.Windows.Forms.Label lblIsInterlaced;
		private System.Windows.Forms.GroupBox grpAspectRatio;
		private System.Windows.Forms.RadioButton rbDAR_185_1;
		private System.Windows.Forms.Label lblDAR;
		private System.Windows.Forms.Label lblPAR;
		private System.Windows.Forms.TextBox txtDAR_Height;
		private System.Windows.Forms.TextBox txtDAR_Width;
		private System.Windows.Forms.RadioButton rbDAR_235_1;
		private System.Windows.Forms.RadioButton rbDAR_Custom;
		private System.Windows.Forms.RadioButton rbDAR_16_9;
		private System.Windows.Forms.RadioButton rbDAR_4_3;
		private System.Windows.Forms.Label lblDAR_Colon;
		private System.Windows.Forms.TextBox txtPAR_Height;
		private System.Windows.Forms.TextBox txtPAR_Width;
		private System.Windows.Forms.Label lblPAR_Colon;
		private System.Windows.Forms.RadioButton rbPAR_NTSC_16_9;
		private System.Windows.Forms.RadioButton rbPAR_Custom;
		private System.Windows.Forms.RadioButton rbPAR_PAL_16_9;
		private System.Windows.Forms.RadioButton rbPAR_PAL_4_3;
		private System.Windows.Forms.RadioButton rbPAR_NTSC_4_3;
		private System.Windows.Forms.RadioButton rbPAR_VGA_1_1;
		private System.Windows.Forms.Button btnBrowseSource;
		private System.Windows.Forms.Label lblInterlaced;
		private System.Windows.Forms.Label lblSource;
		private System.Windows.Forms.GroupBox grpInterlacing;
		private System.Windows.Forms.Button btnVideoInfo;
		private System.Windows.Forms.TextBox txtSourcePath;
	}
}

