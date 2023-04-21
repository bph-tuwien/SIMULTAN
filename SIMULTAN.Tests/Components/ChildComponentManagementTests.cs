using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
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
    public class ChildComponentManagementTests : BaseProjectTest
    {
        private static readonly FileInfo emptyProject = new FileInfo(@".\EmptyProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@".\ComponentAccessTestsProject.simultan");

        #region Child Components

        [TestMethod]
        public void ChildComponentAddEntry()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            Assert.AreNotEqual(0, child.LocalID);
            Assert.AreEqual(project, child.Id.Location);
            Assert.IsTrue(root.Components.Contains(childEntry));
            Assert.AreEqual(projectData.Components, child.Factory);
            Assert.AreEqual(root, childEntry.Parent);
            Assert.AreEqual(childEntry, child.ParentContainer);
            Assert.AreEqual(root, child.Parent);

            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, child.Id.LocalId)));
        }

        [TestMethod]
        public void ChildComponentAdd()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);

            var childEntry = new SimChildComponentEntry(childSlot);
            root.Components.Add(childEntry);

            Assert.IsTrue(root.Components.Contains(childEntry));
            Assert.AreEqual(root, childEntry.Parent);
            Assert.AreEqual(null, childEntry.Component);

            childEntry.Component = child;

            Assert.AreNotEqual(0, child.LocalID);
            Assert.AreEqual(project, child.Id.Location);
            Assert.AreEqual(projectData.Components, child.Factory);
            Assert.AreEqual(root, child.Parent);

            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, child.Id.LocalId)));
        }

        [TestMethod]
        public void ChildComponentAddException()
        {
            LoadProject(emptyProject);
            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            //Add null
            Assert.ThrowsException<ArgumentNullException>(() => { root.Components.Add(null); });

            //Add twice
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            var childEntry = new SimChildComponentEntry(childSlot);
            root.Components.Add(childEntry);

            Assert.ThrowsException<ArgumentException>(() => { root.Components.Add(childEntry); });
        }

        [TestMethod]
        public void ChildComponentRemoveEntry()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            var childEntry = new SimChildComponentEntry(childSlot, child);

            root.Components.Add(childEntry);
            var localId = child.Id.LocalId;

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            root.Components.Remove(childEntry);

            Assert.IsFalse(root.Components.Contains(childEntry));
            Assert.IsFalse(root.Components.Any(x => x.Component == child));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, localId)));
            Assert.AreEqual(null, child.Factory);

            Assert.AreEqual(localId, child.Id.LocalId);
            Assert.AreEqual(null, child.Id.Location);

            Assert.AreEqual(null, childEntry.Parent);
            Assert.AreEqual(null, child.ParentContainer);
            Assert.AreEqual(null, child.Parent);

            Assert.AreEqual(1, counter);

            //Remove null entry
            childEntry = new SimChildComponentEntry(new SimSlot(childSlot));
            root.Components.Add(childEntry);
            root.Components.Remove(childEntry);

            Assert.IsFalse(root.Components.Contains(childEntry));
            Assert.AreEqual(null, childEntry.Parent);
        }

        [TestMethod]
        public void ChildComponentRemove()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            var childEntry = new SimChildComponentEntry(childSlot, child);

            root.Components.Add(childEntry);
            var localId = child.Id.LocalId;

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            childEntry.Component = null;

            Assert.AreEqual(root, childEntry.Parent);
            Assert.AreEqual(null, childEntry.Component);

            Assert.IsTrue(root.Components.Contains(childEntry));
            Assert.IsFalse(root.Components.Any(x => x.Component == child));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, localId)));
            Assert.AreEqual(null, child.Factory);

            Assert.AreEqual(localId, child.Id.LocalId);
            Assert.AreEqual(null, child.Id.Location);
            Assert.AreEqual(null, child.ParentContainer);
            Assert.AreEqual(null, child.Parent);

            Assert.AreEqual(1, counter);
        }

        [TestMethod]
        public void ChildComponentReplaceComponent()
        {
            LoadProject(emptyProject);

            //Setup
            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            var oldId = child.Id;

            SimComponent child2 = new SimComponent();
            child2.CurrentSlot = new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined));

            //Wrong slot
            Assert.ThrowsException<ArgumentException>(() => childEntry.Component = child2);

            //Test
            child2.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            childEntry.Component = child2;

            Assert.AreEqual(child2, childEntry.Component);

            //Old
            Assert.AreEqual(oldId.LocalId, child.Id.LocalId);
            Assert.AreEqual(oldId.GlobalId, child.Id.GlobalId);
            Assert.AreEqual(null, child.Id.Location);
            Assert.AreEqual(null, child.Factory);
            Assert.AreEqual(null, child.ParentContainer);
            Assert.AreEqual(null, child.Parent);

            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, child.Id.LocalId)));


            //New
            Assert.AreNotEqual(0, child2.LocalID);
            Assert.AreNotEqual(oldId.LocalId, child2.LocalID);
            Assert.AreEqual(project, child2.Id.Location);
            Assert.AreEqual(projectData.Components, child2.Factory);
            Assert.AreEqual(childEntry, child2.ParentContainer);
            Assert.AreEqual(root, child2.Parent);

            Assert.AreEqual(child2, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, child2.Id.LocalId)));
        }

        [TestMethod]
        public void ChildComponentReplaceEntry()
        {
            LoadProject(emptyProject);

            //Setup
            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            var oldId = child.Id;

            //Test

            SimComponent child2 = new SimComponent();
            var child2Slot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint), "4");
            child2.CurrentSlot = new SimTaxonomyEntryReference(child2Slot.SlotBase);
            SimChildComponentEntry child2Entry = new SimChildComponentEntry(child2Slot, child2);

            int index = root.Components.IndexOf(childEntry);
            root.Components[index] = child2Entry;

            Assert.AreEqual(child2, child2Entry.Component);

            //Old
            Assert.AreEqual(oldId.LocalId, child.Id.LocalId);
            Assert.AreEqual(oldId.GlobalId, child.Id.GlobalId);
            Assert.AreEqual(null, child.Id.Location);
            Assert.AreEqual(null, child.Factory);
            Assert.AreEqual(null, child.ParentContainer);
            Assert.AreEqual(null, child.Parent);
            Assert.AreEqual(null, childEntry.Parent);
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, child.Id.LocalId)));


            //New
            Assert.AreNotEqual(0, child2.LocalID);
            Assert.AreNotEqual(oldId.LocalId, child2.LocalID);
            Assert.AreEqual(project, child2.Id.Location);
            Assert.AreEqual(projectData.Components, child2.Factory);
            Assert.AreEqual(child2Entry, child2.ParentContainer);
            Assert.AreEqual(root, child2.Parent);
            Assert.AreEqual(root, child2Entry.Parent);
            Assert.AreEqual(child2, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, child2.Id.LocalId)));
        }

        [TestMethod]
        public void ChildComponentClear()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent[] children = new SimComponent[]
            {
                new SimComponent(),
                null,
                new SimComponent(),
            };
            SimSlot[] childSlots = new SimSlot[]
            {
                new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3"),
                new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "4"),
                new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Areas), "2"),
            };

            for (int i = 0; i < children.Length; ++i)
                if (children[i] != null)
                    children[i].CurrentSlot = new SimTaxonomyEntryReference(childSlots[i].SlotBase);

            var childEntries = children.Select((x, xi) => new SimChildComponentEntry(childSlots[xi], children[xi])).ToArray();

            root.Components.AddRange(childEntries);

            var ids = children.Select(x => x == null ? -1 : x.Id.LocalId).ToArray();

            int counter = 0;
            children.Where(x => x != null).ForEach(x => x.IsBeingDeleted += (s) => counter++);

            root.Components.Clear();

            for (int i = 0; i < children.Length; ++i)
            {
                Assert.IsFalse(root.Components.Contains(childEntries[i]));
                Assert.IsFalse(root.Components.Any(x => x.Component == children[i]));

                Assert.AreEqual(null, childEntries[i].Parent);

                if (children[i] != null)
                {
                    Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, ids[i])));
                    Assert.AreEqual(null, children[i].Factory);

                    Assert.AreEqual(ids[i], children[i].Id.LocalId);
                    Assert.AreEqual(null, children[i].Id.Location);

                    Assert.AreEqual(null, children[i].ParentContainer);
                    Assert.AreEqual(null, children[i].Parent);
                }
            }

            Assert.AreEqual(2, counter);
        }

        [TestMethod]
        public void ChildComponentRemoveWithoutDelete()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);


            var startId = child.Id;
            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            root.Components.RemoveWithoutDelete(childEntry);

            Assert.IsFalse(root.Components.Contains(childEntry));
            Assert.IsFalse(root.Components.Any(x => x.Component == child));
            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(new SimId(project, startId.LocalId)));
            Assert.AreEqual(projectData.Components, child.Factory);

            Assert.AreEqual(startId.LocalId, child.Id.LocalId);
            Assert.AreEqual(startId.Location, child.Id.Location);

            Assert.AreEqual(null, childEntry.Parent);
            Assert.AreEqual(childEntry, child.ParentContainer);
            Assert.AreEqual(null, child.Parent);

            Assert.AreEqual(0, counter);
        }

        #endregion

        #region Slots

        [TestMethod]
        public void ChildComponentModifyCurrentSlot()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var costTax = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);
            var jointTax = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint);
            var childSlot = new SimSlot(costTax, "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            Assert.AreEqual(costTax, childEntry.Slot.SlotBase.Target);
            Assert.AreEqual("3", childEntry.Slot.SlotExtension);
            Assert.AreEqual(costTax, child.CurrentSlot.Target);

            PropertyChangedEventCounter childPC = new PropertyChangedEventCounter(child);
            PropertyChangedEventCounter entryPC = new PropertyChangedEventCounter(childEntry);

            child.CurrentSlot = new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint));

            Assert.AreEqual(jointTax, childEntry.Slot.SlotBase.Target);
            Assert.AreEqual("3", childEntry.Slot.SlotExtension);
            Assert.AreEqual(jointTax, child.CurrentSlot.Target);

            childPC.AssertEventCount(1);
            entryPC.AssertEventCount(1);
        }

        [TestMethod]
        public void ChildComponentModifyContainerSlot()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var costTax = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);
            var jointTax = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint);
            var childSlot = new SimSlot(costTax, "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            Assert.AreEqual(costTax, childEntry.Slot.SlotBase.Target);
            Assert.AreEqual("3", childEntry.Slot.SlotExtension);
            Assert.AreEqual(costTax, child.CurrentSlot.Target);

            PropertyChangedEventCounter childPC = new PropertyChangedEventCounter(child);
            PropertyChangedEventCounter entryPC = new PropertyChangedEventCounter(childEntry);

            childEntry.Slot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint), "4");

            Assert.AreEqual(jointTax, childEntry.Slot.SlotBase.Target);
            Assert.AreEqual("4", childEntry.Slot.SlotExtension);
            Assert.AreEqual(jointTax, child.CurrentSlot.Target);

            childPC.AssertEventCount(1);
            entryPC.AssertEventCount(1);
        }

        [TestMethod]
        public void ChildComponentInvalidSlot()
        {
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Calculation));

            Assert.ThrowsException<ArgumentException>(() => { new SimChildComponentEntry(childSlot, child); });
        }

        #endregion


        #region Access

        [TestMethod]
        public void ChildComponentAddEntryAccess()
        {
            //User needs write access to parent for adding
            LoadProject(accessProject, "bph", "bph");

            //Working case
            {
                var root = projectData.Components.First(x => x.Name == "BPHRoot");

                var slot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "4");
                var comp = new SimComponent();
                comp.CurrentSlot = new SimTaxonomyEntryReference(slot.SlotBase);

                var entry = new SimChildComponentEntry(slot, comp);
                root.Components.Add(entry);

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));
            }

            //No access
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");

                var slot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "4");
                var comp = new SimComponent();
                comp.CurrentSlot = new SimTaxonomyEntryReference(slot.SlotBase);

                var entry = new SimChildComponentEntry(slot, comp);

                var startAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                Assert.ThrowsException<AccessDeniedException>(() => root.Components.Add(entry));

                Assert.AreEqual(null, entry.Parent);
                Assert.AreEqual(null, comp.Parent);
                Assert.AreEqual(null, comp.ParentContainer);

                Assert.IsFalse(root.Components.Contains(entry));

                Assert.AreEqual(DateTime.MinValue, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.AreEqual(startAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            //No access, empty entry
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");

                var slot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "4");
                var entry = new SimChildComponentEntry(slot);

                var startAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                Assert.ThrowsException<AccessDeniedException>(() => root.Components.Add(entry));

                Assert.AreEqual(null, entry.Parent);

                Assert.IsFalse(root.Components.Contains(entry));

                Assert.AreEqual(startAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        [TestMethod]
        public void ChildComponentAddAccess()
        {
            //User needs write access to parent for adding
            LoadProject(accessProject, "bph", "bph");

            //Working case
            {
                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.First(x => x.Component == null);

                var comp = new SimComponent();
                comp.CurrentSlot = new SimTaxonomyEntryReference(entry.Slot.SlotBase);

                entry.Component = comp;

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));
            }

            //No access to parent
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");
                var entry = root.Components.First(x => x.Component == null);

                var comp = new SimComponent();
                comp.CurrentSlot = new SimTaxonomyEntryReference(entry.Slot.SlotBase);

                var startAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                Assert.ThrowsException<AccessDeniedException>(() => entry.Component = comp);

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(null, comp.Parent);
                Assert.AreEqual(null, comp.ParentContainer);
                Assert.AreEqual(null, entry.Component);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(startAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(DateTime.MinValue, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        [TestMethod]
        public void ChildComponentRemoveEntryAccess()
        {
            //User needs write access to parent and child to remove it
            LoadProject(accessProject, "bph", "bph");

            //Everything working
            {
                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                root.Components.Remove(entry);

                Assert.AreEqual(null, entry.Parent);
                Assert.AreEqual(null, comp.Parent);
                Assert.AreEqual(null, comp.ParentContainer);

                Assert.IsFalse(root.Components.Contains(entry));
            }

            //No access to child
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "ArchChild");
                var comp = entry.Component;

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsFalse(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components.Remove(entry));

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            //No access to parent
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                Assert.IsFalse(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components.Remove(entry));

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            //Remove empty, no access to parent
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component == null);

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                Assert.IsFalse(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components.Remove(entry));

                Assert.AreEqual(root, entry.Parent);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        [TestMethod]
        public void ChildComponentRemoveComponentAccess()
        {
            //User needs write access to parent and child to remove it
            LoadProject(accessProject, "bph", "bph");

            //Everything working
            {
                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.First(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                entry.Component = null;

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(null, entry.Component);
                Assert.AreEqual(null, comp.Parent);
                Assert.AreEqual(null, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));
            }

            //No access to child
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.First(x => x.Component != null && x.Component.Name == "ArchChild");
                var comp = entry.Component;

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsFalse(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => entry.Component = null);

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(comp, entry.Component);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            //No access to parent
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");
                var entry = root.Components.First(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                Assert.IsFalse(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => entry.Component = null);

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(comp, entry.Component);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        [TestMethod]
        public void ChildComponentReplaceEntryAccess()
        {
            //User needs write access to parent and child to remove it
            LoadProject(accessProject, "bph", "bph");

            //Everything working
            {
                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;
                var index = root.Components.IndexOf(entry);

                var newSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Regulation), "argh");
                var newComp = new SimComponent()
                {
                    CurrentSlot = new SimTaxonomyEntryReference(newSlot.SlotBase)
                };
                var newEntry = new SimChildComponentEntry(newSlot, newComp);

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                root.Components[index] = newEntry;

                Assert.AreEqual(null, entry.Parent);
                Assert.AreEqual(null, comp.Parent);
                Assert.AreEqual(null, comp.ParentContainer);

                Assert.IsFalse(root.Components.Contains(entry));

                Assert.AreEqual(root, newEntry.Parent);
                Assert.AreEqual(root, newComp.Parent);
                Assert.AreEqual(newEntry, newComp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(newEntry));
            }

            //No access to child
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "ArchChild");
                var comp = entry.Component;
                var index = root.Components.IndexOf(entry);

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                var newSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Regulation), "argh");
                var newComp = new SimComponent()
                {
                    CurrentSlot = new SimTaxonomyEntryReference(newSlot.SlotBase)
                };
                var newEntry = new SimChildComponentEntry(newSlot, newComp);

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsFalse(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components[index] = newEntry);

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(null, newEntry.Parent);
                Assert.AreEqual(null, newComp.Parent);
                Assert.AreEqual(null, newComp.ParentContainer);

                Assert.IsFalse(root.Components.Contains(newEntry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(DateTime.MinValue, newComp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            //No access to parent
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;
                var index = root.Components.IndexOf(entry);

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                var newSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Regulation), "argh");
                var newComp = new SimComponent()
                {
                    CurrentSlot = new SimTaxonomyEntryReference(newSlot.SlotBase)
                };
                var newEntry = new SimChildComponentEntry(newSlot, newComp);

                Assert.IsFalse(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components[index] = newEntry);

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(null, newEntry.Parent);
                Assert.AreEqual(null, newComp.Parent);
                Assert.AreEqual(null, newComp.ParentContainer);

                Assert.IsFalse(root.Components.Contains(newEntry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(DateTime.MinValue, newComp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        [TestMethod]
        public void ChildComponentReplaceComponentAccess()
        {
            //User needs write access to parent and child to remove it
            LoadProject(accessProject, "bph", "bph");

            //Everything working
            {
                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;
                var index = root.Components.IndexOf(entry);

                var newComp = new SimComponent()
                {
                    CurrentSlot = new SimTaxonomyEntryReference(entry.Slot.SlotBase)
                };

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                entry.Component = newComp;

                Assert.AreEqual(null, comp.Parent);
                Assert.AreEqual(null, comp.ParentContainer);

                Assert.AreEqual(root, entry.Parent);
                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(root, newComp.Parent);
                Assert.AreEqual(entry, newComp.ParentContainer);
            }

            //No access to child
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "ArchChild");
                var comp = entry.Component;
                var index = root.Components.IndexOf(entry);

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                var newComp = new SimComponent()
                {
                    CurrentSlot = new SimTaxonomyEntryReference(entry.Slot.SlotBase)
                };

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsFalse(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => entry.Component = newComp);

                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.AreEqual(root, entry.Parent);
                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(null, newComp.Parent);
                Assert.AreEqual(null, newComp.ParentContainer);

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(DateTime.MinValue, newComp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            //No access to parent
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;
                var index = root.Components.IndexOf(entry);

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                var newComp = new SimComponent()
                {
                    CurrentSlot = new SimTaxonomyEntryReference(entry.Slot.SlotBase)
                };

                Assert.IsFalse(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => entry.Component = newComp);

                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.AreEqual(root, entry.Parent);
                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(null, newComp.Parent);
                Assert.AreEqual(null, newComp.ParentContainer);

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(DateTime.MinValue, newComp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        [TestMethod]
        public void ChildComponentClearAccess()
        {
            //User needs write access to parent and child to remove it
            LoadProject(accessProject, "bph", "bph");

            //Everything working
            {
                var root = projectData.Components.First(x => x.Name == "BPHRoot");

                using (AccessCheckingDisabler.Disable(projectData.Components))
                {
                    var archEntry = root.Components.FirstOrDefault(x => x.Component.Name == "ArchChild");
                    archEntry.Component.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
                }

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                foreach (var ce in root.Components.Where(x => x.Component != null))
                    Assert.IsTrue(ce.Component.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                root.Components.Clear();

                Assert.AreEqual(0, root.Components.Count);
            }

            ProjectUtils.CleanupTestData(ref project, ref projectData);
            LoadProject(accessProject, "bph", "bph");

            //No access to child
            {
                projectData.Components.ResetChanges();
                var root = projectData.Components.First(x => x.Name == "BPHRoot");

                var startAccess = root.Components.Where(x => x.Component != null).ToDictionary(x => x.Component,
                    x => x.Component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(root.Components.Any(x => x.Component != null &&
                    x.Component.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write)));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components.Clear());


                Assert.AreEqual(3, root.Components.Count);
                foreach (var ce in root.Components.Where(x => x.Component != null))
                {
                    Assert.AreEqual(startAccess[ce.Component], ce.Component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                }
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            ProjectUtils.CleanupTestData(ref project, ref projectData);
            LoadProject(accessProject, "bph", "bph");

            //No access to root
            {
                projectData.Components.ResetChanges();
                var root = projectData.Components.First(x => x.Name == "ArchRoot");

                using (AccessCheckingDisabler.Disable(projectData.Components))
                {
                    var archEntry = root.Components.FirstOrDefault(x => x.Component.Name == "ArchChild");
                    archEntry.Component.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
                }

                var startAccess = root.Components.Where(x => x.Component != null).ToDictionary(x => x.Component,
                    x => x.Component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));

                Assert.IsFalse(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(root.Components.All(x => x.Component == null ||
                    x.Component.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write)));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components.Clear());


                Assert.AreEqual(3, root.Components.Count);
                foreach (var ce in root.Components.Where(x => x.Component != null))
                {
                    Assert.AreEqual(startAccess[ce.Component], ce.Component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                }
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        [TestMethod]
        public void ChildComponentRemoveWithoutDeleteAccess()
        {
            //User needs write access to parent and child to remove it
            LoadProject(accessProject, "bph", "bph");

            //User needs write access to parent and child to remove it
            LoadProject(accessProject, "bph", "bph");

            //Everything working
            {
                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                root.Components.RemoveWithoutDelete(entry);

                Assert.AreEqual(null, entry.Parent);
                Assert.AreEqual(null, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsFalse(root.Components.Contains(entry));
            }

            //No access to child
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "BPHRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "ArchChild");
                var comp = entry.Component;

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                Assert.IsTrue(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsFalse(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components.RemoveWithoutDelete(entry));

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            //No access to parent
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component.Name == "BPHChild");
                var comp = entry.Component;

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
                var compStartAccess = comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                Assert.IsFalse(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));
                Assert.IsTrue(comp.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components.RemoveWithoutDelete(entry));

                Assert.AreEqual(root, entry.Parent);
                Assert.AreEqual(root, comp.Parent);
                Assert.AreEqual(entry, comp.ParentContainer);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.AreEqual(compStartAccess, comp.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }

            //Remove empty, no access to parent
            {
                projectData.Components.ResetChanges();

                var root = projectData.Components.First(x => x.Name == "ArchRoot");
                var entry = root.Components.FirstOrDefault(x => x.Component == null);

                var rootStartAccess = root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

                Assert.IsFalse(root.HasAccess(projectData.UsersManager.CurrentUser, SimComponentAccessPrivilege.Write));

                Assert.ThrowsException<AccessDeniedException>(() => root.Components.RemoveWithoutDelete(entry));

                Assert.AreEqual(root, entry.Parent);

                Assert.IsTrue(root.Components.Contains(entry));

                Assert.AreEqual(rootStartAccess, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write));
                Assert.IsFalse(projectData.Components.HasChanges);
            }
        }

        #endregion

        #region LastChanged

        [TestMethod]
        public void ChildComponentAddEntryChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            SimComponent root = new SimComponent();
            root.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);

            var startTime = DateTime.Now;

            Thread.Sleep(5);
            projectData.Components.ResetChanges();

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startTime);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);
        }

        [TestMethod]
        public void ChildComponentAddComponentChanges()
        {
            //Setup
            LoadProject(emptyProject, "bph", "bph");

            SimComponent root = new SimComponent();
            root.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);

            var childEntry = new SimChildComponentEntry(childSlot);
            root.Components.Add(childEntry);

            projectData.Components.ResetChanges();
            var startTime = DateTime.Now;

            Thread.Sleep(5);

            //Action
            childEntry.Component = child;

            //Test
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startTime);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);
        }

        [TestMethod]
        public void ChildComponentRemoveEntryChanges()
        {
            //Setup
            LoadProject(emptyProject, "bph", "bph");

            SimComponent root = new SimComponent();
            root.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            child.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            var childEntry = new SimChildComponentEntry(childSlot, child);

            root.Components.Add(childEntry);

            projectData.Components.ResetChanges();
            var startTime = DateTime.Now;

            Thread.Sleep(5);

            //Action
            root.Components.Remove(childEntry);

            //Test
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startTime);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);
        }

        [TestMethod]
        public void ChildComponentRemoveComponentChanges()
        {
            //Setup
            LoadProject(emptyProject, "bph", "bph");

            SimComponent root = new SimComponent();
            root.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            child.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            var childEntry = new SimChildComponentEntry(childSlot, child);

            root.Components.Add(childEntry);

            projectData.Components.ResetChanges();
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            //Action
            childEntry.Component = null;

            //Test
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startTime);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);
        }

        [TestMethod]
        public void ChildComponentReplaceEntryChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            SimComponent root = new SimComponent();
            root.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            child.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            SimComponent child2 = new SimComponent();
            var child2Slot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint), "4");
            child2.CurrentSlot = new SimTaxonomyEntryReference(child2Slot.SlotBase);
            SimChildComponentEntry child2Entry = new SimChildComponentEntry(child2Slot, child2);

            int index = root.Components.IndexOf(childEntry);

            projectData.Components.ResetChanges();
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            //Action
            root.Components[index] = child2Entry;

            //Tests
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(child2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startTime);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);
        }

        [TestMethod]
        public void ChildComponentReplaceComponentChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            //Setup
            SimComponent root = new SimComponent();
            root.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            child.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            SimComponent child2 = new SimComponent();
            child2.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            child2.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;

            projectData.Components.ResetChanges();
            var startTime = DateTime.Now;

            Thread.Sleep(5);

            //Action
            childEntry.Component = child2;

            //Test
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(child2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child2.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startTime);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);
        }

        [TestMethod]
        public void ChildComponentClearChanges()
        {
            LoadProject(emptyProject, "bph", "bph");

            SimComponent root = new SimComponent();
            root.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(root);

            SimComponent[] children = new SimComponent[]
            {
                new SimComponent(),
                null,
                new SimComponent(),
            };
            SimSlot[] childSlots = new SimSlot[]
            {
                new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3"),
                new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "4"),
                new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Areas), "2"),
            };

            for (int i = 0; i < children.Length; ++i)
            {
                if (children[i] != null)
                {
                    children[i].CurrentSlot = new SimTaxonomyEntryReference(childSlots[i].SlotBase);
                    children[i].AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
                }
            }

            var childEntries = children.Select((x, xi) => new SimChildComponentEntry(childSlots[xi], children[xi])).ToArray();

            root.Components.AddRange(childEntries);

            projectData.Components.ResetChanges();
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            //Action
            root.Components.Clear();

            //Tests
            foreach (var child in children.Where(x => x != null))
            {
                Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
                Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
                Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);
            }

            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startTime);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);
        }

        [TestMethod]
        public void ChildComponentRemoveWithoutDeleteChanges()
        {
            //Setup
            LoadProject(emptyProject, "bph", "bph");

            SimComponent root = new SimComponent();
            root.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            child.AccessLocal[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            var childSlot = new SimSlot(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost), "3");
            child.CurrentSlot = new SimTaxonomyEntryReference(childSlot.SlotBase);
            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            projectData.Components.ResetChanges();
            var startTime = DateTime.Now;
            Thread.Sleep(5);

            //Action
            root.Components.RemoveWithoutDelete(childEntry);

            //Test
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, child.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess > startTime);
            Assert.IsTrue(root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).lastAccess <= DateTime.Now);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, root.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write).role);

            Assert.IsTrue(projectData.Components.HasChanges);
            Assert.IsTrue(projectData.Components.LastChange > startTime);
            Assert.IsTrue(projectData.Components.LastChange <= DateTime.Now);
        }

        #endregion
    }
}
