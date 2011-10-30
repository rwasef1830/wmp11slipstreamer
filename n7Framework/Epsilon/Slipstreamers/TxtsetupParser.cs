using Epsilon.Parsers;

namespace Epsilon.WindowsModTools
{
    /// <summary>
    /// Specialized IniParser to deal with the quirks of "Txtsetup.sif"
    /// </summary>
    public class TxtsetupParser : IniParser
    {
        public TxtsetupParser(string txtsetupSifPath)
            : base(txtsetupSifPath, true)
        {
            IniParser.SectionOverrides sdnOverrides 
                = new IniParser.SectionOverrides(4);
            base.SetSectionOutputOverrides("SourceDisksNames", sdnOverrides);
            base.SetSectionOutputOverrides("SourceDisksNames.x86", sdnOverrides);
            base.SetSectionOutputOverrides("SourceDisksNames.amd64", sdnOverrides);
            base.SetSectionOutputOverrides("SourceDisksNames.ia64", sdnOverrides);

            IniParser.SectionOverrides keyboardOverrides
                = new IniParser.SectionOverrides(0, IniParser.QuotePolicy.On);
            base.SetSectionOutputOverrides("Keyboard Layout", keyboardOverrides);

            // Fix for bug on Hungarian XP: 
            // http://boooggy.org/forum/viewtopic.php?p=67#p67
            IniParser.SectionOverrides mouseOverrides 
                = new IniParser.SectionOverrides(3);
            this.SetSectionOutputOverrides("Mouse", mouseOverrides);
            this.EnableLastValueQuotes = true;
        }
    }
}
