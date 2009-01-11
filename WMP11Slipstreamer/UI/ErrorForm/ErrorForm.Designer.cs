using Epsilon.WMP11Slipstreamer.Localization;
namespace Epsilon.WMP11Slipstreamer
{
    partial class ErrorForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ErrorForm));
            this.uxLabelHeader = new System.Windows.Forms.Label();
            this.uxTextBoxErrorLog = new System.Windows.Forms.TextBox();
            this.uxButtonClose = new System.Windows.Forms.Button();
            this.uxLabelFooter = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // uxLabelHeader
            // 
            this.uxLabelHeader.AutoSize = true;
            this.uxLabelHeader.Location = new System.Drawing.Point(14, 9);
            this.uxLabelHeader.Name = "uxLabelHeader";
            this.uxLabelHeader.Size = new System.Drawing.Size(454, 34);
            this.uxLabelHeader.TabIndex = 0;
            this.uxLabelHeader.Text = "An unhandled exception has occurred in the worker thread.\r\nPlease copy the follow" +
                "ing diagnostic information and post a bug report.";
            // 
            // uxTextBoxErrorLog
            // 
            this.uxTextBoxErrorLog.BackColor = System.Drawing.Color.WhiteSmoke;
            this.uxTextBoxErrorLog.ForeColor = System.Drawing.Color.Black;
            this.uxTextBoxErrorLog.Location = new System.Drawing.Point(17, 46);
            this.uxTextBoxErrorLog.Multiline = true;
            this.uxTextBoxErrorLog.Name = "uxTextBoxErrorLog";
            this.uxTextBoxErrorLog.ReadOnly = true;
            this.uxTextBoxErrorLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.uxTextBoxErrorLog.Size = new System.Drawing.Size(569, 244);
            this.uxTextBoxErrorLog.TabIndex = 0;
            // 
            // uxButtonClose
            // 
            this.uxButtonClose.Location = new System.Drawing.Point(501, 296);
            this.uxButtonClose.Name = "uxButtonClose";
            this.uxButtonClose.Size = new System.Drawing.Size(86, 29);
            this.uxButtonClose.TabIndex = 1;
            this.uxButtonClose.Text = global::Epsilon.WMP11Slipstreamer.Localization.Msg.dlgError_ButtonClose;
            this.uxButtonClose.UseVisualStyleBackColor = true;
            this.uxButtonClose.Click += new System.EventHandler(this.uxButtonClose_Click);
            // 
            // uxLabelFooter
            // 
            this.uxLabelFooter.AutoSize = true;
            this.uxLabelFooter.Location = new System.Drawing.Point(14, 302);
            this.uxLabelFooter.Name = "uxLabelFooter";
            this.uxLabelFooter.Size = new System.Drawing.Size(280, 17);
            this.uxLabelFooter.TabIndex = 2;
            this.uxLabelFooter.Text = "Click on \"Close\" to return to the application.";
            // 
            // ErrorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 337);
            this.Controls.Add(this.uxLabelFooter);
            this.Controls.Add(this.uxButtonClose);
            this.Controls.Add(this.uxTextBoxErrorLog);
            this.Controls.Add(this.uxLabelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ErrorForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Unhandled Exception";
            this.Load += new System.EventHandler(this.ErrorForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        System.Windows.Forms.Label uxLabelHeader;
        public System.Windows.Forms.TextBox uxTextBoxErrorLog;
        System.Windows.Forms.Button uxButtonClose;
        System.Windows.Forms.Label uxLabelFooter;
    }
}