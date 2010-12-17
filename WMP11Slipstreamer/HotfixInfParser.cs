using System;
using System.Collections.Generic;
using Epsilon.Parsers;
using Epsilon.Slipstreamers;

namespace Epsilon.WMP11Slipstreamer
{
    class HotfixInfParser
    {
        #region Fields
        readonly ConditionalOpExecuterDelegate _conditionOpTester;
        readonly CSVParser _csvParser;
        readonly IniParser _updateInf;
        readonly IDictionary<string, string> _variableDict;
        WindowsSourceInfo _sourceInfo;
        #endregion

        #region Public properties
        public readonly Dictionary<string, string> Catalogs;
        public readonly List<FileListSection> FileList;
        public readonly string HotfixName;
        public readonly string ProductName;
        #endregion

        #region Private constants
        const int c_NumOpLineValues = 5;
        #endregion

        public HotfixInfParser(
            IniParser updateInfEditor,
            ConditionalOpExecuterDelegate conditionExecuter,
            WindowsSourceInfo sourceInfo)
        {
            this._updateInf = updateInfEditor;
            this._csvParser = new CSVParser();
            this._conditionOpTester = conditionExecuter;
            this.FileList = new List<FileListSection>();
            this._sourceInfo = sourceInfo;

            if (updateInfEditor == null)
            {
                throw new ArgumentException(
                    "Argument cannot be null.",
                    updateInfEditor.ToString());
            }
            
            if (conditionExecuter == null)
            {
                throw new ArgumentException(
                    "Argument cannot be null.",
                    conditionExecuter.ToString());
            }

            this.HotfixName = this._updateInf.ReadValue(
                Strings.StringsSection,
                Strings.SpShortTitle,
                true);
            this.ProductName = this._updateInf.ReadValue(
                Strings.StringsSection,
                Strings.UsProductName,
                true);

            this.Catalogs = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            this._variableDict = this._updateInf.ReadSectionJoinedValues(
                Strings.StringsSection);

            if (this._updateInf.SectionExists(Strings.ProdCatsToInstall))
            {
                List<string[]> catCsv = this._updateInf.ReadCsvLines(
                    Strings.ProdCatsToInstall, 2, this._variableDict);
                foreach (var value in catCsv)
                {
                    this.Catalogs.Add(
                        value[0],
                        value[1] ?? value[0]);
                }
            }

            var copyFilesSections = new List<string>();
            copyFilesSections.AddRange(this.GetMandatoryCopyFilesSections());
            copyFilesSections.AddRange(this.GetOptionalCopyFilesSections());
            this.GetFileEntries(copyFilesSections);
        }

        void GetFileEntries(IEnumerable<string> fileListSections)
        {
            foreach (string copyFileSection in fileListSections)
            {
                List<string> destDirComponents = this._updateInf.ReadAllValues(
                    Strings.DestinationDirs, copyFileSection, this._variableDict);

                // x64 32-bit files flag
                bool is6432FileList = false;

                // Skip dllcache file list section
                if (SEqOIC(destDirComponents[0], "65619")) continue;

                // Check if we are an x64 source and we found a 32-bit file-list
                if (SEqOIC(destDirComponents[0], "10")
                    && SEqOIC(destDirComponents[1], "SysWOW64"))
                {
                    switch (this._sourceInfo.Arch)
                    {
                        case TargetArchitecture.x86:
                            throw new InvalidOperationException(
                                "Hotfix target architecture mismatch.");
                        default:
                            is6432FileList = true;
                            break;
                    }
                }

                var files = new Dictionary<string, string>(
                    StringComparer.OrdinalIgnoreCase);
                List<string[]> copyFileData
                    = this._updateInf.ReadCsvLines(
                        copyFileSection,
                        4,
                        this._variableDict);

                foreach (var copyFile in copyFileData)
                {
                    string sourceFileName = copyFile[1];
                    string destinationFileName = copyFile[0];

                    if (String.IsNullOrEmpty(sourceFileName))
                        sourceFileName = destinationFileName;

                    if (!files.ContainsKey(sourceFileName))
                    {
                        files.Add(sourceFileName, destinationFileName);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "Duplicate file specification in file list section [{0}]; "
                                + "File \"{1}\" already exists in dictionary. File: \"{2}\"",
                                copyFileSection,
                                sourceFileName,
                                this._updateInf.IniFileInfo.Name)
                            );
                    }
                }
                this.FileList.Add(
                    new FileListSection(
                        is6432FileList,
                        destDirComponents,
                        files));
            }
        }

        IEnumerable<string> GetMandatoryCopyFilesSections()
        {
            if (this._updateInf.SectionExists(Strings.PICopyFilesAlways))
                return this.GetFileListSections(Strings.PICopyFilesAlways);
            return new List<string>();
        }

        IEnumerable<string> GetOptionalCopyFilesSections()
        {
            var finalOutput = new List<string>();
            if (this._updateInf.SectionExists(Strings.PIReplaceIfExisting))
                finalOutput.AddRange(this.GetFileListSections(Strings.PIReplaceIfExisting));
            if (this._updateInf.SectionExists(Strings.PIExtendedCommands))
            {
                finalOutput.AddRange(this.ParseExtConditions());
            }
            return finalOutput;
        }

        IEnumerable<string> ParseExtConditions()
        {
            List<string> conditions = this._updateInf.ReadAllValues(
                Strings.PIExtendedCommands,
                Strings.ConditionInstall,
                this._variableDict);
            var fileListSections = new List<string>(conditions.Count);

            foreach (string conditionalSection in conditions)
            {
                List<string> conditionsLinesCombined = this._updateInf.ReadAllValues(
                    conditionalSection, Strings.Condition, this._variableDict);
                string conditionalEntryPoint =
                    this._updateInf.ReadValue(
                        conditionalSection,
                        Strings.ConditionalOperations);
                if (String.IsNullOrEmpty(conditionalEntryPoint))
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Conditional section [{0}] does not have a setup "
                            + "entry point.",
                            conditionalSection));
                }

                if (conditionsLinesCombined.Count == 0)
                {
                    throw new InvalidOperationException(
                        String.Format(
                            "Conditional operations list section [{0}] contains no "
                            + "defined condition statements.",
                            conditionalSection));
                }

                bool executeConditionalEntryPoint = true;
                for (int i = 0; i < conditionsLinesCombined.Count; i += 2)
                {
                    string opType = conditionsLinesCombined[i];
                    string opSection = conditionsLinesCombined[i + 1];

                    if (!this._updateInf.SectionExists(opSection))
                    {
                        throw new InvalidOperationException(
                            String.Format(
                                "[{0}] references op-section [{1}] which does "
                                + "not exist.",
                                conditionalSection,
                                opSection));
                    }

                    bool resultOfConditionLine = this.ProcessOpSection(opSection, opType);
                    executeConditionalEntryPoint &= resultOfConditionLine;

                    if (!executeConditionalEntryPoint) break;
                }

                if (executeConditionalEntryPoint)
                {
                    List<string> installSections = this._updateInf.ReadAllValues(
                        conditionalEntryPoint,
                        Strings.FileOperation,
                        this._variableDict);
                    foreach (string section in installSections)
                    {
                        fileListSections.AddRange(
                            this.GetFileListSections(section));
                    }
                }
            }

            return fileListSections;
        }

        bool ProcessOpSection(string opSection, string opType)
        {
            ICollection<KeyValuePair<string, string>> opSectionData =
                this._updateInf.ReadSectionJoinedValues(opSection, null, true);

            bool conditionLinesResult = SEqOIC(opType, Strings.opType_And);
            foreach (var pair in opSectionData)
            {
                bool lineResult = this.ProcessOpLine(pair.Value);

                if (SEqOIC(opType, Strings.opType_Single))
                    conditionLinesResult = lineResult;
                else if (SEqOIC(opType, Strings.opType_Or))
                    conditionLinesResult |= lineResult;
                else if (SEqOIC(opType, Strings.opType_And))
                    conditionLinesResult &= lineResult;
                else
                    ThrowNotImplemented(opType);

                if (!conditionLinesResult) break;
            }

            return conditionLinesResult;
        }

        bool ProcessOpLine(string line)
        {
            string[] opArguments = this._csvParser.Parse(
                line,
                c_NumOpLineValues,
                this._variableDict);
            string operation = opArguments[0];

            if (String.IsNullOrEmpty(operation))
            {
                throw new InvalidOperationException(
                    String.Format(
                        "No operation specified in conditional operation line [{0}].",
                        line));
            }

            if (SEqOIC(operation, Strings.opCmd_CheckReg))
            {
                // TODO: Need more examples before implementing

                //string rootKey = opArguments[1];
                //string subKey = opArguments[2];
                //string valueName = opArguments[3]; // Is this correct ?
                //string valueType = opArguments[4]; //
                // ...... need more info ....... 

                ThrowNotImplemented(operation);
                return false;
            }
            
            if (SEqOIC(operation, Strings.opCmd_CheckFileVer))
            {
                string copyFileSection = opArguments[1];
                string fileName = opArguments[2];
                OperationType op =
                    OperationType.FilePresentOrVersionComparison
                    | GetOperationType(opArguments[3]);
                Version fileVersion = (op != (OperationType.FilePresentOrVersionComparison
                                              | OperationType.Exists))
                                          ? new Version(opArguments[4])
                                          : null;
                // FIXED: Implement ReadAllValues overload with support for vars
                List<string> filePathComponents;
                if (!this._updateInf.TryReadAllValues(
                    Strings.DestinationDirs,
                    copyFileSection,
                    this._variableDict,
                    out filePathComponents))
                {
                    filePathComponents = null;
                }
                return this._conditionOpTester(
                    this._csvParser,
                    op,
                    filePathComponents,
                    fileName,
                    fileVersion,
                    0);
            }

            ThrowNotImplemented(operation);
            return false;
        }

        IEnumerable<string> GetFileListSections(string section)
        {
            return this._updateInf.ReadAllValues(
                section,
                Strings.CopyFiles,
                this._variableDict);
        }

        #region Private static methods
        static bool SEqOIC(string first, string second)
        {
            if (first == null || second == null) return false;
            return String.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        }

        static void ThrowNotImplemented(string operationLine)
        {
            throw new NotImplementedException(
                String.Format(
                    "This operation has not been implemented: [{0}]",
                    operationLine)
                );
        }
        #endregion

        #region Public static methods
        internal static bool OperationTypeContains(
            OperationType combined,
            OperationType singleFlag)
        {
            return ((combined & singleFlag) == singleFlag);
        }
        #endregion

        #region Callback plumbing

        #region Delegates
        public delegate bool ConditionalOpExecuterDelegate(CSVParser csvParser,
                                                           OperationType op, List<string> destFolderOrRootKeyComponents,
                                                           string subKeyOrFilename, Version fileVersionInfo,
                                                           int compareValue);
        #endregion

        [Flags]
        public enum OperationType
        {
            None = 0,

            FilePresentOrVersionComparison = 0x1000,
            RegistryPresentOrComparison = 0x2000,

            Exists = 0x10,
            Greater = 0x20,
            Less = 0x30,
            Equal = 0x40
        }

        static OperationType GetOperationType(string opSymbol)
        {
            switch (opSymbol)
            {
                case ">":
                    return OperationType.Greater;

                case ">=":
                    return OperationType.Greater | OperationType.Equal;

                case "<":
                    return OperationType.Less;

                case "<=":
                    return OperationType.Less | OperationType.Equal;

                case "=":
                case "==":
                    return OperationType.Equal;

                case "":
                case null:
                    return OperationType.Exists;

                default:
                    throw new InvalidCastException(
                        String.Format("Invalid comparison symbol: '{0}'", opSymbol)
                        );
            }
        }
        #endregion

        #region Subclasses

        #region Nested type: FileListSection
        public class FileListSection
        {
            public readonly IDictionary<string, string> FileDictionary;
            public readonly IEnumerable<string> InfDestDirComponents;
            public readonly bool Is64Bit32BitFileList;

            public FileListSection(
                bool is6432Files, IEnumerable<string> infDestDir,
                IDictionary<string, string> fileDict)
            {
                this.Is64Bit32BitFileList = is6432Files;
                this.InfDestDirComponents = infDestDir;
                this.FileDictionary = fileDict;
            }
        }
        #endregion

        #region Nested type: Strings
        /// <summary>
        /// Strings used in this class
        /// </summary>
        static class Strings
        {
            public const string ProdCatsToInstall = "ProductCatalogsToInstall";
            public const string StringsSection = "Strings";
            public const string SpShortTitle = "SP_SHORT_TITLE";
            public const string UsProductName = "US_PRODUCT_NAME";

            public const string PICopyFilesAlways
                = "ProductInstall.CopyFilesAlways";

            public const string PIExtendedCommands
                = "ProductInstall.ExtendedConditional";

            public const string PIReplaceIfExisting
                = "ProductInstall.ReplaceFilesIfExist";

            public const string CopyFiles = "CopyFiles";
            public const string DestinationDirs = "DestinationDirs";
            public const string ConditionInstall = "ConditionInstall";
            public const string ConditionalOperations = "ConditionalOperations";
            public const string Condition = "condition";
            public const string FileOperation = "FileOperation";

            public const string opType_Single = "SingleOp";
            public const string opType_Or = "OrOp";
            public const string opType_And = "AndOp";

            public const string opCmd_CheckFileVer = "CheckFilever";
            public const string opCmd_CheckReg = "CheckReg";
        }
        #endregion

        #endregion
    }
}