using System;
using System.Collections.Generic;
using Epsilon.Parsers;

namespace Epsilon.WMP11Slipstreamer
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
            var sectionsToFix = new[] { "WBEM.CopyMOFs" };

            if (this.CopyOrExpandFromArch(wbemInf, this._extractDir, true))
            {
                var wbemOcEditor = new IniParser(
                    this.CreatePathString(this._extractDir, wbemInf),
                    true);
                var fixDict
                    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "napclientprov.mof", "napprov.mof" },
                        { "napclientschema.mof", "napschem.mof" }
                    };

                foreach (string section in sectionsToFix)
                {
                    foreach (var fix in fixDict)
                    {
                        if (wbemOcEditor.LineExists(section, fix.Key))
                        {
                            wbemOcEditor.RemoveLine(section, fix.Key);
                            wbemOcEditor.Add(
                                section,
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