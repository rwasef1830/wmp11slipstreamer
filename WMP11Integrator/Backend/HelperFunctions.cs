using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Epsilon.IO;
using System.Diagnostics;

namespace WMP11Slipstreamer
{
    partial class Backend
    {
        static bool ByteArraysAreEqual(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length)
                return false;
            else
            {
                for (int i = 0; i < array1.Length; i++)
                {
                    if (array1[i] != array2[i])
                        return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Copy or expand file from i386Directory to the destination else
        /// throw FileNotFoundException if file and compressed cab both not exist.
        /// </summary>
        /// <param name="filename">filename to copy or expand if compressed 
        /// (must specify uncompressed file name) without path</param>
        /// <param name="destinationFolder">destination folder</param>
        /// <param name="ignoreIfNotExist">Don't throw exception if filename 
        /// doesn't exist</param>
        /// <returns>true if file was copied, false if it doesn't exist and 
        /// ignoreIfNotExist is true</returns>
        bool CopyOrExpandFromArch(string filename, string destinationFolder,
            bool ignoreIfNotExist)
        {
            string archFilename = this.CombinePathComponents(
                _archFilesDirectory, filename);
            string compressedArchfilename
                = CM.GetCompressedFileName(archFilename);
            string destinationFilename = this.CombinePathComponents(
                destinationFolder, filename);

            if (File.Exists(archFilename))
            {
                File.Copy(archFilename, destinationFilename);
                return true;
            }
            else if (File.Exists(compressedArchfilename))
            {
                Archival.NativeCabinetExtract(
                    compressedArchfilename, destinationFolder);
                return true;
            }
            else if (ignoreIfNotExist)
            {
                return false;
            }
            else
            {
                throw new FileNotFoundException(
                    String.Format(
                        "Could not copy or extract a file from \"{0}\" to \"{1}\"",
                        this._archFilesDirectory, destinationFolder), filename);
            }
        }

        bool FileExistsInI386(string file)
        {
            return this.FilesExistInI386(new string[] { file }, 0);
        }

        bool FilesExistInI386(string[] kbFiles, int startAt)
        {
            bool allExist = true;
            for (int i = startAt; i < kbFiles.Length && allExist; i++)
            {
                string i386name = this.CombinePathComponents(
                    this._archFilesDirectory, kbFiles[i]);
                string i386compname = CM.GetCompressedFileName(i386name);
                allExist =
                    (File.Exists(i386name) || File.Exists(i386compname)) && allExist;
            }
            return allExist;
        }

        // Path construction StringBuilder (not thread-safe !)
        // used by CombinePathComponents.
        StringBuilder _pathBuilder = new StringBuilder(260);

        string CombinePathComponents(params string[] components)
        {
            return FileSystem.CreatePathString(this._pathBuilder, components);
        }
    }
}
