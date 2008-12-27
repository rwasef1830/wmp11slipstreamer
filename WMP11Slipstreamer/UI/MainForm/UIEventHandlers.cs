using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using Epsilon.Win32.Resources;
using System.Diagnostics;
using Microsoft.Win32;
using Epsilon.WMP11Slipstreamer.Localization;

namespace Epsilon.WMP11Slipstreamer
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
            Process.Start(Globals.WmpRedistUrl);
        }

        void buttonHotfixBrowse_Click(object sender, EventArgs e)
        {
            string[] hotfixes = CM.OpenFileDialogMulti(
                Msg.dlgPickHotfixes_Title,
                String.Format("{0} (*.exe)|*.exe|{1} (*.*)|*.*",
                Msg.dlgPicker_ExeFiles, Msg.dlgPicker_AllFiles));
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
                uxTextBoxHotfixLine.Text = hotfixText.ToString(0, hotfixText.Length - 1);
            }
        }

        void MainForm_Load(object sender, EventArgs e)
        {
        	GetControlMessages();
            this.Text += " v" + Globals.Version;

#if BETA
            this.Text += " BETA";
#endif
        }

        void MainForm_Shown(object sender, EventArgs e)
        {
            this.uxComboType.Focus();

            if (this._immediateLauch)
            {
                this.StartIntegration();
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
                    uxTextBoxWmpRedist.Text);
                wmp11Key.SetValue(Globals.winSourceValue,
                    uxTextBoxWinSrc.Text);
                wmp11Key.SetValue(Globals.addonTypeValue,
                    uxComboType.SelectedIndex);
                if (uxCheckBoxCustomIcon.Checked)
                {
                    wmp11Key.SetValue(Globals.useCustomIconValue, 1);
                }
                else
                {
                    wmp11Key.SetValue(Globals.useCustomIconValue, 0);
                }
                wmp11Key.SetValue(Globals.whichCustomIconValue,
                    uxComboBoxCustomIcon.SelectedIndex);
                if (uxComboBoxCustomIcon.SelectedIndex == 2)
                {
                    wmp11Key.SetValue(Globals.customIconData,
                        this._customIconRaw,
                        Microsoft.Win32.RegistryValueKind.Binary);
                }
                if (HotfixesExist(uxTextBoxHotfixLine.Text))
                {
                    wmp11Key.SetValue(Globals.hotfixLineValue,
                        uxTextBoxHotfixLine.Text);
                }
                wmp11Key.Flush();
                wmp11Key.Close();
            }
        }

        void textBoxWmp11Source_TextChanged(object sender, EventArgs e)
        {
            if (this.uxTextBoxWmpRedist.Text.Length > 0)
            {
                if (!File.Exists(this.uxTextBoxWmpRedist.Text))
                {
                    this._wmp11PathIsReady = false;
                    this.SyncUI();
                    this.uxTextBoxWmpRedist.BackColor = Color.IndianRed;
                    this.uxTextBoxWmpRedist.ForeColor = Color.White;
                }
                else
                {
                    this._wmp11PathIsReady = true;
                    this.SyncUI();
                    this.uxTextBoxWmpRedist.BackColor = Color.White;
                    this.uxTextBoxWmpRedist.ForeColor = Color.Black;
                }
            }
            else
            {
                this._wmp11PathIsReady = false;
                this.SyncUI();
                this.uxTextBoxWmpRedist.BackColor = Color.White;
                this.uxTextBoxWmpRedist.ForeColor = Color.Black;
            }
        }

        void textBoxWindowsSource_TextChanged(object sender, EventArgs e)
        {
            if (this.uxTextBoxWinSrc.Text.Length > 0)
            {
                if (!Directory.Exists(uxTextBoxWinSrc.Text))
                {
                    this._winSrcPathIsReady = false;
                    this.SyncUI();
                    this.uxTextBoxWinSrc.BackColor = Color.IndianRed;
                    this.uxTextBoxWinSrc.ForeColor = Color.White;
                }
                else if (!File.Exists(this.CreatePathString(uxTextBoxWinSrc.Text, 
                    "i386", "LAYOUT.INF"))
                    && !File.Exists(this.CreatePathString(uxTextBoxWinSrc.Text,
                    "amd64", "LAYOUT.INF")))
                {
                    this._winSrcPathIsReady = false;
                    this.SyncUI();
                    this.uxTextBoxWinSrc.BackColor = Color.LavenderBlush;
                    this.uxTextBoxWinSrc.ForeColor = Color.Black;
                }
                else
                {
                    this._winSrcPathIsReady = true;
                    this.SyncUI();
                    this.uxTextBoxWinSrc.BackColor = Color.White;
                    this.uxTextBoxWinSrc.ForeColor = Color.Black;
                }
            }
            else
            {
                this._winSrcPathIsReady = false;
                this.SyncUI();
                this.uxTextBoxWinSrc.BackColor = Color.White;
                this.uxTextBoxWinSrc.ForeColor = Color.Black;
            }
        }

        void btnWmp11SourceBrowse_Click(object sender, EventArgs e)
        {
            string selectedFile =
            CM.OpenFileDialogStandard(
                Msg.dlgWMPRedistPicker_Header,
                String.Format("{0} (*.exe)|*.exe|{1} (*.*)|*.*",
                Msg.dlgPicker_ExeFiles, Msg.dlgPicker_AllFiles)
            );

            if (selectedFile.Length > 0)
            {
                this.uxTextBoxWmpRedist.Text = selectedFile;
                this.uxTextBoxWmpRedist.Focus();
                this.uxTextBoxWmpRedist.SelectionStart 
                    = this.uxTextBoxWmpRedist.Text.Length;
            }
        }

        void buttonWindowsSourceBrowse_Click(object sender, EventArgs e)
        {
            string selectedPath = CM.OpenFolderDialog(Msg.dlgSrcPicker_Header, true);

            if (selectedPath.Length > 0)
            {
                this.uxTextBoxWinSrc.Text = selectedPath;
                this.uxTextBoxWinSrc.Focus();
                this.uxTextBoxWinSrc.SelectionStart 
                    = uxTextBoxWinSrc.Text.Length;
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
                this.uxButtonCancel.Enabled = false;
                this.uxStatusLabelSourceType.Text = Msg.statWaitCancel;
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
            this.uxLabelPreview.Visible = this.uxCheckBoxCustomIcon.Checked;
            this.uxComboBoxCustomIcon.Visible = this.uxCheckBoxCustomIcon.Checked;
            this.uxPictureBoxCustomIconPreview.Visible = this.uxCheckBoxCustomIcon.Checked;
            if (this.uxCheckBoxCustomIcon.Checked)
            {
                this.uxComboBoxCustomIcon.Focus();
            }
        }

        void comboBoxIconSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.uxComboBoxCustomIcon.SelectedIndex)
            {
                case 0:
                    this.uxPictureBoxCustomIconPreview.Image
                        = new Icon(new MemoryStream(Properties.Resources._0)).ToBitmap();
                    this._customIconRaw = Properties.Resources._0;
                    break;

                case 1:
                    this.uxPictureBoxCustomIconPreview.Image
                        = new Icon(new MemoryStream(Properties.Resources._1)).ToBitmap();
                    this._customIconRaw = Properties.Resources._1;
                    break;

                case 2:
                    if (!this._noShowCustomIcoDialog)
                    {
                        this.uxPictureBoxCustomIconPreview.Image = null;
                        string customIconLoc = CM.OpenFileDialogStandard(
                            Msg.dlgIconPicker_Title,
                            String.Format("{0} (*.ico)|*.ico|{1} (*.*)|*.*",
                            Msg.dlgPicker_IconFiles, Msg.dlgPicker_AllFiles));
                        if (String.IsNullOrEmpty(customIconLoc))
                        {
                            this.uxComboBoxCustomIcon.SelectedIndex--;
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
                            this.uxPictureBoxCustomIconPreview.Image = image;
                            customIconFStream.Close();
                        }
                        else
                        {
                            MessageBox.Show(
                                Msg.dlgIconError_Text,
                                Msg.dlgIconError_Title, MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                           this.uxComboBoxCustomIcon.SelectedIndex--;
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
