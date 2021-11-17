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
    }
}