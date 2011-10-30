using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using Epsilon.Win32.API;

namespace Epsilon.Win32.Resources
{
    public class ResourceEditor : IDisposable
    {
        #region Private members
        IntPtr hModule;
        bool disposed;
        string filePath;
        #endregion

        #region Constructor
        public ResourceEditor(string filename)
        {
            this.filePath = Path.GetFullPath(filename);
            this.LoadResourceLibrary();
        }
        #endregion

        #region ReadRawResourceBytes method overloads
        public bool ReadRawResourceBytes(ResourceAPI.ResourceType resType, 
            string resName, out byte[] resData)
        {
            this.EnsureObjectIsAlive();

            IntPtr pName = Marshal.StringToHGlobalUni(resName);
            ushort[] languages = GetResourceLanguages(resType, pName);
            Marshal.FreeHGlobal(pName);
            if (languages.Length > 0)
            {
                return ReadRawResourceBytes(resType, resName, languages[0],
                    out resData);
            }
            else
            {
                resData = null;
                return false;
            }
        }

        public bool ReadRawResourceBytes(ResourceAPI.ResourceType resType, 
            string resName, ushort languageId, out byte[] resData)
        {
            this.EnsureObjectIsAlive();

            IntPtr pFirstByte;
            uint lengthOfArray;
            if (ReadRawResourceBytes("#" + (int)resType, resName, languageId,
                out pFirstByte, out lengthOfArray))
            {
                resData = GetBytesFromPointer(pFirstByte, (int)lengthOfArray);
                return true;
            }
            else
            {
                resData = null;
                return false;
            }
        }
        #endregion

        #region GetResourceLanguages method
        public ushort[] GetResourceLanguages(ResourceAPI.ResourceType resType,
            IntPtr pName)
        {
            this.EnsureObjectIsAlive();

            List<ushort> langList = new List<ushort>();
            ResourceAPI.EnumResourceLanguages(this.hModule, resType,
                pName,
                delegate(IntPtr hModule,
                    IntPtr lpType,
                    IntPtr lpName,
                    ushort wIDLanguage,
                    IntPtr lParam)
                {
                    langList.Add(wIDLanguage);
                    return true;
                },
                IntPtr.Zero
            );
            return langList.ToArray();
        }
        #endregion

        #region Replace Icon
        public void ReplaceMainIcon(byte[] icoData)
        {
            this.EnsureObjectIsAlive();

            MemoryStream mStream = new MemoryStream(icoData);
            ReplaceMainIcon(mStream);
        }

        public void ReplaceMainIcon(string pathToIcon)
        {
            this.EnsureObjectIsAlive();

            FileStream fStream = new FileStream(pathToIcon, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            ReplaceMainIcon(fStream);
        }

        public void ReplaceMainIcon(Stream icoStream)
        {
            this.EnsureObjectIsAlive();

            if (!IsValidIcon(icoStream))
            {
                throw new InvalidOperationException("Invalid icon data");
            }
            BinaryReader iconBReader = new BinaryReader(icoStream);
            IconDirectory iconDir = new IconDirectory();
            iconDir.idReserved = iconBReader.ReadUInt16();
            iconDir.idType = iconBReader.ReadUInt16();
            iconDir.idCount = iconBReader.ReadUInt16();
            iconDir.idEntries = new IconDirectoryEntry[iconDir.idCount];
            for (int i = 0; i < iconDir.idCount; i++)
            {
                byte[] iconDirEntryBytes
                    = new byte[Marshal.SizeOf(typeof(IconDirectoryEntry))];
                iconBReader.Read(iconDirEntryBytes, 0, iconDirEntryBytes.Length);
                GCHandle handle = GCHandle.Alloc(iconDirEntryBytes, 
                    GCHandleType.Pinned);
                iconDir.idEntries[i]
                    = (IconDirectoryEntry)Marshal.PtrToStructure(
                        handle.AddrOfPinnedObject(),
                        typeof(IconDirectoryEntry));
                handle.Free();
            }

            IntPtr ptrFirstGroupIcon = IntPtr.Zero;
            string firstGroupIcon = null;
            ResourceAPI.EnumResourceNames(
                this.hModule,
                ResourceAPI.ResourceType.GroupIcon,
                delegate(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam)
                {
                    firstGroupIcon = GetResourceName(lpName);
                    return false;
                },
                IntPtr.Zero
            );

            if (firstGroupIcon.StartsWith("#"))
            {
                string attemptToParseFirstGroupIcon =
                    firstGroupIcon.Substring(1,
                    firstGroupIcon.Length - 1);
                int possibleId;
                if (int.TryParse(attemptToParseFirstGroupIcon, out possibleId))
                {
                    ptrFirstGroupIcon = new IntPtr(possibleId);
                }
                else
                {
                    ptrFirstGroupIcon = Marshal.StringToHGlobalUni(
                        attemptToParseFirstGroupIcon);
                }
            }
            else
            {
                ptrFirstGroupIcon = Marshal.StringToHGlobalUni(firstGroupIcon);
            }

            byte[] currentIconGroup;
            ushort[] languages = GetResourceLanguages(
                ResourceAPI.ResourceType.GroupIcon,
                ptrFirstGroupIcon);
            if (languages.Length == 0)
                ThrowWin32Exception();
            ushort languageOfFirstGroupIcon = languages[0];

            ReadRawResourceBytes(ResourceAPI.ResourceType.GroupIcon, firstGroupIcon,
                languageOfFirstGroupIcon, out currentIconGroup);
            MemoryStream mStream = new MemoryStream(currentIconGroup);
            BinaryReader resBReader = new BinaryReader(mStream);
            ResourceIconDirectory resDir = new ResourceIconDirectory();
            resDir.idReserved = resBReader.ReadUInt16();
            resDir.idType = resBReader.ReadUInt16();
            if (resDir.idReserved != 0 || resDir.idType != 1)
            {
                throw new InvalidOperationException(
                    String.Format(
                        "Resource Id \"{0}\" in \"{1}\" contains invalid icon data.",
                        firstGroupIcon, this.filePath));
            }
            resDir.idCount = resBReader.ReadUInt16();
            resDir.idEntries = new ResourceIconDirectoryEntry[resDir.idCount];
            for (int i = 0; i < resDir.idCount; i++)
            {
                byte[] resDirEntryBytes
                    = new byte[Marshal.SizeOf(typeof(ResourceIconDirectoryEntry))];
                resBReader.Read(resDirEntryBytes, 0, resDirEntryBytes.Length);
                GCHandle handle = GCHandle.Alloc(resDirEntryBytes,
                    GCHandleType.Pinned);
                resDir.idEntries[i]
                    = (ResourceIconDirectoryEntry)Marshal.PtrToStructure(
                        handle.AddrOfPinnedObject(),
                        typeof(ResourceIconDirectoryEntry));
                handle.Free();
            }

            ushort largestNumberInIconRes = 1;
            ResourceAPI.EnumResourceNames(
                this.hModule,
                ResourceAPI.ResourceType.Icon,
                delegate(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam)
                {
                    ushort currentId;
                    if (TryGetResourceId(lpName, out currentId))
                    {
                        largestNumberInIconRes 
                            = Math.Max(currentId, largestNumberInIconRes);
                    }
                    return true;
                },
                IntPtr.Zero
            );

            // Begin constructing new IconGroup structure
            ResourceIconDirectoryEntry[] newIdEntries
                = new ResourceIconDirectoryEntry[iconDir.idCount];

            // Free the library
            if (this.hModule != IntPtr.Zero)
            {
                DllFunctions.FreeLibrary(this.hModule);
                this.hModule = IntPtr.Zero;
            }

            /*
             * The idea here should work like this:
             * Loop x till it reaches the max of resDir entries and iconDir entries
             * 
             * The situation in which x exists in both, I will simply use the same
             * icon Id and simply update the icondirentry and the icon itself.
             * 
             * The situation in which x exists only in resDir means that the old icongroup
             * had more icons than the one I am replacing it with, in that case I just 
             * delete the corresponding icon resource and ignore the icondirentry for it
             * 
             * The situation in which x exists only in iconDir means that the new icongroup
             * has more icons that the one already present. In this case, I will just
             * increment the largest icon id already used (that I calculated before)
             * 
             */
            IntPtr hUpdate = ResourceAPI.BeginUpdateResource(this.filePath, false);
            if (hUpdate == IntPtr.Zero)
            {
                ThrowWin32Exception();
            }
            for (int x = 0; x < Math.Max(resDir.idCount, iconDir.idCount); x++)
            {
                if (iconDir.idEntries.Length > x)
                {
                    newIdEntries[x] = new ResourceIconDirectoryEntry();
                    newIdEntries[x].bColorCount = iconDir.idEntries[x].bColorCount;
                    newIdEntries[x].bHeight = iconDir.idEntries[x].bHeight;
                    newIdEntries[x].bWidth = iconDir.idEntries[x].bWidth;
                    newIdEntries[x].bReserved = iconDir.idEntries[x].bReserved;
                    newIdEntries[x].dwBytesInRes = iconDir.idEntries[x].dwBytesInRes;
                    newIdEntries[x].wBitCount = iconDir.idEntries[x].wBitCount;
                    newIdEntries[x].wPlanes = iconDir.idEntries[x].wPlanes;
                    if (x > resDir.idEntries.Length - 1)
                    {
                        newIdEntries[x].nID = ++largestNumberInIconRes;
                    }
                    else
                    {
                        newIdEntries[x].nID = resDir.idEntries[x].nID;
                    }
                    icoStream.Seek(iconDir.idEntries[x].dwImageOffset, SeekOrigin.Begin);
                    byte[] rawIconData = new byte[iconDir.idEntries[x].dwBytesInRes];
                    icoStream.Read(rawIconData, 0, rawIconData.Length);
                    GCHandle gcHandle = GCHandle.Alloc(rawIconData, GCHandleType.Pinned);
                    bool resultOne = ResourceAPI.UpdateResource(hUpdate,
                        ResourceAPI.ResourceType.Icon, newIdEntries[x].nID,
                        languageOfFirstGroupIcon, 
                        gcHandle.AddrOfPinnedObject(),
                        (uint)rawIconData.Length);
                    gcHandle.Free();
                    if (!resultOne) ThrowWin32Exception();
                }
                else
                {
                    if (!ResourceAPI.UpdateResource(hUpdate,
                        ResourceAPI.ResourceType.Icon, resDir.idEntries[x].nID,
                        languageOfFirstGroupIcon, IntPtr.Zero, 0))
                    {
                        ThrowWin32Exception();
                    }
                }
            }
            resDir.idCount = (ushort)newIdEntries.Length;
            resDir.idEntries = newIdEntries;
            byte[] resDirData = resDir.ToByteArray();
            GCHandle resDirHandle = GCHandle.Alloc(resDirData, GCHandleType.Pinned);
            bool resultTwo = ResourceAPI.UpdateResource(
                hUpdate, (IntPtr)ResourceAPI.ResourceType.GroupIcon,
                ptrFirstGroupIcon, languageOfFirstGroupIcon,
                resDirHandle.AddrOfPinnedObject(),
                (uint)resDirData.Length);
            if ((uint)ptrFirstGroupIcon > ushort.MaxValue)
                Marshal.FreeHGlobal(ptrFirstGroupIcon);
            resDirHandle.Free();
            if (!resultTwo) ThrowWin32Exception();
            if (!ResourceAPI.EndUpdateResource(hUpdate, false))
            {
                ThrowWin32Exception();
            }

            // Re-load the library after freeing it for other 
            // methods in this class
            LoadResourceLibrary();
        }
        #endregion

        #region Static Helpers
        /// <summary>
        /// Checks if a stream contains a valid icon or not
        /// (checks via reserved and type fields)
        /// </summary>
        /// <param name="icoStream">Stream containing icon, 
        /// it is not closed when this method returns.</param>
        /// <returns>true if stream looks like valid icon</returns>
        public static bool IsValidIcon(Stream icoStream)
        {
            BinaryReader iconBReader = new BinaryReader(icoStream);
            ushort idReserved = iconBReader.ReadUInt16();
            ushort idType = iconBReader.ReadUInt16();
            icoStream.Seek(0, SeekOrigin.Begin);
            return idReserved == 0 && idType == 1;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Loads the resource library
        /// </summary>
        void LoadResourceLibrary()
        {
            this.hModule = DllFunctions.LoadLibraryEx(this.filePath, IntPtr.Zero, 2);
            if (this.hModule == IntPtr.Zero)
            {
                ThrowWin32Exception();
            }
        }

        /// <summary>
        /// Get a resource name from a pointer (which is either 
        /// a literal integer or a pointer to a string)
        /// </summary>
        /// <param name="ptrToNameOrId">Pointer to resource name or Id</param>
        /// <returns>Returns string resource name</returns>
        static string GetResourceName(IntPtr ptrToNameOrId)
        {
            string data;
            ushort id;
            if (TryGetResourceId(ptrToNameOrId, out id))
            {
                data = "#" + id.ToString();
            }
            else
            {
                data = Marshal.PtrToStringAuto(ptrToNameOrId);
            }
            return data;
        }

        /// <summary>
        /// Try to get a resource id (integer), returns true if integer, 
        /// false if string
        /// </summary>
        /// <param name="ptrToNameOrId">Pointer to resource name or Id</param>
        /// <param name="id">Resource Id as an integer if true</param>
        /// <returns>true if successful</returns>
        static bool TryGetResourceId(IntPtr ptrToNameOrId, out ushort id)
        {
            if ((uint)ptrToNameOrId < ushort.MaxValue)
            {
                id = (ushort)ptrToNameOrId.ToInt32();
                return true;
            }
            else
            {
                id = 0;
                return false;
            }
        }

        static void ThrowWin32Exception()
        {
            throw new Win32Exception();
        }

        bool ReadRawResourceBytes(string resType, string resName,
            ushort languageId, out IntPtr pFirstByte, out uint lengthOfArray)
        {
            IntPtr hFound = ResourceAPI.FindResourceEx(this.hModule, resType, resName,
                languageId);
            if (hFound == IntPtr.Zero)
            {
                pFirstByte = IntPtr.Zero;
                lengthOfArray = 0;
                return false;
            }
            else
            {
                IntPtr hLoadedRes = ResourceAPI.LoadResource(this.hModule, hFound);
                pFirstByte = ResourceAPI.LockResource(hLoadedRes);
                lengthOfArray = ResourceAPI.SizeofResource(this.hModule, hFound);
                return true;
            }
        }

        static byte[] GetBytesFromPointer(
            IntPtr firstByte, int sizeOfArray)
        {
            byte[] managedArray = new byte[sizeOfArray];
            Marshal.Copy(firstByte, managedArray, 0, sizeOfArray);
            return managedArray;
        }

        void EnsureObjectIsAlive()
        {
            if (this.disposed)
                throw new ObjectDisposedException(this.ToString());
        }
        #endregion

        #region IDisposable members
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources that can be explicitly disposed
            }

            if (this.hModule != IntPtr.Zero)
            {
                DllFunctions.FreeLibrary(this.hModule);
                this.hModule = IntPtr.Zero;
            }

            this.disposed = true;
        }

        /// <summary>
        /// Unloads the module from memory and releases any resources 
        /// consumed by this instance
        /// </summary>
        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Backup disposer
        ~ResourceEditor()
        {
            Dispose(false);
        }
        #endregion
    }
}
