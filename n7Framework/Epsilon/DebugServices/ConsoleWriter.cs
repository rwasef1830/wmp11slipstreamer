using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using Microsoft.Win32;

namespace Epsilon.DebugServices
{
	public class ConsoleWriter : TextWriter
	{
		#region Variables
		TextWriter writer;
		System.ConsoleColor color;
		ConsoleFlashMode flashing;
		bool beep;
		#endregion

		#region Construction
		public ConsoleWriter(TextWriter writer, ConsoleColor color, 
            ConsoleFlashMode mode, bool beep) 
		{
			this.color = color;
			this.flashing = mode;
			this.writer = writer;
			this.beep = beep;
		}
		#endregion

		#region Properties
		public ConsoleColor Color
		{
			get { return color; }
			set { color = value; }
		}
		
		public ConsoleFlashMode FlashMode
		{
			get { return flashing; }
			set { flashing = value; }
		}

		public bool BeepOnWrite
		{
			get { return beep; }
			set { beep = value; }
		}

		public override Encoding Encoding
		{
			get { return writer.Encoding; }
        }
        #endregion

        #region Write Routines
        protected void Flash()
		{
			switch (flashing)
			{
				case ConsoleFlashMode.FlashOnce:
					HelperConsole.Flash(true);
					break;
				case ConsoleFlashMode.FlashUntilResponse:
					HelperConsole.Flash(false);
					break;
			}

			if (beep)
				HelperConsole.Beep();
		}

		public override void Write(char ch)
		{
			System.ConsoleColor oldColor = HelperConsole.Color;
			try
			{
				HelperConsole.Color = color;
				writer.Write(ch);
			}
			finally
			{
				HelperConsole.Color = oldColor;
			}
			Flash();
		}

		public override void Write(string s)
		{
			ConsoleColor oldColor = HelperConsole.Color;
			try
			{
				HelperConsole.Color = color;
				Flash();
				writer.Write(s);
			}
			finally
			{
				HelperConsole.Color = oldColor;
			}
			Flash();
		}

		public override void Write(char[] data, int start, int count)
		{
			ConsoleColor oldColor = HelperConsole.Color;
			try
			{
				HelperConsole.Color = color;
				writer.Write(data, start, count);
			}
			finally
			{
				HelperConsole.Color = oldColor;
			}
			Flash();
		}
        #endregion
    }

    /// <summary>
    /// Summary description for ConsoleWriter.
    /// </summary>
    public enum ConsoleFlashMode
    {
        NoFlashing,
        FlashOnce,
        FlashUntilResponse,
    }
}