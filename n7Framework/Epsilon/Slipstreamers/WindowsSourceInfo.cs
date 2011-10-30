using System;
using System.Collections.Generic;
using System.Text;

namespace Epsilon.Slipstreamers
{
    /// <summary>
    /// Structure holding information about Windows source
    /// </summary>
    public struct WindowsSourceInfo
    {
        public WindowsType SourceVersion;
        public WindowsEdition Edition;
        public int ServicePack;
        public TargetArchitecture Arch;
        public bool ReducedMediaEdition;
        public int BuildNumber;

        /// <summary>
        /// Produces a human readable string from this instance
        /// </summary>
        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(200);

            // Start building a version string
            builder.Append("Windows™");

            switch (this.SourceVersion)
            {
                case WindowsType._XP:
                    builder.Append(" XP");
                    break;

                case WindowsType._Server2003:
                    builder.Append(" Server 2003");
                    break;

                case WindowsType._2000:
                    builder.Append(" 2000");
                    break;

                case WindowsType._Unknown:
                    builder.Append(" <Unknown>");
                    break;
            }

            switch (this.Edition)
            {
                case WindowsEdition.Home:
                    builder.Append(" Home Edition");
                    break;

                case WindowsEdition.Professional:
                    builder.Append(" Professional");
                    break;

                case WindowsEdition.MediaCenter:
                    builder.Append(" Media Center Edition");
                    break;

                default:
                    break;
            }

            if (this.Arch == TargetArchitecture.x64) builder.Append(" x64");
            else if (this.Arch == TargetArchitecture.IA64) builder.Append(" 64-bit Edition");

            if (this.ReducedMediaEdition)
            {
                builder.Append(" N");
            }

            if (this.ServicePack > 0)
            {
                builder.Append(" SP");
                builder.Append(this.ServicePack);
            }
            else
            {
                builder.Append(" RTM");
            }

            return builder.ToString();
        }

        public Version ToVersion()
        {
            int major = 0;
            int minor = 0;
            int build = 0;
            int revision = this.BuildNumber;

            switch (this.SourceVersion)
            {
                case WindowsType._XP:
                    if (this.Arch == TargetArchitecture.x86)
                    {
                        major = 5; minor = 1; build = 2600;
                    }
                    else
                    {
                        goto case WindowsType._Server2003;
                    }
                    break;

                case WindowsType._Server2003:
                    major = 5; minor = 2; build = 3790;
                    break;

                case WindowsType._2000:
                    major = 5; minor = 0; build = 2190; 
                    break;
            }

            return new Version(major, minor, build, revision);
        }
    }

    /// <summary>
    /// Enumeration describing Windows installation source type
    /// </summary>
    public enum WindowsType : byte
    {
        _Unknown = 0,
        _XP,
        _Server2003,
        _2000,
    }

    public enum WindowsEdition : byte
    {
        Unknown = 0,
        Home,
        Professional,
        MediaCenter,
        Workstation,
        Standard,
        Datacenter,
        HomeServer
    }

    public enum TargetArchitecture : byte
    {
        Unknown = 0,
        x86,
        x64,
        IA64
    }
}
