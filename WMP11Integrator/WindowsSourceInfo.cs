using System;
using System.Collections.Generic;
using System.Text;

namespace Epsilon.WindowsModTools
{
    /// <summary>
    /// Structure holding information about Windows source
    /// </summary>
    public class WindowsSourceInfo
    {
        public WindowsType SourceVersion;
        public WindowsEdition Edition;
        public int ServicePack;
        public TargetArchitecture Arch;
        public bool ReducedMediaEdition;

        public WindowsSourceInfo()
        {
            this.SourceVersion = WindowsType._Unknown;
            this.Edition = WindowsEdition.Unknown;
            this.ServicePack = 0;
            this.Arch = TargetArchitecture.Unknown;
            this.ReducedMediaEdition = false;
        }
    }

    /// <summary>
    /// Enumeration describing Windows installation source type
    /// </summary>
    public enum WindowsType
    {
        _Unknown,
        _XP,
        _Server2003,
        _2000,
    }

    public enum WindowsEdition
    {
        Unknown,
        Home,
        Professional,
        MediaCenter,
        Workstation,
        Standard,
        Datacenter,
        HomeServer
    }

    public enum TargetArchitecture
    {
        Unknown,
        x86,
        x64
    }
}
