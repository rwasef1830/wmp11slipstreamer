using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.Win32.API
{
    public static class Windows
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool ShowWindow(IntPtr hwnd, WindowFlags nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool FlashWindowEx(ref FlashWInfo info);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLong")]
        public static extern uint GetWindowLongPtr(IntPtr hWnd, WindowFieldOffset nIndex);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        public static extern uint SetWindowLongPtr(IntPtr hWnd, WindowFieldOffset nIndex, uint newValue);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwnd2);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int x, int y, int cx, int cy, SizePositionFlags flags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool GetClientRect(IntPtr hWnd, ref Rect rect);

        [Flags]
        public enum WindowFlags : uint
        {
            Hide = 0,
            ShowNormal = 1,
            Normal = 1,
            ShowMinimized = 2,
            ShowMaximized = 3,
            Maximize = 3,
            ShowNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActive = 7,
            ShowNormalNoActivate = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimize = 11
        }

        [Flags]
        public enum WindowStyles : uint
        {
            Overlapped = 0x00000000,
            Popup = 0x80000000,
            Child = 0x40000000,
            Minimize = 0x20000000,
            Visible = 0x10000000,
            Disabled = 0x08000000,
            ClipSiblings = 0x04000000,
            ClipChildren = 0x02000000,
            Maximize = 0x01000000,
            Border = 0x00800000,
            DlgFrame = 0x00400000,
            Vscroll = 0x00200000,
            Hscroll = 0x00100000,
            SysMenu = 0x00080000,
            ThickFrame = 0x00040000,
            Group = 0x00020000,
            TabStop = 0x00010000,

            MinimizeBox = 0x00020000,
            MaximizeBox = 0x00010000,

            Caption = Border | DlgFrame,
            Tiled = Overlapped,
            Iconic = Minimize,
            Sizebox = ThickFrame,
            TiledWindow = OverlappedWindow,

            OverlappedWindow = Overlapped | Caption | SysMenu | ThickFrame | MinimizeBox | MaximizeBox,
            PopupWindow = Popup | Border | SysMenu,
            ChildWindow = Child
        }

        [Flags]
        public enum SizePositionFlags : uint
        {
            NoSize = 0x1,
            NoMove = 0x2,
            NoZOrder = 0x4,
            NoRedraw = 0x8,
            NoActivate = 0x10,
            FrameChanged = 0x20,
            ShowWindow = 0x40,
            HideWindow = 0x80,
            NoCopyBits = 0x100,
            NoOwnerZOrder = 0x200,
            NoSendChanging = 0x400,
            DrawFrame = FrameChanged,
            NoReposition = NoOwnerZOrder,
            DeferErase = 0x2000,
            AsyncWindowPos = 0x4000
        }

        [Flags]
        public enum FlashStatus : uint
        {
            /// <summary>
            /// Stop flashing
            /// </summary>
            Stop = 0,
            /// <summary>
            /// Flash titlebar
            /// </summary>
            Caption = 1,
            /// <summary>
            /// Flash taskbar button
            /// </summary>
            Tray = 2,
            /// <summary>
            /// Flash titlebar and taskbar button
            /// </summary>
            All = Caption | Tray,
            /// <summary>
            /// Flash continuously until Stop flag is set
            /// </summary>
            Timer = 4,
            /// <summary>
            /// Flash continuously until the window comes to the foreground
            /// </summary>
            TimerUntilForgeground = 0xC,
        }

        public enum WindowFieldOffset : int
        {
            WndProc = -4,
            HInstance = -6,
            HWndParent = -8,
            Style = -16,
            ExStyle = -20,
            UserData = -21,
            Id = -12,
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct SmallRect
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FlashWInfo
        {
            public uint Size;
            public IntPtr Hwnd;
            public FlashStatus Flags;
            public uint Count;
            public uint Timeout;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Coord
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}
