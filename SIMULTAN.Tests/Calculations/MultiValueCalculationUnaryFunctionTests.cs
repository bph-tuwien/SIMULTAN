using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.TestUtils;
using System.Collections.Generic;
using System.IO;

namespace SIMULTAN.Tests.Calculations
{
    [TestClass]
    public class MultiValueCalculationUnaryFunctionTests : BaseProjectTest
    {
        private static readonly FileInfo calculationProject = new FileInfo(@"./CalculationTestsProject.simultan");

        [TestMethod]
        public void TransposeTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("Transpose(a)", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionUnary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationUnaryOperation.Transpose;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { 1, 5, 9, 13, 17 },
                { 2, 6, 10, 14, 18 },
                { 3, 7, 11, 15, 19 },
                { 4, 8, 12, 16, 20 },
            }, result);
        }

        [TestMethod]
        public void NegateTest()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters<SimDoubleParameter>("MVCalcEmpty");

            SimCalculation calc = new SimCalculation("-a", "calc",
                new Dictionary<string, SimDoubleParameter> { { "a", demoParams["in1"] } },
                new Dictionary<string, SimDoubleParameter> { { "out", demoParams["out1"] } });
            calc.IsMultiValueCalculation = true;

            var rootStep = ((SimMultiValueExpressionUnary)calc.MultiValueCalculation);
            rootStep.Operation = MultiValueCalculationUnaryOperation.Negate;

            var result = rootStep.Calculate(calc);

            AssertUtil.ContainEqualValues(new double[,]
            {
                { -1, -2, -3, -4 },
                { -5, -6, -7, -8 },
                { -9, -10, -11, -12 },
                { -13, -14, -15, -16 },
                { -17, -18, -19, -20 }
            }, result);
        }
    }
}
