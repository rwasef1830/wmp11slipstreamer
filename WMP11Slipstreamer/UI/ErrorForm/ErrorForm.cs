using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using Epsilon.WMP11Slipstreamer.Localization;

namespace Epsilon.WMP11Slipstreamer
{
    public partial class ErrorForm : Form
    {
        Exception _exception;
        bool _critical;
        string _tempFolder;
        string _osString;
        string _hotfixList;

        public ErrorForm(Exception ex, bool critical, 
            string tempFolder, string osString, string hotfixList)
        {
            this.InitializeComponent();
            this.ReadLocalizedMessages();

            this._exception = ex;
            this._critical = critical;
            this._tempFolder = tempFolder;
            this._osString = osString;
            this._hotfixList = hotfixList;
        }

        void ReadLocalizedMessages()
        {
            this.SuspendLayout();
            this.uxLabelHeader.Text = Msg.dlgError_Header;
            this.uxLabelFooter.Text = Msg.dlgError_Footer;
            this.uxButtonClose.Text = Msg.dlgError_ButtonClose;
            this.ResumeLayout();
        }

        void uxButtonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ErrorForm_Load(object sender, EventArgs e)
        {
            try
            {
                this.uxTextBoxErrorLog.Text
                        = String.Format("WMP11Slipstreamer v{0}",
                        Globals.Version.ToString());
                this.uxTextBoxErrorLog.AppendText(Environment.NewLine);

                this.uxTextBoxErrorLog.AppendText(Msg.dlgError_DetectedSource);
                this.uxTextBoxErrorLog.AppendText(
                    this._osString ?? Msg.dlgError_NotAvailable);
                this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                this.uxTextBoxErrorLog.AppendText(Environment.NewLine);

                string[] hotfixesList = this._hotfixList.Split('|');
                if (hotfixesList.Length > 1)
                {
                    this.uxTextBoxErrorLog.AppendText(Msg.dlgError_Hotfixes);
                    this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                    for (int i = 1; i < hotfixesList.Length; i++)
                    {
                        if (!String.IsNullOrEmpty(hotfixesList[i]))
                        {
                            this.uxTextBoxErrorLog.AppendText(hotfixesList[i]);
                            this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                        }
                    }
                    this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                }

                this.uxTextBoxErrorLog.AppendText(this._exception.ToString());
                this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                this.uxTextBoxErrorLog.AppendText(Environment.NewLine);

                if (this._exception.Data.Count > 0)
                {
                    this.uxTextBoxErrorLog.AppendText(Msg.dlgError_ExtraErrorInfo);
                    this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                    foreach (DictionaryEntry pair in this._exception.Data)
                    {
                        this.uxTextBoxErrorLog.AppendText(pair.Key.ToString());
                        this.uxTextBoxErrorLog.AppendText(": ");
                        this.uxTextBoxErrorLog.AppendText(pair.Value.ToString());
                        this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                    }
                    this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                }

                if (!this._critical)
                {
                    if (this._tempFolder != null)
                    {
                        if (Directory.Exists(this._tempFolder))
                        {
                            this.uxTextBoxErrorLog.AppendText(
                                Msg.dlgError_ErrorDuringCleanup);
                            this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                            this.uxTextBoxErrorLog.AppendText(
                                Msg.dlgError_DeleteTempDirManually);
                            this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                            this.uxTextBoxErrorLog.AppendText(this._tempFolder);
                        }
                        else
                        {
                            this.uxTextBoxErrorLog.AppendText(
                                Msg.dlgError_SourceNotCorrupted
                                + " " + Msg.dlgError_ChangesUndone);
                        }
                    }
                    else
                    {
                        this.uxTextBoxErrorLog.AppendText(
                            Msg.dlgError_ErrorBeforeModifications
                            + Environment.NewLine
                            + Msg.dlgError_SourceNotCorrupted);
                    }
                }
                else
                {
                    this.uxTextBoxErrorLog.AppendText(Msg.dlgError_SourceCorruptedSorry);
                    this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                    this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                    this.uxTextBoxErrorLog.AppendText(
                        String.Format(Msg.dlgError_TemporaryFolder,
                        this._tempFolder));
                    this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                    this.uxTextBoxErrorLog.AppendText(Environment.NewLine);
                    this.uxTextBoxErrorLog.AppendText(Msg.dlgError_ApologiesReportBug);
                }
            }
            catch (Exception unexpected)
            {
                MessageBox.Show("An error occurred inside the error handling "
                + "form constructor. Please report this bug."
                + Environment.NewLine + Environment.NewLine
                + unexpected.ToString(), "Unhandled Error Form Exception",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}