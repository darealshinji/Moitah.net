namespace JDP {
	partial class frmCUETools {
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
			this.btnConvert = new System.Windows.Forms.Button();
			this.grpCUEPaths = new System.Windows.Forms.GroupBox();
			this.btnBrowseOutput = new System.Windows.Forms.Button();
			this.btnBrowseInput = new System.Windows.Forms.Button();
			this.lblOutput = new System.Windows.Forms.Label();
			this.lblInput = new System.Windows.Forms.Label();
			this.txtOutputPath = new System.Windows.Forms.TextBox();
			this.txtInputPath = new System.Windows.Forms.TextBox();
			this.grpOutputStyle = new System.Windows.Forms.GroupBox();
			this.rbGapsLeftOut = new System.Windows.Forms.RadioButton();
			this.rbGapsPrepended = new System.Windows.Forms.RadioButton();
			this.rbGapsAppended = new System.Windows.Forms.RadioButton();
			this.rbSingleFile = new System.Windows.Forms.RadioButton();
			this.btnAbout = new System.Windows.Forms.Button();
			this.grpAudioFilenames = new System.Windows.Forms.GroupBox();
			this.chkKeepOriginalFilenames = new System.Windows.Forms.CheckBox();
			this.txtSpecialExceptions = new System.Windows.Forms.TextBox();
			this.chkRemoveSpecial = new System.Windows.Forms.CheckBox();
			this.chkReplaceSpaces = new System.Windows.Forms.CheckBox();
			this.txtTrackFilenameFormat = new System.Windows.Forms.TextBox();
			this.lblTrackFilenameFormat = new System.Windows.Forms.Label();
			this.lblSingleFilenameFormat = new System.Windows.Forms.Label();
			this.txtSingleFilenameFormat = new System.Windows.Forms.TextBox();
			this.txtStatus = new System.Windows.Forms.TextBox();
			this.grpOutputPathGeneration = new System.Windows.Forms.GroupBox();
			this.txtCustomFormat = new System.Windows.Forms.TextBox();
			this.rbCustomFormat = new System.Windows.Forms.RadioButton();
			this.txtCreateSubdirectory = new System.Windows.Forms.TextBox();
			this.rbDontGenerate = new System.Windows.Forms.RadioButton();
			this.rbCreateSubdirectory = new System.Windows.Forms.RadioButton();
			this.rbAppendFilename = new System.Windows.Forms.RadioButton();
			this.txtAppendFilename = new System.Windows.Forms.TextBox();
			this.grpAudioOutput = new System.Windows.Forms.GroupBox();
			this.rbWavPack = new System.Windows.Forms.RadioButton();
			this.chkNoAudioOutput = new System.Windows.Forms.CheckBox();
			this.rbFLAC = new System.Windows.Forms.RadioButton();
			this.rbWAV = new System.Windows.Forms.RadioButton();
			this.btnBatch = new System.Windows.Forms.Button();
			this.btnFilenameCorrector = new System.Windows.Forms.Button();
			this.btnSettings = new System.Windows.Forms.Button();
			this.grpCUEPaths.SuspendLayout();
			this.grpOutputStyle.SuspendLayout();
			this.grpAudioFilenames.SuspendLayout();
			this.grpOutputPathGeneration.SuspendLayout();
			this.grpAudioOutput.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnConvert
			// 
			this.btnConvert.Location = new System.Drawing.Point(500, 394);
			this.btnConvert.Name = "btnConvert";
			this.btnConvert.Size = new System.Drawing.Size(72, 23);
			this.btnConvert.TabIndex = 5;
			this.btnConvert.Text = "Convert";
			this.btnConvert.UseVisualStyleBackColor = true;
			this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
			// 
			// grpCUEPaths
			// 
			this.grpCUEPaths.Controls.Add(this.btnBrowseOutput);
			this.grpCUEPaths.Controls.Add(this.btnBrowseInput);
			this.grpCUEPaths.Controls.Add(this.lblOutput);
			this.grpCUEPaths.Controls.Add(this.lblInput);
			this.grpCUEPaths.Controls.Add(this.txtOutputPath);
			this.grpCUEPaths.Controls.Add(this.txtInputPath);
			this.grpCUEPaths.Location = new System.Drawing.Point(8, 4);
			this.grpCUEPaths.Name = "grpCUEPaths";
			this.grpCUEPaths.Size = new System.Drawing.Size(564, 84);
			this.grpCUEPaths.TabIndex = 0;
			this.grpCUEPaths.TabStop = false;
			this.grpCUEPaths.Text = "CUE Paths";
			// 
			// btnBrowseOutput
			// 
			this.btnBrowseOutput.Location = new System.Drawing.Point(472, 48);
			this.btnBrowseOutput.Name = "btnBrowseOutput";
			this.btnBrowseOutput.Size = new System.Drawing.Size(80, 23);
			this.btnBrowseOutput.TabIndex = 5;
			this.btnBrowseOutput.Text = "Browse...";
			this.btnBrowseOutput.UseVisualStyleBackColor = true;
			this.btnBrowseOutput.Click += new System.EventHandler(this.btnBrowseOutput_Click);
			// 
			// btnBrowseInput
			// 
			this.btnBrowseInput.Location = new System.Drawing.Point(472, 20);
			this.btnBrowseInput.Name = "btnBrowseInput";
			this.btnBrowseInput.Size = new System.Drawing.Size(80, 23);
			this.btnBrowseInput.TabIndex = 2;
			this.btnBrowseInput.Text = "Browse...";
			this.btnBrowseInput.UseVisualStyleBackColor = true;
			this.btnBrowseInput.Click += new System.EventHandler(this.btnBrowseInput_Click);
			// 
			// lblOutput
			// 
			this.lblOutput.AutoSize = true;
			this.lblOutput.Location = new System.Drawing.Point(8, 52);
			this.lblOutput.Name = "lblOutput";
			this.lblOutput.Size = new System.Drawing.Size(45, 13);
			this.lblOutput.TabIndex = 3;
			this.lblOutput.Text = "Output:";
			// 
			// lblInput
			// 
			this.lblInput.AutoSize = true;
			this.lblInput.Location = new System.Drawing.Point(8, 24);
			this.lblInput.Name = "lblInput";
			this.lblInput.Size = new System.Drawing.Size(37, 13);
			this.lblInput.TabIndex = 0;
			this.lblInput.Text = "Input:";
			// 
			// txtOutputPath
			// 
			this.txtOutputPath.AllowDrop = true;
			this.txtOutputPath.Location = new System.Drawing.Point(60, 48);
			this.txtOutputPath.Name = "txtOutputPath";
			this.txtOutputPath.Size = new System.Drawing.Size(404, 21);
			this.txtOutputPath.TabIndex = 4;
			this.txtOutputPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.PathTextBox_DragDrop);
			this.txtOutputPath.DragEnter += new System.Windows.Forms.DragEventHandler(this.PathTextBox_DragEnter);
			// 
			// txtInputPath
			// 
			this.txtInputPath.AllowDrop = true;
			this.txtInputPath.Location = new System.Drawing.Point(60, 20);
			this.txtInputPath.Name = "txtInputPath";
			this.txtInputPath.Size = new System.Drawing.Size(404, 21);
			this.txtInputPath.TabIndex = 1;
			this.txtInputPath.DragDrop += new System.Windows.Forms.DragEventHandler(this.PathTextBox_DragDrop);
			this.txtInputPath.DragEnter += new System.Windows.Forms.DragEventHandler(this.PathTextBox_DragEnter);
			this.txtInputPath.TextChanged += new System.EventHandler(this.txtInputPath_TextChanged);
			// 
			// grpOutputStyle
			// 
			this.grpOutputStyle.Controls.Add(this.rbGapsLeftOut);
			this.grpOutputStyle.Controls.Add(this.rbGapsPrepended);
			this.grpOutputStyle.Controls.Add(this.rbGapsAppended);
			this.grpOutputStyle.Controls.Add(this.rbSingleFile);
			this.grpOutputStyle.Location = new System.Drawing.Point(60, 228);
			this.grpOutputStyle.Name = "grpOutputStyle";
			this.grpOutputStyle.Size = new System.Drawing.Size(128, 108);
			this.grpOutputStyle.TabIndex = 3;
			this.grpOutputStyle.TabStop = false;
			this.grpOutputStyle.Text = "Output Style";
			// 
			// rbGapsLeftOut
			// 
			this.rbGapsLeftOut.AutoSize = true;
			this.rbGapsLeftOut.Location = new System.Drawing.Point(12, 80);
			this.rbGapsLeftOut.Name = "rbGapsLeftOut";
			this.rbGapsLeftOut.Size = new System.Drawing.Size(92, 17);
			this.rbGapsLeftOut.TabIndex = 3;
			this.rbGapsLeftOut.Text = "Gaps Left Out";
			this.rbGapsLeftOut.UseVisualStyleBackColor = true;
			// 
			// rbGapsPrepended
			// 
			this.rbGapsPrepended.AutoSize = true;
			this.rbGapsPrepended.Location = new System.Drawing.Point(12, 60);
			this.rbGapsPrepended.Name = "rbGapsPrepended";
			this.rbGapsPrepended.Size = new System.Drawing.Size(104, 17);
			this.rbGapsPrepended.TabIndex = 2;
			this.rbGapsPrepended.Text = "Gaps Prepended";
			this.rbGapsPrepended.UseVisualStyleBackColor = true;
			// 
			// rbGapsAppended
			// 
			this.rbGapsAppended.AutoSize = true;
			this.rbGapsAppended.Location = new System.Drawing.Point(12, 40);
			this.rbGapsAppended.Name = "rbGapsAppended";
			this.rbGapsAppended.Size = new System.Drawing.Size(101, 17);
			this.rbGapsAppended.TabIndex = 1;
			this.rbGapsAppended.Text = "Gaps Appended";
			this.rbGapsAppended.UseVisualStyleBackColor = true;
			// 
			// rbSingleFile
			// 
			this.rbSingleFile.AutoSize = true;
			this.rbSingleFile.Checked = true;
			this.rbSingleFile.Location = new System.Drawing.Point(12, 20);
			this.rbSingleFile.Name = "rbSingleFile";
			this.rbSingleFile.Size = new System.Drawing.Size(72, 17);
			this.rbSingleFile.TabIndex = 0;
			this.rbSingleFile.TabStop = true;
			this.rbSingleFile.Text = "Single File";
			this.rbSingleFile.UseVisualStyleBackColor = true;
			// 
			// btnAbout
			// 
			this.btnAbout.Location = new System.Drawing.Point(8, 394);
			this.btnAbout.Name = "btnAbout";
			this.btnAbout.Size = new System.Drawing.Size(64, 23);
			this.btnAbout.TabIndex = 9;
			this.btnAbout.Text = "About";
			this.btnAbout.UseVisualStyleBackColor = true;
			this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
			// 
			// grpAudioFilenames
			// 
			this.grpAudioFilenames.Controls.Add(this.chkKeepOriginalFilenames);
			this.grpAudioFilenames.Controls.Add(this.txtSpecialExceptions);
			this.grpAudioFilenames.Controls.Add(this.chkRemoveSpecial);
			this.grpAudioFilenames.Controls.Add(this.chkReplaceSpaces);
			this.grpAudioFilenames.Controls.Add(this.txtTrackFilenameFormat);
			this.grpAudioFilenames.Controls.Add(this.lblTrackFilenameFormat);
			this.grpAudioFilenames.Controls.Add(this.lblSingleFilenameFormat);
			this.grpAudioFilenames.Controls.Add(this.txtSingleFilenameFormat);
			this.grpAudioFilenames.Location = new System.Drawing.Point(196, 228);
			this.grpAudioFilenames.Name = "grpAudioFilenames";
			this.grpAudioFilenames.Size = new System.Drawing.Size(324, 156);
			this.grpAudioFilenames.TabIndex = 4;
			this.grpAudioFilenames.TabStop = false;
			this.grpAudioFilenames.Text = "Audio Filenames";
			// 
			// chkKeepOriginalFilenames
			// 
			this.chkKeepOriginalFilenames.AutoSize = true;
			this.chkKeepOriginalFilenames.Checked = true;
			this.chkKeepOriginalFilenames.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkKeepOriginalFilenames.Location = new System.Drawing.Point(12, 20);
			this.chkKeepOriginalFilenames.Name = "chkKeepOriginalFilenames";
			this.chkKeepOriginalFilenames.Size = new System.Drawing.Size(135, 17);
			this.chkKeepOriginalFilenames.TabIndex = 0;
			this.chkKeepOriginalFilenames.Text = "Keep original filenames";
			this.chkKeepOriginalFilenames.UseVisualStyleBackColor = true;
			// 
			// txtSpecialExceptions
			// 
			this.txtSpecialExceptions.Location = new System.Drawing.Point(212, 100);
			this.txtSpecialExceptions.Name = "txtSpecialExceptions";
			this.txtSpecialExceptions.Size = new System.Drawing.Size(100, 21);
			this.txtSpecialExceptions.TabIndex = 6;
			this.txtSpecialExceptions.Text = "-()";
			// 
			// chkRemoveSpecial
			// 
			this.chkRemoveSpecial.AutoSize = true;
			this.chkRemoveSpecial.Checked = true;
			this.chkRemoveSpecial.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkRemoveSpecial.Location = new System.Drawing.Point(12, 100);
			this.chkRemoveSpecial.Name = "chkRemoveSpecial";
			this.chkRemoveSpecial.Size = new System.Drawing.Size(194, 17);
			this.chkRemoveSpecial.TabIndex = 5;
			this.chkRemoveSpecial.Text = "Remove special characters except:";
			this.chkRemoveSpecial.UseVisualStyleBackColor = true;
			// 
			// chkReplaceSpaces
			// 
			this.chkReplaceSpaces.AutoSize = true;
			this.chkReplaceSpaces.Checked = true;
			this.chkReplaceSpaces.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkReplaceSpaces.Location = new System.Drawing.Point(12, 128);
			this.chkReplaceSpaces.Name = "chkReplaceSpaces";
			this.chkReplaceSpaces.Size = new System.Drawing.Size(185, 17);
			this.chkReplaceSpaces.TabIndex = 7;
			this.chkReplaceSpaces.Text = "Replace spaces with underscores";
			this.chkReplaceSpaces.UseVisualStyleBackColor = true;
			// 
			// txtTrackFilenameFormat
			// 
			this.txtTrackFilenameFormat.Location = new System.Drawing.Point(92, 72);
			this.txtTrackFilenameFormat.Name = "txtTrackFilenameFormat";
			this.txtTrackFilenameFormat.Size = new System.Drawing.Size(136, 21);
			this.txtTrackFilenameFormat.TabIndex = 4;
			this.txtTrackFilenameFormat.Text = "%N-%A-%T";
			// 
			// lblTrackFilenameFormat
			// 
			this.lblTrackFilenameFormat.AutoSize = true;
			this.lblTrackFilenameFormat.Location = new System.Drawing.Point(10, 76);
			this.lblTrackFilenameFormat.Name = "lblTrackFilenameFormat";
			this.lblTrackFilenameFormat.Size = new System.Drawing.Size(72, 13);
			this.lblTrackFilenameFormat.TabIndex = 3;
			this.lblTrackFilenameFormat.Text = "Track format:";
			// 
			// lblSingleFilenameFormat
			// 
			this.lblSingleFilenameFormat.AutoSize = true;
			this.lblSingleFilenameFormat.Location = new System.Drawing.Point(10, 48);
			this.lblSingleFilenameFormat.Name = "lblSingleFilenameFormat";
			this.lblSingleFilenameFormat.Size = new System.Drawing.Size(74, 13);
			this.lblSingleFilenameFormat.TabIndex = 1;
			this.lblSingleFilenameFormat.Text = "Single format:";
			// 
			// txtSingleFilenameFormat
			// 
			this.txtSingleFilenameFormat.Location = new System.Drawing.Point(92, 44);
			this.txtSingleFilenameFormat.Name = "txtSingleFilenameFormat";
			this.txtSingleFilenameFormat.Size = new System.Drawing.Size(136, 21);
			this.txtSingleFilenameFormat.TabIndex = 2;
			this.txtSingleFilenameFormat.Text = "Range";
			// 
			// txtStatus
			// 
			this.txtStatus.Location = new System.Drawing.Point(8, 427);
			this.txtStatus.Name = "txtStatus";
			this.txtStatus.ReadOnly = true;
			this.txtStatus.Size = new System.Drawing.Size(564, 21);
			this.txtStatus.TabIndex = 10;
			this.txtStatus.TabStop = false;
			this.txtStatus.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
			// 
			// grpOutputPathGeneration
			// 
			this.grpOutputPathGeneration.Controls.Add(this.txtCustomFormat);
			this.grpOutputPathGeneration.Controls.Add(this.rbCustomFormat);
			this.grpOutputPathGeneration.Controls.Add(this.txtCreateSubdirectory);
			this.grpOutputPathGeneration.Controls.Add(this.rbDontGenerate);
			this.grpOutputPathGeneration.Controls.Add(this.rbCreateSubdirectory);
			this.grpOutputPathGeneration.Controls.Add(this.rbAppendFilename);
			this.grpOutputPathGeneration.Controls.Add(this.txtAppendFilename);
			this.grpOutputPathGeneration.Location = new System.Drawing.Point(8, 92);
			this.grpOutputPathGeneration.Name = "grpOutputPathGeneration";
			this.grpOutputPathGeneration.Size = new System.Drawing.Size(324, 132);
			this.grpOutputPathGeneration.TabIndex = 1;
			this.grpOutputPathGeneration.TabStop = false;
			this.grpOutputPathGeneration.Text = "Output Path";
			// 
			// txtCustomFormat
			// 
			this.txtCustomFormat.Location = new System.Drawing.Point(144, 76);
			this.txtCustomFormat.Name = "txtCustomFormat";
			this.txtCustomFormat.Size = new System.Drawing.Size(168, 21);
			this.txtCustomFormat.TabIndex = 5;
			this.txtCustomFormat.Text = "%1:-2\\New\\%-1\\%F.cue";
			this.txtCustomFormat.TextChanged += new System.EventHandler(this.txtCustomFormat_TextChanged);
			// 
			// rbCustomFormat
			// 
			this.rbCustomFormat.AutoSize = true;
			this.rbCustomFormat.Location = new System.Drawing.Point(12, 76);
			this.rbCustomFormat.Name = "rbCustomFormat";
			this.rbCustomFormat.Size = new System.Drawing.Size(119, 17);
			this.rbCustomFormat.TabIndex = 4;
			this.rbCustomFormat.TabStop = true;
			this.rbCustomFormat.Text = "Use custom format:";
			this.rbCustomFormat.UseVisualStyleBackColor = true;
			this.rbCustomFormat.CheckedChanged += new System.EventHandler(this.rbCustomFormat_CheckedChanged);
			// 
			// txtCreateSubdirectory
			// 
			this.txtCreateSubdirectory.Location = new System.Drawing.Point(144, 20);
			this.txtCreateSubdirectory.Name = "txtCreateSubdirectory";
			this.txtCreateSubdirectory.Size = new System.Drawing.Size(76, 21);
			this.txtCreateSubdirectory.TabIndex = 1;
			this.txtCreateSubdirectory.Text = "New";
			this.txtCreateSubdirectory.TextChanged += new System.EventHandler(this.txtCreateSubdirectory_TextChanged);
			// 
			// rbDontGenerate
			// 
			this.rbDontGenerate.AutoSize = true;
			this.rbDontGenerate.Location = new System.Drawing.Point(12, 104);
			this.rbDontGenerate.Name = "rbDontGenerate";
			this.rbDontGenerate.Size = new System.Drawing.Size(59, 17);
			this.rbDontGenerate.TabIndex = 6;
			this.rbDontGenerate.Text = "Manual";
			this.rbDontGenerate.UseVisualStyleBackColor = true;
			// 
			// rbCreateSubdirectory
			// 
			this.rbCreateSubdirectory.AutoSize = true;
			this.rbCreateSubdirectory.Checked = true;
			this.rbCreateSubdirectory.Location = new System.Drawing.Point(12, 20);
			this.rbCreateSubdirectory.Name = "rbCreateSubdirectory";
			this.rbCreateSubdirectory.Size = new System.Drawing.Size(125, 17);
			this.rbCreateSubdirectory.TabIndex = 0;
			this.rbCreateSubdirectory.TabStop = true;
			this.rbCreateSubdirectory.Text = "Create subdirectory:";
			this.rbCreateSubdirectory.UseVisualStyleBackColor = true;
			this.rbCreateSubdirectory.CheckedChanged += new System.EventHandler(this.rbCreateSubdirectory_CheckedChanged);
			// 
			// rbAppendFilename
			// 
			this.rbAppendFilename.AutoSize = true;
			this.rbAppendFilename.Location = new System.Drawing.Point(12, 48);
			this.rbAppendFilename.Name = "rbAppendFilename";
			this.rbAppendFilename.Size = new System.Drawing.Size(122, 17);
			this.rbAppendFilename.TabIndex = 2;
			this.rbAppendFilename.Text = "Append to filename:";
			this.rbAppendFilename.UseVisualStyleBackColor = true;
			this.rbAppendFilename.CheckedChanged += new System.EventHandler(this.rbAppendFilename_CheckedChanged);
			// 
			// txtAppendFilename
			// 
			this.txtAppendFilename.Location = new System.Drawing.Point(144, 48);
			this.txtAppendFilename.Name = "txtAppendFilename";
			this.txtAppendFilename.Size = new System.Drawing.Size(76, 21);
			this.txtAppendFilename.TabIndex = 3;
			this.txtAppendFilename.Text = "-New";
			this.txtAppendFilename.TextChanged += new System.EventHandler(this.txtAppendFilename_TextChanged);
			// 
			// grpAudioOutput
			// 
			this.grpAudioOutput.Controls.Add(this.rbWavPack);
			this.grpAudioOutput.Controls.Add(this.chkNoAudioOutput);
			this.grpAudioOutput.Controls.Add(this.rbFLAC);
			this.grpAudioOutput.Controls.Add(this.rbWAV);
			this.grpAudioOutput.Location = new System.Drawing.Point(340, 92);
			this.grpAudioOutput.Name = "grpAudioOutput";
			this.grpAudioOutput.Size = new System.Drawing.Size(232, 112);
			this.grpAudioOutput.TabIndex = 2;
			this.grpAudioOutput.TabStop = false;
			this.grpAudioOutput.Text = "Audio Output";
			// 
			// rbWavPack
			// 
			this.rbWavPack.AutoSize = true;
			this.rbWavPack.Location = new System.Drawing.Point(12, 84);
			this.rbWavPack.Name = "rbWavPack";
			this.rbWavPack.Size = new System.Drawing.Size(69, 17);
			this.rbWavPack.TabIndex = 3;
			this.rbWavPack.TabStop = true;
			this.rbWavPack.Text = "WavPack";
			this.rbWavPack.UseVisualStyleBackColor = true;
			// 
			// chkNoAudioOutput
			// 
			this.chkNoAudioOutput.AutoSize = true;
			this.chkNoAudioOutput.Location = new System.Drawing.Point(12, 20);
			this.chkNoAudioOutput.Name = "chkNoAudioOutput";
			this.chkNoAudioOutput.Size = new System.Drawing.Size(209, 17);
			this.chkNoAudioOutput.TabIndex = 0;
			this.chkNoAudioOutput.Text = "Create CUE sheet only (no audio files)";
			this.chkNoAudioOutput.UseVisualStyleBackColor = true;
			// 
			// rbFLAC
			// 
			this.rbFLAC.AutoSize = true;
			this.rbFLAC.Location = new System.Drawing.Point(12, 64);
			this.rbFLAC.Name = "rbFLAC";
			this.rbFLAC.Size = new System.Drawing.Size(50, 17);
			this.rbFLAC.TabIndex = 2;
			this.rbFLAC.Text = "FLAC";
			this.rbFLAC.UseVisualStyleBackColor = true;
			// 
			// rbWAV
			// 
			this.rbWAV.AutoSize = true;
			this.rbWAV.Checked = true;
			this.rbWAV.Location = new System.Drawing.Point(12, 44);
			this.rbWAV.Name = "rbWAV";
			this.rbWAV.Size = new System.Drawing.Size(48, 17);
			this.rbWAV.TabIndex = 1;
			this.rbWAV.TabStop = true;
			this.rbWAV.Text = "WAV";
			this.rbWAV.UseVisualStyleBackColor = true;
			// 
			// btnBatch
			// 
			this.btnBatch.Location = new System.Drawing.Point(420, 394);
			this.btnBatch.Name = "btnBatch";
			this.btnBatch.Size = new System.Drawing.Size(72, 23);
			this.btnBatch.TabIndex = 6;
			this.btnBatch.Text = "Batch...";
			this.btnBatch.UseVisualStyleBackColor = true;
			this.btnBatch.Click += new System.EventHandler(this.btnBatch_Click);
			// 
			// btnFilenameCorrector
			// 
			this.btnFilenameCorrector.Location = new System.Drawing.Point(276, 394);
			this.btnFilenameCorrector.Name = "btnFilenameCorrector";
			this.btnFilenameCorrector.Size = new System.Drawing.Size(136, 23);
			this.btnFilenameCorrector.TabIndex = 7;
			this.btnFilenameCorrector.Text = "Filename Corrector...";
			this.btnFilenameCorrector.UseVisualStyleBackColor = true;
			this.btnFilenameCorrector.Click += new System.EventHandler(this.btnFilenameCorrector_Click);
			// 
			// btnSettings
			// 
			this.btnSettings.Location = new System.Drawing.Point(80, 394);
			this.btnSettings.Name = "btnSettings";
			this.btnSettings.Size = new System.Drawing.Size(136, 23);
			this.btnSettings.TabIndex = 8;
			this.btnSettings.Text = "Advanced Settings...";
			this.btnSettings.UseVisualStyleBackColor = true;
			this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);
			// 
			// frmCUETools
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(580, 457);
			this.Controls.Add(this.btnSettings);
			this.Controls.Add(this.btnFilenameCorrector);
			this.Controls.Add(this.btnBatch);
			this.Controls.Add(this.grpAudioOutput);
			this.Controls.Add(this.grpOutputPathGeneration);
			this.Controls.Add(this.txtStatus);
			this.Controls.Add(this.grpAudioFilenames);
			this.Controls.Add(this.btnAbout);
			this.Controls.Add(this.grpOutputStyle);
			this.Controls.Add(this.grpCUEPaths);
			this.Controls.Add(this.btnConvert);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "frmCUETools";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "CUE Tools";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.frmCUETools_FormClosed);
			this.Load += new System.EventHandler(this.frmCUETools_Load);
			this.grpCUEPaths.ResumeLayout(false);
			this.grpCUEPaths.PerformLayout();
			this.grpOutputStyle.ResumeLayout(false);
			this.grpOutputStyle.PerformLayout();
			this.grpAudioFilenames.ResumeLayout(false);
			this.grpAudioFilenames.PerformLayout();
			this.grpOutputPathGeneration.ResumeLayout(false);
			this.grpOutputPathGeneration.PerformLayout();
			this.grpAudioOutput.ResumeLayout(false);
			this.grpAudioOutput.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnConvert;
		private System.Windows.Forms.GroupBox grpCUEPaths;
		private System.Windows.Forms.Button btnBrowseOutput;
		private System.Windows.Forms.Button btnBrowseInput;
		private System.Windows.Forms.Label lblOutput;
		private System.Windows.Forms.Label lblInput;
		private System.Windows.Forms.TextBox txtOutputPath;
		private System.Windows.Forms.TextBox txtInputPath;
		private System.Windows.Forms.GroupBox grpOutputStyle;
		private System.Windows.Forms.Button btnAbout;
		private System.Windows.Forms.RadioButton rbGapsLeftOut;
		private System.Windows.Forms.RadioButton rbGapsPrepended;
		private System.Windows.Forms.RadioButton rbGapsAppended;
		private System.Windows.Forms.RadioButton rbSingleFile;
		private System.Windows.Forms.GroupBox grpAudioFilenames;
		private System.Windows.Forms.CheckBox chkReplaceSpaces;
		private System.Windows.Forms.TextBox txtTrackFilenameFormat;
		private System.Windows.Forms.Label lblTrackFilenameFormat;
		private System.Windows.Forms.Label lblSingleFilenameFormat;
		private System.Windows.Forms.TextBox txtSingleFilenameFormat;
		private System.Windows.Forms.TextBox txtSpecialExceptions;
		private System.Windows.Forms.CheckBox chkRemoveSpecial;
		private System.Windows.Forms.CheckBox chkKeepOriginalFilenames;
		private System.Windows.Forms.TextBox txtStatus;
		private System.Windows.Forms.GroupBox grpOutputPathGeneration;
		private System.Windows.Forms.RadioButton rbDontGenerate;
		private System.Windows.Forms.RadioButton rbCreateSubdirectory;
		private System.Windows.Forms.RadioButton rbAppendFilename;
		private System.Windows.Forms.TextBox txtAppendFilename;
		private System.Windows.Forms.TextBox txtCreateSubdirectory;
		private System.Windows.Forms.GroupBox grpAudioOutput;
		private System.Windows.Forms.RadioButton rbFLAC;
		private System.Windows.Forms.RadioButton rbWAV;
		private System.Windows.Forms.CheckBox chkNoAudioOutput;
		private System.Windows.Forms.RadioButton rbWavPack;
		private System.Windows.Forms.RadioButton rbCustomFormat;
		private System.Windows.Forms.TextBox txtCustomFormat;
		private System.Windows.Forms.Button btnBatch;
		private System.Windows.Forms.Button btnFilenameCorrector;
		private System.Windows.Forms.Button btnSettings;
	}
}

