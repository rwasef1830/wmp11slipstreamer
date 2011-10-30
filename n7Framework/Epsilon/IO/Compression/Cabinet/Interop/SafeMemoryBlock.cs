using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Epsilon.IO.Compression.Cabinet
{
    public class SafeMemoryBlock : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal readonly int AllocatedSize;

        /// <summary>
        /// Allocates a safe unmanaged memory block of a certain size
        /// </summary>
        /// <param name="sizeNeeded">Amount of memory to allocate</param>
        SafeMemoryBlock(int sizeNeeded) : base(true)
        {
            this.handle = Marshal.AllocHGlobal(sizeNeeded);
            this.AllocatedSize = sizeNeeded;
        }

        /// <summary>
        /// Allocates a safe unmanaged memory block the size of the specified type.
        /// </summary>
        internal SafeMemoryBlock(Type T) : this(Marshal.SizeOf(T)) { }

        /// <summary>
        /// Allocates a safe unmanaged memory block and copies the contents 
        /// of the specified structure to it.
        /// </summary>
        internal SafeMemoryBlock(object structure) : this(Marshal.SizeOf(structure))
        {
            Marshal.StructureToPtr(structure, this.handle, true);
        }

        /// <summary>
        /// Copies the object from unmanaged memory back to the specified type.
        /// </summary>
        /// <returns>Marshalled object</returns>
        internal T GetStructure<T>()
        {
            return (T)Marshal.PtrToStructure(this.handle, typeof(T));
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(this.handle);
            return true;
        }
    }
}
