using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using Epsilon.Win32.Resources;
using System.Diagnostics;
using Microsoft.Win32;

namespace WMP11Slipstreamer
{
    partial class MainForm
    {
        void aboutStatusLabel_Click(object sender, 
            LinkLabelLinkClickedEventArgs e)
        {
            AboutBox box = new AboutBox();
            box.ShowDialog();
        }

        void linkLabelWmp11SourceDownload_LinkClicked(object sender, 
            LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.microsoft.com/windows/windowsmedia/player/download/download.aspx");
        }

        void buttonHotfixBrowse_Click(object sender, EventArgs e)
        {
            string[] hotfixes = CM.OpenFileDialogMulti(
                "Select hotfix(es) to integrate into WMP11:",
                "Executable files (*.exe)|*.exe|All files (*.*)|*.*");
            if (hotfixes.Length > 0)
            {
                StringBuilder hotfixText = new StringBuilder(100);
                hotfixText.Append(Path.GetDirectoryName(hotfixes[0]).TrimEnd('\\'));
                hotfixText.Append("|");
                foreach (string hotfix in hotfixes)
                {
                    hotfixText.Append(Path.GetFileName(hotfix));
                    hotfixText.Append("|");
                }
                textBoxHotfixList.Text = hotfixText.ToString(0, hotfixText.Length - 1);
            }
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            if (this._immediateLauch)
            {
                base.Show();
                StartIntegration();
            }
        }

        void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this._workerThread != null && this._workerThread.IsAlive)
            {
                e.Cancel = true;
            }
            else if (this._winSrcPathIsReady && this._wmp11PathIsReady 
                && !this._closeOnSuccess)
            {
                Microsoft.Win32.RegistryKey wmp11Key
                    = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    Globals.wmp11SlipstreamerKey,
                    Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree
                );
                wmp11Key.SetValue(Globals.wmp11InstallerValue,
                    textBoxWmp11Source.Text);
                wmp11Key.SetValue(Globals.winSourceValue,
                    textBoxWindowsSource.Text);
                wmp11Key.SetValue(Globals.addonTypeValue,
                    addonTypeComboBox.SelectedIndex);
                if (checkBoxUseCustIcon.Checked)
                {
                    wmp11Key.SetValue(Globals.useCustomIconValue, 1);
                }
                else
                {
                    wmp11Key.SetValue(Globals.useCustomIconValue, 0);
                }
                wmp11Key.SetValue(Globals.whichCustomIconValue,
                    comboBoxIconSelect.SelectedIndex);
                if (comboBoxIconSelect.SelectedIndex == 2)
                {
                    wmp11Key.SetValue(Globals.customIconData,
                        this._customIconRaw,
                        Microsoft.Win32.RegistryValueKind.Binary);
                }
                if (hotfixesExist(textBoxHotfixList.Text))
                {
                    wmp11Key.SetValue(Globals.hotfixLineValue,
                        textBoxHotfixList.Text);
                }
                wmp11Key.Flush();
                wmp11Key.Close();
            }
        }

        void textBoxWmp11Source_TextChanged(object sender, EventArgs e)
        {
            if (this.textBoxWmp11Source.Text.Length > 0)
            {
                if (!File.Exists(this.textBoxWmp11Source.Text))
                {
                    this._wmp11PathIsReady = false;
                    this.syncWnd();
                    this.textBoxWmp11Source.BackColor = Color.IndianRed;
                    this.textBoxWmp11Source.ForeColor = Color.White;
                }
                else
                {
                    this._wmp11PathIsReady = true;
                    this.syncWnd();
                    this.textBoxWmp11Source.BackColor = Color.White;
                    this.textBoxWmp11Source.ForeColor = Color.Black;
                }
            }
            else
            {
                this._wmp11PathIsReady = false;
                this.syncWnd();
                this.textBoxWmp11Source.BackColor = Color.White;
                this.textBoxWmp11Source.ForeColor = Color.Black;
            }
        }

        void textBoxWindowsSource_TextChanged(object sender, EventArgs e)
        {
            if (this.textBoxWindowsSource.Text.Length > 0)
            {
                if (!Directory.Exists(textBoxWindowsSource.Text))
                {
                    this._winSrcPathIsReady = false;
                    this.syncWnd();
                    this.textBoxWindowsSource.BackColor = Color.IndianRed;
                    this.textBoxWindowsSource.ForeColor = Color.White;
                }
                else if (!File.Exists(this.textBoxWindowsSource.Text
                    + Path.DirectorySeparatorChar + "i386" +
                    Path.DirectorySeparatorChar + "LAYOUT.INF")
                    && !File.Exists(this.textBoxWindowsSource.Text
                    + Path.DirectorySeparatorChar + "amd64" +
                    Path.DirectorySeparatorChar + "LAYOUT.INF"))
                {
                    this._winSrcPathIsReady = false;
                    this.syncWnd();
                    this.textBoxWindowsSource.BackColor = Color.LavenderBlush;
                    this.textBoxWindowsSource.ForeColor = Color.Black;
                }
                else
                {
                    this._winSrcPathIsReady = true;
                    this.syncWnd();
                    this.textBoxWindowsSource.BackColor = Color.White;
                    this.textBoxWindowsSource.ForeColor = Color.Black;
                }
            }
            else
            {
                this._winSrcPathIsReady = false;
                this.syncWnd();
                this.textBoxWindowsSource.BackColor = Color.White;
                this.textBoxWindowsSource.ForeColor = Color.Black;
            }
        }

        void btnWmp11SourceBrowse_Click(object sender, EventArgs e)
        {
            string selectedFile =
            CM.OpenFileDialogStandard(
                "Locate WMP11 installer (eg: wmp11-windowsxp-x86-enu.exe)",
                "WMP11 Installation Executable (*.exe)|*.exe|All files (*.*)|*.*"
            );

            if (selectedFile.Length > 0)
            {
                this.textBoxWmp11Source.Text = selectedFile;
                this.textBoxWmp11Source.Focus();
                this.textBoxWmp11Source.SelectionStart 
                    = this.textBoxWmp11Source.Text.Length;
            }
        }

        void buttonWindowsSourceBrowse_Click(object sender, EventArgs e)
        {
            string selectedPath =
            CM.OpenFolderDialog("Choose the Microsoft® Windows™ source to modify:\r\n"
            + "- [Select the folder which contains the \"i386\" folder.]", true);

            if (selectedPath.Length > 0)
            {
                this.textBoxWindowsSource.Text = selectedPath;
                this.textBoxWindowsSource.Focus();
                this.textBoxWindowsSource.SelectionStart 
                    = textBoxWindowsSource.Text.Length;
            }
        }

        void buttonCancel_Click(object sender, EventArgs e)
        {
            if (this._workerThread == null || !this._workerThread.IsAlive)
            {
                Application.Exit();
            }
            else
            {
                this.buttonCancel.Enabled = false;
                this.statusLabelSourceType.Text
                    = "Waiting for current operation to complete...";
                this._backend.Abort();
            }
        }

        void buttonIntegrate_Click(object sender, EventArgs e)
        {
            this.StartIntegration();
        }

        void checkBoxUseCustIcon_CheckedChanged(object sender,
            EventArgs e)
        {
            this.labelPreview.Visible = this.checkBoxUseCustIcon.Checked;
            this.comboBoxIconSelect.Visible = this.checkBoxUseCustIcon.Checked;
            this.pictureBoxPreview.Visible = this.checkBoxUseCustIcon.Checked;
            if (this.checkBoxUseCustIcon.Checked)
            {
                this.comboBoxIconSelect.Focus();
            }
        }

        void comboBoxIconSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.comboBoxIconSelect.SelectedIndex)
            {
                case 0:
                    this.pictureBoxPreview.Image
                        = new Icon(new MemoryStream(Properties.Resources._0)).ToBitmap();
                    this._customIconRaw = Properties.Resources._0;
                    break;

                case 1:
                    this.pictureBoxPreview.Image
                        = new Icon(new MemoryStream(Properties.Resources._1)).ToBitmap();
                    this._customIconRaw = Properties.Resources._1;
                    break;

                case 2:
                    if (!this._noShowCustomIcoDialog)
                    {
                        this.pictureBoxPreview.Image = null;
                        string customIconLoc = CM.OpenFileDialogStandard(
                            "Locate custom icon to use...",
                            "Icon files (*.ico)|*.ico|All files (*.*)|*.*");
                        if (String.IsNullOrEmpty(customIconLoc))
                        {
                            this.comboBoxIconSelect.SelectedIndex--;
                            return;
                        }

                        FileStream customIconFStream = new FileStream(
                                customIconLoc, FileMode.Open, FileAccess.Read,
                                FileShare.Read);
                        if (ResourceEditor.IsValidIcon(customIconFStream))
                        {
                            Image image = new Icon(customIconFStream).ToBitmap();
                            customIconFStream.Seek(0, SeekOrigin.Begin);
                            this._customIconRaw
                                = new byte[customIconFStream.Length];
                            customIconFStream.Read(this._customIconRaw, 0,
                                this._customIconRaw.Length);
                            this.pictureBoxPreview.Image = image;
                            customIconFStream.Close();
                        }
                        else
                        {
                            MessageBox.Show(
                                "The specified file does not seem to be a valid icon.",
                                "Error loading icon", MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                           this.comboBoxIconSelect.SelectedIndex--;
                        }
                    }
                    else
                    {
                        this._noShowCustomIcoDialog = false;
                    }
                    break;
            }
        }
    }
}
