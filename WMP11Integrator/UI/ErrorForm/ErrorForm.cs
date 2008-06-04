using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;

namespace WMP11Slipstreamer
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
            InitializeComponent();

            this._exception = ex;
            this._critical = critical;
            this._tempFolder = tempFolder;
            this._osString = osString;
            this._hotfixList = hotfixList;
        }

        void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void ErrorForm_Load(object sender, EventArgs e)
        {
            try
            {
                errorLogBox.Text
                        = String.Format("WMP11Slipstreamer v{0}",
                        Globals.Version.ToString());
                errorLogBox.AppendText(Environment.NewLine);

                if (!String.IsNullOrEmpty(this._osString))
                {
                    errorLogBox.AppendText("Detected source: ");
                    errorLogBox.AppendText(this._osString);
                    errorLogBox.AppendText(Environment.NewLine);
                }

                errorLogBox.AppendText(Environment.NewLine);

                string[] hotfixesList = this._hotfixList.Split('|');
                if (hotfixesList.Length > 1)
                {
                    errorLogBox.AppendText("Hotfixes:");
                    errorLogBox.AppendText(Environment.NewLine);
                    for (int i = 1; i < hotfixesList.Length; i++)
                    {
                        if (!String.IsNullOrEmpty(hotfixesList[i]))
                        {
                            errorLogBox.AppendText(hotfixesList[i]);
                            errorLogBox.AppendText(Environment.NewLine);
                        }
                    }
                    errorLogBox.AppendText(Environment.NewLine);
                }

                errorLogBox.AppendText(this._exception.ToString());
                errorLogBox.AppendText(Environment.NewLine);
                errorLogBox.AppendText(Environment.NewLine);

                if (this._exception.Data.Count > 0)
                {
                    errorLogBox.AppendText("Extra error information:");
                    errorLogBox.AppendText(Environment.NewLine);
                    foreach (DictionaryEntry pair in this._exception.Data)
                    {
                        errorLogBox.AppendText(pair.Key.ToString());
                        errorLogBox.AppendText(": ");
                        errorLogBox.AppendText(pair.Value.ToString());
                        errorLogBox.AppendText(Environment.NewLine);
                    }
                    errorLogBox.AppendText(Environment.NewLine);
                }

                if (!this._critical)
                {
                    if (this._tempFolder != null)
                    {
                        this.errorLogBox.AppendText(
                            String.Format("Deleting \"{0}\"...", this._tempFolder));
                        try
                        {
                            Directory.Delete(this._tempFolder, true);
                            errorLogBox.AppendText("  Done!");
                            errorLogBox.AppendText(Environment.NewLine);
                            errorLogBox.AppendText(Environment.NewLine);
                            errorLogBox.AppendText(
                                "The source being modified has not been "
                            + "damaged." + Environment.NewLine
                            + "All changes have been successfully reverted.");
                        }
                        catch (Exception exSub)
                        {
                            errorLogBox.AppendText("  FAILED!");
                            errorLogBox.AppendText(Environment.NewLine);
                            errorLogBox.AppendText(Environment.NewLine);
                            errorLogBox.AppendText(
                                "An exception occurred while attempting to "
                                + "delete the temporary folder to revert "
                                + "the changes done to the source. You can delete "
                                + "the temporary folder by yourself later.");
                            errorLogBox.AppendText(Environment.NewLine);
                            errorLogBox.AppendText(Environment.NewLine);
                            errorLogBox.AppendText(String.Format("Temporary folder: \"{0}\"",
                                this._tempFolder));
                            errorLogBox.AppendText(Environment.NewLine);
                            errorLogBox.AppendText(Environment.NewLine);
                            errorLogBox.AppendText(exSub.ToString());
                        }
                    }
                    else
                    {
                        errorLogBox.AppendText("The error occurred before any changes "
                        + "were done to the source being modified."
                        + Environment.NewLine
                        + Environment.NewLine
                        + "The source being modified has not been damaged.");
                    }
                }
                else
                {
                    errorLogBox.AppendText("Unfortunately, the error occurred during "
                    + "the critical process of overwriting files in the \"i386\" folder. "
                    + "This source is now corrupt, please use a fresh new "
                    + "source for subsequent windows setup installations and modifications.");
                    errorLogBox.AppendText(Environment.NewLine);
                    errorLogBox.AppendText(Environment.NewLine);
                    errorLogBox.AppendText(String.Format("Temporary folder: \"{0}\"",
                        this._tempFolder));
                    errorLogBox.AppendText(Environment.NewLine);
                    errorLogBox.AppendText(Environment.NewLine);
                    errorLogBox.AppendText("We apologize for any inconvenience. "
                    + "Please report this bug.");
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