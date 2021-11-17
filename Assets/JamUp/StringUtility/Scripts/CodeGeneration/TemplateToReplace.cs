namespace JamUp.StringUtility
{
    public readonly struct TemplateToReplace
    {
        public string TemplateString { get; }
        public string ReplacementString { get; }

        public TemplateToReplace(string templateString, string replacementString)
        {
            TemplateString = templateString;
            ReplacementString = replacementString;
        }
    }
}