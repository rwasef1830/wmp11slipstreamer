#region Using statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using Epsilon.Collections.Generic;
using Epsilon.DebugServices;
using Epsilon.IO;
using Epsilon.IO.Compression;
using Epsilon.IO.Compression.Cabinet;
using Epsilon.Parsers;
using Epsilon.Security.Cryptography;
using Epsilon.Slipstreamers;
using Epsilon.Win32;
using Epsilon.Win32.Resources;
using Epsilon.WMP11Slipstreamer.Localization;
#endregion

// This is the main backend that performs the actual integration tasks

namespace Epsilon.WMP11Slipstreamer
{
    public partial class Backend : SlipstreamerBase
    {
        public Backend(BackendParams paramsObject) 
            : base(paramsObject, "wmp11temp") 
        {
            base.AddSlipstreamStep(new SlipstreamStep(
                this.ExtractWMP11Installer, 
                Msg.statExtractWmpRedist));
            
            base.AddSlipstreamStep(new SlipstreamStep(
                this.PrepareDependencies,
                Msg.statReadCoreInfs));

            base.AddSlipstreamStep(new SlipstreamStep(
                this.PrepareForParse,
                Msg.statReadFiles));

            base.AddSlipstreamStep(new SlipstreamStep(
                this.ParseAndEditFiles,
                Msg.statPrepareEdit));

            base.AddSlipstreamStep(new SlipstreamStep(
                this.ApplyFixes,
                Msg.statPreparingFixes));

            base.AddSlipstreamStep(new SlipstreamStep(
                this.SaveFiles,
                Msg.statSavingInis));

            base.AddSlipstreamStep(new SlipstreamStep(
                this.CompressFiles));
        }

        /// <summary>
        /// Judge if source is eligible for integration or not
        /// </summary>
        protected override bool SourceIsSupported()
        {
            switch (this.Parameters.SourceInfo.SourceVersion)
            {
                case WindowsType._Server2003:
                    if (this.Parameters.SourceInfo.ServicePack < 1)
                        return false;
                    break;

                case WindowsType._XP:
                    if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                    {
                        if (this.Parameters.SourceInfo.ServicePack < 2)
                            return false;
                    }
                    else
                    {
                        goto case WindowsType._Server2003;
                    }
                    break;

                default:
                    return false;
            }

            if (this.Parameters.SourceInfo.ReducedMediaEdition)
                return false;

            return true;
        }

        /// <summary>
        /// Prepares for parsing by copying commonly-edited temporary files
        /// from the "i386" directory to the extractedDirectory
        /// </summary>
        void PrepareForParse()
        {
            base.ExtractAndParseArchFiles();

            if (this.Parameters.SourceInfo.SourceVersion == WindowsType._Server2003 || 
                this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
            {
                // Determine which CAB to extract to repack drivers
                this.OnAnnounce(Msg.statReplaceInDriverCab);
                this._drvindexInf = new IniParser(
                    this.CreatePathString(_archDir, "DrvIndex.inf"), true
                );
                
                // XXX On x86, get the highest spn.cab filename, on x64 force it to be driver.cab
                // because we have a bug where there is driver32.cab and driver.cab and we end
                // up selecting the wrong cabinet.
                if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                {
                    List<string> cabFilesData =
                        this._drvindexInf.ReadAllValues("Version", "CabFiles");
                    cabFilesData.Sort(StringComparer.OrdinalIgnoreCase);
                    string variableName = cabFilesData[cabFilesData.Count - 1];
                    this._driverCabFile = this._drvindexInf.ReadValue("Cabs", variableName);
                }
                else
                {
                    this._driverCabFile = "driver.cab";
                }
                
                CancelOrPauseCheckpoint();
                
                this.OnAnnounce(String.Format(Msg.statExtractFile, this._driverCabFile));

                this.OnResetCurrentProgress();
                Archival.NativeCabinetExtract(
                    this.CreatePathString(_archDir, _driverCabFile),
                    _driverDir,
                    null,
                    this.OnUpdateCurrentProgress
                );
                this.OnHideCurrentProgress();
            }
        }

        void ExtractWMP11Installer()
        {
            string[] filesToExtract = null;

            switch (this.Parameters.SourceInfo.Arch)
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
            this.OnResetCurrentProgress();
            ProgressTracker currProgress 
                = new ProgressTracker(filesToExtract.Length + 2);

            HelperConsole.InfoWriteLine(this.Parameters.WmpInstallerSource, "NativeExtractHotfix");
            NativeExtractHotfix(this.Parameters.WmpInstallerSource, _extractDir, null);
            this.OnIncrementCurrentProgress(currProgress);

            // Read control.xml and determine if we're on correct installer or not
            ParseXmlVerifyOS(
                this.CreatePathString(_extractDir, "control.xml"));

            foreach (string file in filesToExtract)
            {
                CancelOrPauseCheckpoint();
                HelperConsole.InfoWriteLine(file, "NativeExtractHotfix");
                NativeExtractHotfix(this.CreatePathString(
                    _extractDir, file), 
                    _extractDir,
                    delegate(string hotfixInstaller)
                    {
                        throw new IntegrationException(Msg.errWmpRedistArchMismatch);
                    });
                this.OnIncrementCurrentProgress(currProgress);
            }

            // Don't extract wmpappcompat for Server 2003 or any x64 architecture
            if (this.Parameters.SourceInfo.SourceVersion != WindowsType._Server2003
                && this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
            {
                NativeExtractHotfix(this.CreatePathString(_extractDir,
                    "wmpappcompat.exe"), _extractDir, null);
            }
            this.OnIncrementCurrentProgress(currProgress);

            // Move anything in SP2QFE back in to extracted (flatten tree)
            if (Directory.Exists(this.CreatePathString(_extractDir, 
                "SP2QFE")))
            {
                foreach (string filepath in Directory.GetFiles(
                    this.CreatePathString(_extractDir, "SP2QFE"), "*", 
                    SearchOption.AllDirectories))
                {
                    string filename = Path.GetFileName(filepath);
                    File.Move(filepath, 
                        this.CreatePathString(_extractDir, filename));
                }
            }

            this.OnHideCurrentProgress();

#if DEBUG
            this.OnMessage("WMP11 installer extracted. This is a reference "
                + "point to examine where the basic files are located.",
                "Notification", MessageEventType.Information);
#endif
        }

        void PrepareDependencies()
        {
            CryptoHelp crypto = new CryptoHelp(Globals.UniqueTag);

            // entries.ini: repository1
            string entriesContent = crypto.DecryptToString(
                Properties.Resources.repository1,
                Globals.Repo1Key,
                Encoding.Default
            );

            this._entriesCombinedEditor =
                new IniParser(entriesContent, true, "entries_wmp11.ini");

            byte[] repository = null;
            // repository1: entries_combined_wmp11.ini
            // repository2: (reserved)
            // repository3: XP Home/Pro x32
            // repository4: XP MCE 2005 x32
            // repository5: Server 2003 x32
            // repository6: (reserved)
            // repository7: XP/2k3 x64
            switch (this.Parameters.SourceInfo.SourceVersion)
            {
                case WindowsType._Unknown:
                    throw new IntegrationException(Msg.errSrcTypeDetectFail);

                case WindowsType._XP:
                    if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
                        repository = Properties.Resources.repository7;
                    else if (this.Parameters.SourceInfo.Edition == WindowsEdition.MediaCenter)
                        repository = Properties.Resources.repository4;
                    else
                        repository = Properties.Resources.repository3;
                    break;

                case WindowsType._Server2003:
                    if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
                        goto case WindowsType._XP;
                    else
                        repository = Properties.Resources.repository5;
                    break;
            }

            // Decrypt repositories
            MemoryStream repositoryDecrypted = new MemoryStream();
            crypto.DecryptStream(new MemoryStream(repository),
                Globals.Repo2Key, repositoryDecrypted, false);
            repositoryDecrypted.Seek(0, SeekOrigin.Begin);

            // Temp Folder
            string tempFolder = this.CreatePathString(_workDir,
                "wmp11int.r" + new Random().Next(99).ToString());

            CancelOrPauseCheckpoint();

            // Extract repository cabinet
            Archival.NativeCabinetExtract(repositoryDecrypted, tempFolder, null, null);

            string[] infEssentialOverwrites = new string[] { 
                "wmp.inf", "wmfsdk.inf", "wpd.inf", "wmp11.inf" };

            // Files that are embedded and should overwrite in extracted
            foreach (string essentialOverwrite in infEssentialOverwrites)
            {
                if (File.Exists(this.CreatePathString(tempFolder,
                    essentialOverwrite)))
                {
                    FileSystem.MoveFileOverwrite(
                        this.CreatePathString(tempFolder, essentialOverwrite),
                        this.CreatePathString(_extractDir, essentialOverwrite)
                    );
                }
            }

            // External INF handled via special routine
            File.Move(
                this.CreatePathString(tempFolder, "wmp11ext.inf"),
                this.CreatePathString(_workDir, "wmp11ext.inf")
            );

            // Attach both INF editors on the 2 main INFs
            _wmp11ExtInfEditor = new IniParser(
                this.CreatePathString(_workDir, "wmp11ext.inf"), true
            );
            _wmp11InfEditor = new IniParser(
                this.CreatePathString(_extractDir, "wmp.inf"), true
            );

            // Migrate localizable [Strings] from original infs
            string wmfSdkPath = this.CreatePathString(this._extractDir,
                "wmfsdk.inf");
            this.MigrateStringsFromOriginalInf("wmp.inf", tempFolder, _wmp11InfEditor);
            if (File.Exists(wmfSdkPath))
            {
                IniParser wmfsdkEditor = new IniParser(wmfSdkPath, true);
                this.MigrateStringsFromOriginalInf("wmfsdk.inf", tempFolder,
                    wmfsdkEditor);
                wmfsdkEditor.SaveIni();
            }

            FileSystem.DeleteFolder(tempFolder);
            CancelOrPauseCheckpoint();
        }

        void ParseXmlVerifyOS(string xmlPath)
        {
            HelperConsole.InfoWriteLine("ParseXmlVerifyOS", "Backend");
            if (!File.Exists(xmlPath))
            {
                throw new IntegrationException(
                    Msg.errWmpRedistNoControlXml);
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

                if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                    osArch = "x86";
                else if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
                    osArch = "amd64";

                // Take number of fields equal to the number of fields
                // that were specified by the ini to prevent false negative.
                osVer = new Version(
                    this.Parameters.SourceInfo.ToVersion().ToString(
                    osUpperBoundVer.ToString().Split('.').Length));

                if (osVer.CompareTo(osLowerBoundVer) < 0
                    || osVer.CompareTo(osUpperBoundVer) > 0
                    || !CM.SEqO(arch, osArch, true))
                {
                    throw new IntegrationException(
                        String.Format(Msg.errWmpRedistWrongVer,
                        osLowerBound + ((osUpperBound == null)? 
                        String.Empty : "-" + osUpperBound), 
                        arch, osVer, osArch));
                }
            }
            else
            {
                throw new IntegrationException(
                    String.Format(Msg.errControlXmlInvalid, 
                    Path.GetFileName(xmlPath)));
            }
        }

        /// <summary>
        /// Checks if the folder to which the hotfix was extracted
        /// matches the target windows architecture by checking the
        /// PE header of the hotfix "Update.exe" installer.
        /// </summary>
        /// <param name="folderToCheck"></param>
        /// <returns></returns>
        bool HotfixMatchesArch(string folderToCheck)
        {
            HelperConsole.InfoWriteLine("HotfixMatchArch", "Backend");
            string updateFilename = this.CreatePathString(folderToCheck,
                "Update", "Update.exe");
            PeEditor editor = new PeEditor(updateFilename);
            return (editor.TargetMachineType == Architecture.x86
                && this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                || (editor.TargetMachineType == Architecture.x64
                && this.Parameters.SourceInfo.Arch == TargetArchitecture.x64);
        }

        void ParseAndEditFiles()
        {
            this.OnAnnounce(Msg.statPrepareEdit);
            string dosnetFilesSection = null;
            string txtsetupFilesSection = null;
            string txtsetupDirSection = null;
            string svcPackSection = null;

            // HACK Fix bug introduced by Microsoft into SP3 in wbemoc.inf
            if (this.Parameters.SourceInfo.SourceVersion == WindowsType._XP
                && this.Parameters.SourceInfo.ServicePack == 3)
            {
                this.FixWbem();
            }

            switch (this.Parameters.SourceInfo.SourceVersion)
            {
                case WindowsType._XP:
                    if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
                    {
                        dosnetFilesSection = "xp_x64_dosnet_files";
                        txtsetupFilesSection = "xp_x64_txtsetup_files";
                        svcPackSection = "xp_x64_svcpack";
                    }
                    else if (this.Parameters.SourceInfo.Edition == WindowsEdition.MediaCenter)
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
                    if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
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

            this.ResolveDestinationDirIdConflicts(
                txtsetupFilesSection, 
                txtsetupDirSection);

            OrderedDictionary<string, List<string>> txtsetupFilesRef
                = this._entriesCombinedEditor.GetRef(txtsetupFilesSection);
            this.OnAnnounce(String.Format(Msg.statEditFile, "Txtsetup.sif"));
            CancelOrPauseCheckpoint();

            // Initialise the file copy dictionaries
            this._filesToCompressInArch = new Dictionary<string, string>(
                txtsetupFilesRef.Count,
                StringComparer.OrdinalIgnoreCase
            );
            this._filesToCopyInArch = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase
            );

            if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
            {
                this._filesToCompressInI386 = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase
                );

                // Figure out which SourceDisksNames entry for
                // I386 folder on x64 platform
                OrderedDictionary<string, List<string>> sourceDisksNames
                    = this._txtsetupSif.GetRef("SourceDisksNames.amd64");
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
                    throw new IntegrationException(Msg.errNoI386RefInX64Src);
                }
            }
            
            this._txtsetupSif.Add(
                "SourceDisksFiles",
                txtsetupFilesRef,
                IniParser.KeyExistsPolicy.Ignore
            );

            if (txtsetupDirSection != null)
            {
                this._txtsetupSif.Add(
                    "WinntDirectories",
                    this._entriesCombinedEditor.GetRef(txtsetupDirSection),
                    IniParser.KeyExistsPolicy.Ignore
                );
            }

            this.OnAnnounce(Msg.statGenFileList);
            foreach (KeyValuePair<string, List<string>> txtPair in txtsetupFilesRef)
            {
                string shortName = txtPair.Key;
                string longName = null;
                bool uncompressed = false;
                bool isX6432bitFile = false;

                if (txtPair.Value.Count < 10)
                {
                    throw new IntegrationException(String.Format(
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
                isX6432bitFile = this.Parameters.SourceInfo.Arch == TargetArchitecture.x64
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

            this.OnAnnounce(String.Format(Msg.statEditFile, "Dosnet.inf"));
            OrderedDictionary<string, List<string>> dosnetRef 
                = this._entriesCombinedEditor.GetRef(dosnetFilesSection);
            this._dosnetInf.Add(
                "Files",
                dosnetRef,
                IniParser.KeyExistsPolicy.Ignore
            );

            this.OnAnnounce(String.Format(Msg.statEditFile, "SysOc.inf"));
            OrderedDictionary<string, List<string>> sysOcRef = 
                this._entriesCombinedEditor.GetRef("common_sysoc");

            if (sysOcRef[0].Value.Count != 5)
                throw new IntegrationException(String.Format(
                    "Invalid SysOC line: [{0} = {1}] in common entries file.",
                    sysOcRef[0].Key, new CSVParser().Join(sysOcRef[0].Value)));

            this._externalInfFilename = sysOcRef[0].Value[2];
            if (this._filesToCompressInArch.ContainsKey(_externalInfFilename))
                this._filesToCompressInArch.Remove(_externalInfFilename);
            else if (this._filesToCompressInI386 != null
                && this._filesToCompressInI386.ContainsKey(_externalInfFilename))
                this._filesToCompressInI386.Remove(_externalInfFilename);
            else
                throw new IntegrationException(
                    "SysOc-referenced Inf not referenced in [dosnet_files].");

            this._sysocInf.Add(
                "Components",
                sysOcRef[0].Key,
                sysOcRef[0].Value,
                IniParser.KeyExistsPolicy.Ignore
            );

            if (!this.Parameters.IgnoreCats)
            {
            	this.OnAnnounce(String.Format(Msg.statEditFile, "Svcpack.inf"));

                List<string[]> svcpackData = new List<string[]>();
                if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
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
                this._svcpackInf.RemoveLine(
                    "SetupData",
                    "CatalogSubDir"
                );

                this._svcpackInf.Add(
                    "SetupData",
                    "CatalogSubDir",
                    (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86) ?
                    @"""\i386\svcpack""" : @"""\amd64\svcpack""",
                    IniParser.KeyExistsPolicy.Discard
                );

                // Write critical svcpack.inf keys
                Dictionary<string, int> svcpackCriticalKeys 
                    = new Dictionary<string, int>(3);

                if (this.Parameters.SourceInfo.SourceVersion == WindowsType._Server2003
                    || (this.Parameters.SourceInfo.SourceVersion == WindowsType._XP
                    && this.Parameters.SourceInfo.Arch == TargetArchitecture.x64))
                {
                    svcpackCriticalKeys.Add("MajorVersion", 5);
                    svcpackCriticalKeys.Add("MinorVersion", 2);
                    svcpackCriticalKeys.Add("BuildNumber", 3790);
                }
                else if ((this.Parameters.SourceInfo.SourceVersion == WindowsType._XP)
                    && this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                {
                    svcpackCriticalKeys.Add("MajorVersion", 5);
                    svcpackCriticalKeys.Add("MinorVersion", 1);
                    svcpackCriticalKeys.Add("BuildNumber", 2600);
                }

                foreach (KeyValuePair<string, int> pair in svcpackCriticalKeys)
                {
                    if (!this._svcpackInf.KeyExists("Version", pair.Key))
                    {
                        this._svcpackInf.Add("Version", pair.Key,
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

                this._svcpackInf.Add(
                    "ProductCatalogsToInstall",
                    this._filesToCompressInSvcpack.Keys,
                    false, false);
            }

            CancelOrPauseCheckpoint();

            this.OnAnnounce(Msg.statGenFileList);
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
                        throw new IntegrationException(
                            String.Format(
                            "Invalid syntax in external inf: [{0}]: ({1}).",
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

            // HACK Fix some xml files overwriting each other due to same short 
            // name but diff folders on x32
            if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
            {
                this._filesToCompressInCab["connecti.xml"]
                    = "connectionmanager_stub.xml";
                this._filesToCompressInCab["contentd.xml"]
                    = "contentdirectory_stub.xml";
                this._filesToCompressInCab["mediarec.xml"]
                    = "mediareceiverregistrar_stub.xml";
            }

            switch (this.Parameters.RequestedType)
            {
                case PackageType.Vanilla:
                    // Removing Tweaks.AddReg sections to get vanilla
                    string[] sectionsToProcess;
                    if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
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
                    if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                        removeSources = new string[] { "PerUserStub" };
                    else if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
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

                case PackageType.Tweaked:
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

                default:
                    // Defaulting to vanilla is better than throwing exception
                    // Because someone could meddle in the registry and set the 
                    // index to something invalid, which would cause us to crash on startup.

                    //NotSupportedException exception = new NotSupportedException(
                    //    "Unknown or unsupport package type requested.");
                    //exception.Data.Add("PackageType", this.Parameters.RequestedType);
                    //throw exception;
                    goto case PackageType.Vanilla;
            }

            CancelOrPauseCheckpoint();

            // Figure out the external cab filename from the external inf
            if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                this._externalCabFilename = this._wmp11ExtInfEditor.ReadAllValues(
                    "SourceDisksNames.x86", "1")[1];
            else if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
                this._externalCabFilename = this._wmp11ExtInfEditor.ReadAllValues(
                    "SourceDisksNames.amd64", "1")[1];

            CancelOrPauseCheckpoint();
        }

        void ResolveDestinationDirIdConflicts(string txtsetupFilesSection, 
            string txtsetupDirSection)
        {
            CSVParser csvParser = new CSVParser();

            int biggestNumber = 0;
            Dictionary<int, string> usedTxtSetupDirs = new Dictionary<int, string>();
            foreach (KeyValuePair<string, List<string>> winntDirPair
                in _txtsetupSif.GetRef("WinntDirectories"))
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
                        throw new IntegrationException(
                            string.Format(
                            Msg.errDirIdDuplicated,
                            number, this._txtsetupSif.IniFileInfo.Name)
                            );
                    }
                }
                else
                {
                    throw new IntegrationException(
                        String.Format(
                        Msg.errInvalidKeyInWinntDirs,
                        winntDirPair.Key, this._txtsetupSif.IniFileInfo.Name));
                }
            }

            OrderedDictionary<string, List<string>> txtsetupFilesRef
                = this._entriesCombinedEditor.GetRef(txtsetupFilesSection);
            Dictionary<int, int> renameDestDirDictionary = null;

            CancelOrPauseCheckpoint();

            if (txtsetupDirSection != null)
            {
                this.OnAnnounce(Msg.statFixingTxtsetupDirs);
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
                        throw new IntegrationException(
                            String.Format(
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
                "{0} -> {1}; Original: [{2} = {3}]",
                txtDir.Key, newDirNumber,
                txtDir.Key, csvParser.Join(txtDir.Value)), 
                "Mapping DirId");

            if (!this._entriesCombinedEditor.TryChangeKey(
                txtsetupDirSection,
                txtDir.Key,
                newDirNumber.ToString(),
                txtDir.Value
             ))
            {
                throw new IntegrationException(
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
            this.OnAnnounce(Msg.statPreparingFixes);
            string fixesFolder = this.CreatePathString(_workDir, "Fixes");
            string tempCompareFolder = this.CreatePathString(_workDir, 
                "FixesCompare");
            string fixesCab = this.CreatePathString(fixesFolder, "fixes.cab");

            // Apply hotfixes
            this.OnAnnounce(Msg.statExtractApplyHotfix);

            // Make the 2 folders
            Directory.CreateDirectory(fixesFolder);
            Directory.CreateDirectory(tempCompareFolder);

            // Mammoth hotfix apply functions
            Dictionary<string, IEnumerable<string>> hotfixFileDictionary 
                = this.StandardHotfixApply(fixesFolder);

            // Get rid of superseded fixes by adding the hotfixes
            // listed in hotfixFileDictionary values only
            if (!this.Parameters.IgnoreCats)
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
            if (this.Parameters.SourceInfo.SourceVersion == WindowsType._XP
                && this.Parameters.SourceInfo.Edition == WindowsEdition.MediaCenter
                && this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
            {
                this.IntegrateKB913800(fixesFolder);
            }

            // HACK Special treatment for KB926239 - Acadproc
            if (this.Parameters.SourceInfo.SourceVersion == WindowsType._XP
                && this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
            {
                this.ProcessKB926239(tempCompareFolder,
                    this.CreatePathString(_extractDir, "Update"));
            }

            // Check Uxtheme.dll and Msobmain.dll versions
            if (this.Parameters.SourceInfo.SourceVersion == WindowsType._XP
                && this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
            {
                Version minUxthemeVer = new Version(6, 0, 2900, 2845);
                Version minMsobmainVer = new Version(5, 1, 2600, 2659);

                if (CopyOrExpandFromArch("uxtheme.dll", tempCompareFolder, true))
                {
                    string uxthemeComparePath = 
                        this.CreatePathString(tempCompareFolder, "uxtheme.dll");
                    FileVersionInfo sourceUxthemeFileVer 
                        = FileVersionInfo.GetVersionInfo(uxthemeComparePath);
                    Version sourceUxthemeVer = CM.VerFromFileVer(sourceUxthemeFileVer);

                    if (minUxthemeVer.CompareTo(sourceUxthemeVer) > 0)
                    {
                        this.OnMessage(
                            Msg.dlgWarnUxtheme_Text,
                            Msg.dlgWarnUxtheme_Title, MessageEventType.Warning);
                    }

                    FileSystem.DeleteFile(uxthemeComparePath);
                }

                if (CopyOrExpandFromArch("msobmain.dll", tempCompareFolder, true))
                {
                    string msobmainComparePath = this.CreatePathString(
                        tempCompareFolder, "msobmain.dll");
                    FileVersionInfo sourceMsobmainFileVer 
                        = FileVersionInfo.GetVersionInfo(msobmainComparePath);
                    Version sourceMsobmainVer = CM.VerFromFileVer(sourceMsobmainFileVer);

                    if (minMsobmainVer.CompareTo(sourceMsobmainVer) > 0)
                    {
                        this.OnMessage(
                            Msg.dlgWarnOobe_Text,
                            Msg.dlgWarnOobe_Title, MessageEventType.Warning);
                    }

                    FileSystem.DeleteFile(msobmainComparePath);
                }
            }

            // Hide progress bar
            this.OnHideCurrentProgress();
        }

        Dictionary<string, IEnumerable<string>> StandardHotfixApply(string fixesFolder)
        {
            // Normal hotfix file list dictionaries
            Dictionary<string, IEnumerable<string>> hotfixFileDictionary
                = new Dictionary<string, IEnumerable<string>>(
                    this.Parameters.HotfixFiles.Count, StringComparer.OrdinalIgnoreCase);

            ProgressTracker hfixExtractProgress = new ProgressTracker(
                this.Parameters.HotfixFiles.Count);
            this.OnResetCurrentProgress();

            foreach (string hotfix in this.Parameters.HotfixFiles)
            {
                try
                {
                    NativeExtractHotfix(this.CreatePathString(
                        this.Parameters.HotfixFolder, hotfix), fixesFolder,
                        delegate(string hotfixInstaller)
                        {
                            throw new IntegrationException(
                                String.Format(Msg.errHotfixArchMismatch,
                                Path.GetFileName(hotfixInstaller)));
                        });
                    CancelOrPauseCheckpoint();

                    // Processing Update.inf
                    IniParser updateInfEditor;
                    string updateInfPath = this.CreatePathString(fixesFolder,
                        "Update", "Update.inf");
                    string updateQfeInfPath = this.CreatePathString(fixesFolder,
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
                        throw new IntegrationException(String.Format(
                            Msg.errNoInfInHotfix,
                            Path.GetFileName(hotfix)));
                    }

                    // Hotfix Update.inf processor condition evaluator
                    HotfixParserEvaluator evaluator = new HotfixParserEvaluator(
                        this._extractDir, this.Parameters.SourceInfo);

                    // Hotfix Update.inf processor
                    HotfixInfParser hotfixParser = new HotfixInfParser(updateInfEditor,
                        evaluator.EvaluateCondition, this.Parameters.SourceInfo);

                    // HACK Block WMPAPPCOMPAT from Server 2003, in case someone 
                    // gets smart and tries to integrate it by itself
                    if (CM.SEqO(hotfixParser.HotfixName, "KB926239", true)
                        && this.Parameters.SourceInfo.SourceVersion == WindowsType._Server2003)
                    {
                        this.OnIncrementCurrentProgress(hfixExtractProgress);
                        continue;
                    }

                    foreach (HotfixInfParser.FileListSection fList in hotfixParser.FileList)
                    {
                        foreach (KeyValuePair<string, string> filePair in fList.FileDictionary)
                        {
                            string relativeFilePath = filePair.Key;
                            string fileNameOnly = Path.GetFileName(relativeFilePath);

                            string fixFullPath = this.CreatePathString(
                                fixesFolder, relativeFilePath);
                            string orgFullPath = this.CreatePathString(
                                this._extractDir, fileNameOnly);

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
                                    orgFullPath = this.CreatePathString(
                                        _extractDir, "i386", fileNameOnly);
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
                                    orgFullPath = this.CreatePathString(
                                        _extractDir, "amd64", fileNameOnly);
                                }
                            }

                            // Bug reported here: Sometimes fixFullPath doesn't exist
                            // and CompareVersions throws FileNotFoundException
                            FileVersionComparison result
                                = CompareVersions(fixFullPath,
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
                                FileSystem.DeleteFile(fixFullPath);
                            }
                            else if (result == FileVersionComparison.NotFound)
                            {
                                throw new IntegrationException(
                                    String.Format(
                                    Msg.errUnsupportedFixAttempt,
                                    relativeFilePath, hotfixParser.HotfixName)
                                );
                            }
                        }
                    }
                    this.OnIncrementCurrentProgress(hfixExtractProgress);
                }
                catch (Exception ex)
                {
                    ex.Data.Add(Msg.errOffendingFix, Path.GetFileName(hotfix));
                    throw ex;
                }
            }
            return hotfixFileDictionary;
        }

        void SaveFiles()
        {
            this.OnAnnounce(Msg.statSavingInis);

            // Save wmp11 infs
            FileSystem.UnsetReadonly(this._wmp11ExtInfEditor.IniFileInfo);
            FileSystem.UnsetReadonly(this._wmp11InfEditor.IniFileInfo);
            this._wmp11ExtInfEditor.SaveIni();
            this._wmp11InfEditor.SaveIni();

            this.SaveArchFiles();
        }

        void CompressFiles()
        {
            // Compress External INF
            this.OnAnnounce(String.Format(Msg.statCompressFile, 
                this._externalInfFilename));
            Archival.NativeCabinetMakeCab(
                this.CreatePathString(
                this._workDir, this._externalInfFilename),
                this._workDir);
            File.Delete(this.CreatePathString(
                this._workDir, this._externalInfFilename));

            // Insert the custom icon
            if (this.Parameters.CustomIcon != null)
            {
                this.OnAnnounce(Msg.statApplyCustIcon);
                ResourceEditor resEdit = new ResourceEditor(
                    this.CreatePathString(this._extractDir, "wmplayer.exe"));
                resEdit.ReplaceMainIcon(this.Parameters.CustomIcon);
                resEdit.Close();
                PeEditor editor = new PeEditor(this.CreatePathString(
                    this._extractDir, "wmplayer.exe"));
                editor.RecalculateChecksum();
            }

            // Create external CAB
            this.OnAnnounce(String.Format(Msg.statCreateCab, 
                this._externalCabFilename));
            string wmp11cabdirectory = this.CreatePathString(
                this._workDir, "wmp11cab");
            Directory.CreateDirectory(wmp11cabdirectory);
            CancelOrPauseCheckpoint();
            foreach (KeyValuePair<string, string> pair in this._filesToCompressInCab)
            {
                string filenameInExtracted = this.CreatePathString(
                    this._extractDir, pair.Key);
                string filenameToBeRenamedInExtracted = this.CreatePathString(
                    this._extractDir, pair.Value);
                string destinationPath = this.CreatePathString(
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
                {
                    throw new Epsilon.Slipstreamers.FileNotFoundException
                        (String.Format(
                        "Creating external cab failed. File: \"{0}\" ({1}) not found.", 
                        pair.Key, pair.Value), pair.Key);
                }
            }

            this.OnResetCurrentProgress();

            Archival.NativeCabinetCreate(this.CreatePathString(
                this._workDir, this._externalCabFilename),
                wmp11cabdirectory, 
                true, FCI.CompressionLevel.Lzx21, null,
                this.OnUpdateCurrentProgress);
            this.OnHideCurrentProgress();
            
            // this.OnIncrementGlobalProgress();

            // Compress all i386 files
            this.OnAnnounce(Msg.statCompressAdded);

            // Hack for wpdshextres.dll.409
            File.Move(this.CreatePathString(
                _extractDir, "locbin", "wpdshextres.dll.409"),
                this.CreatePathString(_extractDir, "wpdshextres.dll")
            );

            // Remove CAB filename from all lists to prevent
            // FileNotFoundException from occuring
            _filesToCompressInArch.Remove(_externalCabFilename);
            _filesToCopyInArch.Remove(_externalCabFilename);

            int totalFileCount = _filesToCompressInArch.Count + _filesToCopyInArch.Count;
            if (this._filesToCompressInI386 != null)
                totalFileCount += this._filesToCompressInI386.Count;

            this.OnResetCurrentProgress();
            ProgressTracker compProgress = new ProgressTracker(totalFileCount);

            foreach (KeyValuePair<string, string> pair in _filesToCompressInArch)
            {
                RenameAndCompressArchFile(pair.Key, pair.Value, 
                    this.Parameters.SourceInfo.Arch, true, null);
                this.OnIncrementCurrentProgress(compProgress);
                this.CancelOrPauseCheckpoint();
            }
            if (this._filesToCompressInI386 != null)
            {
                foreach (KeyValuePair<string, string> pair in _filesToCompressInI386)
                {
                    RenameAndCompressArchFile(pair.Key, pair.Value, 
                        TargetArchitecture.x86, true, this._x64i386WorkDir);
                    this.OnIncrementCurrentProgress(compProgress);
                    this.CancelOrPauseCheckpoint();
                }
            }
            foreach (KeyValuePair<string, string> pair in _filesToCopyInArch)
            {
                RenameAndCompressArchFile(pair.Key, pair.Value,
                    this.Parameters.SourceInfo.Arch, false, null);
                this.OnIncrementCurrentProgress(compProgress);
                this.CancelOrPauseCheckpoint();
            }
            this.OnHideCurrentProgress();
            
            // this.OnIncrementGlobalProgress();

            // Locate files that directly overwrite
            this.OnAnnounce(Msg.statDetOverwrite);

            // HACK: Delete EULA.txt after it went into the external CAB
            FileSystem.DeleteFile(this.CreatePathString(this._extractDir, "EULA.TXT"));

            string[] filesInExtracted = Directory.GetFiles(
                this._extractDir, "*", SearchOption.TopDirectoryOnly);
            Dictionary<int, OverwriteFileBehaviour> filesThatOverwrite
                = new Dictionary<int, OverwriteFileBehaviour>(
                filesInExtracted.Length);
            for (int i = 0; i < filesInExtracted.Length; i++)
            {
                string filename = Path.GetFileName(filesInExtracted[i]);
                string compressedFilename = CM.GetCompressedFileName(filename);

                // For standard arch files
                string archName = this.CreatePathString(
                    this._archDir, filename);
                string archCompressedName = this.CreatePathString(
                    this._archDir, compressedFilename);

                if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
                {
                    // For 32-bit files that come with x64 arch, all of them
                    // in the source are prefixed with a "w" and renamed by
                    // Windows Setup when installed to SysWOW64 (via txtsetup.sif)
                    string i386Name = this.CreatePathString(
                        this._x64i386ArchDir, "w" + filename);
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
            this.OnAnnounce(Msg.statCompressOverwrite);
            this.OnResetCurrentProgress();
            compProgress = new ProgressTracker(filesThatOverwrite.Count);
            foreach (KeyValuePair<int, OverwriteFileBehaviour> 
                pair in filesThatOverwrite)
            {
                string currentFilePath = filesInExtracted[pair.Key];
                string currentFileName = Path.GetFileName(currentFilePath);

                string amd64File = this.CreatePathString(this._extractDir,
                    "AMD64", currentFileName);

                switch (pair.Value)
                {
                    case OverwriteFileBehaviour.UncompressedStandardArch:
                        FileSystem.CopyFile(currentFilePath,
                            this.CreatePathString(_workDir, currentFileName));
                        break;

                    case OverwriteFileBehaviour.CompressedStandardArch:
                        Archival.NativeCabinetMakeCab(
                            currentFilePath,
                            this._workDir
                        );
                        break;

                    case OverwriteFileBehaviour.UncompressedPossiblex32x64Combo:
                        FileSystem.CopyFile(currentFilePath, this.CreatePathString(
                            this._x64i386WorkDir, "w" + currentFileName));
                        if (File.Exists(amd64File))
                        {
                            FileSystem.CopyFile(amd64File,
                                this.CreatePathString(this._workDir, 
                                currentFileName));
                        }
                        break;

                    case OverwriteFileBehaviour.CompressedPossiblex32x64Combo:
                        string x32FileName = 
                            this.CreatePathString(
                            Path.GetDirectoryName(currentFilePath), "w" + currentFileName);
                        File.Move(currentFilePath, x32FileName);
                        Archival.NativeCabinetMakeCab(
                            x32FileName, this._x64i386WorkDir);
                        if (File.Exists(amd64File))
                        {
                            Archival.NativeCabinetMakeCab(
                                amd64File,
                                this._workDir);
                        }
                        break;
                }
                this.OnIncrementCurrentProgress(compProgress);
                this.CancelOrPauseCheckpoint();
            }
            
            // this.OnIncrementGlobalProgress();

            // Svcpack stuff
            if (!this.Parameters.IgnoreCats)
            {
                this.OnAnnounce(Msg.statCompressCats);
                this.OnResetCurrentProgress();
                compProgress = new ProgressTracker(_filesToCompressInSvcpack.Count);
                string svcpackTempFolder = this.CreatePathString(
                    _workDir, "SVCPACK");
                string svcpackExtractedFolder = this.CreatePathString(
                    _extractDir, "Update");
                Directory.CreateDirectory(svcpackTempFolder);
                foreach (KeyValuePair<string, string> pair in _filesToCompressInSvcpack)
                {
                    string shortname = this.CreatePathString(
                        svcpackExtractedFolder, pair.Key);
                    string longname = this.CreatePathString(
                        svcpackExtractedFolder, pair.Value);
                    if (File.Exists(shortname))
                    {
                        Archival.NativeCabinetMakeCab(
                            shortname,
                            svcpackTempFolder
                        );
                        this.OnIncrementCurrentProgress(compProgress);
                    }
                    else if (File.Exists(longname))
                    {
                        File.Move(longname, shortname);
                        Archival.NativeCabinetMakeCab(
                            shortname,
                            svcpackTempFolder
                        );
                        this.OnIncrementCurrentProgress(compProgress);
                    }
                    else
                    {
                        throw new IntegrationException(
                                String.Format(
                                "The file \"{0}\" ({1}) is not present in the svcpack folder.",
                                pair.Key, pair.Value
                                )
                            );
                    }

                    this.CancelOrPauseCheckpoint();
                }
            }

            this.OnHideCurrentProgress();
            // this.OnIncrementGlobalProgress();

            // 2k3/x64 repack DRIVER.CAB
            if (this.Parameters.SourceInfo.SourceVersion == WindowsType._Server2003
                || this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
            {
                Debug.Assert(Directory.Exists(this._driverDir));
                this.OnAnnounce(Msg.statReplaceInDriverCab);
                List<string[]> filesToCopy = null;
                if (this.Parameters.SourceInfo.SourceVersion == WindowsType._Server2003
                    && this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                    filesToCopy = this._entriesCombinedEditor.ReadCsvLines(
                        "2k3_drivercab_expand");
                else if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
                    filesToCopy = this._entriesCombinedEditor.ReadCsvLines(
                        "xp_x64_drivercab_expand");
                foreach (string[] fileComponents in filesToCopy)
                {
                    string filename = fileComponents[0];
                    string compressedFile = CM.GetCompressedFileName(filename);
                    string driverFolderFileName =
                        this.CreatePathString(this._driverDir, 
                        filename);
                    CancelOrPauseCheckpoint();
                    if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x64)
                    {
                        FileSystem.CopyFile(this.CreatePathString(
                            _extractDir, "AMD64", filename),
                            driverFolderFileName, true);
                    }
                    else if (this.Parameters.SourceInfo.Arch == TargetArchitecture.x86)
                    {
                        FileSystem.CopyFile(this.CreatePathString(
                            this._extractDir, filename), 
                            driverFolderFileName, true);
                    }
                    FileVersionComparison compareResult
                        = CompareVersions(driverFolderFileName,
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
                this.OnAnnounce(String.Format(Msg.statCreateCab,
                    Path.GetFileName(_driverCabFile)));
                int numberOfFiles
                    = Directory.GetFiles(this._driverDir).Length;
                this.OnResetCurrentProgress();
                CancelOrPauseCheckpoint();
                Archival.NativeCabinetCreate(this.CreatePathString(
                    _workDir, _driverCabFile),
                    this._driverDir,
                    false,
                    FCI.CompressionLevel.Lzx21,
                    null,
                    this.OnUpdateCurrentProgress
                );
                this.OnHideCurrentProgress();
            }
            
            // this.OnIncrementGlobalProgress();

            this.OnAnnounce(Msg.statCompressEdited);
            foreach (string file in _possCompArchFile)
            {
                string compressedFile = CM.GetCompressedFileName(file);
                if (File.Exists(this.CreatePathString(
                    _archDir, compressedFile)))
                {
                    Archival.NativeCabinetMakeCab(
                        this.CreatePathString(_workDir, file),
                        _workDir
                    );
                    File.Delete(this.CreatePathString(_workDir, file));
                    CancelOrPauseCheckpoint();
                }
            }
            
            // this.OnIncrementGlobalProgress();
            
            CancelOrPauseCheckpoint();
        }

        /// <summary>
        /// Compresses and renames files that are in extractedDirectory.
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
                destinationFolder = this._workDir;
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
                = this.CreatePathString(_extractDir, sourceName);
            string filenameToBeRenamedInExtracted
                = this.CreatePathString(_extractDir, destinationName);
            string filenameInSubFolder 
                = this.CreatePathString(_extractDir, possibleSubfolder, 
                sourceName);
            string filenameToBeRenamedInSubFolder
                = this.CreatePathString(_extractDir, possibleSubfolder,
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
                FileSystem.DeleteFile(filenameInExtracted);
            }
            else
            {
                File.Move(
                    filenameInExtracted,
                    this.CreatePathString(destinationFolder, sourceName)
                );
            }
        }

        /// <summary>
        /// Adds a svcpack catalog to the catalog list in SVCPACK inf
        /// </summary>
        void AddSvcpackCatalog(string catalogRelativePath, string fixesFolder)
        {
            string catalogName = Path.GetFileName(catalogRelativePath);
            string catFullPath = this.CreatePathString(fixesFolder, 
                catalogRelativePath);
            string destCatPath = this.CreatePathString(
                this._extractDir, "Update", catalogName);
            if (!this._filesToCompressInSvcpack.ContainsKey(catalogName))
            {
                this._filesToCompressInSvcpack.Add(catalogName, catalogName);
            }
            if (!File.Exists(destCatPath))
            {
                File.Move(catFullPath, destCatPath);
            }
            this._svcpackInf.Add("ProductCatalogsToInstall", 
                catalogName, false);
        }

        void NativeExtractHotfix(
            string hotfixInstaller, string destinationPath,
            Action<string> archMismatchHandler)
        {
            string tempFolder 
                = FileSystem.GetGuaranteedTempDirectory(this._workDir);
            Stream hotfixStream = Archival.GetCabStream(hotfixInstaller);

            if (hotfixStream == null)
            {
                throw new Exceptions.InvalidCabinetArchiveException(hotfixInstaller);
            }

            Archival.NativeCabinetExtract(hotfixStream, tempFolder);

            if (File.Exists(this.CreatePathString(tempFolder, "_sfx_manifest_")))
            {
                IniParser manifestEditor = new IniParser(this.CreatePathString(
                    tempFolder, "_sfx_manifest_"), true);
                OrderedDictionary<string, List<string>> deltaDict
                    = manifestEditor.GetRef("Deltas");

                foreach (KeyValuePair<string, List<string>> entry in deltaDict)
                {
                    string patchFile = this.CreatePathString(
                        tempFolder, entry.Value[0]);

                    // Don't assume that the basis file will always be in the 
                    // same folder as the patches, Search in that and in 
                    // destination as some hotfixes are using files from the 
                    // destination as a basis for some of their own patches (_sfx_*)
                    string basisFileTemp = this.CreatePathString(
                        tempFolder, entry.Value[1]);
                    string basisFileDest = this.CreatePathString(
                        destinationPath, entry.Value[1]);
                    string basisFile = null;

                    string destinationFile = this.CreatePathString(
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
                            Msg.errDeltaAPIFailed,
                            Path.GetFileName(patchFile), Path.GetFileName(basisFile)),
                            new System.ComponentModel.Win32Exception());
                    }
                }
            }
            else
            {
                FileSystem.MoveFiles(tempFolder, destinationPath, true);
            }
            FileSystem.DeleteFolder(tempFolder);

            // Check architecture
            if (archMismatchHandler != null)
            {
                if (!this.HotfixMatchesArch(destinationPath))
                {
                    archMismatchHandler(hotfixInstaller);
                }
            }

            // HACK: Delete dangerous files (like hotfix installer) as they 
            // can cause problems being detected as overwriting files later on and 
            // overwriting newer versions that are already present in the 
            // destination source.
            string[] dangerousFiles = new string[] { 
                "spmsg.dll", "spuninst.exe", "spupdsvc.exe", 
                this.CreatePathString("Update", "Update.exe") };
            foreach (string fileName in dangerousFiles)
            {
                string dangerousFilePath = this.CreatePathString(destinationPath, fileName);
                if (File.Exists(dangerousFilePath)) FileSystem.DeleteFile(dangerousFilePath);
            }
        }

        void MigrateStringsFromOriginalInf(string infName, string tempFolder,
            IniParser embeddedInfEditor)
        {
            this.CopyOrExpandFromArch(infName, tempFolder, false);
            // WARNING: Malformed lines detection disabled. MS poorly codes their INFs.
            IniParser originalInfEditor = new IniParser(
                this.CreatePathString(tempFolder, infName), false);
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
        // Backend parameters
        protected BackendParams Parameters 
        {
            get { return (BackendParams)this._state; }
        }

        // Other variables
        string _externalInfFilename;
        string _externalCabFilename;

        // SimpleINIEditor instances
        IniParser _entriesCombinedEditor;
        IniParser _wmp11ExtInfEditor;
        IniParser _wmp11InfEditor;

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

        // Which CAB to extract / repack (only for 2k3/x64 so far)
        string _driverCabFile;
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

        #region Exceptions
        public static class Exceptions
        {
            public class InvalidCabinetArchiveException
                : IntegrationException
            {
                static string s_DefaultMessage = Msg.errInvalidSfxCab;

                public InvalidCabinetArchiveException(string filePath) 
                    : base(s_DefaultMessage)
                {
                    base.Data.Add("Filename", filePath);
                }
            }
        }
        #endregion
    }
}
