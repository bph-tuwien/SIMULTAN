using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using TaxonomyUtils = SIMULTAN.Tests.TestUtils.TaxonomyUtils;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class SlotTests : BaseProjectTest
    {

        private static readonly FileInfo slotsTestProject = new FileInfo(@"./SlotsTestProject.simultan");

        #region Slot

        [TestMethod]
        public void Ctor()
        {
            //Default ctor
            SimSlot slot = new SimSlot();
            Assert.AreEqual(null, slot.SlotBase);
            Assert.AreEqual(null, slot.SlotExtension);

            //Invalid base slot
            Assert.ThrowsException<ArgumentNullException>(() => { new SimSlot((SimTaxonomyEntryReference)null, ""); });

            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var costTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);

            //Working
            slot = new SimSlot(costTax, "3");
            Assert.AreEqual(costTax, slot.SlotBase.Target);
            Assert.AreEqual("3", slot.SlotExtension);

            //Extension null
            slot = new SimSlot(costTax, null);
            Assert.AreEqual(costTax, slot.SlotBase.Target);
            Assert.AreEqual("", slot.SlotExtension);
        }

        [TestMethod]
        public void CtorCopy()
        {
            var slotEntry = TaxonomyUtils.GetDefaultSlot(SimDefaultSlotKeys.Cost);
            SimSlot baseSlot = new SimSlot(slotEntry, "4");
            SimSlot slot = new SimSlot(baseSlot);
            Assert.AreEqual(slotEntry, slot.SlotBase.Target);
            Assert.AreEqual("4", slot.SlotExtension);
            Assert.IsFalse(object.ReferenceEquals(baseSlot.SlotBase, slot.SlotBase)); // should create new reference
        }

        [TestMethod]
        public void SlotInvalid()
        {
            SimSlot s = SimSlot.Invalid;
            Assert.AreEqual(null, s.SlotBase);
            Assert.AreEqual(null, s.SlotExtension);
        }

        [TestMethod]
        public void SlotEquals()
        {
            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var costTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);
            var distributerTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Distribution);
            SimSlot a = new SimSlot(costTax, "4");
            SimSlot b = new SimSlot(costTax, "4");
            SimSlot c = new SimSlot(distributerTax, "4");
            SimSlot d = new SimSlot(costTax, "9");

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == d);
        }

        [TestMethod]
        public void SlotNotEquals()
        {
            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var costTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);
            var distributerTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Distribution);
            SimSlot a = new SimSlot(costTax, "4");
            SimSlot b = new SimSlot(costTax, "4");
            SimSlot c = new SimSlot(distributerTax, "4");
            SimSlot d = new SimSlot(costTax, "9");

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != d);
        }

        [TestMethod]
        public void ChildDoesNothavePlaceholdersSlot()
        {
            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var jointTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint);
            var root = new SimComponent();
            root.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            root.Components.Add(new SimChildComponentEntry(new SimSlot(jointTax, "0")));
            var childComponent = new SimComponent();

            Assert.ThrowsException<ArgumentException>(() => { root.Components[0].Component = childComponent; });

        }


        [TestMethod]
        public void CanNotDeleteMainSlottest()
        {
            LoadProject(slotsTestProject);

            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var jointTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint);
            var costTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);

            var root = new SimComponent();
            root.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            var childComponent = new SimComponent();
            childComponent.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            childComponent.Slots.Add(new SimTaxonomyEntryReference(costTax));
            root.Components.Add(new SimChildComponentEntry(new SimSlot(jointTax, "0"), childComponent));
            projectData.Components.Add(root);

            Assert.ThrowsException<NotSupportedException>(() =>
            {
                childComponent.Slots.Remove(childComponent.Slots.FirstOrDefault(t => t.Target == jointTax)); 
            });
        }



        [TestMethod]
        public void CanNotClearSlotsCollection()
        {
            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var jointTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint);
            var costTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);


            var root = new SimComponent();
            root.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            var childComponent = new SimComponent();
            childComponent.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            childComponent.Slots.Add(new SimTaxonomyEntryReference(costTax));
            root.Components.Add(new SimChildComponentEntry(new SimSlot(jointTax, "0"), childComponent));

            Assert.ThrowsException<NotSupportedException>(()
                =>
            { childComponent.Slots.Clear(); });
        }




        [TestMethod]
        public void AddComponentToAChildEntry()
        {
            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var jointTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint);
            var costTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);


            var root = new SimComponent();
            root.Slots.Add(new SimTaxonomyEntryReference(jointTax));

            var childComponent = new SimComponent();

            childComponent.Slots.Add(new SimTaxonomyEntryReference(costTax));
            var entry = new SimChildComponentEntry(new SimSlot(jointTax, "0"));
            root.Components.Add(entry);

            Assert.ThrowsException<ArgumentException>(()
                =>
            { entry.Component = childComponent; }); // should throw exception, becasue childComponet does not contain the joinTax


            childComponent.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            entry.Component = childComponent;
            Assert.AreEqual(entry.Component, childComponent);
        }


        [TestMethod]
        public void SlotsCollectionHasChanges()
        {
            LoadProject(slotsTestProject, "admin", "admin");

            var taxonomies = projectData.Taxonomies;
            var jointTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint);
            var costTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);


            var root = new SimComponent();
            root.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            projectData.Components.Add(root);
            var childComponent = new SimComponent();

            Assert.IsTrue(projectData.Components.HasChanges);
            projectData.Components.ResetChanges();
            Assert.IsFalse(projectData.Components.HasChanges);

            childComponent.Slots.Add(new SimTaxonomyEntryReference(costTax));
            childComponent.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            root.Components.Add(new SimChildComponentEntry(new SimSlot(jointTax, "0"), childComponent));
            Assert.IsTrue(projectData.Components.HasChanges);
            projectData.Components.ResetChanges();
            Assert.IsFalse(projectData.Components.HasChanges);



            Assert.ThrowsException<NotSupportedException>
                (() => { childComponent.Slots.RemoveWhere(t => t.Target == jointTax); }); //should not be able to remove the parent container´s slot


            var childEntry = new SimChildComponentEntry(new SimSlot(jointTax, "0"));
            root.Components.Add(childEntry);
            Assert.IsTrue(projectData.Components.HasChanges);
            projectData.Components.ResetChanges();
            Assert.IsFalse(projectData.Components.HasChanges);


            var otherChildComp = new SimComponent();
            otherChildComp.Slots.Add(new SimTaxonomyEntryReference(costTax));
            otherChildComp.Slots.Add(new SimTaxonomyEntryReference(jointTax));
            projectData.Components.Add(otherChildComp);
            Assert.IsTrue(projectData.Components.HasChanges);
            projectData.Components.ResetChanges();
            Assert.IsFalse(projectData.Components.HasChanges);


            childEntry.Component = otherChildComp;
            Assert.IsTrue(projectData.Components.HasChanges);
            projectData.Components.ResetChanges();
            Assert.IsFalse(projectData.Components.HasChanges);


            childEntry.Slot = new SimSlot(costTax, "0");
            otherChildComp.Slots.RemoveWhere(t => t.Target == jointTax);

        }

        [TestMethod]
        public void TrySetMainSlot()
        {
            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var jointTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Joint);
            var costTax = taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Cost);

            var root = new SimComponent();
            var childComponent = new SimComponent();
            childComponent.Slots.Add(new SimTaxonomyEntryReference(jointTax));

            root.Components.Add(new SimChildComponentEntry(new SimSlot(jointTax, "0"), childComponent));

            Assert.ThrowsException<NotSupportedException>
              (() => { childComponent.Slots[0] = new SimTaxonomyEntryReference(costTax); });

        }

        [TestMethod]
        public void InvalidTaxonomyEntryShouldThrowException()
        {
            LoadProject(slotsTestProject, "admin", "admin");
            TaxonomyUtils.LoadDefaultTaxonomies(projectData);

            var entry = new SimTaxonomyEntry("entry", "test");
            Assert.ThrowsException<Exception>(() =>
            {
                new SimSlot(new SimTaxonomyEntryReference(entry), "0");
            });

            var root = new SimComponent();
            Assert.ThrowsException<Exception>(() =>
            {
                root.Slots.Add(new SimTaxonomyEntryReference(entry));
            });

            root.Slots.Add(new SimTaxonomyEntryReference(projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined)));

            Assert.ThrowsException<Exception>(() =>
            {
                root.Slots[0] = new SimTaxonomyEntryReference(entry);
            });


        }

        #endregion
    }
}
