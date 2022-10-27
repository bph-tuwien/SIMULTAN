using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Tests.Utils;
using System;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Values
{
    [TestClass]
    public class MultiValueBigTablePointerTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\BigTableTestsProject.simultan");

        private void CheckPointer(SimMultiValueBigTable.SimMultiValueBigTablePointer ptr, SimMultiValueBigTable table, int row, int column)
        {
            Assert.AreEqual(table, ptr.ValueField);
            Assert.AreEqual(row, ptr.Row);
            Assert.AreEqual(column, ptr.Column);
        }

        #region Tests without Parameters

        [TestMethod]
        public void Ctor()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);

            Assert.ThrowsException<ArgumentNullException>(() => { new SimMultiValueBigTable.SimMultiValueBigTablePointer(null, 0, 0); });

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);
            CheckPointer(ptr, data.table, 1, 2);

            //Invalid location
            ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 99, 2);
            CheckPointer(ptr, data.table, -1, -1);

            ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 99);
            CheckPointer(ptr, data.table, -1, -1);

            ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, -99, 2);
            CheckPointer(ptr, data.table, -1, -1);

            ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, -99);
            CheckPointer(ptr, data.table, -1, -1);
        }

        [TestMethod]
        public void CloneTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);

            var ptr2 = ptr.Clone();
            Assert.IsTrue(ptr2 is SimMultiValueBigTable.SimMultiValueBigTablePointer);
            CheckPointer((SimMultiValueBigTable.SimMultiValueBigTablePointer)ptr2, data.table, 1, 2);
        }

        [TestMethod]
        public void GetValueTests()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);
            Assert.AreEqual(5002.0, ptr.GetValue());

            ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, -1, -1);
            Assert.IsTrue(double.IsNaN(ptr.GetValue()));
        }

        [TestMethod]
        public void IsSamePointerTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);
            var ptr2 = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);
            Assert.IsTrue(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 2, 2);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            ptr2 = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 1);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            var data2 = SimMultiValueBigTableTests.TestDataTable(4, 3);
            ptr2 = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data2.table, 1, 2);
            Assert.IsFalse(ptr.IsSamePointer(ptr2));

            //Test other pointer types
        }

        [TestMethod]
        public void MemoryLeakTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);

            var ptrRef = new WeakReference(new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2));

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsTrue(ptrRef.IsAlive);

            ((SimMultiValueBigTable.SimMultiValueBigTablePointer)ptrRef.Target).Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ptrRef.IsAlive);
        }

        [TestMethod]
        public void DefaultPointerTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);
            var ptr = data.table.DefaultPointer;

            CheckPointer((SimMultiValueBigTable.SimMultiValueBigTablePointer)ptr, data.table, 0, 0);
        }

        [TestMethod]
        public void CreateNewPointerTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);

            var ptr = data.table.CreateNewPointer();
            CheckPointer((SimMultiValueBigTable.SimMultiValueBigTablePointer)ptr, data.table, 0, 0);

            ptr = data.table.CreateNewPointer(null);
            CheckPointer((SimMultiValueBigTable.SimMultiValueBigTablePointer)ptr, data.table, 0, 0);

            var sourcePtr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);
            ptr = data.table.CreateNewPointer(sourcePtr);
            CheckPointer((SimMultiValueBigTable.SimMultiValueBigTablePointer)ptr, data.table, 1, 2);
        }

        [TestMethod]
        public void DataChangedTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);
            Assert.AreEqual(5002.0, ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.table[1, 2] = 999.9;

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(999.9, ptr.GetValue());
        }

        [TestMethod]
        public void DataReplacedTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);
            Assert.AreEqual(5002.0, ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.data.rowHeaders.RemoveAt(0);
            data.data.values.RemoveAt(0);

            data.table.ReplaceData(data.data.columnHeaders, data.data.rowHeaders, data.data.values);

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(10002.0, ptr.GetValue());
        }

        [TestMethod]
        public void ColumnsChangedTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 5);

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 1, 2);
            Assert.AreEqual(5002.0, ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.table.ColumnHeaders.RemoveAt(4);
            CheckPointer(ptr, data.table, 1, 2);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(5002.0, ptr.GetValue());

            data.table.ColumnHeaders.RemoveAt(0);
            CheckPointer(ptr, data.table, 1, 1);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(5002.0, ptr.GetValue());
        }

        [TestMethod]
        public void RowsChangedTest()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(5, 3);

            var ptr = new SimMultiValueBigTable.SimMultiValueBigTablePointer(data.table, 2, 1);
            Assert.AreEqual(10001.0, ptr.GetValue());

            int eventCount = 0;
            ptr.ValueChanged += (o, e) => eventCount++;

            data.table.RowHeaders.RemoveAt(4);
            CheckPointer(ptr, data.table, 2, 1);
            Assert.AreEqual(0, eventCount);
            Assert.AreEqual(10001.0, ptr.GetValue());

            data.table.RowHeaders.RemoveAt(0);
            CheckPointer(ptr, data.table, 1, 1);
            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(10001.0, ptr.GetValue());
        }

        #endregion

        #region Test with Parameters (and pointer parameters)

        [TestMethod]
        public void PointerParameterTest()
        {
            LoadProject(testProject);

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target");

            var ptr = (SimMultiValueBigTable.SimMultiValueBigTablePointer)param.MultiValuePointer;
            CheckPointer(ptr, (SimMultiValueBigTable)ptr.ValueField, 1, 1);
            Assert.AreEqual(param, ptr.TargetParameter);
            Assert.AreEqual(20003.0, ptr.GetValue());
        }

        [TestMethod]
        public void PointerParameterChangedTest()
        {
            LoadProject(testProject);

            int eventCounter = 0;

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target");
            var colParam = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target.ValuePointer.OffsetColumn");
            var rowParam = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target.ValuePointer.OffsetRow");

            var ptr = (SimMultiValueBigTable.SimMultiValueBigTablePointer)param.MultiValuePointer;
            ptr.ValueChanged += (s, e) => eventCounter++;
            Assert.AreEqual(20003.0, ptr.GetValue());

            colParam.ValueCurrent = -1;
            Assert.AreEqual(1, eventCounter);
            Assert.AreEqual(20001.0, ptr.GetValue());

            rowParam.ValueCurrent = 1;
            Assert.AreEqual(2, eventCounter);
            Assert.AreEqual(15001.0, ptr.GetValue());
        }

        [TestMethod]
        public void RemovePointerParameterTest()
        {
            LoadProject(testProject);

            int eventCounter = 0;

            //Find ptr
            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target");
            var colParam = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target.ValuePointer.OffsetColumn");
            var rowParam = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target.ValuePointer.OffsetRow");

            var ptr = (SimMultiValueBigTable.SimMultiValueBigTablePointer)param.MultiValuePointer;
            ptr.ValueChanged += (s, e) => eventCounter++;

            comp.Parameters.Remove(colParam);
            Assert.AreEqual(1, eventCounter);
            Assert.AreEqual(20002, ptr.GetValue());

            comp.Parameters.Remove(rowParam);
            Assert.AreEqual(2, eventCounter);
            Assert.AreEqual(10002, ptr.GetValue());

            //Make sure no further updates are called
            colParam.ValueCurrent = 0;
            Assert.AreEqual(2, eventCounter);
        }

        [TestMethod]
        public void AddPointerParameterTest()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "NoParameters");
            var param = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target");

            param.MultiValuePointer.CreateValuePointerParameters(projectData.UsersManager.Users.First());

            var colParam = comp.Parameters.FirstOrDefault(x => x.TaxonomyEntry.Name == "Target.ValuePointer.OffsetColumn");
            var rowParam = comp.Parameters.FirstOrDefault(x => x.TaxonomyEntry.Name == "Target.ValuePointer.OffsetRow");

            Assert.IsNotNull(colParam);
            Assert.IsNotNull(rowParam);

            colParam.ValueCurrent = -1;
            Assert.AreEqual(10001, param.ValueCurrent);

            rowParam.ValueCurrent = -1;
            Assert.AreEqual(5001, param.ValueCurrent);
        }

        [TestMethod]
        public void MemoryLeakRemoveFromParameterTest()
        {
            LoadProject(testProject);

            var comp = projectData.Components.First(x => x.Name == "WithPointer");
            var param = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target");

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
            var param = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target");

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
            var param = comp.Parameters.First(x => x.TaxonomyEntry.Name == "Target");

            var table = (SimMultiValueBigTable)param.MultiValuePointer.ValueField;
            table[3, 2] = 9977;

            Assert.AreEqual(9977, param.ValueCurrent);
        }

        #endregion

        //IsSamePointer: Test other pointer type
    }
}
