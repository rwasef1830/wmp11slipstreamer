using System;
using System.IO;

namespace Epsilon.IO.Compression.Cabinet
{
	/// <summary>
	/// Provides constants and helper functions to convert 
    /// between stdlib flags and .NET I/O flags.
	/// </summary>
	public static class FCntl
	{
        [Flags]
        public enum COpenModes : int
        {
            ReadOnly = 0x0000,
            WriteOnly = 0x0001,
            ReadWrite = 0x0002,
            Append = 0x0008,
            Create = 0x0100,
            Truncate = 0x0200,
            Exclusive = 0x0400,
            TextMode = 0x4000,
            BinaryMode = 0x8000,

            Temporary = 0x0040,
            ShortLived = 0x1000,
            Sequential = 0x0020,
            Random = 0x0010
        }

        [Flags]
        public enum CShareModes : int
        {
            ShareRead = 0x0100,
            ShareWrite = 0x0080
        }

        /// <summary>
        /// Returns a .NET FileAccess value from the passed Windows file access flags
        /// </summary>
        /// <param name="oflag">The Windows file access flags.</param>
        /// <returns>The FileAccess value that corresponds to the 
        /// passed Windows flags.</returns>
		public static FileAccess FileAccessFromOFlag(int oflag)
		{
            FileAccess fAccess = FileAccess.Read;

            // Translate access and sharing flags into .NET equivalents.
            switch (oflag & (int)(COpenModes.ReadOnly | COpenModes.WriteOnly 
                | COpenModes.ReadWrite))
            {
                case (int)COpenModes.ReadOnly:
                    fAccess = FileAccess.Read;
                    break;
                case (int)COpenModes.WriteOnly:
                    fAccess = FileAccess.Write;
                    break;
                case (int)COpenModes.ReadWrite:
                    fAccess = FileAccess.ReadWrite;
                    break;
            }
            return fAccess;
		}

        /// <summary>
        /// Returns a .NET FileMode value from the passed Windows file mode flags.
        /// </summary>
        /// <param name="oflag">The Windows file mode flags</param>
        /// <returns>The FileMode value that corresponds to the 
        /// passed Windows flags.</returns>
		public static FileMode FileModeFromOFlag(int oflag)
		{
			FileMode fMode;

			// creation mode flags
			if ((oflag & (int)COpenModes.Create) != 0)
			{
				if ((oflag & (int)COpenModes.Exclusive) != 0)
					fMode = FileMode.CreateNew;
				else if ((oflag & (int)COpenModes.Truncate) != 0)
					fMode = FileMode.Create;
				else
					fMode = FileMode.OpenOrCreate;
			}
			else if ((oflag & (int)COpenModes.Truncate) != 0)
				fMode = FileMode.Truncate;
			else if ((oflag & (int)COpenModes.Exclusive) != 0)
				fMode = FileMode.Open;
			else
				fMode = FileMode.Open;

			return fMode;
		}

        /// <summary>
        /// Returns a .NET FileAttributes value from the passed Windows 
        /// file attributes flags.
        /// </summary>
        /// <param name="attrs">The Windows file access flags.</param>
        /// <returns>The FileAttributes value that corresponds to the passed 
        /// Windows file attributes flags.</returns>
		public static FileAttributes FileAttributesFromFAttrs(ushort attrs)
		{
            return (FileAttributes)attrs;
		}
	
        /// <summary>
        /// Returns the Windows file access flags from the passed FileAttributes value
        /// </summary>
        /// <param name="fa">The .NET FileAttributes value to convert.</param>
        /// <returns>The Windows file access flags that correspond to the 
        /// passed FileAttributes value.</returns>
		public static ushort FAttrsFromFileAttributes(FileAttributes fa)
		{
            return (ushort)fa;
		}

        /// <summary>
        /// Creates a System.DateTime from the passed DOS date and time values.
        /// </summary>
        /// <param name="date">The date to include.</param>
        /// <param name="time">The time to include.</param>
        /// <param name="kind">Indicates whether this is UTC time or 
        /// local or neither</param>
        /// <returns>A DateTime value that corresponds to the passed DOS 
        /// date and time</returns>
        public static DateTime DateTimeFromDosDateTime(ushort date, 
            ushort time, DateTimeKind kind)
        {
            // Format of date is:
            // Bits 0-4 - Day of month (1-31)
            // Bits 5-8 - Month (1-12)
            // Bits 9-15 - Year offset from 1980 (1990 = 10)
            int day = date & 0x001f;
            int month = (date >> 5) & 0x000f;
            int year = 1980 + ((date >> 9) & 0x007f);

            // Format of time is:
            // Bits 0-4 - second divided by 2
            // Bits 5-10 - minute (0-59)
            // Bits 11-15 - (0-23 on a 24-hour clock)
            int second = 2 * (time & 0x001f);
            int minute = (time >> 5) & 0x003f;
            int hour = (time >> 11) & 0x001f;

            return new DateTime(year, month, day, hour, minute, second, kind);
        }

        /// <summary>
        /// Creates DOS date/time fields from a System.DateTime value
        /// </summary>
        /// <param name="fileDate">The System.DateTime to convert.</param>
        /// <param name="fatDate">The returned DOS date.</param>
        /// <param name="fatTime">The returned DOS time.</param>
        public static void DosDateTimeFromDateTime(DateTime fileDate,
            ref ushort fatDate, ref ushort fatTime)
        {
            fatDate = (ushort)(((fileDate.Year - 1980) << 9)
                | (fileDate.Month << 5) | fileDate.Day);
            fatTime = (ushort)((fileDate.Hour << 11)
                | (fileDate.Minute << 5) | (fileDate.Second) / 2);
        }
	}
}
