using System;
using System.Linq;
using JamUp.TypescriptGenerator.Scripts;
using NUnit.Framework;

namespace JamUp.TypescriptGenerator.EditModeTests
{
    public class ConverterTests
    {
        private enum Dummy
        {
            A,
            B,
            C,
            LongName
        }
        
        [Test]
        public void EnumTest()
        {
            var names = Enum.GetNames(typeof(Dummy));
            var values = Enum.GetValues(typeof(Dummy)) as Dummy[];
            
            Assert.NotNull(values);
            names.ToList().Select((name, index) => (name: name, index: index)).ToList().ForEach((tuple =>
            {
                Assert.AreEqual(Converter.ConvertEnumValueBack(tuple.name, typeof(Dummy)), values[tuple.index]);
            }));
        }
    }
}