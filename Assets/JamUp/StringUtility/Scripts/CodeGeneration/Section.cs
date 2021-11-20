using System;

namespace JamUp.StringUtility
{
    public readonly struct Section
    {
        public string SectionOpen { get; }
        public string SectionClose { get; }

        public Section(string sectionOpen, string sectionClose)
        {
            SectionOpen = sectionOpen;
            SectionClose = sectionClose;
        }

        public override string ToString()
        {
            return $"{SectionOpen}{Environment.NewLine}{SectionClose}";
        }
    }
}