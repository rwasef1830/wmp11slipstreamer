using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Epsilon.IO;
using Epsilon.Win32.Resources;
using Epsilon.Win32;
using Epsilon.DebugServices;

namespace WMP11Slipstreamer
{
    partial class MainForm : Form
    {
        bool _immediateLauch;
        bool _closeOnSuccess;
        bool _wmp11PathIsReady;
        bool _winSrcPathIsReady;
        byte[] _customIconRaw;
        bool _noShowCustomIcoDialog;
        Thread _workerThread;
        StringBuilder _pathBuffer;

        public MainForm(string installer, string winsource, string hotfixes,
            string output, string customicon, bool nocats, bool slipstream, 
            bool close, string customIconPath)
        {
            InitializeComponent();
            GetControlMessages();

            // Initialise the buffer
            _pathBuffer = new StringBuilder(FileSystem.MaximumPath);

            SuspendLayout();
            uxComboBoxCustomIcon.SelectedIndex = 0;
            uxComboType.SelectedIndex = 0;
            Text += " v" + Globals.Version;
            int readiness = ProcessParameters(installer, winsource, hotfixes, 
                output, customicon, nocats, slipstream, customIconPath);
            ResumeLayout();

            if (slipstream)
            {
                if (readiness == 4)
                {
                    _immediateLauch = true;
                    if (close)
                        _closeOnSuccess = close;
                }
            }
        }

        int ProcessParameters(string installer, string winsource, 
            string hotfixes, string output, string customicon, bool nocats, 
            bool slipstream, string customIconPath)
        {
            Microsoft.Win32.RegistryKey wmp11Key
                = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                Globals.wmp11SlipstreamerKey);

            int readiness = 0;
            if (installer != null)
            {
                if (File.Exists(installer))
                {
                    uxTextBoxWmpRedist.Text = installer;
                    readiness++;
                }
            }
            else
            {
                if (wmp11Key != null && !slipstream)
                {
                    string installerFromReg
                        = wmp11Key.GetValue(Globals.wmp11InstallerValue,
                        String.Empty).ToString();
                    if (!String.IsNullOrEmpty(installerFromReg))
                    {
                        if (File.Exists(installerFromReg))
                        {
                            uxTextBoxWmpRedist.Text = installerFromReg;
                        }
                    }
                }
            }
            if (winsource != null)
            {
                if (SourceMinimumValid(winsource))
                {
                    uxTextBoxWinSrc.Text = winsource.TrimEnd(Path.DirectorySeparatorChar);
                    readiness++;
                }
            }
            else
            {
                if (wmp11Key != null && !slipstream)
                {
                    string sourceFromReg
                            = wmp11Key.GetValue(Globals.winSourceValue,
                            String.Empty).ToString();
                    if (!String.IsNullOrEmpty(sourceFromReg))
                    {
                        if (SourceMinimumValid(sourceFromReg))
                        {
                            uxTextBoxWinSrc.Text = sourceFromReg;
                        }
                    }
                }
            }
            if (output != null)
            {
                if (CM.SEqO(output, "Normal", true))
                    uxComboType.SelectedIndex = 0;
                else if (CM.SEqO(output, "Tweaked", true))
                    uxComboType.SelectedIndex = 1;
                readiness++;
            }
            else if (wmp11Key != null && !slipstream)
            {
                int outputFromReg;
                if (int.TryParse(wmp11Key.GetValue(Globals.addonTypeValue,
                        0).ToString(), out outputFromReg))
                {
                    if (outputFromReg < uxComboType.Items.Count)
                    {
                        uxComboType.SelectedIndex = outputFromReg;
                    }
                }
            }
            if (customicon != null)
            {
                if (CM.SEqO(customicon, "Boooggy", true)) 
                {
                    uxCheckBoxCustomIcon.Checked = true;
                    uxComboBoxCustomIcon.SelectedIndex = 0;
                }
                else if (CM.SEqO(customicon, "Vista", true))
                {
                    uxCheckBoxCustomIcon.Checked = true;
                    uxComboBoxCustomIcon.SelectedIndex = 1;
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
                            _customIconRaw = new byte[icoStream.Length];
                            icoStream.Read(_customIconRaw, 0,
                                _customIconRaw.Length);
                            icoStream.Seek(0, SeekOrigin.Begin);
                            uxPictureBoxCustomIconPreview.Image
                                = new Icon(icoStream).ToBitmap();
                            icoStream.Close();
                            _noShowCustomIcoDialog = true;
                            uxCheckBoxCustomIcon.Checked = true;
                            uxComboBoxCustomIcon.SelectedIndex = 2;
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
            else
            {
                if (wmp11Key != null && !slipstream)
                {
                    int iconIndexFromReg;
                    if (int.TryParse(wmp11Key.GetValue(Globals.whichCustomIconValue,
                            0).ToString(), out iconIndexFromReg))
                    {
                        if (iconIndexFromReg < uxComboBoxCustomIcon.Items.Count)
                        {
                            if (iconIndexFromReg == 2)
                            {
                                try
                                {
                                    byte[] data =
                                        (byte[])wmp11Key.GetValue(
                                        Globals.customIconData, new byte[1]);
                                    if (data.Length > 0)
                                    {
                                        MemoryStream icoStream
                                            = new MemoryStream(data);
                                        if (ResourceEditor.IsValidIcon(icoStream))
                                        {
                                            _customIconRaw 
                                                = new byte[icoStream.Length];
                                            icoStream.Read(_customIconRaw, 0,
                                                _customIconRaw.Length);
                                            icoStream.Seek(0, SeekOrigin.Begin);
                                            uxPictureBoxCustomIconPreview.Image
                                                = new Icon(icoStream).ToBitmap();
                                            icoStream.Close();
                                            _noShowCustomIcoDialog = true;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                    uxComboBoxCustomIcon.SelectedIndex = 0;
                                }
                            }
                            uxComboBoxCustomIcon.SelectedIndex
                                = iconIndexFromReg;
                        }
                    }

                    int useCustomIconFromReg;
                    if (int.TryParse(wmp11Key.GetValue(Globals.useCustomIconValue,
                           0).ToString(), out useCustomIconFromReg))
                    {
                        switch (useCustomIconFromReg)
                        {
                            case 0:
                                uxCheckBoxCustomIcon.Checked = false;
                                break;

                            case 1:
                                uxCheckBoxCustomIcon.Checked = true;
                                break;

                            default:
                                goto case 0;
                        }
                    }
                }
            }
            if (hotfixes != null)
            {
                if (HotfixesExist(hotfixes))
                {
                    uxTextBoxHotfixLine.Text = hotfixes;
                }
                else if (readiness > 0)
                {
                    readiness--;
                }
            }
            else
            {
                if (wmp11Key != null && !slipstream)
                {
                    string hotfixLineFromReg
                            = wmp11Key.GetValue(Globals.hotfixLineValue,
                            String.Empty).ToString();
                    if (!String.IsNullOrEmpty(hotfixLineFromReg))
                    {
                        if (HotfixesExist(hotfixLineFromReg))
                        {
                            uxTextBoxHotfixLine.Text = hotfixLineFromReg;
                        }
                    }
                }
            }
            if (nocats) uxCheckBoxNoCats.Checked = true;
            readiness++;
            return readiness;
        }

        bool SourceMinimumValid(string winsource)
        {
            return File.Exists(this.CreatePathString(winsource, "i386", "LAYOUT.INF"))
                || File.Exists(this.CreatePathString(winsource, "amd64", "LAYOUT.INF"));
        }

        void SyncUI()
        {
            uxButtonIntegrate.Enabled = _wmp11PathIsReady && _winSrcPathIsReady;
        }

        bool CheckEssentialFiles(string sFolder)
        {
            // Check for essential files
            string[] essentialFiles = new string[] 
                { "wmp.inf", "wmplayer.exe", "mplayer2.exe" };

            foreach (string filepath in essentialFiles)
            {
                if (!File.Exists(this.CreatePathString(sFolder, "i386", filepath))
                    && !File.Exists(this.CreatePathString(sFolder, "i386", 
                    CM.GetCompressedFileName(filepath)))
                    && !File.Exists(this.CreatePathString(sFolder, "amd64", filepath))
                    && !File.Exists(this.CreatePathString(sFolder, "amd64",
                    CM.GetCompressedFileName(filepath)))
                    && !File.Exists(this.CreatePathString(sFolder, "i386", 
                    "w" + filepath))
                    && !File.Exists(this.CreatePathString(sFolder, "i386", 
                    "w" + CM.GetCompressedFileName(filepath))))
                {
                    return false;
                }
            }

            return true;
        }

        void StartIntegration()
        {
            uxTextBoxWinSrc.Text = uxTextBoxWinSrc.Text.TrimEnd(
                Path.DirectorySeparatorChar);
            if (!File.Exists(uxTextBoxWmpRedist.Text))
            {
                MessageBox.Show(Messages.dlgText_WmpRedistNotFound,
                    Messages.dlgTitle_WmpRedistNotFound, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                _closeOnSuccess = false;
            }
            else
            {
                if (!CheckEssentialFiles(uxTextBoxWinSrc.Text))
                {
                    MessageBox.Show(Messages.dlgText_Wmp64FilesMissing,
                        Messages.dlgTitle_Wmp64FilesMissing, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    _closeOnSuccess = false;
                }
                else if (!HotfixesExist(uxTextBoxHotfixLine.Text))
                {
                    MessageBox.Show(Messages.dlgText_InvalidHotfixLine,
                        Messages.dlgTitle_InvalidHotfixLine, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    _closeOnSuccess = false;
                }
                else
                {
                    try
                    {
                        // Disable UI
                        ControlUserInterface(false);

                        // Reset critical condition flag in case we crashed before 
                        _workerInCriticalOperation = false;

                        BackendParams settings = new BackendParams();
                        settings.WinSource = uxTextBoxWinSrc.Text;
                        settings.WmpInstallerSource = uxTextBoxWmpRedist.Text;
                        settings.AddonType = uxComboType.SelectedIndex;
                        settings.HotfixLine = uxTextBoxHotfixLine.Text;
                        settings.IgnoreCats = uxCheckBoxNoCats.Checked;
                        if (uxCheckBoxCustomIcon.Checked)
                        {
                            settings.CustomIcon = _customIconRaw;
                        }
                        _workerThread = new Thread(WorkerMethod);
                        _workerThread.Start(settings);
                        while (_workerThread.IsAlive)
                        {
                            Thread.Sleep(10);
                            Application.DoEvents();
                        }

                        if (settings.Result == BackendResult.Cancelled)
                        {
                            MessageBox.Show(Messages.dlgText_Cancelled,
                                Messages.dlgTitle_Cancelled, MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else if (!_immediateLauch && !_closeOnSuccess
                            && settings.Result == BackendResult.Success)
                        {
                            MessageBox.Show(Messages.dlgText_Success,
                                Messages.dlgTitle_Success, MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }

                        if (_closeOnSuccess && settings.Result == BackendResult.Success)
                        {
                            Application.Exit();
                        }
                        else
                        {
                            ControlUserInterface(true);
                            // Release memory held by Backend (which is quite a lot)
                            this._backend = null;
                            GC.Collect();
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
            }
        }

        void ControlUserInterface(bool state)
        {
            SuspendLayout();
            uxTextBoxWinSrc.Enabled = state;
            uxTextBoxWmpRedist.Enabled = state;
            uxTextBoxHotfixLine.Enabled = state;
            uxTextBoxWinSrc.BackColor = SystemColors.ControlLightLight;
            uxTextBoxWmpRedist.BackColor = SystemColors.ControlLightLight;
            uxTextBoxHotfixLine.BackColor = SystemColors.ControlLightLight;        
            uxComboType.Enabled = state;
            uxButtonWmpRedistPicker.Enabled = state;
            uxButtonWinSrcPicker.Enabled = state;
            uxButtonHotfixPicker.Enabled = state;
            uxCheckBoxNoCats.Enabled = state;
            uxButtonIntegrate.Enabled = state;
            uxCheckBoxCustomIcon.Enabled = state;
            uxComboBoxCustomIcon.Enabled = state;
            uxPictureBoxCustomIconPreview.Enabled = state;
            uxLabelOperation.Visible = !state;
            uxLinkAbout.Visible = state;
            if (state)
            {
                uxProgressBarOverall.Value = 0;
                uxStatusLabelSourceType.Text = String.Empty;
                uxButtonCancel.Text = Messages.uxButtonExit;
            }
            if (!state)
            {
                uxButtonCancel.Text = Messages.uxButtonCancel;
            }
            ResumeLayout();
        }

        bool HotfixesExist(string hotfixLine)
        {
            string hotfixLineCleaned = hotfixLine.Trim();
            if (hotfixLineCleaned.Length > 0)
            {
                string[] hotfixData = hotfixLineCleaned.Split(new char[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries);
                string folder = hotfixData[0];
                if (!Directory.Exists(folder))
                {
                    return false;
                }
                else if (hotfixData.Length == 1)
                {
                    return false;
                }
                else
                {
                    for (int i = 1; i < hotfixData.Length; i++)
                    {
                        string fix = hotfixData[i];
                        if (!File.Exists(this.CreatePathString(folder, fix))) return false;
                    }
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        string CreatePathString(params string[] components)
        {
            return FileSystem.CreatePathString(this._pathBuffer, components);
        }
    }
}