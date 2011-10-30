using System;
using System.Runtime.InteropServices;

namespace Epsilon.Win32
{
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public unsafe struct ImageDosHeader
    {
        /// <summary>
        /// Magic number
        /// </summary>
        public ushort e_magic;
        /// <summary>
        /// Bytes on last page of file
        /// </summary>
        public ushort e_cblp;
        /// <summary>
        /// Pages in file
        /// </summary>
        public ushort e_cp;
        /// <summary>
        /// Relocations
        /// </summary>
        public ushort e_crlc;
        /// <summary>
        /// Size of header in paragraphs
        /// </summary>
        public ushort e_cparhdr;
        /// <summary>
        /// Minimum extra paragraphs needed
        /// </summary>
        public ushort e_minalloc;
        /// <summary>
        /// Maximum extra paragraphs needed
        /// </summary>
        public ushort e_maxalloc;
        /// <summary>
        /// Initial (relative) SS value
        /// </summary>
        public ushort e_ss;
        /// <summary>
        /// Initial SP value
        /// </summary>
        public ushort e_sp;
        /// <summary>
        /// DOS checksum
        /// </summary>
        public ushort e_csum;
        /// <summary>
        /// Initial IP value
        /// </summary>
        public ushort e_ip;
        /// <summary>
        /// Initial (relative) CS value
        /// </summary>
        public ushort e_cs;
        /// <summary>
        /// File address of relocation table
        /// </summary>
        public ushort e_lfarlc;
        /// <summary>
        /// Overlay number
        /// </summary>
        public ushort e_ovno;
        /// <summary>
        /// Reserved words (UInt16s)
        /// </summary>
        public fixed ushort e_res[4];
        /// <summary>
        /// OEM Identifier for e_oeminfo
        /// </summary>
        public ushort e_oemid;
        /// <summary>
        /// OEM-specific information
        /// </summary>
        public ushort e_oeminfo;
        /// <summary>
        /// Reserved words (UInt16s)
        /// </summary>
        public fixed ushort e_res2[10];
        /// <summary>
        /// File address of new header
        /// </summary>
        public int e_lfanew;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageNtHeaders32
    {
        public uint Signature;
        public ImageFileHeader FileHeader;
        public ImageOptionalHeader32 OptionalHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageNtHeaders64
    {
        public uint Signature;
        public ImageFileHeader FileHeader;
        public ImageOptionalHeader64 OptionalHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageFileHeader
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageOptionalHeader32
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public uint BaseOfData;
        public uint ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public uint SizeOfStackReserve;
        public uint SizeOfStackCommit;
        public uint SizeOfHeapReserve;
        public uint SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public ImageDataDirectory DataDirectory;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageOptionalHeader64
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public UInt64 ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public UInt64 SizeOfStackReserve;
        public UInt64 SizeOfStackCommit;
        public UInt64 SizeOfHeapReserve;
        public UInt64 SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public ImageDataDirectory DataDirectory;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageDataDirectory
    {
        public uint VirtualAddress;
        public uint Size;
    }

    public enum Architecture : ushort
    {
        Native = 0,
        x86 = 0x014c,
        Itanium = 0x0200,
        x64 = 0x8664
    }

}
