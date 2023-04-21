using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Calculations
{
    [TestClass]
    public class MultiValueCalculationTests
    {
        private static double[,] TestData(int rows, int columns, int startValue = 0)
        {
            double[,] result = new double[rows, columns];

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    result[r, c] = startValue;
                    startValue++;
                }
            }

            return result;
        }

        [TestMethod]
        public void MatrixSumTest()
        {
            var lhs = TestData(4, 3, 0);
            var rhs = TestData(4, 3, 1);

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.MatrixSum(null, rhs, false, false); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.MatrixSum(lhs, null, false, false); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.MatrixSum(new double[,] { }, rhs, false, false); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.MatrixSum(lhs, new double[,] { }, false, false); });

            //Same size
            var result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, false, false);
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 1, 3, 5 },
                    { 7, 9, 11 },
                    { 13, 15, 17 },
                    { 19, 21, 23 },
                }, result);

            //rhs smaller
            rhs = TestData(2, 2, 99);
            result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, false, false); //Clamp
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 99, 101, 2 },
                    { 104, 106, 5 },
                    { 6, 7, 8 },
                    { 9, 10, 11 },
                }, result);

            //rhs larger
            rhs = TestData(4, 3, 0);
            lhs = TestData(2, 2, 99);
            result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, false, false); //Clamp
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 99, 101, 2 },
                    { 104, 106, 5 },
                    { 6, 7, 8 },
                    { 9, 10, 11 },
                }, result);
        }

        [TestMethod]
        public void MatrixSumTest_RepeatRows()
        {
            //rhs smaller
            var lhs = TestData(4, 3, 0);
            var rhs = TestData(2, 2, 99);

            var result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, true, false); //Repeat row
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 99, 101, 2 },
                    { 104, 106, 5 },
                    { 107, 109, 8 },
                    { 110, 112, 11 }
                }, result);

            //rhs larger
            rhs = TestData(4, 3, 0);
            lhs = TestData(2, 2, 99);

            result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, true, false); //Repeat row
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 99, 101, 2 },
                    { 104, 106, 5 },
                    { 107, 109, 8 },
                    { 110, 112, 11 }
                }, result);
        }

        [TestMethod]
        public void MatrixSumTest_RepeatColumns()
        {
            //rhs smaller
            var lhs = TestData(4, 3, 0);
            var rhs = TestData(2, 2, 99);

            var result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, false, true); //Repeat column
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 99, 101, 102 },
                    { 104, 106, 107 },
                    { 6, 7, 8 },
                    { 9, 10, 11 }
                }, result);

            //rhs larger
            rhs = TestData(4, 3, 0);
            lhs = TestData(2, 2, 99);

            result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, false, true); //Repeat column
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 99, 101, 102 },
                    { 104, 106, 107 },
                    { 6, 7, 8 },
                    { 9, 10, 11 }
                }, result);
        }

        [TestMethod]
        public void MatrixSumTest_RepeatBoth()
        {
            //rhs smaller
            var lhs = TestData(4, 3, 0);
            var rhs = TestData(2, 2, 99);

            var result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, true, true); //Repeat row & column
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 99, 101, 102 },
                    { 104, 106, 107 },
                    { 107, 109, 110 },
                    { 110, 112, 113 }
                }, result);

            //rhs larger
            rhs = TestData(4, 3, 0);
            lhs = TestData(2, 2, 99);

            result = MultiValueCalculationsNEW.MatrixSum(lhs, rhs, true, true); //Repeat row & column
            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 99, 101, 102 },
                    { 104, 106, 107 },
                    { 107, 109, 110 },
                    { 110, 112, 113 }
                }, result);
        }

        [TestMethod]
        public void AggregateAverageTest()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.Average(null); });

            List<double[,]> values = new List<double[,]>();
            values.Add(TestData(3, 2, 0));
            values.Add(TestData(4, 2, 0));
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.Average(values); });

            values = new List<double[,]>();
            for (int i = 0; i < 4; ++i)
                values.Add(TestData(3, 2, i));

            var avg = MultiValueCalculationsNEW.Average(values);

            AssertUtil.ContainEqualValues(new double[,]
                {
                    { 1.5, 2.5 },
                    { 3.5, 4.5 },
                    { 5.5, 6.5 },
                }, avg);
        }

        [TestMethod]
        public void InnerProductTest()
        {
            var lhs = TestData(3, 1, 0);
            var rhs = TestData(3, 1, 2);

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.InnerProduct(null, rhs); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.InnerProduct(lhs, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.InnerProduct(new double[,] { }, rhs); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.InnerProduct(lhs, new double[,] { }); });

            var result = MultiValueCalculationsNEW.InnerProduct(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,] { { 11 } }, result);

            //rhs smaller
            rhs = TestData(2, 1, 2);
            result = MultiValueCalculationsNEW.InnerProduct(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,] { { 9 } }, result);

            //lhs smaller
            lhs = TestData(2, 1, 1);
            rhs = TestData(3, 1, 2);
            result = MultiValueCalculationsNEW.InnerProduct(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,] { { 16 } }, result);

            //Matrices
            lhs = TestData(3, 2, 1);
            rhs = TestData(3, 2, 2);
            result = MultiValueCalculationsNEW.InnerProduct(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,] { { 44 } }, result);
        }

        [TestMethod]
        public void OuterProductTest()
        {
            var lhs = TestData(3, 1, 0);
            var rhs = TestData(3, 1, 2);

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.OuterProduct(null, rhs); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.OuterProduct(lhs, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.OuterProduct(new double[,] { }, rhs); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.OuterProduct(lhs, new double[,] { }); });

            var result = MultiValueCalculationsNEW.OuterProduct(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 0, 0 },
                { 2, 3, 4 },
                { 4, 6, 8 },
            },
                result);

            //Matrices
            lhs = TestData(3, 2, 1);
            rhs = TestData(3, 2, 2);
            result = MultiValueCalculationsNEW.OuterProduct(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 2, 4, 6 },
                { 6, 12, 18 },
                { 10, 20, 30 },
            }, result);
        }

        [TestMethod]
        public void OuterProductFlatTest()
        {
            var lhs = TestData(3, 1, 0);
            var rhs = TestData(3, 1, 2);

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.OuterProductFlat(null, rhs); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.OuterProductFlat(lhs, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.OuterProductFlat(new double[,] { }, rhs); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.OuterProductFlat(lhs, new double[,] { }); });

            var result = MultiValueCalculationsNEW.OuterProductFlat(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0 }, { 2 },{ 4 }, { 0 }, { 3 }, { 6 }, { 0 }, { 4 }, { 8 }
            },
                result);

            //Matrices
            lhs = TestData(3, 2, 1);
            rhs = TestData(3, 2, 2);
            result = MultiValueCalculationsNEW.OuterProductFlat(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 2 }, { 6 },{ 10 }, { 4 }, { 12 }, { 20 }, { 6 }, { 18 }, { 30 }
            }, result);
        }

        [TestMethod]
        public void MatrixProductTest()
        {
            var lhs = TestData(2, 3, 0);
            var rhs = TestData(3, 4, 2);

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.MatrixProduct(null, rhs); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.MatrixProduct(lhs, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.MatrixProduct(new double[,] { }, rhs); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.MatrixProduct(lhs, new double[,] { }); });

            var result = MultiValueCalculationsNEW.MatrixProduct(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 26, 29, 32, 35 },
                { 80, 92, 104, 116 }
            }, result);

            //Dimenison missmatch
            lhs = TestData(3, 2, 1);
            rhs = TestData(3, 2, 2);
            result = MultiValueCalculationsNEW.MatrixProduct(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { double.NaN }
            }, result);
        }

        [TestMethod]
        public void MatrixElementwiseProductTest()
        {
            var lhs = TestData(2, 3, 0);
            var rhs = TestData(2, 3, 2);

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.MatrixElementwiseProduct(null, rhs, false, false); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, null, false, false); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.MatrixElementwiseProduct(new double[,] { }, rhs, false, false); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, new double[,] { }, false, false); });

            //Same size
            var result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, false, false);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8 },
                { 15, 24, 35 }
            }, result);

            //Lhs smaller
            rhs = TestData(3, 4, 2);

            //No repeat
            result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, false, false);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8, 5 },
                { 18, 28, 40, 9 },
                { 10, 11, 12, 13 }
            }, result);

            //Rhs smaller
            lhs = TestData(3, 4, 0);
            rhs = TestData(2, 3, 2);

            //No repeat
            result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, false, false);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8, 3 },
                { 20, 30, 42, 7 },
                { 8, 9, 10, 11 }
            }, result);
        }

        [TestMethod]
        public void MatrixElementwiseProductTest_RepeatRows()
        {
            //Lhs smaller
            var lhs = TestData(2, 3, 0);
            var rhs = TestData(3, 4, 2);

            var result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, true, false);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8, 5 },
                { 18, 28, 40, 9 },
                { 30, 44, 60, 13 }
            }, result);

            //Rhs smaller
            lhs = TestData(3, 4, 0);
            rhs = TestData(2, 3, 2);

            result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, true, false);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8, 3 },
                { 20, 30, 42, 7 },
                { 40, 54, 70, 11 }
            }, result);
        }

        [TestMethod]
        public void MatrixElementwiseProductTest_RepeatColumns()
        {
            //Lhs smaller
            var lhs = TestData(2, 3, 0);
            var rhs = TestData(3, 4, 2);

            var result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, false, true);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8, 10 },
                { 18, 28, 40, 45 },
                { 10, 11, 12, 13 }
            }, result);

            //Rhs smaller
            lhs = TestData(3, 4, 0);
            rhs = TestData(2, 3, 2);

            result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, false, true);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8, 12 },
                { 20, 30, 42, 49 },
                { 8, 9, 10, 11 }
            }, result);
        }

        [TestMethod]
        public void MatrixElementwiseProductTest_RepeatBoth()
        {
            //Lhs smaller
            var lhs = TestData(2, 3, 0);
            var rhs = TestData(3, 4, 2);

            var result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, true, true);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8, 10 },
                { 18, 28, 40, 45 },
                { 30, 44, 60, 65 }
            }, result);

            //Rhs smaller
            lhs = TestData(3, 4, 0);
            rhs = TestData(2, 3, 2);

            result = MultiValueCalculationsNEW.MatrixElementwiseProduct(lhs, rhs, true, true);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 8, 12 },
                { 20, 30, 42, 49 },
                { 40, 54, 70, 77 }
            }, result);
        }

        [TestMethod]
        public void SelectColumnTest()
        {
            var lhs = TestData(2, 3, 0);
            var rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 1, 3 } });

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectColumns(null, rhs); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectColumns(lhs, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectColumns(new double[,] { }, rhs); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectColumns(lhs, new double[,] { }); });

            var result = MultiValueCalculationsNEW.SelectColumns(lhs, rhs);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { 0, 3, 0, 3, 2, 5 }
            }), result);

            //Matrix instead of vector
            rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 2, 1 }, { 3, 0 } });

            result = MultiValueCalculationsNEW.SelectColumns(lhs, rhs);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { 1, 4, 0, 3 }
            }), result);

            //Outside
            rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 0, 1, 4 } });
            result = MultiValueCalculationsNEW.SelectColumns(lhs, rhs);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { double.NaN, double.NaN, 0, 3, double.NaN, double.NaN }
            }), result);
        }

        [TestMethod]
        public void SelectColumnAsMatrixTest()
        {
            var lhs = TestData(2, 3, 0);
            var rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 1, 3 } });

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectColumnsAsMatrix(null, rhs); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectColumnsAsMatrix(lhs, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectColumnsAsMatrix(new double[,] { }, rhs); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectColumnsAsMatrix(lhs, new double[,] { }); });

            var result = MultiValueCalculationsNEW.SelectColumnsAsMatrix(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 0, 2 },
                { 3, 3, 5 }
            }, result);

            //Matrix instead of vector
            rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 2, 1 }, { 3, 0 } });

            result = MultiValueCalculationsNEW.SelectColumnsAsMatrix(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 0 },
                { 4, 3 }
            }, result);

            //Outside
            rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 0, 1, 4 } });
            result = MultiValueCalculationsNEW.SelectColumnsAsMatrix(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { double.NaN, 0, double.NaN },
                { double.NaN, 3, double.NaN }
            }, result);
        }

        [TestMethod]
        public void SelectColumnAsDiagonalMatrixTest()
        {
            var lhs = TestData(2, 3, 0);
            var rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 1, 3 } });

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectColumnsAsDiagonalMatrix(null, rhs); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectColumnsAsDiagonalMatrix(lhs, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectColumnsAsDiagonalMatrix(new double[,] { }, rhs); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectColumnsAsDiagonalMatrix(lhs, new double[,] { }); });

            var result = MultiValueCalculationsNEW.SelectColumnsAsDiagonalMatrix(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 0, 0 },
                { 3, 0, 0 },
                { 0, 0, 0 },
                { 0, 3, 0 },
                { 0, 0, 2 },
                { 0, 0, 5 }
            }, result);

            //Matrix instead of vector
            rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 2, 1 }, { 3, 0 } });

            result = MultiValueCalculationsNEW.SelectColumnsAsDiagonalMatrix(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 0 },
                { 4, 0 },
                { 0, 0 },
                { 0, 3 },
            }, result);

            //Outside
            rhs = MultiValueCalculationsNEW.Transpose(new double[,] { { 0, 1, 4 } });
            result = MultiValueCalculationsNEW.SelectColumnsAsDiagonalMatrix(lhs, rhs);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { double.NaN, 0, 0 },
                { double.NaN, 0, 0 },
                { 0, 0, 0 },
                { 0, 3, 0 },
                { 0, 0, double.NaN },
                { 0, 0, double.NaN },
            }, result);
        }

        [TestMethod]
        public void GroupBySumTest()
        {
            var data = TestData(5, 3);
            var categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 4, 3 } });

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.GroupBySum(null, categories); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.GroupBySum(data, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.GroupBySum(new double[,] { }, categories); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.GroupBySum(data, new double[,] { }); });

            var result = MultiValueCalculationsNEW.GroupBySum(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 6, 8, 10 },
                { 15, 17, 19 },
                { 9, 10, 11 },
            }, result);

            //Too less categories
            categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 4 } });

            result = MultiValueCalculationsNEW.GroupBySum(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 6, 8, 10 },
                { 3, 4, 5 },
                { 9, 10, 11 },
            }, result);

            //Too many categories
            categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 4, 3, 5, 7 } });

            result = MultiValueCalculationsNEW.GroupBySum(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 6, 8, 10 },
                { 15, 17, 19 },
                { 9, 10, 11 },
            }, result);
        }

        [TestMethod]
        public void GroupByAverageTest()
        {
            var data = TestData(5, 3);
            var categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1, 3 } });

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.GroupByAverage(null, categories); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.GroupByAverage(data, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.GroupByAverage(new double[,] { }, categories); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.GroupByAverage(data, new double[,] { }); });

            var result = MultiValueCalculationsNEW.GroupByAverage(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 5, 6, 7 },
                { 7.5, 8.5, 9.5 },
            }, result);

            //Too less categories
            categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1 } });

            result = MultiValueCalculationsNEW.GroupByAverage(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 5, 6, 7 },
                { 3, 4, 5 },
            }, result);

            //Too many categories
            categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1, 3, 5, 7 } });

            result = MultiValueCalculationsNEW.GroupByAverage(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 5, 6, 7 },
                { 7.5, 8.5, 9.5 },
            }, result);
        }

        [TestMethod]
        public void GroupByMaxTest()
        {
            var data = TestData(5, 3, -15);
            var categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1, 3 } });

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.GroupByMax(null, categories); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.GroupByMax(data, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.GroupByMax(new double[,] { }, categories); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.GroupByMax(data, new double[,] { }); });

            var result = MultiValueCalculationsNEW.GroupByMax(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { -6, -5, -4 },
                { -3, -2, -1 },
            }, result);

            //Too less categories
            categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1 } });

            result = MultiValueCalculationsNEW.GroupByMax(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { -6, -5, -4 },
                { -12, -11, -10 },
            }, result);

            //Too many categories
            categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1, 3, 5, 7 } });

            result = MultiValueCalculationsNEW.GroupByMax(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { -6, -5, -4 },
                { -3, -2, -1 },
            }, result);
        }

        [TestMethod]
        public void GroupByMinTest()
        {
            var data = TestData(5, 3);
            var categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1, 3 } });

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.GroupByMin(null, categories); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.GroupByMin(data, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.GroupByMin(new double[,] { }, categories); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.GroupByMin(data, new double[,] { }); });

            var result = MultiValueCalculationsNEW.GroupByMin(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 1, 2 },
                { 3, 4, 5 },
            }, result);

            //Too less categories
            categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1 } });

            result = MultiValueCalculationsNEW.GroupByMin(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 1, 2 },
                { 3, 4, 5 },
            }, result);

            //Too many categories
            categories = MultiValueCalculationsNEW.Transpose(new double[,] { { 1, 3, 1, 1, 3, 5, 7 } });

            result = MultiValueCalculationsNEW.GroupByMin(data, categories);
            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 1, 2 },
                { 3, 4, 5 },
            }, result);
        }


        [TestMethod]
        public void SelectMinTest()
        {
            var M = new double[,]
                {
                    { 3, -4, 1 },
                    { 1, 2, 99 }
                };
            var N = new double[,] { { 3 } };

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectMinIndex(null, N); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectMinIndex(M, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectMinIndex(new double[,] { }, N); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectMinIndex(M, new double[,] { }); });

            var result = MultiValueCalculationsNEW.SelectMinIndex(M, N);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { -4, 0, 1, 1, 0, 2, 1, 1, 0 }
            }), result);

            //Invert order
            result = MultiValueCalculationsNEW.SelectMinIndex(N, M);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { -4, 0, 1, 1, 0, 2, 1, 1, 0 }
            }), result);

            //N larger than matrix
            N = new double[,] { { 7 } };
            result = MultiValueCalculationsNEW.SelectMinIndex(N, M);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { -4, 0, 1, 1, 0, 2, 1, 1, 0, 2, 1, 1, 3, 0, 0, 99, 1, 2, double.NaN, double.NaN, double.NaN }
            }), result);
        }

        [TestMethod]
        public void SelectMaxTest()
        {
            var M = new double[,]
                {
                    { 3, -100, 3 },
                    { 1, 2, 99 }
                };
            var N = new double[,] { { 3 } };

            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectMaxIndex(null, N); });
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.SelectMaxIndex(M, null); });

            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectMaxIndex(new double[,] { }, N); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.SelectMaxIndex(M, new double[,] { }); });

            var result = MultiValueCalculationsNEW.SelectMaxIndex(M, N);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { 99, 1, 2, 3, 0, 0, 3, 0, 2 }
            }), result);

            //Invert order
            result = MultiValueCalculationsNEW.SelectMaxIndex(N, M);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { 99, 1, 2, 3, 0, 0, 3, 0, 2 }
            }), result);

            //N larger than matrix
            N = new double[,] { { 7 } };
            result = MultiValueCalculationsNEW.SelectMaxIndex(N, M);
            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { 99, 1, 2, 3, 0, 0, 3, 0, 2, 2, 1, 1, 1, 1, 0, -100, 0, 1, double.NaN, double.NaN, double.NaN }
            }), result);
        }

        [TestMethod]
        public void TransposeTest()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.Transpose(null); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.Transpose(new double[,] { }); });

            var mat = TestData(4, 3, 0);
            var result = MultiValueCalculationsNEW.Transpose(mat);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 0, 3, 6, 9 },
                { 1, 4, 7, 10 },
                { 2, 5, 8, 11 }
            }, result);
        }

        [TestMethod]
        public void NegateTest()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { MultiValueCalculationsNEW.Transpose(null); });
            Assert.ThrowsException<ArgumentException>(() => { MultiValueCalculationsNEW.Transpose(new double[,] { }); });

            var mat = new double[,]
            {
                { -1, 1, 0 },
                { 5, -99, 3 },
                { double.PositiveInfinity, double.NegativeInfinity, double.NaN }
            };
            var result = MultiValueCalculationsNEW.Negate(mat);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, -1, 0 },
                { -5, 99, -3 },
                { double.NegativeInfinity, double.PositiveInfinity, double.NaN }
            }, result);
        }
    }
}
