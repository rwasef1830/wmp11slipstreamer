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

namespace Epsilon.WMP11Slipstreamer
{
    /// <summary>
    /// Subroutines commonly used by this program
    /// </summary>
    public static class CM
    {
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
        /// <param name="filter">Filetype filter, Syntax: 
        /// 'MyExtensions|*.ext1;*.ext2;*.ext3|MyExtension2|*.ext4'</param>
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
        /// <param name="filter">Filetype filter, Syntax: 
        /// 'MyExtensions|*.ext1;*.ext2;*.ext3|MyExtension2|*.ext4'</param>
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
        /// <param name="filter">Filetype filter, syntax: 
        /// 'MyExtensions|*.ext1;*.ext2;*.ext3|MyExtension2|*.ext4'</param>
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
            return new Version(fileVerInfo.FileMajorPart, fileVerInfo.FileMinorPart, 
                fileVerInfo.FileBuildPart, fileVerInfo.FilePrivatePart);
        }

        public static void LaunchInDefaultHandler(string documentOrUrl)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = documentOrUrl;
            startInfo.Verb = "open";
            startInfo.UseShellExecute = true;
            Process.Start(startInfo);
        }
    }
}
