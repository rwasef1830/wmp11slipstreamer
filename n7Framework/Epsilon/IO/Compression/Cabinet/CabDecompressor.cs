using System;
using System.Collections.Generic;
using Epsilon.IO.Compression.Cabinet;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace Epsilon.IO.Compression
{
    public class CabDecompressor : IDisposable
    {
        bool _streamMode;
        FDIWrapper<object> _wrapper;
        CabStreamIO<object> _cabStreamIo;
        readonly string _destFolder;

        // Ugly but inevitable with FDI's design
        int _lastCabNumFiles;
        int _lastCabFilePos;

        public CabDecompressor(string destinationPath) 
        {
            if (destinationPath == null)
                throw new ArgumentNullException("destinationPath");

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            this._destFolder = Path.GetFullPath(destinationPath);

            if (!this._destFolder.EndsWith(Path.DirectorySeparatorChar.ToString()))
                this._destFolder += Path.DirectorySeparatorChar;

            // Assume extracting from file
            this._wrapper = new FDIWrapper<object>(this.OpenDestFile, this.CloseDestFile);
        }

        #region Public methods
        public void Extract(Stream stream, bool closeWhenDone)
        {
            long initialPos = stream.Position;
            if (!this._streamMode)
            {
                this._streamMode = true;
                this._cabStreamIo = new CabStreamIO<object>(stream);
                this._wrapper = new FDIWrapper<object>(this._cabStreamIo, 
                    this.OpenDestFile, this.CloseDestFile);
            }
            else
            {
                this._cabStreamIo.SetCabinetStream(stream);
            }

            FDI.CabinetInfo cabinetInfo = new FDI.CabinetInfo();
            if (!this._wrapper.IsCabinet(CabStreamIO<object>.FakeCabName, cabinetInfo))
            {
                ThrowNotValidCabinetException(stream);
            }

            // Seek back to the original position of the stream
            stream.Seek(initialPos, SeekOrigin.Begin);
            this._lastCabNumFiles = cabinetInfo.NumberOfFiles;
            this._wrapper.Extract(CabStreamIO<object>.FakeCabName);

            if (closeWhenDone) stream.Close();
        }

        public void Extract(string cabinetPath)
        {
            if (this._streamMode)
            {
                this._wrapper = new FDIWrapper<object>(this.OpenDestFile, 
                    this.CloseDestFile);
            }

            FDI.CabinetInfo cabinetInfo = new FDI.CabinetInfo();
            if (!this._wrapper.IsCabinet(cabinetPath, cabinetInfo))
            {
                ThrowNotValidCabinetException(cabinetPath);
            }

            this._lastCabNumFiles = cabinetInfo.NumberOfFiles;
            this._wrapper.Extract(cabinetPath);
        }

        public void Close()
        {
            this.Dispose();
        }
        #endregion

        #region Default FDI Event Handlers
        IntPtr OpenDestFile(string fileName, out bool skipFile,
            int uncompressedSize, DateTime lastModified, FileAttributes fileAttribs,
            out bool abortOperation, object userObject)
        {
            // Notify filename
            if (this.OnFilenameNotify != null) this.OnFilenameNotify(fileName);

            // Notify progress
            if (this.OnProgressNotify != null)
                this.OnProgressNotify(++this._lastCabFilePos, this._lastCabNumFiles);

            string fullDestPath = this._destFolder + fileName;
            string currentDestFolder = Path.GetDirectoryName(fullDestPath);

            if (!Directory.Exists(currentDestFolder))
                Directory.CreateDirectory(currentDestFolder);

            IntPtr fileHandle = Win32.API.IO.Unsafe.CreateFile(fullDestPath, 
                (uint)(Win32.API.IO.DesiredAccessWin32.FileAccess.AllRead
                | Win32.API.IO.DesiredAccessWin32.FileAccess.AllWrite), 
                FileShare.Read, IntPtr.Zero, FileMode.Create, 
                Epsilon.Win32.API.IO.ExtendedFileAttributes.Normal, IntPtr.Zero);

            // TODO: Hook into some kind of abort function
            abortOperation = false;

            // TODO: Support filtering files to extract
            skipFile = false;

            if (!Helpers.IsValidHandle(fileHandle))
                throw new Win32Exception();
            else
                return fileHandle;
        }

        bool CloseDestFile(string fileName, IntPtr fileHandle,
            DateTime lastModified, FileAttributes fileAttribs, bool execute,
            out bool abortOperation, object userObject)
        {
            if (!Epsilon.Win32.API.IO.Unsafe.SetFileTime(fileHandle, null, null, 
                new Epsilon.Win32.API.FileTimeRef(lastModified.ToFileTime())))
            {
                throw new Win32Exception();
            }
            bool result = Win32.API.IO.Unsafe.CloseHandle(fileHandle);
            File.SetAttributes(this._destFolder + fileName, fileAttribs);

            // TODO: Hook into some kind of abort function
            abortOperation = false;

            return result;
        }
        #endregion

        #region Helper methods
        static void ThrowNotValidCabinetException(object stream)
        {
            throw new InvalidDataException(String.Format(
                "\"{0}\" does not seem to be a valid cabinet file.",
                   stream.ToString()));
        }
        #endregion

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
                this._wrapper.Close();
            }
        }
        #endregion
    }
}
