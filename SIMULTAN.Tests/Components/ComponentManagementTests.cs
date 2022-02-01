using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.Utils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class ComponentManagementTests : BaseProjectTest
    {
        private static readonly FileInfo emptyProject = new FileInfo(@".\EmptyProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@".\ComponentAccessTestsProject.simultan");

        #region Operations

        [TestMethod]
        public void RootComponentAdd()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            SimComponent component = new SimComponent();

            Assert.AreEqual(SimId.Empty, component.Id);
            Assert.AreEqual(null, component.Factory);

            //Test
            projectData.Components.Add(component);

            Assert.AreNotEqual(0, component.LocalID);
            Assert.AreEqual(project, component.Id.Location);
            Assert.IsTrue(projectData.Components.Contains(component));
            Assert.AreEqual(projectData.Components, component.Factory);
            Assert.AreEqual(null, component.Parent);

            Assert.AreEqual(component, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, component.Id.LocalId)));
        }

        [TestMethod]
        public void RootComponentAddTwice()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            SimComponent component = new SimComponent();
            projectData.Components.Add(component);

            //Test
            Assert.ThrowsException<ArgumentException>(() => { projectData.Components.Add(component); });
        }

        [TestMethod]
        public void RootComponentAddExceptions()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Exception
            Assert.ThrowsException<ArgumentNullException>(() => { projectData.Components.Add(null); });

            //Add with existing id
            SimComponent component = new SimComponent();
            component.Id = new SimId(99);

            Assert.ThrowsException<NotSupportedException>(() => { projectData.Components.Add(component); });
        }

        [TestMethod]
        public void RootComponentRemove()
        {
            LoadProject(emptyProject);

            //Setup
            SimComponent component = new SimComponent();
            int counter = 0;
            component.IsBeingDeleted += (s) => counter++;

            projectData.Components.Add(component);

            //Actual Test

            var localId = component.Id.LocalId;

            projectData.Components.Remove(component);

            Assert.IsFalse(projectData.Components.Contains(component));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, localId)));
            Assert.AreEqual(null, component.Factory);

            Assert.AreEqual(localId, component.Id.LocalId);
            Assert.AreEqual(null, component.Id.Location);

            Assert.AreEqual(1, counter);
        }

        [TestMethod]
        public void RootComponentReplace()
        {
            LoadProject(emptyProject);

            //Setup
            SimComponent component = new SimComponent();
            SimComponent component2 = new SimComponent();
            int counter = 0;
            component.IsBeingDeleted += (s) => counter++;

            projectData.Components.Add(component);

            var localId = component.Id.LocalId;

            //Exception
            Assert.ThrowsException<ArgumentNullException>(() => { projectData.Components[0] = null; });

            //Test

            projectData.Components[0] = component2;

            //Old
            Assert.IsFalse(projectData.Components.Contains(component));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, localId)));
            Assert.AreEqual(null, component.Factory);
            Assert.AreEqual(localId, component.Id.LocalId);
            Assert.AreEqual(null, component.Id.Location);

            //New
            Assert.IsTrue(projectData.Components.Contains(component2));
            Assert.AreEqual(component2, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, component2.LocalID)));
            Assert.AreEqual(projectData.Components, component2.Factory);
            Assert.AreNotEqual(0, component2.Id.LocalId);
            Assert.AreEqual(project, component2.Id.Location);

            Assert.AreEqual(1, counter);
        }

        [TestMethod]
        public void RootComponentClear()
        {
            LoadProject(emptyProject);

            //Setup
            SimComponent[] components = new SimComponent[]
            {
                new SimComponent(),
                new SimComponent(),
                new SimComponent()
            };

            int counter = 0;
            components.ForEach(x => x.IsBeingDeleted += (s) => counter++);

            projectData.Components.AddRange(components);
            var ids = components.ToDictionary(x => x, x => x.Id.LocalId);

            //Test
            projectData.Components.Clear();

            //Old
            foreach (var component in components)
            {
                Assert.AreNotEqual(0, ids[component]);
                Assert.IsFalse(projectData.Components.Contains(component));
                Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, ids[component])));
                Assert.AreEqual(null, component.Factory);
                Assert.AreEqual(ids[component], component.Id.LocalId);
                Assert.AreEqual(null, component.Id.Location);
            }

            Assert.AreEqual(components.Length, counter);
        }

        [TestMethod]
        public void RootComponentRemoveWithoutDelete()
        {
            LoadProject(emptyProject);

            //Setup
            SimComponent component = new SimComponent();
            int counter = 0;
            component.IsBeingDeleted += (s) => counter++;

            projectData.Components.Add(component);

            var localId = component.Id.LocalId;

            //Test
            projectData.Components.RemoveWithoutDelete(component);

            Assert.IsFalse(projectData.Components.Contains(component));
            Assert.AreEqual(component, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, localId)));
            Assert.AreEqual(projectData.Components, component.Factory);

            Assert.AreEqual(localId, component.Id.LocalId);
            Assert.AreEqual(project, component.Id.Location);

            Assert.AreEqual(0, counter);
        }

        #endregion

        #region Access

        [TestMethod]
        public void RootComponentAddAccess()
        {
            //All users except for guest can create top-level components
            LoadProject(emptyProject, "bph", "bph");

            SimComponent component = new SimComponent();
            projectData.Components.Add(component);
            Assert.AreEqual(1, projectData.Components.Count);

            ProjectUtils.CleanupTestData(ref project, ref projectData);

            //create by guest is not allowed
            LoadProject(emptyProject, "guest", "guest");

            component = new SimComponent();
            projectData.Components.ResetChanges();

            Assert.ThrowsException<AccessDeniedException>(() => projectData.Components.Add(component));

            Assert.AreEqual(DateTime.MinValue, component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
            Assert.IsFalse(projectData.Components.HasChanges);
            Assert.AreEqual(0, projectData.Components.Count);
        }

        [TestMethod]
        public void RootComponentRemoveAccess()
        {
            //Users must have write access to the component in order to remove it
            LoadProject(accessProject, "bph", "bph");

            Assert.AreEqual(4, projectData.Components.Count);
            var bphComponent = projectData.Components.First(x => x.Name == "BPHRoot");
            var archComponent = projectData.Components.First(x => x.Name == "ArchRoot");
            var bphExclComponent = projectData.Components.First(x => x.Name == "BPHExclusiveRoot");

            projectData.Components.Remove(bphExclComponent);
            Assert.AreEqual(3, projectData.Components.Count);


            //No access to component
            projectData.Components.ResetChanges();
            var startAccess = archComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.ThrowsException<AccessDeniedException>(() => projectData.Components.Remove(archComponent));

            Assert.AreEqual(startAccess, archComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
            Assert.IsFalse(projectData.Components.HasChanges);
            Assert.AreEqual(3, projectData.Components.Count);

            //No access to child of component
            projectData.Components.ResetChanges();
            startAccess = bphComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.ThrowsException<AccessDeniedException>(() => projectData.Components.Remove(bphComponent));

            Assert.AreEqual(startAccess, bphComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
            Assert.IsFalse(projectData.Components.HasChanges);
            Assert.AreEqual(3, projectData.Components.Count);
        }

        [TestMethod]
        public void RootComponentReplaceAccess()
        {
            //Users must have write access to the component in order to remove it, and may not be guest to add the new one
            LoadProject(accessProject, "bph", "bph");
            Assert.AreEqual(4, projectData.Components.Count);

            {
                var bphComponent = projectData.Components.First(x => x.Name == "BPHExclusiveRoot");
                var index = projectData.Components.IndexOf(bphComponent);

                var replaceComp = new SimComponent();
                projectData.Components[index] = replaceComp;

                Assert.AreEqual(4, projectData.Components.Count);
                Assert.IsTrue(projectData.Components.Contains(replaceComp));
                Assert.IsFalse(projectData.Components.Contains(bphComponent));
            }
            //No write access to component
            {
                projectData.Components.ResetChanges();

                var archComponent = projectData.Components.First(x => x.Name == "ArchRoot");
                var index = projectData.Components.IndexOf(archComponent);

                var replaceComp = new SimComponent();

                var startAccess = archComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                Assert.ThrowsException<AccessDeniedException>(() => projectData.Components[index] = replaceComp);

                Assert.AreEqual(4, projectData.Components.Count);
                Assert.IsFalse(projectData.Components.Contains(replaceComp));
                Assert.IsTrue(projectData.Components.Contains(archComponent));

                Assert.AreEqual(startAccess, archComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(DateTime.MinValue, replaceComp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.IsFalse(projectData.Components.HasChanges);
            }
            //No write access to components child
            {
                projectData.Components.ResetChanges();

                var bphComponent = projectData.Components.First(x => x.Name == "BPHRoot");
                var index = projectData.Components.IndexOf(bphComponent);

                var replaceComp = new SimComponent();

                var startAccess = bphComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                Assert.ThrowsException<AccessDeniedException>(() => projectData.Components[index] = replaceComp);

                Assert.AreEqual(4, projectData.Components.Count);
                Assert.IsFalse(projectData.Components.Contains(replaceComp));
                Assert.IsTrue(projectData.Components.Contains(bphComponent));

                Assert.AreEqual(startAccess, bphComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(DateTime.MinValue, replaceComp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.IsFalse(projectData.Components.HasChanges);
            }
            ProjectUtils.CleanupTestData(ref project, ref projectData);


            //Guest
            LoadProject(accessProject, "guest", "guest");
            {
                projectData.Components.ResetChanges();

                var rootComponent = projectData.Components.First(x => x.Name == "GuestRoot");
                var index = projectData.Components.IndexOf(rootComponent);

                var replaceComp = new SimComponent();

                var startAccess = rootComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                Assert.ThrowsException<AccessDeniedException>(() => projectData.Components[index] = replaceComp);

                Assert.AreEqual(4, projectData.Components.Count);
                Assert.IsFalse(projectData.Components.Contains(replaceComp));
                Assert.IsTrue(projectData.Components.Contains(rootComponent));

                Assert.AreEqual(startAccess, rootComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(DateTime.MinValue, replaceComp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        [TestMethod]
        public void RootComponentClearAccess()
        {
            //Clear is only allowed when the user has write access to all components
            LoadProject(emptyProject, "bph", "bph");

            for (int i = 0; i < 3; i++)
            {
                var comp = new SimComponent();
                comp.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
                projectData.Components.Add(comp);
            }


            Assert.AreEqual(3, projectData.Components.Count);
            projectData.Components.Clear();
            Assert.AreEqual(0, projectData.Components.Count);
            ProjectUtils.CleanupTestData(ref project, ref projectData);


            LoadProject(accessProject, "bph", "bph");
            var startAccess = projectData.Components.ToDictionary(x => x, x => x.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));

            Assert.ThrowsException<AccessDeniedException>(() => projectData.Components.Clear());

            Assert.AreEqual(4, projectData.Components.Count);

            foreach (var comp in projectData.Components)
            {
                Assert.AreEqual(startAccess[comp], comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        #endregion

        #region HasChanges, LastChanges

        [TestMethod]
        public void RootComponentAddChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            var startCollectionAccess = projectData.Components.LastChange;
            SimComponent component = new SimComponent();

            Assert.AreEqual(DateTime.MinValue, component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
            var startTime = DateTime.Now;

            Thread.Sleep(5);

            //Test
            projectData.Components.Add(component);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);
        }

        [TestMethod]
        public void RootComponentRemoveChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            SimComponent component = new SimComponent();
            component.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;

            projectData.Components.Add(component);

            Assert.AreNotEqual(DateTime.MinValue, component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
            var startTime = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess;

            projectData.Components.ResetChanges();
            var startCollectionAccess = projectData.Components.LastChange;
            Thread.Sleep(5);

            //Actual Test
            projectData.Components.Remove(component);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);
        }

        [TestMethod]
        public void RootComponentReplaceChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            SimComponent component = new SimComponent();
            component.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            SimComponent component2 = new SimComponent();

            projectData.Components.Add(component);

            projectData.Components.ResetChanges();
            var startCollectionAccess = projectData.Components.LastChange;
            var startTimeNew = DateTime.Now;
            var startTimeOld = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess;

            Thread.Sleep(5);

            //Test
            projectData.Components[0] = component2;

            //Factory
            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            //LastAccess
            Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTimeOld);
            Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(component2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTimeNew);
            Assert.IsTrue(component2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, component2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);
        }

        [TestMethod]
        public void RootComponentClearChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            SimComponent[] components = new SimComponent[]
            {
                new SimComponent(),
                new SimComponent(),
                new SimComponent()
            };
            components.ForEach(x =>
            {
                x.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            });

            projectData.Components.AddRange(components);

            var startTimes = components.ToDictionary(x => x, x => x.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);

            projectData.Components.ResetChanges();
            var startCollectionAccess = projectData.Components.LastChange;
            Thread.Sleep(5);

            //Test
            projectData.Components.Clear();

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            foreach (var component in components)
            {
                Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTimes[component]);
                Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
                Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);
            }
        }

        [TestMethod]
        public void RootComponentRemoveWithoutDeleteChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            SimComponent component = new SimComponent();
            component.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(component);

            projectData.Components.ResetChanges();
            var startCollectionAccess = projectData.Components.LastChange;
            var startTime = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess;
            Thread.Sleep(5);

            //Test
            projectData.Components.RemoveWithoutDelete(component);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);
        }

        #endregion
    }
}
