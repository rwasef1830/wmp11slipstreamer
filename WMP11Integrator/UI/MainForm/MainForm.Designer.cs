namespace WMP11Slipstreamer
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
            this.buttonHotfixBrowse = new System.Windows.Forms.Button();
            this.textBoxHotfixList = new System.Windows.Forms.TextBox();
            this.labelHotfixes = new System.Windows.Forms.Label();
            this.uxLabelChooseType = new System.Windows.Forms.Label();
            this.addonTypeComboBox = new System.Windows.Forms.ComboBox();
            this.btnWindowsSourceBrowse = new System.Windows.Forms.Button();
            this.textBoxWindowsSource = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.linkLabelWmp11SourceDownload = new System.Windows.Forms.LinkLabel();
            this.btnWmp11SourceBrowse = new System.Windows.Forms.Button();
            this.textBoxWmp11Source = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonIntegrate = new System.Windows.Forms.Button();
            this.progressBarTotalProgress = new System.Windows.Forms.ProgressBar();
            this.progressBarCurrentItem = new System.Windows.Forms.ProgressBar();
            this.checkBoxRemoveCATs = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.labelPreview = new System.Windows.Forms.Label();
            this.pictureBoxPreview = new System.Windows.Forms.PictureBox();
            this.comboBoxIconSelect = new System.Windows.Forms.ComboBox();
            this.checkBoxUseCustIcon = new System.Windows.Forms.CheckBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabelSourceType = new System.Windows.Forms.ToolStripStatusLabel();
            this.aboutLinkLabel = new System.Windows.Forms.LinkLabel();
            this.uxGroupBoxBasicOpts.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // uxGroupBoxBasicOpts
            // 
            this.uxGroupBoxBasicOpts.Controls.Add(this.buttonHotfixBrowse);
            this.uxGroupBoxBasicOpts.Controls.Add(this.textBoxHotfixList);
            this.uxGroupBoxBasicOpts.Controls.Add(this.labelHotfixes);
            this.uxGroupBoxBasicOpts.Controls.Add(this.uxLabelChooseType);
            this.uxGroupBoxBasicOpts.Controls.Add(this.addonTypeComboBox);
            this.uxGroupBoxBasicOpts.Controls.Add(this.btnWindowsSourceBrowse);
            this.uxGroupBoxBasicOpts.Controls.Add(this.textBoxWindowsSource);
            this.uxGroupBoxBasicOpts.Controls.Add(this.label3);
            this.uxGroupBoxBasicOpts.Controls.Add(this.linkLabelWmp11SourceDownload);
            this.uxGroupBoxBasicOpts.Controls.Add(this.btnWmp11SourceBrowse);
            this.uxGroupBoxBasicOpts.Controls.Add(this.textBoxWmp11Source);
            this.uxGroupBoxBasicOpts.Controls.Add(this.label1);
            this.uxGroupBoxBasicOpts.Location = new System.Drawing.Point(11, 12);
            this.uxGroupBoxBasicOpts.Name = "uxGroupBoxBasicOpts";
            this.uxGroupBoxBasicOpts.Size = new System.Drawing.Size(466, 197);
            this.uxGroupBoxBasicOpts.TabIndex = 0;
            this.uxGroupBoxBasicOpts.TabStop = false;
            this.uxGroupBoxBasicOpts.Text = "Basic Options";
            // 
            // buttonHotfixBrowse
            // 
            this.buttonHotfixBrowse.Location = new System.Drawing.Point(432, 167);
            this.buttonHotfixBrowse.Name = "buttonHotfixBrowse";
            this.buttonHotfixBrowse.Size = new System.Drawing.Size(29, 22);
            this.buttonHotfixBrowse.TabIndex = 7;
            this.buttonHotfixBrowse.Text = "...";
            this.buttonHotfixBrowse.UseVisualStyleBackColor = true;
            this.buttonHotfixBrowse.Click += new System.EventHandler(this.buttonHotfixBrowse_Click);
            // 
            // textBoxHotfixList
            // 
            this.textBoxHotfixList.Location = new System.Drawing.Point(7, 169);
            this.textBoxHotfixList.Name = "textBoxHotfixList";
            this.textBoxHotfixList.Size = new System.Drawing.Size(419, 20);
            this.textBoxHotfixList.TabIndex = 6;
            // 
            // labelHotfixes
            // 
            this.labelHotfixes.AutoSize = true;
            this.labelHotfixes.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelHotfixes.Location = new System.Drawing.Point(6, 152);
            this.labelHotfixes.Name = "labelHotfixes";
            this.labelHotfixes.Size = new System.Drawing.Size(291, 13);
            this.labelHotfixes.TabIndex = 9;
            this.labelHotfixes.Text = "Use the \"...\" button to select WMP11 hotfixes to integrate:";
            // 
            // uxLabelChooseType
            // 
            this.uxLabelChooseType.AutoSize = true;
            this.uxLabelChooseType.Location = new System.Drawing.Point(5, 21);
            this.uxLabelChooseType.Name = "uxLabelChooseType";
            this.uxLabelChooseType.Size = new System.Drawing.Size(146, 13);
            this.uxLabelChooseType.TabIndex = 7;
            this.uxLabelChooseType.Text = "Choose WMP11 output type:";
            // 
            // addonTypeComboBox
            // 
            this.addonTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.addonTypeComboBox.FormattingEnabled = true;
            this.addonTypeComboBox.Items.AddRange(new object[] {
            "Vanilla (same as original installer with WGA)",
            "Tweaked (with registry tweaks and no WGA)"});
            this.addonTypeComboBox.Location = new System.Drawing.Point(162, 18);
            this.addonTypeComboBox.Name = "addonTypeComboBox";
            this.addonTypeComboBox.Size = new System.Drawing.Size(298, 21);
            this.addonTypeComboBox.TabIndex = 0;
            // 
            // btnWindowsSourceBrowse
            // 
            this.btnWindowsSourceBrowse.Location = new System.Drawing.Point(432, 121);
            this.btnWindowsSourceBrowse.Name = "btnWindowsSourceBrowse";
            this.btnWindowsSourceBrowse.Size = new System.Drawing.Size(29, 22);
            this.btnWindowsSourceBrowse.TabIndex = 5;
            this.btnWindowsSourceBrowse.Text = "...";
            this.btnWindowsSourceBrowse.UseVisualStyleBackColor = true;
            this.btnWindowsSourceBrowse.Click += new System.EventHandler(this.buttonWindowsSourceBrowse_Click);
            // 
            // textBoxWindowsSource
            // 
            this.textBoxWindowsSource.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.textBoxWindowsSource.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystemDirectories;
            this.textBoxWindowsSource.Location = new System.Drawing.Point(7, 123);
            this.textBoxWindowsSource.Name = "textBoxWindowsSource";
            this.textBoxWindowsSource.Size = new System.Drawing.Size(419, 20);
            this.textBoxWindowsSource.TabIndex = 4;
            this.textBoxWindowsSource.TextChanged += new System.EventHandler(this.textBoxWindowsSource_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(6, 106);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(447, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Enter the path to the Windows source to integrate WMP11 in (must contain a \"i386\"" +
                " folder):";
            // 
            // linkLabelWmp11SourceDownload
            // 
            this.linkLabelWmp11SourceDownload.AutoSize = true;
            this.linkLabelWmp11SourceDownload.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.linkLabelWmp11SourceDownload.LinkArea = new System.Windows.Forms.LinkArea(49, 15);
            this.linkLabelWmp11SourceDownload.Location = new System.Drawing.Point(5, 84);
            this.linkLabelWmp11SourceDownload.Name = "linkLabelWmp11SourceDownload";
            this.linkLabelWmp11SourceDownload.Size = new System.Drawing.Size(444, 17);
            this.linkLabelWmp11SourceDownload.TabIndex = 3;
            this.linkLabelWmp11SourceDownload.TabStop = true;
            this.linkLabelWmp11SourceDownload.Text = "If you don\'t have the installation file, you can download it now (Genuine Windows" +
                " Required).";
            this.linkLabelWmp11SourceDownload.UseCompatibleTextRendering = true;
            this.linkLabelWmp11SourceDownload.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabelWmp11SourceDownload_LinkClicked);
            // 
            // btnWmp11SourceBrowse
            // 
            this.btnWmp11SourceBrowse.Location = new System.Drawing.Point(432, 58);
            this.btnWmp11SourceBrowse.Name = "btnWmp11SourceBrowse";
            this.btnWmp11SourceBrowse.Size = new System.Drawing.Size(28, 22);
            this.btnWmp11SourceBrowse.TabIndex = 2;
            this.btnWmp11SourceBrowse.Text = "...";
            this.btnWmp11SourceBrowse.UseVisualStyleBackColor = true;
            this.btnWmp11SourceBrowse.Click += new System.EventHandler(this.btnWmp11SourceBrowse_Click);
            // 
            // textBoxWmp11Source
            // 
            this.textBoxWmp11Source.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.textBoxWmp11Source.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.FileSystem;
            this.textBoxWmp11Source.Location = new System.Drawing.Point(7, 61);
            this.textBoxWmp11Source.Name = "textBoxWmp11Source";
            this.textBoxWmp11Source.Size = new System.Drawing.Size(419, 20);
            this.textBoxWmp11Source.TabIndex = 1;
            this.textBoxWmp11Source.TextChanged += new System.EventHandler(this.textBoxWmp11Source_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Tahoma", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 44);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(444, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Enter the full path to the Media Player 11 installation file or use the \"...\" but" +
                "ton to locate it:";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(408, 300);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(70, 22);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "E&xit";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // buttonIntegrate
            // 
            this.buttonIntegrate.Enabled = false;
            this.buttonIntegrate.Location = new System.Drawing.Point(331, 300);
            this.buttonIntegrate.Name = "buttonIntegrate";
            this.buttonIntegrate.Size = new System.Drawing.Size(70, 22);
            this.buttonIntegrate.TabIndex = 2;
            this.buttonIntegrate.Text = "&Integrate";
            this.buttonIntegrate.UseVisualStyleBackColor = true;
            this.buttonIntegrate.Click += new System.EventHandler(this.buttonIntegrate_Click);
            // 
            // progressBarTotalProgress
            // 
            this.progressBarTotalProgress.Location = new System.Drawing.Point(11, 341);
            this.progressBarTotalProgress.Name = "progressBarTotalProgress";
            this.progressBarTotalProgress.Size = new System.Drawing.Size(467, 10);
            this.progressBarTotalProgress.TabIndex = 3;
            // 
            // progressBarCurrentItem
            // 
            this.progressBarCurrentItem.Location = new System.Drawing.Point(11, 328);
            this.progressBarCurrentItem.Name = "progressBarCurrentItem";
            this.progressBarCurrentItem.Size = new System.Drawing.Size(467, 10);
            this.progressBarCurrentItem.TabIndex = 4;
            this.progressBarCurrentItem.Visible = false;
            // 
            // checkBoxRemoveCATs
            // 
            this.checkBoxRemoveCATs.AutoSize = true;
            this.checkBoxRemoveCATs.Location = new System.Drawing.Point(5, 48);
            this.checkBoxRemoveCATs.Name = "checkBoxRemoveCATs";
            this.checkBoxRemoveCATs.Size = new System.Drawing.Size(411, 17);
            this.checkBoxRemoveCATs.TabIndex = 2;
            this.checkBoxRemoveCATs.Text = "Do not add security catalog files (for those who disable Windows File Protection)" +
                "";
            this.checkBoxRemoveCATs.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.labelPreview);
            this.groupBox2.Controls.Add(this.pictureBoxPreview);
            this.groupBox2.Controls.Add(this.comboBoxIconSelect);
            this.groupBox2.Controls.Add(this.checkBoxUseCustIcon);
            this.groupBox2.Controls.Add(this.checkBoxRemoveCATs);
            this.groupBox2.Location = new System.Drawing.Point(11, 215);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(466, 71);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Advanced Options";
            // 
            // labelPreview
            // 
            this.labelPreview.AutoSize = true;
            this.labelPreview.Location = new System.Drawing.Point(368, 22);
            this.labelPreview.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelPreview.Name = "labelPreview";
            this.labelPreview.Size = new System.Drawing.Size(49, 13);
            this.labelPreview.TabIndex = 4;
            this.labelPreview.Text = "Preview:";
            this.labelPreview.Visible = false;
            // 
            // pictureBoxPreview
            // 
            this.pictureBoxPreview.Location = new System.Drawing.Point(421, 16);
            this.pictureBoxPreview.Margin = new System.Windows.Forms.Padding(2);
            this.pictureBoxPreview.Name = "pictureBoxPreview";
            this.pictureBoxPreview.Size = new System.Drawing.Size(26, 26);
            this.pictureBoxPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxPreview.TabIndex = 3;
            this.pictureBoxPreview.TabStop = false;
            this.pictureBoxPreview.Visible = false;
            // 
            // comboBoxIconSelect
            // 
            this.comboBoxIconSelect.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxIconSelect.Items.AddRange(new object[] {
            "Boooggy WMP11 Icon",
            "Vista WMP11 Icon",
            "Custom Icon..."});
            this.comboBoxIconSelect.Location = new System.Drawing.Point(213, 20);
            this.comboBoxIconSelect.Margin = new System.Windows.Forms.Padding(2);
            this.comboBoxIconSelect.Name = "comboBoxIconSelect";
            this.comboBoxIconSelect.Size = new System.Drawing.Size(139, 21);
            this.comboBoxIconSelect.TabIndex = 1;
            this.comboBoxIconSelect.Visible = false;
            this.comboBoxIconSelect.SelectedIndexChanged += new System.EventHandler(this.comboBoxIconSelect_SelectedIndexChanged);
            // 
            // checkBoxUseCustIcon
            // 
            this.checkBoxUseCustIcon.AutoSize = true;
            this.checkBoxUseCustIcon.Location = new System.Drawing.Point(5, 22);
            this.checkBoxUseCustIcon.Margin = new System.Windows.Forms.Padding(2);
            this.checkBoxUseCustIcon.Name = "checkBoxUseCustIcon";
            this.checkBoxUseCustIcon.Size = new System.Drawing.Size(200, 17);
            this.checkBoxUseCustIcon.TabIndex = 0;
            this.checkBoxUseCustIcon.Text = "Use a custom icon for wmplayer.exe";
            this.checkBoxUseCustIcon.UseVisualStyleBackColor = true;
            this.checkBoxUseCustIcon.CheckedChanged += new System.EventHandler(this.checkBoxUseCustIcon_CheckedChanged);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusLabel.Location = new System.Drawing.Point(9, 312);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(98, 14);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.Text = "No Operations";
            this.statusLabel.Visible = false;
            // 
            // statusStrip
            // 
            this.statusStrip.AllowMerge = false;
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabelSourceType});
            this.statusStrip.Location = new System.Drawing.Point(0, 357);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Padding = new System.Windows.Forms.Padding(1, 0, 10, 0);
            this.statusStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip.Size = new System.Drawing.Size(488, 22);
            this.statusStrip.SizingGrip = false;
            this.statusStrip.TabIndex = 6;
            // 
            // statusLabelSourceType
            // 
            this.statusLabelSourceType.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.statusLabelSourceType.Font = new System.Drawing.Font("Verdana", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.statusLabelSourceType.Name = "statusLabelSourceType";
            this.statusLabelSourceType.Size = new System.Drawing.Size(43, 17);
            this.statusLabelSourceType.Text = "Ready";
            this.statusLabelSourceType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // aboutLinkLabel
            // 
            this.aboutLinkLabel.AutoSize = true;
            this.aboutLinkLabel.Location = new System.Drawing.Point(418, 7);
            this.aboutLinkLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.aboutLinkLabel.Name = "aboutLinkLabel";
            this.aboutLinkLabel.Size = new System.Drawing.Size(48, 13);
            this.aboutLinkLabel.TabIndex = 8;
            this.aboutLinkLabel.TabStop = true;
            this.aboutLinkLabel.Text = "About...";
            this.aboutLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.aboutStatusLabel_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(488, 379);
            this.Controls.Add(this.aboutLinkLabel);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.progressBarCurrentItem);
            this.Controls.Add(this.progressBarTotalProgress);
            this.Controls.Add(this.buttonIntegrate);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.uxGroupBoxBasicOpts);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Windows Media Player 11 Slipstreamer";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.uxGroupBoxBasicOpts.ResumeLayout(false);
            this.uxGroupBoxBasicOpts.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxPreview)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        System.Windows.Forms.GroupBox uxGroupBoxBasicOpts;
        System.Windows.Forms.Label label1;
        System.Windows.Forms.Button btnWmp11SourceBrowse;
        System.Windows.Forms.TextBox textBoxWmp11Source;
        System.Windows.Forms.LinkLabel linkLabelWmp11SourceDownload;
        System.Windows.Forms.Label label3;
        System.Windows.Forms.Button btnWindowsSourceBrowse;
        System.Windows.Forms.TextBox textBoxWindowsSource;
        System.Windows.Forms.Button buttonIntegrate;
        System.Windows.Forms.ComboBox addonTypeComboBox;
        System.Windows.Forms.Label uxLabelChooseType;
        System.Windows.Forms.GroupBox groupBox2;
        System.Windows.Forms.ProgressBar progressBarTotalProgress;
        System.Windows.Forms.ProgressBar progressBarCurrentItem;
        System.Windows.Forms.Label statusLabel;
        System.Windows.Forms.StatusStrip statusStrip;
        System.Windows.Forms.Button buttonCancel;
        System.Windows.Forms.CheckBox checkBoxRemoveCATs;
        System.Windows.Forms.CheckBox checkBoxUseCustIcon;
        System.Windows.Forms.ComboBox comboBoxIconSelect;
        System.Windows.Forms.PictureBox pictureBoxPreview;
        System.Windows.Forms.Label labelPreview;
        System.Windows.Forms.ToolStripStatusLabel statusLabelSourceType;
        System.Windows.Forms.LinkLabel aboutLinkLabel;
        System.Windows.Forms.Button buttonHotfixBrowse;
        System.Windows.Forms.Label labelHotfixes;
        System.Windows.Forms.TextBox textBoxHotfixList;
    }
}

