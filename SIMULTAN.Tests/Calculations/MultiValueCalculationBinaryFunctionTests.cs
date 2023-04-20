using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.TestUtils;
using System.Collections.Generic;
using System.IO;

namespace SIMULTAN.Tests.Calculations
{
    [TestClass]
    public class MultiValueCalculationBinaryFunctionTests : BaseProjectTest
    {
        private static readonly FileInfo calculationProject = new FileInfo(@".\CalculationTestsProject.simultan");

        #region General

        [TestMethod]
        public void PropertyChanged()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a + b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            PropertyChangedEventCounter c = new PropertyChangedEventCounter(rootStep);

            rootStep.Operation = MultiValueCalculationBinaryOperation.EXTREME_MAX_OF_MATRIX;

            c.AssertEventCount(1);
            Assert.AreEqual(nameof(SimMultiValueExpressionBinary.Operation), c.PropertyChangedArgs[0]);
        }

        #endregion

        #region Addition

        [TestMethod]
        public void MatrixSumTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a + b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.MATRIX_SUM;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 2, 4, 6, 4 },
                { 9, 11, 13, 8 },
                { 9, 10, 11, 12 },
                { 13, 14, 15, 16 },
                { 17, 18, 19, 20 },
            }, result);
        }

        [TestMethod]
        public void MatrixSumRepeatColumnTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a + b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.MATRIX_SUM_REPEAT_COLUMN;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 2, 4, 6, 7 },
                { 9, 11, 13, 14 },
                { 9, 10, 11, 12 },
                { 13, 14, 15, 16 },
                { 17, 18, 19, 20 },
            }, result);
        }

        [TestMethod]
        public void MatrixSumRepeatRowColumnTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a + b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.MATRIX_SUM_REPEAT_ROWCOLUMN;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 2, 4, 6, 7 },
                { 9, 11, 13, 14 },
                { 13, 15, 17, 18 },
                { 17, 19, 21, 22 },
                { 21, 23, 25, 26 },
            }, result);
        }

        #endregion

        #region Vector-Vector

        [TestMethod]
        public void InnerProductTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.INNER_PRODUCT;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 177 }
            }, result);
        }

        [TestMethod]
        public void OuterProductTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.OUTER_PRODUCT;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 4 },
                { 5, 20 },
                { 9, 36 },
                { 13, 52 },
                { 17, 68 },
            }, result);
        }

        [TestMethod]
        public void OuterProductFlatTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.OUTER_PRODUCT_FLAT;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1 },
                { 5 },
                { 9 },
                { 13 },
                { 17 },
                { 4 },
                { 20 },
                { 36 },
                { 52 },
                { 68 },
            }, result);
        }

        #endregion

        #region Matrix-Matrix

        [TestMethod]
        public void MatrixProductTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in2"] }, { "b", demoParams["in3"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.MATRIX_PRODUCT;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 28, 34 },
                { 64, 79 }
            }, result);
        }

        [TestMethod]
        public void MatrixProductPerElementTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.MATRIX_PRODUCT_PERELEMENT;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 4, 9, 4 },
                { 20, 30, 42, 8 },
                { 9, 10, 11, 12 },
                { 13, 14, 15, 16 },
                { 17, 18, 19, 20 },
            }, result);
        }

        [TestMethod]
        public void MatrixProductPerElementRepeatTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in2"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.MATRIX_PRODUCT_PERELEMENT_REPEAT;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 4, 9, 12 },
                { 20, 30, 42, 48 },
                { 36, 50, 66, 72 },
                { 52, 70, 90, 96 },
                { 68, 90, 114, 120 },
            }, result);
        }

        #endregion

        #region Selection

        [TestMethod]
        public void ColumnSelectionTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in_idx"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.COLUMN_SELECTION;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { 1, 5, 9, 13, 17, 1, 5, 9, 13, 17, 3, 7, 11, 15, 19 }
            }), result);
        }

        [TestMethod]
        public void ColumnSelectionMatrixTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in_idx"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.COLUMN_SELECTION_AS_MATRIX;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 1, 3 },
                { 5, 5, 7 },
                { 9, 9, 11 },
                { 13, 13, 15 },
                { 17, 17, 19 },
            }, result);
        }

        [TestMethod]
        public void ColumnSelectionDiagonalMatrixTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in_idx"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.COLUMN_SELECTION_AS_DIAGONAL;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 0, 0 },
                { 5, 0, 0 },
                { 9, 0, 0 },
                { 13, 0, 0 },
                { 17, 0, 0 },
                { 0, 1, 0 },
                { 0, 5, 0 },
                { 0, 9, 0 },
                { 0, 13, 0 },
                { 0, 17, 0 },
                { 0, 0, 3 },
                { 0, 0, 7 },
                { 0, 0, 11 },
                { 0, 0, 15 },
                { 0, 0, 19 },
            }, result);
        }

        #endregion

        #region GroupBy

        [TestMethod]
        public void GroupBySumTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in_cat"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.CATEGORY_SUM;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 23, 26, 29, 32 },
                { 22, 24, 26, 28 },
            }, result);
        }

        [TestMethod]
        public void GroupByAverageTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in_cat"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.CATEGORY_AVERAGE;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 23 / 3.0, 26 / 3.0, 29 / 3.0, 32 / 3.0 },
                { 22 / 2.0, 24 / 2.0, 26 / 2.0, 28 / 2.0 },
            }, result);
        }

        [TestMethod]
        public void GroupByMinTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in_cat"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.CATEGORY_MIN;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 2, 3, 4 },
                { 9, 10, 11, 12 },
            }, result);
        }

        [TestMethod]
        public void GroupByMaxTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * b", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] }, { "b", demoParams["in_cat"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.CATEGORY_MAX;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 17, 18, 19, 20 },
                { 13, 14, 15, 16 },
            }, result);
        }

        #endregion

        #region Extrema Selection

        [TestMethod]
        public void SelectMinTests()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * 5", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.EXTREME_MIN_OF_MATRIX;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { 1, 0, 0, 2, 0, 1, 3, 0, 2, 4, 0, 3, 5, 1, 0 }
            }), result);
        }

        [TestMethod]
        public void SelectMaxTests()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");
            SimCalculation calc = new SimCalculation("a * 5", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionBinary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationBinaryOperation.EXTREME_MAX_OF_MATRIX;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(MultiValueCalculationsNEW.Transpose(new double[,]
            {
                { 20, 4, 3, 19, 4, 2, 18, 4, 1, 17, 4, 0, 16, 3, 3 }
            }), result);
        }

        #endregion
    }
}
