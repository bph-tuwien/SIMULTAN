using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Values;


namespace SIMULTAN.Tests.Parameters
{
    [TestClass]
    public class ParameterMultiValuePointerTests
    {


        [TestMethod]
        public void ParameterBigTableValueChangedInstanceValueCheckDouble()
        {
            var data = SimMultiValueBigTableTests.DoubleTestDataTable(3, 4);

            var component = new SimComponent()
            {
                InstanceType = SimInstanceType.SimNetworkBlock,
            };
            var block = new SimNetworkBlock("BLOCK", new SimPoint(0, 0));

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimDoubleParameter("asdf", "", 0.0);

            parameter.InstancePropagationMode = SimParameterInstancePropagation.PropagateNever;


            component.Parameters.Add(parameter);
            var componentInstance = new SimComponentInstance(block);
            component.Instances.Add(componentInstance);

            AssertUtil.AssertDoubleEqual((double)componentInstance.InstanceParameterValuesPersistent[parameter], 0.0);
            AssertUtil.AssertDoubleEqual((double)componentInstance.InstanceParameterValuesTemporary[parameter], 0.0);
            parameter.ValueSource = ptr;
            Assert.AreEqual(5002.0, parameter.Value);
            AssertUtil.AssertDoubleEqual((double)componentInstance.InstanceParameterValuesPersistent[parameter], 0.0);
            AssertUtil.AssertDoubleEqual((double)componentInstance.InstanceParameterValuesTemporary[parameter], 0.0);

            parameter.InstancePropagationMode = SimParameterInstancePropagation.PropagateAlways;

            AssertUtil.AssertDoubleEqual((double)componentInstance.InstanceParameterValuesPersistent[parameter], 5002.0);

            data.table[1, 2] = 999.9;

            AssertUtil.AssertDoubleEqual((double)componentInstance.InstanceParameterValuesPersistent[parameter], 999.9);
            Assert.AreEqual(999.9, parameter.Value);
        }


        [TestMethod]
        public void ParameterBigTableValueChangedInstanceValueCheckInt()
        {
            var data = SimMultiValueBigTableTests.IntTestDataTable(3, 4);

            var component = new SimComponent()
            {
                InstanceType = SimInstanceType.SimNetworkBlock,
            };
            var block = new SimNetworkBlock("BLOCK", new SimPoint(0, 0));

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimIntegerParameter("asdf", "", 0);

            parameter.InstancePropagationMode = SimParameterInstancePropagation.PropagateNever;


            component.Parameters.Add(parameter);
            var componentInstance = new SimComponentInstance(block);
            component.Instances.Add(componentInstance);

            Assert.AreEqual((int)componentInstance.InstanceParameterValuesPersistent[parameter], 0);
            Assert.AreEqual((int)componentInstance.InstanceParameterValuesTemporary[parameter], 0);
            parameter.ValueSource = ptr;
            Assert.AreEqual(5002, parameter.Value);
            Assert.AreEqual((int)componentInstance.InstanceParameterValuesPersistent[parameter], 0);
            Assert.AreEqual((int)componentInstance.InstanceParameterValuesTemporary[parameter], 0);

            parameter.InstancePropagationMode = SimParameterInstancePropagation.PropagateAlways;

            Assert.AreEqual((int)componentInstance.InstanceParameterValuesPersistent[parameter], 5002);

            data.table[1, 2] = 999;

            Assert.AreEqual((int)componentInstance.InstanceParameterValuesPersistent[parameter], 999);
            Assert.AreEqual(999, parameter.Value);
        }




        [TestMethod]
        public void ParameterBigTableValueChangedInstanceValueCheckString()
        {
            var data = SimMultiValueBigTableTests.StringTestDataTable(3, 4);

            var component = new SimComponent()
            {
                InstanceType = SimInstanceType.SimNetworkBlock,
            };
            var block = new SimNetworkBlock("BLOCK", new SimPoint(0, 0));

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimStringParameter("asdf", "ASD");

            parameter.InstancePropagationMode = SimParameterInstancePropagation.PropagateNever;


            component.Parameters.Add(parameter);
            var componentInstance = new SimComponentInstance(block);
            component.Instances.Add(componentInstance);

            Assert.AreEqual((string)componentInstance.InstanceParameterValuesPersistent[parameter], "ASD");
            Assert.AreEqual((string)componentInstance.InstanceParameterValuesTemporary[parameter], "ASD");
            parameter.ValueSource = ptr;
            Assert.AreEqual("ASD2", parameter.Value);
            Assert.AreEqual((string)componentInstance.InstanceParameterValuesPersistent[parameter], "ASD");
            Assert.AreEqual((string)componentInstance.InstanceParameterValuesTemporary[parameter], "ASD");

            parameter.InstancePropagationMode = SimParameterInstancePropagation.PropagateAlways;

            Assert.AreEqual((string)componentInstance.InstanceParameterValuesPersistent[parameter], "ASD2");

            data.table[1, 2] = "ASD2++";

            Assert.AreEqual((string)componentInstance.InstanceParameterValuesPersistent[parameter], "ASD2++");
            Assert.AreEqual("ASD2++", parameter.Value);
        }




        [TestMethod]
        public void ParameterBigTableValueChangedInstanceValueCheckBool()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var component = new SimComponent()
            {
                InstanceType = SimInstanceType.SimNetworkBlock,
            };
            var block = new SimNetworkBlock("BLOCK", new SimPoint(0, 0));

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimBoolParameter("asdf", false);

            parameter.InstancePropagationMode = SimParameterInstancePropagation.PropagateNever;


            component.Parameters.Add(parameter);
            var componentInstance = new SimComponentInstance(block);
            component.Instances.Add(componentInstance);

            Assert.AreEqual((bool)componentInstance.InstanceParameterValuesPersistent[parameter], false);
            Assert.AreEqual((bool)componentInstance.InstanceParameterValuesTemporary[parameter], false);
            parameter.ValueSource = ptr;
            Assert.AreEqual(true, parameter.Value);
            Assert.AreEqual((bool)componentInstance.InstanceParameterValuesPersistent[parameter], false);
            Assert.AreEqual((bool)componentInstance.InstanceParameterValuesTemporary[parameter], false);

            parameter.InstancePropagationMode = SimParameterInstancePropagation.PropagateAlways;

            Assert.AreEqual((bool)componentInstance.InstanceParameterValuesPersistent[parameter], true);

            data.table[1, 2] = false;

            Assert.AreEqual((bool)componentInstance.InstanceParameterValuesPersistent[parameter], false);
            Assert.AreEqual(false, parameter.Value);
        }






        [TestMethod]
        public void DoubleParameterValueChangedTest()
        {
            var data = SimMultiValueBigTableTests.DoubleTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimDoubleParameter("asdf", "", 0.0);
            parameter.ValueSource = ptr;

            Assert.AreEqual(5002.0, parameter.Value);

            data.table[1, 2] = 999.9;

            Assert.AreEqual(999.9, parameter.Value);
        }
        [TestMethod]
        public void IntParameterValueChangedTest()
        {
            var data = SimMultiValueBigTableTests.IntTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimIntegerParameter("asdf", "", 0);
            parameter.ValueSource = ptr;

            Assert.AreEqual(5002, parameter.Value);

            data.table[1, 2] = 999;

            Assert.AreEqual(999, parameter.Value);
        }
        [TestMethod]
        public void StringParameterValueChangedTest()
        {
            var data = SimMultiValueBigTableTests.StringTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimStringParameter("asdf", "", 0.0);
            parameter.ValueSource = ptr;

            Assert.AreEqual("ASD2", parameter.Value);

            data.table[1, 2] = "ASD++";

            Assert.AreEqual("ASD++", parameter.Value);
        }
        [TestMethod]
        public void BoolParameterValueChangedTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimBoolParameter("asdf", true);
            parameter.ValueSource = ptr;

            Assert.AreEqual(true, parameter.Value);

            data.table[1, 2] = false;

            Assert.AreEqual(false, parameter.Value);
        }

        [TestMethod]
        public void BigTableRemovedTest()
        {
            var data = SimMultiValueBigTableTests.DoubleTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var parameter = new SimDoubleParameter("asdf", "", 0.0);
            parameter.ValueSource = ptr;

            Assert.AreEqual(ptr, parameter.ValueSource);

            data.projectData.ValueManager.Remove(ptr.ValueField);

            Assert.AreEqual(null, parameter.ValueSource);
        }

        [TestMethod]
        public void Field3DRemovedTest()
        {
            var data = SimMultiValueField3DTests.TestDataTable(3, 4, 2);

            var ptr = new SimMultiValueField3DParameterSource(data.table, 1, 2, 0);
            var parameter = new SimDoubleParameter("asdf", "", 0.0);
            parameter.ValueSource = ptr;

            Assert.AreEqual(ptr, parameter.ValueSource);

            data.projectData.ValueManager.Remove(ptr.ValueField);

            Assert.AreEqual(null, parameter.ValueSource);
        }

        [TestMethod]
        public void FunctionRemovedTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);

            var ptr = new SimMultiValueFunctionParameterSource(data.function, "graph_0_0", 1.5, 0.5);
            var parameter = new SimDoubleParameter("asdf", "", 0.0);
            parameter.ValueSource = ptr;

            Assert.AreEqual(ptr, parameter.ValueSource);

            data.projectData.ValueManager.Remove(ptr.ValueField);

            Assert.AreEqual(null, parameter.ValueSource);
        }
    }
}
