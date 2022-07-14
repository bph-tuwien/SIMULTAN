using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class ReferenceTests : BaseProjectTest
    {
        private static readonly FileInfo referencesProject = new FileInfo(@".\ReferencesTestProject.simultan");

        #region Ctor

        [TestMethod]
        public void CtorNoTarget()
        {
            var slot = new SimSlot(new SimSlotBase(SimDefaultSlots.Cost), "a1");
            var guid = Guid.NewGuid();

            SimComponentReference ref1 = new SimComponentReference(slot);
            Assert.AreEqual(slot, ref1.Slot);
            Assert.AreEqual(SimId.Empty, ref1.TargetId);
            Assert.AreEqual(null, ref1.Target);

            ref1 = new SimComponentReference(slot, new SimId(guid, 100));
            Assert.AreEqual(slot, ref1.Slot);
            Assert.AreEqual(guid, ref1.TargetId.GlobalId);
            Assert.AreEqual(100, ref1.TargetId.LocalId);
            Assert.AreEqual(null, ref1.Target);
        }

        [TestMethod]
        public void CtorWithTarget()
        {
            LoadProject(referencesProject, "admin", "admin");

            var slot = new SimSlot(new SimSlotBase(SimDefaultSlots.Cost), "a1");
            var target = projectData.Components.First(x => x.Name == "Root1");

            var ref1 = new SimComponentReference(slot, target);
            Assert.AreEqual(slot, ref1.Slot);
            Assert.AreEqual(target.Id, ref1.TargetId);
            Assert.AreEqual(target, ref1.Target);
        }

        #endregion

        #region Properties

        [TestMethod]
        public void Target()
        {
            LoadProject(referencesProject, "admin", "admin");

            var slot = new SimSlot(new SimSlotBase(SimDefaultSlots.Cost), "a1");
            var target1 = projectData.Components.First(x => x.Name == "Root1");
            var target2 = target1.Components.First(x => x.Component.Name == "Child1").Component;

            var ref1 = new SimComponentReference(slot, target1);
            Assert.AreEqual(target1.Id, ref1.TargetId);
            Assert.AreEqual(target1, ref1.Target);

            ref1.Target = target2;
            Assert.AreEqual(target2.Id, ref1.TargetId);
            Assert.AreEqual(target2, ref1.Target);
        }

        [TestMethod]
        public void Slot()
        {
            LoadProject(referencesProject, "admin", "admin");

            var slot = new SimSlot(new SimSlotBase(SimDefaultSlots.Cost), "a1");
            var target1 = projectData.Components.First(x => x.Name == "Root1");

            var ref1 = new SimComponentReference(slot, target1);
            Assert.AreEqual(slot, ref1.Slot);

            var slot2 = new SimSlot(new SimSlotBase(SimDefaultSlots.Joint), "b2");
            ref1.Slot = slot2;
            Assert.AreEqual(slot2, ref1.Slot);
        }

        #endregion
    }
}
