using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Randomize;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Calculations
{
    class FakeRandomizer : IRandomizer
    {
        int count;
        int index = 0;

        public FakeRandomizer(int count)
        {
            this.count = count;
        }

        public double Next()
        {
            var result = (-2.0 + (index % count) * 4.0 / (count - 1));
            index++;
            return result;
        }
    }

    [TestClass]
    public class MultiValueCalculationOperandParameterTests : BaseProjectTest
    {
        private static readonly FileInfo calculationProject = new FileInfo(@"./CalculationTestsProject.simultan");

        private SimMultiValueExpressionParameter FindOperand(SimCalculation calc, string symbol)
        {
            return FindOperand(calc.MultiValueCalculation, symbol);
        }
        private SimMultiValueExpressionParameter FindOperand(SimMultiValueExpression op, string symbol)
        {
            if (op is SimMultiValueExpressionParameter p && p.Symbol == symbol)
                return p;

            if (op is SimMultiValueExpressionBinary step)
            {
                var left = FindOperand(step.Left, symbol);
                if (left != null)
                    return left;

                var right = FindOperand(step.Right, symbol);
                if (right != null)
                    return right;
            }

            return null;
        }

        [TestMethod]
        public void DefaultSettings()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", null } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var aMeta = calc.InputParams.GetMetaData("a");
            Assert.AreEqual(new RowColumnRange(0, 0, int.MaxValue, int.MaxValue), aMeta.Range);
            Assert.AreEqual(false, aMeta.IsRandomized);
            Assert.AreEqual(1.0, aMeta.RandomizeRelativeMean);
            Assert.AreEqual(1.0, aMeta.RandomizeDeviation);
            Assert.AreEqual(CalculationParameterMetaData.DeviationModeType.Absolute, aMeta.RandomizeDeviationMode);
            Assert.AreEqual(false, aMeta.RandomizeIsClamping);
            Assert.AreEqual(1.0, aMeta.RandomizeClampDeviation);
        }


        #region Range

        [TestMethod]
        public void SymbolNotBound()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", null } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;
            var aOperand = FindOperand(calc, "a");

            var aResult = aOperand.Calculate(calc);

            AssertUtil.ContainEqualValues(aResult, new double[,] { });
        }

        [TestMethod]
        public void ParameterBound()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;
            var aOperand = FindOperand(calc, "a");

            var aResult = aOperand.Calculate(calc);

            AssertUtil.ContainEqualValues(aResult, new double[,]
            {
                { 1, 2, 3, 4 },
                { 5, 6, 7, 8 },
                { 9, 10, 11, 12 },
                { 13, 14, 15, 16 },
                { 17, 18, 19, 20 }
            });
        }

        [TestMethod]
        public void ParameterRange()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;
            var aOperand = FindOperand(calc, "a");
            var aMeta = calc.InputParams.GetMetaData("a");
            PropertyChangedEventCounter propC = new PropertyChangedEventCounter(aMeta);

            //Max range
            Assert.AreEqual(new RowColumnRange(0, 0, int.MaxValue, int.MaxValue), aMeta.Range);
            var aResult = aOperand.Calculate(calc);
            AssertUtil.ContainEqualValues(aResult, new double[,]
            {
                { 1, 2, 3, 4 },
                { 5, 6, 7, 8 },
                { 9, 10, 11, 12 },
                { 13, 14, 15, 16 },
                { 17, 18, 19, 20 }
            });

            // Less columns/rows
            aMeta.Range = new RowColumnRange(0, 0, 2, 3);
            propC.AssertEventCount(1);
            Assert.AreEqual(nameof(CalculationParameterMetaData.Range), propC.PropertyChangedArgs[0]);
            Assert.AreEqual(new RowColumnRange(0, 0, 2, 3), aMeta.Range);
            aResult = aOperand.Calculate(calc);
            AssertUtil.ContainEqualValues(aResult, new double[,]
            {
                { 1, 2, 3 },
                { 5, 6, 7 }
            });

            //Non 0 start
            aMeta.Range = new RowColumnRange(2, 1, 2, 3);
            propC.AssertEventCount(2);
            Assert.AreEqual(nameof(CalculationParameterMetaData.Range), propC.PropertyChangedArgs[1]);
            Assert.AreEqual(new RowColumnRange(2, 1, 2, 3), aMeta.Range);
            aResult = aOperand.Calculate(calc);
            AssertUtil.ContainEqualValues(aResult, new double[,]
            {
                { 10, 11, 12 },
                { 14, 15, 16 }
            });

            //Too many rows/columns requested
            aMeta.Range = new RowColumnRange(2, 1, 10, 20);
            Assert.AreEqual(new RowColumnRange(2, 1, 10, 20), aMeta.Range);
            aResult = aOperand.Calculate(calc);
            AssertUtil.ContainEqualValues(aResult, new double[,]
            {
                { 10, 11, 12 },
                { 14, 15, 16 },
                { 18, 19, 20 }
            });
        }

        #endregion

        #region Randomization

        [TestMethod]
        public void RandomizeTest()
        {
            CalculationParameterMetaData.Randomizer = new FakeRandomizer(9);

            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in_scalar1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var aOperand = FindOperand(calc, "a");
            var aMeta = calc.InputParams.GetMetaData("a");
            PropertyChangedEventCounter propC = new PropertyChangedEventCounter(aMeta);

            aMeta.IsRandomized = true;
            propC.AssertEventCount(1);
            Assert.AreEqual(nameof(CalculationParameterMetaData.IsRandomized), propC.PropertyChangedArgs[0]);
            Assert.AreEqual(true, aMeta.IsRandomized);

            var results = new List<double>();
            for (int i = 0; i < 9; i++)
                results.Add(aOperand.Calculate(calc)[0, 0]);

            AssertUtil.ContainEqualValues(new List<double> { 13.0, 13.5, 14.0, 14.5, 15.0, 15.5, 16.0, 16.5, 17.0 }, results);
        }

        [TestMethod]
        public void RandomizeMeanTest()
        {
            CalculationParameterMetaData.Randomizer = new FakeRandomizer(9);

            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in_scalar1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var aOperand = FindOperand(calc, "a");
            var aMeta = calc.InputParams.GetMetaData("a");
            aMeta.IsRandomized = true;
            Assert.AreEqual(true, aMeta.IsRandomized);

            PropertyChangedEventCounter propC = new PropertyChangedEventCounter(aMeta);
            aMeta.RandomizeRelativeMean = 0.8;
            propC.AssertEventCount(1);
            Assert.AreEqual(nameof(CalculationParameterMetaData.RandomizeRelativeMean), propC.PropertyChangedArgs[0]);
            AssertUtil.AssertDoubleEqual(0.8, aMeta.RandomizeRelativeMean);


            var results = new List<double>();
            for (int i = 0; i < 9; i++)
                results.Add(aOperand.Calculate(calc)[0, 0]);

            AssertUtil.ContainEqualValues(new List<double> { 10.0, 10.5, 11.0, 11.5, 12.0, 12.5, 13.0, 13.5, 14.0 }, results);
        }

        [TestMethod]
        public void RandomizeDeviationTest()
        {
            CalculationParameterMetaData.Randomizer = new FakeRandomizer(9);

            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in_scalar1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var aOperand = FindOperand(calc, "a");
            var aMeta = calc.InputParams.GetMetaData("a");
            aMeta.IsRandomized = true;
            Assert.AreEqual(true, aMeta.IsRandomized);

            //Change Deviation
            PropertyChangedEventCounter propC = new PropertyChangedEventCounter(aMeta);

            aMeta.RandomizeDeviation = 2.0;

            propC.AssertEventCount(1);
            Assert.AreEqual(nameof(CalculationParameterMetaData.RandomizeDeviation), propC.PropertyChangedArgs[0]);
            AssertUtil.AssertDoubleEqual(2.0, aMeta.RandomizeDeviation);


            var results = new List<double>();
            for (int i = 0; i < 9; i++)
                results.Add(aOperand.Calculate(calc)[0, 0]);

            AssertUtil.ContainEqualValues(new List<double> { 11.0, 12.0, 13.0, 14.0, 15.0, 16.0, 17.0, 18.0, 19.0 }, results);

            //Change to relative deviation
            aMeta.RandomizeDeviationMode = CalculationParameterMetaData.DeviationModeType.Relative;

            propC.AssertEventCount(2);
            Assert.AreEqual(nameof(CalculationParameterMetaData.RandomizeDeviationMode), propC.PropertyChangedArgs[1]);
            Assert.AreEqual(CalculationParameterMetaData.DeviationModeType.Relative, aMeta.RandomizeDeviationMode);

            results = new List<double>();
            for (int i = 0; i < 9; i++)
                results.Add(aOperand.Calculate(calc)[0, 0]);

            AssertUtil.ContainEqualValues(new List<double> { -45.0, -30.0, -15.0, 0.0, 15.0, 30.0, 45.0, 60.0, 75.0 }, results);
        }

        [TestMethod]
        public void RandomizeClampTest()
        {
            CalculationParameterMetaData.Randomizer = new FakeRandomizer(9);

            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("a * a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in_scalar1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var aOperand = FindOperand(calc, "a");
            var aMeta = calc.InputParams.GetMetaData("a");
            aMeta.IsRandomized = true;
            Assert.AreEqual(true, aMeta.IsRandomized);

            PropertyChangedEventCounter propC = new PropertyChangedEventCounter(aMeta);

            aMeta.RandomizeIsClamping = true;
            propC.AssertEventCount(1);
            Assert.AreEqual(nameof(CalculationParameterMetaData.RandomizeIsClamping), propC.PropertyChangedArgs[0]);
            Assert.AreEqual(true, aMeta.RandomizeIsClamping);


            var results = new List<double>();
            for (int i = 0; i < 9; i++)
                results.Add(aOperand.Calculate(calc)[0, 0]);

            AssertUtil.ContainEqualValues(new List<double> { 14.0, 14.0, 14.0, 14.5, 15.0, 15.5, 16.0, 16.0, 16.0 }, results);

            aMeta.RandomizeClampDeviation = 0.5;
            propC.AssertEventCount(2);
            Assert.AreEqual(nameof(CalculationParameterMetaData.RandomizeClampDeviation), propC.PropertyChangedArgs[1]);
            AssertUtil.AssertDoubleEqual(0.5, aMeta.RandomizeClampDeviation);

            results = new List<double>();
            for (int i = 0; i < 9; i++)
                results.Add(aOperand.Calculate(calc)[0, 0]);

            AssertUtil.ContainEqualValues(new List<double> { 14.5, 14.5, 14.5, 14.5, 15.0, 15.5, 15.5, 15.5, 15.5 }, results);
        }

        #endregion

        #region Serialization File Version = 3

        private static readonly FileInfo importFileV3 = new FileInfo(@"./FormatTest_V3.simultan");

        [TestMethod]
        public void ImportConversionTestVersion3()
        {
            LoadProject(importFileV3);

            var comp = projectData.Components.First(x => x.Name == "Calc");
            var calc = comp.Calculations.First();

            var aMeta = calc.InputParams.GetMetaData("A");
            Assert.AreEqual(1, aMeta.Range.RowStart);
            Assert.AreEqual(3, aMeta.Range.RowCount);
            Assert.AreEqual(0, aMeta.Range.ColumnStart);
            Assert.AreEqual(2, aMeta.Range.ColumnCount);

            Assert.AreEqual(false, aMeta.IsRandomized);
            Assert.AreEqual(1.0, aMeta.RandomizeRelativeMean);
            Assert.AreEqual(1.0, aMeta.RandomizeDeviation);
            Assert.AreEqual(CalculationParameterMetaData.DeviationModeType.Relative, aMeta.RandomizeDeviationMode);
            Assert.AreEqual(true, aMeta.RandomizeIsClamping);
            Assert.AreEqual(1.0, aMeta.RandomizeClampDeviation);
        }

        #endregion

        #region ReferenceParameter

        [TestMethod]
        public void ReferencedTest()
        {
            LoadProject(calculationProject);
            var calcParent = projectData.Components.First(x => x.Name == "ReferenceUsingCalcParent");
            var calcComp = calcParent.Components.First(x => x.Component != null && x.Component.Name == "ReferenceUsingCalc")?.Component;
            var calc = calcComp.Calculations.First();

            calc.Calculate(projectData.ValueManager);

            var resultParam = calcComp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "out");

            Assert.IsTrue(resultParam.ValueSource != null);
            Assert.IsTrue(resultParam.ValueSource is SimMultiValueBigTableParameterSource);

            var resultTable = ((SimMultiValueBigTableParameterSource)resultParam.ValueSource).Table;
            AssertUtil.ContainEqualValues(new object[,]
            {
                { 4.0, 9.0 },
                { 16.0, 25.0 },
                { 36.0, 49.0 }
            }, resultTable);
        }

        #endregion
    }
}