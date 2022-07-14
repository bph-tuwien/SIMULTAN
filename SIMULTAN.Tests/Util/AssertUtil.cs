using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Utils
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

            return Math.Abs(expected - actual) <= tolerance;
        }

        public static bool Compare(Point3D expected, Point3D actual)
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

        public static void ContainEqualValues(double[,] expected, SimMultiValueBigTable actual)
        {
            Assert.AreEqual(expected.GetLength(0), actual.Count(0),
                string.Format("Dimension 0 is not matching: Expected {0}, Actual {1}", expected.GetLength(0), actual.Count(0)));
            Assert.AreEqual(expected.GetLength(1), actual.Count(1),
                string.Format("Dimensions 1 is not matching: Expected {0}, Actual {1}", expected.GetLength(1), actual.Count(1)));

            for (int i = 0; i < expected.GetLength(0); ++i)
            {
                for (int j = 0; j < expected.GetLength(1); ++j)
                {
                    AssertDoubleEqual(expected[i, j], actual[i, j]);
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

        public static void AreEqual(Point3D expected, Point3D actual, double tolerance = 0.00001)
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
        public static void ContainEqualValuesDifferentStart(IEnumerable<Point3D> expected, IEnumerable<Point3D> actual)
        {
            ContainEqualValuesDifferentStart(expected, actual, Compare);
        }
    
        public static void AreEqualMultiline(string expected, string actual)
        {
            int count = Math.Min(expected.Length, actual.Length);
            int lineCount = 0;

            for (int i = 0; i < count; ++i)
            {
                if (expected[i] != actual[i])
                {
                    int start = Math.Max(i - 10, 0);
                    string expectedPrint = 
                        expected.Substring(start, Math.Min(10, expected.Length - start))
                        + "--> " + expected.Substring(i, Math.Min(20, expected.Length - i));
                    string actualPrint =
                        actual.Substring(start, Math.Min(10, actual.Length - start))
                        + "--> " + actual.Substring(i, Math.Min(20, actual.Length - i));


                    Assert.Fail("Strings are not equal.\nMismatch in line {0} around\n--- Expected ---\n{1}\n\n--- Actual ---\n{2}",
                        lineCount,
                        expectedPrint,
                        actualPrint
                        );
                }
                else if (expected[i] == '\n')
                    lineCount++;
            }

            if (expected.Length != actual.Length)
            {
                int start = Math.Max(0, count - 10);

                Assert.Fail("String length doesn't match. Expected={0}, Actual={1}\n--- Expected ---\n{2}\n\n--- Actual ---\n{3}",
                    expected.Length, actual.Length,
                    expected.Substring(start), actual.Substring(start));
            }
        }
    }
}
