#region Using statements
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml;
using Epsilon.IO.Compression;
using Epsilon.IO;
using Epsilon.WindowsModTools;
using Epsilon.Win32.Resources;
using Epsilon.Win32;
using Epsilon.Parsers;
using Epsilon.DebugServices;
using Epsilon.Security.Cryptography;
using Epsilon.Collections.Generic;
using System.Text.RegularExpressions;
using Epsilon.IO.Compression.Cabinet;
#endregion

// This is the main backend that performs the actual integration tasks

namespace WMP11Slipstreamer
{
    partial class Backend
    {
        internal Backend(BackendParams paramsObject)
        {
            // Initialise fields
            this.AnnounceOperation("Initialising...");

            this._addonTypeIndex = paramsObject.AddonType;
            this._sourceDirectory = Path.GetFullPath(paramsObject.WinSource);

            if (Path.GetDirectoryName(this._sourceDirectory) == null)
                this._sourceDirectory = this._sourceDirectory.TrimEnd(
                    Path.DirectorySeparatorChar);

            this._wmp11InstallerPath = Path.GetFullPath(paramsObject.WmpInstallerSource);
            this._customIcon = paramsObject.CustomIcon;

            if (paramsObject.HotfixLine.Trim().Length > 0)
            {
                string[] hotfixData = paramsObject.HotfixLine.Split(new char[] { '|' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (hotfixData.Length > 1)
                {
                    _hotfixInstallerList = new List<string>(hotfixData.Length - 1);
                    for (int i = 1; i < hotfixData.Length; i++)
                    {
                        _hotfixInstallerList.Add(
                            this.CombinePathComponents(hotfixData[0], hotfixData[i]));
                    }
                }
            }
            else
            {
                _hotfixInstallerList = new List<string>();
            }

            // Ignore cats option
            this._ignoreCats = paramsObject.IgnoreCats;

            // Max number of global steps
            this._progressTotalSteps = 14;

            this.IncrementGlobalProgress();

            CancelOpportunity();
        }

        /// <summary>
        /// Runs the slipstream operation
        /// </summary>
        internal void SlipstreamWMP11()
        {
            this.DetectSourceType();
            this.PassJudgementOnSource();
            this.ExtractWMP11Installer();
            this.PrepareDependencies();
            this.PrepareForParse();
            this.ParseAndEditFiles();
            this.ApplyFixes();
            this.SaveFiles();
            this.CompressFiles();
#if DEBUG
            if (this.OnBeforeMergeFolders())
            {
#endif
                this.MergeFolders();
                this.CleanUpFolders();
#if DEBUG
            }
#endif
        }

        void DetectSourceType()
        {
            this.AnnounceOperation("Detecting source type...");
            this._sourceInfo = new WindowsSourceInfo();

            string archFolder32 = this.CombinePathComponents(this._sourceDirectory, 
                "i386");
            string archFolder64 = this.CombinePathComponents(this._sourceDirectory, 
                "amd64");

            string commonExceptionError
                = "\"{0}\" does not seem to contain a valid {1}Windows™ source.";
            if (File.Exists(this.CombinePathComponents(archFolder32, "LAYOUT.INF")))
            {
                PeEditor editor = new PeEditor(this.CombinePathComponents(archFolder32,
                    "SYSTEM32", "NTDLL.DLL"));
                if (editor.TargetMachineType != Architecture.x86)
                {
                    throw new Exceptions.IntegrationException(
                        String.Format(
                        commonExceptionError,
                        this._sourceDirectory, "32-bit ")
                    );
                }
                this._sourceInfo.Arch = TargetArchitecture.x86;
                this._archFilesDirectory = archFolder32;
            }
            else if (File.Exists(this.CombinePathComponents(archFolder64, "LAYOUT.INF")))
            {
                PeEditor editor = new PeEditor(this.CombinePathComponents(archFolder64,
                    "SYSTEM32", "NTDLL.DLL"));
                if (editor.TargetMachineType != Architecture.x64)
                {
                    throw new Exceptions.IntegrationException(
                        String.Format(
                        commonExceptionError,
                        this._sourceDirectory, "x64 ")
                    );
                }
                this._sourceInfo.Arch = TargetArchitecture.x64;
                this._archFilesDirectory = archFolder64;
            }
            else
            {
                throw new InvalidOperationException(
                    String.Format(
                    commonExceptionError,
                    this._sourceDirectory, String.Empty)
                    );
            }

            IniParser layoutINFEditor = new IniParser(
                this.CombinePathComponents(this._archFilesDirectory, "LAYOUT.INF"), true
                );

            #region Detect product type
            string prodversion = layoutINFEditor.ReadValue("Strings", "productname");
            string EULA = File.ReadAllText(this.CombinePathComponents(
                _archFilesDirectory, "EULA.TXT"));
            int indexOfEulaId = EULA.IndexOf("EULAID:");
            if (prodversion.Contains("XP"))
            {
                _sourceInfo.SourceVersion = WindowsType._XP;

                if (prodversion.Contains("Profess"))
                {
                    if (EULA.Contains("Media Center") &&
                        (EULA.Contains("Media Player")
                        || EULA.Contains("Lecteur Windows Media"))
                        && (indexOfEulaId > 0
                        && EULA.IndexOf("MCE", indexOfEulaId) > 0))
                    {
                        _sourceInfo.Edition = WindowsEdition.MediaCenter;
                    }
                    else
                    {
                        _sourceInfo.Edition = WindowsEdition.Professional;
                    }
                }
                else if (prodversion.Contains("Home") ||
                    prodversion.Contains("familiale"))
                {
                    if (EULA.Contains("Media Center") &&
                        (EULA.Contains("Media Player")
                        || EULA.Contains("Lecteur Windows Media"))
                        && (indexOfEulaId > 0
                        && EULA.IndexOf("MCE", indexOfEulaId) > 0))
                    {
                        _sourceInfo.Edition = WindowsEdition.MediaCenter;
                    }
                    else
                    {
                        _sourceInfo.Edition = WindowsEdition.Home;
                    }
                }
            }
            else if (prodversion.Contains("2003"))
                _sourceInfo.SourceVersion = WindowsType._Server2003;
            else if (prodversion.Contains("2000"))
                _sourceInfo.SourceVersion = WindowsType._2000;
            else
                _sourceInfo.SourceVersion = WindowsType._Unknown;

            // Start building a version string
            _osVersionBuilder.Append("Windows™");

            switch (_sourceInfo.SourceVersion)
            {
                case WindowsType._XP:
                    _osVersionBuilder.Append(" XP");
                    break;
               
                case WindowsType._Server2003:
                    _osVersionBuilder.Append(" Server 2003");
                    break;

                case WindowsType._2000:
                    _osVersionBuilder.Append(" 2000");
                    break;

                case WindowsType._Unknown:
                    _osVersionBuilder.Append(" <Unknown>");
                    break;
            }
            #endregion

            #region Edition
            switch (_sourceInfo.Edition)
            {
                case WindowsEdition.Home:
                    _osVersionBuilder.Append(" Home Edition");
                    break;

                case WindowsEdition.Professional:
                    _osVersionBuilder.Append(" Professional");
                    break;

                case WindowsEdition.MediaCenter:
                    _osVersionBuilder.Append(" Media Center Edition");
                    break;

                default:
                    break;
            }
            #endregion

            #region Special Architecture Name
            if (_sourceInfo.Arch == TargetArchitecture.x64)
                _osVersionBuilder.Append(" x64");
            #endregion

            #region Reduced Media Edition Detection
            if (indexOfEulaId > 0 && EULA.IndexOf("RME", indexOfEulaId) > 0
                && _sourceInfo.SourceVersion == WindowsType._XP)
            {
                _sourceInfo.ReducedMediaEdition = true;
                _osVersionBuilder.Append(" N");
            }
            #endregion

            #region Detect service pack level
            string splevelstr = null;
            if (layoutINFEditor.KeyExists("Strings", "spcdname"))
                splevelstr = layoutINFEditor.ReadValue("Strings", "spcdname");
            else if (layoutINFEditor.KeyExists("Strings", "spcd"))
                splevelstr = layoutINFEditor.ReadValue("Strings", "spcd");

            if (splevelstr != null)
            {
                string strMatched 
                    = Regex.Match(splevelstr, @"Service\x20Pack\x20(\d+)")
                    .Groups[1].Value;
                if (strMatched.Length >= 1)
                    _sourceInfo.ServicePack = int.Parse(strMatched);
            }

            if (_sourceInfo.ServicePack > 0)
            {
                _osVersionBuilder.Append(" SP");
                _osVersionBuilder.Append(_sourceInfo.ServicePack);
            }
            #endregion

            if (!_aborting && this.OnSourceDetected != null)
            {
                this.OnSourceDetected(this._osVersionBuilder.ToString());
            }

            this.CancelOpportunity();
            this.IncrementGlobalProgress();
        }

        /// <summary>
        /// Judge if source is eligible for integration or not
        /// </summary>
        void PassJudgementOnSource()
        {
            switch (_sourceInfo.SourceVersion)
            {
                case WindowsType._Server2003:
                    if (_sourceInfo.ServicePack < 1)
                        throw new Exceptions.SourceNotSupportedException(this._sourceInfo);
                    break;

                case WindowsType._XP:
                    if (_sourceInfo.Arch == TargetArchitecture.x86)
                    {
                        if (_sourceInfo.ServicePack < 2)
                            throw new Exceptions.SourceNotSupportedException(this._sourceInfo);
                    }
                    else
                    {
                        if (_sourceInfo.ServicePack < 1)
                            throw new Exceptions.SourceNotSupportedException(this._sourceInfo);
                    }
                    break;

                case WindowsType._2000:
                    throw new Exceptions.SourceNotSupportedException(this._sourceInfo);
            }

            if (this._sourceInfo.ReducedMediaEdition)
                throw new Exceptions.SourceNotSupportedException(this._sourceInfo);

            // Init rest of common folders
            _workingDirectory = this.CombinePathComponents(_archFilesDirectory, 
                "wmp11temp");
            _extractedDirectory = this.CombinePathComponents(_workingDirectory,
                "extracted");
            _drivercabExtractedDirectory = this.CombinePathComponents(_extractedDirectory,
                "drivercab");
            FileSystem.CreateEmptyDirectory(_workingDirectory);
        }

        /// <summary>
        /// Prepares for parsing by copying commonly-edited temporary files
        /// from the "i386" directory to the extractedDirectory
        /// </summary>
        void PrepareForParse()
        {
            this.AnnounceOperation("Copying setup files to edit...");
            string[] uncompressedTempFiles = new string[]
            {
                "TXTSETUP.SIF",
                "DOSNET.INF",
                "DRVINDEX.INF"
            };
            foreach (string file in uncompressedTempFiles)
            {
                string fileToCopy = this.CombinePathComponents(
                    this._archFilesDirectory, file);
                if (File.Exists(fileToCopy))
                {
                    FileSystem.CopyFile(fileToCopy,
                        this.CombinePathComponents(this._workingDirectory, file));
                }
                else
                {
                    throw new
                        Exceptions.IntegrationException(
                        String.Format(
                        "The file \"{0}\" is not present in the i386 folder.",
                        file
                        )
                    );
                }
            }

            foreach (string file in _possCompArchFile)
            {
                CancelOpportunity();
                CopyOrExpandFromArch(file, this._workingDirectory, false);
            }

            CancelOpportunity();

            if (_sourceInfo.SourceVersion == WindowsType._Server2003 || 
                _sourceInfo.Arch == TargetArchitecture.x64)
            {
                // Determine which CAB to extract to repack drivers
                this.AnnounceOperation("Determining driver cabinet filename...");
                _drvIndexEditor = new IniParser(
                    this.CombinePathComponents(_archFilesDirectory, "DrvIndex.inf"), true
                );
                this.AnnounceOperation("Extracting drivers. Please wait...");

                if (this._sourceInfo.Arch == TargetArchitecture.x86)
                {
                    List<string> cabFilesData =
                        this._drvIndexEditor.ReadAllValues("Version", "CabFiles");
                    cabFilesData.Sort(StringComparer.OrdinalIgnoreCase);
                    string variableName = cabFilesData[cabFilesData.Count - 1];
                    _driverCabFile = _drvIndexEditor.ReadValue("Cabs", variableName);
                }
                else
                {
                    _driverCabFile = "driver.cab";
                }

                CancelOpportunity();
                Directory.CreateDirectory(_drivercabExtractedDirectory);

                this.ResetCurrentProgress();
#if DEBUG
                DateTime before = DateTime.Now;
#endif
                Archival.NativeCabinetExtract(
                    this.CombinePathComponents(_archFilesDirectory, _driverCabFile),
                    _drivercabExtractedDirectory,
                    null,
                    this.UpdateCurrentProgress
                );
#if DEBUG
                DateTime after = DateTime.Now;
                TimeSpan timer = after - before;
                this.OnDebuggingMessage(timer.ToString(), "Extraction Speed");
#endif
                this.HideCurrentProgress();
            }

            this.AnnounceOperation("Reading setup files...");
            _txtsetupSifEditor = new IniParser(
                this.CombinePathComponents(_workingDirectory, "Txtsetup.Sif"), true
            );
            _dosnetInfEditor = new IniParser(
                this.CombinePathComponents(_workingDirectory, "Dosnet.Inf"), true
            );

            ApplyTxtsetupEditorHacks(_txtsetupSifEditor);

            _sysocInfEditor = new IniParser(
                this.CombinePathComponents(_workingDirectory, "SysOc.Inf"), true
            );
            _svcpackInfEditor = new IniParser(
                this.CombinePathComponents(_workingDirectory, "Svcpack.Inf"), true
            );
            CancelOpportunity();
            this.IncrementGlobalProgress();
        }

        void PrepareDependencies()
        {
            CryptoHelp crypto = new CryptoHelp(Globals.uniqueTag);

            // Nice message for progress
            this.AnnounceOperation("Reading parse files...");

            // Create extracted folder
            Directory.CreateDirectory(_extractedDirectory);

            // entries.ini: repository1
            string entriesContent = crypto.DecryptToString(
                Properties.Resources.repository1,
                Globals.repository1Key,
                Encoding.Default
            );

            this._entriesCombinedEditor =
                new IniParser(new StringReader(entriesContent), 
                true, "entries_wmp11.ini");

            byte[] repository = null;
            // repository1: entries_combined_wmp11.ini
            // repository2: (reserved)
            // repository3: XP Home/Pro x32
            // repository4: XP MCE 2005 x32
            // repository5: Server 2003 x32
            // repository6: (reserved)
            // repository7: XP/2k3 x64
            switch (_sourceInfo.SourceVersion)
            {
                case WindowsType._Unknown:
                    throw new Exceptions.IntegrationException("Unable to detect"
                        + " source type. Unable to continue slipstream.");

                case WindowsType._XP:
                    if (_sourceInfo.Arch == TargetArchitecture.x64)
                        repository = Properties.Resources.repository7;
                    else if (_sourceInfo.Edition == WindowsEdition.MediaCenter)
                        repository = Properties.Resources.repository4;
                    else
                        repository = Properties.Resources.repository3;
                    break;

                case WindowsType._Server2003:
                    if (_sourceInfo.Arch == TargetArchitecture.x64)
                        goto case WindowsType._XP;
                    else
                        repository = Properties.Resources.repository5;
                    break;
            }

            // Decrypt repositories
            MemoryStream repositoryDecrypted = new MemoryStream();
            crypto.DecryptStream(new MemoryStream(repository),
                Globals.otherReposKeys, repositoryDecrypted, false);
            repositoryDecrypted.Seek(0, SeekOrigin.Begin);

            // Temp Folder
            string tempFolder = this.CombinePathComponents(_workingDirectory,
                "wmp11int.r" + new Random().Next(99).ToString());

            CancelOpportunity();

            // Extract repository cabinet
            Archival.NativeCabinetExtract(repositoryDecrypted, tempFolder, null, null);

            string[] infEssentialOverwrites = new string[] { 
                "wmp.inf", "wmfsdk.inf", "wpd.inf", "wmp11.inf" };

            // Files that are embedded and should overwrite in extracted
            foreach (string essentialOverwrite in infEssentialOverwrites)
            {
                if (File.Exists(this.CombinePathComponents(tempFolder, 
                    essentialOverwrite)))
                {
                    FileSystem.MoveFileOverwrite(
                        this.CombinePathComponents(tempFolder, essentialOverwrite),
                        this.CombinePathComponents(_extractedDirectory, 
                        essentialOverwrite)
                    );
                }
            }

            // External INF handled via special routine
            File.Move(
                this.CombinePathComponents(tempFolder, "wmp11ext.inf"),
                this.CombinePathComponents(_workingDirectory, "wmp11ext.inf")
            );

            // Attach both INF editors on the 2 main INFs
            _wmp11ExtInfEditor = new IniParser(
                this.CombinePathComponents(_workingDirectory, "wmp11ext.inf"), true
            );
            _wmp11InfEditor = new IniParser(
                this.CombinePathComponents(_extractedDirectory, "wmp.inf"), true
            );

            // Migrate localizable [Strings] from original infs
            string wmfSdkPath = this.CombinePathComponents(this._extractedDirectory,
                "wmfsdk.inf");
            this.MigrateStringsFromOriginalInf("wmp.inf", tempFolder, _wmp11InfEditor);
            if (File.Exists(wmfSdkPath))
            {
                IniParser wmfsdkEditor = new IniParser(wmfSdkPath, true);
                this.MigrateStringsFromOriginalInf("wmfsdk.inf", tempFolder, 
                    wmfsdkEditor);
                wmfsdkEditor.SaveIni();
            }

            FileSystem.Delete(tempFolder);
            CancelOpportunity();
            this.IncrementGlobalProgress();
        }

        void ExtractWMP11Installer()
        {
            CancelOpportunity();
            this.AnnounceOperation("Extracting Windows Media Player 11 Installer...");

            string[] filesToExtract = null;

            switch (this._sourceInfo.Arch)
            {
                case TargetArchitecture.x86:
                    filesToExtract = new string[] {
                        "umdf.exe",
                        "WindowsXP-MSCompPackV1-x86.exe", 
                        "wmfdist11.exe",
                        "wmp11.exe"};
                    break;

                case TargetArchitecture.x64:
                    filesToExtract = new string[] {
                        "umdf.exe",
                        "WindowsServer2003.WindowsXP-MSCompPackV1-x64.exe", 
                        "wmfdist11-64.exe",
                        "wmp11-64.exe"};
                    break;
            }

            // The +2 is for the installer itself, and wmpappcompat in a special case
            this.ResetCurrentProgress();
            ProgressTracker currProgress 
                = new ProgressTracker(filesToExtract.Length + 2);

            HelperConsole.InfoWriteLine(String.Format("NativeExtractHotfix: {0}",
                _wmp11InstallerPath));
            NativeExtractHotfix(_wmp11InstallerPath, _extractedDirectory);
            this.IncrementCurrentProgress(currProgress);

            // Read control.xml and determine if we're on correct installer or not
            ParseXmlAndVerifyDestinationOs(
                this.CombinePathComponents(_extractedDirectory, "control.xml"));

            foreach (string file in filesToExtract)
            {
                CancelOpportunity();
                HelperConsole.InfoWriteLine(String.Format("NativeExtractHotfix: {0}",
                    file));
                NativeExtractHotfix(this.CombinePathComponents(
                    _extractedDirectory, file), 
                    _extractedDirectory);
                if (!HotfixMatchesArch(_extractedDirectory))
                {
                    throw new Exceptions.IntegrationException(
                        "This version of Windows Media Player 11 installer is not compatible with your OS installation source.\nMake sure that both are 32-bit or x64 versions.");
                }
                this.IncrementCurrentProgress(currProgress);
            }
            if (this._sourceInfo.SourceVersion != WindowsType._Server2003
                && this._sourceInfo.Arch == TargetArchitecture.x86)
            {
                NativeExtractHotfix(this.CombinePathComponents(_extractedDirectory,
                    "wmpappcompat.exe"), _extractedDirectory);
            }
            this.IncrementCurrentProgress(currProgress);

            if (Directory.Exists(this.CombinePathComponents(_extractedDirectory, 
                "SP2QFE")))
            {
                foreach (string filepath in Directory.GetFiles(
                    this.CombinePathComponents(_extractedDirectory, "SP2QFE"), "*", 
                    SearchOption.AllDirectories))
                {
                    string filename = Path.GetFileName(filepath);
                    File.Move(filepath, this.CombinePathComponents(_extractedDirectory, 
                        filename));
                }
            }

            this.HideCurrentProgress();
            this.IncrementGlobalProgress();

#if DEBUG
            this.OnDebuggingMessage("WMP11 installer extracted. This is a reference "
                + "point to examine where the basic files are located.",
                "Control");
#endif
        }

        void ParseXmlAndVerifyDestinationOs(string xmlPath)
        {
            HelperConsole.InfoWriteLine("ParseXmlAndVerifyDestinationOs");
            if (!File.Exists(xmlPath))
            {
                throw new Exceptions.IntegrationException(
                    "Invalid WMP11 installer selected. Unable to locate control.xml to verify destination source type.");
            }
            string fileContents = File.ReadAllText(xmlPath);
            XmlDocument document = new XmlDocument();
            document.LoadXml(fileContents);
            XmlNodeList nodes = document.GetElementsByTagName("updatedb");
            if (nodes.Count > 0)
            {
                string os = nodes[0].Attributes["OS"].Value;
                string arch = nodes[0].Attributes["Arch"].Value;

                string[] osBounds = os.Split('-');
                string osLowerBound = osBounds[0];
                string osUpperBound = (osBounds.Length > 1) ? osBounds[1] : null;

                Version osLowerBoundVer = new Version(osLowerBound);
                Version osUpperBoundVer
                    = (!String.IsNullOrEmpty(osUpperBound)) ? 
                    new Version(osUpperBound) : new Version(int.MaxValue, 
                        int.MaxValue);
                Version osVer = null;
                string osArch = null;

                if (this._sourceInfo.Arch == TargetArchitecture.x86)
                    osArch = "x86";
                else if (this._sourceInfo.Arch == TargetArchitecture.x64)
                    osArch = "amd64";

                if (this._sourceInfo.SourceVersion == WindowsType._2000)
                    osVer = new Version(5, 0, 2195);
                else if (this._sourceInfo.SourceVersion == WindowsType._Server2003
                    || (this._sourceInfo.SourceVersion == WindowsType._XP 
                    && this._sourceInfo.Arch == TargetArchitecture.x64))
                    osVer = new Version(5, 2, 3790);
                else if (this._sourceInfo.SourceVersion == WindowsType._XP)
                    osVer = new Version(5, 1, 2600);

                if (osVer.CompareTo(osLowerBoundVer) < 0
                    || osVer.CompareTo(osUpperBoundVer) > 0
                    || !CM.SEqO(arch, osArch, true))
                {
                    throw new Exceptions.IntegrationException(
                        String.Format("The specified WMP11 installer is not compatible with the specified destination source.\r\n\r\nCurrent WMP11 installer is designed for: Windows v{0} {1}\r\nCurrent source: Windows v{2} {3}",
                        osLowerBound + ((osUpperBound == null)? String.Empty : "-" + osUpperBound), arch, osVer, osArch));
                }
            }
            else
            {
                throw new Exceptions.IntegrationException(
                    String.Format("Unable to read \"{0}\" in the installer to validate target source type.", 
                    Path.GetFileName(xmlPath)));
            }
        }

        bool HotfixMatchesArch(string folderToCheck)
        {
            HelperConsole.InfoWriteLine("HotfixMatchArch");
            string updateFilename = this.CombinePathComponents(folderToCheck,
                "Update", "Update.exe");
            PeEditor editor = new PeEditor(updateFilename);
            FileSystem.Delete(updateFilename);
            return (editor.TargetMachineType == Architecture.x86
                && this._sourceInfo.Arch == TargetArchitecture.x86)
                || (editor.TargetMachineType == Architecture.x64
                && this._sourceInfo.Arch == TargetArchitecture.x64);
        }

        void ParseAndEditFiles()
        {
            this.AnnounceOperation("Preparing to edit files...");
            string dosnetFilesSection = null;
            string txtsetupFilesSection = null;
            string txtsetupDirSection = null;
            string svcPackSection = null;

            // HACK SP3 Bug fix
            if (this._sourceInfo.SourceVersion == WindowsType._XP
                && this._sourceInfo.ServicePack == 3)
            {
                this.FixWBEM();
            }

            switch (this._sourceInfo.SourceVersion)
            {
                case WindowsType._XP:
                    if (this._sourceInfo.Arch == TargetArchitecture.x64)
                    {
                        dosnetFilesSection = "xp_x64_dosnet_files";
                        txtsetupFilesSection = "xp_x64_txtsetup_files";
                        svcPackSection = "xp_x64_svcpack";
                    }
                    else if (this._sourceInfo.Edition == WindowsEdition.MediaCenter)
                    {
                        dosnetFilesSection = "mce_dosnet_files";
                        txtsetupFilesSection = "mce_txtsetup_files";
                        svcPackSection = "mce_svcpack";
                    }
                    else
                    {
                        dosnetFilesSection = "xp_dosnet_files";
                        txtsetupFilesSection = "xp_txtsetup_files";
                        txtsetupDirSection = "xp_txtsetup_dirs";
                    }
                    break;

                case WindowsType._Server2003:
                    if (this._sourceInfo.Arch == TargetArchitecture.x64)
                    {
                        goto case WindowsType._XP;
                    }
                    else
                    {
                        dosnetFilesSection = "2k3_dosnet_files";
                        txtsetupFilesSection = "2k3_txtsetup_files";
                    }
                    break;
            }

            this.ResolveDestinationDirIdConflicts(txtsetupFilesSection, 
                txtsetupDirSection);

            OrderedDictionary<string, List<string>> txtsetupFilesRef
                = this._entriesCombinedEditor.GetRef(txtsetupFilesSection);
            this.AnnounceOperation("Editing Txtsetup.sif...");
            CancelOpportunity();

            // Initialise the file copy dictionaries
            this._filesToCompressInArch = new Dictionary<string, string>(
                txtsetupFilesRef.Count,
                StringComparer.OrdinalIgnoreCase
            );
            this._filesToCopyInArch = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase
            );
            if (this._sourceInfo.Arch == TargetArchitecture.x64)
            {
                this._filesToCompressInI386 = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );

                // Figure out which SourceDisksNames entry for
                // I386 folder on x64 platform
                OrderedDictionary<string, List<string>> sourceDisksNames
                    = this._txtsetupSifEditor.GetRef("SourceDisksNames.amd64");
                foreach (KeyValuePair<string, List<string>> pair in sourceDisksNames)
                {
                    if (pair.Value.Count > 3 && CM.SEqO(pair.Value[3], @"\i386", true))
                    {
                        this._sdnEntryI386Folder = int.Parse(pair.Key);
                        break;
                    }
                }
                if (this._sdnEntryI386Folder == 0)
                {
                    throw new Exceptions.IntegrationException(
                        "Cannot find index of 32-bit i386 folder. This x64 source"
                        + " is corrupt.");
                }
            }
            
            this._txtsetupSifEditor.Add(
                "SourceDisksFiles",
                txtsetupFilesRef,
                IniParser.KeyExistsPolicy.Ignore
            );

            if (txtsetupDirSection != null)
            {
                this._txtsetupSifEditor.Add(
                    "WinntDirectories",
                    this._entriesCombinedEditor.GetRef(txtsetupDirSection),
                    IniParser.KeyExistsPolicy.Ignore
                );
            }

            this.AnnounceOperation("Building file list...");
            foreach (KeyValuePair<string, List<string>> txtPair in txtsetupFilesRef)
            {
                string shortName = txtPair.Key;
                string longName = null;
                bool uncompressed = false;
                bool isX6432bitFile = false;

                if (txtPair.Value.Count < 10)
                {
                    throw new Exceptions.IntegrationException(String.Format(
                        "Malformed line in entries file. ({0} = {1})",
                        txtPair.Key, new CSVParser().Join(txtPair.Value))
                    );
                }
                else if (txtPair.Value.Count >= 11)
                {
                    // Try to get the longname, make sure it is not just a number
                    // (which would indicate it's one of those weird undocumented flags.
                    int testNumber;
                    if (txtPair.Value[10].Length > 0 &&
                        !int.TryParse(txtPair.Value[10], out testNumber))
                    {
                        longName = txtPair.Value[10];
                    }
                }

                // Check if the file should be compressed or not
                uncompressed = String.Equals(txtPair.Value[6], "_x",
                    StringComparison.OrdinalIgnoreCase);

                // Check if this a 32-bit file on a x64 source
                isX6432bitFile = this._sourceInfo.Arch == TargetArchitecture.x64
                    && int.Parse(txtPair.Value[0]) == this._sdnEntryI386Folder;

                // Add to the dictionary
                if (longName == null) longName = shortName;

                if (!isX6432bitFile)
                {
                    if (uncompressed)
                    {
                        this._filesToCopyInArch.Add(shortName, longName);
                    }
                    else
                    {
                        this._filesToCompressInArch.Add(shortName, longName);
                    }
                }
                else
                {
                    this._filesToCompressInI386.Add(shortName, longName);
                }
            }

            this.AnnounceOperation("Editing Dosnet.inf...");
            OrderedDictionary<string, List<string>> dosnetRef 
                = this._entriesCombinedEditor.GetRef(dosnetFilesSection);
            this._dosnetInfEditor.Add(
                "Files",
                dosnetRef,
                IniParser.KeyExistsPolicy.Ignore
            );

            this.AnnounceOperation("Editing SysOc.inf...");
            OrderedDictionary<string, List<string>> sysOcRef = 
                this._entriesCombinedEditor.GetRef("common_sysoc");

            if (sysOcRef[0].Value.Count != 5)
                throw new InvalidDataException(String.Format(
                    "Invalid SysOC line: [{0} = {1}] in common entries file.",
                    sysOcRef[0].Key, new CSVParser().Join(sysOcRef[0].Value)));

            this._externalInfFilename = sysOcRef[0].Value[2];
            if (this._filesToCompressInArch.ContainsKey(_externalInfFilename))
                this._filesToCompressInArch.Remove(_externalInfFilename);
            else if (this._filesToCompressInI386 != null
                && this._filesToCompressInI386.ContainsKey(_externalInfFilename))
                this._filesToCompressInI386.Remove(_externalInfFilename);
            else
                throw new Exceptions.IntegrationException(
                    "SysOc-referenced Inf not referenced in [dosnet_files].");

            this._sysocInfEditor.Add(
                "Components",
                sysOcRef[0].Key,
                sysOcRef[0].Value,
                IniParser.KeyExistsPolicy.Ignore
            );

            if (!this._ignoreCats)
            {
                this.AnnounceOperation("Editing Svcpack.inf...");

                List<string[]> svcpackData = new List<string[]>();
                if (this._sourceInfo.Arch == TargetArchitecture.x86)
                {
                    svcpackData.AddRange(this._entriesCombinedEditor.ReadCsvLines(
                        "common_svcpack", 2));
                }

                if (svcPackSection != null)
                {
                    svcpackData.AddRange(
                        this._entriesCombinedEditor.ReadCsvLines(svcPackSection, 2));
                }

                // Fixing fresh svcpack.inf
                this._svcpackInfEditor.RemoveLine(
                    "SetupData",
                    "CatalogSubDir"
                );

                this._svcpackInfEditor.Add(
                    "SetupData",
                    "CatalogSubDir",
                    (this._sourceInfo.Arch == TargetArchitecture.x86) ?
                    @"""\i386\svcpack""" : @"""\amd64\svcpack""",
                    IniParser.KeyExistsPolicy.Discard
                );

                // Add data if not exists
                Dictionary<string, int> svcpackCriticalKeys 
                    = new Dictionary<string, int>(3);

                if (this._sourceInfo.SourceVersion == WindowsType._Server2003
                    || (this._sourceInfo.SourceVersion == WindowsType._XP
                    && this._sourceInfo.Arch == TargetArchitecture.x64))
                {
                    svcpackCriticalKeys.Add("MajorVersion", 5);
                    svcpackCriticalKeys.Add("MinorVersion", 2);
                    svcpackCriticalKeys.Add("BuildNumber", 3790);
                }
                else if ((_sourceInfo.SourceVersion == WindowsType._XP)
                    && _sourceInfo.Arch == TargetArchitecture.x86)
                {
                    svcpackCriticalKeys.Add("MajorVersion", 5);
                    svcpackCriticalKeys.Add("MinorVersion", 1);
                    svcpackCriticalKeys.Add("BuildNumber", 2600);
                }

                foreach (KeyValuePair<string, int> pair in svcpackCriticalKeys)
                {
                    if (!this._svcpackInfEditor.KeyExists("Version", pair.Key))
                    {
                        this._svcpackInfEditor.Add("Version", pair.Key,
                            pair.Value.ToString(), 
                            IniParser.KeyExistsPolicy.Discard);
                    }
                }

                // Init SVCPACK dictionary
                this._filesToCompressInSvcpack
                    = new Dictionary<string, string>(
                        svcpackData.Count, StringComparer.OrdinalIgnoreCase
                    );

                foreach (string[] catalogPair in svcpackData)
                {
                    if (catalogPair.Length > 1 && !String.IsNullOrEmpty(catalogPair[1]))
                    {
                        this._filesToCompressInSvcpack.Add(
                            catalogPair[0], catalogPair[1]);
                    }
                    else
                    {
                        this._filesToCompressInSvcpack.Add(
                            catalogPair[0], catalogPair[0]);
                    }
                }

                this._svcpackInfEditor.Add(
                    "ProductCatalogsToInstall",
                    this._filesToCompressInSvcpack.Keys,
                    false, false);
            }

            CancelOpportunity();

            this.AnnounceOperation("Generating file list...");
            try
            {
                string wmp11ExtSect
                    = this._wmp11ExtInfEditor.ReadCsvLines("Optional Components")[0][0];
                List<string> copyfilesSects
                    = this._wmp11ExtInfEditor.ReadAllValues(wmp11ExtSect, "CopyFiles");
                this._filesToCompressInCab = new Dictionary<string, string>(10,
                    StringComparer.OrdinalIgnoreCase);

                foreach (string copyfilesSect in copyfilesSects)
                {
                    List<string[]> files 
                        = this._wmp11ExtInfEditor.ReadCsvLines(copyfilesSect, 4);
                    foreach (string[] fileComponents in files)
                    {
                        int flags;
                        if (!String.IsNullOrEmpty(fileComponents[3]) &&
                            !int.TryParse(fileComponents[3], 
                            System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.CurrentCulture,
                            out flags))
                        {
                            throw new Exceptions.IntegrationException(
                                String.Format("Invalid syntax in [{0}]: ({1}).",
                                copyfilesSect, fileComponents)
                            );
                        }
                        else
                        {
                            string destName = fileComponents[0];
                            string srcName = fileComponents[1];

                            // Rare case when parser encounters ,,,, or something 
                            // like that. Just continue on since a line like 
                            // that would have no meaning.
                            if (String.IsNullOrEmpty(destName))
                                continue;

                            // If no destination name, then file is not renamed
                            // during INF FileCopy operations
                            if (String.IsNullOrEmpty(srcName))
                                srcName = destName;

                            if (this._filesToCompressInCab.ContainsKey(srcName))
                                this._filesToCompressInCab[srcName] = destName;
                            else
                                this._filesToCompressInCab.Add(srcName, destName);
                        }
                    }
                }

                // HACK Fix some xml files overwriting each other due to same short name but diff folders on x32
                if (this._sourceInfo.Arch == TargetArchitecture.x86)
                {
                    this._filesToCompressInCab["connecti.xml"]
                        = "connectionmanager_stub.xml";
                    this._filesToCompressInCab["contentd.xml"]
                        = "contentdirectory_stub.xml";
                    this._filesToCompressInCab["mediarec.xml"]
                        = "mediareceiverregistrar_stub.xml";
                }

                switch (this._addonTypeIndex)
                {
                    case 0:
                        // Removing Tweaks.AddReg sections to get vanilla
                        string[] sectionsToProcess;
                        if (this._sourceInfo.Arch == TargetArchitecture.x86)
                        {
                            sectionsToProcess = new string[] {
                                "InstallWMP7", "InstallWMP7.Reg"
                            };
                        }
                        else
                        {
                            sectionsToProcess = new string[] {
                                "PerUserStub"
                            };
                        }

                        const string wmpShortcutsSect = "WMP11.Shortcuts";
                        const string tweaksSect = "Tweaks.AddReg";
                        const string addRegDirective = "AddReg";

                        foreach (string wmp11Sect in sectionsToProcess)
                        {
                            bool result1 = this._wmp11InfEditor.Remove(wmp11Sect,
                                addRegDirective, tweaksSect);
                            Debug.Assert(result1, String.Format(
                                "wmp.inf doesn't have reference to \"{0}\" in \"{1}\" of [{0}]", 
                                tweaksSect, addRegDirective, wmp11Sect));
                        }
                        bool result2 = this._wmp11InfEditor.Remove(tweaksSect);
                        Debug.Assert(result2, String.Format(
                            "wmp.inf doesn't have a [{0}] section.", tweaksSect));

                        // Remove quick launch and desktop icon stuff
                        string[] removeSources = null;
                        if (this._sourceInfo.Arch == TargetArchitecture.x86)
                            removeSources = new string[] { "PerUserStub" };
                        else if (this._sourceInfo.Arch == TargetArchitecture.x64)
                            removeSources = sectionsToProcess;
                        foreach (string wmp11Sect in removeSources)
                        {
                            result2 = this._wmp11InfEditor.Remove(wmp11Sect,
                                addRegDirective,
                                wmpShortcutsSect
                            );
                            Debug.Assert(result2, String.Format(
                                "wmp.inf does not have a reference to \"{0}\" in \"{1}\" of [{0}]",
                                wmpShortcutsSect, addRegDirective, wmp11Sect));
                        }
                        result2 = this._wmp11InfEditor.Remove(wmpShortcutsSect);
                        Debug.Assert(result2, String.Format(
                            "wmp.inf doesn't have a [{0}] section.", wmpShortcutsSect));
                        break;

                    case 1:
                        // Removing WGA files to make Tweaked version
                        bool result3 = _wmp11ExtInfEditor.RemoveLine("SourceDisksFiles",
                            "LegitLibM.dll");
                        Debug.Assert(result3, 
                            "Wmp11Ext embedded doesn't have LegitLibM.dll in [SourceDisksFiles] for vanilla.");
                        result3 = _wmp11ExtInfEditor.RemoveLine("WMPlayer.Copy",
                            "LegitLibM.dll");
                        Debug.Assert(result3,
                            "Wmp11Ext embedded doesn't have LegitLibM.dll in [WMPlayer.Copy] for vanilla.");
                        this._filesToCompressInCab.Remove("LegitLibM.dll");
                        break;
                }
            }
            catch (Exception ex)
            {
                throw new Exceptions.IntegrationException(
                    "An error occurred while generating the file list.",
                    ex
                );
            }

            CancelOpportunity();

            // Figure out the external cab filename from the external inf
            if (this._sourceInfo.Arch == TargetArchitecture.x86)
                this._externalCabFilename = this._wmp11ExtInfEditor.ReadAllValues(
                    "SourceDisksNames.x86", "1")[1];
            else if (this._sourceInfo.Arch == TargetArchitecture.x64)
                this._externalCabFilename = this._wmp11ExtInfEditor.ReadAllValues(
                    "SourceDisksNames.amd64", "1")[1];

            CancelOpportunity();
            this.IncrementGlobalProgress();
        }

        void ResolveDestinationDirIdConflicts(string txtsetupFilesSection, 
            string txtsetupDirSection)
        {
            CSVParser csvParser = new CSVParser();

            int biggestNumber = 0;
            Dictionary<int, string> usedTxtSetupDirs = new Dictionary<int, string>();
            foreach (KeyValuePair<string, List<string>> winntDirPair
                in _txtsetupSifEditor.GetRef("WinntDirectories"))
            {
                int number;
                if (int.TryParse(winntDirPair.Key, out number))
                {
                    if (!usedTxtSetupDirs.ContainsKey(number))
                    {
                        if (number > biggestNumber) biggestNumber = number;
                        usedTxtSetupDirs.Add(number,
                            csvParser.Join(winntDirPair.Value, true));
                    }
                    else
                    {
                        throw new Exceptions.IntegrationException(
                            string.Format(
                            "DirId: \"{0}\" is defined more than once in \"{1}\"",
                            number, this._txtsetupSifEditor.IniFileInfo.Name)
                            );
                    }
                }
                else
                {
                    throw new Exceptions.IntegrationException(
                        String.Format(
                        "Invalid key in [WinntDirectories] section in \"{0}\": \"{1}\"",
                        winntDirPair.Key, this._txtsetupSifEditor.IniFileInfo.Name));
                }
            }

            OrderedDictionary<string, List<string>> txtsetupFilesRef
                = this._entriesCombinedEditor.GetRef(txtsetupFilesSection);
            Dictionary<int, int> renameDestDirDictionary = null;

            CancelOpportunity();

            if (txtsetupDirSection != null)
            {
                this.AnnounceOperation("Fixing [txtsetup_dirs] section...");
                OrderedDictionary<string, List<string>> txtsetupDirsRef 
                    = this._entriesCombinedEditor.GetRef(txtsetupDirSection);
                renameDestDirDictionary = new Dictionary<int, int>(5);

                // Fix added winnt directories to prevent conflicts
                foreach (KeyValuePair<string, List<string>> txtDir
                    in txtsetupDirsRef)
                {
                    string txtDirVal = csvParser.Join(txtDir.Value, true);

                    int number;
                    if (int.TryParse(txtDir.Key, out number))
                    {
                        #region Search by value
                        bool foundByValue = false;
                        foreach (KeyValuePair<int, string> usedTxtDir
                            in usedTxtSetupDirs)
                        {
                            if (CM.SEqCC(usedTxtDir.Value, txtDirVal, true))
                            {
                                renameDestDirDictionary.Add(number, usedTxtDir.Key);
                                ChangeEntriesTxtsetupDirKey(txtsetupDirSection,
                                    txtDir, usedTxtDir.Key);
                                foundByValue = true;
                                break;
                            }
                        }
                        if (foundByValue) continue;
                        #endregion

                        #region Search by key
                        if (usedTxtSetupDirs.ContainsKey(number))
                        {
                            if (CM.SEqCC(usedTxtSetupDirs[number],
                                txtDirVal, true))
                            {
                                // Same key detected with same value, 
                                // continue with others
                                continue;
                            }
                            else
                            {
                                // Conflict detected, set its number to the next
                                // biggest number and rename the txtsetup files entries
                                // related to it.
                                renameDestDirDictionary.Add(number, ++biggestNumber);
                                ChangeEntriesTxtsetupDirKey(txtsetupDirSection,
                                    txtDir, biggestNumber);
                                continue;
                            }
                        }
                        else
                        {
                            // The one to add is not referenced before, add it
                            if (number > biggestNumber) biggestNumber = number;
                            usedTxtSetupDirs.Add(number, txtDirVal);
                        }
                        #endregion
                    }
                    else
                    {
                        throw new Exceptions.IntegrationException(
                            string.Format(
                            "Invalid key in [txtsetup_dirs] section in \"{0}\": [{1}].",
                            txtDir.Key, this._entriesCombinedEditor.IniFileInfo.Name
                            )
                        );
                    }
                }

                if (renameDestDirDictionary.Count > 0)
                {
                    // Fix destdir parameter in txtsetup_files syntax
                    foreach (KeyValuePair<string, List<string>> txtFile 
                        in txtsetupFilesRef)
                    {
                        string location = 
                            (txtFile.Value.Count > 7)? txtFile.Value[7] : String.Empty;

                        if (location.Length > 0)
                        {
                            int currentDestDir;
                            if (int.TryParse(location, out currentDestDir))
                            {
                                if (renameDestDirDictionary.ContainsKey(
                                    currentDestDir))
                                {
                                    txtFile.Value[7] = renameDestDirDictionary[
                                        currentDestDir].ToString();
                                }
                            }
                        }
                    }
                }
            }
        }

        void ChangeEntriesTxtsetupDirKey(
            string txtsetupDirSection, KeyValuePair<string, List<string>> txtDir, 
            int newDirNumber)
        {
            CSVParser csvParser = new CSVParser();

            // Fix the [txtsetup_dirs] entry
            HelperConsole.InfoWriteLine(String.Format(
                "Mapping DirId: {0} -> {1}; Original: [{2} = {3}]",
                txtDir.Key, newDirNumber,
                txtDir.Key, csvParser.Join(txtDir.Value)));

            if (!this._entriesCombinedEditor.TryChangeKey(
                txtsetupDirSection,
                txtDir.Key,
                newDirNumber.ToString(),
                txtDir.Value
             ))
            {
                throw new Exceptions.IntegrationException(
                    String.Format(
                    "Unable to modify the key for this [txtsetup_dirs] "
                        + "entry in \"{0}\": [{1} = {2}].",
                        this._entriesCombinedEditor.IniFileInfo.Name, txtDir.Key, 
                        csvParser.Join(txtDir.Value, true)));
            }
        }

        void ApplyFixes()
        {
            // Check for DLL versions (msobmain.dll, uxtheme.dll)
            this.AnnounceOperation("Preparing fixes...");
            string fixesFolder = this.CombinePathComponents(_workingDirectory, "Fixes");
            string tempCompareFolder = this.CombinePathComponents(_workingDirectory, 
                "FixesCompare");
            string fixesCab = this.CombinePathComponents(fixesFolder, "fixes.cab");

            // Apply hotfixes
            this.AnnounceOperation("Extracting and applying hotfixes...");

            // Make the 2 folders
            Directory.CreateDirectory(fixesFolder);
            Directory.CreateDirectory(tempCompareFolder);

            // Normal hotfix file list dictionaries
            Dictionary<string, IEnumerable<string>> hotfixFileDictionary
                = new Dictionary<string, IEnumerable<string>>(
                    _hotfixInstallerList.Count, StringComparer.OrdinalIgnoreCase);

            // Mammoth hotfix apply functions
            StandardHotfixApply(fixesFolder, hotfixFileDictionary);

            // Get rid of superseded fixes by adding the hotfixes
            // listed in hotfixFileDictionary values only
            if (!this._ignoreCats)
            {
                foreach (IEnumerable<string> hotfixList in hotfixFileDictionary.Values)
                {
                    foreach (string hotfixRelPath in hotfixList)
                    {
                        AddSvcpackCatalog(hotfixRelPath, fixesFolder);
                    }
                }
            }

            // MCE requires KB913800 even with WMP11 installed
            if (this._sourceInfo.SourceVersion == WindowsType._XP
                && this._sourceInfo.Edition == WindowsEdition.MediaCenter
                && this._sourceInfo.Arch == TargetArchitecture.x86)
            {
                this.IntegrateKB913800(fixesFolder);
            }

            // HACK Special treatment for KB926239 - Acadproc
            if (_sourceInfo.SourceVersion == WindowsType._XP
                && _sourceInfo.Arch == TargetArchitecture.x86)
            {
                ProcessKB926239(tempCompareFolder,
                    this.CombinePathComponents(_extractedDirectory, "Update"));
            }

            // Check Uxtheme.dll and Msobmain.dll versions
            if (this._sourceInfo.SourceVersion == WindowsType._XP
                && this._sourceInfo.Arch == TargetArchitecture.x86)
            {
                Version minUxthemeVer = new Version(6, 0, 2900, 2845);
                Version minMsobmainVer = new Version(5, 1, 2600, 2659);

                if (CopyOrExpandFromArch("uxtheme.dll", tempCompareFolder, true))
                {
                    string uxthemeComparePath = 
                        this.CombinePathComponents(tempCompareFolder, "uxtheme.dll");
                    FileVersionInfo sourceUxthemeFileVer 
                        = FileVersionInfo.GetVersionInfo(uxthemeComparePath);
                    Version sourceUxthemeVer = new Version(
                        sourceUxthemeFileVer.FileMajorPart,
                        sourceUxthemeFileVer.FileMinorPart,
                        sourceUxthemeFileVer.FilePrivatePart,
                        sourceUxthemeFileVer.FileBuildPart
                    );

                    if (minUxthemeVer.CompareTo(sourceUxthemeVer) > 0)
                    {
                        this.ShowMessage(
                            "Please integrate KB908536 after this process is finished,"
                            + " otherwise the \"Themes\" service will not start after"
                            + " installing Windows from this source.",
                            "\"Themes\" service warning", MessageEventType.Warning);
                    }

                    FileSystem.Delete(uxthemeComparePath);
                }

                if (CopyOrExpandFromArch("msobmain.dll", tempCompareFolder, true))
                {
                    string msobmainComparePath = this.CombinePathComponents(
                        tempCompareFolder, "msobmain.dll");
                    FileVersionInfo sourceMsobmainFileVer 
                        = FileVersionInfo.GetVersionInfo(msobmainComparePath);
                    Version sourceMsobmainVer = new Version(
                        sourceMsobmainFileVer.FileMajorPart,
                        sourceMsobmainFileVer.FileMinorPart,
                        sourceMsobmainFileVer.FilePrivatePart,
                        sourceMsobmainFileVer.FileBuildPart
                    );

                    if (minMsobmainVer.CompareTo(sourceMsobmainVer) > 0)
                    {
                        this.ShowMessage(
                            "Please integrate KB894871 after this process is finished,"
                            + " otherwise the OOBE (out-of-box-experience) wizard will"
                            + " not launch on first boot after installing Windows from"
                            + " this source.",
                            "\"Themes\" service warning", MessageEventType.Warning);
                    }

                    FileSystem.Delete(msobmainComparePath);
                }
            }

            // Hide progress bar
            this.HideCurrentProgress();
        }

        void StandardHotfixApply(string fixesFolder, 
            Dictionary<string, IEnumerable<string>> hotfixFileDictionary)
        {
            #region Standard hotfix extract routine
            ProgressTracker hfixExtractProgress = new ProgressTracker(
                _hotfixInstallerList.Count);
            this.ResetCurrentProgress();

            foreach (string hotfix in this._hotfixInstallerList)
            {
                try
                {
                    NativeExtractHotfix(hotfix, fixesFolder);
                    if (!HotfixMatchesArch(fixesFolder))
                    {
                        throw new Exceptions.IntegrationException(
                            String.Format("The hotfix you are trying to slipstream ({0}) "
                            + "is not designed for your target source's CPU architecture.\n"
                            + "Make sure both are 32-bit or x64 versions.",
                            Path.GetFileName(hotfix)));
                    }
                    CancelOpportunity();

                    // Processing Update.inf
                    IniParser updateInfEditor;
                    string updateInfPath = this.CombinePathComponents(fixesFolder,
                        "Update", "Update.inf");
                    string updateQfeInfPath = this.CombinePathComponents(fixesFolder,
                        "Update", "Update_SP2QFE.inf");

                    if (File.Exists(updateInfPath))
                    {
                        updateInfEditor = new IniParser(updateInfPath, true);
                    }
                    else if (File.Exists(updateQfeInfPath))
                    {
                        updateInfEditor = new IniParser(updateQfeInfPath, true);
                    }
                    else
                    {
                        throw new FileNotFoundException(String.Format(
                            "Unable to find a suitable information file in \"{0}\".",
                            Path.GetFileName(hotfix)));
                    }

                    // Hotfix Update.inf processor condition evaluator
                    HotfixParserEvaluator evaluator = new HotfixParserEvaluator(
                        this._extractedDirectory, this._sourceInfo);

                    // Hotfix Update.inf processor
                    HotfixInfParser hotfixParser = new HotfixInfParser(updateInfEditor,
                        evaluator.EvaluateCondition, this._sourceInfo);

                    // HACK Block WMPAPPCOMPAT from Server 2003, in case someone 
                    // gets smart and tries to integrate it by itself
                    if (CM.SEqO(hotfixParser.HotfixName, "KB926239", true)
                        && _sourceInfo.SourceVersion == WindowsType._Server2003)
                    {
                        this.IncrementCurrentProgress(hfixExtractProgress);
                        continue;
                    }

                    foreach (HotfixInfParser.FileListSection fList in hotfixParser.FileList)
                    {
                        foreach (KeyValuePair<string, string> filePair in fList.FileDictionary)
                        {
                            string relativeFilePath = filePair.Key;
                            string fileNameOnly = Path.GetFileName(relativeFilePath);

                            string fixFullPath = this.CombinePathComponents(
                                fixesFolder, relativeFilePath);
                            string orgFullPath = this.CombinePathComponents(
                                this._extractedDirectory, fileNameOnly);

                            // HACK: Try to correct filepaths for x64 WMP11 
                            // and x64 hotfixes, Is64Bit32BitFileList will only
                            // be true on a x64 hotfix
                            if (fList.Is64Bit32BitFileList)
                            {
                                PeEditor peReader = new PeEditor(orgFullPath);
                                if (peReader.TargetMachineType == Architecture.x64)
                                {
                                    // Not sure if I should use relativeFilePath here;
                                    // There are no subfolders in amd64 or i386 in extracted
                                    orgFullPath = this.CombinePathComponents(
                                        _extractedDirectory, "i386", fileNameOnly);
                                }
                            }
                            else
                            {
                                PeEditor peReaderOrg = new PeEditor(orgFullPath);
                                PeEditor peReaderNew = new PeEditor(fixFullPath);
                                if (peReaderOrg.TargetMachineType == Architecture.x86
                                    && peReaderNew.TargetMachineType == Architecture.x64)
                                {
                                    // Not sure if I should use relativeFilePath here;
                                    // There are no subfolders in amd64 or i386 in extracted
                                    orgFullPath = this.CombinePathComponents(
                                        _extractedDirectory, "amd64", fileNameOnly);
                                }
                            }

                            // Bug reported here: Sometimes fixFullPath doesn't exist
                            // and CompareVersions throws FileNotFoundException
                            FileVersionComparison result
                                = CM.CompareVersions(fixFullPath,
                                orgFullPath);
                            if (result == FileVersionComparison.Newer)
                            {
                                if (hotfixFileDictionary.ContainsKey(fileNameOnly))
                                {
                                    hotfixFileDictionary[fileNameOnly]
                                        = hotfixParser.Catalogs.Values;
                                }
                                else
                                {
                                    hotfixFileDictionary.Add(fileNameOnly,
                                        hotfixParser.Catalogs.Values);
                                }
                                FileSystem.MoveFileOverwrite(
                                    fixFullPath, orgFullPath);
                            }
                            else if (result == FileVersionComparison.Older)
                            {
                                FileSystem.Delete(fixFullPath);
                            }
                            else if (result == FileVersionComparison.NotFound)
                            {
                                throw new InvalidOperationException(
                                    String.Format(
                                    "Attempt to integrate a non-WMP11 hotfix. Offending file: \"{0}\", belongs to hotfix {1}{2}{2}You may need an updated version of this program to properly integrate this hotfix.",
                                    relativeFilePath, hotfixParser.HotfixName,
                                    Environment.NewLine)
                                );
                            }
                        }
                    }
                    this.IncrementCurrentProgress(hfixExtractProgress);
                }
                catch (Exception ex)
                {
                    ex.Data.Add("Offending hotfix", Path.GetFileName(hotfix));
                    throw ex;
                }
            #endregion
            }

        }

        void SaveFiles()
        {
            this.AnnounceOperation("Saving edited files...");
            FileSystem.UnsetReadonly(this._wmp11ExtInfEditor.IniFileInfo);
            FileSystem.UnsetReadonly(this._wmp11InfEditor.IniFileInfo);
            FileSystem.UnsetReadonly(this._txtsetupSifEditor.IniFileInfo);
            FileSystem.UnsetReadonly(this._dosnetInfEditor.IniFileInfo);
            FileSystem.UnsetReadonly(this._sysocInfEditor.IniFileInfo);
            FileSystem.UnsetReadonly(this._svcpackInfEditor.IniFileInfo);

            this._wmp11ExtInfEditor.SaveIni();
            this._wmp11InfEditor.SaveIni();
            this._txtsetupSifEditor.SaveIni();
            this._dosnetInfEditor.SaveIni();
            this._sysocInfEditor.SaveIni();
            this._svcpackInfEditor.SaveIni();
            this.IncrementGlobalProgress();
        }

        void CompressFiles()
        {
            // Compress External INF
            this.AnnounceOperation(String.Format("Compressing \"{0}\"...", 
                this._externalInfFilename));
            Archival.NativeCabinetMakeCab(
                this.CombinePathComponents(
                this._workingDirectory, this._externalInfFilename),
                this._workingDirectory);
            File.Delete(this.CombinePathComponents(
                this._workingDirectory, this._externalInfFilename));

            // Insert the custom icon
            if (this._customIcon != null)
            {
                this.AnnounceOperation("Applying custom icon...");
                ResourceEditor resEdit = new ResourceEditor(
                    this.CombinePathComponents(this._extractedDirectory, "wmplayer.exe"));
                resEdit.ReplaceMainIcon(this._customIcon);
                resEdit.Close();
                PeEditor editor = new PeEditor(this.CombinePathComponents(
                    this._extractedDirectory, "wmplayer.exe"));
                editor.RecalculateChecksum();
            }

            // Create external CAB
            this.AnnounceOperation(String.Format("Creating \"{0}\"...", 
                this._externalCabFilename));
            string wmp11cabdirectory = this.CombinePathComponents(
                this._workingDirectory, "wmp11cab");
            Directory.CreateDirectory(wmp11cabdirectory);
            CancelOpportunity();
            foreach (KeyValuePair<string, string> pair in this._filesToCompressInCab)
            {
                string filenameInExtracted = this.CombinePathComponents(
                    this._extractedDirectory, pair.Key);
                string filenameToBeRenamedInExtracted = this.CombinePathComponents(
                    this._extractedDirectory, pair.Value);
                string destinationPath = this.CombinePathComponents(
                    wmp11cabdirectory, pair.Key);

                if (File.Exists(filenameToBeRenamedInExtracted))
                {
                    FileSystem.CopyFile(filenameToBeRenamedInExtracted, destinationPath);
                    continue;
                }
                else if (File.Exists(filenameInExtracted))
                {
                    FileSystem.CopyFile(filenameInExtracted, destinationPath);
                    continue;
                }
                else
                    throw new FileNotFoundException(string.Format(
                        "File: \"{0}\" ({1}) not found.", pair.Key, pair.Value), 
                        pair.Key);
            }

            this.ResetCurrentProgress();

#if DEBUG
            DateTime before = DateTime.Now;
#endif
            Archival.NativeCabinetCreate(this.CombinePathComponents(
                this._workingDirectory, this._externalCabFilename),
                wmp11cabdirectory, 
                true, FCI.CompressionLevel.Lzx21, null,
                this.UpdateCurrentProgress);
#if DEBUG
            DateTime after = DateTime.Now;
            TimeSpan timeTaken = after - before;
            this.OnDebuggingMessage(timeTaken.ToString(), "Time Taken");
#endif
            this.HideCurrentProgress();
            this.IncrementGlobalProgress();

            // Compress all i386 files
            this.AnnounceOperation("Compressing added files...");

            // Hack for wpdshextres.dll.409
            File.Move(this.CombinePathComponents(
                _extractedDirectory, "locbin", "wpdshextres.dll.409"),
                this.CombinePathComponents(_extractedDirectory, "wpdshextres.dll")
            );

            // Remove CAB filename from all lists to prevent
            // FileNotFoundException from occuring
            _filesToCompressInArch.Remove(_externalCabFilename);
            _filesToCopyInArch.Remove(_externalCabFilename);

            int totalFileCount = _filesToCompressInArch.Count + _filesToCopyInArch.Count;
            if (this._filesToCompressInI386 != null)
                totalFileCount += this._filesToCompressInI386.Count;

            this.ResetCurrentProgress();
            ProgressTracker compProgress = new ProgressTracker(totalFileCount);

            foreach (KeyValuePair<string, string> pair in _filesToCompressInArch)
            {
                RenameAndCompressArchFile(pair.Key, pair.Value, 
                    this._sourceInfo.Arch, true, null);
                this.IncrementCurrentProgress(compProgress);
                this.CancelOpportunity();
            }
            if (this._filesToCompressInI386 != null)
            {
                this._x64I386Folder = this.CombinePathComponents(
                    _workingDirectory, "x64_i386");
                FileSystem.CreateEmptyDirectory(_x64I386Folder);
                foreach (KeyValuePair<string, string> pair in _filesToCompressInI386)
                {
                    RenameAndCompressArchFile(pair.Key, pair.Value, 
                        TargetArchitecture.x86, true, _x64I386Folder);
                    this.IncrementCurrentProgress(compProgress);
                    this.CancelOpportunity();
                }
            }
            foreach (KeyValuePair<string, string> pair in _filesToCopyInArch)
            {
                RenameAndCompressArchFile(pair.Key, pair.Value,
                    this._sourceInfo.Arch, false, null);
                this.IncrementCurrentProgress(compProgress);
                this.CancelOpportunity();
            }
            this.HideCurrentProgress();
            this.IncrementGlobalProgress();

            // Locate files that directly overwrite
            this.AnnounceOperation("Determining overwriting files...");

            // HACK Prevent Windows EULA from being overwritten
            FileSystem.Delete(this.CombinePathComponents(
                this._extractedDirectory, "EULA.TXT"));

            string[] filesInExtracted = Directory.GetFiles(
                this._extractedDirectory, "*", SearchOption.TopDirectoryOnly);
            Dictionary<int, OverwriteFileBehaviour> filesThatOverwrite
                = new Dictionary<int, OverwriteFileBehaviour>(
                filesInExtracted.Length);
            for (int i = 0; i < filesInExtracted.Length; i++)
            {
                string filename = Path.GetFileName(filesInExtracted[i]);
                string compressedFilename = CM.GetCompressedFileName(filename);

                // For standard arch files
                string archName = this.CombinePathComponents(
                    this._archFilesDirectory, filename);
                string archCompressedName = this.CombinePathComponents(
                    this._archFilesDirectory, compressedFilename);

                if (this._sourceInfo.Arch == TargetArchitecture.x64)
                {
                    // For 32-bit files that come with x64 arch, all of them
                    // in the source are prefixed with a "w" and renamed by
                    // Windows Setup when installed to SysWOW64 (via txtsetup.sif)
                    string i386Name = this.CombinePathComponents(
                        this._sourceDirectory, "i386", "w" + filename);
                    string i386CompressedName = CM.GetCompressedFileName(i386Name);

                    // Then the file I have now could be the 32-bit version
                    // and a 64-bit version could exist in an AMD64 subfolder
                    if (File.Exists(i386CompressedName))
                    {
                        filesThatOverwrite.Add(i, 
                            OverwriteFileBehaviour.CompressedPossiblex32x64Combo);
                        continue;
                    }
                    else if (File.Exists(i386Name))
                    {
                        filesThatOverwrite.Add(i,
                            OverwriteFileBehaviour.UncompressedPossiblex32x64Combo);
                        continue;
                    }
                }

                // Otherwise if we didn't "continue" the loop from
                // the x64 "if statement" above then we have a standard
                // x64 file in the extracted root (or standard x32 
                // for x32 arch)

                // Note: To cater for x64 in extracted root
                // without repeating code, I used "if" and not "else if"
                // and continue the loop manually if x64/x32 combo found.

                if (File.Exists(archCompressedName))
                    filesThatOverwrite.Add(i, 
                        OverwriteFileBehaviour.CompressedStandardArch);
                else if (File.Exists(archName))
                    filesThatOverwrite.Add(i,
                        OverwriteFileBehaviour.UncompressedStandardArch);
            }

            // Compress directly overwriting files
            this.AnnounceOperation("Compressing overwriting files...");
            this.ResetCurrentProgress();
            compProgress = new ProgressTracker(filesThatOverwrite.Count);
            foreach (KeyValuePair<int, OverwriteFileBehaviour> 
                pair in filesThatOverwrite)
            {
                string currentFilePath = filesInExtracted[pair.Key];
                string currentFileName = Path.GetFileName(currentFilePath);

                string amd64File = this.CombinePathComponents(this._extractedDirectory,
                    "AMD64", currentFileName);

                switch (pair.Value)
                {
                    case OverwriteFileBehaviour.UncompressedStandardArch:
                        FileSystem.CopyFile(currentFilePath,
                            this.CombinePathComponents(_workingDirectory, currentFileName));
                        break;

                    case OverwriteFileBehaviour.CompressedStandardArch:
                        Archival.NativeCabinetMakeCab(
                            currentFilePath,
                            this._workingDirectory
                        );
                        break;

                    case OverwriteFileBehaviour.UncompressedPossiblex32x64Combo:
                        FileSystem.CopyFile(currentFilePath, this.CombinePathComponents(
                            this._x64I386Folder, "w" + currentFileName));
                        if (File.Exists(amd64File))
                        {
                            FileSystem.CopyFile(amd64File,
                                this.CombinePathComponents(this._workingDirectory, 
                                currentFileName));
                        }
                        break;

                    case OverwriteFileBehaviour.CompressedPossiblex32x64Combo:
                        string x32FileName = 
                            this.CombinePathComponents(
                            Path.GetDirectoryName(currentFilePath), "w" + currentFileName);
                        File.Move(currentFilePath, x32FileName);
                        Archival.NativeCabinetMakeCab(
                            x32FileName, this._x64I386Folder);
                        if (File.Exists(amd64File))
                        {
                            Archival.NativeCabinetMakeCab(
                                amd64File,
                                this._workingDirectory);
                        }
                        break;
                }
                this.IncrementCurrentProgress(compProgress);
                this.CancelOpportunity();
            }
            this.IncrementGlobalProgress();

            // Svcpack stuff
            if (!this._ignoreCats)
            {
                this.AnnounceOperation("Compressing security catalogs...");
                this.ResetCurrentProgress();
                compProgress = new ProgressTracker(_filesToCompressInSvcpack.Count);
                string svcpackTempFolder = this.CombinePathComponents(
                    _workingDirectory, "SVCPACK");
                string svcpackExtractedFolder = this.CombinePathComponents(
                    _extractedDirectory, "Update");
                Directory.CreateDirectory(svcpackTempFolder);
                foreach (KeyValuePair<string, string> pair in _filesToCompressInSvcpack)
                {
                    string shortname = this.CombinePathComponents(
                        svcpackExtractedFolder, pair.Key);
                    string longname = this.CombinePathComponents(
                        svcpackExtractedFolder, pair.Value);
                    if (File.Exists(shortname))
                    {
                        Archival.NativeCabinetMakeCab(
                            shortname,
                            svcpackTempFolder
                        );
                        this.IncrementCurrentProgress(compProgress);
                    }
                    else if (File.Exists(longname))
                    {
                        File.Move(longname, shortname);
                        Archival.NativeCabinetMakeCab(
                            shortname,
                            svcpackTempFolder
                        );
                        this.IncrementCurrentProgress(compProgress);
                    }
                    else
                    {
                        throw new Exceptions.IntegrationException(
                                String.Format(
                                "The file \"{0}\" ({1}) is not present in the svcpack folder.",
                                pair.Key, pair.Value
                                )
                            );
                    }

                    this.CancelOpportunity();
                }
            }

            this.HideCurrentProgress();
            this.IncrementGlobalProgress();

            // 2k3/x64 repack DRIVER.CAB
            if (_sourceInfo.SourceVersion == WindowsType._Server2003
                || _sourceInfo.Arch == TargetArchitecture.x64)
            {
                Debug.Assert(Directory.Exists(_drivercabExtractedDirectory));
                this.AnnounceOperation("Replacing files in driver cabinet...");
                List<string[]> filesToCopy = null;
                if (_sourceInfo.SourceVersion == WindowsType._Server2003
                    && _sourceInfo.Arch == TargetArchitecture.x86)
                    filesToCopy = this._entriesCombinedEditor.ReadCsvLines(
                        "2k3_drivercab_expand");
                else if (_sourceInfo.Arch == TargetArchitecture.x64)
                    filesToCopy = this._entriesCombinedEditor.ReadCsvLines(
                        "xp_x64_drivercab_expand");
                foreach (string[] fileComponents in filesToCopy)
                {
                    string filename = fileComponents[0];
                    string compressedFile = CM.GetCompressedFileName(filename);
                    string driverFolderFileName =
                        this.CombinePathComponents(this._drivercabExtractedDirectory, 
                        filename);
                    CancelOpportunity();
                    if (this._sourceInfo.Arch == TargetArchitecture.x64)
                    {
                        FileSystem.CopyFile(this.CombinePathComponents(
                            _extractedDirectory, "AMD64", filename),
                            driverFolderFileName, true);
                    }
                    else if (this._sourceInfo.Arch == TargetArchitecture.x86)
                    {
                        FileSystem.CopyFile(this.CombinePathComponents(
                            this._extractedDirectory, filename), 
                            driverFolderFileName, true);
                    }
                    FileVersionComparison compareResult
                        = CM.CompareVersions(driverFolderFileName,
                        Path.GetTempPath() + fileComponents);
                    if (compareResult == FileVersionComparison.Older)
                    {
                        if (File.Exists(driverFolderFileName))
                        {
                            File.Delete(driverFolderFileName);
                        }
                        File.Move(Path.GetTempPath() + fileComponents,
                            driverFolderFileName);
                    }
                    else
                    {
                        File.Delete(Path.GetTempPath() + fileComponents);
                    }
                }
                this.AnnounceOperation(
                    String.Format(
                    "Repacking \"{0}\" cabinet. Please wait...",
                    Path.GetFileName(_driverCabFile)));
                int numberOfFiles
                    = Directory.GetFiles(_drivercabExtractedDirectory).Length;
                this.ResetCurrentProgress();
                CancelOpportunity();
                Archival.NativeCabinetCreate(this.CombinePathComponents(
                    _workingDirectory, _driverCabFile),
                    _drivercabExtractedDirectory,
                    false,
                    FCI.CompressionLevel.Lzx21,
                    null,
                    this.UpdateCurrentProgress
                );
                this.HideCurrentProgress();
            }
            this.IncrementGlobalProgress();

            this.AnnounceOperation("Compressing edited files...");
            foreach (string file in _possCompArchFile)
            {
                string compressedFile = CM.GetCompressedFileName(file);
                if (File.Exists(this.CombinePathComponents(
                    _archFilesDirectory, compressedFile)))
                {
                    Archival.NativeCabinetMakeCab(
                        this.CombinePathComponents(_workingDirectory, file),
                        _workingDirectory
                    );
                    File.Delete(this.CombinePathComponents(_workingDirectory, file));
                    CancelOpportunity();
                }
            }
            this.IncrementGlobalProgress();
            CancelOpportunity();
        }

        /// <summary>
        /// Compresses and renames files that are in extractedDirectory,
        /// allows to check and prioritise files that are in a certain subfolder.
        /// </summary>
        /// <param name="sourceName">Name of file that will go in arch folder</param>
        /// <param name="destinationFolder">Name of file on the hard drive 
        /// when installed by Windows Setup</param>
        /// <param name="architecture">Architecture of file</param>
        /// <param name="compress">true to makecab</param>
        /// <param name="destinationName">Custom folder to place result in, 
        /// null for default (which is this.WorkingDirectory)</param>
        void RenameAndCompressArchFile(string sourceName, 
            string destinationName, TargetArchitecture architecture, 
            bool compress, string destinationFolder)
        {
            string possibleSubfolder = null;
            if (String.IsNullOrEmpty(destinationFolder))
                destinationFolder = this._workingDirectory;
            switch (architecture)
            {
                case TargetArchitecture.x86:
                    possibleSubfolder = "i386";
                    break;

                case TargetArchitecture.x64:
                    possibleSubfolder = "amd64";
                    break;
            }

            string filenameInExtracted
                = this.CombinePathComponents(_extractedDirectory, sourceName);
            string filenameToBeRenamedInExtracted
                = this.CombinePathComponents(_extractedDirectory, destinationName);
            string filenameInSubFolder 
                = this.CombinePathComponents(_extractedDirectory, possibleSubfolder, 
                sourceName);
            string filenameToBeRenamedInSubFolder
                = this.CombinePathComponents(_extractedDirectory, possibleSubfolder,
                destinationName);

            if (File.Exists(filenameToBeRenamedInSubFolder))
            {
                File.Move(filenameToBeRenamedInSubFolder,
                    filenameInSubFolder);
                filenameInExtracted = filenameInSubFolder;
            }
            else if (File.Exists(filenameInSubFolder))
            {
                filenameInExtracted = filenameInSubFolder;
            }
            else if (File.Exists(filenameToBeRenamedInExtracted))
            {
                File.Move(filenameToBeRenamedInExtracted,
                    filenameInExtracted);
            }

            // This should throw an exception if it or its renamed form
            // do not exist in the extracted or i386 or amd64
            if (compress)
            {
                Archival.NativeCabinetMakeCab(
                    filenameInExtracted,
                    destinationFolder
                );
                FileSystem.Delete(filenameInExtracted);
            }
            else
            {
                File.Move(
                    filenameInExtracted,
                    this.CombinePathComponents(destinationFolder, sourceName)
                );
            }
        }

        /// <summary>
        /// Adds a svcpack catalog to the catalog list in SVCPACK inf
        /// </summary>
        void AddSvcpackCatalog(string catalogRelativePath, string fixesFolder)
        {
            string catalogName = Path.GetFileName(catalogRelativePath);
            string catFullPath = this.CombinePathComponents(fixesFolder, 
                catalogRelativePath);
            string destCatPath = this.CombinePathComponents(
                this._extractedDirectory, "Update", catalogName);
            if (!this._filesToCompressInSvcpack.ContainsKey(catalogName))
            {
                this._filesToCompressInSvcpack.Add(catalogName, catalogName);
            }
            if (!File.Exists(destCatPath))
            {
                File.Move(catFullPath, destCatPath);
            }
            this._svcpackInfEditor.Add("ProductCatalogsToInstall", 
                catalogName, false);
        }

        /*
        static void UxthemePatchNewIfOld(string oldUxthemePath, 
            string newUxthemePath)
        {
            // Get version of new uxtheme
            int i386Version = FileVersionInfo.GetVersionInfo(
                newUxthemePath).FilePrivatePart;

            // Patch array
            string[] UxThemePatchList = new string[]
            { 
                @"2180|113178|83EC1C568D4DE4|33C0C9C2040090",
                @"2523|104714|83EC1C568D4DE4|33C0C9C2040090"
            };

            // Check if UxTheme is patched or not
            string uxthemePatchLine = null;
            foreach (string line in UxThemePatchList)
            {
                string[] data = line.Split(new char[] { '|' });
                int version = int.Parse(data[0]);
                if (i386Version == version)
                {
                    uxthemePatchLine = line;
                    break;
                }
            }
            if (String.IsNullOrEmpty(uxthemePatchLine))
            {
                throw new Exceptions.IntegrationException(
                    "Unable to locate appropriate patch line for UxTheme.dll. "
                    + "This is definitely a bug, please report it."
                    );
            }
            else
            {
                string[] data = uxthemePatchLine.Split(new char[] { '|' });
                int offset = int.Parse(data[1]);
                string oldByteString = data[2];
                string newByteString = data[3];

                List<byte> oldBytes = null;
                int capacity = newByteString.Length / 2;
                oldBytes = new List<byte>(capacity);
                for (int i = 0; i < oldByteString.Length; i += 2)
                {
                    oldBytes.Add(
                        byte.Parse(oldByteString[i].ToString()
                        + oldByteString[i + 1].ToString(),
                        System.Globalization.NumberStyles.HexNumber
                    )
                    );
                }
                List<byte> newBytes = new List<byte>(capacity);
                for (int i = 0; i < newByteString.Length; i += 2)
                {
                    newBytes.Add(
                        byte.Parse(newByteString[i].ToString()
                        + newByteString[i + 1].ToString(),
                        System.Globalization.NumberStyles.HexNumber
                    )
                    );
                }

                FileStream fstream = new FileStream(oldUxthemePath, FileMode.Open);
                byte[] compareArray = new byte[capacity];
                fstream.Seek(offset, SeekOrigin.Begin);
                fstream.Read(compareArray, 0, capacity);
                fstream.Close();

                bool resultOfCompareOld = ByteArraysAreEqual(
                    compareArray, oldBytes.ToArray());
                bool resultOfCompareNew = ByteArraysAreEqual(
                    compareArray, newBytes.ToArray());

                if (!resultOfCompareOld && !resultOfCompareNew)
                {
                    throw new Exceptions.IntegrationException(
                        "UxTheme.dll in source is corrupted!");
                }
                else if (resultOfCompareOld) return;

                fstream = new FileStream(
                    newUxthemePath,
                    FileMode.Open
                );
                fstream.Seek(offset, SeekOrigin.Begin);
                List<byte> ourNewBytes = new List<byte>(capacity);
                for (int i = 0; i < newByteString.Length; i += 2)
                {
                    ourNewBytes.Add(
                        byte.Parse(newByteString[i].ToString()
                        + newByteString[i + 1].ToString(),
                        System.Globalization.NumberStyles.HexNumber
                    )
                    );
                }
                fstream.Write(ourNewBytes.ToArray(),
                    0, capacity);
                fstream.Flush();
                fstream.Close();

                // Fix the checksum
                PeEditor editor = new PeEditor(newUxthemePath);
                editor.FixChecksum();
            }
        }
         */

        void MergeFolders()
        {
            this.AnnounceOperation("Merging folders...");
            this.BeginCriticalOperation();
            FileSystem.MoveFiles(this._workingDirectory, this._archFilesDirectory, 
                false);
            if (!String.IsNullOrEmpty(this._x64I386Folder)
                && Directory.Exists(this._x64I386Folder))
            {
                FileSystem.MoveFiles(this._x64I386Folder,
                    this.CombinePathComponents(this._sourceDirectory, "i386"), false);
            }
            if (!this._ignoreCats)
            {
                if (!Directory.Exists(this.CombinePathComponents(
                    _archFilesDirectory, "SVCPACK")))
                {
                    Directory.CreateDirectory(this.CombinePathComponents(
                        _archFilesDirectory, "SVCPACK"));
                }
                foreach (string filepath in
                    Directory.GetFiles(this.CombinePathComponents(_workingDirectory,
                        "SVCPACK"), "*.*", SearchOption.TopDirectoryOnly))
                {
                    string filename = Path.GetFileName(filepath);
                    string svcpackname = this.CombinePathComponents(
                        _archFilesDirectory, "SVCPACK", filename);
                    if (File.Exists(svcpackname))
                    {
                        File.SetAttributes(svcpackname, FileAttributes.Normal);
                        File.Delete(svcpackname);
                    }

                    File.Move(filepath, svcpackname);
                }
            }
        }

        void CleanUpFolders()
        {
            this.AnnounceOperation("Cleaning up temporary folders...");
            FileSystem.Delete(this._workingDirectory);
            this.EndCriticalOperation();
        }

        /// <summary>
        /// Signals the backend to abort at the next checkpoint.
        /// Call (obviously) from another thread.
        /// </summary>
        internal void Abort()
        {
            this._aborting = true;
        }

        void NativeExtractHotfix(string hotfixInstaller,
            string destinationPath)
        {
            string tempFolder = FileSystem.GetGuaranteedTempDirectory(
                this._workingDirectory);
            Stream hotfixStream = CM.GetCabStream(hotfixInstaller);
            Archival.NativeCabinetExtract(hotfixStream, tempFolder);

            if (File.Exists(this.CombinePathComponents(tempFolder, "_sfx_manifest_")))
            {
                IniParser manifestEditor = new IniParser(this.CombinePathComponents(
                    tempFolder, "_sfx_manifest_"), true);
                OrderedDictionary<string, List<string>> deltaDict
                    = manifestEditor.GetRef("Deltas");

                foreach (KeyValuePair<string, List<string>> entry in deltaDict)
                {
                    string patchFile = this.CombinePathComponents(
                        tempFolder, entry.Value[0]);

                    // Don't assume that the basis file must always be in the 
                    // same folder as the patches, will search in that and in 
                    // destination as some hotfixes are using files from the 
                    // destination as a basis for some of the patch (_sfx_*) files.
                    string basisFileTemp = this.CombinePathComponents(
                        tempFolder, entry.Value[1]);
                    string basisFileDest = this.CombinePathComponents(
                        destinationPath, entry.Value[1]);
                    string basisFile = null;

                    string destinationFile = this.CombinePathComponents(
                        destinationPath, entry.Key);

                    string destinationSubDir = Path.GetDirectoryName(destinationFile);
                    if (!Directory.Exists(destinationSubDir))
                    {
                        Directory.CreateDirectory(destinationSubDir);
                    }
                    if (File.Exists(destinationFile))
                    {
                        File.SetAttributes(destinationFile,
                            FileAttributes.Normal);
                    }

                    // Check where the basis file is
                    if (File.Exists(basisFileTemp))
                    {
                        basisFile = basisFileTemp;
                    }
                    else
                    {
                        basisFile = basisFileDest;
                    }

                    bool result = Delta.ApplyPatchToFile(patchFile, basisFile,
                        destinationFile, Delta.ApplyOptionFlags.FailIfExact
                        | Delta.ApplyOptionFlags.FailIfClose);
                    if (!result)
                    {
                        throw new Exception(String.Format(
                            "Delta API failed to apply patch \"{0}\" to the file \"{1}\".",
                            Path.GetFileName(patchFile), Path.GetFileName(basisFile)),
                            new System.ComponentModel.Win32Exception());
                    }
                }
            }
            else
            {
                FileSystem.MoveFiles(tempFolder, destinationPath, true);
            }
            FileSystem.Delete(tempFolder);

        }

        void CancelOpportunity()
        {
            if (this._aborting)
            {
                throw new Exceptions.BackendAbortedException();
            }
        }


        void MigrateStringsFromOriginalInf(string infName, string tempFolder,
            IniParser embeddedInfEditor)
        {
            this.CopyOrExpandFromArch(infName, tempFolder, false);
            // WARNING: Malformed lines detection disabled. MS poorly codes their INFs.
            IniParser originalInfEditor = new IniParser(
                this.CombinePathComponents(tempFolder, infName), false);
            OrderedDictionary<string, List<string>> origStrings
                = originalInfEditor.ReadSection("Strings");
            origStrings.Remove("Version");
            if (!embeddedInfEditor.SectionExists("Strings"))
            {
                embeddedInfEditor.Add("Strings");
            }
            embeddedInfEditor.Add("Strings", origStrings,
                IniParser.KeyExistsPolicy.Discard);
        }

        #region Fields
        // Windows source info object
        WindowsSourceInfo _sourceInfo;

        // Ignore catalogs ?
        bool _ignoreCats;

        // Directory Vars
        string _workingDirectory;
        string _wmp11InstallerPath;
        string _sourceDirectory;
        string _archFilesDirectory;
        string _extractedDirectory;
        string _drivercabExtractedDirectory;

        // Other variables
        string _externalInfFilename;
        string _externalCabFilename;

        // Icon raw data
        byte[] _customIcon;

        // Thread abort signaling
        volatile bool _aborting;

        // Hotfix list
        List<string> _hotfixInstallerList;

        // Addon type index
        int _addonTypeIndex;

        // SimpleINIEditor instances
        IniParser _entriesCombinedEditor;
        IniParser _wmp11ExtInfEditor;
        IniParser _wmp11InfEditor;
        IniParser _txtsetupSifEditor;
        IniParser _dosnetInfEditor;
        IniParser _sysocInfEditor;
        IniParser _svcpackInfEditor;
        IniParser _drvIndexEditor;

        // List of files to copy
        Dictionary<string, string> _filesToCompressInArch;
        Dictionary<string, string> _filesToCopyInArch;
        /// <summary>
        /// Key = Shortname, Value = Longname
        /// </summary>
        Dictionary<string, string> _filesToCompressInSvcpack;
        Dictionary<string, string> _filesToCompressInCab;
        
        // HACK for x64 32-bit support
        Dictionary<string, string> _filesToCompressInI386;
        int _sdnEntryI386Folder;
        string _x64I386Folder;

        // Which CAB to extract / repack (only for 2k3/x64 so far)
        string _driverCabFile;

        // Possibly compressed files
        static string[] _possCompArchFile
            = new string[] { "SYSOC.INF", "SVCPACK.INF" };

        // Source Type String
        StringBuilder _osVersionBuilder = new StringBuilder(50);
        #endregion

        #region Internal accessors
        internal string OsVersion
        {
            get { return this._osVersionBuilder.ToString(); }
        }

        internal string WorkingDirectory
        {
            get { return this._workingDirectory; }
        }
        #endregion

        #region Enums
        enum OverwriteFileBehaviour
        {
            UncompressedStandardArch = 0,
            CompressedStandardArch = 1,
            UncompressedPossiblex32x64Combo = 2,
            CompressedPossiblex32x64Combo = 3
        }
        #endregion

        #region Debugging events
#if DEBUG
        internal delegate bool DebuggingYesNoQuestionDelegate();
        internal event DebuggingYesNoQuestionDelegate OnBeforeMergeFolders;

        internal delegate void DebuggingMessageDelegate(string message, string title);
        internal event DebuggingMessageDelegate OnDebuggingMessage;
#endif
        #endregion
    }
}
