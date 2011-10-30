using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.Win32.Resources
{
    [StructLayout(LayoutKind.Sequential)]
    public class IconDirectory
    {
        /// <summary>
        /// Initialises an empty instance of the IconDirectory structure
        /// </summary>
        public IconDirectory() { }

        /// <summary>
        /// Reserved (must be 0)
        /// </summary>
        public ushort idReserved;

        /// <summary>
        /// Resource Type (1 for icons)
        /// </summary>
        public ushort idType;

        /// <summary>
        /// Number of images
        /// </summary>
        public ushort idCount;

        /// <summary>
        /// IconDirectoryEntries (idCount of them)
        /// </summary>
        public IconDirectoryEntry[] idEntries;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class IconDirectoryEntry
    {
        /// <summary>
        /// Initialises an empty instance of the IconDirectoryEntry structure
        /// </summary>
        public IconDirectoryEntry() { }

        /// <summary>
        /// Width of the image in pixels
        /// </summary>
        public byte bWidth;

        /// <summary>
        /// Height of the image in pixels
        /// </summary>
        public byte bHeight;

        /// <summary>
        /// Number of colours in the image (0 if >= 8bpp)
        /// </summary>
        public byte bColorCount;

        /// <summary>
        /// Reserved (must be 0)
        /// </summary>
        public byte bReserved;

        /// <summary>
        /// Colour planes
        /// </summary>
        public ushort wPlanes;

        /// <summary>
        /// Bits per pixel
        /// </summary>
        public ushort wBitCount;

        /// <summary>
        /// Number of bytes in this resource
        /// </summary>
        public uint dwBytesInRes;

        /// <summary>
        /// Offset in the file for this image's data
        /// </summary>
        public uint dwImageOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class ResourceIconDirectory
    {
        /// <summary>
        /// Initialises an empty instance of the IconDirectory structure
        /// </summary>
        public ResourceIconDirectory() { }

        /// <summary>
        /// Reserved (must be 0)
        /// </summary>
        public ushort idReserved;

        /// <summary>
        /// Resource Type (1 for icons)
        /// </summary>
        public ushort idType;

        /// <summary>
        /// Number of images
        /// </summary>
        public ushort idCount;

        /// <summary>
        /// IconDirectoryEntries (idCount of them)
        /// </summary>
        public ResourceIconDirectoryEntry[] idEntries;

        /// <summary>
        /// Return a byte array of the data in this structure
        /// </summary>
        /// <returns>Byte Array</returns>
        public byte[] ToByteArray()
        {
            int lengthNeeded = 2 + 2 + 2 + (14 * this.idEntries.Length);
            List<byte> arrayOfData = new List<byte>(lengthNeeded);
            arrayOfData.AddRange(BitConverter.GetBytes(this.idReserved));
            arrayOfData.AddRange(BitConverter.GetBytes(this.idType));
            arrayOfData.AddRange(BitConverter.GetBytes(this.idCount));
            foreach (ResourceIconDirectoryEntry entry in this.idEntries)
            {
                arrayOfData.Add(entry.bWidth);
                arrayOfData.Add(entry.bHeight);
                arrayOfData.Add(entry.bColorCount);
                arrayOfData.Add(entry.bReserved);
                arrayOfData.AddRange(BitConverter.GetBytes(entry.wPlanes));
                arrayOfData.AddRange(BitConverter.GetBytes(entry.wBitCount));
                arrayOfData.AddRange(BitConverter.GetBytes(entry.dwBytesInRes));
                arrayOfData.AddRange(BitConverter.GetBytes(entry.nID));
            }
            return arrayOfData.ToArray();
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public class ResourceIconDirectoryEntry
    {
        /// <summary>
        /// Initialises an empty instance of the IconDirectoryEntry structure
        /// </summary>
        public ResourceIconDirectoryEntry() { }

        /// <summary>
        /// Width of the image in pixels
        /// </summary>
        public byte bWidth;

        /// <summary>
        /// Height of the image in pixels
        /// </summary>
        public byte bHeight;

        /// <summary>
        /// Number of colours in the image (0 if >= 8bpp)
        /// </summary>
        public byte bColorCount;

        /// <summary>
        /// Reserved (must be 0)
        /// </summary>
        public byte bReserved;

        /// <summary>
        /// Colour planes
        /// </summary>
        public ushort wPlanes;

        /// <summary>
        /// Bits per pixel
        /// </summary>
        public ushort wBitCount;

        /// <summary>
        /// Number of bytes in this resource
        /// </summary>
        public uint dwBytesInRes;

        /// <summary>
        /// RT_ICON ID in the resource table
        /// </summary>
        public ushort nID;
    }
}
