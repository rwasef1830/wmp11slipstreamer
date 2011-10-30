using System;
// using System.Text;
using System.IO;
// using System.Diagnostics;
// using System.Collections.Generic;
using Epsilon.IO.Compression;
using Epsilon.IO.Compression.Cabinet;
using Epsilon.Win32.Resources;
using Epsilon.Win32;
using System.Text;
using Epsilon.DebugServices;
using System.Diagnostics;
// using Epsilon.Collections.Specialized;

namespace Epsilon.IO
{
    public static class Archival
    {
        // Note to self: This has been optimized now and should work much
        // better than before in terms of maintainability, and slightly
        // better in terms of performance (and .NET overhead)

        #region Archivers FileInfos
        //public static FileInfo SevenZipPath;
        //public static FileInfo RarPath;
        //public static FileInfo UnRarPath;
        #endregion

        #region Native Cabinet.dll calls
        /// <summary>
        /// Creates a cabinet containing a single file in MakeCab style
        /// (eg: Compress "Sysoc.Inf" to "Sysoc.In_") using native calls
        /// to Cabinet.dll.
        /// </summary>
        /// <param name="fullFilename">Full path to the file to 
        /// compress</param>
        /// <param name="destinationFolder">Destination of
        /// the compressed file</param>
        /// <returns>Full path to the created cabinet file.</returns>
        public static string NativeCabinetMakeCab(
            string fullFilename,
            string destinationFolder)
        {
            Debug.WriteLine(String.Format("{0} -> {1}", fullFilename, destinationFolder), 
                "NativeCabinetMakeCab");

            string filename = Path.GetFileName(fullFilename);
            string cabExtension = Path.GetExtension(filename);

            string cabFileName = GetCabinetCompressedFileName(filename);

            if (!File.Exists(fullFilename))
            {
                throw new FileNotFoundException("Could not locate input file.", 
                    fullFilename);
            }

            string destDir = null;
            if (!destinationFolder.EndsWith(
                Path.DirectorySeparatorChar.ToString()))
                destDir = destinationFolder + Path.DirectorySeparatorChar;
            else 
                destDir = destinationFolder;

            CabCompressor instance = null;
            try
            {
                string cabinetFilePath = destDir + cabFileName;
                instance = new CabCompressor(cabinetFilePath, int.MaxValue, 
                    int.MaxValue);
                instance.AddFile(fullFilename, FCI.CompressionLevel.Lzx21);
                instance.Close();
                return cabinetFilePath;
            }
            catch (Exception ex)
            {
                ThrowArchiveException(cabFileName, 
                    ArchiveProcessOperation.Create, ex);
            }

            return null;
        }

        /// <summary>
        /// Create a CAB file containing many files (CabArc-style)
        /// </summary>
        /// <param name="cabinetPath">Path to cabinet file to create</param>
        /// <param name="sourceFolder">Folder containing files to add</param>
        /// <param name="recursive">Add subfolders or not</param>
        /// <param name="compressionLevel">Compression Level</param>
        /// <param name="filenameCallback">Callback function to write
        /// to console or update a label</param>
        /// <param name="progressCallback">Callback function to update 
        /// a progress bar</param>
        public static void NativeCabinetCreate(
            string cabinetPath,
            string sourceFolder,
            bool recursive,
            FCI.CompressionLevel compressionLevel,
            CabCompressor.FilenameNotifyDelegate filenameCallback,
            CabCompressor.ProgressNotifyDelegate progressCallback)
        {
            NativeCabinetCreate(cabinetPath, sourceFolder, "*", true,
                compressionLevel, filenameCallback, progressCallback);
        }

        /// <summary>
        /// Create a CAB file containing many files (CabArc-style)
        /// </summary>
        /// <param name="cabinetPath">Path to cabinet file to create</param>
        /// <param name="sourceFolder">Folder containing files to add</param>
        /// <param name="fileFilter">File search filter (eg: *.exe)</param>
        /// <param name="recursive">Add subfolders or not</param>
        /// <param name="compressionLevel">Compression Level</param>
        /// <param name="filenameCallback">Callback function to write
        /// to console or update a label</param>
        /// <param name="progressCallback">Callback function to update 
        /// a progress bar</param>
        public static void NativeCabinetCreate(
            string cabinetPath, 
            string sourceFolder, 
            string fileFilter,
            bool recursive, 
            FCI.CompressionLevel compressionLevel, 
            CabCompressor.FilenameNotifyDelegate filenameCallback, 
            CabCompressor.ProgressNotifyDelegate progressCallback)
        {
            try
            {
                CabCompressor instance = new CabCompressor(cabinetPath);
                instance.OnFilenameNotify += filenameCallback;
                instance.OnProgressNotify += progressCallback;
                instance.AddFolder(sourceFolder, "*", compressionLevel,
                    true, true);
                instance.Close();
            }
            catch (Exception ex)
            {
                ThrowArchiveException(cabinetPath, 
                    ArchiveProcessOperation.Create, ex);
            }
        }
        
        /// <summary>
        /// Creates a CAB file containing many files (CabArc-style)
        /// </summary>
        /// <param name="cabinetPath">Path to cabinet file to create</param>
        /// <param name="fileList">List of files to add into the new cabinet</param>
        /// <param name="compressionLevel">Compression Level</param>
        /// <param name="filenameCallback">Callback function to write to console 
        /// or update a label</param>
        /// <param name="progressCallback">Callback function to update
        /// a progress bar</param>
        public static void NativeCabinetCreate(
            string cabinetPath, 
            string[] fileList, 
            string fileListAbsRoot,
            FCI.CompressionLevel compressionLevel, 
            CabCompressor.FilenameNotifyDelegate filenameCallback, 
            CabCompressor.ProgressNotifyDelegate progressCallback)
        {
            try
            {
                CabCompressor instance = new CabCompressor(cabinetPath);
                instance.OnFilenameNotify += filenameCallback;
                instance.OnProgressNotify += progressCallback;
                instance.AddFiles(fileList, compressionLevel, fileListAbsRoot);
                instance.Close();
            }
            catch (Exception ex)
            {
                ThrowArchiveException(cabinetPath, ArchiveProcessOperation.Create, ex);
            }
        }

        /// <summary>
        /// Extracts all the files in a cabinet
        /// </summary>
        /// <param name="cabFile">Full path to the cabinet to extract</param>
        /// <param name="destinationFolder">Folder to put the 
        /// extracted files</param>
        public static void NativeCabinetExtract(string cabFile,
            string destinationFolder)
        {
            NativeCabinetExtract(cabFile, destinationFolder, null, null);
        }

        /// <summary>
        /// Extracts all the files from a cabinet stored in stream
        /// </summary>
        /// <param name="stream">Cabinet stream to extract. It is closed automatically 
        /// when extraction is done or in case of an error.</param>
        /// <param name="destinationFolder">Folder to put the 
        /// extracted files</param>
        public static void NativeCabinetExtract(
            Stream stream,
            string destinationFolder)
        {
            NativeCabinetExtract(stream, destinationFolder, null, null);
        }

        /// <summary>
        /// Extracts all the files from a cabinet stored in stream
        /// </summary>
        /// <param name="stream">Cabinet stream to extract. It is closed automatically 
        /// when extraction is done or in case of an error.</param>
        /// <param name="destinationFolder">Folder to put the 
        /// extracted files</param>
        public static void NativeCabinetExtract(
            Stream stream,
            string destinationFolder, 
            CabDecompressor.FilenameNotifyDelegate filenameCallback,
            CabDecompressor.ProgressNotifyDelegate progressCallback)
        {
            CabDecompressor instance = null;
            try
            {
                instance = new CabDecompressor(destinationFolder);
                instance.OnFilenameNotify += filenameCallback;
                instance.OnProgressNotify += progressCallback;
                instance.Extract(stream, true);
                instance.Close();
            }
            catch (Exception ex)
            {
                ThrowArchiveException(stream.ToString(),
                    ArchiveProcessOperation.Extract, ex);
            }
            finally
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Extracts all the files in a cabinet with optional notification
        /// </summary>
        /// <param name="cabFile">Full path to the cabinet to extract</param>
        /// <param name="destinationFolder">Folder to put the 
        /// extracted files</param>
        /// <param name="filenameCallback">Callback function to write to console 
        /// or update a label</param>
        /// <param name="progressCallback">Callback function to update
        /// a progress bar</param>
        public static void NativeCabinetExtract(
            string cabFile,
            string destinationFolder,
            CabDecompressor.FilenameNotifyDelegate filenameCallback,
            CabDecompressor.ProgressNotifyDelegate progressCallback)
        {
            if (!File.Exists(cabFile))
                throw new FileNotFoundException(
                    String.Format("Cannot find \"{0}\" to extract.",
                    Path.GetFullPath(cabFile)), cabFile);
            CabDecompressor instance = null;
            try
            {
                instance = new CabDecompressor(destinationFolder);
                instance.OnFilenameNotify += filenameCallback;
                instance.OnProgressNotify += progressCallback;
                instance.Extract(cabFile);
                instance.Close();
            }
            catch (Exception ex)
            {
                ThrowArchiveException(cabFile, ArchiveProcessOperation.Extract, ex);
            }
        }

        /// <summary>
        /// Search the executable file for a cabinet stream,
        /// returns null if stream was not found.
        /// </summary>
        /// <param name="pathToSfxCab">path to the executable file</param>
        public static Stream GetCabStream(string pathToSfxCab)
        {
            ResourceEditor win32Res = new ResourceEditor(pathToSfxCab);
            byte[] cabData = null;
            if (win32Res.ReadRawResourceBytes(ResourceAPI.ResourceType.RcData,
                "CABINET", out cabData))
            {
                // Close the resource editor
                win32Res.Close();
                return new MemoryStream(cabData);
            }
            else
            {
                // Close the resource editor
                win32Res.Close();

                FileStream fileStream = File.Open(pathToSfxCab, FileMode.Open,
                    FileAccess.Read, FileShare.Read);
                long fileLength = fileStream.Length;

                string header = "MSCF\0\0\0\0";
                byte[] sequence = Encoding.ASCII.GetBytes(header);
                long seqPos = Streams.Search(fileStream, sequence);

                if (seqPos < 0)
                {
                    fileStream.Close();
                    return null;
                }

                BinaryReader bReader = new BinaryReader(fileStream);
                uint reserved = 0;

                while (seqPos > 0 && seqPos + 8 < fileLength)
                {
                    fileStream.Seek(seqPos + sequence.LongLength, SeekOrigin.Begin);
                    bReader.ReadUInt32();
                    reserved = bReader.ReadUInt32();
                    if (reserved == 0) break; // Valid cabinet header
                    else
                    {
                        // Not valid header, search again
                        seqPos = Streams.Search(fileStream, sequence);
                    }
                }

                if (reserved != 0)
                {
                    throw new InvalidDataException(
                        String.Format("The file \"{0}\" is not a valid hotfix",
                        pathToSfxCab));
                }
                else
                {
                    // Seek the stream again to the beginning of the 
                    // header to allow FDI to process it correctly
                    fileStream.Seek(seqPos, SeekOrigin.Begin);
                    return fileStream;
                }
            }
        }

        public static string GetCabinetCompressedFileName(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            string compressedExtension;

            if (extension == null) compressedExtension = "_";
            else if (extension.Length >= 4)
            {
                compressedExtension = extension.Substring(0, extension.Length - 1) + "_";
            }
            else
            {
                compressedExtension = extension + "_";
            }

            string compressedFile = Path.ChangeExtension(
                filePath,
                compressedExtension);

            return compressedFile;
        }
        #endregion

        #region 7-zip methods
        ///// <summary>
        ///// Extract a 7z that contains many files (used for extracting AddON 7z for testing)
        ///// </summary>
        ///// <param name="sevenZipArchive">7z File to uncompress (relative path or absolute)</param>
        ///// <param name="destinationFolder">Folder to put the resulting files in</param>
        //public static void SevenZipExtract(string sevenZipArchive, 
        //    string destinationFolder)
        //{
        //    Process sevenZipProcess = new Process();
        //    sevenZipProcess.StartInfo.FileName = SevenZipPath.FullName;
        //    sevenZipProcess.StartInfo.RedirectStandardOutput = false;
        //    sevenZipProcess.StartInfo.UseShellExecute = false;
        //    sevenZipProcess.StartInfo.WorkingDirectory 
        //        = Path.GetFullPath(destinationFolder);
        //    sevenZipProcess.StartInfo.Arguments =
        //        String.Format("x -o.\\ -y \"{0}\"", 
        //        Path.GetFullPath(sevenZipArchive));
        //    startLogExternalProcessInfo(sevenZipProcess);
        //    launchProcess(sevenZipProcess);
        //    endLogExternalProcessInfo(sevenZipProcess);
        //    if (sevenZipProcess.ExitCode != 0)
        //        throwArchiveException(
        //            sevenZipArchive, 
        //            ArchiveProcessOperation.Extract, 
        //            SevenZipPath.Name, 
        //            sevenZipProcess.ExitCode, null);
        //}

        ///// <summary>
        ///// Creates a 7z archive (solid = ON, Compress = Ultra)
        ///// </summary>
        ///// <param name="sourceFolder">Folder containing files to compress</param>
        ///// <param name="outputArchive">Full path of output 7z to create</param>
        //public static void SevenZipCreateArchive(string sourceFolder, 
        //    string outputArchive)
        //{
        //    Process sevenZipProcess = new Process();
        //    sevenZipProcess.StartInfo.FileName = SevenZipPath.FullName;
        //    sevenZipProcess.StartInfo.RedirectStandardOutput = false;
        //    sevenZipProcess.StartInfo.UseShellExecute = false;
        //    sevenZipProcess.StartInfo.WorkingDirectory 
        //        = Path.GetFullPath(sourceFolder);
        //    sevenZipProcess.StartInfo.Arguments
        //        = String.Format("a -r0 -t7z -ms=on -mx=9 -y \"{0}\" *", 
        //        Path.GetFullPath(outputArchive));
        //    startLogExternalProcessInfo(sevenZipProcess);
        //    launchProcess(sevenZipProcess);
        //    endLogExternalProcessInfo(sevenZipProcess);
        //    if (sevenZipProcess.ExitCode != 0)
        //        throwArchiveException(
        //            outputArchive,
        //            ArchiveProcessOperation.Create,
        //            SevenZipPath.Name,
        //            sevenZipProcess.ExitCode, null);
        //}
        #endregion

        #region Zip calls
        ///// <summary>
        ///// Creates a zip archive (Compress = Ultra)
        ///// </summary>
        ///// <param name="sourceFolder">Folder containing files to compress</param>
        ///// <param name="outputArchive">Full path of output zip to create</param>
        //public static void ZipCreateArchive(string sourceFolder, 
        //    string outputArchive)
        //{
        //    Process sevenZipProcess = new Process();
        //    sevenZipProcess.StartInfo.FileName = SevenZipPath.FullName;
        //    sevenZipProcess.StartInfo.RedirectStandardOutput = false;
        //    sevenZipProcess.StartInfo.UseShellExecute = false;
        //    sevenZipProcess.StartInfo.WorkingDirectory 
        //        = Path.GetFullPath(sourceFolder);
        //    sevenZipProcess.StartInfo.Arguments 
        //        = String.Format("a -r0 -tzip -mx=9 -y \"{0}\" *", 
        //        Path.GetFullPath(outputArchive));
        //    startLogExternalProcessInfo(sevenZipProcess);
        //    launchProcess(sevenZipProcess);
        //    endLogExternalProcessInfo(sevenZipProcess);
        //    if (sevenZipProcess.ExitCode != 0)
        //        throwArchiveException(
        //            outputArchive,
        //            ArchiveProcessOperation.Create,
        //            SevenZipPath.Name,
        //            sevenZipProcess.ExitCode, null);
        //}
        #endregion

        #region Rar calls
        ///// <summary>
        ///// Creates a RAR archive (solid = ON, compress = Ultra)
        ///// </summary>
        ///// <param name="sourceFolder">Folder containing files to compress</param>
        ///// <param name="outputArchive">Full path of output RAR to create</param>
        //public static void RarCreateArchive(string sourceFolder, string outputArchive)
        //{
        //    Process rarProcess = new Process();
        //    rarProcess.StartInfo.FileName = RarPath.FullName;
        //    rarProcess.StartInfo.RedirectStandardOutput = false;
        //    rarProcess.StartInfo.UseShellExecute = false;
        //    rarProcess.StartInfo.WorkingDirectory = 
        //        Path.GetFullPath(sourceFolder);
        //    rarProcess.StartInfo.Arguments =
        //        String.Format("a -r0 -m5 -rr -idp -s \"{0}\" *",
        //        Path.GetFullPath(outputArchive));
        //    startLogExternalProcessInfo(rarProcess);
        //    launchProcess(rarProcess);
        //    endLogExternalProcessInfo(rarProcess);
        //    if (rarProcess.ExitCode != 0)
        //        throwArchiveException(
        //            outputArchive,
        //            ArchiveProcessOperation.Create,
        //            RarPath.Name,
        //            rarProcess.ExitCode, null);
        //}

        ///// <summary>
        ///// Extract a RAR that contains many files (used for extracting AddON RAR for testing)
        ///// </summary>
        ///// <param name="sevenZipArchive">RAR File to uncompress (relative path or absolute)</param>
        ///// <param name="destinationFolder">Folder to put the resulting files in</param>
        //public static void UnRarExtract(string rarArchive,
        //    string destinationFolder)
        //{
        //    Process unRarProcess = new Process();
        //    unRarProcess.StartInfo.FileName = UnRarPath.FullName;
        //    unRarProcess.StartInfo.RedirectStandardOutput = false;
        //    unRarProcess.StartInfo.UseShellExecute = false;
        //    unRarProcess.StartInfo.WorkingDirectory
        //        = Path.GetFullPath(destinationFolder);
        //    unRarProcess.StartInfo.Arguments =
        //        String.Format("x -y \"{0}\" * .{1}",
        //        Path.GetFullPath(rarArchive), Path.DirectorySeparatorChar);
        //    startLogExternalProcessInfo(unRarProcess);
        //    launchProcess(unRarProcess);
        //    endLogExternalProcessInfo(unRarProcess);
        //    if (unRarProcess.ExitCode != 0)
        //        throwArchiveException(
        //            rarArchive,
        //            ArchiveProcessOperation.Extract,
        //            UnRarPath.Name,
        //            unRarProcess.ExitCode, null);
        //}
        #endregion

        #region Logging helper functions
        //static void startLogExternalProcessInfo(Process externalProcess)
        //{
        //    CM.LogWriteLine("-------------------------------------");
        //    CM.LogWriteLine("- CreateProcess(): \"{0}\"",
        //        externalProcess.StartInfo.FileName);
        //    CM.LogWriteLine("- Arguments: \"{0}\"", externalProcess.StartInfo.Arguments);
        //    CM.LogWriteLine("- Working Directory: \"{0}\"", 
        //        externalProcess.StartInfo.WorkingDirectory);
        //}

        //static void endLogExternalProcessInfo(Process externalProcess)
        //{
        //    CM.LogWriteLine("- CreateProcess(): Exit Code: {0}", externalProcess.ExitCode);
        //    CM.LogWriteLine("-------------------------------------");
        //}
        #endregion

        #region Archive process error handlers
        static void ThrowArchiveException(string archiveName,
            ArchiveProcessOperation opType, Exception innerException)
        {
            string exceptionLine = "An error occurred while {0} \"{1}\"";
            string operationText = null;
            switch (opType)
            {
                case ArchiveProcessOperation.Create:
                    operationText = "creating";
                    break;

                case ArchiveProcessOperation.Extract:
                    operationText = "extracting files from";
                    break;
            }
            throw new ArchiveProcessException(
                String.Format(exceptionLine, operationText, archiveName),
                innerException);
        }

        static void throwArchiveException(string archiveName,
            ArchiveProcessOperation opType, string externalProcessName, int ExitCode,
            Exception innerException)
        {
            string exceptionLine = "An error occurred while {0} \"{1}\", {2} exited with code {3}";
            string operationText = null;
            switch (opType)
            {
                case ArchiveProcessOperation.Create:
                    operationText = "creating";
                    break;

                case ArchiveProcessOperation.Extract:
                    operationText = "extracting files from";
                    break;
            }
            throw new ArchiveProcessException(
                String.Format(exceptionLine, operationText, 
                archiveName, externalProcessName, ExitCode),
                innerException);
        }

        enum ArchiveProcessOperation
        {
            Create,
            Extract
        }

        /// <summary>
        /// Definition of special exception to throw if an error occurs in any 
        /// of the methods of this part of the Program.cs class (specifically
        /// those that deal with archives (CAB / 7-Zip))
        /// </summary>
        [Serializable]
        public class ArchiveProcessException : Exception
        {
            public ArchiveProcessException(string message)
                : base(message) { }
            public ArchiveProcessException(string message, Exception
                innerException)
                : base(message, innerException) { }
        }
        #endregion

        #region Redirection helpers
        ///// <summary>
        ///// For providing automatic redirection of stdout 
        ///// and stderror to log file when logger is active
        ///// </summary>
        //static void launchProcess(Process commonProcess)
        //{
        //    if (Constants.Logger != null)
        //    {
        //        commonProcess.StartInfo.RedirectStandardOutput = true;
        //        commonProcess.StartInfo.RedirectStandardError = true;
        //        commonProcess.OutputDataReceived 
        //            += new DataReceivedEventHandler(commonProcess_DataReceived);
        //        commonProcess.ErrorDataReceived
        //            += new DataReceivedEventHandler(commonProcess_DataReceived);
        //        CM.LogWriteLine();
        //    }
        //    commonProcess.Start();
        //    if (Constants.Logger != null)
        //    {
        //        commonProcess.BeginOutputReadLine();
        //        commonProcess.BeginErrorReadLine();
        //    }
        //    commonProcess.WaitForExit();
        //}

        //static void commonProcess_DataReceived(object sender, 
        //    DataReceivedEventArgs e)
        //{
        //    CM.OutputLine(e.Data);
        //}
        #endregion
    }
}
