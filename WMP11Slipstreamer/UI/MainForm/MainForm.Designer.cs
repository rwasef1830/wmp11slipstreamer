namespace Epsilon.WMP11Slipstreamer
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.uxGroupBoxBasicOpts = new System.Windows.Forms.GroupBox();
            this.uxLinkAbout = new System.Windows.Forms.LinkLabel();
            this.uxButtonHotfixPicker = new System.Windows.Forms.Button();
            this.uxTextBoxHotfixLine = new System.Windows.Forms.TextBox();
            this.uxLabelEnterHotfixLine = new System.Windows.Forms.Label();
            this.uxLabelChooseType = new System.Windows.Forms.Label();
            this.uxComboType = new System.Windows.Forms.ComboBox();
            this.uxButtonWinSrcPicker = new System.Windows.Forms.Button();
            this.uxTextBoxWinSrc = new System.Windows.Forms.TextBox();
            this.uxLabelEnterWinSrc = new System.Windows.Forms.Label();
            this.uxLinkDownloadWmpRedist = new System.Windows.Forms.LinkLabel();
            this.uxButtonWmpRedistPicker = new System.Windows.Forms.Button();
            this.uxTextBoxWmpRedist = new System.Windows.Forms.TextBox();
            this.uxLabelEnterWmpRedist = new System.Windows.Forms.Label();
            this.uxButtonCancel = new System.Windows.Forms.Button();
            this.uxButtonIntegrate = new System.Windows.Forms.Button();
            this.uxProgressBarOverall = new System.Windows.Forms.ProgressBar();
            this.uxProgressBarCurrent = new System.Windows.Forms.ProgressBar();
            this.uxCheckBoxNoCats = new System.Windows.Forms.CheckBox();
            this.uxGroupBoxAdvOpts = new System.Windows.Forms.GroupBox();
            this.uxLabelPreview = new System.Windows.Forms.Label();
            this.uxPictureBoxCustomIconPreview = new System.Windows.Forms.PictureBox();
            this.uxComboBoxCustomIcon = new System.Windows.Forms.ComboBox();
            this.uxCheckBoxCustomIcon = new System.Windows.Forms.CheckBox();
            this.uxLabelOperation = new System.Windows.Forms.Label();
            this.uxStatusStrip = new System.Windows.Forms.StatusStrip();
            this.uxStatusLabelSourceType = new System.Windows.Forms.ToolStripStatusLabel();
            this.uxGroupBoxBasicOpts.SuspendLayout();
            this.uxGroupBoxAdvOpts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxPictureBoxCustomIconPreview)).BeginInit();
            this.uxStatusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // uxGroupBoxBasicOpts
            // 
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxLinkAbout);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxButtonHotfixPicker);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxTextBoxHotfixLine);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxLabelEnterHotfixLine);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxLabelChooseType);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxComboType);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxButtonWinSrcPicker);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxTextBoxWinSrc);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxLabelEnterWinSrc);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxLinkDownloadWmpRedist);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxButtonWmpRedistPicker);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxTextBoxWmpRedist);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxLabelEnterWmpRedist);
            this.uxGroupBoxBasicOpts.Location = new System.Drawing.Point(14, 15);
            this.uxGroupBoxBasicOpts.Margin = new System.Windows.Forms.Padding(4);
            this.uxGroupBoxBasicOpts.Name = "uxGroupBoxBasicOpts";
            this.uxGroupBoxBasicOpts.Padding = new System.Windows.Forms.Padding(4);
            this.uxGroupBoxBasicOpts.Size = new System.Drawing.Size(647, 246);
            this.uxGroupBoxBasicOpts.TabIndex = 0;
            this.uxGroupBoxBasicOpts.TabStop = false;
            this.uxGroupBoxBasicOpts.Text = "Placeholder";
            // 
            // uxLinkAbout
            // 
            this.uxLinkAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.uxLinkAbout.AutoSize = true;
            this.uxLinkAbout.Location = new System.Drawing.Point(568, -3);
            this.uxLinkAbout.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.uxLinkAbout.Name = "uxLinkAbout";
            this.uxLinkAbout.Size = new System.Drawing.Size(83, 17);
            this.uxLinkAbout.TabIndex = 0;
            this.uxLinkAbout.TabStop = true;
            this.uxLinkAbout.Text = "Placeholder";
            this.uxLinkAbout.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.uxLinkAbout.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.aboutStatusLabel_Click);
            // 
            // uxButtonHotfixPicker
            // 
            this.uxButtonHotfixPicker.Location = new System.Drawing.Point(603, 208);
            this.uxButtonHotfixPicker.Margin = new System.Windows.Forms.Padding(4);
            this.uxButtonHotfixPicker.Name = "uxButtonHotfixPicker";
            this.uxButtonHotfixPicker.Size = new System.Drawing.Size(36, 28);
            this.uxButtonHotfixPicker.TabIndex = 8;
            this.uxButtonHotfixPicker.Text = "...";
            this.uxButtonHotfixPicker.UseVisualStyleBackColor = true;
            this.uxButtonHotfixPicker.Click += new System.EventHandler(this.buttonHotfixBrowse_Click);
            // 
            // uxTextBoxHotfixLine
            // 
            this.uxTextBoxHotfixLine.Location = new System.Drawing.Point(9, 211);
            this.uxTextBoxHotfixLine.Margin = new System.Windows.Forms.Padding(4);
            this.uxTextBoxHotfixLine.Name = "uxTextBoxHotfixLine";
            this.uxTextBoxHotfixLine.Size = new System.Drawing.Size(586, 22);
            this.uxTextBoxHotfixLine.TabIndex = 7;
            // 
            // uxLabelEnterHotfixLine
            // 
            this.uxLabelEnterHotfixLine.AutoSize = true;
            this.uxLabelEnterHotfixLine.Font = new System.Drawing.Font("Tahoma", 7.8F);
            this.uxLabelEnterHotfixLine.Location = new System.Drawing.Point(8, 190);
            this.uxLabelEnterHotfixLine.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.uxLabelEnterHotfixLine.Name = "uxLabelEnterHotfixLine";
            this.uxLabelEnterHotfixLine.Size = new System.Drawing.Size(77, 17);
            this.uxLabelEnterHotfixLine.TabIndex = 9;
            this.uxLabelEnterHotfixLine.Text = "Placeholder";
            // 
            // uxLabelChooseType
            // 
            this.uxLabelChooseType.AutoSize = true;
            this.uxLabelChooseType.Location = new System.Drawing.Point(6, 26);
            this.uxLabelChooseType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.uxLabelChooseType.Name = "uxLabelChooseType";
            this.uxLabelChooseType.Size = new System.Drawing.Size(83, 17);
            this.uxLabelChooseType.TabIndex = 0;
            this.uxLabelChooseType.Text = "Placeholder";
            // 
            // uxComboType
            // 
            this.uxComboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.uxComboType.FormattingEnabled = true;
            this.uxComboType.Items.AddRange(new object[] {
            "Vanilla",
            "Tweaked"});
            this.uxComboType.Location = new System.Drawing.Point(246, 23);
            this.uxComboType.Margin = new System.Windows.Forms.Padding(4);
            this.uxComboType.Name = "uxComboType";
            this.uxComboType.Size = new System.Drawing.Size(393, 24);
            this.uxComboType.TabIndex = 1;
            // 
            // uxButtonWinSrcPicker
            // 
            this.uxButtonWinSrcPicker.Location = new System.Drawing.Point(603, 151);
            this.uxButtonWinSrcPicker.Margin = new System.Windows.Forms.Padding(4);
            this.uxButtonWinSrcPicker.Name = "uxButtonWinSrcPicker";
            this.uxButtonWinSrcPicker.Size = new System.Drawing.Size(36, 28);
            this.uxButtonWinSrcPicker.TabIndex = 6;
            this.uxButtonWinSrcPicker.Text = "...";
            this.uxButtonWinSrcPicker.UseVisualStyleBackColor = true;
            this.uxButtonWinSrcPicker.Click += new System.EventHandler(this.buttonWindowsSourceBrowse_Click);
            // 
            // uxTextBoxWinSrc
            // 
            this.uxTextBoxWinSrc.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.uxTextBoxWinSrc.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.uxTextBoxWinSrc.Location = new System.Drawing.Point(9, 154);
            this.uxTextBoxWinSrc.Margin = new System.Windows.Forms.Padding(4);
            this.uxTextBoxWinSrc.Name = "uxTextBoxWinSrc";
            this.uxTextBoxWinSrc.Size = new System.Drawing.Size(584, 22);
            this.uxTextBoxWinSrc.TabIndex = 5;
            this.uxTextBoxWinSrc.TextChanged += new System.EventHandler(this.textBoxWindowsSource_TextChanged);
            // 
            // uxLabelEnterWinSrc
            // 
            this.uxLabelEnterWinSrc.AutoSize = true;
            this.uxLabelEnterWinSrc.Font = new System.Drawing.Font("Tahoma", 7.8F);
            this.uxLabelEnterWinSrc.Location = new System.Drawing.Point(8, 132);
            this.uxLabelEnterWinSrc.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.uxLabelEnterWinSrc.Name = "uxLabelEnterWinSrc";
            this.uxLabelEnterWinSrc.Size = new System.Drawing.Size(77, 17);
            this.uxLabelEnterWinSrc.TabIndex = 5;
            this.uxLabelEnterWinSrc.Text = "Placeholder";
            // 
            // uxLinkDownloadWmpRedist
            // 
            this.uxLinkDownloadWmpRedist.AutoSize = true;
            this.uxLinkDownloadWmpRedist.Font = new System.Drawing.Font("Tahoma", 7.8F);
            this.uxLinkDownloadWmpRedist.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.uxLinkDownloadWmpRedist.Location = new System.Drawing.Point(6, 105);
            this.uxLinkDownloadWmpRedist.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.uxLinkDownloadWmpRedist.Name = "uxLinkDownloadWmpRedist";
            this.uxLinkDownloadWmpRedist.Size = new System.Drawing.Size(77, 17);
            this.uxLinkDownloadWmpRedist.TabIndex = 4;
            this.uxLinkDownloadWmpRedist.TabStop = true;
            this.uxLinkDownloadWmpRedist.Text = "Placeholder";
            this.uxLinkDownloadWmpRedist.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelWmp11SourceDownload_LinkClicked);
            // 
            // uxButtonWmpRedistPicker
            // 
            this.uxButtonWmpRedistPicker.Location = new System.Drawing.Point(604, 73);
            this.uxButtonWmpRedistPicker.Margin = new System.Windows.Forms.Padding(4);
            this.uxButtonWmpRedistPicker.Name = "uxButtonWmpRedistPicker";
            this.uxButtonWmpRedistPicker.Size = new System.Drawing.Size(35, 28);
            this.uxButtonWmpRedistPicker.TabIndex = 3;
            this.uxButtonWmpRedistPicker.Text = "...";
            this.uxButtonWmpRedistPicker.UseVisualStyleBackColor = true;
            this.uxButtonWmpRedistPicker.Click += new System.EventHandler(this.btnWmp11SourceBrowse_Click);
            // 
            // uxTextBoxWmpRedist
            // 
            this.uxTextBoxWmpRedist.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.uxTextBoxWmpRedist.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.uxTextBoxWmpRedist.Location = new System.Drawing.Point(9, 76);
            this.uxTextBoxWmpRedist.Margin = new System.Windows.Forms.Padding(4);
            this.uxTextBoxWmpRedist.Name = "uxTextBoxWmpRedist";
            this.uxTextBoxWmpRedist.Size = new System.Drawing.Size(586, 22);
            this.uxTextBoxWmpRedist.TabIndex = 2;
            this.uxTextBoxWmpRedist.TextChanged += new System.EventHandler(this.textBoxWmp11Source_TextChanged);
            // 
            // uxLabelEnterWmpRedist
            // 
            this.uxLabelEnterWmpRedist.AutoSize = true;
            this.uxLabelEnterWmpRedist.Font = new System.Drawing.Font("Tahoma", 7.8F);
            this.uxLabelEnterWmpRedist.Location = new System.Drawing.Point(8, 55);
            this.uxLabelEnterWmpRedist.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.uxLabelEnterWmpRedist.Name = "uxLabelEnterWmpRedist";
            this.uxLabelEnterWmpRedist.Size = new System.Drawing.Size(77, 17);
            this.uxLabelEnterWmpRedist.TabIndex = 0;
            this.uxLabelEnterWmpRedist.Text = "Placeholder";
            // 
            // uxButtonCancel
            // 
            this.uxButtonCancel.Location = new System.Drawing.Point(573, 374);
            this.uxButtonCancel.Margin = new System.Windows.Forms.Padding(4);
            this.uxButtonCancel.Name = "uxButtonCancel";
            this.uxButtonCancel.Size = new System.Drawing.Size(88, 28);
            this.uxButtonCancel.TabIndex = 3;
            this.uxButtonCancel.Text = "E&xit";
            this.uxButtonCancel.UseVisualStyleBackColor = true;
            this.uxButtonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // uxButtonIntegrate
            // 
            this.uxButtonIntegrate.Enabled = false;
            this.uxButtonIntegrate.Location = new System.Drawing.Point(477, 374);
            this.uxButtonIntegrate.Margin = new System.Windows.Forms.Padding(4);
            this.uxButtonIntegrate.Name = "uxButtonIntegrate";
            this.uxButtonIntegrate.Size = new System.Drawing.Size(88, 28);
            this.uxButtonIntegrate.TabIndex = 2;
            this.uxButtonIntegrate.Text = "Placeholder";
            this.uxButtonIntegrate.UseVisualStyleBackColor = true;
            this.uxButtonIntegrate.Click += new System.EventHandler(this.buttonIntegrate_Click);
            // 
            // uxProgressBarOverall
            // 
            this.uxProgressBarOverall.Location = new System.Drawing.Point(14, 426);
            this.uxProgressBarOverall.Margin = new System.Windows.Forms.Padding(4);
            this.uxProgressBarOverall.Name = "uxProgressBarOverall";
            this.uxProgressBarOverall.Size = new System.Drawing.Size(647, 12);
            this.uxProgressBarOverall.TabIndex = 3;
            // 
            // uxProgressBarCurrent
            // 
            this.uxProgressBarCurrent.Location = new System.Drawing.Point(14, 410);
            this.uxProgressBarCurrent.Margin = new System.Windows.Forms.Padding(4);
            this.uxProgressBarCurrent.Name = "uxProgressBarCurrent";
            this.uxProgressBarCurrent.Size = new System.Drawing.Size(647, 12);
            this.uxProgressBarCurrent.TabIndex = 4;
            this.uxProgressBarCurrent.Visible = false;
            // 
            // uxCheckBoxNoCats
            // 
            this.uxCheckBoxNoCats.AutoSize = true;
            this.uxCheckBoxNoCats.Location = new System.Drawing.Point(6, 60);
            this.uxCheckBoxNoCats.Margin = new System.Windows.Forms.Padding(4);
            this.uxCheckBoxNoCats.Name = "uxCheckBoxNoCats";
            this.uxCheckBoxNoCats.Size = new System.Drawing.Size(102, 21);
            this.uxCheckBoxNoCats.TabIndex = 3;
            this.uxCheckBoxNoCats.Text = "Placeholder";
            this.uxCheckBoxNoCats.UseVisualStyleBackColor = true;
            // 
            // uxGroupBoxAdvOpts
            // 
            this.uxGroupBoxAdvOpts.Controls.Add(this.uxLabelPreview);
            this.uxGroupBoxAdvOpts.Controls.Add(this.uxPictureBoxCustomIconPreview);
            this.uxGroupBoxAdvOpts.Controls.Add(this.uxComboBoxCustomIcon);
            this.uxGroupBoxAdvOpts.Controls.Add(this.uxCheckBoxCustomIcon);
            this.uxGroupBoxAdvOpts.Controls.Add(this.uxCheckBoxNoCats);
            this.uxGroupBoxAdvOpts.Location = new System.Drawing.Point(14, 269);
            this.uxGroupBoxAdvOpts.Margin = new System.Windows.Forms.Padding(4);
            this.uxGroupBoxAdvOpts.Name = "uxGroupBoxAdvOpts";
            this.uxGroupBoxAdvOpts.Padding = new System.Windows.Forms.Padding(4);
            this.uxGroupBoxAdvOpts.Size = new System.Drawing.Size(647, 89);
            this.uxGroupBoxAdvOpts.TabIndex = 1;
            this.uxGroupBoxAdvOpts.TabStop = false;
            this.uxGroupBoxAdvOpts.Text = "Placeholder";
            // 
            // uxLabelPreview
            // 
            this.uxLabelPreview.AutoSize = true;
            this.uxLabelPreview.Location = new System.Drawing.Point(524, 30);
            this.uxLabelPreview.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.uxLabelPreview.Name = "uxLabelPreview";
            this.uxLabelPreview.Size = new System.Drawing.Size(83, 17);
            this.uxLabelPreview.TabIndex = 2;
            this.uxLabelPreview.Text = "Placeholder";
            this.uxLabelPreview.Visible = false;
            // 
            // uxPictureBoxCustomIconPreview
            // 
            this.uxPictureBoxCustomIconPreview.Location = new System.Drawing.Point(606, 23);
            this.uxPictureBoxCustomIconPreview.Margin = new System.Windows.Forms.Padding(2);
            this.uxPictureBoxCustomIconPreview.Name = "uxPictureBoxCustomIconPreview";
            this.uxPictureBoxCustomIconPreview.Size = new System.Drawing.Size(32, 32);
            this.uxPictureBoxCustomIconPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.uxPictureBoxCustomIconPreview.TabIndex = 3;
            this.uxPictureBoxCustomIconPreview.TabStop = false;
            this.uxPictureBoxCustomIconPreview.Visible = false;
            // 
            // uxComboBoxCustomIcon
            // 
            this.uxComboBoxCustomIcon.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.uxComboBoxCustomIcon.Items.AddRange(new object[] {
            "Boooggy",
            "Vista",
            "Custom..."});
            this.uxComboBoxCustomIcon.Location = new System.Drawing.Point(369, 27);
            this.uxComboBoxCustomIcon.Margin = new System.Windows.Forms.Padding(2);
            this.uxComboBoxCustomIcon.Name = "uxComboBoxCustomIcon";
            this.uxComboBoxCustomIcon.Size = new System.Drawing.Size(142, 24);
            this.uxComboBoxCustomIcon.TabIndex = 1;
            this.uxComboBoxCustomIcon.Visible = false;
            this.uxComboBoxCustomIcon.SelectedIndexChanged += new System.EventHandler(this.comboBoxIconSelect_SelectedIndexChanged);
            // 
            // uxCheckBoxCustomIcon
            // 
            this.uxCheckBoxCustomIcon.AutoSize = true;
            this.uxCheckBoxCustomIcon.Location = new System.Drawing.Point(6, 28);
            this.uxCheckBoxCustomIcon.Margin = new System.Windows.Forms.Padding(2);
            this.uxCheckBoxCustomIcon.Name = "uxCheckBoxCustomIcon";
            this.uxCheckBoxCustomIcon.Size = new System.Drawing.Size(102, 21);
            this.uxCheckBoxCustomIcon.TabIndex = 0;
            this.uxCheckBoxCustomIcon.Text = "Placeholder";
            this.uxCheckBoxCustomIcon.UseVisualStyleBackColor = true;
            this.uxCheckBoxCustomIcon.CheckedChanged += new System.EventHandler(this.checkBoxUseCustIcon_CheckedChanged);
            // 
            // uxLabelOperation
            // 
            this.uxLabelOperation.AutoSize = true;
            this.uxLabelOperation.Font = new System.Drawing.Font("Verdana", 9F);
            this.uxLabelOperation.Location = new System.Drawing.Point(11, 390);
            this.uxLabelOperation.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.uxLabelOperation.Name = "uxLabelOperation";
            this.uxLabelOperation.Size = new System.Drawing.Size(92, 18);
            this.uxLabelOperation.TabIndex = 5;
            this.uxLabelOperation.Text = "Placeholder";
            this.uxLabelOperation.Visible = false;
            // 
            // uxStatusStrip
            // 
            this.uxStatusStrip.AllowMerge = false;
            this.uxStatusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.uxStatusLabelSourceType});
            this.uxStatusStrip.Location = new System.Drawing.Point(0, 452);
            this.uxStatusStrip.Name = "uxStatusStrip";
            this.uxStatusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 12, 0);
            this.uxStatusStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.uxStatusStrip.Size = new System.Drawing.Size(674, 22);
            this.uxStatusStrip.SizingGrip = false;
            this.uxStatusStrip.TabIndex = 6;
            // 
            // uxStatusLabelSourceType
            // 
            this.uxStatusLabelSourceType.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.uxStatusLabelSourceType.Font = new System.Drawing.Font("Verdana", 7.8F);
            this.uxStatusLabelSourceType.Name = "uxStatusLabelSourceType";
            this.uxStatusLabelSourceType.Size = new System.Drawing.Size(86, 17);
            this.uxStatusLabelSourceType.Text = "Placeholder";
            this.uxStatusLabelSourceType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(120F, 120F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(674, 474);
            this.Controls.Add(this.uxStatusStrip);
            this.Controls.Add(this.uxLabelOperation);
            this.Controls.Add(this.uxProgressBarCurrent);
            this.Controls.Add(this.uxProgressBarOverall);
            this.Controls.Add(this.uxButtonIntegrate);
            this.Controls.Add(this.uxButtonCancel);
            this.Controls.Add(this.uxGroupBoxAdvOpts);
            this.Controls.Add(this.uxGroupBoxBasicOpts);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Windows Media Player 11 Slipstreamer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.uxGroupBoxBasicOpts.ResumeLayout(false);
            this.uxGroupBoxBasicOpts.PerformLayout();
            this.uxGroupBoxAdvOpts.ResumeLayout(false);
            this.uxGroupBoxAdvOpts.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.uxPictureBoxCustomIconPreview)).EndInit();
            this.uxStatusStrip.ResumeLayout(false);
            this.uxStatusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        System.Windows.Forms.GroupBox uxGroupBoxBasicOpts;
        System.Windows.Forms.Label uxLabelEnterWmpRedist;
        System.Windows.Forms.Button uxButtonWmpRedistPicker;
        System.Windows.Forms.TextBox uxTextBoxWmpRedist;
        System.Windows.Forms.LinkLabel uxLinkDownloadWmpRedist;
        System.Windows.Forms.Label uxLabelEnterWinSrc;
        System.Windows.Forms.Button uxButtonWinSrcPicker;
        System.Windows.Forms.TextBox uxTextBoxWinSrc;
        System.Windows.Forms.Button uxButtonIntegrate;
        System.Windows.Forms.ComboBox uxComboType;
        System.Windows.Forms.Label uxLabelChooseType;
        System.Windows.Forms.GroupBox uxGroupBoxAdvOpts;
        System.Windows.Forms.ProgressBar uxProgressBarOverall;
        System.Windows.Forms.ProgressBar uxProgressBarCurrent;
        System.Windows.Forms.Label uxLabelOperation;
        System.Windows.Forms.StatusStrip uxStatusStrip;
        System.Windows.Forms.Button uxButtonCancel;
        System.Windows.Forms.CheckBox uxCheckBoxNoCats;
        System.Windows.Forms.CheckBox uxCheckBoxCustomIcon;
        System.Windows.Forms.ComboBox uxComboBoxCustomIcon;
        System.Windows.Forms.PictureBox uxPictureBoxCustomIconPreview;
        System.Windows.Forms.Label uxLabelPreview;
        System.Windows.Forms.ToolStripStatusLabel uxStatusLabelSourceType;
        System.Windows.Forms.LinkLabel uxLinkAbout;
        System.Windows.Forms.Button uxButtonHotfixPicker;
        System.Windows.Forms.Label uxLabelEnterHotfixLine;
        System.Windows.Forms.TextBox uxTextBoxHotfixLine;
    }
}

