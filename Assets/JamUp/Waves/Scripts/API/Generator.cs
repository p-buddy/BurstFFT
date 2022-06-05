namespace JamUp.Waves
{
    public static class Generator
    {
        public static string APIDeclaration()
        {
            return TypescriptGenerator.Scripts.Generator.CodeForType<KeyFrame>();
        }
    }
}