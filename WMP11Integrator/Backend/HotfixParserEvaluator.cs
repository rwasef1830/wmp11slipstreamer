using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Epsilon.Parsers;
using Epsilon.WindowsModTools;
using Epsilon.IO;
using System.Diagnostics;
using Epsilon.DebugServices;

namespace Epsilon.Slipstreamers.WMP11Slipstreamer
{
    class HotfixParserEvaluator
    {
        #region Private members
        string _extractedPath;
        StringBuilder _hotfixFilePathBuffer;
        WindowsSourceInfo _sourceInfo;
        #endregion

        public HotfixParserEvaluator(string extractedPath, WindowsSourceInfo sourceInfo)
        {
            this._extractedPath = extractedPath;
            this._hotfixFilePathBuffer = new StringBuilder();
            this._sourceInfo = sourceInfo;
        }

        public bool EvaluateCondition(CSVParser csvParser,
            HotfixInfParser.OperationType op, List<string> destFolderOrRootKeyComponents,
            string subKeyOrFilename, Version viConditional, int compareValue)
        {
            HelperConsole.RawWriteLine(String.Empty);
            HelperConsole.InfoWriteLine("Conditional evaluator invoked", "Backend");
            HelperConsole.InfoWriteLine(op.ToString(), "Op");
            HelperConsole.RawWriteLine(String.Empty);

            if (CM.OperationTypeContains(op,
                HotfixInfParser.OperationType.RegistryPresentOrComparison))
            {
                bool finalResult = false;
                HelperConsole.RawWrite("Returning ");
                HelperConsole.RawWriteLine(finalResult.ToString());
                return finalResult;
            }
            else
            {
                // TODO: This will break when a conditional x64 fix comes out.
                // The solution is a way to reach the source file to figure
                // out whether it is 64-bit or 32-bit
                string sourceFilePath 
                    = MapInfFilePathToSourceFilePath(subKeyOrFilename, false);
                HelperConsole.InfoWriteLine(String.Format("\"{0}\"", sourceFilePath), 
                    "Mapped");

                op &= ~HotfixInfParser.OperationType.FilePresentOrVersionComparison;

                if (op == HotfixInfParser.OperationType.Exists)
                {
                    HelperConsole.InfoWriteLine("File exists, returning true.", "Evaluator");
                    return true;
                }
                else 
                {
                    FileVersionInfo fileVerInfo 
                        = FileVersionInfo.GetVersionInfo(sourceFilePath);
                    Version verInfo = new Version(fileVerInfo.FileMajorPart,
                        fileVerInfo.FileMinorPart, fileVerInfo.FileBuildPart,
                        fileVerInfo.FilePrivatePart);
                    int result = verInfo.CompareTo(viConditional);
                    HelperConsole.InfoWriteLine(String.Format(
                        "Source Version: {0}; Conditional Version: {1}",
                        verInfo.ToString(4), viConditional.ToString(4)), "Evaluator");

                    bool finalResult;
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
                        throw new InvalidOperationException(String.Format(
                            "Invalid operation type specified: '{0}'",
                            Enum.GetName(typeof(HotfixInfParser.OperationType), 
                            op)));
                    }

                    HelperConsole.InfoWriteLine(String.Format("Returning {0}.", 
                        finalResult), "Evaluator");
                    return finalResult;
                }
            }
        }

        string MapInfFilePathToSourceFilePath(string filename, bool is64bitFile)
        {
            string possPath = this.CreatePathString(this._extractedPath, filename);
            string poss64Path = this.CreatePathString(this._extractedPath, "amd64", filename);
            string poss32Path = this.CreatePathString(this._extractedPath, "i386", filename);

            if (is64bitFile)
            {
                // 64-bit file in x64 architecture
                if (File.Exists(poss64Path)) return poss64Path;
                else if (File.Exists(possPath)) return possPath;
                else ThrowMapFailed(filename, "[64in64]");
            }
            else if (this._sourceInfo.Arch == TargetArchitecture.x64)
            {
                // 32-bit file in x64 architecture
                if (File.Exists(poss32Path)) return poss32Path;
                else if (File.Exists(possPath)) return possPath;
                else ThrowMapFailed(filename, "[32in64]");
            }
            else if (File.Exists(possPath)) return possPath;
            else ThrowMapFailed(filename, "[32]");

            // Should never reach here, this is just to shut the compiler up
            return null;
        }

        static void ThrowMapFailed(string filename, string mapType)
        {
            throw new InvalidOperationException(String.Format(
                "Map {0} failed: \"{1}\", unable to locate file in source files.",
                mapType, filename));
        }

        string CreatePathString(params string[] components)
        {
            return FileSystem.CreatePathString(this._hotfixFilePathBuffer, components);
        }
    }
}
