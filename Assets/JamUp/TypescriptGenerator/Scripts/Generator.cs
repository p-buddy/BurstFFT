using System;
using System.Linq;
using SkbKontur.TypeScript.ContractGenerator;
using SkbKontur.TypeScript.ContractGenerator.Internals;

namespace JamUp.TypescriptGenerator.Scripts
{
    public static class Generator
    {
        public static string CodeForType<T>()
        {
            var options = TypeScriptGenerationOptions.Default;
            options.NullabilityMode = NullabilityMode.Optimistic;
            var generator = new TypeScriptGenerator(options,
                                                    CustomTypeGenerator.Null,
                                                    new RootTypesProvider(typeof(T)));
            var context = new DefaultCodeGenerationContext();
            return String.Join(Environment.NewLine, generator.Generate().ToList().Select(unit => unit.GenerateCode(context)));
        }

        public static TEnum ConvertEnumValueBack<TEnum>(string jsValue) where TEnum: Enum
        {
            return (TEnum)Enum.Parse(typeof(TEnum), jsValue);
        }
    }
}