using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.Win32.API
{
    public static class Registry
    {
        /// <param name="ulOptions">Reserved: Must be 0.</param>
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegOpenKeyEx(SafeRegistryHandle hKey,
            string lpSubKey, uint ulOptions, RegDesiredAccess samDesired,
            out SafeRegistryHandle hkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegEnumKeyEx(SafeRegistryHandle hKey,
            uint dwIndex, IntPtr lpName, out uint lpcName,
            IntPtr lpReserved, IntPtr lpClass, IntPtr lpcClass,
            IntPtr lpftLastWriteTime);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public unsafe static extern int RegEnumValue(
              SafeRegistryHandle hKey,
              uint dwIndex,
              IntPtr lpValueName,
              ref uint lpcValueName,
              IntPtr lpReserved,
              ref RegValueType lpType,
              byte* lpData,
              [In, Out] ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public unsafe static extern int RegEnumValue(
              SafeRegistryHandle hKey,
              uint dwIndex,
              IntPtr lpValueName,
              ref uint lpcValueName,
              IntPtr lpReserved,
              ref RegValueType lpType,
              IntPtr lpData,
              ref uint lpcbData);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegQueryInfoKey(
            SafeRegistryHandle hKey,
            IntPtr lpClass, IntPtr lpcbClass,
            IntPtr lpReserved, ref uint lpcSubKeys, IntPtr lpcbMaxSubKeyLen,
            IntPtr lpcbMaxClassLen, ref uint lpcValues,
            ref uint lpcbMaxValueNameLen, IntPtr lpcbMaxValueLen,
            IntPtr lpcbSecurityDescriptor,
            IntPtr lpftLastWriteTime);

        public enum RegValueType : uint
        {
            None = 0,
            String = 1,
            ExpandString = 2,
            Binary = 3,
            Dword = 4,
            DwordLittleEndian = 4,
            DwordBigEndian = 5,
            Link = 6,
            MultiString = 7,
            ResourceList = 8,
            FullResourceDescriptor = 9,
            ResourceRequirementsList = 10,
            Qword = 11,
            QwordLittleEndian = 11
        }

        [Flags]
        public enum RegDesiredAccess : uint
        {
            AllAccess = AccessMask.StandardRightsRequired | QueryValue
                | SetValue | CreateSubKey | EnumerateSubKeys | Notify 
                | CreateLink,
            Read = AccessMask.StandardRightsRead | QueryValue 
                | EnumerateSubKeys | Notify,
            Write = AccessMask.StandardRightsWrite 
                | SetValue | CreateSubKey,
            Delete = AccessMask.Delete,
            ReadControl = AccessMask.ReadControl,
            WriteDac = AccessMask.WriteDac,
            WriteOwner = AccessMask.WriteOwner,

            /// <summary>
            /// Reserved for system use.
            /// </summary>
            CreateLink = 0x0020,
            CreateSubKey = 0x0004,
            EnumerateSubKeys = 0x0008,
            Execute = Read,
            Notify = 0x0010,
            QueryValue = 0x0001,
            SetValue = 0x0002,
            /// <summary>
            /// Indicates that an application on 64-bit Windows should 
            /// operate on the 32-bit registry view.
            /// 
            /// Not supported on Windows 2000.
            /// </summary>
            Wow6432Key = 0x0200,
            /// <summary>
            /// Indicates that an application on 64-bit Windows should 
            /// operate on the 64-bit registry view.
            /// 
            /// Not supported on Windows 2000.
            /// </summary>
            Wow6464Key = 0x0100
        }
    }
}
