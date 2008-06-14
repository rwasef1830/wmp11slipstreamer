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
        /// Copy or expand file from the arch directory to the destination else
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
        
        bool FileExistsInArch(string file)
        {
        	return this.FileExistsInArch(file, true);
        }

        bool FileExistsInArch(string file, bool checkCompressed)
        {
            return this.FilesExistInArch(new string[] { file }, 0, checkCompressed);
        }
        
        bool FilesExistInArch(string[] kbFiles, int startAt)
        {
        	return this.FilesExistInArch(kbFiles, startAt, true);
        }

        bool FilesExistInArch(string[] kbFiles, int startAt, bool checkCompressed)
        {
            for (int i = startAt; i < kbFiles.Length; i++)
            {
            	if (!FileExistsInSourceFolder(this._pathBuilder, kbFiles[i], 
            	                              this._archFilesDirectory, checkCompressed)) 
            	{
            		return false;
            	}
            }
            return true;
        }
        
        public static bool FileExistsInSourceFolder(StringBuilder buffer, 
			string fileName, string sourceFolder, bool checkCompressed)
        {
        	string archPath = FileSystem.CreatePathString(buffer, sourceFolder, fileName);
        	string archPathCompressed = CM.GetCompressedFileName(archPath);
        	
        	bool found = File.Exists(archPath);
        	if (checkCompressed) found |= File.Exists(archPathCompressed);
        	
        	return found;
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
