using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using Microsoft.Win32;
using Epsilon.Win32.API;
using System.Reflection;
using Epsilon.IO;

namespace Epsilon.DebugServices
{
	/// <summary>
	/// Extended console for both Windows and Console Applications.
	/// </summary>
	public static class HelperConsole
	{
		#region Private static fields
		static IntPtr s_Buffer;
		static bool s_Initialized;
		static bool s_BreakHit;
        static ConsoleWriter s_DebugListenerWriter;
        static StringBuilder s_FormatBuffer;
		#endregion

        #region Public static properties
        public static event WinConsole.HandlerRoutine Break;
        public const string ListenerName = "DebugConsole";
        #endregion

        #region Static constructor
        static HelperConsole()
        {
            s_FormatBuffer = new StringBuilder(200);
        }
        #endregion

        #region Properties
        /// <summary>
		/// Specifies whether the console window should be visible or hidden
		/// </summary>
		public static bool Visible
		{
			get 
			{
				IntPtr hwnd = WinConsole.GetConsoleWindow();
				return hwnd != IntPtr.Zero && Windows.IsWindowVisible(hwnd);
			}
			set
			{
				if (!s_Initialized) Initialize();
				IntPtr hwnd = WinConsole.GetConsoleWindow();
				if (hwnd != IntPtr.Zero)
					Windows.ShowWindow(hwnd, value ? Windows.WindowFlags.Show : Windows.WindowFlags.Hide);
			}
		}

        /// <summary>
        /// Initializes DebugConsole -- should be called at the start of the program using it
        /// </summary>
        public static void Initialize()
        {
            Initialize("DebugConsole");
        }

		/// <summary>
		/// Initializes DebugConsole -- should be called at the start of the program using it
		/// </summary>
        /// <param name="title">Title of the newly created console window</param>
		public static void Initialize(string title)
		{
			IntPtr hwnd = WinConsole.GetConsoleWindow();
			s_Initialized = true;

			WinConsole.SetConsoleCtrlHandler(new WinConsole.HandlerRoutine(HandleBreak), true);
			
			// Console app
			if (hwnd != IntPtr.Zero)
			{
				s_Buffer = WinConsole.GetStdHandle(WinConsole.StandardHandle.StdOut);
				return;
			}

			// Windows app
			bool success = WinConsole.AllocConsole();
			if (!success)
				return;

			s_Buffer = WinConsole.CreateConsoleScreenBuffer(GenericAccessRights.Read 
                | GenericAccessRights.Write, WinConsole.ConsoleShareMode.Read 
                | WinConsole.ConsoleShareMode.Write, IntPtr.Zero, 1, IntPtr.Zero);

			bool result = WinConsole.SetConsoleActiveScreenBuffer(s_Buffer);

			WinConsole.SetStdHandle(WinConsole.StandardHandle.StdOut, s_Buffer);
			WinConsole.SetStdHandle(WinConsole.StandardHandle.StdErr, s_Buffer);

			Title = title;

			Stream s = Console.OpenStandardInput(WinConsole.DefaultConsoleBufferSize);
			StreamReader reader = null;
            if (s == Stream.Null)
                reader = StreamReader.Null;
            else
            {
                reader = new StreamReader(s, 
                    Encoding.GetEncoding((int)WinConsole.GetConsoleCP()),
                    false, WinConsole.DefaultConsoleBufferSize);
            }

			Console.SetIn(reader);
    
			// Set up Console.Out
			StreamWriter writer = null;
			s = Console.OpenStandardOutput(WinConsole.DefaultConsoleBufferSize);
			if (s == Stream.Null) 
				writer = StreamWriter.Null;
			else 
			{
				writer = new StreamWriter(s, Encoding.GetEncoding((int)WinConsole.GetConsoleOutputCP()),
					WinConsole.DefaultConsoleBufferSize);
				writer.AutoFlush = true;
			}

			Console.SetOut(writer);

			s = Console.OpenStandardError(WinConsole.DefaultConsoleBufferSize);
			if (s == Stream.Null) 
				writer = StreamWriter.Null;
			else 
			{
				writer = new StreamWriter(s, Encoding.GetEncoding((int)WinConsole.GetConsoleOutputCP()),
					WinConsole.DefaultConsoleBufferSize);
				writer.AutoFlush = true;
			}
			
			Console.SetError(writer);
		}

		/// <summary>
		/// Gets or sets the title of the console window
		/// </summary>
		public static string Title
		{
			get 
			{
                return Console.Title;
			}
			set
			{
                Console.Title = value;
			}
		}

		/// <summary>
		/// Get the HWND of the console window
		/// </summary>
		public static IntPtr Handle
		{
			get
			{
				if (!s_Initialized) Initialize();
				return WinConsole.GetConsoleWindow();
			}
		}

		/// <summary>
		/// Gets and sets a new parent hwnd to the console window
		/// </summary>
		/// <param name="window"></param>
		public static IntPtr ParentHandle
		{
			get
			{
				IntPtr hwnd = WinConsole.GetConsoleWindow();
				return Windows.GetParent(hwnd);
			}
			set
			{
				IntPtr hwnd = Handle;
				if (hwnd == IntPtr.Zero)
					return;

				Windows.SetParent(hwnd, value);
				uint style = Windows.GetWindowLongPtr(hwnd, Windows.WindowFieldOffset.Style);
                if (value == IntPtr.Zero)
                    Windows.SetWindowLongPtr(hwnd, Windows.WindowFieldOffset.Style, ((style & ~(uint)Windows.WindowStyles.Child) | (uint)Windows.WindowStyles.OverlappedWindow));
                else
                    Windows.SetWindowLongPtr(hwnd, Windows.WindowFieldOffset.Style, ((style | (uint)Windows.WindowStyles.Child) & ~(uint)Windows.WindowStyles.OverlappedWindow));
				Windows.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, Windows.SizePositionFlags.NoSize | Windows.SizePositionFlags.NoZOrder | Windows.SizePositionFlags.NoActivate);
			}
		}

		/// <summary>
		/// Get the current Win32 buffer handle
		/// </summary>

		public static IntPtr Buffer
		{
			get 
			{
				if (!s_Initialized) Initialize();
				return s_Buffer;
			}
		}

		/// <summary>
		/// Produces a simple beep.
		/// </summary>
		public static void Beep()
		{
			Epsilon.Win32.API.Sound.MessageBeep(Win32.API.Sound.MessageBeepType.SimpleBeep);
		}

		/// <summary>
		/// Flashes the console window
		/// </summary>
		/// <param name="once">if off, flashes repeated until the user makes the console foreground</param>
		public static void Flash(bool once)
		{
			IntPtr hwnd = WinConsole.GetConsoleWindow();
			if (hwnd == IntPtr.Zero)
				return;

            uint style = (Windows.GetWindowLongPtr(hwnd, Windows.WindowFieldOffset.Style));
			if ((style & (uint)Windows.WindowStyles.Caption) == 0)
				return;

			Windows.FlashWInfo info = new Windows.FlashWInfo();
			info.Size = (uint)Marshal.SizeOf(typeof(Windows.FlashWInfo));
			info.Flags = Windows.FlashStatus.All;
			if (!once) info.Flags |= Windows.FlashStatus.TimerUntilForgeground;
			Windows.FlashWindowEx(ref info);
		}

		/// <summary>
		/// Clear the console window
		/// </summary>
		public static void Clear()
		{
			if (!s_Initialized) Initialize();
            Console.Clear();
		}

        /// <summary>
        /// Gets or sets the row position of the console cursor
        /// </summary>
        public static int CursorTop
        {
            get
            {
                if (!s_Initialized) Initialize();
                return Console.CursorTop;
            }
            set
            {
                if (!s_Initialized) Initialize();
                Console.CursorTop = value;
            }
        }

        /// <summary>
        /// Gets or sets the left column position of the console cursor
        /// </summary>
        public static int CursorLeft
        {
            get
            {
                if (!s_Initialized) Initialize();
                return Console.CursorLeft;
            }
            set
            {
                if (!s_Initialized) Initialize();
                Console.CursorLeft = value;
            }
        }

		/// <summary>
		/// Returns a coordinates of visible window of the buffer
		/// </summary>
		public static Windows.SmallRect ScreenSize
		{
			get { return Info.Window; }
		}

		/// <summary>
		/// Returns the size of buffer
		/// </summary>
		public static Windows.Coord BufferSize
		{
			get { return Info.Size; }
		}

		/// <summary>
		/// Returns the maximum size of the screen given the desktop dimensions
		/// </summary>
		public static Windows.Coord MaximumScreenSize
		{
			get { return Info.MaximumWindowSize; }
		}

		/// <summary>
		/// Redirects debug output to the console
		/// </summary>
		/// <param name="clear">clear all other listeners first</param>
		/// <param name="color">color to use for display debug output</param>
        [ConditionalAttribute("DEBUG")]
		public static void RedirectDebugOutput(bool clear, ConsoleColor color, bool beep)
		{
			if (clear)
			{
				Debug.Listeners.Clear();
			}
            
            s_DebugListenerWriter = new ConsoleWriter(Console.Out, color,
                ConsoleFlashMode.FlashUntilResponse, beep);
			Debug.Listeners.Add(
                new TextWriterTraceListener(s_DebugListenerWriter, "console"));
		}

		/// <summary>
		/// Redirects trace output to the console
		/// </summary>
		/// <param name="clear">clear all other listeners first</param>
		/// <param name="color">color to use for display trace output</param>
        [ConditionalAttribute("TRACE")]
		public static void RedirectTraceOutput(bool clear, ConsoleColor color)
		{
			if (clear)
			{
				Trace.Listeners.Clear();
				// Trace.Listeners.Remove("Default");
			}
			Trace.Listeners.Add( new TextWriterTraceListener(new ConsoleWriter(Console.Error, color, 0, false), "console") );
		}


		/// <summary>
		/// Returns various information about the screen buffer
		/// </summary>
		static WinConsole.ConsoleScreenBufferInfo Info
		{
			get
			{
				WinConsole.ConsoleScreenBufferInfo info 
                    = new WinConsole.ConsoleScreenBufferInfo();
				IntPtr buffer = Buffer;
				if (buffer!=IntPtr.Zero)
					WinConsole.GetConsoleScreenBufferInfo(buffer, out info);
				return info;
			}
		}

		/// <summary>
		/// Gets or sets the current color and attributes of text 
		/// </summary>
		public static System.ConsoleColor Color
		{
			get 
			{
				return Info.Attributes;
			}
			set
			{
				IntPtr buffer = Buffer;
				if (buffer != IntPtr.Zero)
					WinConsole.SetConsoleTextAttribute(buffer, (ushort)value);
			}
		}

		/// <summary>
		/// Returns true if Ctrl-C or Ctrl-Break was hit since the last time this property
		/// was called. The value of this property is set to false after each request.
		/// </summary>
		public static bool CtrlBreakPressed
		{
			get 
			{ 
				bool value = s_BreakHit;
				s_BreakHit = false;
				return value; 
			}
		}

		static bool HandleBreak(int type)
		{
			s_BreakHit = true;
			if (Break != null)
				Break(type);
			return true;
		}
		#endregion

		#region Location
        /// <summary>
		/// Gets the Console Window location and size in pixels
		/// </summary>
		public static void GetWindowPosition(out int x, out int y, out int width, 
            out int height)
		{
			Windows.Rect rect = new Windows.Rect();
			Windows.GetClientRect(Handle, ref rect);
			x = rect.top;
			y = rect.left;
			width = rect.right - rect.left;
			height = rect.bottom - rect.top;
		}

		/// <summary>
		/// Sets the console window location and size in pixels
		/// </summary>
		public static void SetWindowPosition(int x, int y, int width, int height)
		{
			Windows.SetWindowPos(Handle, IntPtr.Zero, x, y, width, height, 
                Windows.SizePositionFlags.NoZOrder 
                | Windows.SizePositionFlags.NoActivate);
		}

		#endregion

		#region Console Replacements
		/// <summary>
		/// Returns the error stream (same as Console.Error)
		/// </summary>
		public static TextWriter Error
		{
			get 
			{ 
				return Console.Error; 
			} 
		}

		/// <summary>
		/// Returns the input stream (same as Console.In)
		/// </summary>
		public static TextReader In
		{
			get { return Console.In; }
		}

		/// <summary>
		/// Returns the output stream (same as Console.Out)
		/// </summary>
		public static TextWriter Out 
		{
			get { return Console.Out; }
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static Stream OpenStandardInput() 
		{
			return Console.OpenStandardInput();
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static Stream OpenStandardInput(int bufferSize) 
		{
			return Console.OpenStandardInput(bufferSize);
		}

		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static Stream OpenStandardError() 
		{
			return Console.OpenStandardError();
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static Stream OpenStandardError(int bufferSize) 
		{
			return Console.OpenStandardError(bufferSize);
		}

		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static Stream OpenStandardOutput() 
		{
			return Console.OpenStandardOutput();
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static Stream OpenStandardOutput(int bufferSize) 
		{
			return Console.OpenStandardOutput(bufferSize);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void SetIn(TextReader newIn) 
		{
			Console.SetIn(newIn);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void SetOut(TextWriter newOut) 
		{
			Console.SetOut(newOut);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void SetError(TextWriter newError) 
		{
			Console.SetError(newError);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static int Read()
		{
			return Console.Read();
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static String ReadLine()
		{
			return Console.ReadLine();
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine()
		{
			Console.WriteLine();
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(bool value)
		{
			Console.WriteLine(value);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(char value)
		{
			Console.WriteLine(value);
		}   
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(char[] buffer)
		{
			Console.WriteLine(buffer);
		}
                   
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(char[] buffer, int index, int count)
		{
			Console.WriteLine(buffer, index, count);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(decimal value)
		{
			Console.WriteLine(value);
		}   

		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(double value)
		{
			Console.WriteLine(value);
		}   
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(float value)
		{
			Console.WriteLine(value);
		}   
           
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(int value)
		{
			Console.WriteLine(value);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(uint value)
		{
			Console.WriteLine(value);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(long value)
		{
			Console.WriteLine(value);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(ulong value)
		{
			Console.WriteLine(value);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(Object value)
		{
			Console.WriteLine(value);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(String value)
		{
			Console.WriteLine(value);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(String format, Object arg0)
		{
			Console.WriteLine(format, arg0);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(String format, Object arg0, Object arg1)
		{
			Console.WriteLine(format, arg0, arg1);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(String format, Object arg0, Object arg1, Object arg2)
		{
			Console.WriteLine(format, arg0, arg1, arg2);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void WriteLine(String format, params Object[] arg)
		{
			Console.WriteLine(format, arg);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(String format, Object arg0)
		{
			Console.Write(format, arg0);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(String format, Object arg0, Object arg1)
		{
			Console.Write(format, arg0, arg1);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(String format, Object arg0, Object arg1, Object arg2)
		{
			Console.Write(format, arg0, arg1, arg2);
		}

		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(String format, params Object[] arg)
		{
			Console.Write(format, arg);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(bool value)
		{
			Console.Write(value);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(char value)
		{
			Console.Write(value);
		}   
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(char[] buffer)
		{
			Console.Write(buffer);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(char[] buffer, int index, int count)
		{
			Console.Write(buffer, index, count);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(double value)
		{
			Console.Write(value);
		}   
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(decimal value)
		{
			Console.Write(value);
		}   
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(float value)
		{
			Console.Write(value);
		}   
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(int value)
		{
			Console.Write(value);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(uint value)
		{
			Console.Write(value);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(long value)
		{
			Console.Write(value);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(ulong value)
		{
			Console.Write(value);
		}
    
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(Object value)
		{
			Console.Write(value);
		}
        
		/// <summary>
		/// Same as the Console counterpart
		/// </summary>
		public static void Write(String value)
		{
			Console.Write(value);
		}

		#endregion

        #region Static helpers
        public static void InitializeDefaultConsole(string logPath)
        {
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            Initialize(FormatString("{0} Debug Console", assemblyName.Name));
            Epsilon.DebugServices.HelperConsole.RedirectDebugOutput(false,
                ConsoleColor.White, false);
            string title = FormatString("** {0} v{1}", assemblyName.Name, assemblyName.Version);
            Debug.WriteLine(title);
            if (!String.IsNullOrEmpty(logPath))
            {
                FileSystem.DeleteFile(logPath);
                ((DefaultTraceListener)Debug.Listeners["Default"]).LogFileName = logPath;
                Debug.Listeners["Default"].WriteLine(title);
                Debug.WriteLine(FormatString("** Log: \"{0}\"", logPath));
                Debug.Write("** Log created on: ");
                Debug.WriteLine(DateTime.Now.ToUniversalTime().ToString(
                    "dddd dd/MM/yyyy - HH:MM:ss tt \\G\\M\\T"));
            }
            else
            {
                Epsilon.DebugServices.HelperConsole.WarnWriteLine(
                    "** Messages here will be discarded after program terminates.");
            }

#if BETA
            HelperConsole.DebugWrite("This is a beta version. Do not distribute.", "DISCLAIMER", 
                ConsoleColor.Magenta);
            HelperConsole.RawWriteLine(null);
#endif

            WarnWriteLine("** Closing this window will terminate the application.");
            Debug.WriteLine(null);
        }

        public static string FormatString(string format, params object[] args)
        {
            s_FormatBuffer.AppendFormat(format, args);
            string result = s_FormatBuffer.ToString();
            s_FormatBuffer.Length = 0;
            return result;
        }
        #endregion

        #region Debug Helpers
        [ConditionalAttribute("DEBUG")]
        public static void InfoWrite(string message, string category) { DebugWrite(message, category, ConsoleColor.White); }
        [ConditionalAttribute("DEBUG")]
        public static void ErrorWrite(string message) { DebugWrite(message, "ERR", ConsoleColor.Red); }
        [ConditionalAttribute("DEBUG")]
        public static void WarnWrite(string message) { DebugWrite(message, "WARN", ConsoleColor.Yellow); }
        [ConditionalAttribute("DEBUG")]
        public static void RawWrite(string message) { DebugWrite(message, null, ConsoleColor.White); }

        [ConditionalAttribute("DEBUG")]
        public static void ErrorWriteLine(string message) {
            ErrorWrite(message); Debug.Write(Environment.NewLine); }
        [ConditionalAttribute("DEBUG")]
        public static void WarnWriteLine(string message) {
            WarnWrite(message); Debug.Write(Environment.NewLine); }
        [ConditionalAttribute("DEBUG")]
        public static void InfoWriteLine(string message, string category) {
            InfoWrite(message, category); Debug.Write(Environment.NewLine); }
        [ConditionalAttribute("DEBUG")]
        public static void RawWriteLine(string message) { 
            RawWrite(message); Debug.Write(Environment.NewLine); }

        [ConditionalAttribute("DEBUG")]
        public static void DebugWrite(string message, string category, 
            ConsoleColor color)
        {
            if (s_DebugListenerWriter != null)
            {
                ConsoleColor oldColor;
                oldColor = s_DebugListenerWriter.Color;
                s_DebugListenerWriter.Color = color;
                if (!String.IsNullOrEmpty(category))
                    Debug.Write(message, category);
                else
                    Debug.Write(message);
                s_DebugListenerWriter.Color = oldColor;
            }
        }
        #endregion
    }
}