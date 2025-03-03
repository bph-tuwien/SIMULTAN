using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace SIMULTAN.Tests.TestUtils
{
    public static class AssertUtil
    {
        #region Comparer

        public static bool Compare(double expected, double actual)
        {
            return Compare(expected, actual, 0.00001);
        }

        public static bool Compare(double expected, double actual, double tolerance)
        {
            if (double.IsPositiveInfinity(expected) && double.IsPositiveInfinity(actual))
                return true;
            if (double.IsNegativeInfinity(expected) && double.IsNegativeInfinity(actual))
                return true;
            if (double.IsNaN(expected) && double.IsNaN(actual))
                return true;

            return System.Math.Abs(expected - actual) <= tolerance;
        }

        public static bool Compare(SimPoint3D expected, SimPoint3D actual)
        {
            return Compare(expected.X, actual.X) && Compare(expected.Y, actual.Y) && Compare(expected.Z, actual.Z);
        }

        #endregion

        public static void ContainEqualValues<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);

            var l1 = first.ToList();
            var l2 = second.ToList();

            Assert.AreEqual(l1.Count, l2.Count);

            for (int i = 0; i < l1.Count; ++i)
            {
                Assert.AreEqual(l1[i], l2[i]);
            }
        }

        public static void ContainEqualValues<T>(IEnumerable<T> first, IEnumerable<T> second, Func<T, object> selector)
        {
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);

            var l1 = first.ToList();
            var l2 = second.ToList();

            Assert.AreEqual(l1.Count, l2.Count);

            for (int i = 0; i < l1.Count; ++i)
            {
                Assert.AreEqual(selector(l1[i]), selector(l2[i]));
            }
        }

        public static void ContainEqualValues(IEnumerable<double> expected, IEnumerable<double> actual)
        {
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);

            var l1 = expected.ToList();
            var l2 = actual.ToList();

            Assert.AreEqual(l1.Count, l2.Count);

            for (int i = 0; i < l1.Count; ++i)
            {
                AssertUtil.AssertDoubleEqual(l1[i], l2[i]);
            }
        }

        public static void ContainEqualValues(List<List<double>> expected, List<List<double>> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count,
                string.Format("Dimensions are not matching: Expected {0}, Actual {1}", expected.Count, actual.Count));

            for (int i = 0; i < expected.Count; ++i)
            {
                Assert.AreEqual(expected[i].Count, actual[i].Count,
                    string.Format("Inner dimensions are not matching in row {0}: Expected: {1}, Actual {2}", i, expected[i].Count, actual[i].Count));

                for (int j = 0; j < expected[i].Count; ++j)
                {
                    AssertDoubleEqual(expected[i][j], actual[i][j]);
                }
            }
        }

        public static void ContainEqualValues(List<List<double>> expected, List<double> actualRowMajor)
        {
            Assert.AreEqual(expected.Sum(x => x.Count), actualRowMajor.Count,
                string.Format("Number of elements not matching: Expected {0}, Actual {1}", expected.Sum(x => x.Count), actualRowMajor.Count));

            int linearIndex = 0;

            for (int i = 0; i < expected.Count; ++i)
            {
                for (int j = 0; j < expected.Count; ++j)
                {
                    AssertDoubleEqual(expected[i][j], actualRowMajor[linearIndex]);
                    linearIndex++;
                }
            }
        }

        public static void ContainEqualValues(List<List<double>> expected, double[,] actual)
        {
            Assert.AreEqual(expected.Count, actual.GetLength(0),
                string.Format("Dimensions are not matching: Expected {0}, Actual {1}", expected.Count, actual.GetLength(0)));

            for (int i = 0; i < expected.Count; ++i)
            {
                Assert.AreEqual(expected[i].Count, actual.GetLength(1),
                    string.Format("Inner dimensions are not matching in row {0}: Expected: {1}, Actual {2}", i, expected[i].Count, actual.GetLength(1)));

                for (int j = 0; j < expected[i].Count; ++j)
                {
                    AssertDoubleEqual(expected[i][j], actual[i, j]);
                }
            }
        }

        public static void ContainEqualValues(double[,] expected, List<List<double>> actual)
        {
            Assert.AreEqual(expected.GetLength(0), actual.Count,
                string.Format("Dimensions are not matching: Expected {0}, Actual {1}", expected.GetLength(0), actual.Count));

            for (int i = 0; i < expected.GetLength(0); ++i)
            {
                Assert.AreEqual(expected.GetLength(1), actual[i].Count,
                    string.Format("Inner dimensions are not matching in row {0}: Expected: {1}, Actual {2}", i, expected.GetLength(1), actual[i].Count));

                for (int j = 0; j < expected.GetLength(1); ++j)
                {
                    AssertDoubleEqual(expected[i, j], actual[i][j]);
                }
            }
        }

        public static void ContainEqualValues(object[,] expected, SimMultiValueBigTable actual)
        {
            Assert.AreEqual(expected.GetLength(0), actual.Count(0),
                string.Format("Dimension 0 is not matching: Expected {0}, Actual {1}", expected.GetLength(0), actual.Count(0)));
            Assert.AreEqual(expected.GetLength(1), actual.Count(1),
                string.Format("Dimensions 1 is not matching: Expected {0}, Actual {1}", expected.GetLength(1), actual.Count(1)));

            for (int i = 0; i < expected.GetLength(0); ++i)
            {
                for (int j = 0; j < expected.GetLength(1); ++j)
                {
                    if (expected[i, j] == null && actual[i, j] == null) { }
                    else if (expected[i, j] == null)
                    {
                        Assert.Fail("Values aren't matching at [{1}, {2}]. Expect is null, actual = {0}", actual[i, j], i, j);
                    }
                    else if (actual[i, j] == null)
                    {
                        Assert.Fail("Values aren't matching at [{1}, {2}]. Expect is null, actual = {0}", actual[i, j], i, j);
                    }
                    else if (actual[i, j].GetType() != expected[i, j].GetType())
                    {
                        Assert.Fail("Values aren't matching at [{0}, {1}]. Expect is {2}, actual is {3}", i, j,
                            expected[i, j].GetType(), actual[i, j].GetType());
                    }
                    else //Same type
                    {
                        if (expected[i, j] is double d1 && actual[i, j] is double d2)
                            AssertDoubleEqual(d1, d2);
                        else if (expected[i, j] is int i1 && actual[i, j] is int i2)
                            Assert.AreEqual(i1, i2);
                        else if (expected[i, j] is uint ui1 && actual[i, j] is uint ui2)
                            Assert.AreEqual(ui1, ui2);
                        else if (expected[i, j] is bool b1 && actual[i, j] is bool b2)
                            Assert.AreEqual(b1, b2);
                        else if (expected[i, j] is string s1 && actual[i, j] is string s2)
                            Assert.AreEqual(s1, s2);
                        else if (expected[i, j] is long l1 && actual[i, j] is long l2)
                            Assert.AreEqual(l1, l2);
                        else if (expected[i, j] is ulong ul1 && actual[i, j] is ulong ul2)
                            Assert.AreEqual(ul1, ul2);
                        else
                            Assert.Fail("Unknown error");
                    }
                }
            }
        }

        public static void ContainEqualValues(List<List<object>> expected, SimMultiValueBigTable actual)
        {
            Assert.AreEqual(expected.Count, actual.Count(0),
                string.Format("Dimension 0 is not matching: Expected {0}, Actual {1}", expected.Count, actual.Count(0)));

            if (expected.Count > 0) //Otherwise empty table
            {
                Assert.AreEqual(expected[0].Count, actual.Count(1),
                    string.Format("Dimensions 1 is not matching: Expected {0}, Actual {1}", expected[0].Count, actual.Count(1)));

                for (int i = 0; i < expected.Count; ++i)
                {
                    for (int j = 0; j < expected[i].Count; ++j)
                    {
                        if (expected[i][j] == null && actual[i, j] == null) { }
                        else if (expected[i][j] == null)
                        {
                            Assert.Fail("Values aren't matching at [{1}, {2}]. Expect is null, actual = {0}", actual[i, j], i, j);
                        }
                        else if (actual[i, j] == null)
                        {
                            Assert.Fail("Values aren't matching at [{1}, {2}]. Expect is null, actual = {0}", actual[i, j], i, j);
                        }
                        else if (actual[i, j].GetType() != expected[i][j].GetType())
                        {
                            Assert.Fail("Values aren't matching at [{0}, {1}]. Expect is {2}, actual is {3}", i, j,
                                expected[i][j].GetType(), actual[i, j].GetType());
                        }
                        else //Same type
                        {
                            if (expected[i][j] is double d1 && actual[i, j] is double d2)
                                AssertDoubleEqual(d1, d2);
                            else if (expected[i][j] is int i1 && actual[i, j] is int i2)
                                Assert.AreEqual(i1, i2);
                            else if (expected[i][j] is uint ui1 && actual[i, j] is uint ui2)
                                Assert.AreEqual(ui1, ui2);
                            else if (expected[i][j] is bool b1 && actual[i, j] is bool b2)
                                Assert.AreEqual(b1, b2);
                            else if (expected[i][j] is string s1 && actual[i, j] is string s2)
                                Assert.AreEqual(s1, s2);
                            else if (expected[i][j] is long l1 && actual[i, j] is long l2)
                                Assert.AreEqual(l1, l2);
                            else if (expected[i][j] is ulong ul1 && actual[i, j] is ulong ul2)
                                Assert.AreEqual(ul1, ul2);
                            else
                                Assert.Fail("Unknown error");
                        }
                    }
                }
            }
        }

        public static void ContainEqualValues(SimMultiValueBigTable expected, SimMultiValueBigTable actual)
        {
            Assert.AreEqual(expected.Count(0), actual.Count(0),
                string.Format("Dimension 0 is not matching: Expected {0}, Actual {1}", expected.Count(0), actual.Count(0)));
            Assert.AreEqual(expected.Count(1), actual.Count(1),
                string.Format("Dimension 1 is not matching: Expected {0}, Actual {1}", expected.Count(1), actual.Count(1)));

            for (int i = 0; i < expected.Count(0); ++i)
            {
                for (int j = 0; j < expected.Count(1); ++j)
                {
                    if (expected[i, j] == null && actual[i, j] == null) { }
                    else if (expected[i, j] == null)
                    {
                        Assert.Fail("Values aren't matching at [{1}, {2}]. Expect is null, actual = {0}", actual[i, j], i, j);
                    }
                    else if (actual[i, j] == null)
                    {
                        Assert.Fail("Values aren't matching at [{1}, {2}]. Expect is null, actual = {0}", actual[i, j], i, j);
                    }
                    else if (actual[i, j].GetType() != expected[i, j].GetType())
                    {
                        Assert.Fail("Values aren't matching at [{0}, {1}]. Expect is {2}, actual is {3}", i, j,
                            expected[i, j].GetType(), actual[i, j].GetType());
                    }
                    else //Same type
                    {
                        if (expected[i, j] is double d1 && actual[i, j] is double d2)
                            AssertDoubleEqual(d1, d2);
                        else if (expected[i, j] is int i1 && actual[i, j] is int i2)
                            Assert.AreEqual(i1, i2);
                        else if (expected[i, j] is bool b1 && actual[i, j] is bool b2)
                            Assert.AreEqual(b1, b2);
                        else if (expected[i, j] is string s1 && actual[i, j] is string s2)
                            Assert.AreEqual(s1, s2);
                        else
                            Assert.Fail("Unknown error");
                    }
                }
            }
        }

        public static void ContainEqualValues(double[,] expected, double[,] actual)
        {
            Assert.AreEqual(expected.GetLength(0), actual.GetLength(0),
                string.Format("Dimension 0 not matching: Expected {0}, Actual {1}", expected.GetLength(0), actual.GetLength(0)));
            Assert.AreEqual(expected.GetLength(1), actual.GetLength(1),
                string.Format("Dimension 1 not matching: Expected {0}, Actual {1}", expected.GetLength(1), actual.GetLength(1)));

            for (int i = 0; i < expected.GetLength(0); ++i)
            {
                for (int j = 0; j < expected.GetLength(1); ++j)
                {
                    AssertDoubleEqual(expected[i, j], actual[i, j]);
                }
            }
        }

        public static void AssertDoubleEqual(double expected, double actual, double tolerance = 0.00001)
        {
            Assert.IsTrue(Compare(expected, actual, tolerance), string.Format("Expected {0} Actual: {1}", expected, actual));
        }

        public static void AreEqual(SimPoint3D expected, SimPoint3D actual, double tolerance = 0.00001)
        {
            AssertDoubleEqual(expected.X, actual.X, tolerance);
            AssertDoubleEqual(expected.Y, actual.Y, tolerance);
            AssertDoubleEqual(expected.Z, actual.Z, tolerance);
        }

        public static void ContainEqualValuesDifferentStart<T>(IEnumerable<T> expected, IEnumerable<T> actual, Func<T, T, bool> comparer)
        {
            Assert.IsNotNull(expected);
            Assert.IsNotNull(actual);

            var l1 = expected.ToList();
            var l2 = actual.ToList();

            Assert.AreEqual(l1.Count, l2.Count, "Lists contain different number of items");

            //Find some matching item
            int l2Idx = 0;
            while (l2Idx < l2.Count && !comparer(l1[0], l2[l2Idx]))
                l2Idx++;

            if (l2Idx >= l2.Count)
                Assert.Fail("Start index not found");

            int direction = 1;
            if (!comparer(l1[1], l2[(l2Idx + 1) % l2.Count]) && comparer(l1[1], l2[(l2Idx - 1 + l2.Count) % l2.Count]))
                direction = -1;

            for (int i = 0; i < l1.Count; i++)
            {
                Assert.IsTrue(comparer(l1[i], l2[l2Idx]), string.Format("Expected: {0}. Actual: {1}", l1[i], l2[l2Idx]));
                l2Idx = (l2Idx + direction + l2.Count) % l2.Count;
            }
        }

        public static void ContainEqualValuesDifferentStart(IEnumerable<double> expected, IEnumerable<double> actual)
        {
            ContainEqualValuesDifferentStart(expected, actual, Compare);
        }
        public static void ContainEqualValuesDifferentStart(IEnumerable<SimPoint3D> expected, IEnumerable<SimPoint3D> actual)
        {
            ContainEqualValuesDifferentStart(expected, actual, Compare);
        }

        public static void AreEqualMultiline(string expected, string actual)
        {
            int lineCount = 0;
            using (var expectedReader = new StringReader(expected))
            {
                using (var actualReader = new StringReader(actual))
                {
                    string prevExpectedLine = null;
                    string prevActualLine = null;
                    var expectedLine = expectedReader.ReadLine();
                    var actualLine = actualReader.ReadLine();
                    while (expectedLine != null || actualLine != null)
                    {
                        lineCount++;
                        if (expectedLine != null && actualLine != null &&
                            !expectedLine.Equals(actualLine))
                        {
                            int count = System.Math.Min(expectedLine.Length, actualLine.Length);
                            for (int i = 0; i < count; ++i)
                            {
                                if (expectedLine[i] != actualLine[i])
                                {
                                    int start = System.Math.Max(i - 10, 0);
                                    var nextExpect = expectedReader.ReadLine();
                                    var nextActual = actualReader.ReadLine();
                                    string expectedPrint =
                                        (prevExpectedLine == null ? "" : prevExpectedLine + "\n")
                                        + expectedLine.Substring(start, System.Math.Min(10, expectedLine.Length - start))
                                        + "--> " + expectedLine.Substring(i, System.Math.Min(20, expectedLine.Length - i))
                                        + (nextExpect == null ? "" : "\n" + nextExpect);
                                    string actualPrint =
                                        (prevActualLine == null ? "" : prevActualLine + "\n")
                                        + actualLine.Substring(start, System.Math.Min(10, actualLine.Length - start))
                                        + "--> " + actualLine.Substring(i, System.Math.Min(20, actualLine.Length - i))
                                        + (nextActual == null ? "" : "\n" + nextActual);


                                    Assert.Fail("Strings are not equal.\nMismatch in line {0} around\n--- Expected ---\n{1}\n\n--- Actual ---\n{2}",
                                        lineCount,
                                        expectedPrint,
                                        actualPrint
                                        );
                                }
                            }
                            if (expectedLine.Length != actualLine.Length)
                            {
                                int start = System.Math.Max(0, count - 10);

                                Assert.Fail("Line {4} length doesn't match. Expected={0}, Actual={1}\n--- Expected ---\n{2}\n\n--- Actual ---\n{3}",
                                    expectedLine.Length, actualLine.Length,
                                    expectedLine.Substring(start), actualLine.Substring(start), lineCount);
                            }
                        }
                        else if (!(expectedLine == null && actualLine == null))
                        {
                            // one file is shorter
                            if (expectedLine == null)
                                Assert.Fail("Actual string is too long at line {0}", lineCount);
                            if (actualLine == null)
                                Assert.Fail("Actual string is too short at line {0}\n--- Expected ---\n{1}", lineCount, expectedLine);
                        }

                        prevExpectedLine = expectedLine;
                        prevActualLine = actualLine;
                        expectedLine = expectedReader.ReadLine();
                        actualLine = actualReader.ReadLine();
                    }
                }
            }


        }


        public static void AssertContains<T, U>(Dictionary<T, U> dictionary, T key, U expected)
        {
            if (dictionary.TryGetValue(key, out var actual))
            {
                if (expected is double de && actual is double da)
                    Assert.AreEqual(de, da, 0.001);
                else
                    Assert.AreEqual(expected, actual);
            }
            else
                Assert.Fail("Dictionary does not contain key {0}", key);
        }
    }
}
