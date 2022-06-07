using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Values
{
    [TestClass]
    public class MultiValueFunctionPointerTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\FunctionTestsProject.simultan");

        public void CheckPointer(SimMultiValueFunction.MultiValueFunctionPointer ptr, SimMultiValueFunction func, string graph, double x, double y)
        {
            Assert.AreEqual(func, ptr.ValueField);
            Assert.AreEqual(graph, ptr.GraphName);
            AssertUtil.AssertDoubleEqual(x, ptr.AxisValueX);
            AssertUtil.AssertDoubleEqual(y, ptr.AxisValueY);
        }

        #region Tests without Parameter

        [TestMethod]
        public void Ctor()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);

            Assert.ThrowsException<ArgumentNullException>(() => { new SimMultiValueFunction.MultiValueFunctionPointer(null, "graph_0_0", 0.0, 0.0); });

            var ptr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.5);
            CheckPointer(ptr, data.function, "graph_0_0", 1.5, 0.5);

            //Wrong graph name
            ptr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "asdf", 0, 0);
            CheckPointer(ptr, data.function, null, double.NaN, double.NaN);
        }

        [TestMethod]
        public void CloneTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);

            var ptr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.5);

            var ptr2 = ptr.Clone();
            Assert.IsTrue(ptr2 is SimMultiValueFunction.MultiValueFunctionPointer);
            CheckPointer((SimMultiValueFunction.MultiValueFunctionPointer)ptr2, data.function, "graph_0_0", 1.5, 0.5);
        }

        [TestMethod]
        public void GetValueTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);
            var ptr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.0);

            AssertUtil.AssertDoubleEqual(0.5, ptr.GetValue());
        }

        [TestMethod]
        public void IsSamePointerTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);

            var ptr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.0);
            var ptr2 = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.0);
            Assert.IsTrue(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_1", 1.5, 0.0);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.4, 0.0);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.4, 1.0);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            var data2 = SimMultiValueFunctionTests.TestDataFunction(2);
            ptr2 = new SimMultiValueFunction.MultiValueFunctionPointer(data2.function, "graph_0_0", 1.5, 0.0);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            //Test other pointer types
        }

        [TestMethod]
        public void MemoryLeakTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);

            var ptrRef = new WeakReference(new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.0));

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(ptrRef.IsAlive);

            ((SimMultiValueFunction.MultiValueFunctionPointer)ptrRef.Target).Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        [TestMethod]
        public void DefaultPointerTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);
            var ptr = data.function.DefaultPointer;

            CheckPointer((SimMultiValueFunction.MultiValueFunctionPointer)ptr, data.function, null, double.NaN, double.NaN);
        }

        [TestMethod]
        public void CreateNewPointerTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);
            var ptr = data.function.CreateNewPointer();

            CheckPointer((SimMultiValueFunction.MultiValueFunctionPointer)ptr, data.function, null, double.NaN, double.NaN);

            ptr = data.function.CreateNewPointer(null);
            CheckPointer((SimMultiValueFunction.MultiValueFunctionPointer)ptr, data.function, null, double.NaN, double.NaN);

            var sourcePtr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1, 2);
            ptr = data.function.CreateNewPointer(sourcePtr);
            CheckPointer((SimMultiValueFunction.MultiValueFunctionPointer)ptr, data.function, "graph_0_0", 1, 2);
        }

        [TestMethod]
        public void DataChangedTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);
            var ptr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.0);

            Assert.AreEqual(0.5, ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.function.Graphs.First(x => x.Name == "graph_0_0").Points[1] = new Point3D(1.0, 2.0, 0.0);

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(1.5, ptr.GetValue());

            data.function.Graphs.First(x => x.Name == "graph_0_0").Points.Insert(2, new Point3D(2.0, 2.0, 0.0));
            Assert.AreEqual(2, eventCount);
            Assert.AreEqual(2.0, ptr.GetValue());
        }

        [TestMethod]
        public void GraphRemovedTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);
            var ptr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.0);

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.function.Graphs.Remove(data.function.Graphs.First(x => x.Name == "graph_0_0"));

            Assert.AreEqual(1, eventCount);
            CheckPointer(ptr, data.function, null, double.NaN, double.NaN);
        }

        [TestMethod]
        public void GraphRenamedTest()
        {
            var data = SimMultiValueFunctionTests.TestDataFunction(2);
            var ptr = new SimMultiValueFunction.MultiValueFunctionPointer(data.function, "graph_0_0", 1.5, 0.0);

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.function.Graphs.First(x => x.Name == "graph_0_0").Name = "demograph";

            Assert.AreEqual(1, eventCount);
            CheckPointer(ptr, data.function, "demograph", 1.5, 0.0);
        }

        #endregion

        #region Tests with Parameter

        [TestMethod]
        public void PointerParameterTest()
        {
            LoadProject(testProject);

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.Name == "Target");

            var ptr = (SimMultiValueFunction.MultiValueFunctionPointer)param.MultiValuePointer;
            CheckPointer(ptr, (SimMultiValueFunction)ptr.ValueField, "graph1", 0.5, 1.0);
            Assert.AreEqual(param, ptr.TargetParameter);
            Assert.AreEqual(1.5, ptr.GetValue());
        }

        [TestMethod]
        public void PointerParameterChangedTest()
        {
            LoadProject(testProject);

            int eventCounter = 0;

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.Name == "Target");
            var xParam = comp.Parameters.First(x => x.Name == "Target.ValuePointer.OffsetX");

            var ptr = (SimMultiValueFunction.MultiValueFunctionPointer)param.MultiValuePointer;
            ptr.ValueChanged += (s, e) => eventCounter++;
            Assert.AreEqual(1.5, ptr.GetValue());

            xParam.ValueCurrent = 0.5;
            Assert.AreEqual(1, eventCounter);
            Assert.AreEqual(2.0, ptr.GetValue());
        }

        [TestMethod]
        public void RemovePointerParameterTest()
        {
            LoadProject(testProject);

            int eventCounter = 0;

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.Name == "Target");
            var xParam = comp.Parameters.First(x => x.Name == "Target.ValuePointer.OffsetX");

            var ptr = (SimMultiValueFunction.MultiValueFunctionPointer)param.MultiValuePointer;
            ptr.ValueChanged += (s, e) => eventCounter++;

            comp.Parameters.Remove(xParam);
            Assert.AreEqual(1, eventCounter);
            AssertUtil.AssertDoubleEqual(1.0, ptr.GetValue());

            //Make sure no further updates are called
            xParam.ValueCurrent = 0;
            Assert.AreEqual(1, eventCounter);
        }

        [TestMethod]
        public void AddPointerParameterTest()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "WithoutParameter");
            var param = comp.Parameters.First(x => x.Name == "Target");

            param.MultiValuePointer.CreateValuePointerParameters(projectData.UsersManager.Users.First());

            var xParam = comp.Parameters.FirstOrDefault(x => x.Name == "Target.ValuePointer.OffsetX");

            Assert.IsNotNull(xParam);

            xParam.ValueCurrent = 1.0;
            Assert.AreEqual(1.5, param.ValueCurrent);
        }

        [TestMethod]
        public void MemoryLeakRemoveFromParameterTest()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.Name == "Target");

            WeakReference ptrRef = new WeakReference(param.MultiValuePointer);
            param.MultiValuePointer = null;
            param = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        [TestMethod]
        public void MemoryLeakRemoveParameterTest()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.Name == "Target");

            WeakReference ptrRef = new WeakReference(param.MultiValuePointer);

            comp.Parameters.Remove(param);
            param = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        [TestMethod]
        public void ValueChangedWithOffsetParameters()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.Name == "Target");

            var function = (SimMultiValueFunction)param.MultiValuePointer.ValueField;
            function.Graphs.First(x => x.Name == "graph1").Points[1] = new Point3D(1.0, 2.0, 0.0);

            Assert.AreEqual(1.5, param.ValueCurrent);
        }

        #endregion
    }
}
