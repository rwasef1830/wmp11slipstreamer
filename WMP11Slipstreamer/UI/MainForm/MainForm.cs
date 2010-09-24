#region Using statements
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Epsilon.IO;
using Epsilon.Slipstreamers;
using Epsilon.Win32.Resources;
using Epsilon.WMP11Slipstreamer.Localization;
using Microsoft.Win32;

#endregion

namespace Epsilon.WMP11Slipstreamer
{
    partial class MainForm : Form
    {
        readonly bool _immediateLauch;
        readonly StringBuilder _pathBuffer;
        bool _closeOnSuccess;
        byte[] _customIconRaw;
        bool _noShowCustomIcoDialog;
        bool _winSrcPathIsReady;
        bool _wmp11PathIsReady;
        Thread _workerThread;

        public MainForm(
            string installer, string winsource, string hotfixes,
            string output, string customicon, bool nocats, bool slipstream,
            bool close, string customIconPath)
        {
            // Initialise the buffer
            this._pathBuffer = new StringBuilder(FileSystem.MaximumPathLength);

            this.InitializeComponent();

            this.uxComboBoxCustomIcon.SelectedIndex = 0;
            this.uxComboType.SelectedIndex = 0;

            int readiness = this.ProcessParameters(
                installer,
                winsource,
                hotfixes,
                output,
                customicon,
                nocats,
                slipstream,
                customIconPath);

            if (slipstream && readiness == 4)
            {
                this._immediateLauch = true;
                this._closeOnSuccess = close;
            }
        }

        public void GetControlMessages()
        {
            bool rtl = Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft;
            this.RightToLeft = (rtl) ? RightToLeft.Yes : RightToLeft.No;
            this.RightToLeftLayout = rtl;

            this.uxGroupBoxBasicOpts.Text = Msg.uxGroupBoxBasicOpts;
            this.uxGroupBoxAdvOpts.Text = Msg.uxGroupBoxAdvOpts;
            this.uxLabelChooseType.Text = Msg.uxLabelChooseType;
            this.uxLinkLabelAbout.Text = Msg.uxLinkLabelAbout;
            this.uxLabelEnterWmpRedist.Text = Msg.uxLabelEnterWMPRedistPath;
            this.uxLinkLabelWmp11SourceDownload.Text = Msg.uxLinkLabelWMPRedist;
            this.uxLabelEnterWinSrc.Text = Msg.uxLabelEnterSrcPath;
            this.uxLabelEnterHotfixLine.Text = Msg.uxLabelEnterHotfixLine;
            this.uxCheckBoxCustomIcon.Text = Msg.uxCheckboxCustomIcon;
            this.uxCheckBoxNoCats.Text = Msg.uxCheckboxNoCats;
            this.uxLabelOperation.Text = Msg.uxLabelDefaultOp;
            this.uxStatusLabelSourceType.Text = Msg.uxStatusBarDefaultText;
            this.uxButtonIntegrate.Text = Msg.uxButtonIntegrate;
            this.uxButtonCancel.Text = Msg.uxButtonExit;

            this.uxComboType.Items[0] = Msg.uxTypeVanilla;
            this.uxComboType.Items[1] = Msg.uxTypeTweaked;
        }

        int ProcessParameters(
            string installer, string winsource,
            string hotfixes, string output, string customicon, bool nocats,
            bool slipstream, string customIconPath)
        {
            RegistryKey wmp11Key
                = Registry.CurrentUser.OpenSubKey(
                    Globals.wmp11SlipstreamerKey);

            int readiness = 0;
            if (!String.IsNullOrEmpty(installer))
            {
                if (File.Exists(installer))
                {
                    this.uxTextBoxWmpRedist.Text = installer;
                    readiness++;
                }
            }
            else if (wmp11Key != null && !slipstream)
            {
                string installerFromReg
                    = wmp11Key.GetValue(
                        Globals.wmp11InstallerValue,
                        String.Empty).ToString();
                if (!String.IsNullOrEmpty(installerFromReg))
                {
                    if (File.Exists(installerFromReg))
                    {
                        this.uxTextBoxWmpRedist.Text = installerFromReg;
                    }
                }
            }

            if (!String.IsNullOrEmpty(winsource))
            {
                if (this.SourceMinimumValid(winsource))
                {
                    this.uxTextBoxWinSrc.Text = winsource.TrimEnd(Path.DirectorySeparatorChar);
                    readiness++;
                }
            }
            else if (wmp11Key != null && !slipstream)
            {
                string sourceFromReg
                    = wmp11Key.GetValue(
                        Globals.winSourceValue,
                        String.Empty).ToString();
                if (!String.IsNullOrEmpty(sourceFromReg))
                {
                    if (this.SourceMinimumValid(sourceFromReg))
                    {
                        this.uxTextBoxWinSrc.Text = sourceFromReg;
                    }
                }
            }

            if (!String.IsNullOrEmpty(output))
            {
                if (CM.SEqO(output, "Normal", true))
                    this.uxComboType.SelectedIndex = 0;
                else if (CM.SEqO(output, "Tweaked", true))
                    this.uxComboType.SelectedIndex = 1;
                readiness++;
            }
            else if (wmp11Key != null && !slipstream)
            {
                int outputFromReg;
                if (int.TryParse(
                    wmp11Key.GetValue(
                        Globals.addonTypeValue,
                        0).ToString(),
                    out outputFromReg))
                {
                    if (outputFromReg < this.uxComboType.Items.Count)
                    {
                        this.uxComboType.SelectedIndex = outputFromReg;
                    }
                }
            }

            if (!String.IsNullOrEmpty(customicon))
            {
                if (CM.SEqO(customicon, "Boooggy", true))
                {
                    this.uxCheckBoxCustomIcon.Checked = true;
                    this.uxComboBoxCustomIcon.SelectedIndex = 0;
                }
                else if (CM.SEqO(customicon, "Vista", true))
                {
                    this.uxCheckBoxCustomIcon.Checked = true;
                    this.uxComboBoxCustomIcon.SelectedIndex = 1;
                }
                else if (CM.SEqO(customicon, "Custom", true))
                {
                    if (String.IsNullOrEmpty(customIconPath)
                        || !File.Exists(customIconPath))
                        readiness--;
                    else
                    {
                        FileStream icoStream = File.OpenRead(customIconPath);
                        if (ResourceEditor.IsValidIcon(icoStream))
                        {
                            this._customIconRaw = new byte[icoStream.Length];
                            icoStream.Read(
                                this._customIconRaw,
                                0,
                                this._customIconRaw.Length);
                            icoStream.Seek(0, SeekOrigin.Begin);
                            this.uxPictureBoxCustomIconPreview.Image
                                = new Icon(icoStream).ToBitmap();
                            icoStream.Close();
                            this._noShowCustomIcoDialog = true;
                            this.uxCheckBoxCustomIcon.Checked = true;
                            this.uxComboBoxCustomIcon.SelectedIndex = 2;
                        }
                        else
                        {
                            readiness--;
                        }
                    }
                }
                else if (readiness > 0)
                    readiness--;
            }
            else if (wmp11Key != null && !slipstream)
            {
                int iconIndexFromReg;
                if (int.TryParse(
                    wmp11Key.GetValue(Globals.whichCustomIconValue, 0).ToString(),
                    out iconIndexFromReg) && iconIndexFromReg < this.uxComboBoxCustomIcon.Items.Count
                    && iconIndexFromReg == 2)
                {
                    try
                    {
                        var data =
                            (byte[])wmp11Key.GetValue(
                                Globals.customIconData, new byte[1]);
                        if (data.Length > 0)
                        {
                            var icoStream
                                = new MemoryStream(data);
                            if (ResourceEditor.IsValidIcon(icoStream))
                            {
                                this._customIconRaw
                                    = new byte[icoStream.Length];
                                icoStream.Read(
                                    this._customIconRaw,
                                    0,
                                    this._customIconRaw.Length);
                                icoStream.Seek(0, SeekOrigin.Begin);
                                this.uxPictureBoxCustomIconPreview.Image
                                    = new Icon(icoStream).ToBitmap();
                                icoStream.Close();
                                this._noShowCustomIcoDialog = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                        this.uxComboBoxCustomIcon.SelectedIndex = 0;
                    }
                }
                this.uxComboBoxCustomIcon.SelectedIndex = iconIndexFromReg;


                int useCustomIconFromReg;
                if (int.TryParse(
                    wmp11Key.GetValue(Globals.useCustomIconValue, 0).ToString(),
                    out useCustomIconFromReg))
                {
                    switch (useCustomIconFromReg)
                    {
                        case 0:
                            this.uxCheckBoxCustomIcon.Checked = false;
                            break;

                        case 1:
                            this.uxCheckBoxCustomIcon.Checked = true;
                            break;

                        default:
                            goto case 0;
                    }
                }
            }

            if (!String.IsNullOrEmpty(hotfixes))
            {
                if (this.HotfixesExist(hotfixes))
                {
                    this.uxTextBoxHotfixLine.Text = hotfixes;
                }
                else if (readiness > 0)
                {
                    readiness--;
                }
            }
            else if (wmp11Key != null && !slipstream)
            {
                string hotfixLineFromReg
                    = wmp11Key.GetValue(
                        Globals.hotfixLineValue,
                        String.Empty).ToString();
                if (!String.IsNullOrEmpty(hotfixLineFromReg))
                {
                    if (this.HotfixesExist(hotfixLineFromReg))
                    {
                        this.uxTextBoxHotfixLine.Text = hotfixLineFromReg;
                    }
                }
            }

            if (nocats) this.uxCheckBoxNoCats.Checked = true;
            readiness++;
            return readiness;
        }

        bool SourceMinimumValid(string sourceFolder)
        {
            return this.CheckEssentialFiles(
                sourceFolder,
                "LAYOUT.INF",
                "TXTSETUP.SIF",
                "DOSNET.INF",
                "SYSOC.INF");
        }

        void SyncUI()
        {
            this.uxButtonIntegrate.Enabled = this._wmp11PathIsReady && this._winSrcPathIsReady;
        }

        bool CheckEssentialWMPFiles(string sourceFolder)
        {
            // Check for essential files
            return this.CheckEssentialFiles(sourceFolder, "wmp.inf", "wmplayer.exe", "mplayer2.exe");
        }

        bool CheckEssentialFiles(string sourceFolder, params string[] essentialFiles)
        {
            string amd64Path = this.CreatePathString(sourceFolder, "amd64");
            string i386Path = this.CreatePathString(sourceFolder, "i386");

            foreach (string file in essentialFiles)
            {
                if (!SlipstreamerBase.FileExistsInSourceFolder(
                    this._pathBuffer,
                    file,
                    i386Path,
                    true)
                    && !SlipstreamerBase.FileExistsInSourceFolder(
                        this._pathBuffer,
                        "w" + file,
                        i386Path,
                        true)
                    && !SlipstreamerBase.FileExistsInSourceFolder(
                        this._pathBuffer,
                        file,
                        amd64Path,
                        true))
                {
                    return false;
                }
            }

            return true;
        }

        void StartIntegration()
        {
            this.uxTextBoxWinSrc.Text = this.uxTextBoxWinSrc.Text.TrimEnd(
                Path.DirectorySeparatorChar);
            if (!File.Exists(this.uxTextBoxWmpRedist.Text))
            {
                MessageBox.Show(
                    Msg.dlgText_WmpRedistNotFound,
                    Msg.dlgTitle_WmpRedistNotFound,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                this._closeOnSuccess = false;
            }
            else
            {
                if (!this.CheckEssentialWMPFiles(this.uxTextBoxWinSrc.Text))
                {
                    MessageBox.Show(
                        Msg.dlgText_Wmp64FilesMissing,
                        Msg.dlgTitle_Wmp64FilesMissing,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    this._closeOnSuccess = false;
                }
                else if (!this.HotfixesExist(this.uxTextBoxHotfixLine.Text))
                {
                    MessageBox.Show(
                        Msg.dlgText_InvalidHotfixLine,
                        Msg.dlgTitle_InvalidHotfixLine,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    this._closeOnSuccess = false;
                }
                else
                {
                    // Disable UI
                    this.ControlUserInterface(false);

                    // Detect source type
                    this.uxLabelOperation.Text = Msg.statDetectingSource;
                    var winSrcInfo = new WindowsSourceInfo();
                    var sourceDetector = new Thread(
                        delegate()
                        {
                            winSrcInfo = SourceDetector.Detect(
                                this.uxTextBoxWinSrc.Text,
                                this._pathBuffer);
                        });
                    this.uxButtonCancel.Enabled = false;
                    sourceDetector.Start();
                    while (sourceDetector.IsAlive)
                    {
                        Application.DoEvents();
                        Thread.Sleep(50);
                    }
                    this.uxStatusLabelSourceType.Text
                        = "Source Type: " + winSrcInfo;
                    this.uxButtonCancel.Enabled = true;

                    var settings = new BackendParams(
                        this.uxTextBoxWinSrc.Text,
                        winSrcInfo,
                        this.uxTextBoxWmpRedist.Text,
                        this.uxTextBoxHotfixLine.Text,
                        (PackageType)(this.uxComboType.SelectedIndex + 1),
                        (this.uxCheckBoxCustomIcon.Checked) ? this._customIconRaw : null,
                        this.uxCheckBoxNoCats.Checked);

                    this._workerThread = new Thread(this.WorkerMethod)
                    {
                        CurrentUICulture = Thread.CurrentThread.CurrentUICulture
                    };
                    this._workerThread.Start(settings);

                    while (this._workerThread.IsAlive)
                    {
                        Thread.Sleep(10);
                        Application.DoEvents();
                    }

                    if (settings.Status == SlipstreamerStatus.Cancelled)
                    {
                        MessageBox.Show(
                            Msg.dlgText_Cancelled,
                            Msg.dlgTitle_Cancelled,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                    else if (!this._immediateLauch && !this._closeOnSuccess
                             && settings.Status == SlipstreamerStatus.Success)
                    {
                        MessageBox.Show(
                            Msg.dlgText_Success,
                            Msg.dlgTitle_Success,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }

                    if (this._closeOnSuccess
                        && settings.Status == SlipstreamerStatus.Success)
                    {
                        Application.Exit();
                    }
                    else
                    {
                        // Release memory held by Backend (which is quite a lot)
                        this._backend = null;
                        GC.Collect();
                        this.ControlUserInterface(true);
                    }
                }
            }
        }

        void ControlUserInterface(bool state)
        {
            this.SuspendLayout();
            this.uxTextBoxWinSrc.Enabled = state;
            this.uxTextBoxWmpRedist.Enabled = state;
            this.uxTextBoxHotfixLine.Enabled = state;
            this.uxTextBoxWinSrc.BackColor = SystemColors.ControlLightLight;
            this.uxTextBoxWmpRedist.BackColor = SystemColors.ControlLightLight;
            this.uxTextBoxHotfixLine.BackColor = SystemColors.ControlLightLight;
            this.uxComboType.Enabled = state;
            this.uxButtonWmpRedistPicker.Enabled = state;
            this.uxButtonWinSrcPicker.Enabled = state;
            this.uxButtonHotfixPicker.Enabled = state;
            this.uxCheckBoxNoCats.Enabled = state;
            this.uxButtonIntegrate.Enabled = state;
            this.uxCheckBoxCustomIcon.Enabled = state;
            this.uxComboBoxCustomIcon.Enabled = state;
            this.uxPictureBoxCustomIconPreview.Enabled = state;
            this.uxLabelOperation.Visible = !state;
            this.uxLinkLabelAbout.Visible = state;
            this.uxLinkLabelWmp11SourceDownload.Enabled = state;
            if (state)
            {
                this.uxProgressBarOverall.Value = 0;
                this.uxStatusLabelSourceType.Text = String.Empty;
                this.uxButtonCancel.Text = Msg.uxButtonExit;
            }
            if (!state)
            {
                this.uxButtonCancel.Text = Msg.uxButtonCancel;
            }
            this.ResumeLayout();
        }

        bool HotfixesExist(string hotfixLine)
        {
            string hotfixLineCleaned = hotfixLine.Trim();
            if (hotfixLineCleaned.Length > 0)
            {
                string[] hotfixData = hotfixLineCleaned.Split(
                    new[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries);
                string folder = hotfixData[0];
                if (!Directory.Exists(folder))
                {
                    return false;
                }
                if (hotfixData.Length == 1)
                {
                    return false;
                }
                for (int i = 1; i < hotfixData.Length; i++)
                {
                    string fix = hotfixData[i];
                    if (!File.Exists(this.CreatePathString(folder, fix))) return false;
                }
                return true;
            }
            return true;
        }

        string CreatePathString(params string[] components)
        {
            return FileSystem.CreatePathString(this._pathBuffer, components);
        }
    }
}