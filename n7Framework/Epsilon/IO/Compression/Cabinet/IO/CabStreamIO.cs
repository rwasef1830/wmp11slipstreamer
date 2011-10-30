using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;

namespace Epsilon.IO.Compression.Cabinet
{
    public class CabStreamIO<T> : CabIO<T> where T : class
    {
        #region Fields
        Stream _stream;
        List<IntPtr> _streamInfoHandleList;
        #endregion

        #region Public properties
        public const string FakeCabName = "\\_";
        #endregion

        public CabStreamIO(Stream cabStream)
        {
            if (!cabStream.CanRead)
                throw new ArgumentException("Stream must be readable.", "cabStream");
            if (!cabStream.CanSeek)
                throw new ArgumentException("Stream must be seekable.", "cabStream");
            this._streamInfoHandleList = new List<IntPtr>(2);

            // The stream that we are going to read from must be at position
            // 0 as cabinet.dll does absolute seeks (from beginning of the file).
            //
            // In case the stream is not at position 0 (SFX CAB for example),
            // I wrote a stream wrapper that wraps all the seek and position
            // functions to emulate a fresh stream.
            this.SetCabinetStream(cabStream);
        }

        public void SetCabinetStream(Stream cabStream)
        {
            if (cabStream.Position != 0L)
            {
                // Wrap the stream into an emulation wrapper
                this._stream = new StreamWrapper(cabStream);
            }
            else
            {
                this._stream = cabStream;
            }
        }

        public override IntPtr FileOpen(string fileName, int oflag, int pmode, 
            ref int err, IntPtr userData)
        {
            if (String.Equals(fileName, FakeCabName, StringComparison.Ordinal))
            {
                StreamInstanceInfo instanceInfo = new StreamInstanceInfo();
                IntPtr hInstanceInfo = (IntPtr)GCHandle.Alloc(instanceInfo);
                this._streamInfoHandleList.Add(hInstanceInfo);
                return hInstanceInfo;
            }
            else
            {
                return base.FileOpen(fileName, oflag, pmode, ref err, userData);
            }
        }

        public override int FileRead(IntPtr hf, byte[] buffer, int cb, 
            ref int err, IntPtr userData)
        {
            if (this._streamInfoHandleList.Contains(hf))
            {
                try
                {
                    StreamInstanceInfo sInfo = TargetFromPtr<StreamInstanceInfo>(hf);
                    this._stream.Position = sInfo.CurrentPosition;
                    int bytesRead = this._stream.Read(buffer, 0, cb);
                    sInfo.CurrentPosition += bytesRead;
                    return bytesRead;
                }
                catch (Exception ex)
                {
                    base.ReportException(ex);
                    return -1;
                }
            }
            else
            {
                return base.FileRead(hf, buffer, cb, ref err, userData);
            }
        }

        public override int FileSeek(IntPtr hf, int dist, int seekType, 
            ref int err, IntPtr userData)
        {
            if (this._streamInfoHandleList.Contains(hf))
            {
                try
                {
                    StreamInstanceInfo sInfo = TargetFromPtr<StreamInstanceInfo>(hf);
                    this._stream.Position = sInfo.CurrentPosition;
                    long newPosition = this._stream.Seek(dist, (SeekOrigin)seekType);
                    sInfo.CurrentPosition = newPosition;
                    return (int)newPosition;
                }
                catch (Exception ex)
                {
                    base.ReportException(ex);
                    return -1;
                }
            }
            else
            {
                return base.FileSeek(hf, dist, seekType, ref err, userData);
            }
        }

        public override int FileClose(IntPtr hf, ref int err, IntPtr userData)
        {
            if (this._streamInfoHandleList.Contains(hf))
            {
                try
                {
                    // Let my GCHandle go !
                    ((GCHandle)(hf)).Free();

                    // Remove the pointer from the list
                    this._streamInfoHandleList.Remove(hf);

                    return 0;
                }
                catch (Exception ex)
                {
                    base.ReportException(ex);
                    return -1;
                }
            }
            else
            {
                return base.FileClose(hf, ref err, userData);
            }
        }

        TTarget TargetFromPtr<TTarget>(IntPtr ptr)
        {
            return (TTarget)(((GCHandle)ptr).Target);
        }

        class StreamInstanceInfo
        {
            internal long CurrentPosition;
        }
    }
}
