using System;
using System.Collections.Generic;
using System.Text;
using Epsilon.Parsers;
using Epsilon.WindowsModTools;
using Epsilon.IO;
using System.IO;

namespace WMP11Slipstreamer
{
    partial class Backend
    {
        /// <summary>
        /// Must be called before determining overwriting files,
        /// Applies a fix to wbemoc.inf to fix missing file error
        /// during Windows setup.
        /// </summary>
        void FixWBEM()
        {
            const string wbemInf = "wbemoc.inf";
            string[] sectionsToFix = new string[] { "WBEM.CopyMOFs" };

            if (this.CopyOrExpandFromArch(wbemInf, this._extractedDirectory, true))
            {
                IniParser wbemOcEditor = new IniParser(
                    this.CombinePathComponents(this._extractedDirectory, wbemInf), 
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

        /// <summary>
        /// Applies various IniParser overrides to fix txtsetup.sif output
        /// to accomodate for the MS' broken INF parser
        /// </summary>
        /// <param name="parser">The editor for "txtsetup.sif"</param>
        static void ApplyTxtsetupEditorHacks(IniParser parser)
        {
            const int csvSDN = 4;
            IniParser.SectionOverrides overrides = new IniParser.SectionOverrides(csvSDN);
            parser.SetSectionOutputOverrides("SourceDisksNames", overrides);
            parser.SetSectionOutputOverrides("SourceDisksNames.x86", overrides);
            parser.SetSectionOutputOverrides("SourceDisksNames.amd64", overrides);
            parser.SetSectionOutputOverrides("SourceDisksNames.ia64", overrides);

            overrides = new IniParser.SectionOverrides(0, IniParser.QuotePolicy.On);
            parser.SetSectionOutputOverrides("Keyboard Layout", overrides);

            parser.EnableLastValueQuotes = true;
        }
    }
}
