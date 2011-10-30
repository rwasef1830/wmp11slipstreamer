using System;
using System.ComponentModel;
using System.IO;
using Microsoft.Win32.SafeHandles;
using Epsilon.Win32.API;
using System.Diagnostics;

namespace Epsilon.Win32
{
    public class PeEditor
    {
        #region Public members
        public readonly string PathToPe;
        public readonly long FileSize;
        public uint Checksum
        {
            get { return _checksum; }
        }
        public readonly Architecture TargetMachineType;
        #endregion

        #region Private members
        uint _checksum;
        #endregion

        /// <summary>
        /// Initialises the PeEditor class by reading the PE headers
        /// into memory and performing sanity checks
        /// </summary>
        /// <param name="pathToPe">Path to PE file to load</param>
        public PeEditor(string pathToPe)
        {
            FileInfo PeFileInfo = new FileInfo(pathToPe);
            this.PathToPe = PeFileInfo.FullName;
            this.FileSize = PeFileInfo.Length;
            FileStream peFStream = PeFileInfo.OpenRead();
            byte[] peHeaders = new byte[4096];
            peFStream.Read(peHeaders, 0, peHeaders.Length);
            peFStream.Close();
            unsafe
            {
                fixed (byte* pData = peHeaders)
                {
                    ImageDosHeader* pDosHeader = (ImageDosHeader*)pData;
                    ImageNtHeaders32* pNtHeader =
                        (ImageNtHeaders32*)(pDosHeader->e_lfanew
                        + (int)pDosHeader);
                    if (pDosHeader->e_magic != 0x5a4d
                        || pNtHeader->Signature != 0x4550)
                    {
                        throw new InvalidOperationException(
                            String.Format(
                            "The file: \"{0}\" is not a valid PE file.",
                                pathToPe));
                    }
                    this.TargetMachineType
                        = (Architecture)pNtHeader->FileHeader.Machine;
                }
            }
        }

        /// <summary>
        /// Recomputes and updates PE file checksum
        /// </summary>
        public void RecalculateChecksum()
        {
            uint sizeOfFile = (uint)this.FileSize;
            SafeFileHandle hFile
                = API.IO.Safe.CreateFile(this.PathToPe,
                    FileAccess.ReadWrite, FileShare.None, IntPtr.Zero,
                    FileMode.Open, API.IO.ExtendedFileAttributes.Normal,
                    IntPtr.Zero);
            if (hFile.IsInvalid) throw new Win32Exception();
            SafeWin32Handle hMapping = MemoryManagement.CreateFileMapping(
                hFile,
                IntPtr.Zero,
                API.PageProtection.ReadWrite,
                0, 0, IntPtr.Zero);
            if (hMapping.IsInvalid) throw new Win32Exception();
            SafeMappedViewHandle hView
                = MemoryManagement.MapViewOfFile(hMapping,
                API.MappingAccess.SectionAllAccess,
                0, 0, IntPtr.Zero);
            if (hView.IsInvalid) throw new Win32Exception();
            unsafe
            {
                uint oldChecksum;
                uint newChecksum;
                IntPtr newNtHeader =
                    DebugHelp.CheckSumMappedFile(hView,
                    sizeOfFile, (IntPtr)(&oldChecksum),
                    (IntPtr)(&newChecksum));
                if (newNtHeader == IntPtr.Zero) throw new Win32Exception();
                ((ImageNtHeaders32*)newNtHeader)->OptionalHeader.CheckSum
                    = this._checksum = newChecksum;
            }
            hView.Close();
            hMapping.Close();
            hFile.Close();
        }

        public Version GetVersion()
        {
            FileVersionInfo fileVerInfo
                = FileVersionInfo.GetVersionInfo(this.PathToPe);
            return new Version(
                fileVerInfo.FileMajorPart,
                fileVerInfo.FileMinorPart,
                fileVerInfo.FileBuildPart,
                fileVerInfo.FilePrivatePart);
        }
    }
}
