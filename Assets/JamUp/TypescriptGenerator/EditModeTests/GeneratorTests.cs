using System;
using System.Linq;
using JamUp.TypescriptGenerator.Scripts;
using NUnit.Framework;
using UnityEngine;

namespace JamUp.TypescriptGenerator.EditModeTests
{
    public class GeneratorTests
    {
        private struct SimpleVector
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }

        [Test]
        public void SimpleType()
        {
            Debug.Log(Generator.CodeForType<SimpleVector>());
        }

        private enum Dummy
        {
            A,
            B,
            C,
            LongName
        }
        
        [Test]
        public void EnumType()
        {
            Debug.Log(Generator.CodeForType<Dummy>());
            var names = Enum.GetNames(typeof(Dummy));
            var values = Enum.GetValues(typeof(Dummy)) as Dummy[];
            
            Assert.NotNull(values);
            names.ToList().Select((name, index) => (name: name, index: index)).ToList().ForEach((tuple =>
            {
                Assert.AreEqual(Generator.ConvertEnumValueBack<Dummy>(tuple.name), values[tuple.index]);
            }));
        }
        
        private struct Nested
        {
            public SimpleVector Start { get; set; }
            public SimpleVector End { get; set; }
            public SimpleVector[] Positions { get; set; }
            public Dummy Type { get; set; }
        }
        
        [Test]
        public void NestedType()
        {
            Debug.Log(Generator.CodeForType<Nested>());
        }
    }
}