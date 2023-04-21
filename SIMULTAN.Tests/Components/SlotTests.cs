using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Tests.TestUtils;
using System;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class SlotTests
    {

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
        public void ToSerializerString()
        {
            SimSlot slot = new SimSlot(TaxonomyUtils.GetDefaultSlot(SimDefaultSlotKeys.Composite), "17");

            Assert.AreEqual("Aufbau_017", slot.ToSerializerString());
        }

        #endregion
    }
}
