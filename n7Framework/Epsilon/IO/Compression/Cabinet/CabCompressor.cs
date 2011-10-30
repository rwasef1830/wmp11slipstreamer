using System;
using System.Collections.Generic;
using System.Text;
using Epsilon.IO.Compression.Cabinet;
using System.IO;

namespace Epsilon.IO.Compression
{
    public class CabCompressor : IDisposable
    {
        bool _flushNeeded;
        FCIWrapper<object> _wrapper;

        public CabCompressor(string cabFilePath) : this(cabFilePath, 0, 0) { }

        public CabCompressor(string cabFilePath, int maxSizePerCabinet, 
            int maxFolderThresh)
        {
            FCI.CurrentCabinet ccab = new FCI.CurrentCabinet();
            ccab.CabinetName = Path.GetFileName(cabFilePath);
            ccab.CabinetPath = Path.GetDirectoryName(cabFilePath);
            ccab.MaximumCabinetSize = maxSizePerCabinet;
            ccab.FolderThreshold = maxFolderThresh;

            this._wrapper = new FCIWrapper<object>(ccab);
        }

        public void AddFile(string absFilePath, FCI.CompressionLevel compressLevel)
        {
            this.AddFiles(new string[] { absFilePath }, compressLevel,
                Path.GetDirectoryName(absFilePath));
        }

        public void AddFolder(string pathToFolder, string fileFilter,
            FCI.CompressionLevel compressLevel, bool recursive, 
            bool folderContentsOnly)
        {
            string fullPathName = Path.GetFullPath(pathToFolder);
            string[] allFiles = Directory.GetFiles(fullPathName, fileFilter,
                (recursive)? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            string pathAbsToFilesRoot = (folderContentsOnly)? 
                fullPathName : Path.GetDirectoryName(fullPathName);

            this.AddFiles(allFiles, compressLevel, pathAbsToFilesRoot);
        }

        public void AddFiles(string[] fileList, FCI.CompressionLevel compressLevel)
        {
            this.AddFiles(fileList, compressLevel, String.Empty);
        }

        public void AddFiles(string[] fileList, FCI.CompressionLevel compressLevel, 
            string pathAbsToFilesRoot)
        {
            this.EnsureWrapperIsAlive();

            if (fileList.Length > ushort.MaxValue)
            {
                throw new ArgumentException("Too many files to compress. Maximum is " 
                    + ushort.MaxValue.ToString());
            }

            if (fileList.Length > 0)
            {
                string prefix = String.Empty;
                int prefixIndex = pathAbsToFilesRoot.Length;

                if (pathAbsToFilesRoot.Length > 0 
                    && !pathAbsToFilesRoot.EndsWith(
                    Path.DirectorySeparatorChar.ToString())) prefixIndex++;

                int currentFileIndex = 0;
                foreach (string filePath in fileList)
                {
                    int jumpFactor = 0;
                    if (!Directory.Exists(filePath))
                    {
                        // Chop away the prefix
                        string filenameInCab = filePath.Substring(prefixIndex);
                        
                        // Notify filename being added
                        if (this.OnFilenameNotify != null)
                            this.OnFilenameNotify(filenameInCab);

                        // Add to cabinet
                        this._wrapper.AddFile(filePath, filenameInCab, false,
                            compressLevel);
                        currentFileIndex += ++jumpFactor;

                        // Reset the jump factor
                        jumpFactor = 0;

                        // Progress notify
                        if (this.OnProgressNotify != null)
                        {
                            this.OnProgressNotify(currentFileIndex, fileList.Length);
                        }
                    }
                    else
                    {
                        // This is a folder in the list. We don't want to do
                        // a useless progress notification for that.
                        jumpFactor++;
                    }
                }

                this._flushNeeded = true;
            }
        }

        /// <summary>
        /// Writes the current cabinet to disk and releases all resources
        /// and handles held by this instance
        /// </summary>
        public void Close()
        {
            this.Dispose();
        }

        void EnsureWrapperIsAlive()
        {
            if (this._wrapper == null)
                throw new ObjectDisposedException(this.ToString(), 
                    "FCI context is closed.");
        }

        #region Progress notification events
        public delegate void ProgressNotifyDelegate(int val, int max);
        public delegate void FilenameNotifyDelegate(string filename);

        public event ProgressNotifyDelegate OnProgressNotify;
        public event FilenameNotifyDelegate OnFilenameNotify;
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this._flushNeeded) this._wrapper.FlushCabinet();
                this._wrapper.Close();
            }

            this._flushNeeded = false;
        }
        #endregion
    }
}
