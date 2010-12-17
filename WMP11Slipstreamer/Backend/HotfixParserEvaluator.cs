using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Epsilon.DebugServices;
using Epsilon.IO;
using Epsilon.Parsers;
using Epsilon.Slipstreamers;

namespace Epsilon.WMP11Slipstreamer
{
    class HotfixParserEvaluator
    {
        #region Private members
        readonly string _extractedPath;
        readonly string _archPath;
        readonly StringBuilder _hotfixFilePathBuffer;
        WindowsSourceInfo _sourceInfo;
        #endregion

        public HotfixParserEvaluator(string extractedPath, string archPath, WindowsSourceInfo sourceInfo)
        {
            this._extractedPath = extractedPath;
            this._archPath = archPath;
            this._hotfixFilePathBuffer = new StringBuilder();
            this._sourceInfo = sourceInfo;
        }

        public bool EvaluateCondition(
            CSVParser csvParser,
            HotfixInfParser.OperationType op, List<string> destFolderOrRootKeyComponents,
            string subKeyOrFilename, Version viConditional, int compareValue)
        {
            HelperConsole.RawWriteLine(String.Empty);
            HelperConsole.InfoWriteLine("Conditional evaluator invoked", "Backend");
            HelperConsole.InfoWriteLine(op.ToString(), "Op");
            HelperConsole.RawWriteLine(String.Empty);

            bool finalResult;

            if (HotfixInfParser.OperationTypeContains(
                op,
                HotfixInfParser.OperationType.RegistryPresentOrComparison))
            {
                finalResult = false;
                HelperConsole.RawWrite("Returning ");
                HelperConsole.RawWriteLine(finalResult.ToString());
                return false;
            }
            // TODO: This will break when a conditional x64 fix comes out.
            // The solution is a way to reach the source file to figure
            // out whether it is 64-bit or 32-bit
            string sourceFilePath
                = this.MapInfFilePathToSourceFilePath(subKeyOrFilename, false);
            if (sourceFilePath == null)
            {
                return false;
            }

            HelperConsole.InfoWriteLine(
                String.Format("\"{0}\"", sourceFilePath),
                "Mapped");

            op &= ~HotfixInfParser.OperationType.FilePresentOrVersionComparison;

            if (op == HotfixInfParser.OperationType.Exists)
            {
                HelperConsole.InfoWriteLine("File exists, returning true.", "Evaluator");
                return true;
            }
            FileVersionInfo fileVerInfo
                = FileVersionInfo.GetVersionInfo(sourceFilePath);
            var verInfo = new Version(
                fileVerInfo.FileMajorPart,
                fileVerInfo.FileMinorPart,
                fileVerInfo.FileBuildPart,
                fileVerInfo.FilePrivatePart);
            int result = verInfo.CompareTo(viConditional);
            HelperConsole.InfoWriteLine(
                String.Format(
                    "Source Version: {0}; Conditional Version: {1}",
                    verInfo.ToString(4),
                    viConditional.ToString(4)),
                "Evaluator");

            if (op == HotfixInfParser.OperationType.Equal)
                finalResult = result == 0;
            else if (op == HotfixInfParser.OperationType.Greater)
                finalResult = result > 0;
            else if (op == (HotfixInfParser.OperationType.Greater
                            | HotfixInfParser.OperationType.Equal))
                finalResult = result >= 0;
            else if (op == HotfixInfParser.OperationType.Less)
                finalResult = result < 0;
            else if (op == (HotfixInfParser.OperationType.Less
                            | HotfixInfParser.OperationType.Equal))
                finalResult = result <= 0;
            else
            {
                throw new InvalidOperationException(
                    String.Format(
                        "Invalid operation type specified: '{0}'",
                        Enum.GetName(
                            typeof(HotfixInfParser.OperationType),
                            op)));
            }

            HelperConsole.InfoWriteLine(
                String.Format(
                    "Returning {0}.",
                    finalResult),
                "Evaluator");
            return finalResult;
        }

        string MapInfFilePathToSourceFilePath(string filename, bool is64bitFile)
        {
            string possPath = this.CreatePathString(this._extractedPath, filename);
            string poss64Path = this.CreatePathString(this._extractedPath, "amd64", filename);
            string poss32Path = this.CreatePathString(this._extractedPath, "i386", filename);
            string possArchPath = this.CreatePathString(this._archPath, filename);
            string possArch32Path = this.CreatePathString(Path.GetDirectoryName(this._archPath), "i386", "w" + filename);

            if (is64bitFile)
            {
                // 64-bit file in x64 architecture
                if (File.Exists(poss64Path)) return poss64Path;
                if (File.Exists(possPath)) return possPath;

                if (this.CopyOrExtract(possArchPath, Path.GetDirectoryName(possPath)))
                {
                    return possPath;
                }

                return null;
            }
            
            if (this._sourceInfo.Arch == TargetArchitecture.x64)
            {
                // 32-bit file in x64 architecture
                if (File.Exists(poss32Path)) return poss32Path;
                if (File.Exists(possPath)) return possPath;

                if (this.CopyOrExtract(possArch32Path, Path.GetDirectoryName(possPath)))
                {
                    string possPrefixedPath = this.CreatePathString(
                        Path.GetDirectoryName(possPath), "w" + filename);

                    if (File.Exists(possPrefixedPath))
                    {
                        File.Move(possPrefixedPath, possPath);
                    }

                    return possPath;
                }

                return null;
            }
            
            if (File.Exists(possPath)) return possPath;
            
            if (this.CopyOrExtract(possArchPath, Path.GetDirectoryName(possPath)))
            {
                return possPath;
            }

            // Should never reach here, this is just to shut the compiler up
            return null;
        }

        bool CopyOrExtract(string path, string destinationFolder)
        {
            var compressedPath = Archival.GetCabinetCompressedFileName(path);
            var destFilePath = this.CreatePathString(destinationFolder, Path.GetFileName(path));

            if (File.Exists(path))
            {
                File.Copy(path, destFilePath);
                return true;
            }

            if (File.Exists(compressedPath))
            {
                Archival.NativeCabinetExtract(compressedPath, destinationFolder);
                if (File.Exists(destFilePath)) return true;
            }

            return false;
        }

        string CreatePathString(params string[] components)
        {
            return FileSystem.CreatePathString(this._hotfixFilePathBuffer, components);
        }
    }
}
