using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.TestUtils;
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
    public class ReferenceManagementTests : BaseProjectTest
    {
        private static readonly FileInfo referencesProject = new FileInfo(@"./ReferencesTestProject.simultan");

        #region Operations

        [TestMethod]
        public void Add()
        {
            LoadProject(referencesProject, "admin", "admin");

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var owner = projectData.Components.First(x => x.Name == "Root3");

            var ref1 = new SimComponentReference(slot, target);
            Assert.AreEqual(1, target.ReferencedBy.Count);
            Assert.IsFalse(target.ReferencedBy.Any(x => x.Owner == owner));

            owner.ReferencedComponents.Add(ref1);
            Assert.AreEqual(2, target.ReferencedBy.Count);
            Assert.IsTrue(target.ReferencedBy.Any(x => x.Owner == owner));
        }

        [TestMethod]
        public void AddException()
        {
            LoadProject(referencesProject, "admin", "admin");
            var owner = projectData.Components.First(x => x.Name == "Root3");

            Assert.ThrowsException<ArgumentNullException>(() => owner.ReferencedComponents.Add(null));
        }

        [TestMethod]
        public void Remove()
        {
            LoadProject(referencesProject, "admin", "admin");

            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];

            Assert.AreEqual(owner, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(ref1, target.ReferencedBy[0]);

            owner.ReferencedComponents.Remove(ref1);
            Assert.AreEqual(0, owner.ReferencedComponents.Count);
            Assert.AreEqual(null, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(0, target.ReferencedBy.Count);
        }

        [TestMethod]
        public void Replace()
        {
            LoadProject(referencesProject, "admin", "admin");

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var target2 = projectData.Components.First(x => x.Name == "Root3");
            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];

            Assert.AreEqual(owner, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(ref1, target.ReferencedBy[0]);

            var ref2 = new SimComponentReference(slot, target2);

            owner.ReferencedComponents[0] = ref2;
            Assert.AreEqual(1, owner.ReferencedComponents.Count);
            Assert.IsTrue(owner.ReferencedComponents.Contains(ref2));

            Assert.AreEqual(null, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(0, target.ReferencedBy.Count);

            Assert.AreEqual(owner, ref2.Owner);
            Assert.AreEqual(target2, ref2.Target);
            Assert.IsTrue(target2.ReferencedBy.Contains(ref2));
        }

        [TestMethod]
        public void ReplaceException()
        {
            LoadProject(referencesProject, "admin", "admin");

            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var owner = projectData.Components.First(x => x.Name == "Root2");

            Assert.ThrowsException<ArgumentNullException>(() => owner.ReferencedComponents[0] = null);
        }

        [TestMethod]
        public void ReplaceTarget()
        {
            LoadProject(referencesProject, "admin", "admin");

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var target2 = projectData.Components.First(x => x.Name == "Root3");
            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];

            ref1.Target = target2;

            Assert.AreEqual(1, owner.ReferencedComponents.Count);
            Assert.IsTrue(owner.ReferencedComponents.Contains(ref1));

            Assert.AreEqual(owner, ref1.Owner);
            Assert.AreEqual(target2, ref1.Target);
            Assert.AreEqual(0, target.ReferencedBy.Count);
            Assert.IsTrue(target2.ReferencedBy.Contains(ref1));
        }

        [TestMethod]
        public void ReplaceTargetToNull()
        {
            LoadProject(referencesProject, "admin", "admin");

            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];
            var target = ref1.Target;

            ref1.Target = null;

            Assert.AreEqual(0, target.ReferencedBy.Count);
        }

        [TestMethod]
        public void Clear()
        {
            LoadProject(referencesProject, "admin", "admin");

            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];
            var target = ref1.Target;

            owner.ReferencedComponents.Clear();

            Assert.AreEqual(0, owner.ReferencedComponents.Count);
            Assert.AreEqual(null, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(0, target.ReferencedBy.Count);
        }

        #endregion

        #region Access

        [TestMethod]
        public void AddAccess()
        {
            //Arch doesn't have write access to owner
            LoadProject(referencesProject, "arch", "arch");

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var owner = projectData.Components.First(x => x.Name == "Root3");

            var ref1 = new SimComponentReference(slot, target);
            Assert.AreEqual(0, owner.ReferencedComponents.Count);

            Assert.ThrowsException<AccessDeniedException>(() => owner.ReferencedComponents.Add(ref1));
            Assert.AreEqual(0, owner.ReferencedComponents.Count);
            Assert.AreEqual(1, target.ReferencedBy.Count);
            Assert.IsFalse(target.ReferencedBy.Any(x => x.Owner == owner));

            ProjectUtils.CleanupTestData(ref project, ref projectData);

            //bph does have write access
            LoadProject(referencesProject, "bph", "bph");

            target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            owner = projectData.Components.First(x => x.Name == "Root3");

            ref1 = new SimComponentReference(new SimSlot(slot), target);
            owner.ReferencedComponents.Add(ref1);
            Assert.AreEqual(1, owner.ReferencedComponents.Count);
            Assert.IsTrue(owner.ReferencedComponents.Any(x => x.Target == target));
        }

        [TestMethod]
        public void RemoveAccess()
        {
            //Arch doesn't have write access to owner
            LoadProject(referencesProject, "arch", "arch");

            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];

            Assert.ThrowsException<AccessDeniedException>(() => owner.ReferencedComponents.Remove(ref1));
            Assert.AreEqual(1, owner.ReferencedComponents.Count);
            Assert.AreEqual(owner, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(1, target.ReferencedBy.Count);

            ProjectUtils.CleanupTestData(ref project, ref projectData);

            //bph does have write access
            LoadProject(referencesProject, "bph", "bph");

            target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            owner = projectData.Components.First(x => x.Name == "Root2");
            ref1 = owner.ReferencedComponents[0];

            owner.ReferencedComponents.Remove(ref1);
            Assert.AreEqual(0, owner.ReferencedComponents.Count);
            Assert.AreEqual(null, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(0, target.ReferencedBy.Count);
        }

        [TestMethod]
        public void ReplaceAccess()
        {
            //Arch doesn't have write access to owner
            LoadProject(referencesProject, "arch", "arch");

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var target2 = projectData.Components.First(x => x.Name == "Root3");
            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];
            var ref2 = new SimComponentReference(slot, target2);

            Assert.ThrowsException<AccessDeniedException>(() => owner.ReferencedComponents[0] = ref2);

            Assert.AreEqual(owner, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(ref1, target.ReferencedBy[0]);
            Assert.AreEqual(null, ref2.Owner);
            Assert.AreEqual(0, target2.ReferencedComponents.Count);

            ProjectUtils.CleanupTestData(ref project, ref projectData);

            //bph does have write access
            LoadProject(referencesProject, "bph", "bph");

            target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            target2 = projectData.Components.First(x => x.Name == "Root3");
            owner = projectData.Components.First(x => x.Name == "Root2");
            ref1 = owner.ReferencedComponents[0];
            ref2 = new SimComponentReference(new SimSlot(slot), target2);

            owner.ReferencedComponents[0] = ref2;

            Assert.AreEqual(1, owner.ReferencedComponents.Count);
            Assert.IsTrue(owner.ReferencedComponents.Contains(ref2));

            Assert.AreEqual(null, ref1.Owner);
            Assert.AreEqual(target, ref1.Target);
            Assert.AreEqual(0, target.ReferencedBy.Count);

            Assert.AreEqual(owner, ref2.Owner);
            Assert.AreEqual(target2, ref2.Target);
            Assert.IsTrue(target2.ReferencedBy.Contains(ref2));
        }

        [TestMethod]
        public void ReplaceTargetAccess()
        {
            //Arch doesn't have write access to owner
            LoadProject(referencesProject, "arch", "arch");

            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];
            var target2 = projectData.Components.First(x => x.Name == "Root3");

            Assert.ThrowsException<AccessDeniedException>(() => ref1.Target = target2);

            ProjectUtils.CleanupTestData(ref project, ref projectData);

            //bph does have write access
            LoadProject(referencesProject, "bph", "bph");

            owner = projectData.Components.First(x => x.Name == "Root2");
            ref1 = owner.ReferencedComponents[0];
            target2 = projectData.Components.First(x => x.Name == "Root3");

            ref1.Target = target2;
        }

        [TestMethod]
        public void ClearAccess()
        {
            //Arch doesn't have write access to owner
            LoadProject(referencesProject, "arch", "arch");

            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];
            var target1 = ref1.Target;

            Assert.ThrowsException<AccessDeniedException>(() => owner.ReferencedComponents.Clear());
            Assert.IsTrue(owner.ReferencedComponents.Contains(ref1));

            ProjectUtils.CleanupTestData(ref project, ref projectData);

            //bph does have write access
            LoadProject(referencesProject, "bph", "bph");

            owner = projectData.Components.First(x => x.Name == "Root2");
            ref1 = owner.ReferencedComponents[0];
            target1 = ref1.Target;

            owner.ReferencedComponents.Clear();
        }

        #endregion

        #region HasChanges, LastChanges

        [TestMethod]
        public void AddChanges()
        {
            //Arch doesn't have write access to owner
            LoadProject(referencesProject, "bph", "bph");

            var startCollectionAccess = projectData.Components.LastChange;
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var owner = projectData.Components.First(x => x.Name == "Root3");

            var ref1 = new SimComponentReference(slot, target);
            owner.ReferencedComponents.Add(ref1);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            //No access to target should be recorded
            Assert.IsTrue(target.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess < startTime);
        }

        [TestMethod]
        public void RemoveChanges()
        {
            //Arch doesn't have write access to owner
            LoadProject(referencesProject, "bph", "bph");

            var startCollectionAccess = projectData.Components.LastChange;
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var owner = projectData.Components.First(x => x.Name == "Root2");

            var ref1 = owner.ReferencedComponents[0];
            owner.ReferencedComponents.Remove(ref1);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            //No access to target should be recorded
            Assert.IsTrue(target.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess < startTime);
        }

        [TestMethod]
        public void ReplaceChanges()
        {
            LoadProject(referencesProject, "bph", "bph");

            var startCollectionAccess = projectData.Components.LastChange;
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var target2 = projectData.Components.First(x => x.Name == "Root3");
            var owner = projectData.Components.First(x => x.Name == "Root2");

            var ref1 = owner.ReferencedComponents[0];
            var ref2 = new SimComponentReference(slot, target2);

            owner.ReferencedComponents[0] = ref2;

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            //No access to target should be recorded
            Assert.IsTrue(target.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess < startTime);
            Assert.IsTrue(target2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess < startTime);
        }

        [TestMethod]
        public void ReplaceTargetChanges()
        {
            LoadProject(referencesProject, "bph", "bph");

            var startCollectionAccess = projectData.Components.LastChange;
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            var target = projectData.Components.First(x => x.Name == "Root1").Components.First(x => x.Component.Name == "Child1").Component;
            var target2 = projectData.Components.First(x => x.Name == "Root3");
            var owner = projectData.Components.First(x => x.Name == "Root2");

            var ref1 = owner.ReferencedComponents[0];

            ref1.Target = target2;

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            //No access to target should be recorded
            Assert.IsTrue(target.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess < startTime);
            Assert.IsTrue(target2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess < startTime);
        }

        [TestMethod]
        public void ClearChanges()
        {
            LoadProject(referencesProject, "bph", "bph");

            var startCollectionAccess = projectData.Components.LastChange;
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            var owner = projectData.Components.First(x => x.Name == "Root2");
            var ref1 = owner.ReferencedComponents[0];
            var target = ref1.Target;

            owner.ReferencedComponents.Clear();

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startCollectionAccess);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);

            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, owner.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            //No access to target should be recorded
            Assert.IsTrue(target.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess < startTime);
        }

        #endregion

        #region Consistency

        [TestMethod]
        public void RemoveComponentCheckReferences()
        {
            LoadProject(referencesProject);

            var root1 = projectData.Components.First(x => x.Name == "Root1");
            var root2 = projectData.Components.First(x => x.Name == "Root2");
            var child1 = root1.Components.First(x => x.Component.Name == "Child1").Component;

            Assert.AreEqual(child1, root2.ReferencedComponents.First().Target);

            projectData.Components.Remove(root1);

            Assert.AreEqual(null, root2.ReferencedComponents.First().Target);
            Assert.AreEqual(0, child1.ReferencedBy.Count);
        }

        #endregion

        #region Referencing Parameters

        [TestMethod]
        public void UpdateParameterOnAddReference()
        {
            LoadProject(referencesProject, "admin", "admin");

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var owner = projectData.Components.First(x => x.Name == "RootWithParam1");
            var target = projectData.Components.First(x => x.Name == "RootWithParam2");

            var ownerParam = (SimDoubleParameter)owner.Parameters.First(x => x.NameTaxonomyEntry.Text == "A");
            var targetParam = (SimDoubleParameter)target.Parameters.First(x => x.NameTaxonomyEntry.Text == "A");

            Assert.AreEqual(0, ownerParam.Value);

            var ref1 = new SimComponentReference(slot, target);
            owner.ReferencedComponents.Add(ref1);

            Assert.AreEqual(15.8, ownerParam.Value);
        }

        [TestMethod]
        public void UpdateParameterOnChangeReference()
        {
            LoadProject(referencesProject, "admin", "admin");

            var slot = new SimSlot(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost)), "a1");
            var owner = projectData.Components.First(x => x.Name == "RootWithParam1");
            var target = projectData.Components.First(x => x.Name == "RootWithParam2");
            var target2 = projectData.Components.First(x => x.Name == "RootWithParam3");

            var ownerParam = (SimDoubleParameter)owner.Parameters.First(x => x.NameTaxonomyEntry.Text == "A");

            var ref1 = new SimComponentReference(slot, target);
            owner.ReferencedComponents.Add(ref1);

            Assert.AreEqual(15.8, ownerParam.Value);

            ref1.Target = target2;

            Assert.AreEqual(123.456, ownerParam.Value);
        }

        #endregion
    }
}
