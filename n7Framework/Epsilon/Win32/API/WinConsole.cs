using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.Win32.API
{
    public static class WinConsole
    {
        public const int DefaultConsoleBufferSize = 256;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfo(IntPtr consoleOutput,
            out ConsoleScreenBufferInfo info);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetConsoleTitle(StringBuilder text, uint size);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(StandardHandle handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCursorPosition(IntPtr buffer, 
            Windows.Coord position);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int FillConsoleOutputCharacter(IntPtr buffer, 
            char character, uint length, Windows.Coord position, out uint written);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleTextAttribute(IntPtr hConsoleOutput, 
            ushort wAttributes);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetConsoleTitle(string lpConsoleTitle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine routine, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateConsoleScreenBuffer(
            GenericAccessRights access, ConsoleShareMode share, IntPtr security, 
            uint flagsMustBe1, IntPtr reserved);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleActiveScreenBuffer(IntPtr handle);

        [DllImport("kernel32.dll")]
        public static extern uint GetConsoleCP();

        [DllImport("kernel32.dll")]
        public static extern uint GetConsoleOutputCP();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr handle, out int flags);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetStdHandle(StandardHandle stdHandle, IntPtr handle2);

        [Flags]
        public enum ConsoleShareMode : uint
        {
            Read = 1,
            Write = 2
        }

        public enum StandardHandle : uint
        {
            StdIn = unchecked((uint)-10),
            StdOut = unchecked((uint)-11),
            StdErr = unchecked((uint)-12),
        }

        public delegate bool HandlerRoutine(int type);

        [StructLayout(LayoutKind.Sequential)]
        public struct ConsoleScreenBufferInfo
        {
            public Windows.Coord Size;
            public Windows.Coord CursorPosition;
            public ConsoleColor Attributes;
            public Windows.SmallRect Window;
            public Windows.Coord MaximumWindowSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ConsoleSelectionInfo
        {
            public int Flags;
            public Windows.Coord SelectionAnchor;
            public Windows.SmallRect Selection;
        }
    }
}
