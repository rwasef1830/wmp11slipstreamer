using System;
using System.Collections.Generic;
using System.IO;
using Epsilon.IO;
using Epsilon.Parsers;
using Epsilon.Slipstreamers;

namespace Epsilon.WMP11Slipstreamer
{
    partial class Backend
    {
        void IntegrateKB913800(string fixesFolder)
        {
            string kbName = "KB913800";
            string[] kbFiles = new string[] { 
                "EasyCD.inf", "KB913800.exe", "Wpdtrace.dll" };

            Archival.NativeCabinetExtract(new MemoryStream(
                Properties.Resources.KB913800), fixesFolder);

            foreach (string file in kbFiles)
            {
                FileSystem.MoveFileOverwrite(
                    this.CreatePathString(fixesFolder, file),
                    this.CreatePathString(this._extractDir, file)
                );
            }
            if (!this._params.IgnoreCats)
            {
                this.AddSvcpackCatalog(this.CreatePathString(
                    "Update", kbName + ".cat"), fixesFolder);
            }
        }

        void ProcessKB926239(string tempCompareFolder, string catalogFolder)
        {
            string kbName = "KB926239";
            string[] kbFiles = new string[] {
                "acadproc.dll", "apph_sp.sdb", "apphelp.sdb", "sysmain.sdb"
            };
            bool allFourExist = this.FilesExistInArch(kbFiles, 0);
            bool onlyThreeExist = this.FilesExistInArch(kbFiles, 1);

            KeyValuePair<string, string> acadprocTxtPair =
                new KeyValuePair<string, string>("acadproc.dll", "100,,,,,,,60,0,0");
            string acadprocDosLine = "d1,acadproc.dll";

            int counter = 0;

            foreach (string file in kbFiles)
            {
                string extractedName = this.CreatePathString(
                    this._extractDir, file);
                string tempCompareName = this.CreatePathString(
                    tempCompareFolder, file);
                bool isAcadProc = String.Equals(file, "acadproc.dll",
                            StringComparison.OrdinalIgnoreCase);
                bool resultOfCopyOrExpand =
                    this.CopyOrExpandFromArch(file, tempCompareFolder, true);
                if (resultOfCopyOrExpand || isAcadProc)
                {
                    FileVersionComparison result
                        = CompareVersions(
                            extractedName,
                            tempCompareName);
                    if ((result == FileVersionComparison.Newer && onlyThreeExist)
                        || (result == FileVersionComparison.NotFound
                        && isAcadProc && onlyThreeExist))
                    {
                        if (isAcadProc)
                        {
                            if (!base._txtsetupSif.KeyExists(
                                "SourceDisksFiles", file))
                            {
                                base._txtsetupSif.Add("SourceDisksFiles",
                                    acadprocTxtPair.Key, acadprocTxtPair.Value, 
                                    IniParser.KeyExistsPolicy.Ignore);
                            }
                            this._dosnetInf.Add("Files", acadprocDosLine, false);
                            if (!this._filesToCompressInArch.ContainsKey("acadproc.dll")
                                && !resultOfCopyOrExpand)
                            {
                                this._filesToCompressInArch.Add(
                                    "acadproc.dll",
                                    "acadproc.dll");
                            }
                        }
                        counter++;
                    }
                    else
                    {
                        FileSystem.Delete(extractedName);
                    }
                }
                else
                {
                    FileSystem.Delete(extractedName);
                }
            }
            if (counter > 0 && !this._params.IgnoreCats)
            {
                this.AddSvcpackCatalog(this.CreatePathString(
                    "Update", kbName + ".cat"), 
                    this._extractDir);
            }
        }
    }
}