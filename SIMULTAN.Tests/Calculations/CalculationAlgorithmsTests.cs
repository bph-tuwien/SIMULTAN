using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Calculations
{
    [TestClass]
    public class CalculationAlgorithmsTests : BaseProjectTest
    {
        private static readonly FileInfo calculationProject = new FileInfo(@".\CalculationTestsProject.simultan");

        [TestMethod]
        public void ReplaceSingleParameter()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            //Trivial case
            {
                SimCalculation calc = new SimCalculation("a + b", "calc1",
                    new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                    new Dictionary<string, SimParameter> { { "out", demoParams["out"] } }
                    );

                //Failure case
                Assert.ThrowsException<ArgumentNullException>(() => { calc.ReplaceParameter(null, 0.0); });

                calc.ReplaceParameter("a", 9.9);

                Assert.AreEqual("9.9 + b", calc.Expression);
                Assert.AreEqual(SimCalculationValidity.Valid, calc.State);
                Assert.AreEqual(1, calc.InputParams.Count);
                Assert.AreEqual(demoParams["param2"], calc.InputParams["b"]);
                Assert.AreEqual(1, calc.ReturnParams.Count);
            }

            //Case with brackets and multi-use
            {
                SimCalculation calc = new SimCalculation("(a + b) * a", "calc1",
                    new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                    new Dictionary<string, SimParameter> { { "out", demoParams["out"] } }
                    );

                calc.ReplaceParameter("a", 9.9);

                Assert.AreEqual("(9.9 + b) * 9.9", calc.Expression);
                Assert.AreEqual(SimCalculationValidity.Valid, calc.State);
                Assert.AreEqual(1, calc.InputParams.Count);
                Assert.AreEqual(demoParams["param2"], calc.InputParams["b"]);
                Assert.AreEqual(1, calc.ReturnParams.Count);
            }

            //Case with functions
            {
                SimCalculation calc = new SimCalculation("a + b * Sin(a)", "calc1",
                    new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                    new Dictionary<string, SimParameter> { { "out", demoParams["out"] } }
                    );

                calc.ReplaceParameter("a", 9.9);

                Assert.AreEqual("9.9 + (b * Sin(9.9))", calc.Expression);
                Assert.AreEqual(SimCalculationValidity.Valid, calc.State);
                Assert.AreEqual(1, calc.InputParams.Count);
                Assert.AreEqual(demoParams["param2"], calc.InputParams["b"]);
                Assert.AreEqual(1, calc.ReturnParams.Count);
            }

            //Case with constants
            {
                SimCalculation calc = new SimCalculation("a + b * PI", "calc1",
                    new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                    new Dictionary<string, SimParameter> { { "out", demoParams["out"] } }
                    );

                calc.ReplaceParameter("a", 9.9);

                Assert.AreEqual("9.9 + (b * PI)", calc.Expression);
                Assert.AreEqual(SimCalculationValidity.Valid, calc.State);
                Assert.AreEqual(1, calc.InputParams.Count);
                Assert.AreEqual(demoParams["param2"], calc.InputParams["b"]);
                Assert.AreEqual(1, calc.ReturnParams.Count);
            }

            //Case with names similar to functions
            {
                SimCalculation calc = new SimCalculation("Sin + b * Sin(Sin)", "calc1",
                    new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                    new Dictionary<string, SimParameter> { { "out", demoParams["out"] } }
                    );

                calc.ReplaceParameter("Sin", 9.9);

                Assert.AreEqual("9.9 + (b * Sin(9.9))", calc.Expression);
                Assert.AreEqual(SimCalculationValidity.Valid, calc.State);
                Assert.AreEqual(1, calc.InputParams.Count);
                Assert.AreEqual(demoParams["param2"], calc.InputParams["b"]);
                Assert.AreEqual(1, calc.ReturnParams.Count);
            }
        }

        [TestMethod]
        public void MoveCalculation()
        {
            LoadProject(calculationProject);
            var comp1 = projectData.Components.First(x => x.Name == "NoCalculation");
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b*c", "calc",
                new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] }, { "c", demoParams["param3"] } },
                new Dictionary<string, SimParameter> { { "out1", demoParams["out"] }, { "out2", demoParams["out2"] } });

            comp1.Calculations.Add(calc);
            Assert.IsTrue(comp1.Calculations.Contains(calc));
            Assert.AreEqual(comp1, calc.Component);

            var comp2 = projectData.Components.First(x => x.Name == "MoveTarget");
            var comp2child = comp2.Components.First(x => x.Component != null && x.Component.Name == "SubMoveTarget")?.Component;

            CalculationAlgorithms.MoveCalculation(calc, comp2);
            Assert.IsFalse(comp1.Calculations.Contains(calc));
            Assert.IsTrue(comp2.Calculations.Contains(calc));
            Assert.AreEqual(comp2, calc.Component);

            //Check parameters
            Assert.AreEqual(comp2.Parameters.First(x => x.Name == "param1"), calc.InputParams["a"]);
            Assert.AreEqual(comp2child.Parameters.First(x => x.Name == "param2"), calc.InputParams["b"]);
            Assert.AreEqual(null, calc.InputParams["c"]);
            Assert.AreEqual(comp2.Parameters.First(x => x.Name == "out"), calc.ReturnParams["out1"]);
            Assert.AreEqual(null, calc.ReturnParams["out2"]);
        }

        [TestMethod]
        public void CopyCalculation()
        {
            LoadProject(calculationProject);
            var comp1 = projectData.Components.First(x => x.Name == "NoCalculation");
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b*c", "calc",
                new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] }, { "c", demoParams["param3"] } },
                new Dictionary<string, SimParameter> { { "out1", demoParams["out"] }, { "out2", demoParams["out2"] } });

            comp1.Calculations.Add(calc);
            Assert.IsTrue(comp1.Calculations.Contains(calc));
            Assert.AreEqual(comp1, calc.Component);

            var comp2 = projectData.Components.First(x => x.Name == "MoveTarget");
            var comp2child = comp2.Components.First(x => x.Component != null && x.Component.Name == "SubMoveTarget")?.Component;

            var copy = CalculationAlgorithms.CopyCalculation(calc, comp2);
            Assert.IsTrue(comp2.Calculations.Contains(copy));
            Assert.AreEqual(comp2, copy.Component);

            //Check parameters
            Assert.AreEqual(comp2.Parameters.First(x => x.Name == "param1"), copy.InputParams["a"]);
            Assert.AreEqual(comp2child.Parameters.First(x => x.Name == "param2"), copy.InputParams["b"]);
            Assert.AreEqual(null, copy.InputParams["c"]);
            Assert.AreEqual(comp2.Parameters.First(x => x.Name == "out"), copy.ReturnParams["out1"]);
            Assert.AreEqual(null, copy.ReturnParams["out2"]);
        }
    }
}
