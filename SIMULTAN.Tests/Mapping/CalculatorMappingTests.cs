﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Mapping
{
    [TestClass]
    public class CalculatorMappingTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\CalculatorTestsProject.simultan");

        [TestMethod]
        public void CtorTest()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            //Valid projectData
            var testMapping = new CalculatorMapping("TestMapping", testData.calcComp, testData.inputMapping, testData.outputMapping);

            Assert.AreEqual("TestMapping", testMapping.Name);
            Assert.AreEqual(testData.calcComp, testMapping.Calculator);

            foreach (var entry in testData.inputMapping)
                Assert.IsTrue(testMapping.InputMapping.Any(x => x.CalculatorParameter == entry.CalculatorParameter && x.DataParameter == entry.DataParameter));
            foreach (var entry in testData.outputMapping)
                Assert.IsTrue(testMapping.OutputMapping.Any(x => x.CalculatorParameter == entry.CalculatorParameter && x.DataParameter == entry.DataParameter));
        }
        [TestMethod]
        public void CtorExceptionsTest()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            //Null checks
            {
                Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    new CalculatorMapping(null, testData.calcComp,
                        new List<CalculatorMapping.MappingParameterTuple>(),
                        new List<CalculatorMapping.MappingParameterTuple>());
                });
                Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    new CalculatorMapping("TestMapping", null,
                        new List<CalculatorMapping.MappingParameterTuple>(),
                        new List<CalculatorMapping.MappingParameterTuple>());
                });
                Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    new CalculatorMapping("TestMapping", testData.calcComp,
                        null,
                        new List<CalculatorMapping.MappingParameterTuple>());
                });
                Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    new CalculatorMapping("TestMapping", testData.calcComp,
                        new List<CalculatorMapping.MappingParameterTuple>(),
                        null);
                });
            }

            //Null in mapping
            {
                Assert.ThrowsException<ArgumentException>((() =>
                {
                    new CalculatorMapping("TestMapping", testData.calcComp,
                        new List<CalculatorMapping.MappingParameterTuple> { new CalculatorMapping.MappingParameterTuple(null, testData.inputMapping[0].CalculatorParameter) },
                        new List<CalculatorMapping.MappingParameterTuple>());
                }));
                Assert.ThrowsException<ArgumentException>((() =>
                {
                    new CalculatorMapping("TestMapping", testData.calcComp,
                        new List<CalculatorMapping.MappingParameterTuple> { new CalculatorMapping.MappingParameterTuple(testData.inputMapping[0].DataParameter, null) },
                        new List<CalculatorMapping.MappingParameterTuple>());
                }));
                Assert.ThrowsException<ArgumentException>((() =>
                {
                    new CalculatorMapping("TestMapping", testData.calcComp,
                        new List<CalculatorMapping.MappingParameterTuple>(),
                        new List<CalculatorMapping.MappingParameterTuple> { new CalculatorMapping.MappingParameterTuple(null, testData.outputMapping[0].DataParameter) });
                }));
                Assert.ThrowsException<ArgumentException>((() =>
                {
                    new CalculatorMapping("TestMapping", testData.calcComp,
                        new List<CalculatorMapping.MappingParameterTuple>(),
                        new List<CalculatorMapping.MappingParameterTuple> { new CalculatorMapping.MappingParameterTuple(testData.outputMapping[0].DataParameter, null) });
                }));
            }
        }
        [TestMethod]
        public void CtorInvalidData()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            Assert.ThrowsException<ArgumentException>(() =>
            {
                new CalculatorMapping("TestMapping", testData.calcComp,
                    new List<CalculatorMapping.MappingParameterTuple> { new CalculatorMapping.MappingParameterTuple(null, testData.inputMapping[0].CalculatorParameter) },
                    testData.outputMapping);
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                new CalculatorMapping("TestMapping", testData.calcComp,
                    new List<CalculatorMapping.MappingParameterTuple> { new CalculatorMapping.MappingParameterTuple(testData.inputMapping[0].DataParameter, null) },
                    testData.outputMapping);
            });

            Assert.ThrowsException<ArgumentException>(() =>
            {
                new CalculatorMapping("TestMapping", testData.calcComp,
                    testData.inputMapping,
                    new List<CalculatorMapping.MappingParameterTuple> { new CalculatorMapping.MappingParameterTuple(null, testData.outputMapping[0].CalculatorParameter) });
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                new CalculatorMapping("TestMapping", testData.calcComp,
                    testData.inputMapping,
                    new List<CalculatorMapping.MappingParameterTuple> { new CalculatorMapping.MappingParameterTuple(testData.outputMapping[0].DataParameter, null) });
            });
        }


        [TestMethod]
        public void CreateMappingTo()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            Assert.ThrowsException<ArgumentNullException>(() => { testData.dataComp.CreateMappingTo(null, testData.calcComp); });
            Assert.ThrowsException<ArgumentNullException>(() => { testData.dataComp.CreateMappingTo("TestMapping", null); });

            var testMapping = testData.dataComp.CreateMappingTo("TestMapping1", testData.calcComp);
            Assert.AreEqual("TestMapping1", testMapping.Name);
            Assert.AreEqual(testData.calcComp, testMapping.Calculator);
            Assert.AreEqual(0, testMapping.InputMapping.Count);
            Assert.AreEqual(0, testMapping.OutputMapping.Count);
            Assert.IsTrue(testData.dataComp.CalculatorMappings.Contains(testMapping));
            Assert.IsTrue(testData.calcComp.MappedToBy.Contains(testData.dataComp));
        }
        [TestMethod]
        public void CreateMappingToWithMappings()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            Assert.ThrowsException<ArgumentNullException>(() => { testData.dataComp.CreateMappingTo(null, testData.calcComp, testData.inputMapping, testData.outputMapping); });
            Assert.ThrowsException<ArgumentNullException>(() => { testData.dataComp.CreateMappingTo("TestMapping", null, testData.inputMapping, testData.outputMapping); });
            Assert.ThrowsException<ArgumentNullException>(() => { testData.dataComp.CreateMappingTo("TestMapping", testData.calcComp, null, testData.outputMapping); });
            Assert.ThrowsException<ArgumentNullException>(() => { testData.dataComp.CreateMappingTo("TestMapping", testData.calcComp, testData.inputMapping, null); });

            var testMapping = testData.dataComp.CreateMappingTo("TestMapping", testData.calcComp, testData.inputMapping, testData.outputMapping);
            Assert.AreEqual("TestMapping", testMapping.Name);
            Assert.AreEqual(testData.calcComp, testMapping.Calculator);
            Assert.AreEqual(2, testMapping.InputMapping.Count);
            Assert.AreEqual(1, testMapping.OutputMapping.Count);
            Assert.IsTrue(testData.dataComp.CalculatorMappings.Contains(testMapping));
            Assert.IsTrue(testData.calcComp.MappedToBy.Contains(testData.dataComp));
        }

        [TestMethod]
        public void RemoveMapping()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            var testMapping = testData.dataComp.CreateMappingTo("TestMapping1", testData.calcComp);

            WeakReference testMappingRef = new WeakReference(testMapping);
            Assert.IsTrue(testMappingRef.IsAlive);

            testData.dataComp.RemoveMapping(testMapping);

            Assert.IsFalse(testData.dataComp.CalculatorMappings.Contains(testMapping));
            Assert.IsFalse(testData.calcComp.MappedToBy.Contains(testData.dataComp));
            testMapping = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(testMappingRef.IsAlive);
        }
        [TestMethod]
        public void RemoveMappingTo()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            var testMapping = testData.dataComp.CreateMappingTo("TestMapping1", testData.calcComp);

            WeakReference testMappingRef = new WeakReference(testMapping);
            Assert.IsTrue(testMappingRef.IsAlive);

            testData.dataComp.RemoveMappingTo(testData.calcComp);

            Assert.IsFalse(testData.dataComp.CalculatorMappings.Contains(testMapping));
            Assert.IsFalse(testData.calcComp.MappedToBy.Contains(testData.dataComp));
            testMapping = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(testMappingRef.IsAlive);
        }


        [TestMethod]
        public void ExchangeDataParameter()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);
            var testMapping = testData.dataComp.CreateMappingTo("TestMapping", testData.calcComp, testData.inputMapping, testData.outputMapping);

            var data2Comp = projectData.Components.First(x => x.Name == "Data2");

            Dictionary<SimParameter, SimParameter> paramMapping = new Dictionary<SimParameter, SimParameter>();
            foreach (var param in testData.dataComp.Parameters)
            {
                paramMapping.Add(param, data2Comp.Parameters.First(x => x.Name == param.Name));
            }

            string[] testNames = new string[] { "a", "b" };
            for (int i = 0; i < testMapping.InputMapping.Count; ++i)
            {
                Assert.AreEqual(testData.dataComp, testMapping.InputMapping[i].DataParameter.Component);
                Assert.AreEqual(testNames[i], testMapping.InputMapping[i].DataParameter.Name);
            }
            Assert.AreEqual(1, testMapping.OutputMapping.Count);
            Assert.AreEqual(testData.dataComp, testMapping.OutputMapping[0].DataParameter.Component);
            Assert.AreEqual("result", testMapping.OutputMapping[0].DataParameter.Name);

            var exchangedMapping = testMapping.ExchangeDataParameter(paramMapping);

            for (int i = 0; i < testMapping.InputMapping.Count; ++i)
            {
                var mapping = exchangedMapping.InputMapping[i];
                Assert.AreEqual(data2Comp, mapping.DataParameter.Component);
                Assert.AreEqual(testNames[i], mapping.DataParameter.Name);
            }
            Assert.AreEqual(1, exchangedMapping.OutputMapping.Count);
            Assert.AreEqual(data2Comp, exchangedMapping.OutputMapping[0].DataParameter.Component);
            Assert.AreEqual("result", exchangedMapping.OutputMapping[0].DataParameter.Name);
        }


        [TestMethod]
        public void AddInvalidInputData()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            //Valid projectData
            var testMapping = new CalculatorMapping("TestMapping", testData.calcComp, testData.inputMapping, testData.outputMapping);

            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.InputMapping.Add(new CalculatorMapping.MappingParameterTuple(null, testData.inputMapping[0].CalculatorParameter));
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.InputMapping.Add(new CalculatorMapping.MappingParameterTuple(testData.inputMapping[0].DataParameter, null));
            });
        }
        [TestMethod]
        public void AddInvalidOutputData()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            //Valid projectData
            var testMapping = new CalculatorMapping("TestMapping", testData.calcComp, testData.inputMapping, testData.outputMapping);

            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.OutputMapping.Add(new CalculatorMapping.MappingParameterTuple(null, testData.outputMapping[0].CalculatorParameter));
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.OutputMapping.Add(new CalculatorMapping.MappingParameterTuple(testData.outputMapping[0].DataParameter, null));
            });

            var invalidParam = projectData.Components.First(x => x.Name == "Other")
                .Parameters.First(x => x.Name == "Output_NotValid");

            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.OutputMapping.Add(new CalculatorMapping.MappingParameterTuple(invalidParam, testData.outputMapping[0].CalculatorParameter));
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.OutputMapping.Add(new CalculatorMapping.MappingParameterTuple(testData.outputMapping[0].DataParameter, invalidParam));
            });
        }

        [TestMethod]
        public void SetInvalidInputData()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            //Valid projectData
            var testMapping = new CalculatorMapping("TestMapping", testData.calcComp, testData.inputMapping, testData.outputMapping);

            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.InputMapping[0] = new CalculatorMapping.MappingParameterTuple(null, testData.inputMapping[0].CalculatorParameter);
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.InputMapping[0] = new CalculatorMapping.MappingParameterTuple(testData.inputMapping[0].DataParameter, null);
            });
        }
        [TestMethod]
        public void SetInvalidOutputData()
        {
            LoadProject(testProject);
            var testData = TestData(projectData);

            //Valid projectData
            var testMapping = new CalculatorMapping("TestMapping", testData.calcComp, testData.inputMapping, testData.outputMapping);

            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.OutputMapping[0] = new CalculatorMapping.MappingParameterTuple(null, testData.outputMapping[0].CalculatorParameter);
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.OutputMapping[0] = new CalculatorMapping.MappingParameterTuple(testData.outputMapping[0].DataParameter, null);
            });

            var invalidParam = projectData.Components.First(x => x.Name == "Other")
                .Parameters.First(x => x.Name == "Output_NotValid");

            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.OutputMapping[0] = new CalculatorMapping.MappingParameterTuple(invalidParam, testData.outputMapping[0].CalculatorParameter);
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                testMapping.OutputMapping[0] = new CalculatorMapping.MappingParameterTuple(testData.outputMapping[0].DataParameter, invalidParam);
            });
        }

        [TestMethod]
        public void SimpleCalculationTest()
        {
            LoadProject(testProject);

            var dataComp = projectData.Components.First(x => x.Name == "SimpleData");
            var calcComp = projectData.Components.First(x => x.Name == "SimpleCalc");

            var resultParam = dataComp.Parameters.First(x => x.Name == "result");
            Assert.AreEqual(0.0, resultParam.ValueCurrent);

            dataComp.EvaluateAllMappings();

            Assert.AreEqual(6.0, resultParam.ValueCurrent);
        }
        [TestMethod]
        public void SimpleCalculationTestWithSubTree()
        {
            LoadProject(testProject);

            var dataComp = projectData.Components.First(x => x.Name == "WithSubData");
            var calcComp = projectData.Components.First(x => x.Name == "SimpleCalc");

            var resultParam = dataComp.Components.First(x => x.Component != null && x.Component.Name == "SubResult").Component
                .Parameters.First(x => x.Name == "result");
            Assert.AreEqual(0.0, resultParam.ValueCurrent);

            dataComp.EvaluateAllMappings();

            Assert.AreEqual(9.0, resultParam.ValueCurrent);
        }
        [TestMethod]
        public void ComplexCalculationTest()
        {
            LoadProject(testProject);

            var dataComp = projectData.Components.First(x => x.Name == "ComplexData");
            var calcComp = projectData.Components.First(x => x.Name == "ComplexCalc");

            var resultParam = dataComp.Components.First(x => x.Component != null && x.Component.Name == "SubResult").Component
                .Parameters.First(x => x.Name == "result");
            Assert.AreEqual(0.0, resultParam.ValueCurrent);

            dataComp.EvaluateAllMappings();

            Assert.AreEqual(40.0, resultParam.ValueCurrent);
        }


        [TestMethod]
        public void GetErrorsTest()
        {
            LoadProject(testProject);

            var dataComp = projectData.Components.First(x => x.Name == "SimpleData");
            var calcComp = projectData.Components.First(x => x.Name == "SimpleCalc");

            var errorDataComp1 = projectData.Components.First(x => x.Name == "ErrorData_NoCalculation");
            var errorDataComp2 = projectData.Components.First(x => x.Name == "ErrorData_InvalidPropagation");
            var errorDataComp3 = projectData.Components.First(x => x.Name == "ErrorData_NoOutput");

            Assert.ThrowsException<ArgumentNullException>(() => { dataComp.CalculatorMappings.First().GetErrors(null).Count(); });

            //No erors
            Assert.IsFalse(dataComp.CalculatorMappings.First().GetErrors(dataComp).Any());

            //Self ref
            Assert.IsTrue(dataComp.CalculatorMappings.First().GetErrors(calcComp).Contains(CalculatorMappingErrors.SELF_REFERENCE));

            //Other
            Assert.IsTrue(errorDataComp1.CalculatorMappings.First().GetErrors(errorDataComp1).Contains(CalculatorMappingErrors.NO_CALCULATION_FOUND));
            Assert.IsTrue(errorDataComp2.CalculatorMappings.First().GetErrors(errorDataComp1).Contains(CalculatorMappingErrors.INVALID_PARAMETER_PROPAGATION));
            Assert.IsTrue(errorDataComp3.CalculatorMappings.First().GetErrors(errorDataComp1).Contains(CalculatorMappingErrors.NO_OUTPUT_MAPPING));
        }


        private (SimComponent dataComp, SimComponent calcComp,
            List<CalculatorMapping.MappingParameterTuple> inputMapping,
            List<CalculatorMapping.MappingParameterTuple> outputMapping)
            TestData(ExtendedProjectData projectData)
        {
            var dataComp = projectData.Components.First(x => x.Name == "Data");
            var calcComp = projectData.Components.First(x => x.Name == "Calc");

            var inputMapping = new List<CalculatorMapping.MappingParameterTuple>
            {
                new CalculatorMapping.MappingParameterTuple(dataComp.Parameters.First(x => x.Name == "a"), calcComp.Parameters.First(x => x.Name == "a")),
                new CalculatorMapping.MappingParameterTuple(dataComp.Parameters.First(x => x.Name == "b"), calcComp.Parameters.First(x => x.Name == "b")),
            };
            var outputMapping = new List<CalculatorMapping.MappingParameterTuple>
            {
                new CalculatorMapping.MappingParameterTuple(dataComp.Parameters.First(x => x.Name == "result"), calcComp.Parameters.First(x => x.Name == "result")),
            };

            return (
                    dataComp,
                    calcComp,
                    inputMapping,
                    outputMapping
                );
        }
    }
}