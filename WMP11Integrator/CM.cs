using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using Epsilon.Win32.Resources;
using Epsilon.Win32;
using Epsilon.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Security.Cryptography;
using Epsilon.IO;

namespace WMP11Slipstreamer
{
    /// <summary>
    /// Subroutines commonly used by this program
    /// </summary>
    public static class CM
    {
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

                BinaryReader bReader = new BinaryReader(fileStream);
                uint reserved = 0;
                uint cabSize = 0;

                while (seqPos > 0 && seqPos + 8 < fileLength)
                {
                    fileStream.Seek(seqPos + sequence.LongLength, SeekOrigin.Begin);
                    cabSize = bReader.ReadUInt32();
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

        internal static bool OperationTypeContains(
            HotfixInfParser.OperationType combined, 
            HotfixInfParser.OperationType singleFlag)
        {
            return ((combined & singleFlag) == singleFlag);
        }

        /// <summary>
        /// Checks if a file is newer than the other. For EXEs and DLLs that have
        /// the FileVersionInfo structure, their version is used, otherwise, thier last
        /// modified date is used.
        /// </summary>
        /// <param name="fileToCompareAgainst">File to compare against</param>
        /// <param name="fileToCompareTo">File to compare to the first</param>
        /// <returns>FileVersionComparison result</returns>
        public static FileVersionComparison CompareVersions(
            string fileToCompareAgainst, 
            string fileToCompareTo)
        {
            FileVersionInfo version1 
                = FileVersionInfo.GetVersionInfo(fileToCompareAgainst);
            if (!File.Exists(fileToCompareTo))
                return FileVersionComparison.NotFound;
            FileVersionInfo version2
                = FileVersionInfo.GetVersionInfo(fileToCompareTo);
            int? result = null;
            if (version1.FileVersion == null || version2.FileVersion == null)
            {
                DateTime version1DateTime 
                    = File.GetLastWriteTimeUtc(fileToCompareAgainst);
                DateTime version2DateTime 
                    = File.GetLastWriteTimeUtc(fileToCompareTo);
                result = version1DateTime.CompareTo(version2DateTime);
            }
            else
            {
                Version vFirst 
                    = new Version(version1.FileMajorPart, version1.FileMinorPart, 
                    version1.FileBuildPart, version1.FilePrivatePart);
                Version vSecond
                    = new Version(version2.FileMajorPart, version2.FileMinorPart,
                    version2.FileBuildPart, version2.FilePrivatePart);
                result = vFirst.CompareTo(vSecond);
            }
            if (result.HasValue)
            {
                if (result == 0)
                    return FileVersionComparison.Same;
                else if (result < 0)
                    return FileVersionComparison.Older;
                else if (result > 0)
                    return FileVersionComparison.Newer;
            }
            return FileVersionComparison.Error;
        }
    

        /// <summary>
        /// String comparer function using current culture
        /// </summary>
        /// <param name="stringA">1st string</param>
        /// <param name="stringB">2nd string</param>
        /// <param name="caseInsensitive">True for case-insensitive compare</param>
        /// <returns>True if equal</returns>
        public static bool SEqCC(string stringA, string stringB,
            bool caseInsensitive)
        {
            if (caseInsensitive)
            {
                return String.Compare(stringA, stringB,
                    StringComparison.CurrentCultureIgnoreCase) == 0;
            }
            else
            {
                return String.Compare(stringA, stringB,
                    StringComparison.CurrentCulture) == 0;
            }
        }

        /// <summary>
        /// String comparer function using byte to byte comparison (faster)
        /// </summary>
        /// <param name="stringA">1st string</param>
        /// <param name="stringB">2nd string</param>
        /// <param name="caseInsensitive">True for case-insensitive compare</param>
        /// <returns>True if equal</returns>
        public static bool SEqO(string stringA, string stringB,
            bool caseInsensitive)
        {
            if (caseInsensitive)
            {
                return String.Compare(stringA, stringB,
                    StringComparison.OrdinalIgnoreCase) == 0;
            }
            else
            {
                return String.Compare(stringA, stringB,
                    StringComparison.Ordinal) == 0;
            }
        }

        /// <summary>
        /// Opens a standard win32 file open dialog for single file selection
        /// </summary>
        /// <param name="title">Title of dialog</param>
        /// <param name="filter">Filetype filter: Use syntax: 'MyExtensions|*.ext1;*.ext2;*.ext3|MyExtension2|*.ext4'</param>
        /// <returns>Filename selected by user</returns>
        public static string OpenFileDialogStandard(string title, string filter)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = title;
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.Multiselect = false;
            dialog.Filter = filter;
            dialog.ShowHelp = false;
            dialog.ShowReadOnly = false;
            dialog.SupportMultiDottedExtensions = true;
            dialog.ValidateNames = true;
            dialog.ShowDialog();
            return dialog.FileName;
        }

        /// <summary>
        /// Opens a standard win32 file save dialog for single file selection
        /// </summary>
        /// <param name="title">Title of dialog</param>
        /// <param name="filter">Filetype filter: Use syntax: 'MyExtensions|*.ext1;*.ext2;*.ext3|MyExtension2|*.ext4'</param>
        /// <returns>Filename selected by user</returns>
        public static string SaveFileDialogStandard(string title, string filter)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = title;
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;
            dialog.Filter = filter;
            dialog.ShowHelp = false;
            dialog.SupportMultiDottedExtensions = true;
            dialog.ValidateNames = true;
            dialog.ShowDialog();
            return dialog.FileName;
        }

        /// <summary>
        /// Opens a standard win32 file open dialog for multi file selection
        /// </summary>
        /// <param name="title">Title of dialog</param>
        /// <param name="filter">Filetype filter: Use syntax: 'MyExtensions|*.ext1;*.ext2;*.ext3|MyExtension2|*.ext4'</param>
        /// <returns>String array of selected files</returns>
        public static string[] OpenFileDialogMulti(string title, string filter)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = title;
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.Multiselect = true;
            dialog.Filter = filter;
            dialog.ShowHelp = false;
            dialog.ShowReadOnly = false;
            dialog.SupportMultiDottedExtensions = true;
            dialog.ValidateNames = true;
            dialog.ShowDialog();
            return dialog.FileNames;
        }
        
        /// <summary>
        /// Opens a standard win32 choose folder dialog
        /// </summary>
        /// <param name="title">Text to display above treeview</param>
        /// <param name="showMakeNewFolderButton">Show "Make New Folder" button</param>
        /// <returns>Selected folder path</returns>
        public static string OpenFolderDialog(string title, 
            bool showMakeNewFolderButton)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = title;
            dialog.ShowNewFolderButton = false;
            dialog.RootFolder = Environment.SpecialFolder.MyComputer;
            dialog.ShowNewFolderButton = showMakeNewFolderButton;
            dialog.ShowDialog();
            return dialog.SelectedPath;
        }

        /// <summary>
        /// Calculates the md5 hash of the given file
        /// </summary>
        /// <param name="filename">Full path to the file to calculate the hash for</param>
        /// <returns>md5 string</returns>
        public static string MD5(string filename)
        {            
            StringBuilder sb = new StringBuilder(32);
            FileStream fs = new FileStream(filename, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] hash = md5.ComputeHash(fs);
            fs.Close();
            foreach (byte hex in hash)
                sb.Append(hex.ToString("x2"));
            return sb.ToString();
        }

        public static string GetCompressedFileName(string filePath)
        {
            string extension = Path.GetExtension(filePath);
            string compressedFile = Path.ChangeExtension(filePath,
                extension.Substring(0, extension.Length - 1) + "_");
            return compressedFile;
        }

        public static Version VerFromFileVer(FileVersionInfo fileVerInfo)
        {
            return new Version(fileVerInfo.FileMajorPart, fileVerInfo.FileMinorPart, fileVerInfo.FileBuildPart,
                fileVerInfo.FilePrivatePart);
        }
    }

    /// <summary>
    /// Enumeration for the file version comparison results
    /// </summary>
    public enum FileVersionComparison
    {
        /// <summary>
        /// The files have identical dates or versions
        /// </summary>
        Same,
        /// <summary>
        /// The first file is newer than the second
        /// </summary>
        Newer,
        /// <summary>
        /// The first file is older than the second
        /// </summary>
        Older,
        /// <summary>
        /// The second file was not found
        /// </summary>
        NotFound,
        /// <summary>
        /// An unknown error occurred
        /// </summary>
        Error
    }
}
