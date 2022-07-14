using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class SlotTests
    {
        #region Slot Base

        [TestMethod]
        public void SlotBaseCtor()
        {
            SimSlotBase sb = new SimSlotBase();
            Assert.AreEqual(null, sb.Base);

            Assert.ThrowsException<ArgumentNullException>(() => { new SimSlotBase(null); });
            Assert.ThrowsException<ArgumentException>(() => { new SimSlotBase("asdf"); });

            sb = new SimSlotBase(SimDefaultSlots.Cost);
            Assert.AreEqual(SimDefaultSlots.Cost, sb.Base);
        }

        [TestMethod]
        public void SlotBaseInvalid()
        {
            var sb = SimSlotBase.Invalid;

            Assert.AreEqual(null, sb.Base);
        }

        [TestMethod]
        public void SlotBaseEquals()
        {
            var sb1 = new SimSlotBase(SimDefaultSlots.Areas);
            var sb2 = new SimSlotBase(SimDefaultSlots.Areas);
            var sb3 = new SimSlotBase(SimDefaultSlots.Cost);

            Assert.IsTrue(sb1 == sb2);
            Assert.IsFalse(sb1 == sb3);

            sb1 = new SimSlotBase();
            Assert.IsTrue(SimSlotBase.Invalid == sb1);
        }

        [TestMethod]
        public void SlotBaseNotEquals()
        {
            var sb1 = new SimSlotBase(SimDefaultSlots.Areas);
            var sb2 = new SimSlotBase(SimDefaultSlots.Areas);
            var sb3 = new SimSlotBase(SimDefaultSlots.Cost);

            Assert.IsFalse(sb1 != sb2);
            Assert.IsTrue(sb1 != sb3);

            sb1 = new SimSlotBase();
            Assert.IsFalse(SimSlotBase.Invalid != sb1);
        }

        #endregion

        #region Slot

        [TestMethod]
        public void Ctor()
        {
            //Default ctor
            SimSlot slot = new SimSlot();
            Assert.AreEqual(null, slot.SlotBase.Base);
            Assert.AreEqual(null, slot.SlotExtension);

            //Invalid base slot
            Assert.ThrowsException<ArgumentException>(() => { new SimSlot("asdf", ""); });

            //Working
            slot = new SimSlot(SimDefaultSlots.Cost, "3");
            Assert.AreEqual(new SimSlotBase(SimDefaultSlots.Cost), slot.SlotBase);
            Assert.AreEqual("3", slot.SlotExtension);

            //Extension null
            slot = new SimSlot(SimDefaultSlots.Cost, null);
            Assert.AreEqual(new SimSlotBase(SimDefaultSlots.Cost), slot.SlotBase);
            Assert.AreEqual("", slot.SlotExtension);
        }

        [TestMethod]
        public void CtorCopy()
        {
            SimSlot slot = new SimSlot(new SimSlot(SimDefaultSlots.Cost, "4"));
            Assert.AreEqual(new SimSlotBase(SimDefaultSlots.Cost), slot.SlotBase);
            Assert.AreEqual("4", slot.SlotExtension);
        }

        [TestMethod]
        public void SlotInvalid()
        {
            SimSlot s = SimSlot.Invalid;
            Assert.AreEqual(SimSlotBase.Invalid, s.SlotBase);
            Assert.AreEqual(null, s.SlotExtension);
        }

        [TestMethod]
        public void SlotEquals()
        {
            SimSlot a = new SimSlot(SimDefaultSlots.Cost, "4");
            SimSlot b = new SimSlot(SimDefaultSlots.Cost, "4");
            SimSlot c = new SimSlot(SimDefaultSlots.Distributer, "4");
            SimSlot d = new SimSlot(SimDefaultSlots.Cost, "9");

            Assert.IsTrue(a == b);
            Assert.IsFalse(a == c);
            Assert.IsFalse(a == d);
        }

        [TestMethod]
        public void SlotNotEquals()
        {
            SimSlot a = new SimSlot(SimDefaultSlots.Cost, "4");
            SimSlot b = new SimSlot(SimDefaultSlots.Cost, "4");
            SimSlot c = new SimSlot(SimDefaultSlots.Distributer, "4");
            SimSlot d = new SimSlot(SimDefaultSlots.Cost, "9");

            Assert.IsFalse(a != b);
            Assert.IsTrue(a != c);
            Assert.IsTrue(a != d);
        }

        [TestMethod]
        public void ToSerializerString()
        {
            SimSlot slot = new SimSlot(SimDefaultSlots.Composite, "17");

            Assert.AreEqual("Aufbau_017", slot.ToSerializerString());
        }

        [TestMethod]
        public void FromSerializerString()
        {
            Assert.ThrowsException<ArgumentException>(() => SimSlot.FromSerializerString((string)null));
            Assert.ThrowsException<ArgumentException>(() => SimSlot.FromSerializerString(""));
            Assert.ThrowsException<ArgumentException>(() => SimSlot.FromSerializerString("asdf_02"));

            var slot = SimSlot.FromSerializerString(SimDefaultSlots.Cost + "_03");
            Assert.AreEqual(new SimSlotBase(SimDefaultSlots.Cost), slot.SlotBase);
            Assert.AreEqual("3", slot.SlotExtension);

            slot = SimSlot.FromSerializerString(SimDefaultSlots.Cost);
            Assert.AreEqual(new SimSlotBase(SimDefaultSlots.Cost), slot.SlotBase);
            Assert.AreEqual("", slot.SlotExtension);
        }

        #endregion
    }
}
