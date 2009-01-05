namespace Epsilon.WMP11Slipstreamer
{
    partial class AboutBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox));
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.logoPictureBox = new System.Windows.Forms.PictureBox();
            this.uxLabelProductName = new System.Windows.Forms.Label();
            this.uxTextBoxDescription = new System.Windows.Forms.TextBox();
            this.uxLabelVersion = new System.Windows.Forms.Label();
            this.uxLabelCopyright = new System.Windows.Forms.Label();
            this.uxLinkLabelWebSite = new System.Windows.Forms.LinkLabel();
            this.uxButtonOk = new System.Windows.Forms.Button();
            this.uxLabelTranslator = new System.Windows.Forms.Label();
            this.tableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 67F));
            this.tableLayoutPanel.Controls.Add(this.logoPictureBox, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.uxLabelProductName, 1, 0);
            this.tableLayoutPanel.Controls.Add(this.uxTextBoxDescription, 1, 4);
            this.tableLayoutPanel.Controls.Add(this.uxLabelVersion, 1, 1);
            this.tableLayoutPanel.Controls.Add(this.uxLabelCopyright, 1, 2);
            this.tableLayoutPanel.Controls.Add(this.uxLinkLabelWebSite, 1, 3);
            this.tableLayoutPanel.Controls.Add(this.uxButtonOk, 1, 6);
            this.tableLayoutPanel.Controls.Add(this.uxLabelTranslator, 1, 5);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(11, 11);
            this.tableLayoutPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 7;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel.Size = new System.Drawing.Size(557, 338);
            this.tableLayoutPanel.TabIndex = 0;
            // 
            // logoPictureBox
            // 
            this.logoPictureBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.logoPictureBox.Image = global::Epsilon.WMP11Slipstreamer.Properties.Resources.Logo;
            this.logoPictureBox.Location = new System.Drawing.Point(3, 4);
            this.logoPictureBox.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.logoPictureBox.Name = "logoPictureBox";
            this.tableLayoutPanel.SetRowSpan(this.logoPictureBox, 7);
            this.logoPictureBox.Size = new System.Drawing.Size(177, 332);
            this.logoPictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.logoPictureBox.TabIndex = 12;
            this.logoPictureBox.TabStop = false;
            // 
            // uxLabelProductName
            // 
            this.uxLabelProductName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxLabelProductName.Location = new System.Drawing.Point(191, 0);
            this.uxLabelProductName.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.uxLabelProductName.MaximumSize = new System.Drawing.Size(0, 21);
            this.uxLabelProductName.Name = "uxLabelProductName";
            this.uxLabelProductName.Size = new System.Drawing.Size(363, 21);
            this.uxLabelProductName.TabIndex = 19;
            this.uxLabelProductName.Text = "Windows Media Player 11 Slipstreamer";
            this.uxLabelProductName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxTextBoxDescription
            // 
            this.uxTextBoxDescription.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxTextBoxDescription.Location = new System.Drawing.Point(191, 84);
            this.uxTextBoxDescription.Margin = new System.Windows.Forms.Padding(8, 4, 3, 4);
            this.uxTextBoxDescription.Multiline = true;
            this.uxTextBoxDescription.Name = "uxTextBoxDescription";
            this.uxTextBoxDescription.ReadOnly = true;
            this.uxTextBoxDescription.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.uxTextBoxDescription.Size = new System.Drawing.Size(363, 198);
            this.uxTextBoxDescription.TabIndex = 23;
            this.uxTextBoxDescription.TabStop = false;
            this.uxTextBoxDescription.Text = resources.GetString("uxTextBoxDescription.Text");
            // 
            // uxLabelVersion
            // 
            this.uxLabelVersion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxLabelVersion.Location = new System.Drawing.Point(191, 21);
            this.uxLabelVersion.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.uxLabelVersion.MaximumSize = new System.Drawing.Size(0, 21);
            this.uxLabelVersion.Name = "uxLabelVersion";
            this.uxLabelVersion.Size = new System.Drawing.Size(363, 21);
            this.uxLabelVersion.TabIndex = 0;
            this.uxLabelVersion.Text = "Version Unknown";
            this.uxLabelVersion.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxLabelCopyright
            // 
            this.uxLabelCopyright.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uxLabelCopyright.Location = new System.Drawing.Point(191, 42);
            this.uxLabelCopyright.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.uxLabelCopyright.MaximumSize = new System.Drawing.Size(0, 21);
            this.uxLabelCopyright.Name = "uxLabelCopyright";
            this.uxLabelCopyright.Size = new System.Drawing.Size(363, 21);
            this.uxLabelCopyright.TabIndex = 21;
            this.uxLabelCopyright.Text = "Copyright © boooggy and n7Epsilon";
            this.uxLabelCopyright.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // uxLinkLabelWebSite
            // 
            this.uxLinkLabelWebSite.AutoSize = true;
            this.uxLinkLabelWebSite.Location = new System.Drawing.Point(191, 63);
            this.uxLinkLabelWebSite.Margin = new System.Windows.Forms.Padding(8, 0, 3, 0);
            this.uxLinkLabelWebSite.Name = "uxLinkLabelWebSite";
            this.uxLinkLabelWebSite.Size = new System.Drawing.Size(197, 17);
            this.uxLinkLabelWebSite.TabIndex = 25;
            this.uxLinkLabelWebSite.TabStop = true;
            this.uxLinkLabelWebSite.Text = "Click here to go to the website";
            this.uxLinkLabelWebSite.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
            // 
            // uxButtonOk
            // 
            this.uxButtonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.uxButtonOk.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.uxButtonOk.Location = new System.Drawing.Point(455, 309);
            this.uxButtonOk.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.uxButtonOk.Name = "uxButtonOk";
            this.uxButtonOk.Size = new System.Drawing.Size(99, 27);
            this.uxButtonOk.TabIndex = 26;
            this.uxButtonOk.Text = global::Epsilon.WMP11Slipstreamer.Localization.Msg.dlgAbout_ButtonOK;
            // 
            // uxLabelTranslator
            // 
            this.uxLabelTranslator.AutoSize = true;
            this.uxLabelTranslator.Location = new System.Drawing.Point(186, 286);
            this.uxLabelTranslator.Name = "uxLabelTranslator";
            this.uxLabelTranslator.Size = new System.Drawing.Size(148, 17);
            this.uxLabelTranslator.TabIndex = 27;
            this.uxLabelTranslator.Text = "Current translation by:";
            // 
            // AboutBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(579, 360);
            this.Controls.Add(this.tableLayoutPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutBox";
            this.Padding = new System.Windows.Forms.Padding(11, 11, 11, 11);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About WMP11 Slipstreamer";
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.logoPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        System.Windows.Forms.PictureBox logoPictureBox;
        System.Windows.Forms.Label uxLabelProductName;
        System.Windows.Forms.Label uxLabelVersion;
        System.Windows.Forms.Label uxLabelCopyright;
        System.Windows.Forms.TextBox uxTextBoxDescription;
        System.Windows.Forms.LinkLabel uxLinkLabelWebSite;
        private System.Windows.Forms.Button uxButtonOk;
        private System.Windows.Forms.Label uxLabelTranslator;
    }
}
