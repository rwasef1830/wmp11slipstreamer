using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using Epsilon.Win32;
using Epsilon.Win32.API;
using Epsilon.Win32.CustomMarshalers;

namespace Epsilon.Win32.API
{
    public static class SCM
    {
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeServiceHandle OpenSCManager(
            string lpMachineName, 
            string lpScDb, 
            SCMDesiredAccess scParameter);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeServiceHandle CreateService(
            SafeServiceHandle hSCManager,
            string lpServiceName,
            string lpDisplayName,
            ServiceDesiredAccess dwDesiredAccess,
            ServiceType dwServiceType,
            ServiceStartType dwStartType,
            ServiceErrorSeverity dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            out uint lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword
            );

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeServiceHandle CreateService(
            SafeServiceHandle hSCManager,
            string lpServiceName,
            string lpDisplayName,
            ServiceDesiredAccess dwDesiredAccess,
            ServiceType dwServiceType,
            ServiceStartType dwStartType,
            ServiceErrorSeverity dwErrorControl,
            string lpBinaryPathName,
            string lpLoadOrderGroup,
            IntPtr lpdwTagId,
            string lpDependencies,
            string lpServiceStartName,
            string lpPassword
            );

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeServiceHandle OpenService(
            SafeServiceHandle hSCManager,
            string lpServiceName,
            ServiceDesiredAccess dwDesiredAccess
            );

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DeleteService(
            SafeServiceHandle hService);

        [Flags]
        public enum SCMDesiredAccess : uint
        {
            AllAccess = 0xF003F,
            CreateService = 0x0002,
            Connect = 0x0001,
            EnumerateService = 0x0004,
            Lock = 0x0008,
            ModifyBootConfig = 0x0020,
            QueryLockStatus = 0x0010,

            GenericRead = AccessMask.StandardRightsRead 
                | EnumerateService | QueryLockStatus,
            GenericWrite = AccessMask.StandardRightsWrite
                | CreateService | ModifyBootConfig,
            GenericExecute = AccessMask.StandardRightsExecute
                | Connect | Lock,
            GenericAll = AllAccess
        }

        [Flags]
        public enum ServiceDesiredAccess : uint
        {
            AllAccess = 0xF01FF,
            ChangeConfig = 0x0002,
            EnumerateDependents = 0x0008,
            Interrogate = 0x0080,
            PauseContinue = 0x0040,
            QueryConfig = 0x0001,
            QueryStatus = 0x0004,
            Start = 0x0010,
            Stop = 0x0020,
            UserDefinedControl = 0x0100,

            AccessSystemSecurity = AccessMask.AccessSystemSecurity,
            Delete = AccessMask.Delete,
            ReadControl = AccessMask.ReadControl,
            WriteDac = AccessMask.WriteDac,
            WriteOwner = AccessMask.WriteOwner,

            GenericRead = AccessMask.StandardRightsRead | QueryConfig 
                | QueryStatus | Interrogate | EnumerateDependents,
            GenericWrite = AccessMask.StandardRightsWrite | ChangeConfig,
            GenericExecute = AccessMask.StandardRightsExecute | Start 
                | Stop | PauseContinue | UserDefinedControl
        }

        public enum ServiceType : uint
        {
            Adapter = 0x00000004,
            FileSystemDriver = 0x00000002,
            KernelDriver = 0x00000001,
            RecognizerDriver = 0x00000008,
            Win32OwnProcess = 0x00000010,
            Win32ShareProcess = 0x00000020,
            /// <summary>
            /// Can only be used with OwnProcess or ShareProcess and when
            /// running as NT AUTHORITY\SYSTEM
            /// </summary>
            InteractiveProcess = 0x00000100
        }

        public enum ServiceStartType : uint
        {
            AutoStart = 0x00000002,
            BootStart = 0x00000000,
            DemandStart = 0x00000003,
            Disabled = 0x00000004,
            SystemStart = 0x00000001
        }

        public enum ServiceErrorSeverity : uint
        {
            /// <summary>
            /// The startup program logs the error in the event log, if possible. 
            /// If the last-known-good configuration is being started, the startup operation 
            /// fails. Otherwise, the system is restarted with the last-known good configuration.
            /// </summary>
            Critical = 0x00000003,
            /// <summary>
            /// The startup program ignores the error and continues the startup operation.
            /// </summary>
            Ignore = 0x00000000,
            /// <summary>
            /// The startup program logs the error in the event log but continues 
            /// the startup operation.
            /// </summary>
            Normal = 0x00000001,
            /// <summary>
            /// The startup program logs the error in the event log. If the last-known-good configuration 
            /// is being started, the startup operation continues. Otherwise, the system is restarted 
            /// with the last-known-good configuration.
            /// </summary>
            Severe = 0x00000002
        }
    }
}
