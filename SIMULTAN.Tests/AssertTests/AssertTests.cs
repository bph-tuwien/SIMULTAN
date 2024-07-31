using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.AssertTests
{
    /// <summary>
    /// Tests for custom asserts
    /// </summary>
    [TestClass]
    public class AssertTests
    {
        [TestMethod]
        public void TestAssertMultilineEquals()
        {
            string a = "TEST\nSecond Line\nThird Line\n";
            string b = "TEST\nSecond Line\nThird Line\n";

            AssertUtil.AreEqualMultiline(a, b);
        }

        [TestMethod]
        public void TestAssertMultilineNotEqual()
        {
            string a = "TEST\nSecond Line is also very lona just to see if it works correctly\nThird Line\n";
            string b = "TEST\nSecond Line is also very long just to see if it works correctly\nThird Line\n";

            try
            {
                AssertUtil.AreEqualMultiline(a, b);
                Assert.Fail("Did not throw and AssertFailedException");
            }
            catch (AssertFailedException e)
            {
                Debug.WriteLine(e.Message);
                Assert.IsTrue(e.Message.Contains("line 2"));
            }
        }

        [TestMethod]
        public void TestAssertMultilineDifferentLength()
        {
            string a = "TEST\nSecond Line\n";
            string b = "TEST\nSecond Line\nThird Line\n";

            try
            {
                AssertUtil.AreEqualMultiline(a, b);
                Assert.Fail("Did not throw and AssertFailedException");
            }
            catch (AssertFailedException e)
            {
                Debug.WriteLine(e.Message);
                Assert.IsTrue(e.Message.Contains("Actual string is too long at line 3"));
            }

            a = "TEST\nSecond Line\nThird Line\n";
            b = "TEST\nSecond Line\n";

            try
            {
                AssertUtil.AreEqualMultiline(a, b);
                Assert.Fail("Did not throw and AssertFailedException");
            }
            catch (AssertFailedException e)
            {
                Debug.WriteLine(e.Message);
                Assert.IsTrue(e.Message.Contains("Actual string is too short at line 3"));
            }
        }
    }
}
