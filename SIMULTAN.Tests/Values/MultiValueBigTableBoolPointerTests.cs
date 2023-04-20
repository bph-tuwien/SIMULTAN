using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Values
{
    /// <summary>
    /// <see cref="SimBoolParameter"/>
    /// </summary>
    [TestClass]
    public class MultiValueBigTableBoolPointerTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\BigTableTestsProject.simultan");

        private void CheckPointer(SimMultiValueBigTableParameterSource ptr, SimMultiValueBigTable table, int row, int column)
        {
            Assert.AreEqual(table, ptr.ValueField);
            Assert.AreEqual(row, ptr.Row);
            Assert.AreEqual(column, ptr.Column);
        }

        #region Tests without Parameters

        [TestMethod]
        public void Ctor()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            Assert.ThrowsException<ArgumentNullException>(() => { new SimMultiValueBigTableParameterSource(null, 0, 0); });

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            CheckPointer(ptr, data.table, 1, 2);

            //Invalid location
            ptr = new SimMultiValueBigTableParameterSource(data.table, 99, 2);
            CheckPointer(ptr, data.table, -1, -1);

            ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 99);
            CheckPointer(ptr, data.table, -1, -1);

            ptr = new SimMultiValueBigTableParameterSource(data.table, -99, 2);
            CheckPointer(ptr, data.table, -1, -1);

            ptr = new SimMultiValueBigTableParameterSource(data.table, 1, -99);
            CheckPointer(ptr, data.table, -1, -1);
        }

        [TestMethod]
        public void CloneTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);

            var ptr2 = ptr.Clone();
            Assert.IsTrue(ptr2 is SimMultiValueBigTableParameterSource);
            CheckPointer((SimMultiValueBigTableParameterSource)ptr2, data.table, 1, 2);
        }

        [TestMethod]
        public void GetValueTests()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            Assert.AreEqual(true, (bool)ptr.GetValue());

            ptr = new SimMultiValueBigTableParameterSource(data.table, -1, -1);
            Assert.IsTrue(ptr.GetValue() == null);
        }

        [TestMethod]
        public void IsSamePointerTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            var ptr2 = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            Assert.IsTrue(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueBigTableParameterSource(data.table, 2, 2);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueBigTableParameterSource(data.table, 1, 1);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            var data2 = SimMultiValueBigTableTests.BoolTestDataTable(4, 3);
            ptr2 = new SimMultiValueBigTableParameterSource(data2.table, 1, 2);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            //Test other pointer types
        }

        private WeakReference MemoryLeakTest_Action(SimMultiValueBigTable table)
        {
            return new WeakReference(new SimMultiValueBigTableParameterSource(table, 1, 2));
        }
        private void MemoryLeakTest_Action2(WeakReference ptrRef)
        {
            ((SimMultiValueBigTableParameterSource)ptrRef.Target).Dispose();
        }
        [TestMethod]
        public void MemoryLeakTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var ptrRef = MemoryLeakTest_Action(data.table);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(ptrRef.IsAlive);

            MemoryLeakTest_Action2(ptrRef);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        [TestMethod]
        public void DefaultPointerTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);
            var ptr = data.table.DefaultPointer;

            CheckPointer((SimMultiValueBigTableParameterSource)ptr, data.table, 0, 0);
        }

        [TestMethod]
        public void CreateNewPointerTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var ptr = data.table.CreateNewPointer();
            CheckPointer((SimMultiValueBigTableParameterSource)ptr, data.table, 0, 0);

            ptr = data.table.CreateNewPointer(null);
            CheckPointer((SimMultiValueBigTableParameterSource)ptr, data.table, 0, 0);

            var sourcePtr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            ptr = data.table.CreateNewPointer(sourcePtr);
            CheckPointer((SimMultiValueBigTableParameterSource)ptr, data.table, 1, 2);
        }

        [TestMethod]
        public void DataChangedTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            Assert.AreEqual(true, (bool)ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.table[1, 2] = false;

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(false, (bool)ptr.GetValue());
        }

        [TestMethod]
        public void DataReplacedTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            Assert.AreEqual(true, (bool)ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.data.rowHeaders.RemoveAt(0);
            data.data.values.RemoveAt(0);

            data.table.ReplaceData(data.data.columnHeaders, data.data.rowHeaders, data.data.values);

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(true, (bool)ptr.GetValue());
        }

        [TestMethod]
        public void ColumnsChangedTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(3, 5);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 1, 2);
            Assert.AreEqual(true, (bool)ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.table.ColumnHeaders.RemoveAt(4);
            CheckPointer(ptr, data.table, 1, 2);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(true, (bool)ptr.GetValue());

            data.table.ColumnHeaders.RemoveAt(0);
            CheckPointer(ptr, data.table, 1, 1);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(true, (bool)ptr.GetValue());
        }

        [TestMethod]
        public void RowsChangedTest()
        {
            var data = SimMultiValueBigTableTests.BoolTestDataTable(5, 3);

            var ptr = new SimMultiValueBigTableParameterSource(data.table, 2, 1);
            Assert.AreEqual(true, (bool)ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.table.RowHeaders.RemoveAt(4);
            CheckPointer(ptr, data.table, 2, 1);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(true, (bool)ptr.GetValue());

            data.table.RowHeaders.RemoveAt(0);
            CheckPointer(ptr, data.table, 1, 1);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(true, (bool)ptr.GetValue());
        }

        #endregion

        #region Test with Parameters (and pointer parameters)

        [TestMethod]
        public void PointerParameterTest()
        {
            LoadProject(testProject);

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer_Bool");
            var param = comp.Parameters.OfType<SimBoolParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool");

            var ptr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            CheckPointer(ptr, ptr.Table, 1, 1);
            Assert.AreEqual(param, ptr.TargetParameter);
            Assert.AreEqual(true, (bool)ptr.GetValue());
        }

        [TestMethod]
        public void PointerParameterChangedTest()
        {
            LoadProject(testProject);

            int eventCounter = 0;

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer_Bool");
            var param = comp.Parameters.OfType<SimBoolParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool");
            var colParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool.ValuePointer.OffsetColumn");
            var rowParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool.ValuePointer.OffsetRow");

            var ptr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            ptr.ValueChanged += (s, e) => eventCounter++;
            Assert.AreEqual(true, (bool)ptr.GetValue());

            colParam.Value = -1;
            Assert.AreEqual(1, eventCounter);
            Assert.AreEqual(true, (bool)ptr.GetValue());

            rowParam.Value = 1;
            Assert.AreEqual(2, eventCounter);
            Assert.AreEqual(true, (bool)ptr.GetValue());
        }

        [TestMethod]
        public void RemovePointerParameterTest()
        {
            LoadProject(testProject);

            int eventCounter = 0;

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer_Bool");
            var param = comp.Parameters.OfType<SimBoolParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool");
            var colParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool.ValuePointer.OffsetColumn");
            var rowParam = comp.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool.ValuePointer.OffsetRow");

            var ptr = (SimMultiValueBigTableParameterSource)param.ValueSource;
            ptr.ValueChanged += (s, e) => eventCounter++;

            comp.Parameters.Remove(colParam);
            Assert.AreEqual(1, eventCounter);
            Assert.AreEqual(false, (bool)ptr.GetValue());

            comp.Parameters.Remove(rowParam);
            Assert.AreEqual(2, eventCounter);
            Assert.AreEqual(false, (bool)ptr.GetValue());

            //Make sure no further updates are called
            colParam.Value = 0;
            Assert.AreEqual(2, eventCounter);
        }

        [TestMethod]
        public void AddPointerParameterTest()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "NoParameters");
            var param = comp.Parameters.OfType<SimBoolParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool");

            ((SimMultiValueParameterSource)param.ValueSource).CreateValuePointerParameters(projectData.UsersManager.Users.First());

            var colParam = comp.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(x => x.NameTaxonomyEntry.Name == "Target_Bool.ValuePointer.OffsetColumn");
            var rowParam = comp.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(x => x.NameTaxonomyEntry.Name == "Target_Bool.ValuePointer.OffsetRow");

            Assert.IsNotNull(colParam);
            Assert.IsNotNull(rowParam);

            colParam.Value = -1;
            Assert.AreEqual(true, param.Value);

            rowParam.Value = -1;
            Assert.AreEqual(true, param.Value);
        }

        private WeakReference MemoryLeakRemoveFromParameterTest_Action()
        {
            var comp = projectData.Components.First(x => x.Name == "WithPointer_Bool");
            var param = comp.Parameters.OfType<SimBoolParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool");

            WeakReference ptrRef = new WeakReference(param.ValueSource);
            param.ValueSource = null;

            return ptrRef;
        }
        [TestMethod]
        public void MemoryLeakRemoveFromParameterTest()
        {
            LoadProject(testProject);

            var ptrRef = MemoryLeakRemoveFromParameterTest_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        private WeakReference MemoryLeakRemoveParameterTest_Action()
        {
            var comp = projectData.Components.First(x => x.Name == "WithPointer_Bool");
            var param = comp.Parameters.OfType<SimBoolParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool");

            WeakReference ptrRef = new WeakReference(param.ValueSource);

            comp.Parameters.Remove(param);
            return ptrRef;
        }

        [TestMethod]
        public void MemoryLeakRemoveParameterTest()
        {
            LoadProject(testProject);

            var ptrRef = MemoryLeakRemoveParameterTest_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        [TestMethod]
        public void ValueChangedWithOffsetParameters()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "WithPointer_Bool");
            var param = comp.Parameters.OfType<SimBoolParameter>().First(x => x.NameTaxonomyEntry.Name == "Target_Bool");

            var table = ((SimMultiValueBigTableParameterSource)param.ValueSource).Table;
            table[3, 2] = false;

            Assert.AreEqual(false, param.Value);
        }

        #endregion

        //IsSamePointer: Test other pointer type
    }
}
