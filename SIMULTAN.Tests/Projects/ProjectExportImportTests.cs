using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer.Projects;
using SIMULTAN.Tests.TestUtils;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Projects
{
    [TestClass]
    public class ProjectExportImportTests : BaseProjectTest
    {
        private static readonly FileInfo exportProject = new FileInfo(@"./ComponentExportTest.simultan");
        private static readonly FileInfo emptyProject = new FileInfo(@"./EmptyProject.simultan");
        private static readonly FileInfo importArchive = new FileInfo(@"./ComponentImportTest.zip");

        #region Export

        [TestMethod]
        public void GetAllReferencedComponentsSimpleReference()
        {
            LoadProject(exportProject, "admin", "admin");

            var comp1 = projectData.Components.First(x => x.Name == "EdgeParent");
            var target1 = projectData.Components.First(x => x.Name == "Target");
            var target2 = projectData.Components.First(x => x.Name == "Target2");

            var export = ProjectExportImport.GetAllReferencedComponents(new SimComponent[] { comp1 }).ToList();

            Assert.AreEqual(3, export.Count);
            Assert.IsTrue(export.Contains(comp1));
            Assert.IsTrue(export.Contains(target1));
            Assert.IsTrue(export.Contains(target2));
        }

        [TestMethod]
        public void GetAllReferencedComponentsReferenceLoop()
        {
            LoadProject(exportProject, "admin", "admin");

            var comp1 = projectData.Components.First(x => x.Name == "RefChain1");
            var allChainComps = projectData.Components.Where(x => x.Name.StartsWith("RefChain")).ToList();

            var export = ProjectExportImport.GetAllReferencedComponents(new SimComponent[] { comp1 }).ToList();

            Assert.AreEqual(3, export.Count);
            Assert.IsTrue(export.Contains(comp1));

            foreach (var c in allChainComps)
                Assert.IsTrue(export.Contains(c));
        }

        [TestMethod]
        public void GetReferencedComponentsAndNetworksWithNetworks()
        {
            LoadProject(exportProject, "admin", "admin");

            var comp1 = projectData.Components.First(x => x.Name == "EdgeParent").Components.First(x => x.Component.Name == "Edge").Component;

            SimComponent[] allResultComponents = new SimComponent[]
            {
                comp1,
                projectData.Components.First(x => x.Name == "Node"),
                projectData.Components.First(x => x.Name == "Node2"),
                projectData.Components.First(x => x.Name == "Target"),
                projectData.Components.First(x => x.Name == "Target2").Components.First(x => x.Component.Name == "ChildTarget2").Component,
            };

            var export = ProjectExportImport.GetReferencedComponentsAndNetworks(new SimComponent[] { comp1 });

            Assert.AreEqual(5, export.components.Count());
            foreach (var c in allResultComponents)
                Assert.IsTrue(export.components.Contains(c));

            Assert.AreEqual(1, export.networks.Count());
            Assert.IsTrue(export.networks.Contains(projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Demo Network")));
        }


        [TestMethod]
        public void GetReferencedMultiValues()
        {
            LoadProject(exportProject, "admin", "admin");

            var comp1 = projectData.Components.First(x => x.Name == "Target2");
            var mv = ProjectExportImport.GetReferencedMultiValues(new SimComponent[] { comp1 });

            var table1 = projectData.ValueManager.First(x => x.Name == "Table1") as SimMultiValueBigTable;

            Assert.AreEqual(1, mv.Count());
            Assert.IsTrue(mv.Contains(table1));
        }

        #endregion

        #region Import

        [TestMethod]
        public void ImportComponents()
        {
            LoadProject(emptyProject, "admin", "admin");

            ProjectExportImport.ImportComponentLibrary(project, importArchive);

            Assert.AreEqual(1, projectData.Components.Count);
            var importComp = projectData.Components.First();

            Assert.AreEqual(5, importComp.Components.Count);

            HashSet<string> undefinedExtensions = new HashSet<string> { "0", "1", "2", "3" };

            var undefinedSlot = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined);

            var comp = importComp.Components.First(x => x.Component.Name == "ChildTarget2");
            {
                Assert.AreEqual(0, comp.Component.Components.Count);
                Assert.AreEqual(undefinedSlot, comp.Slot.SlotBase.Target);
            }

            comp = importComp.Components.First(x => x.Component.Name == "Edge");
            {
                Assert.AreEqual(1, comp.Component.Components.Count);
                Assert.AreEqual(undefinedSlot, comp.Slot.SlotBase.Target);

                var childComp = comp.Component.Components.First(x => x.Component.Name == "Cumulative");
                Assert.AreEqual(0, childComp.Component.Components.Count);
                Assert.AreEqual(SimDefaultSlotKeys.GeometricReference, childComp.Slot.SlotBase.Target.Key);
                Assert.AreEqual("AG0", childComp.Slot.SlotExtension);
            }

            comp = importComp.Components.First(x => x.Component.Name == "Node");
            {
                Assert.AreEqual(2, comp.Component.Components.Count);
                Assert.AreEqual(undefinedSlot, comp.Slot.SlotBase.Target);

                var childComp = comp.Component.Components.First(x => x.Component.Name == "Cumulative");
                Assert.AreEqual(0, childComp.Component.Components.Count);
                Assert.AreEqual(SimDefaultSlotKeys.GeometricReference, childComp.Slot.SlotBase.Target.Key);
                Assert.AreEqual("AG0", childComp.Slot.SlotExtension);

                childComp = comp.Component.Components.First(x => x.Component.Name == "NodeChild");
                Assert.AreEqual(0, childComp.Component.Components.Count);
                Assert.AreEqual(undefinedSlot, childComp.Slot.SlotBase.Target);
                Assert.AreEqual("0", childComp.Slot.SlotExtension);
            }

            comp = importComp.Components.First(x => x.Component.Name == "Node2");
            {
                Assert.AreEqual(1, comp.Component.Components.Count);
                Assert.AreEqual(undefinedSlot, comp.Slot.SlotBase.Target);

                var childComp = comp.Component.Components.First(x => x.Component.Name == "Cumulative");
                Assert.AreEqual(0, childComp.Component.Components.Count);
                Assert.AreEqual(SimDefaultSlotKeys.GeometricReference, childComp.Slot.SlotBase.Target.Key);
                Assert.AreEqual("AG0", childComp.Slot.SlotExtension);
            }

            comp = importComp.Components.First(x => x.Component.Name == "Target");
            {
                Assert.AreEqual(0, comp.Component.Components.Count);
                Assert.AreEqual(SimDefaultSlotKeys.Specification, comp.Slot.SlotBase.Target.Key);
            }

            //Make sure that all components got unique slot extensions assigned
            Assert.IsFalse(importComp.Components.Any(x => importComp.Components.Count(y => y.Slot == x.Slot) > 1));
        }

        [TestMethod]
        public void ImportReferences()
        {
            LoadProject(emptyProject, "admin", "admin");

            ProjectExportImport.ImportComponentLibrary(project, importArchive);

            Assert.AreEqual(1, projectData.Components.Count);
            var importComp = projectData.Components.First();

            var target = importComp.Components.First(x => x.Component.Name == "Target").Component;
            var childTarget2 = importComp.Components.First(x => x.Component.Name == "ChildTarget2").Component;

            var comp = importComp.Components.First(x => x.Component.Name == "Edge").Component;
            Assert.AreEqual(1, comp.ReferencedComponents.Count);
            Assert.AreEqual(target, comp.ReferencedComponents.First().Target);

            comp = importComp.Components.First(x => x.Component.Name == "Node").Component.Components.First(x => x.Component.Name == "NodeChild").Component;
            Assert.AreEqual(2, comp.ReferencedComponents.Count);
            Assert.IsTrue(comp.ReferencedComponents.Any(x => x.Target == target));
            Assert.IsTrue(comp.ReferencedComponents.Any(x => x.Target == childTarget2));
        }

        [TestMethod]
        public void ImportMultiValues()
        {
            LoadProject(emptyProject, "admin", "admin");

            ProjectExportImport.ImportComponentLibrary(project, importArchive);

            //Check MV
            Assert.AreEqual(1, projectData.ValueManager.Count);
            var table1 = projectData.ValueManager.First(x => x.Name.StartsWith("Table1")) as SimMultiValueBigTable;

            Assert.AreEqual(2, table1.ColumnHeaders.Count);
            Assert.AreEqual(2, table1.RowHeaders.Count);

            //Check pointer
            var importComp = projectData.Components.First();
            var childTarget2 = importComp.Components.First(x => x.Component.Name == "ChildTarget2").Component;
            var param = childTarget2.Parameters.OfType<SimDoubleParameter>().First(x => x.NameTaxonomyEntry.Text == "from MV");

            Assert.AreNotEqual(null, param.ValueSource);

            var ptr = param.ValueSource as SimMultiValueBigTableParameterSource;
            Assert.IsNotNull(ptr);
            Assert.AreEqual(table1, ptr.Table);

        }

        [TestMethod]
        public void ImportCalculations()
        {
            LoadProject(emptyProject, "admin", "admin");

            ProjectExportImport.ImportComponentLibrary(project, importArchive);

            //Find calculation
            var importComp = projectData.Components.First();
            var node = importComp.Components.First(x => x.Component.Name == "Node").Component;
            var nodeChild = node.Components.First(x => x.Component.Name == "NodeChild").Component;

            Assert.AreEqual(1, node.Calculations.Count);
            var calc = node.Calculations[0];

            //Check if parameters have been found
            Assert.AreEqual(2, calc.InputParams.Count);
            Assert.AreEqual(1, calc.ReturnParams.Count);
            Assert.AreEqual(nodeChild.Parameters.First(x => x.NameTaxonomyEntry.Text == "b"), calc.InputParams["y"]);
            Assert.AreEqual(node.Parameters.First(x => x.NameTaxonomyEntry.Text == "a"), calc.InputParams["x"]);
            Assert.AreEqual(node.Parameters.First(x => x.NameTaxonomyEntry.Text == "c"), calc.ReturnParams["out1"]);
        }

        #endregion
    }
}