using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using Epsilon.Win32.API;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using Epsilon.DebugServices;

namespace Epsilon.IO.Compression.Cabinet
{
    public class CabIO<T> where T : class
    {
        #region Exception reporting
        public event Action<Exception> OnError;

        protected void ReportException(Exception exception)
        {
            if (this.OnError != null) this.OnError(exception);
        }
        #endregion

        #region Static members
        readonly static string s_ShortTempPath;
        #endregion

        #region Constructors
        static CabIO()
        {
            StringBuilder buffer = new StringBuilder(256);
            Win32.API.IO.GetShortPathName(
                Path.GetTempPath(),
                buffer, (uint)buffer.Capacity);
            if (buffer.Length == 0)
            {
                Debug.Fail("GetShortPathName failed. Falling back to normal temporary path.");
                buffer.Append(Path.GetTempPath());
            }

            if (buffer[buffer.Length - 1] != Path.DirectorySeparatorChar)
            {
                buffer.Append(Path.DirectorySeparatorChar);
            }

            s_ShortTempPath = buffer.ToString();
        }
        #endregion

        #region Debugging help
#if DEBUG
        protected Dictionary<IntPtr, string> _ptrFileDict = new Dictionary<IntPtr,string>();
#endif
        #endregion

        public virtual IntPtr MemAlloc(int sizeNeeded)
        {
            try
            {
                return Marshal.AllocHGlobal(sizeNeeded);
            }
            catch (Exception ex)
            {
                this.ReportException(ex);
                return IntPtr.Zero;
            }
        }

        public virtual void MemFree(IntPtr memory)
        {
            Marshal.FreeHGlobal(memory);
        }

        public virtual IntPtr FileOpen(
            string filePath,
            int oflag,
            int pmode)
        {
            int err = 0;
            return this.FileOpen(filePath, oflag, pmode, ref err, IntPtr.Zero);
        }

        public virtual IntPtr FileOpen(
            string filePath,
            int oflag,
            int pmode,
            ref int err,
            IntPtr userData)
        {
            try
            {
                Win32.API.IO.DesiredAccessWin32.FileAccess fileAccess = 
                    (Win32.API.IO.DesiredAccessWin32.FileAccess)FCntl.FileAccessFromOFlag(oflag);
                FileMode fileMode = FCntl.FileModeFromOFlag(oflag);
                FileShare fileShare = FileShare.Read;

                // If opening file for writing, prevent other processes from accessing it
                if ((fileAccess & Win32.API.IO.DesiredAccessWin32.FileAccess.ReadData) 
                    == Epsilon.Win32.API.IO.DesiredAccessWin32.FileAccess.ReadData)
                {
                    fileAccess |= Epsilon.Win32.API.IO.DesiredAccessWin32.FileAccess.ReadAttributes;
                    fileShare = FileShare.Read;
                }
                else if ((fileAccess & Epsilon.Win32.API.IO.DesiredAccessWin32.FileAccess.WriteData)
                    == Epsilon.Win32.API.IO.DesiredAccessWin32.FileAccess.WriteData)
                {
                    fileShare = FileShare.None;
                }

                IntPtr hFile = Win32.API.IO.Unsafe.CreateFile(filePath, (uint)fileAccess,
                    fileShare, IntPtr.Zero, fileMode,
                    Epsilon.Win32.API.IO.ExtendedFileAttributes.Normal, IntPtr.Zero);
                if (Helpers.IsValidHandle(hFile))
                {
#if DEBUG
                    this._ptrFileDict.Add(hFile, filePath); 
#endif
                    return hFile;
                }
                else
                {
                    throw new Win32Exception();
                }
            }
            catch (Exception ex)
            {
                this.ReportException(ex);
                return (IntPtr)(-1);
            }
        }

        public virtual int FileRead(IntPtr hf, byte[] buffer, int cb)
        {
            int err = 0;
            return this.FileRead(hf, buffer, cb, ref err, IntPtr.Zero);
        }

        public virtual int FileRead(IntPtr hf, byte[] buffer, int cb,
            ref int err, IntPtr userData)
        {
            try
            {
                uint bytesRead;
                if (!Win32.API.IO.Unsafe.ReadFile(
                    hf, buffer, (uint)cb, out bytesRead,
                    IntPtr.Zero))
                {
                    throw new Win32Exception();
                }
                return (int)bytesRead;
            }
            catch (Exception ex)
            {
                this.ReportException(ex);
                return -1;
            }
        }

        public virtual int FileWrite(IntPtr hf, byte[] buffer, int cb)
        {
            int err = 0;
            return this.FileWrite(hf, buffer, cb, ref err, IntPtr.Zero);
        }

        public virtual int FileWrite(IntPtr hr, byte[] buffer, int cb, 
            ref int err, IntPtr userData)
        {
            try
            {
                uint bytesWritten;
                if (!Win32.API.IO.Unsafe.WriteFile(
                    hr, buffer, (uint)cb, out bytesWritten,
                    IntPtr.Zero))
                {
                    throw new Win32Exception();
                }
                return (int)bytesWritten;
            }
            catch (Exception ex)
            {
                this.ReportException(ex);
                return -1;
            }
        }

        public virtual int FileClose(IntPtr hf)
        {
            int err = 0;
            return this.FileClose(hf, ref err, IntPtr.Zero);
        }

        public virtual int FileClose(IntPtr hf, ref int err, IntPtr userData)
        {
            try
            {
#if DEBUG
                Debug.Assert(Helpers.IsValidHandle(hf), 
                    "Closing an invalid handle.");

                // Closing an alien handle has occurred so far during corrupted cabinet
                // extraction. What worries me is that it was not opened by my code.
                Debug.WriteLineIf(
                    !this._ptrFileDict.Remove(hf), 
                    "Closing an alien handle: " + hf);
#endif

                if (!Win32.API.IO.Unsafe.CloseHandle(hf))
                {
                    throw new Win32Exception();
                }
                return 0;
            }
            catch (Exception ex)
            {
                this.ReportException(ex);
                return -1;
            }
        }

        public virtual int FileSeek(IntPtr hf, int dist, int seekType)
        {
            int err = 0;
            return this.FileSeek(hf, dist, seekType, ref err, IntPtr.Zero);
        }

        public virtual int FileSeek(IntPtr hf, int dist, int seekType,
            ref int err, IntPtr userData)
        {
            try
            {
                long newPosition;
                if (!Win32.API.IO.Unsafe.SetFilePointerEx(hf, dist, out newPosition,
                    (uint)seekType))
                {
                    throw new Win32Exception();
                }
                return (int)newPosition;
            }
            catch (Exception ex)
            {
                this.ReportException(ex);
                return -1;
            }
        }

        public virtual int FileDelete(string filename)
        {
            int err = 0;
            return this.FileDelete(filename, ref err, IntPtr.Zero);
        }

        public virtual int FileDelete(string filename, ref int err, IntPtr userData)
        {
            try
            {
                FileSystem.DeleteFile(filename);
                return 0;
            }
            catch (Exception ex)
            {
                this.ReportException(ex);
                return -1;
            }
        }

        public virtual bool GetTempFile(IntPtr pBuffer, int bufferLength, IntPtr pObject)
        {
            try
            {
                IntPtr bufferPos = pBuffer;
                string tempFileName = s_ShortTempPath + Path.GetRandomFileName();

                int bufferLengthNeeded = tempFileName.Length + 1;

                // Heap corruption protection
                if (bufferLength < bufferLengthNeeded)
                {
                    throw new PathTooLongException(
                        "Cabinet.dll did not allocate a big enough buffer for path string.");
                }
                else
                {
                    byte[] tempFileNameBytes = Encoding.Default.GetBytes(tempFileName);
                    Marshal.Copy(tempFileNameBytes, 0, bufferPos,
                        tempFileNameBytes.Length);
                    bufferPos = new IntPtr(bufferPos.ToInt64()
                        + tempFileNameBytes.LongLength);
                    Marshal.WriteByte(bufferPos, 0);
                    return true;
                }
            }
            catch (Exception ex)
            {
                this.ReportException(ex);
                return false;
            }
        }

#if DEBUG
        #region Catch unclosed handles
        ~CabIO()
        {
            if (this._ptrFileDict.Count > 0)
            {
                HelperConsole.WarnWriteLine("Handle leak detected in CabIO! Check FileClose.");
            }
        }
        #endregion
#endif
    }
}
