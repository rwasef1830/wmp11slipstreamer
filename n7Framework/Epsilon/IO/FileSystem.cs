using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Epsilon.DebugServices;
using Epsilon.Win32;

namespace Epsilon.IO
{
    /// <summary>
    /// Contains commonly needed functions on the filesystem
    /// </summary>
    public static class FileSystem
    {
        public const int MaximumPathLength = 260;
        const int c_RetryDelayMs = 50;

        public static void UnsetReadonly(string path)
        {
            FileAttributes attributes = File.GetAttributes(path);
            attributes = attributes & ~FileAttributes.ReadOnly;
            File.SetAttributes(path, attributes);
        }

        public static void UnsetReadonly(FileSystemInfo path)
        {
            path.Attributes = path.Attributes & ~FileAttributes.ReadOnly;
        }

        /// <summary>
        /// Creates a temporary folder and returns its full path.
        /// If the folder exists, it is erased.
        /// </summary>
        /// <returns>Guaranteed path to an empty temporary folder</returns>
        public static string GetGuaranteedTempDirectory()
        {
            return GetGuaranteedTempDirectory(Path.GetTempPath());
        }

        /// <summary>
        /// Creates a temporary folder under the specified path and returns 
        /// its full path. If the folder exists, it is erased.
        /// </summary>
        /// <param name="tempFolder">Parent temp folder</param>
        /// <returns>Guaranteed path to an empty temporary folder</returns>
        public static string GetGuaranteedTempDirectory(string tempFolder)
        {
            if (!tempFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                tempFolder += Path.DirectorySeparatorChar;

            string tempDirectory = tempFolder + Path.GetRandomFileName();
            CreateEmptyDirectory(tempDirectory);
            return tempDirectory;
        }

        /// <summary>
        /// Creates several folders deleting previously existing
        /// </summary>
        public static void CreateEmptyDirectories(params string[] directories)
        {
            foreach (string directory in directories)
            {
                CreateEmptyDirectory(directory);
            }
        }

        /// <summary>
        /// Creates a folder deleting previously existing
        /// </summary>
        /// <param name="directory">Path to directory to create</param>
        public static void CreateEmptyDirectory(string directory)
        {
            if (Directory.Exists(directory)) DeleteFolder(directory);
            Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// Moves the contents of a folder to another location overwriting files
        /// in the destination that exist in the source
        /// </summary>
        /// <param name="srcFolder">Source folder</param>
        /// <param name="dstFolder">Destination folder. If it exists, files that
        /// already exist in it will be overwritten.</param>
        /// <param name="includeSubFolders">true to recursively copy including subfolders, 
        /// else it will copy only files in the 1st level only</param>
        public static void MoveFiles(
            string srcFolder,
            string dstFolder,
            bool includeSubFolders)
        {
            MoveFiles(srcFolder, dstFolder, includeSubFolders, MoveFileOverwrite);
        }

        /// <summary>
        /// Moves the contents of a folder to another location
        /// </summary>
        /// <param name="srcFolder">Source folder</param>
        /// <param name="destFolder">Destination folder. If it exists, files that
        /// already exist in it will be handled by overwriteHandler.</param>
        /// <param name="includeSubFolders">true to recursively move subfolders as well.</param>
        /// <param name="overwriteHandler">Callback that will be executed
        /// in case the destination path already exists.</param>
        public static void MoveFiles(
            string srcFolder, 
            string destFolder,
            bool includeSubFolders,
            MoveFileOverwriteDelegate overwriteHandler)
        {
            if (!Directory.Exists(destFolder)) Directory.CreateDirectory(destFolder);
            StringBuilder pathBuffer = new StringBuilder(MaximumPathLength);
            foreach (string file in Directory.GetFiles(srcFolder, "*", 
                SearchOption.TopDirectoryOnly))
            {
                string destPath = CreatePathString(pathBuffer, destFolder, Path.GetFileName(file));
                if (File.Exists(destPath)) overwriteHandler(file, destPath);
                else File.Move(file, destPath);
            }

            if (includeSubFolders)
            {
                foreach (string directory in Directory.GetDirectories(srcFolder, "*",
                    SearchOption.TopDirectoryOnly))
                {
                    MoveFiles(
                        directory,
                        CreatePathString(pathBuffer, destFolder, Path.GetFileName(directory)),
                        true
                    );
                }
            }
        }

        /// <summary>
        /// Moves a file from one place to another overwriting the
        /// destination if it already exists
        /// </summary>
        /// <param name="sourceFilePath">Source path</param>
        /// <param name="destFilePath">Destination path</param>
        public static void MoveFileOverwrite(string sourceFilePath,
            string destFilePath)
        {
            HelperConsole.InfoWriteLine(String.Format("\"{0}\" \"{1}\"",
                sourceFilePath, destFilePath), "MoveFileOverwrite");
            if (File.Exists(destFilePath))
            {
                DeleteFile(destFilePath);
            }
            File.Move(sourceFilePath, destFilePath);
        }

        /// <summary>
        /// Delegate that specifies the signature of the behavior method for
        /// the MoveFolder method
        /// </summary>
        /// <param name="sourceFilePath">File that is going to be moved</param>
        /// <param name="destFilePath">Destination folder</param>
        public delegate void MoveFileOverwriteDelegate(
            string sourceFilePath,
            string destFilePath
            );

        /// <summary>
        /// Deletes the specified folder.
        /// </summary>
        public static void DeleteFolder(string folderPath)
        {
            foreach (FileSystemEntry entry in WalkTree(folderPath))
            {
                if (entry.IsDirectory) DeleteFolder(entry.FullPath);
                else DeleteFile(entry.FullPath);
            }

            UnsetReadonly(folderPath);
            RetryAction<UnauthorizedAccessException>(
                Directory.Delete, folderPath, 3, "DeleteFolder");
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        public static void DeleteFile(string filePath)
        {
            if (!File.Exists(filePath)) return;

            UnsetReadonly(filePath);
            RetryAction<UnauthorizedAccessException>(
                File.Delete, filePath, 3, "DeleteFile");
        }

        public static IEnumerable<FileSystemEntry> WalkTree(string folderPath)
        {
            if (String.IsNullOrEmpty(folderPath))
                throw new ArgumentException("folderPath cannot be null or empty.");

            if (folderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                throw new ArgumentException("folderPath cannot end with a backslash");

            if (!Directory.Exists(folderPath))
            {
                throw new DirectoryNotFoundException(
                    String.Format("\"{0}\" does not exist.", folderPath));
            }

            string searchPath = folderPath + Path.DirectorySeparatorChar + "*";
            Win32.API.FindData findData = new Epsilon.Win32.API.FindData();
            using (SafeFindHandle findHandle 
                = Win32.API.IO.Safe.FindFirstFile(searchPath, findData))
            {
                if (findHandle.IsInvalid) throw new Win32Exception();
                else
                {
                    // FindNextFile again to get past ..
                    Win32.API.IO.Safe.FindNextFile(findHandle, findData);

                    while (Win32.API.IO.Safe.FindNextFile(findHandle, findData))
                    {
                        DateTime? createTime = (findData.ftCreationTime != 0) ?
                            DateTime.FromFileTimeUtc(findData.ftCreationTime) : (DateTime?)null;
                        DateTime? accessTime = (findData.ftLastAccessTime != 0) ?
                            DateTime.FromFileTimeUtc(findData.ftLastAccessTime) : (DateTime?)null;
                        DateTime? modifiedTime = (findData.ftLastWriteTime != 0) ?
                            DateTime.FromFileTimeUtc(findData.ftLastWriteTime) : (DateTime?)null;

                        yield return new FileSystemEntry(
                            findData.cFileName,
                            folderPath,
                            findData.dwFileAttributes,
                            createTime,
                            accessTime,
                            modifiedTime,
                            findData.cAlternateFileName,
                            findData.FileSize);
                    }
                }
            }
        }

        /// <summary>
        /// Copy Directory Structure Recursively
        /// </summary>
        /// <param name="src">Source Folder</param>
        /// <param name="dst">Destination Folder</param>
        public static void CopyFolder(string src, string dst)
        {
            CopyFolder(src, dst, "*");
        }

        /// <summary>
        /// Copy Directory Structure Recursively
        /// </summary>
        /// <param name="src">Source Folder</param>
        /// <param name="dst">Destination Folder</param>
        /// <param name="searchPattern">The pattern of files to copy (will also be 
        /// applied for files within subfolders within) (eg: "*.cab")</param>
        public static void CopyFolder(string src, string dst, string searchPattern)
        {
            // If directory not exists, create it...
            if (!dst.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                dst += Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(dst))
            {
                Directory.CreateDirectory(dst);
            }

            // Files Array for current folder
            string[] files = Directory.GetFileSystemEntries(src, searchPattern);

            // StringBuilder buffer for pathname construction
            StringBuilder pathBuffer = new StringBuilder(MaximumPathLength);

            foreach (string element in files)
            {
                string destPath = CreatePathString(pathBuffer, dst, Path.GetFileName(element));

                // Sub directories
                if (Directory.Exists(element))
                {
                    CopyFolder(element, destPath);
                }
                // Files in directory
                else
                {
                    CopyFile(element, destPath);
                }
            }
        }

        /// <summary>
        /// Copy a file from one path to another
        /// </summary>
        /// <param name="sourcePath">Path to source</param>
        /// <param name="destPath">Path to destination</param>
        public static void CopyFile(string sourcePath, string destPath)
        {
            CopyFile(sourcePath, destPath, false);
        }

        /// <summary>
        /// Copy a file from one path to another
        /// </summary>
        /// <param name="sourcePath">Path to source</param>
        /// <param name="destPath">Path to destination</param>
        /// <param name="overwrite">Overwrite destination if exists</param>
        public static void CopyFile(string sourcePath, string destPath, bool overwrite)
        {
            Debug.WriteLine(String.Format("[{0}] -> [{1}]", sourcePath, destPath), "CopyFile");
            File.Copy(sourcePath, destPath, overwrite);
        }

        /// <summary>
        /// Merges path components into a path string.
        /// </summary>
        /// <param name="components">Path components</param>
        /// <returns>Path string (not checked for existence or validity)</returns>
        public static string CreatePathString(params string[] components)
        {
            return CreatePathString(new StringBuilder(), components);
        }

        /// <summary>
        /// Merges path components into a path string.
        /// </summary>
        /// <param name="buffer">Buffer to use</param>
        /// <param name="components">Path components</param>
        /// <returns>Path string (not checked for existence or validity)</returns>
        public static string CreatePathString(StringBuilder buffer, params string[] components)
        {
            foreach (string component in components)
            {
                buffer.Append(component);
                if (!component.EndsWith(Path.DirectorySeparatorChar.ToString()))
                {
                    buffer.Append(Path.DirectorySeparatorChar);
                }
            }

            // If empty buffer return empty string
            if (buffer.Length == 0) return String.Empty;

            // Trim last Path.DirectorySeparationChar
            int length = --buffer.Length;

            // Sanity check
            Debug.Assert(length >= 0, "Length is less than zero.");

            string returnedPath = buffer.ToString(0, length);

            // Reset builder
            buffer.Length = 0;

            // Another sanity check
            Debug.Assert(buffer.Length == 0,
                "Buffer was not properly emptied before returning.");

            return returnedPath;
        }

        static void RetryAction<TException>(
            Action<string> action, string arg, int retries, string actionName)
            where TException : Exception
        {
            int i = 0;

            while (true)
            {
                try
                {
                    action(arg);
                    break;
                }
                catch (TException)
                {
                    if (i >= retries) throw;

                    Debug.Write(actionName);
                    Debug.Write(": Stalled by ");
                    Debug.Write(typeof(TException).Name);
                    Debug.Write("; Retrying after ");
                    Debug.Write(c_RetryDelayMs);
                    Debug.WriteLine("ms");

                    Thread.Sleep(c_RetryDelayMs);
                    i++;
                }
            }
        }
    }
    
}