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
        static char pSC = Path.DirectorySeparatorChar;
        bool _immediateLauch;
        bool _closeOnSuccess;
        bool _wmp11PathIsReady;
        bool _winSrcPathIsReady;
        byte[] _customIconRaw;
        bool _noShowCustomIcoDialog;
        Thread _workerThread;

        public MainForm(string installer, string winsource, string hotfixes,
            string output, string customicon, bool nocats, bool slipstream, 
            bool close, string customIconPath)
        {
            InitializeComponent();

            SuspendLayout();
            comboBoxIconSelect.SelectedIndex = 0;
            addonTypeComboBox.SelectedIndex = 0;
            Text += " v" + Globals.Version;

#if DEBUG
            string cabdllfilename;
            if (!File.Exists("Cabinet.dll"))
            {
                cabdllfilename = Environment.GetEnvironmentVariable("WinDir") + "\\"
                + "SYSTEM32\\cabinet.dll";
            }
            else
            {
                cabdllfilename = "Cabinet.dll";
            }
            FileVersionInfo cabInfo = FileVersionInfo.GetVersionInfo(cabdllfilename);
            Text = "WMP11Slip v" + Globals.Version + "; CABINET.DLL v" 
                + cabInfo.FileMajorPart;
#endif

            int readiness = processParameters(installer, winsource, hotfixes, 
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

        int processParameters(string installer, string winsource, 
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
                    textBoxWmp11Source.Text = installer;
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
                            textBoxWmp11Source.Text = installerFromReg;
                        }
                    }
                }
            }
            if (winsource != null)
            {
                if (sourceMinimumValid(winsource))
                {
                    textBoxWindowsSource.Text = winsource.TrimEnd(pSC);
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
                        if (sourceMinimumValid(sourceFromReg))
                        {
                            textBoxWindowsSource.Text = sourceFromReg;
                        }
                    }
                }
            }
            if (output != null)
            {
                if (CM.SEqO(output, "Normal", true))
                    addonTypeComboBox.SelectedIndex = 0;
                else if (CM.SEqO(output, "Tweaked", true))
                    addonTypeComboBox.SelectedIndex = 1;
                readiness++;
            }
            else if (wmp11Key != null && !slipstream)
            {
                int outputFromReg;
                if (int.TryParse(wmp11Key.GetValue(Globals.addonTypeValue,
                        0).ToString(), out outputFromReg))
                {
                    if (outputFromReg < addonTypeComboBox.Items.Count)
                    {
                        addonTypeComboBox.SelectedIndex = outputFromReg;
                    }
                }
            }
            if (customicon != null)
            {
                if (CM.SEqO(customicon, "Boooggy", true)) 
                {
                    checkBoxUseCustIcon.Checked = true;
                    comboBoxIconSelect.SelectedIndex = 0;
                }
                else if (CM.SEqO(customicon, "Vista", true))
                {
                    checkBoxUseCustIcon.Checked = true;
                    comboBoxIconSelect.SelectedIndex = 1;
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
                            pictureBoxPreview.Image
                                = new Icon(icoStream).ToBitmap();
                            icoStream.Close();
                            _noShowCustomIcoDialog = true;
                            checkBoxUseCustIcon.Checked = true;
                            comboBoxIconSelect.SelectedIndex = 2;
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
                        if (iconIndexFromReg < comboBoxIconSelect.Items.Count)
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
                                            pictureBoxPreview.Image
                                                = new Icon(icoStream).ToBitmap();
                                            icoStream.Close();
                                            _noShowCustomIcoDialog = true;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                    comboBoxIconSelect.SelectedIndex = 0;
                                }
                            }
                            comboBoxIconSelect.SelectedIndex
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
                                checkBoxUseCustIcon.Checked = false;
                                break;

                            case 1:
                                checkBoxUseCustIcon.Checked = true;
                                break;

                            default:
                                goto case 0;
                        }
                    }
                }
            }
            if (hotfixes != null)
            {
                if (hotfixesExist(hotfixes))
                {
                    textBoxHotfixList.Text = hotfixes;
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
                        if (hotfixesExist(hotfixLineFromReg))
                        {
                            textBoxHotfixList.Text = hotfixLineFromReg;
                        }
                    }
                }
            }
            if (nocats) checkBoxRemoveCATs.Checked = true;
            readiness++;
            return readiness;
        }

        static bool sourceMinimumValid(string winsource)
        {
            return File.Exists(winsource + pSC + "i386" + pSC + "LAYOUT.INF")
                || File.Exists(winsource + pSC + "amd64" + pSC + "LAYOUT.INF");
        }

        void syncWnd()
        {
            buttonIntegrate.Enabled = _wmp11PathIsReady && _winSrcPathIsReady;
        }

        static bool checkEssentialFiles(string sFolder)
        {
            // Check for essential files
            string[] essentialFiles = new string[] 
                { "wmp.inf", "wmplayer.exe", "mplayer2.exe" };

            int readiness = 0;
            string EULA;

            if (File.Exists(sFolder + pSC + "i386" + pSC + "EULA.TXT"))
                EULA = File.ReadAllText(sFolder + pSC + "i386" + pSC + "EULA.TXT");
            else if (File.Exists(sFolder + pSC + "amd64" + pSC + "EULA.TXT"))
                EULA = File.ReadAllText(sFolder + pSC + "amd64" + pSC + "EULA.TXT");
            else 
                return false;

            int indexOfEulaId = EULA.IndexOf("EULAID:", 
                StringComparison.OrdinalIgnoreCase);
            if (indexOfEulaId > 0)
            {
                if (EULA.IndexOf("RME", indexOfEulaId,
                    StringComparison.OrdinalIgnoreCase)
                    > 0)
                {
                    return true;
                }
            }

            foreach (string filepath in essentialFiles)
            {
                if (File.Exists(sFolder + pSC + "i386" + pSC + filepath)
                    || File.Exists(sFolder + pSC + "i386" + pSC
                    + CM.GetCompressedFileName(filepath))
                    || File.Exists(sFolder + pSC + "amd64" + pSC + filepath)
                    || File.Exists(sFolder + pSC + "amd64" + pSC
                    + CM.GetCompressedFileName(filepath))
                    || File.Exists(sFolder + pSC + "i386" + pSC + "w" + filepath)
                    || File.Exists(sFolder + pSC + "i386" + pSC + "w"
                    + CM.GetCompressedFileName(filepath))
                    )
                {
                    readiness++;
                }
            }

            return (readiness == essentialFiles.Length);
        }

        void StartIntegration()
        {
            textBoxWindowsSource.Text = textBoxWindowsSource.Text.TrimEnd('\\');
            if (!File.Exists(textBoxWmp11Source.Text))
                MessageBox.Show("Windows Media Player 11 installer could not be found.\nPlease locate it and try again.", "Unable to find WMP11 installer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
            {
                if (!checkEssentialFiles(textBoxWindowsSource.Text))
                {

                    string message = "Essential Windows Media Player 9 and 6.4 files are missing from the source.\r\n\r\nWindows Media Player 11 cannot be slipstreamed into a source from which these\r\ntwo components have been removed with nLite or similar source reduction utilities.\r\n\r\nThis dialog box will close after 10 seconds.";
                    SourceError errorWindow = new SourceError(message);
                    errorWindow.ShowDialog();
                    if (_closeOnSuccess)
                    {
                        Application.Exit();
                    }
                }
                else if (!hotfixesExist(textBoxHotfixList.Text))
                {
                    string message = "Invalid hotfix line specified or some hotfixes in the list do not exist.\r\n\r\nPlease choose the hotfixes to integrate and try again.\r\n\r\nThis dialog will close automatically after 10 seconds.";
                    SourceError errorWindow = new SourceError(message);
                    errorWindow.ShowDialog();
                    if (_closeOnSuccess)
                    {
                        Application.Exit();
                    }
                }
                else
                {
                    try
                    {
                        // Disable UI
                        ControlUserInterface(false);

                        // Reset critical condition flag in case we crashed
                        // before or something
                        _workerInCriticalOperation = false;

                        BackendParams settings = new BackendParams();
                        settings.WinSource = textBoxWindowsSource.Text;
                        settings.WmpInstallerSource = textBoxWmp11Source.Text;
                        settings.AddonType = addonTypeComboBox.SelectedIndex;
                        settings.HotfixLine = textBoxHotfixList.Text;
                        settings.IgnoreCats = this.checkBoxRemoveCATs.Checked;
                        if (checkBoxUseCustIcon.Checked)
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
                            MessageBox.Show(Globals.cancelMessage,
                                "Cancelled", MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                        else if (!_immediateLauch && !_closeOnSuccess
                            && settings.Result == BackendResult.Success)
                        {
                            MessageBox.Show(Globals.successMessage,
                                "Success", MessageBoxButtons.OK,
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
            textBoxWindowsSource.Enabled = state;
            textBoxWmp11Source.Enabled = state;
            textBoxHotfixList.Enabled = state;
            textBoxWindowsSource.BackColor = SystemColors.ControlLightLight;
            textBoxWmp11Source.BackColor = SystemColors.ControlLightLight;
            textBoxHotfixList.BackColor = SystemColors.ControlLightLight;        
            addonTypeComboBox.Enabled = state;
            btnWmp11SourceBrowse.Enabled = state;
            btnWindowsSourceBrowse.Enabled = state;
            buttonHotfixBrowse.Enabled = state;
            checkBoxRemoveCATs.Enabled = state;
            buttonIntegrate.Enabled = state;
            checkBoxUseCustIcon.Enabled = state;
            comboBoxIconSelect.Enabled = state;
            pictureBoxPreview.Enabled = state;
            statusLabel.Visible = !state;
            aboutLinkLabel.Visible = state;
            if (state)
            {
                progressBarTotalProgress.Value = 0;
                statusLabelSourceType.Text = String.Empty;
                buttonCancel.Text = "E&xit";

            }
            if (!state)
            {
                buttonCancel.Text = "&Cancel";
            }
            ResumeLayout();
        }

        static bool hotfixesExist(string hotfixLine)
        {
            if (hotfixLine.Trim().Length > 0)
            {
                string hotfixLineCleaned = hotfixLine.Trim();
                string[] hotfixData = hotfixLineCleaned.Split(new char[] { '|' },
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

                bool first = false;
                foreach (string line in hotfixData)
                {
                    if (!first)
                    {
                        first = true;
                        continue;
                    }
                    else
                    {
                        if (!File.Exists(Path.Combine(folder, line)))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            else
            {
                return true;
            }
        }
    }
}