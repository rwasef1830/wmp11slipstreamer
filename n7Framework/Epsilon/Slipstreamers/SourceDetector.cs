using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Epsilon.IO;
using Epsilon.Parsers;
using Epsilon.Win32;

namespace Epsilon.Slipstreamers
{
    public static class SourceDetector
    {
        public static WindowsSourceInfo Detect(string winSrcDir)
        {
            return Detect(winSrcDir, new StringBuilder(FileSystem.MaximumPathLength));
        }

        public static WindowsSourceInfo Detect(string winSrcDir, 
            StringBuilder pathBuffer)
        {
            WindowsSourceInfo sourceInfo = new WindowsSourceInfo();

            string archDir;
            string archDir32 = FileSystem.CreatePathString(pathBuffer, winSrcDir, 
                "i386");
            string archDir64 = FileSystem.CreatePathString(pathBuffer, winSrcDir, 
                "amd64");
            
            if (File.Exists(FileSystem.CreatePathString(pathBuffer, archDir32, 
                "LAYOUT.INF")))
            {
                PeEditor editor = new PeEditor(FileSystem.CreatePathString(
                    pathBuffer, archDir32, "SYSTEM32", "NTDLL.DLL"));
                if (editor.TargetMachineType != Architecture.x86)
                {
                    return sourceInfo;
                }
                sourceInfo.Arch = TargetArchitecture.x86;
                sourceInfo.BuildNumber = editor.GetVersion().MinorRevision;
                archDir = archDir32;
            }
            else if (File.Exists(FileSystem.CreatePathString(pathBuffer, archDir64, 
                "LAYOUT.INF")))
            {
                PeEditor editor = new PeEditor(FileSystem.CreatePathString(
                    pathBuffer, archDir64, "SYSTEM32", "NTDLL.DLL"));
                if (editor.TargetMachineType != Architecture.x64)
                {
                    return sourceInfo;
                }
                sourceInfo.Arch = TargetArchitecture.x64;
                sourceInfo.BuildNumber = editor.GetVersion().MinorRevision;
                archDir = archDir64;
            }
            else
            {
                return sourceInfo;
            }

            IniParser layoutInf = new IniParser(
                FileSystem.CreatePathString(pathBuffer, archDir, "LAYOUT.INF"), 
                true
                );

            #region Detect product type
            string prodVersion = layoutInf.ReadValue("Strings", "productname");
            string eulaText = File.ReadAllText(FileSystem.CreatePathString(pathBuffer, 
                archDir, "EULA.TXT"));
            int indexOfEulaId = eulaText.IndexOf("EULAID:");
            if (prodVersion.Contains("XP"))
            {
                sourceInfo.SourceVersion = WindowsType._XP;

                if (prodVersion.Contains("Profess"))
                {
                    if (eulaText.Contains("Media Center") &&
                        (eulaText.Contains("Media Player")
                        || eulaText.Contains("Lecteur Windows Media"))
                        && (indexOfEulaId > 0
                        && eulaText.IndexOf("MCE", indexOfEulaId) > 0))
                    {
                        sourceInfo.Edition = WindowsEdition.MediaCenter;
                    }
                    else
                    {
                        sourceInfo.Edition = WindowsEdition.Professional;
                    }
                }
                else if (prodVersion.Contains("Home") ||
                    prodVersion.Contains("familiale"))
                {
                    if (eulaText.Contains("Media Center") &&
                        (eulaText.Contains("Media Player")
                        || eulaText.Contains("Lecteur Windows Media"))
                        && (indexOfEulaId > 0
                        && eulaText.IndexOf("MCE", indexOfEulaId) > 0))
                    {
                        sourceInfo.Edition = WindowsEdition.MediaCenter;
                    }
                    else
                    {
                        sourceInfo.Edition = WindowsEdition.Home;
                    }
                }
            }
            else if (prodVersion.Contains("2003"))
                sourceInfo.SourceVersion = WindowsType._Server2003;
            else if (prodVersion.Contains("2000"))
                sourceInfo.SourceVersion = WindowsType._2000;
            else
                sourceInfo.SourceVersion = WindowsType._Unknown;
            #endregion

            #region Reduced Media Edition Detection
            if (indexOfEulaId > 0 && eulaText.IndexOf("RME", indexOfEulaId) > 0
                && sourceInfo.SourceVersion == WindowsType._XP)
            {
                sourceInfo.ReducedMediaEdition = true;
            }
            #endregion

            #region Detect service pack level
            string splevelstr = null;
            if (layoutInf.KeyExists("Strings", "spcdname"))
                splevelstr = layoutInf.ReadValue("Strings", "spcdname");
            else if (layoutInf.KeyExists("Strings", "spcd"))
                splevelstr = layoutInf.ReadValue("Strings", "spcd");

            if (splevelstr != null)
            {
                string strMatched 
                    = Regex.Match(splevelstr, @"Service\x20Pack\x20(\d+)")
                    .Groups[1].Value;
                if (strMatched.Length >= 1)
                    sourceInfo.ServicePack = int.Parse(strMatched);
            }
            #endregion

            return sourceInfo;
        }
    }
}
