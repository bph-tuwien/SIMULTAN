using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

            var param = new SimParameter("param", "unicorns", 2, SimParameterOperations.All);
            comp.Parameters.Add(param);
            var write = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.IsTrue(comp.Parameters.Contains(param));
            Assert.AreEqual(comp, param.Component);
            Assert.AreEqual(projectData.Components, param.Factory);
            Assert.AreEqual(project, param.Id.Location);
            Assert.AreNotEqual(0, param.Id.LocalId);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            Assert.AreEqual(param, projectData.IdGenerator.GetById<SimParameter>(param.Id));

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

            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimParameter>(param.Id));
        }

        [TestMethod]
        public void ReplaceParameter()
        {
            LoadProject(parameterProject);
            var comp = projectData.Components.First(x => x.Name == "Empty");
            var param = new SimParameter("param", "unicorns", 2, SimParameterOperations.All);
            comp.Parameters.Add(param);
            var lastWrite = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            var index = comp.Parameters.IndexOf(param);

            var param2 = new SimParameter("param2", "unicorns", 2, SimParameterOperations.All);

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
                Assert.AreEqual(null, projectData.IdGenerator.GetById<SimParameter>(p.Id));
            }
        }

        [TestMethod]
        public void MemoryLeakTest()
        {
            LoadProject(parameterProject);

            var sourceComponent = projectData.Components.FirstOrDefault(x => x.Name == "NotEmpty");
            WeakReference paramRef = new WeakReference(sourceComponent.Parameters.First());

            Assert.IsTrue(paramRef.IsAlive);

            sourceComponent.Parameters.Remove((SimParameter)paramRef.Target);

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

            var param = new SimParameter("param", "unicorns", 2, SimParameterOperations.All);
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

            var param = new SimParameter("add", "football fields", 3.5, SimParameterOperations.All);

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

            var param = new SimParameter("add", "football fields", 3.5, SimParameterOperations.All);

            //Add
            bphComp.Parameters.Add(param);

            //Remove
            Assert.ThrowsException<AccessDeniedException>(() => { archComp.Parameters.RemoveAt(0); });
            bphComp.Parameters.Remove(param);
            Assert.AreEqual(1, bphComp.Parameters.Count);
        }
    }
}
