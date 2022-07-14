using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Tests.Utils;
using SIMULTAN.Utils;
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
    public class SimMultiValueCollectionTests : BaseProjectTest
    {
        private static readonly FileInfo bigTableProject = new FileInfo(@".\BigTableTestsProject.simultan");
        private static readonly FileInfo bigTableProjectImport = new FileInfo(@".\BigTableTestsProject_Import.simultan");

        private static readonly FileInfo functionProject = new FileInfo(@".\FunctionTestsProject.simultan");
        private static readonly FileInfo field3DProject = new FileInfo(@".\Field3DTestsProject.simultan");


        #region General

        [TestMethod]
        public void LoadedIds()
        {
            LoadProject(bigTableProject);

            foreach (var mv in projectData.ValueManager)
            {
                Assert.AreEqual(projectData.ValueManager, mv.Factory);
                Assert.AreEqual(project, mv.Id.Location);
                Assert.AreEqual(project.GlobalID, mv.Id.GlobalId);
                Assert.AreNotEqual(0, mv.Id.LocalId);
                Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != mv.LocalID || x == mv));
            }
        }

        [TestMethod]
        public void Clear()
        {
            LoadProject(bigTableProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var oldItems = projectData.ValueManager.ToList();
            projectData.ValueManager.Clear();

            Assert.AreEqual(0, projectData.ValueManager.Count);

            foreach (var item in oldItems)
            {
                Assert.AreEqual(SimId.Empty, item.Id);
                Assert.AreEqual(null, item.Factory);
            }

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            eventCounter.AssertEventCount(1);

        }

        [TestMethod]
        public void ResetChanges()
        {
            LoadProject(bigTableProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            eventCounter.AssertEventCount(0);

            projectData.ValueManager.Clear();

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            eventCounter.AssertEventCount(1);

            projectData.ValueManager.ResetChanges();

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            eventCounter.AssertEventCount(2);
        }

        [TestMethod]
        public void Merge()
        {
            LoadProject(bigTableProject);
            (var projectImport, var projectDataImport, _) = ProjectUtils.LoadTestData(bigTableProjectImport);

            var originalMVs = projectData.ValueManager.ToList();
            var originalImportMVs = projectDataImport.ValueManager.ToList();
            var originalIds = projectDataImport.ValueManager.Select(x => (x, x.Id.LocalId)).ToList();

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager.Merge(null); });


            var idMapping = projectData.ValueManager.Merge(projectDataImport.ValueManager);


            Assert.AreEqual(originalMVs.Count + originalImportMVs.Count, projectData.ValueManager.Count);
            Assert.AreEqual(0, projectDataImport.ValueManager.Count);

            foreach (var mv in originalMVs)
            {
                Assert.IsTrue(projectData.ValueManager.Contains(mv));
                Assert.AreEqual(project.GlobalID, mv.Id.GlobalId);
                Assert.AreEqual(project, mv.Id.Location);
            }
            foreach (var mv in originalImportMVs)
            {
                Assert.IsTrue(projectData.ValueManager.Contains(mv));
                Assert.AreEqual(project.GlobalID, mv.Id.GlobalId);
                Assert.AreEqual(project, mv.Id.Location);
            }

            //Id check
            Assert.AreEqual(originalImportMVs.Count, idMapping.Count);
            foreach (var old in originalIds)
            {
                Assert.AreEqual(idMapping[old.LocalId], old.x.Id.LocalId);
            }

            ProjectUtils.CleanupTestData(ref projectImport, ref projectDataImport);
        }

        [TestMethod]
        public void RemoveUnused()
        {
            LoadProject(bigTableProject);

            var count = projectData.ValueManager.Count;
            Assert.AreEqual(2, projectData.ValueManager.Count(x => x.Name.StartsWith("unused_table")));

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager.RemoveUnused(null, null); });

            projectData.ValueManager.RemoveUnused(projectData.Components, null);

            Assert.AreEqual(count - 2, projectData.ValueManager.Count);
            Assert.AreEqual(0, projectData.ValueManager.Count(x => x.Name.StartsWith("unused_table")));

        }

        [TestMethod]
        public void RemoveUnusedWithExclude()
        {
            LoadProject(bigTableProject);

            var count = projectData.ValueManager.Count;
            Assert.AreEqual(2, projectData.ValueManager.Count(x => x.Name.StartsWith("unused_table")));

            projectData.ValueManager.RemoveUnused(projectData.Components, projectData.ValueManager.Where(x => x.Name == "unused_table2"));

            Assert.AreEqual(count - 1, projectData.ValueManager.Count);
            Assert.AreEqual(0, projectData.ValueManager.Count(x => x.Name == "unused_table"));
            Assert.AreEqual(1, projectData.ValueManager.Count(x => x.Name == "unused_table2"));

        }

        [TestMethod]
        public void GetById()
        {
            LoadProject(bigTableProject);

            //Missmatching location
            Assert.AreEqual(null,
                projectData.ValueManager.GetByID(Guid.NewGuid(), projectData.ValueManager.First().LocalID)
                );

            //Matching location
            Assert.AreEqual(projectData.ValueManager.First(),
                projectData.ValueManager.GetByID(projectData.ValueManager.CalledFromLocation.GlobalID, projectData.ValueManager.First().LocalID)
                );

            //No location check
            Assert.AreEqual(projectData.ValueManager.First(),
                projectData.ValueManager.GetByID(Guid.Empty, projectData.ValueManager.First().LocalID)
                );
        }

        [TestMethod]
        public void SetLocationWithMethod()
        {
            LoadProject(bigTableProject);

            foreach (var mv in projectData.ValueManager)
                Assert.AreEqual(projectData.ValueManager.CalledFromLocation.GlobalID, mv.Id.GlobalId);

            //Working
            {
                var newId = Guid.NewGuid();
                var newLocation = new DummyReferenceLocation(newId);
                projectData.ValueManager.SetCallingLocation(newLocation);

                Assert.AreEqual(newLocation, projectData.ValueManager.CalledFromLocation);
                foreach (var mv in projectData.ValueManager)
                    Assert.AreEqual(newId, mv.Id.GlobalId);
            }

            //Working: Existing file
            {
                var newId = Guid.NewGuid();
                var newLocation = new DummyReferenceLocation(newId);
                projectData.ValueManager.SetCallingLocation(newLocation);

                Assert.AreEqual(newLocation, projectData.ValueManager.CalledFromLocation);
                foreach (var mv in projectData.ValueManager)
                    Assert.AreEqual(newId, mv.Id.GlobalId);
            }

            //Working: Existing folder
            {
                var newId = Guid.NewGuid();
                var newLocation = new DummyReferenceLocation(newId);
                projectData.ValueManager.SetCallingLocation(newLocation);

                Assert.AreEqual(newLocation, projectData.ValueManager.CalledFromLocation);
                foreach (var mv in projectData.ValueManager)
                    Assert.AreEqual(newId, mv.Id.GlobalId);
            }

            //Working: Null
            {
                projectData.ValueManager.SetCallingLocation(null);

                Assert.AreEqual(null, projectData.ValueManager.CalledFromLocation);
                foreach (var mv in projectData.ValueManager)
                    Assert.AreEqual(Guid.Empty, mv.Id.GlobalId);
            }
        }

        [TestMethod]
        public void NullHandling()
        {
            LoadProject(bigTableProject);

            Assert.ThrowsException<ArgumentNullException>(() => projectData.ValueManager.Add(null));
            Assert.ThrowsException<ArgumentNullException>(() => projectData.ValueManager.Insert(0, null));
            Assert.ThrowsException<ArgumentNullException>(() => projectData.ValueManager[0] = null);
        }

        #endregion

        #region BigTable

        [TestMethod]
        public void BigTableAdd()
        {
            DateTime startTime = DateTime.Now;

            LoadProject(bigTableProject);
            var testData = SimMultiValueBigTableTests.TestData(3, 4);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager.Add(null); });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            var tab1 = new SimMultiValueBigTable(testData.name, testData.unitColumn, testData.unitRow,
                testData.columnHeaders, testData.rowHeaders, testData.values);

            projectData.ValueManager.Add(tab1);
            Assert.AreEqual(tab1.Id.Location, project);
            Assert.AreEqual(tab1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(tab1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, tab1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != tab1.LocalID || x == tab1));

            Assert.IsTrue(projectData.ValueManager.Contains(tab1));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
            Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
            factoryEvents.AssertEventCount(1);

            Assert.ThrowsException<ArgumentException>(() => projectData.ValueManager.Add(tab1));
        }

        [TestMethod]
        public void BigTableInsert()
        {
            LoadProject(bigTableProject);
            var testData = SimMultiValueBigTableTests.TestData(3, 4);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager.Insert(0, null); });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            var tab1 = new SimMultiValueBigTable(testData.name, testData.unitColumn, testData.unitRow,
                testData.columnHeaders, testData.rowHeaders, testData.values);

            projectData.ValueManager.Insert(0, tab1);
            Assert.AreEqual(tab1.Id.Location, project);
            Assert.AreEqual(tab1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(tab1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, tab1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != tab1.LocalID || x == tab1));

            Assert.IsTrue(projectData.ValueManager.Contains(tab1));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(1);
        }

        [TestMethod]
        public void BigTableReplace()
        {
            LoadProject(bigTableProject);
            var testData = SimMultiValueBigTableTests.TestData(3, 4);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager[0] = null; });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            int replaceIdx = projectData.ValueManager.FindIndex(x => x.Name == "unused_table");
            var replaceTable = projectData.ValueManager[replaceIdx];
            var tab1 = new SimMultiValueBigTable(testData.name, testData.unitColumn, testData.unitRow,
                testData.columnHeaders, testData.rowHeaders, testData.values);

            projectData.ValueManager[replaceIdx] = tab1;
            Assert.AreEqual(tab1.Id.Location, project);
            Assert.AreEqual(tab1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(tab1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, tab1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != tab1.LocalID || x == tab1));

            Assert.AreEqual(null, replaceTable.Factory);
            Assert.AreEqual(SimId.Empty, replaceTable.Id);

            Assert.IsTrue(projectData.ValueManager.Contains(tab1));
            Assert.IsFalse(projectData.ValueManager.Contains(replaceTable));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(1);
        }

        [TestMethod]
        public void BigTableRemove()
        {
            LoadProject(bigTableProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var unusedTable = projectData.ValueManager.First(x => x.Name == "unused_table");
            projectData.ValueManager.Remove(unusedTable);

            Assert.IsFalse(projectData.ValueManager.Contains(unusedTable));
            Assert.AreEqual(SimId.Empty, unusedTable.Id);
            Assert.AreEqual(null, unusedTable.Factory);
            Assert.IsTrue(projectData.ValueManager.HasChanges);
            eventCounter.AssertEventCount(1);
        }

        [TestMethod]
        public void BigTableRemoveMemoryLeakTest()
        {
            LoadProject(bigTableProject);

            WeakReference unusedTableRef = new WeakReference(projectData.ValueManager.First(x => x.Name == "unused_table"));
            projectData.ValueManager.Remove((SimMultiValueBigTable)unusedTableRef.Target);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(unusedTableRef.IsAlive);
        }

        [TestMethod]
        public void BigTableHasChanges()
        {
            var startTime = DateTime.Now;
            LoadProject(bigTableProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var tab = (SimMultiValueBigTable)fac.FirstOrDefault(x => x is SimMultiValueBigTable);

            {
                tab.AdditionalInfo = "New String";
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.Description = "New String";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.UnitX = "New Unit";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.UnitY = "New Unit";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.UnitZ = "New Unit";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.Name = "New Name";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab[0, 0] = 99.9;
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        [TestMethod]
        public void BigTableColumnHeadersHasChanges()
        {
            LoadProject(bigTableProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var tab = (SimMultiValueBigTable)fac.FirstOrDefault(x => x is SimMultiValueBigTable);

            {
                tab.ColumnHeaders.RemoveAt(0);
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.ColumnHeaders.Add(new SimMultiValueBigTableHeader("asdf", "asdf"));
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.ColumnHeaders[0].Name = "name";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.ColumnHeaders[0].Name = "unit";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.ColumnHeaders.Clear();
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        [TestMethod]
        public void BigTableRowHeadersHasChanges()
        {
            LoadProject(bigTableProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var tab = (SimMultiValueBigTable)fac.FirstOrDefault(x => x is SimMultiValueBigTable);

            {
                tab.RowHeaders.RemoveAt(0);
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.RowHeaders.Add(new SimMultiValueBigTableHeader("asdf", "asdf"));
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.RowHeaders[0].Name = "name";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.RowHeaders[0].Name = "unit";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.RowHeaders.Clear();
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        #endregion

        #region Field3D

        [TestMethod]
        public void Field3DAdd()
        {
            DateTime startTime = DateTime.Now;

            LoadProject(field3DProject);
            var testData = SimMultiValueField3DTests.TestData(3, 4, 5);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager.Add(null); });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            var tab1 = new SimMultiValueField3D(testData.name, testData.xaxis, testData.unitX, testData.yaxis, testData.unitY, testData.zaxis, testData.unitZ,
                testData.data, false);
            projectData.ValueManager.Add(tab1);

            //projectData.ValueManager.Add(tab1);
            Assert.AreEqual(tab1.Id.Location, project);
            Assert.AreEqual(tab1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(tab1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, tab1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != tab1.LocalID || x == tab1));

            Assert.IsTrue(projectData.ValueManager.Contains(tab1));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
            Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
            factoryEvents.AssertEventCount(1);

            Assert.ThrowsException<ArgumentException>(() => projectData.ValueManager.Add(tab1));
        }

        [TestMethod]
        public void Field3DInsert()
        {
            LoadProject(field3DProject);
            var testData = SimMultiValueField3DTests.TestData(3, 4, 5);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager.Insert(0, null); });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            var tab1 = new SimMultiValueField3D(testData.name, testData.xaxis, testData.unitX,
                testData.yaxis, testData.unitY, testData.zaxis, testData.unitZ, testData.data, false);

            projectData.ValueManager.Insert(0, tab1);
            Assert.AreEqual(tab1.Id.Location, project);
            Assert.AreEqual(tab1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(tab1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, tab1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != tab1.LocalID || x == tab1));

            Assert.IsTrue(projectData.ValueManager.Contains(tab1));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(1);
        }

        [TestMethod]
        public void Field3DReplace()
        {
            LoadProject(field3DProject);
            var testData = SimMultiValueField3DTests.TestData(3, 4, 5);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager[0] = null; });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            int replaceIdx = projectData.ValueManager.FindIndex(x => x.Name == "unused_field");
            var replaceTable = projectData.ValueManager[replaceIdx];
            var tab1 = new SimMultiValueField3D(testData.name, testData.xaxis, testData.unitX,
                testData.yaxis, testData.unitY, testData.zaxis, testData.unitZ, testData.data, false);

            projectData.ValueManager[replaceIdx] = tab1;
            Assert.AreEqual(tab1.Id.Location, project);
            Assert.AreEqual(tab1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(tab1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, tab1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != tab1.LocalID || x == tab1));

            Assert.AreEqual(null, replaceTable.Factory);
            Assert.AreEqual(SimId.Empty, replaceTable.Id);

            Assert.IsTrue(projectData.ValueManager.Contains(tab1));
            Assert.IsFalse(projectData.ValueManager.Contains(replaceTable));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(1);
        }

        [TestMethod]
        public void Field3DRemove()
        {
            LoadProject(field3DProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var unusedTable = projectData.ValueManager.First(x => x.Name == "unused_field");
            projectData.ValueManager.Remove(unusedTable);

            Assert.IsFalse(projectData.ValueManager.Contains(unusedTable));
            Assert.AreEqual(SimId.Empty, unusedTable.Id);
            Assert.AreEqual(null, unusedTable.Factory);
            Assert.IsTrue(projectData.ValueManager.HasChanges);
            eventCounter.AssertEventCount(1);
        }

        [TestMethod]
        public void Field3DRemoveMemoryLeakTest()
        {
            LoadProject(field3DProject);

            WeakReference unusedTableRef = new WeakReference(projectData.ValueManager.First(x => x.Name == "unused_field"));
            projectData.ValueManager.Remove((SimMultiValueField3D)unusedTableRef.Target);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(unusedTableRef.IsAlive);
        }

        [TestMethod]
        public void Field3DHasChanges()
        {
            var startTime = DateTime.Now;
            LoadProject(field3DProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var tab = (SimMultiValueField3D)fac.FirstOrDefault(x => x is SimMultiValueField3D);

            {
                tab.Description = "New String";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.UnitX = "New Unit";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.UnitY = "New Unit";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.UnitZ = "New Unit";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab.Name = "New Name";
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                tab[0, 0, 0] = 99.9;
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        [TestMethod]
        public void Field3DXsChanges()
        {
            var startTime = DateTime.Now;
            LoadProject(field3DProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var tab = (SimMultiValueField3D)fac.FirstOrDefault(x => x is SimMultiValueField3D);

            {
                tab.XAxis.Add(100);
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }

            {
                tab.XAxis.RemoveAt(0);
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }

            {
                tab.XAxis[0] = 100.0;
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        [TestMethod]
        public void Field3DYsChanges()
        {
            var startTime = DateTime.Now;
            LoadProject(field3DProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var tab = (SimMultiValueField3D)fac.FirstOrDefault(x => x is SimMultiValueField3D);

            {
                tab.YAxis.Add(100);
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }

            {
                tab.YAxis.RemoveAt(0);
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }

            {
                tab.YAxis[0] = 100.0;
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        [TestMethod]
        public void Field3DZsChanges()
        {
            var startTime = DateTime.Now;
            LoadProject(field3DProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var tab = (SimMultiValueField3D)fac.FirstOrDefault(x => x is SimMultiValueField3D);

            {
                tab.ZAxis.Add(100);
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }

            {
                tab.ZAxis.RemoveAt(0);
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }

            {
                tab.ZAxis[0] = 100.0;
                Assert.IsTrue(fac.HasChanges);
                eventCounter.AssertEventCount(1);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        #endregion

        #region Function

        [TestMethod]
        public void FunctionAdd()
        {
            DateTime startTime = DateTime.Now;

            LoadProject(functionProject);
            var testData = SimMultiValueFunctionTests.TestData(2);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager.Add(null); });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            var func1 = new SimMultiValueFunction(testData.name, testData.unitX, testData.unitY,
                testData.unitZ, testData.bounds, testData.zs, testData.graphs);

            projectData.ValueManager.Add(func1);
            Assert.AreEqual(func1.Id.Location, project);
            Assert.AreEqual(func1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(func1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, func1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != func1.LocalID || x == func1));

            Assert.IsTrue(projectData.ValueManager.Contains(func1));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
            Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
            factoryEvents.AssertEventCount(1);

            Assert.ThrowsException<ArgumentException>(() => projectData.ValueManager.Add(func1));
        }
        [TestMethod]
        public void FunctionInsert()
        {
            LoadProject(functionProject);
            var testData = SimMultiValueFunctionTests.TestData(2);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager.Insert(0, null); });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            var func1 = new SimMultiValueFunction(testData.name, testData.unitX, testData.unitY,
                testData.unitZ, testData.bounds, testData.zs, testData.graphs);

            projectData.ValueManager.Insert(0, func1);
            Assert.AreEqual(func1.Id.Location, project);
            Assert.AreEqual(func1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(func1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, func1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != func1.LocalID || x == func1));

            Assert.IsTrue(projectData.ValueManager.Contains(func1));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(1);
        }
        [TestMethod]
        public void FunctionReplace()
        {
            LoadProject(functionProject);
            var testData = SimMultiValueFunctionTests.TestData(2);
            var factoryEvents = new SimFactoryEventCounter(projectData.ValueManager);

            Assert.ThrowsException<ArgumentNullException>(() => { projectData.ValueManager[0] = null; });

            Assert.IsFalse(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(0);

            //Working examples
            int replaceIdx = projectData.ValueManager.FindIndex(x => x.Name == "unused_function");
            var replaceTable = projectData.ValueManager[replaceIdx];
            var func1 = new SimMultiValueFunction(testData.name, testData.unitX, testData.unitY,
                testData.unitZ, testData.bounds, testData.zs, testData.graphs);

            projectData.ValueManager[replaceIdx] = func1;
            Assert.AreEqual(func1.Id.Location, project);
            Assert.AreEqual(func1.Id.GlobalId, project.GlobalID);
            Assert.IsTrue(func1.LocalID > 0);
            Assert.AreEqual(projectData.ValueManager, func1.Factory);
            Assert.IsTrue(projectData.ValueManager.All(x => x.LocalID != func1.LocalID || x == func1));

            Assert.AreEqual(null, replaceTable.Factory);
            Assert.AreEqual(SimId.Empty, replaceTable.Id);

            Assert.IsTrue(projectData.ValueManager.Contains(func1));
            Assert.IsFalse(projectData.ValueManager.Contains(replaceTable));

            Assert.IsTrue(projectData.ValueManager.HasChanges);
            factoryEvents.AssertEventCount(1);
        }
        [TestMethod]
        public void FunctionRemove()
        {
            LoadProject(functionProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var unusedTable = projectData.ValueManager.First(x => x.Name == "unused_function");
            projectData.ValueManager.Remove(unusedTable);

            Assert.IsFalse(projectData.ValueManager.Contains(unusedTable));
            Assert.AreEqual(SimId.Empty, unusedTable.Id);
            Assert.AreEqual(null, unusedTable.Factory);
            Assert.IsTrue(projectData.ValueManager.HasChanges);
            eventCounter.AssertEventCount(1);
        }

        [TestMethod]
        public void FunctionRemoveMemoryLeakTest()
        {
            LoadProject(functionProject);

            WeakReference unusedTableRef = new WeakReference(projectData.ValueManager.First(x => x.Name == "unused_function"));
            projectData.ValueManager.Remove((SimMultiValueFunction)unusedTableRef.Target);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(unusedTableRef.IsAlive);
        }

        [TestMethod]
        public void FunctionHasChanges()
        {
            var startTime = DateTime.Now;
            LoadProject(functionProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var func = (SimMultiValueFunction)fac.FirstOrDefault(x => x is SimMultiValueFunction);

            {
                func.Description = "New String";
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.Name = "New String";
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.Range = new Range3D(new Point3D(0, 0, 0), new Point3D(1, 1, 1));
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.UnitX = "newunitx";
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.UnitY = "newunitx";
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.UnitZ = "newunitx";
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        [TestMethod]
        public void FunctionGraphHasChanges()
        {
            var startTime = DateTime.Now;
            LoadProject(functionProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var func = (SimMultiValueFunction)fac.FirstOrDefault(x => x is SimMultiValueFunction);

            {
                func.Graphs.RemoveAt(0);
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                var testData = SimMultiValueFunctionGraphTests.TestDataGraph(3, 2, 2, 0);
                func.Graphs.Add(testData.graph);
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.Graphs[0].Name = "changedname";
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.Graphs[0].Points.RemoveAt(0);
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.Graphs[0].Points.Insert(0, new Point3D(0, 0, 0));
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.Graphs[0].Points[0] = new Point3D(0, 2, 0);
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.Graphs.Clear();
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        [TestMethod]
        public void FunctionZsHasChanges()
        {
            var startTime = DateTime.Now;
            LoadProject(functionProject);
            var eventCounter = new SimFactoryEventCounter(projectData.ValueManager);

            var fac = projectData.ValueManager;
            var func = (SimMultiValueFunction)fac.FirstOrDefault(x => x is SimMultiValueFunction);

            {
                func.ZAxis.Add(1000.0);
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.ZAxis.RemoveAt(func.ZAxis.Count - 1);
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
            {
                func.ZAxis[0] = -1;
                Assert.IsTrue(fac.HasChanges);
                Assert.IsTrue(projectData.ValueManager.LastChange >= startTime);
                Assert.IsTrue(projectData.ValueManager.LastChange <= DateTime.Now);
                startTime = projectData.ValueManager.LastChange;
                eventCounter.AssertEventCount(1);
                fac.ResetChanges();
                Assert.IsFalse(fac.HasChanges);
                eventCounter.AssertEventCount(2);
                eventCounter.Reset();
            }
        }

        #endregion
    }
}
