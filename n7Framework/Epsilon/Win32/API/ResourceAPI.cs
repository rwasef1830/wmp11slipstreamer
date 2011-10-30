using System;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace Epsilon.Win32
{
    public static class ResourceAPI
    {
        #region Interop members
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindResourceEx(
            IntPtr hModule,
            string lpName, string lpType,
            ushort langFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadResource(IntPtr hModule,
            IntPtr hResInfo);

        [DllImport("kernel32.dll")]
        public static extern IntPtr LockResource(IntPtr hResLoad);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr BeginUpdateResource(string file, bool deleteExisting);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool UpdateResource(
            IntPtr hUpdate,
            IntPtr lpType,
            IntPtr lpName,
            ushort langFlags,
            IntPtr lpData,
            uint cbData
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool EndUpdateResource(IntPtr hUpdate, bool discard);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SizeofResource(IntPtr hModule, IntPtr hResFound);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool EnumResourceNames(IntPtr hModule, 
            string lpType, EnumResNameProc callback, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool EnumResourceLanguages(IntPtr hModule,
            string lpType, IntPtr lpName, EnumResLangProc callback, 
            IntPtr lParam);
        #endregion

        #region Helpers
        public static IntPtr FindResourceEx(
            IntPtr hModule,
            string lpName,
            ResourceType resType,
            ushort langFlags)
        {
            return FindResourceEx(hModule, lpName, "#" + (int)resType, langFlags);
        }
        public static bool UpdateResource(
            IntPtr hUpdate,
            ResourceType resType,
            ushort id,
            ushort langFlags,
            IntPtr lpData,
            uint cbData)
        {
            return UpdateResource(hUpdate, (IntPtr)resType, (IntPtr)id,
                langFlags, lpData, cbData);
        }
        public static bool UpdateResource(
            IntPtr hUpdate,
            string lpType,
            ushort id,
            ushort langFlags,
            IntPtr lpData,
            uint cbData)
        {
            IntPtr pType = Marshal.StringToHGlobalAuto(lpType);
            bool result = UpdateResource(hUpdate, pType, 
                (IntPtr)id, langFlags, lpData, cbData);
            Marshal.FreeHGlobal(pType);
            return result;
        }
        public static bool UpdateResource(
            IntPtr hUpdate,
            ResourceType resType,
            string lpName,
            ushort langFlags,
            IntPtr lpData,
            uint cbData)
        {
            IntPtr pName = Marshal.StringToHGlobalAuto(lpName);
            bool result = UpdateResource(hUpdate, (IntPtr)resType,
                pName, langFlags, lpData, cbData);
            Marshal.FreeHGlobal(pName);
            return result;
        }
        public static bool UpdateResource(
            IntPtr hUpdate,
            string lpType,
            string lpName,
            ushort langFlags,
            IntPtr lpData,
            uint cbData)
        {
            IntPtr pName = Marshal.StringToHGlobalAuto(lpName);
            IntPtr pType = Marshal.StringToHGlobalAuto(lpType);
            bool result = UpdateResource(hUpdate, pType,
                pName, langFlags, lpData, cbData);
            Marshal.FreeHGlobal(pName);
            Marshal.FreeHGlobal(pType);
            return result;
        }
        public static bool EnumResourceNames(
            IntPtr hModule,
            ResourceType resType, 
            EnumResNameProc callback, 
            IntPtr lParam)
        {
            return EnumResourceNames(hModule, "#" + (int)resType, 
                callback, lParam);
        }
        public static bool EnumResourceLanguages(IntPtr hModule,
            ResourceType resType, string lpName, EnumResLangProc callback,
            IntPtr lParam)
        {
            IntPtr pName = Marshal.StringToHGlobalAuto(lpName);
            bool result = EnumResourceLanguages(hModule, resType, pName, 
                callback, lParam);
            Marshal.FreeHGlobal(pName);
            return result;
        }
        public static bool EnumResourceLanguages(IntPtr hModule,
            ResourceType resType, IntPtr lpName, EnumResLangProc callback,
            IntPtr lParam)
        {
            string lpType = "#" + (int)resType;
            return EnumResourceLanguages(hModule, lpType, lpName, callback,
                lParam);
        }
        #endregion

        #region Delegate callbacks
        /// <summary>
        /// Delegate defining callback method for EnumResourceNames
        /// </summary>
        /// <returns>true to continue enumeration, false to stop</returns>
        public delegate bool EnumResNameProc(
            IntPtr hModule, 
            IntPtr lpType, 
            IntPtr lpName,
            IntPtr lParam);

        /// <summary>
        /// Delegate defining callback method for EnumResourceLanguages
        /// </summary>
        /// <returns>true to continue enumeration, false to stop</returns>
        public delegate bool EnumResLangProc(      
            IntPtr hModule,
            IntPtr lpType,
            IntPtr lpName,
            ushort wIDLanguage,
            IntPtr lParam);
        #endregion

        #region Enumerations
        /// <summary>
        /// Standard Win32 Resource Types
        /// </summary>
        public enum ResourceType
        {
            Cursor = 1,
            Bitmap = 2,
            Icon = 3,
            Menu = 4,
            Dialog = 5,
            String = 6,
            FontDir = 7,
            Font = 8,
            Accelerator = 9,
            RcData = 10,
            MessageTable = 11,
            GroupCursor = 12,
            GroupIcon = 14,
            Version = 16,
            DialogInclude = 17,
            PlugAndPlay = 19,
            Vxd = 20,
            AnimatedCursor = 21,
            AnimatedIcon = 22
        }
        #endregion
    }
}
