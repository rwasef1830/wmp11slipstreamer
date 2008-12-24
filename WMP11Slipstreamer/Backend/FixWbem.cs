using System;
using System.Collections.Generic;
using System.Text;
using Epsilon.Parsers;
using Epsilon.WindowsModTools;
using Epsilon.IO;
using System.IO;

namespace Epsilon.Slipstreamers.WMP11Slipstreamer
{
    partial class Backend
    {
        /// <summary>
        /// Must be called before determining overwriting files,
        /// Applies a fix to wbemoc.inf to fix missing file error
        /// during Windows setup.
        /// </summary>
        void FixWbem()
        {
            const string wbemInf = "wbemoc.inf";
            string[] sectionsToFix = new string[] { "WBEM.CopyMOFs" };

            if (this.CopyOrExpandFromArch(wbemInf, this._extractDir, true))
            {
                IniParser wbemOcEditor = new IniParser(
                    this.CreatePathString(this._extractDir, wbemInf), 
                    true);
                Dictionary<string, string> fixDict
                    = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
                fixDict.Add("napclientprov.mof", "napprov.mof");
                fixDict.Add("napclientschema.mof", "napschem.mof");

                foreach (string section in sectionsToFix)
                {
                    foreach (KeyValuePair<string, string> fix in fixDict)
                    {
                        if (wbemOcEditor.LineExists(section, fix.Key))
                        {
                            wbemOcEditor.RemoveLine(section, fix.Key);
                            wbemOcEditor.Add(section,
                                String.Format("{0},{1}", fix.Key, fix.Value),
                                false);
                        }
                    }
                }
                wbemOcEditor.SaveIni();
            }
        }
    }
}
