using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace Epsilon.Win32.API
{
    public static class ProcessesAndThreads
    {
        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcess(
            StringBuilder applicationName,
            [In, Out] string commandLine,
            ref SecurityAttributes processAttributes,
            ref SecurityAttributes threadAttributes,
            bool inheritHandles,
            ProcessCreationFlags creationFlags,
            IntPtr pEnvironment,
            StringBuilder workingDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInfo
        );

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcess(
            StringBuilder applicationName,
            [In, Out] string commandLine,
            ref SecurityAttributes processAttributes,
            ref SecurityAttributes threadAttributes,
            bool inheritHandles,
            ProcessCreationFlags creationFlags,
            IntPtr pEnvironment,
            StringBuilder workingDirectory,
            ref StartupInfoEx startupInfoEx,
            out ProcessInformation processInfo
        );

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessAsUser(
            SafeWin32Handle hToken,
            StringBuilder applicationName,
            string commandLine,
            ref SecurityAttributes processAttributes,
            ref SecurityAttributes threadAttributes,
            bool inheritHandles,
            ProcessCreationFlags creationFlags,
            IntPtr pEnvironment,
            StringBuilder workingDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInfo
        );

        [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessAsUser(
            SafeWin32Handle hToken,
            StringBuilder applicationName,
            string commandLine,
            ref SecurityAttributes processAttributes,
            ref SecurityAttributes threadAttributes,
            bool inheritHandles,
            ProcessCreationFlags creationFlags,
            IntPtr pEnvironment,
            StringBuilder workingDirectory,
            ref StartupInfoEx startupInfoEx,
            out ProcessInformation processInfo
        );
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SecurityAttributes
    {
        public uint Length;
        public IntPtr pSecurityDescriptor;
        public bool InheritHandle;
    }

    [Flags]
    public enum ProcessCreationFlags : uint
    {
        BreakawayFromJob = 0x01000000,
        DefaultErrorMode = 0x04000000,
        NewConsole = 0x00000010,
        NewProcessGroup = 0x00000200,
        NoWindow = 0x08000000,
        /// <summary>
        /// Available in Windows Vista and upwards only
        /// </summary>
        ProtectedProcess = 0x00040000,
        /// <summary>
        /// Available in Windows XP and upwards only
        /// </summary>
        PreserveCodeAuthZLevel = 0x02000000,
        SeparateWowVDM = 0x00000800,
        SharedWowVDM = 0x00001000,
        Suspended = 0x00000004,
        UnicodeEnvironment = 0x00000400,
        DebugOnlyThisProcess = 0x00000002,
        DebugProcess = 0x00000001,
        DetachedProcess = 0x00000008,
        /// <summary>
        /// Available in Windows Vista and upwards only, must be specified
        /// when using the StartupInfoEx overload
        /// </summary>
        ExtendedStartupInfoPresent = 0x00080000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StartupInfo
    {
        public uint SizeOfStructure;
        public string Reserved;
        public string Title;
        public uint X;
        public uint Y;
        public uint XSize;
        public uint YSize;
        public uint XCountChars;
        public uint YCountChars;
        public uint FillAttribute;
        public StartupInfoFlags SIFlags;
        public Windows.WindowFlags SWFlags;
        public ushort Reserved2;
        public IntPtr Reserved2Bytes;
        public IntPtr hStdIn;
        public IntPtr hStdOut;
        public IntPtr hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StartupInfoEx
    {
        public StartupInfo StartupInfo;
        public IntPtr AttributeList;
    }

    [Flags]
    public enum StartupInfoFlags : uint
    {
        ForceOnFeedback = 0x00000040,
        ForceOffFeedback = 0x00000080,
        RunFullScreen = 0x00000020,
        UseCountChars = 0x00000008,
        UseFillAttribute = 0x00000010,
        UsePosition = 0x00000004,
        UseShowWindow = 0x00000001,
        UseSize = 0x00000002,
        UseStdHandles = 0x00000100,
        /// <summary>
        /// Undocumented. Indicates that the process was launched
        /// from a shortcut. The StartupInfo.Title member can then
        /// be used to retrieve the full path to this shortcut.
        /// </summary>
        TitleShortcut = 0x00000800,
        /// <summary>
        /// Undocumented. Indicates that the hStdOut member of the
        /// StartupInfo structure contains a handle to a monitor obtained
        /// from EnumDisplayMonitors, MonitorFromPoint, MonitorFromWindow
        /// and so on. This flag cannot be specified with UseStdHandles.
        /// This flag will only work for programs that don't create their
        /// windows using explicit X/Y co-ordinates.
        /// </summary>
        OutputHandleIsMonitorHandle = 0x00000400
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessInformation
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public uint ProcessId;
        public uint ThreadId;
    }
}
