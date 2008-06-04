namespace WMP11Slipstreamer
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
            this.label1 = new System.Windows.Forms.Label();
            this.errorLogBox = new System.Windows.Forms.TextBox();
            this.buttonClose = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(441, 34);
            this.label1.TabIndex = 0;
            this.label1.Text = "An unhandled exception has occurred in the worker thread.\r\nPlease copy the follow" +
                "ing diagnostic information and post a bug report.";
            // 
            // errorLogBox
            // 
            this.errorLogBox.BackColor = System.Drawing.Color.WhiteSmoke;
            this.errorLogBox.ForeColor = System.Drawing.Color.Black;
            this.errorLogBox.Location = new System.Drawing.Point(15, 46);
            this.errorLogBox.Multiline = true;
            this.errorLogBox.Name = "errorLogBox";
            this.errorLogBox.ReadOnly = true;
            this.errorLogBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.errorLogBox.Size = new System.Drawing.Size(498, 244);
            this.errorLogBox.TabIndex = 0;
            // 
            // buttonClose
            // 
            this.buttonClose.Location = new System.Drawing.Point(438, 296);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(75, 29);
            this.buttonClose.TabIndex = 1;
            this.buttonClose.Text = "&Close";
            this.buttonClose.UseVisualStyleBackColor = true;
            this.buttonClose.Click += new System.EventHandler(this.buttonClose_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 302);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(275, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "Click on \"Close\" to return to the application.";
            // 
            // ErrorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(525, 337);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.errorLogBox);
            this.Controls.Add(this.label1);
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

        System.Windows.Forms.Label label1;
        public System.Windows.Forms.TextBox errorLogBox;
        System.Windows.Forms.Button buttonClose;
        System.Windows.Forms.Label label2;
    }
}