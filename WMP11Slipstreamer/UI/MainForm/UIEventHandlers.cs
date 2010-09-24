using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Epsilon.Win32.Resources;
using Epsilon.WMP11Slipstreamer.Localization;
using Epsilon.WMP11Slipstreamer.Properties;
using Microsoft.Win32;

namespace Epsilon.WMP11Slipstreamer
{
    partial class MainForm
    {
        static void uxLinkLabelAbout_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            var box = new AboutBox();
            box.ShowDialog();
        }

        static void uxLinkLabelWmp11SourceDownload_LinkClicked(
            object sender,
            LinkLabelLinkClickedEventArgs e)
        {
            CM.LaunchInDefaultHandler(Globals.WmpRedistUrl);
        }

        void uxButtonHotfixPicker_Click(object sender, EventArgs e)
        {
            string[] hotfixes = CM.OpenFileDialogMulti(
                Msg.dlgPickHotfixes_Title,
                String.Format(
                    "{0} (*.exe)|*.exe|{1} (*.*)|*.*",
                    Msg.dlgPicker_ExeFiles,
                    Msg.dlgPicker_AllFiles));
            if (hotfixes.Length > 0)
            {
                var hotfixText = new StringBuilder(100);
                hotfixText.Append(Path.GetDirectoryName(hotfixes[0]).TrimEnd('\\'));
                hotfixText.Append("|");
                foreach (string hotfix in hotfixes)
                {
                    hotfixText.Append(Path.GetFileName(hotfix));
                    hotfixText.Append("|");
                }
                this.uxTextBoxHotfixLine.Text = hotfixText.ToString(0, hotfixText.Length - 1);
            }
        }

        void MainForm_Load(object sender, EventArgs e)
        {
            this.GetControlMessages();
            this.Text += " v" + Globals.Version;
            this.AppendBetaToTitle();
        }

        [Conditional("BETA")]
        void AppendBetaToTitle()
        {
            this.Text += " BETA";
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
                RegistryKey wmp11Key
                    = Registry.CurrentUser.CreateSubKey(
                        Globals.wmp11SlipstreamerKey,
                        RegistryKeyPermissionCheck.ReadWriteSubTree
                        );
                wmp11Key.SetValue(
                    Globals.wmp11InstallerValue,
                    this.uxTextBoxWmpRedist.Text);
                wmp11Key.SetValue(
                    Globals.winSourceValue,
                    this.uxTextBoxWinSrc.Text);
                wmp11Key.SetValue(
                    Globals.addonTypeValue,
                    this.uxComboType.SelectedIndex);
                wmp11Key.SetValue(Globals.useCustomIconValue, this.uxCheckBoxCustomIcon.Checked ? 1 : 0);
                wmp11Key.SetValue(
                    Globals.whichCustomIconValue,
                    this.uxComboBoxCustomIcon.SelectedIndex);
                if (this.uxComboBoxCustomIcon.SelectedIndex == 2)
                {
                    wmp11Key.SetValue(
                        Globals.customIconData,
                        this._customIconRaw,
                        RegistryValueKind.Binary);
                }
                if (this.HotfixesExist(this.uxTextBoxHotfixLine.Text))
                {
                    wmp11Key.SetValue(
                        Globals.hotfixLineValue,
                        this.uxTextBoxHotfixLine.Text);
                }
                wmp11Key.Flush();
                wmp11Key.Close();
            }
        }

        void uxTextBoxWmpRedist_TextChanged(object sender, EventArgs e)
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

        void uxTextBoxWinSrc_TextChanged(object sender, EventArgs e)
        {
            if (this.uxTextBoxWinSrc.Text.Length > 0)
            {
                if (!Directory.Exists(this.uxTextBoxWinSrc.Text))
                {
                    this._winSrcPathIsReady = false;
                    this.SyncUI();
                    this.uxTextBoxWinSrc.BackColor = Color.IndianRed;
                    this.uxTextBoxWinSrc.ForeColor = Color.White;
                }
                else if (!File.Exists(
                    this.CreatePathString(
                        this.uxTextBoxWinSrc.Text,
                        "i386",
                        "LAYOUT.INF"))
                         && !File.Exists(
                             this.CreatePathString(
                                 this.uxTextBoxWinSrc.Text,
                                 "amd64",
                                 "LAYOUT.INF")))
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

        void uxButtonWmpRedistPicker_Click(object sender, EventArgs e)
        {
            string selectedFile =
                CM.OpenFileDialogStandard(
                    Msg.dlgWMPRedistPicker_Header,
                    String.Format(
                        "{0} (*.exe)|*.exe|{1} (*.*)|*.*",
                        Msg.dlgPicker_ExeFiles,
                        Msg.dlgPicker_AllFiles)
                    );

            if (selectedFile.Length > 0)
            {
                this.uxTextBoxWmpRedist.Text = selectedFile;
                this.uxTextBoxWmpRedist.Focus();
                this.uxTextBoxWmpRedist.SelectionStart
                    = this.uxTextBoxWmpRedist.Text.Length;
            }
        }

        void uxButtonWinSrcPicker_Click(object sender, EventArgs e)
        {
            string selectedPath = CM.OpenFolderDialog(Msg.dlgSrcPicker_Header, true);

            if (selectedPath.Length > 0)
            {
                this.uxTextBoxWinSrc.Text = selectedPath;
                this.uxTextBoxWinSrc.Focus();
                this.uxTextBoxWinSrc.SelectionStart
                    = this.uxTextBoxWinSrc.Text.Length;
            }
        }

        void uxButtonCancel_Click(object sender, EventArgs e)
        {
            if (this._workerThread == null || !this._workerThread.IsAlive)
            {
                Application.Exit();
            }
            else
            {
                this.uxButtonCancel.Enabled = false;
                this._backend.Pause();

                DialogResult result
                    = MessageBox.Show(
                        Msg.dlgCancel_Text,
                        Msg.dlgCancel_Title,
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button2);

                if (result == DialogResult.Yes)
                {
                    this.uxStatusLabelSourceType.Text = Msg.statWaitCancel;
                    this._backend.Abort();
                }
                else
                {
                    this.uxButtonCancel.Enabled = true;
                    this._backend.Resume();
                }
            }
        }

        void uxButtonIntegrate_Click(object sender, EventArgs e)
        {
            this.StartIntegration();
        }

        void uxCheckBoxCustomIcon_CheckedChanged(
            object sender,
            EventArgs e)
        {
            this.uxComboBoxCustomIcon.Visible = this.uxCheckBoxCustomIcon.Checked;
            this.uxPictureBoxCustomIconPreview.Visible
                = this.uxCheckBoxCustomIcon.Checked;
            if (this.uxCheckBoxCustomIcon.Checked)
            {
                this.uxComboBoxCustomIcon.Focus();
            }
        }

        void uxComboBoxCustomIcon_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.uxComboBoxCustomIcon.SelectedIndex)
            {
                case 0:
                    this.uxPictureBoxCustomIconPreview.Image
                        = new Icon(
                            new MemoryStream(Resources._0)).ToBitmap();
                    this._customIconRaw = Resources._0;
                    break;

                case 1:
                    this.uxPictureBoxCustomIconPreview.Image
                        = new Icon(
                            new MemoryStream(Resources._1)).ToBitmap();
                    this._customIconRaw = Resources._1;
                    break;

                case 2:
                    if (!this._noShowCustomIcoDialog)
                    {
                        this.uxPictureBoxCustomIconPreview.Image = null;
                        string customIconLoc = CM.OpenFileDialogStandard(
                            Msg.dlgIconPicker_Title,
                            String.Format(
                                "{0} (*.ico)|*.ico|{1} (*.*)|*.*",
                                Msg.dlgPicker_IconFiles,
                                Msg.dlgPicker_AllFiles));
                        if (String.IsNullOrEmpty(customIconLoc))
                        {
                            this.uxComboBoxCustomIcon.SelectedIndex--;
                            return;
                        }

                        var customIconFStream = new FileStream(
                            customIconLoc,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read);
                        if (ResourceEditor.IsValidIcon(customIconFStream))
                        {
                            Image image = new Icon(customIconFStream).ToBitmap();
                            customIconFStream.Seek(0, SeekOrigin.Begin);
                            this._customIconRaw
                                = new byte[customIconFStream.Length];
                            customIconFStream.Read(
                                this._customIconRaw,
                                0,
                                this._customIconRaw.Length);
                            this.uxPictureBoxCustomIconPreview.Image = image;
                            customIconFStream.Close();
                        }
                        else
                        {
                            MessageBox.Show(
                                Msg.dlgIconError_Text,
                                Msg.dlgIconError_Title,
                                MessageBoxButtons.OK,
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