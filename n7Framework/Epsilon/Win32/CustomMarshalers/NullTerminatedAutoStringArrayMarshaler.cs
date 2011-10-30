using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace Epsilon.Win32.CustomMarshalers
{
    /// <summary>
    /// Marshals a managed array of strings returning a pointer to a string that
    /// has the following format: String1\0String2\0String3\0\0 to unmanaged code.
    /// 
    /// It cannot be used to marshal back from unmanaged to managed.
    /// 
    /// It uses char size as determined by <see cref="Marshal.SystemDefaultCharSize" />.
    /// </summary>
    public sealed class NullTerminatedAutoStringArrayMarshaler : ICustomMarshaler
    {
        static readonly NullTerminatedAutoStringArrayMarshaler s_Instance
            = new NullTerminatedAutoStringArrayMarshaler();

        NullTerminatedAutoStringArrayMarshaler() { }

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static NullTerminatedAutoStringArrayMarshaler() { }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            return s_Instance;
        }

        public void CleanUpManagedData(object ManagedObj)
        {
            // No action required
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeCoTaskMem(pNativeData);
        }

        /// <summary>
        /// This method is never called by the CLR. Returns -1 because
        /// the size is variable from object to object.
        /// </summary>
        /// <returns></returns>
        public int GetNativeDataSize()
        {
            return -1;
        }

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null) return IntPtr.Zero;

            // Count final null by default
            int numChars = 1;
            int sizeToAllocate;
            IntPtr unmanagedMem;
            Encoding encoder;
            IEnumerable<string> strs;
            int charSize = Marshal.SystemDefaultCharSize;

            if (!typeof(IEnumerable<string>).IsAssignableFrom(managedObj.GetType()))
            {
                throw new NotSupportedException(String.Format(
                    "Cannot marshal object of type {0} using marshaler.",
                    managedObj.GetType()));
            }

            if (charSize == 1)
                encoder = Encoding.Default;
            else if (charSize == 2)
                encoder = Encoding.Unicode;
            else
                throw new NotSupportedException(
                    "The system current char size is not supported.");

            strs = (IEnumerable<string>)managedObj;
            foreach (string str in strs)
            {
                numChars += str.Length; 

                // Null terminator
                numChars++;
            }

            sizeToAllocate = numChars * charSize;
            unmanagedMem = Marshal.AllocCoTaskMem(sizeToAllocate);

            unsafe
            {
                using (Stream memStream = new UnmanagedMemoryStream(
                    (byte*)unmanagedMem, 0, sizeToAllocate, FileAccess.Write))
                {
                    byte[] nullTerminatorBytes = encoder.GetBytes(String.Empty);

                    foreach (string str in strs)
                    {
                        byte[] strBytes = encoder.GetBytes(str);
                        memStream.Write(strBytes, 0, strBytes.Length);
                        memStream.Write(nullTerminatorBytes, 0, nullTerminatorBytes.Length);
                    }

                    memStream.Write(nullTerminatorBytes, 0, nullTerminatorBytes.Length);
                }
            }

            return unmanagedMem;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            throw new NotSupportedException(
                "This marshaler cannot be used to marshal from unmanaged to managed code.");
        }
    }
}