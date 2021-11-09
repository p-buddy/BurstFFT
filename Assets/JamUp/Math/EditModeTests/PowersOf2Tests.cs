using System.Collections.Generic;
using NUnit.Framework;

namespace JamUp.Math.EditModeTests
{
    public class PowersOf2Tests
    {
        public struct TestCase<TGiven, TExpected>
        {
            public TGiven Given;
            public TExpected Expected;
            public override string ToString()
            {
                return $"Given {Given}, Expect {Expected}";
            }

            public void ThenExpectedMatchesActual(TExpected actual)
            {
                Assert.AreEqual(Expected, actual);
            }
        }

        private static IEnumerable<TestCase<int, int>> NextTestCases = new List<TestCase<int, int>>
        {
            new TestCase<int, int> {Given = 2, Expected = 2},
            new TestCase<int, int> {Given = 3, Expected = 4},
            new TestCase<int, int> {Given = 44100, Expected = 65536},
            new TestCase<int, int> {Given = 0, Expected = 1},
        };
        
        [Test]
        [TestCaseSource(nameof(NextTestCases))]
        public void CheckNext(TestCase<int, int> testCase)
        {
            int actual = PowersOf2.Next(testCase.Given);
            testCase.ThenExpectedMatchesActual(actual);
        }

        private static IEnumerable<TestCase<int, bool>> IsPowerTestCases = new List<TestCase<int, bool>>
        {
            new TestCase<int, bool> {Given = 2, Expected = true},
            new TestCase<int, bool> {Given = 1024, Expected = true},
            new TestCase<int, bool> {Given = 44100, Expected = false},
            new TestCase<int, bool> {Given = 3, Expected = false},
            new TestCase<int, bool> {Given = 0, Expected = false},
        };
        
        [Test]
        [TestCaseSource(nameof(IsPowerTestCases))]
        public void CheckIsPower(TestCase<int, bool> testCase)
        {
            bool actual = PowersOf2.IsPowerOf2(testCase.Given);
            testCase.ThenExpectedMatchesActual(actual);
        }
    }
}