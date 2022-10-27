using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Calculations
{
    [TestClass]
    public class CalculationTests : BaseProjectTest
    {
        private static readonly FileInfo calculationProject = new FileInfo(@".\CalculationTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@".\AccessTestsProject.simultan");

        #region Utils

        public static void CheckCalculation(SimCalculation calc,
            (string expression, string name, Dictionary<string, SimParameter> input, Dictionary<string, SimParameter> output) data)
        {
            Assert.AreEqual(data.name, calc.Name);
            Assert.AreEqual(data.expression, calc.Expression);

            CheckParamDict(data.input, calc.InputParams);
            CheckParamDict(data.output, calc.ReturnParams);
        }

        private static void CheckParamDict(IDictionary<string, SimParameter> expected, SimCalculation.BaseCalculationParameterCollections actual)
        {
            if (expected == null)
                Assert.AreEqual(0, actual.Count);
            else
            {
                Assert.AreEqual(expected.Count, actual.Count);

                foreach (var expectedEntry in expected)
                {
                    var found = actual.TryGetValue(expectedEntry.Key, out var actualEntry);
                    Assert.IsTrue(found);
                    Assert.AreEqual(expectedEntry.Value, actualEntry);
                }
            }
        }

        private static void AssertParamCollectionChanged(NotifyCollectionChangedEventArgs arg, NotifyCollectionChangedAction action,
            KeyValuePair<string, SimParameter> expectedOld, KeyValuePair<string, SimParameter> expectedNew)
        {
            Assert.AreEqual(action, arg.Action);

            switch (action)
            {
                case NotifyCollectionChangedAction.Add:
                    Assert.AreEqual(null, arg.OldItems);
                    Assert.IsTrue(arg.NewItems.Contains(expectedNew));
                    break;
                case NotifyCollectionChangedAction.Replace:
                    Assert.IsTrue(arg.NewItems.Contains(expectedNew));
                    Assert.IsTrue(arg.OldItems.Contains(expectedOld));
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Assert.IsTrue(arg.OldItems.Contains(expectedOld));
                    Assert.AreEqual(null, arg.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Assert.AreEqual(null, arg.OldItems);
                    Assert.AreEqual(null, arg.NewItems);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion


        [TestMethod]
        public void Ctor()
        {
            var calc = new SimCalculation("x", "calc1", null, null);
            CheckCalculation(calc, ("x", "calc1", new Dictionary<string, SimParameter> { { "x", null } }, null));
            Assert.AreEqual(SimId.Empty, calc.Id);
            Assert.AreEqual(1, calc.IterationCount);
            Assert.AreEqual(true, calc.OverrideResult);
            Assert.AreEqual(SimResultAggregationMethod.Average, calc.ResultAggregation);

            calc = new SimCalculation(null, "calc2", null, null);
            CheckCalculation(calc, ("", "calc2", null, null));

            calc = new SimCalculation("a+b", "calc3", null, null);
            CheckCalculation(calc, ("a+b", "calc3", new Dictionary<string, SimParameter> { { "a", null }, { "b", null } }, null));

            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            //Starting parameter passing
            calc = new SimCalculation("a+b", "calc4", new Dictionary<string, SimParameter> { { "a", demoParams["param1"] } }, null);
            CheckCalculation(calc, ("a+b", "calc4", new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", null } }, null));

            //Mismatching parameter names
            calc = new SimCalculation("a+b", "calc5", new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "c", demoParams["param2"] } }, null);
            CheckCalculation(calc, ("a+b", "calc5", new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", null } }, null));

            //Output passing
            calc = new SimCalculation("a+b", "calc6", null, new Dictionary<string, SimParameter> { { "out1", demoParams["param1"] }, { "out2", null } });
            CheckCalculation(calc, ("a+b", "calc6", new Dictionary<string, SimParameter> { { "a", null }, { "b", null } },
                new Dictionary<string, SimParameter> { { "out1", demoParams["param1"] }, { "out2", null } }));

            //Wrong propagation in input
            Assert.ThrowsException<InvalidStateException>(() =>
            {
                new SimCalculation("a+b", "calc4", new Dictionary<string, SimParameter> { { "a", demoParams["out"] } }, null);
            });

            //Wrong propagation in output
            Assert.ThrowsException<InvalidStateException>(() =>
            {
                new SimCalculation("a+b", "calc4", null, new Dictionary<string, SimParameter> { { "out1", demoParams["in"] } });
            });
        }

        [TestMethod]
        public void CtorParsing()
        {
            var calc = new SimCalculation(12345, "x", "calc1", null, null, null, null, 99, false, SimResultAggregationMethod.Separate);
            CheckCalculation(calc, ("x", "calc1", new Dictionary<string, SimParameter> { { "x", null } }, null));
            Assert.AreEqual(12345, calc.LocalID);
            Assert.AreEqual(99, calc.IterationCount);
            Assert.AreEqual(false, calc.OverrideResult);
            Assert.AreEqual(SimResultAggregationMethod.Separate, calc.ResultAggregation);
        }



        #region Properties

        [TestMethod]
        public void PropertyName()
        {
            SimCalculation calc = new SimCalculation("a+b", "calc1", null, null);
            PropertyTestUtils.CheckProperty(calc, nameof(SimCalculation.Name), "someName");
        }

        [TestMethod]
        public void PropertyDescription()
        {
            SimCalculation calc = new SimCalculation("a+b", "calc1", null, null);
            PropertyTestUtils.CheckProperty(calc, nameof(SimCalculation.Description), "someDescription");
        }

        [TestMethod]
        public void PropertyExpression()
        {
            SimCalculation calc = new SimCalculation("a+b", "calc1", null, null);
            PropertyTestUtils.CheckProperty(calc, nameof(SimCalculation.Expression), "a*b");
        }

        [TestMethod]
        public void PropertyIsMultiValueCalculation()
        {
            SimCalculation calc = new SimCalculation("a+b", "calc1", null, null);
            PropertyTestUtils.CheckProperty(calc, nameof(SimCalculation.IsMultiValueCalculation), true);
        }

        [TestMethod]
        public void PropertyIterationCount()
        {
            SimCalculation calc = new SimCalculation("a+b", "calc1", null, null);
            PropertyTestUtils.CheckProperty(calc, nameof(SimCalculation.IterationCount), 99);
        }

        [TestMethod]
        public void PropertyOverrideResult()
        {
            SimCalculation calc = new SimCalculation("a+b", "calc1", null, null);
            PropertyTestUtils.CheckProperty(calc, nameof(SimCalculation.OverrideResult), false);
        }

        [TestMethod]
        public void PropertyResultAggregation()
        {
            SimCalculation calc = new SimCalculation("a+b", "calc1", null, null);
            PropertyTestUtils.CheckProperty(calc, nameof(SimCalculation.ResultAggregation), SimResultAggregationMethod.Separate);
        }

        #endregion

        #region Property Access

        private void CheckCalculationPropertyAccess<T>(string prop, T value)
        {
            LoadProject(accessProject, "bph", "bph");

            var archCalc = projectData.Components.First(x => x.Name == "ArchComp").Calculations.First(x => x.Name == "Calc1");
            var bphCalc = projectData.Components.First(x => x.Name == "BPHComp").Calculations.First(x => x.Name == "Calc2");

            PropertyTestUtils.CheckPropertyAccess(bphCalc, archCalc, prop, value);
        }

        [TestMethod]
        public void PropertyNameAccess()
        {
            CheckCalculationPropertyAccess(nameof(SimCalculation.Name), "someRandomName");
        }

        [TestMethod]
        public void PropertyDescriptionAccess()
        {
            CheckCalculationPropertyAccess(nameof(SimCalculation.Description), "someRandomDescription");
        }

        [TestMethod]
        public void PropertyExpressionAccess()
        {
            CheckCalculationPropertyAccess(nameof(SimCalculation.Expression), "a+b");
        }

        [TestMethod]
        public void PropertyIsMultiValueCalculationAccess()
        {
            CheckCalculationPropertyAccess(nameof(SimCalculation.IsMultiValueCalculation), true);
        }

        [TestMethod]
        public void PropertyIterationCountAccess()
        {
            CheckCalculationPropertyAccess(nameof(SimCalculation.IterationCount), 99);
        }

        [TestMethod]
        public void PropertyOverrideResultAccess()
        {
            CheckCalculationPropertyAccess(nameof(SimCalculation.OverrideResult), false);
        }

        [TestMethod]
        public void PropertyResultAggregationAccess()
        {
            CheckCalculationPropertyAccess(nameof(SimCalculation.ResultAggregation), SimResultAggregationMethod.Separate);
        }

        #endregion

        #region Property Changes

        private void CheckCalculationPropertyChanges<T>(string prop, T value)
        {
            //Setup
            LoadProject(accessProject, "bph", "bph");

            var bphComponent = projectData.Components.First(x => x.Name == "BPHComp");
            var bphCalc = bphComponent.Calculations.First(x => x.Name == "Calc2");

            PropertyTestUtils.CheckPropertyChanges(bphCalc, prop, value, SimUserRole.BUILDING_PHYSICS, bphComponent, projectData.Components);
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            CheckCalculationPropertyChanges(nameof(SimCalculation.Name), "someRandomName");
        }

        [TestMethod]
        public void PropertyDescriptionChanges()
        {
            CheckCalculationPropertyChanges(nameof(SimCalculation.Description), "someRandomDescription");
        }

        [TestMethod]
        public void PropertyExpressionChanges()
        {
            CheckCalculationPropertyChanges(nameof(SimCalculation.Expression), "a+b");
        }

        [TestMethod]
        public void PropertyIsMultiValueCalculationChanges()
        {
            CheckCalculationPropertyChanges(nameof(SimCalculation.IsMultiValueCalculation), true);
        }

        [TestMethod]
        public void PropertyIterationCountChanges()
        {
            CheckCalculationPropertyChanges(nameof(SimCalculation.IterationCount), 99);
        }

        [TestMethod]
        public void PropertyOverrideResultChanges()
        {
            CheckCalculationPropertyChanges(nameof(SimCalculation.OverrideResult), false);
        }

        [TestMethod]
        public void PropertyResultAggregationChanges()
        {
            CheckCalculationPropertyChanges(nameof(SimCalculation.ResultAggregation), SimResultAggregationMethod.Separate);
        }

        #endregion


        #region Input Parameter Bindings

        [TestMethod]
        public void InputParamsAdd()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.InputParams);
            Assert.AreEqual(null, calc.InputParams["a"]);
            ccCounter.AssertEventCount(0);

            //Add parameter
            calc.InputParams["a"] = demoParams["param1"];
            Assert.AreEqual(demoParams["param1"], calc.InputParams["a"]);
            ccCounter.AssertEventCount(1);
            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Replace,
                new KeyValuePair<string, SimParameter>("a", null),
                new KeyValuePair<string, SimParameter>("a", demoParams["param1"]));
            ccCounter.Reset();
        }

        [TestMethod]
        public void InputParamsAddWrongPropagation()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.InputParams);

            Assert.AreEqual(null, calc.InputParams["a"]);
            ccCounter.AssertEventCount(0);

            //Add parameter with wrong propagation
            Assert.ThrowsException<InvalidStateException>(() => { calc.InputParams["a"] = demoParams["out"]; });
            Assert.AreEqual(null, calc.InputParams["a"]);
            ccCounter.AssertEventCount(0);
        }

        [TestMethod]
        public void InputParamsReplace()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.InputParams["a"] = demoParams["param1"];

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.InputParams);

            //Replace parameter
            calc.InputParams["a"] = demoParams["param2"];
            Assert.AreEqual(demoParams["param2"], calc.InputParams["a"]);
            ccCounter.AssertEventCount(1);
            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Replace,
                new KeyValuePair<string, SimParameter>("a", demoParams["param1"]),
                new KeyValuePair<string, SimParameter>("a", demoParams["param2"]));
            ccCounter.Reset();
        }

        [TestMethod]
        public void InputParamsReplaceWrongPropagation()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.InputParams["a"] = demoParams["param1"];

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.InputParams);

            //Replace parameter with wrong propagation
            Assert.ThrowsException<InvalidStateException>(() => { calc.InputParams["a"] = demoParams["out"]; });
            Assert.AreEqual(demoParams["param1"], calc.InputParams["a"]);
            ccCounter.AssertEventCount(0);
        }

        [TestMethod]
        public void InputParamsRemove()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.InputParams["a"] = demoParams["param1"];

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.InputParams);

            //Remove Parameter
            calc.InputParams["a"] = null;
            Assert.AreEqual(null, calc.InputParams["a"]);
            ccCounter.AssertEventCount(1);
            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Replace,
                new KeyValuePair<string, SimParameter>("a", demoParams["param1"]),
                new KeyValuePair<string, SimParameter>("a", null));
            ccCounter.Reset();
        }

        [TestMethod]
        public void InputParams_ParameterChanged()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            //Exception for added parameter
            {
                var calc = new SimCalculation("a+b", "calc", null, null);
                calc.InputParams["a"] = demoParams["param1"];

                Assert.ThrowsException<InvalidStateException>(() => { demoParams["param1"].Propagation = SimInfoFlow.Output; });
            }

            //Working case, no exception after remove
            {
                var calc = new SimCalculation("a+b", "calc", null, null);
                calc.InputParams["a"] = demoParams["param2"];
                demoParams["param2"].Propagation = SimInfoFlow.Input;

                Assert.AreEqual(demoParams["param2"], calc.InputParams["a"]);
                Assert.AreEqual(SimInfoFlow.Input, demoParams["param2"].Propagation);

                calc.InputParams["a"] = null;
                demoParams["param2"].Propagation = SimInfoFlow.Output;
            }

            //Exception for initial parameter
            {
                var calc = new SimCalculation("a+b", "calc", new Dictionary<string, SimParameter> { { "a", demoParams["param3"] } }, null);
                Assert.ThrowsException<InvalidStateException>(() => { demoParams["param3"].Propagation = SimInfoFlow.Output; });
            }
        }

        [TestMethod]
        public void InputParamsAccess()
        {
            LoadProject(accessProject, "bph", "bph");

            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var archCalc = archComp.Calculations.First(x => x.Name == "Calc1");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var bphCalc = bphComp.Calculations.First(x => x.Name == "Calc2");
            var param2 = bphComp.Parameters.First(x => x.TaxonomyEntry.Name == "Parameter2");

            //Working
            bphCalc.InputParams["b"] = param2;

            //Not working
            Assert.ThrowsException<AccessDeniedException>(() => { archCalc.InputParams["b"] = null; });
        }

        #endregion

        #region Return Parameter Bindings

        [TestMethod]
        public void ReturnParamsAddEmpty()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.ReturnParams);
            Assert.AreEqual(0, calc.ReturnParams.Count);

            //Add empty output parameter
            calc.ReturnParams.Add("out1", null);
            Assert.AreEqual(null, calc.ReturnParams["out1"]);
            ccCounter.AssertEventCount(1);
            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Add,
                new KeyValuePair<string, SimParameter>(),
                new KeyValuePair<string, SimParameter>("out1", null));
            ccCounter.Reset();
        }

        [TestMethod]
        public void ReturnParamsWrongPropagation()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.ReturnParams.Add("out1", null);

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.ReturnParams);

            //Add parameter with wrong propagation
            Assert.ThrowsException<InvalidStateException>(() => { calc.ReturnParams.Add("out2", demoParams["in"]); });
            Assert.AreEqual(1, calc.ReturnParams.Count);
            ccCounter.AssertEventCount(0);
        }

        [TestMethod]
        public void ReturnParamsAddParameter()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.ReturnParams);
            Assert.AreEqual(0, calc.ReturnParams.Count);

            //Add parameter
            calc.ReturnParams.Add("out2", demoParams["out"]);
            Assert.AreEqual(demoParams["out"], calc.ReturnParams["out2"]);
            ccCounter.AssertEventCount(1);
            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Add,
                new KeyValuePair<string, SimParameter>(),
                new KeyValuePair<string, SimParameter>("out2", demoParams["out"]));
            ccCounter.Reset();
        }

        [TestMethod]
        public void ReturnParamsReplaceParameter()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.ReturnParams.Add("out1", null);
            calc.ReturnParams.Add("out2", demoParams["out"]);

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.ReturnParams);

            //Replace parameter
            calc.ReturnParams["out1"] = demoParams["param1"];
            Assert.AreEqual(demoParams["param1"], calc.ReturnParams["out1"]);
            ccCounter.AssertEventCount(1);
            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Replace,
                new KeyValuePair<string, SimParameter>("out1", null),
                new KeyValuePair<string, SimParameter>("out1", demoParams["param1"]));
            ccCounter.Reset();
        }

        [TestMethod]
        public void ReturnParamsReplaceWrongPropagation()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.ReturnParams.Add("out1", null);
            calc.ReturnParams.Add("out2", demoParams["out"]);

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.ReturnParams);

            //Replace parameter with wrong propagation
            Assert.ThrowsException<InvalidStateException>(() => { calc.ReturnParams["out1"] = demoParams["in"]; });
            Assert.AreEqual(null, calc.ReturnParams["out1"]);
            ccCounter.AssertEventCount(0);
        }

        [TestMethod]
        public void ReturnParamsUnbind()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.ReturnParams.Add("out1", demoParams["param1"]);
            calc.ReturnParams.Add("out2", demoParams["out"]);

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.ReturnParams);

            //Unbind Parameter
            calc.ReturnParams["out2"] = null;
            Assert.AreEqual(null, calc.ReturnParams["out2"]);
            Assert.AreEqual(2, calc.ReturnParams.Count);
            ccCounter.AssertEventCount(1);
            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Replace,
                new KeyValuePair<string, SimParameter>("out2", demoParams["out"]),
                new KeyValuePair<string, SimParameter>("out2", null));
            ccCounter.Reset();
        }

        [TestMethod]
        public void ReturnParamsRemove()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.ReturnParams.Add("out1", demoParams["param1"]);
            calc.ReturnParams.Add("out2", demoParams["out"]);

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.ReturnParams);

            //Remove Parameter
            calc.ReturnParams.Remove("out1");
            Assert.AreEqual(1, calc.ReturnParams.Count);
            Assert.AreEqual("out2", calc.ReturnParams.First().Key);
            ccCounter.AssertEventCount(1);

            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Remove,
                new KeyValuePair<string, SimParameter>("out1", demoParams["param1"]),
                new KeyValuePair<string, SimParameter>());
            ccCounter.Reset();
        }

        [TestMethod]
        public void ReturnParamsClear()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, null);
            calc.ReturnParams.Add("out2", demoParams["out"]);

            CollectionChangedEventCounter ccCounter = new CollectionChangedEventCounter(calc.ReturnParams);

            //Clear
            calc.ReturnParams.Clear();
            Assert.AreEqual(0, calc.ReturnParams.Count);
            ccCounter.AssertEventCount(1);
            AssertParamCollectionChanged(ccCounter.CollectionChangedArgs[0], NotifyCollectionChangedAction.Reset,
                new KeyValuePair<string, SimParameter>(),
                new KeyValuePair<string, SimParameter>());
            ccCounter.Reset();
        }

        [TestMethod]
        public void ReturnParams_ParameterChanged()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            //Exception for added parameter
            {
                var calc = new SimCalculation("a+b", "calc", null, null);
                calc.ReturnParams.Add("out1", demoParams["out"]);

                Assert.ThrowsException<InvalidStateException>(() => { demoParams["out"].Propagation = SimInfoFlow.Input; });
            }

            //Working case, change to allowed propagation, no exception after remove
            {
                var calc = new SimCalculation("a+b", "calc", null, null);
                calc.ReturnParams.Add("out1", demoParams["param2"]);
                demoParams["param2"].Propagation = SimInfoFlow.Output;

                Assert.AreEqual(demoParams["param2"], calc.ReturnParams["out1"]);
                Assert.AreEqual(SimInfoFlow.Output, demoParams["param2"].Propagation);

                calc.ReturnParams["out1"] = null;
                demoParams["param2"].Propagation = SimInfoFlow.Input;
            }

            //Exception for initial parameter
            {
                var calc = new SimCalculation("a+b", "calc", null, new Dictionary<string, SimParameter> { { "out1", demoParams["param3"] } });
                Assert.ThrowsException<InvalidStateException>(() => { demoParams["param3"].Propagation = SimInfoFlow.Input; });
            }
        }


        [TestMethod]
        public void ReturnParamsAddEmptyAccess()
        {
            LoadProject(accessProject, "bph", "bph");

            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var archCalc = archComp.Calculations.First(x => x.Name == "Calc1");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var bphCalc = bphComp.Calculations.First(x => x.Name == "Calc2");
            var param2 = bphComp.Parameters.First(x => x.TaxonomyEntry.Name == "Parameter2");

            //Working
            bphCalc.ReturnParams.Add("out2", null);

            //Not working
            Assert.ThrowsException<AccessDeniedException>(() => { archCalc.ReturnParams.Add("out2", null); });
        }

        [TestMethod]
        public void ReturnParamsAddParameterAccess()
        {
            LoadProject(accessProject, "bph", "bph");

            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var archCalc = archComp.Calculations.First(x => x.Name == "Calc1");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var bphCalc = bphComp.Calculations.First(x => x.Name == "Calc2");
            var param2 = bphComp.Parameters.First(x => x.TaxonomyEntry.Name == "Parameter2");

            var outparam = new SimParameter("outputtest", "", 15.0);

            //Working
            bphCalc.ReturnParams["out1"] = outparam;

            //Not working
            Assert.ThrowsException<AccessDeniedException>(() => { archCalc.ReturnParams["out01"] = outparam; });
        }

        [TestMethod]
        public void ReturnParamsRemoveAccess()
        {
            LoadProject(accessProject, "bph", "bph");

            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var archCalc = archComp.Calculations.First(x => x.Name == "Calc1");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var bphCalc = bphComp.Calculations.First(x => x.Name == "Calc2");
            var param2 = bphComp.Parameters.First(x => x.TaxonomyEntry.Name == "Parameter2");

            var outparam = new SimParameter("outputtest", "", 15.0);

            //Working
            bphCalc.ReturnParams.Remove("out1");

            //Not working
            Assert.ThrowsException<AccessDeniedException>(() => { archCalc.ReturnParams.Remove("out01"); });
        }

        #endregion

        #region State

        [TestMethod]
        public void State_InputParameters()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc", null, new Dictionary<string, SimParameter> { { "out1", demoParams["out"] } });
            PropertyChangedEventCounter propCounter = new PropertyChangedEventCounter(calc);

            Assert.AreEqual(SimCalculationValidity.ParamNotBound, calc.State);

            //Set Param, b still missing
            calc.InputParams["a"] = demoParams["param1"];
            propCounter.AssertEventCount(0);

            //All parameters set
            calc.InputParams["b"] = demoParams["param2"];
            propCounter.AssertEventCount(1);
            Assert.AreEqual(nameof(SimCalculation.State), propCounter.PropertyChangedArgs[0]);
            propCounter.Reset();

            //Rest expression, c missing
            calc.Expression = "a*b*c";
            Assert.IsTrue(propCounter.PropertyChangedArgs.Contains(nameof(SimCalculation.State)));
            propCounter.Reset();

            //All parameters set again
            calc.InputParams["c"] = demoParams["param3"];
            propCounter.AssertEventCount(1);
            Assert.AreEqual(nameof(SimCalculation.State), propCounter.PropertyChangedArgs[0]);
            propCounter.Reset();

            //Unbind one
            calc.InputParams["b"] = null;
            propCounter.AssertEventCount(1);
            Assert.AreEqual(nameof(SimCalculation.State), propCounter.PropertyChangedArgs[0]);
            propCounter.Reset();
        }

        [TestMethod]
        public void State_OutputParameters()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc",
                new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                new Dictionary<string, SimParameter> { { "out1", null } });
            PropertyChangedEventCounter propCounter = new PropertyChangedEventCounter(calc);

            Assert.AreEqual(SimCalculationValidity.ParamNotBound, calc.State);

            //Bind parameter
            calc.ReturnParams["out1"] = demoParams["out"];
            propCounter.AssertEventCount(1);
            Assert.AreEqual(nameof(SimCalculation.State), propCounter.PropertyChangedArgs[0]);
            propCounter.Reset();

            //Add new bound paramter
            calc.ReturnParams.Add("out2", demoParams["param3"]);
            propCounter.AssertEventCount(0);

            //Add unbound parameter
            calc.ReturnParams.Add("unboundOut", null);
            propCounter.AssertEventCount(0);

            //Remove the bound params
            calc.ReturnParams.Remove("out1");
            propCounter.AssertEventCount(0);

            calc.ReturnParams.Remove("out2");
            propCounter.AssertEventCount(1);
            Assert.AreEqual(nameof(SimCalculation.State), propCounter.PropertyChangedArgs[0]);
            propCounter.Reset();
        }

        [TestMethod]
        public void State_Expression()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc",
                new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                new Dictionary<string, SimParameter> { { "out1", demoParams["out"] } });
            PropertyChangedEventCounter propCounter = new PropertyChangedEventCounter(calc);
            Assert.AreEqual(SimCalculationValidity.Valid, calc.State);

            //Change to invalid
            calc.Expression = "a *) b";
            Assert.AreEqual(SimCalculationValidity.InvalidExpression, calc.State);
            propCounter.AssertEventCount(2);
            Assert.IsTrue(propCounter.PropertyChangedArgs.Contains(nameof(SimCalculation.State)));
            propCounter.Reset();

            //Change to valid
            calc.Expression = "a * b";
            Assert.AreEqual(SimCalculationValidity.Valid, calc.State);
            propCounter.AssertEventCount(2);
            Assert.IsTrue(propCounter.PropertyChangedArgs.Contains(nameof(SimCalculation.State)));
            propCounter.Reset();


            //Create invalid
            calc = new SimCalculation("a $ b", "calc",
                new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                new Dictionary<string, SimParameter> { { "out1", demoParams["out"] } });
            propCounter = new PropertyChangedEventCounter(calc);
            Assert.AreEqual(SimCalculationValidity.InvalidExpression, calc.State);

            //Make valid
            calc.Expression = "a + b";
            Assert.AreEqual(SimCalculationValidity.ParamNotBound, calc.State);
            propCounter.AssertEventCount(2);
            Assert.IsTrue(propCounter.PropertyChangedArgs.Contains(nameof(SimCalculation.State)));
            propCounter.Reset();

            //Bind params
            calc.InputParams["a"] = demoParams["param1"];
            calc.InputParams["b"] = demoParams["param2"];
            Assert.AreEqual(SimCalculationValidity.Valid, calc.State);
            propCounter.AssertEventCount(1);
            Assert.IsTrue(propCounter.PropertyChangedArgs.Contains(nameof(SimCalculation.State)));
            propCounter.Reset();
        }
        #endregion


        #region Calculations

        [TestMethod]
        public void SimpleCalculations()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc",
                new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                new Dictionary<string, SimParameter> { { "out1", demoParams["out"] } });
            PropertyChangedEventCounter propCounter = new PropertyChangedEventCounter(calc);
            Assert.AreEqual(SimCalculationValidity.Valid, calc.State);

            calc.Calculate(projectData.ValueManager);
            AssertUtil.AssertDoubleEqual(3.0, demoParams["out"].ValueCurrent);

            //Change parameter value
            demoParams["param1"].ValueCurrent = 99.0;
            calc.Calculate(projectData.ValueManager);
            AssertUtil.AssertDoubleEqual(101.0, demoParams["out"].ValueCurrent);

            //Change parameter
            calc.InputParams["a"] = demoParams["param3"];
            calc.Calculate(projectData.ValueManager);
            AssertUtil.AssertDoubleEqual(5.0, demoParams["out"].ValueCurrent);

            //Change equation
            calc.Expression = "a * b + c";
            calc.InputParams["c"] = demoParams["param1"];
            calc.Calculate(projectData.ValueManager);
            AssertUtil.AssertDoubleEqual(105.0, demoParams["out"].ValueCurrent);

            //Multiple outputs
            calc.ReturnParams.Add("out2", demoParams["out2"]);
            calc.Calculate(projectData.ValueManager);
            AssertUtil.AssertDoubleEqual(105.0, demoParams["out"].ValueCurrent);
            AssertUtil.AssertDoubleEqual(105.0, demoParams["out2"].ValueCurrent);
        }

        [TestMethod]
        public void CalculateWithOverwrite()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc",
                new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                new Dictionary<string, SimParameter> { { "out1", demoParams["out"] } });
            PropertyChangedEventCounter propCounter = new PropertyChangedEventCounter(calc);
            Assert.AreEqual(SimCalculationValidity.Valid, calc.State);

            calc.Calculate(projectData.ValueManager);
            AssertUtil.AssertDoubleEqual(3.0, demoParams["out"].ValueCurrent);

            //Overwrite input
            Dictionary<SimParameter, SimParameter> overwrites = new Dictionary<SimParameter, SimParameter>
            {
                { demoParams["param1"], demoParams["param3"] }
            };
            calc.Calculate(projectData.ValueManager, null, null, overwrites);
            AssertUtil.AssertDoubleEqual(5.0, demoParams["out"].ValueCurrent);
        }

        [TestMethod]
        public void CalculateInstance()
        {
            LoadProject(calculationProject);
            var demoParams = projectData.GetParameters("NoCalculation");

            var calc = new SimCalculation("a+b", "calc",
                new Dictionary<string, SimParameter> { { "a", demoParams["param1"] }, { "b", demoParams["param2"] } },
                new Dictionary<string, SimParameter> { { "out1", demoParams["out"] } });

            PropertyChangedEventCounter propCounter = new PropertyChangedEventCounter(calc);
            Assert.AreEqual(SimCalculationValidity.Valid, calc.State);

            //Make sure that output parameter isn't changed
            demoParams["out"].ValueCurrent = 9999.9;

            Dictionary<SimParameter, double> overwrites = new Dictionary<SimParameter, double>()
            {
                { demoParams["param1"], 17 },
                { demoParams["out"], 76.0 }
            };

            Assert.ThrowsException<ArgumentNullException>(() => { calc.Calculate((Dictionary<SimParameter, double>)null); });

            calc.Calculate(overwrites);
            Assert.AreEqual(2, overwrites.Count);
            Assert.AreEqual(17, overwrites[demoParams["param1"]]);
            Assert.AreEqual(19, overwrites[demoParams["out"]]);
            Assert.AreEqual(1, demoParams["param1"].ValueCurrent);
            Assert.AreEqual(9999.9, demoParams["out"].ValueCurrent);
        }

        #endregion
    }
}
