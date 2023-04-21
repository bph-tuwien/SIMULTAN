using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Exceptions;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class ParameterManagementTests : BaseProjectTest
    {
        private static readonly FileInfo parameterProject = new FileInfo(@".\ParameterTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@".\AccessTestsProject.simultan");


        [TestMethod]
        public void AddParameter()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "Empty");
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Thread.Sleep(5);

            Assert.ThrowsException<ArgumentNullException>(() => { comp.Parameters.Add(null); });

            var param = new SimDoubleParameter("param", "unicorns", 2, SimParameterOperations.All);
            comp.Parameters.Add(param);
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.IsTrue(comp.Parameters.Contains(param));
            Assert.AreEqual(comp, param.Component);
            Assert.AreEqual(projectData.Components, param.Factory);
            Assert.AreEqual(project, param.Id.Location);
            Assert.AreNotEqual(0, param.Id.LocalId);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            Assert.AreEqual(param, projectData.IdGenerator.GetById<SimDoubleParameter>(param.Id));
        }

        [TestMethod]
        public void AddParameterNull()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "Empty");
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Thread.Sleep(5);

            Assert.ThrowsException<ArgumentNullException>(() => { comp.Parameters.Add(null); });

            var param = new SimDoubleParameter("param", "unicorns", 2, SimParameterOperations.All);
            comp.Parameters.Add(param);
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.IsTrue(comp.Parameters.Contains(param));
            Assert.AreEqual(comp, param.Component);
            Assert.AreEqual(projectData.Components, param.Factory);
            Assert.AreEqual(project, param.Id.Location);
            Assert.AreNotEqual(0, param.Id.LocalId);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            Assert.AreEqual(param, projectData.IdGenerator.GetById<SimDoubleParameter>(param.Id));
        }

        [TestMethod]
        public void AddParameterTwice()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "Empty");
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            var param = new SimDoubleParameter("param", "unicorns", 2, SimParameterOperations.All);
            comp.Parameters.Add(param);

            Assert.ThrowsException<ArgumentException>(() => { comp.Parameters.Add(param); });
        }

        [TestMethod]
        public void RemoveParameter()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "NotEmpty");
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            var param = comp.Parameters.First();
            var paramId = param.Id.LocalId;

            Thread.Sleep(5);
            comp.Parameters.Remove(param);
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.IsFalse(comp.Parameters.Contains(param));
            Assert.AreEqual(null, param.Component);
            Assert.AreEqual(null, param.Factory);
            Assert.AreEqual(null, param.Id.Location);
            Assert.AreEqual(paramId, param.Id.LocalId);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimBaseParameter>(param.Id));
        }

        [TestMethod]
        public void ReplaceDoubleParameter()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "Empty");
            var param = new SimDoubleParameter("param", "unicorns", 2, SimParameterOperations.All);
            comp.Parameters.Add(param);
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            var index = comp.Parameters.IndexOf(param);

            var param2 = new SimDoubleParameter("param2", "unicorns", 2, SimParameterOperations.All);

            Assert.ThrowsException<ArgumentNullException>(() => { comp.Parameters[index] = null; });

            Thread.Sleep(5);
            comp.Parameters[index] = param2;

            Assert.IsFalse(comp.Parameters.Contains(param));
            Assert.AreEqual(null, param.Component);
            Assert.AreEqual(null, param.Factory);
            Assert.AreEqual(null, param.Id.Location);

            Assert.IsTrue(comp.Parameters.Contains(param2));
            Assert.AreEqual(comp, param2.Component);
            Assert.AreEqual(projectData.Components, param2.Factory);
            Assert.AreEqual(project, param2.Id.Location);
            Assert.AreNotEqual(0, param2.Id.LocalId);

            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
        }

        [TestMethod]
        public void ClearParameters()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "NotEmpty");
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            var oldParams = comp.Parameters.ToArray();
            Assert.AreEqual(2, comp.Parameters.Count);
            Thread.Sleep(5);

            //Clear
            comp.Parameters.Clear();
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.AreEqual(0, comp.Parameters.Count);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            foreach (var p in oldParams)
            {
                Assert.AreEqual(null, p.Component);
                Assert.AreEqual(null, p.Factory);
                Assert.AreEqual(null, p.Id.Location);
                Assert.AreEqual(null, projectData.IdGenerator.GetById<SimBaseParameter>(p.Id));
            }
        }

        [TestMethod]
        public void MoveParameter()
        {
            LoadProject(parameterProject);
            var comp1 = projectData.Components.First(x => x.Name == "NotEmpty");
            var comp2 = projectData.Components.First(x => x.Name == "Empty");

            var param = comp1.Parameters.First(x => x.NameTaxonomyEntry.Name == "a");
            var id = param.Id;

            //Move to other component
            comp2.Parameters.Add(param);

            Assert.IsTrue(comp2.Parameters.Contains(param));
            Assert.AreEqual(id, param.Id);
            Assert.AreEqual(comp2, param.Component);
        }

        [TestMethod]
        public void MoveParameterAsNew()
        {
            LoadProject(parameterProject);
            var comp1 = projectData.Components.First(x => x.Name == "NotEmpty");
            var comp2 = projectData.Components.First(x => x.Name == "Empty");

            var param = comp1.Parameters.First(x => x.NameTaxonomyEntry.Name == "a");
            var id = param.Id;

            //Remove from old, add to other component
            comp1.Parameters.Remove(param);

            Assert.ThrowsException<NotSupportedException>(() => { comp2.Parameters.Add(param); });

            //Reset Id
            param.Id = SimId.Empty;
            comp2.Parameters.Add(param);

            Assert.IsTrue(comp2.Parameters.Contains(param));
            Assert.AreNotEqual(id, param.Id);
            Assert.AreEqual(comp2, param.Component);
        }

        [TestMethod]
        public void MoveParameterToOtherFactory()
        {
            LoadProject(parameterProject);
            var comp1 = projectData.Components.First(x => x.Name == "NotEmpty");

            var param = comp1.Parameters.First(x => x.NameTaxonomyEntry.Name == "a");
            var id = param.Id;

            ExtendedProjectData data = new ExtendedProjectData();
            var comp2 = new SimComponent();
            data.Components.Add(comp2);

            //Move to other component
            Assert.ThrowsException<NotSupportedException>(() => { comp2.Parameters.Add(param); });

            Assert.IsTrue(comp1.Parameters.Contains(param));
        }


        private WeakReference MemoryLeakTest_Action()
        {
            var sourceComponent = projectData.Components.FirstOrDefault(x => x.Name == "NotEmpty");
            WeakReference paramRef = new WeakReference(sourceComponent.Parameters.First());

            Assert.IsTrue(paramRef.IsAlive);

            sourceComponent.Parameters.Remove((SimBaseParameter)paramRef.Target);
            return paramRef;
        }
        [TestMethod]
        public void MemoryLeakTest()
        {
            LoadProject(parameterProject);

            var paramRef = MemoryLeakTest_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(paramRef.IsAlive);
        }

        [TestMethod]
        public void CategoryUpdateTest()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "Empty");

            var param = new SimDoubleParameter("param", "unicorns", 2, SimParameterOperations.All);
            param.Category = SimCategory.Light_Natural;

            Assert.IsFalse(comp.Category.HasFlag(SimCategory.Light_Natural));

            comp.Parameters.Add(param);
            Assert.IsTrue(comp.Category.HasFlag(SimCategory.Light_Natural));

            comp.Parameters.Remove(param);
            Assert.IsFalse(comp.Category.HasFlag(SimCategory.Light_Natural));
        }


        [TestMethod]
        public void AddAccessTests()
        {
            LoadProject(accessProject, "bph", "bph");

            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var param = new SimDoubleParameter("add", "football fields", 3.5, SimParameterOperations.All);

            //Add
            Assert.ThrowsException<AccessDeniedException>(() => { archComp.Parameters.Add(param); });
            Assert.AreEqual(1, archComp.Parameters.Count);

            bphComp.Parameters.Add(param);
            Assert.AreEqual(2, bphComp.Parameters.Count);
        }

        [TestMethod]
        public void RemoveAccessTests()
        {
            LoadProject(accessProject, "bph", "bph");

            var archComp = projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = projectData.Components.First(x => x.Name == "BPHComp");
            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x.Name == "Table");

            var param = new SimDoubleParameter("add", "football fields", 3.5, SimParameterOperations.All);

            //Add
            bphComp.Parameters.Add(param);

            //Remove
            Assert.ThrowsException<AccessDeniedException>(() => { archComp.Parameters.RemoveAt(0); });
            bphComp.Parameters.Remove(param);
            Assert.AreEqual(1, bphComp.Parameters.Count);
        }
    }
}
