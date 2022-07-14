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
    public class ComponentInteractionTests : BaseProjectTest
    {
        private static readonly FileInfo emptyProject = new FileInfo(@".\EmptyProject.simultan");

        #region Move

        [TestMethod]
        public void RemoveAddEntry()
        {
            //Setup
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(SimDefaultSlots.Cost, "3");
            child.CurrentSlot = childSlot.SlotBase;

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            SimComponent root2 = new SimComponent();
            projectData.Components.Add(root2);

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            var startId = child.Id;

            //Action
            root.Components.Remove(childEntry);
            childEntry.Component.Id = SimId.Empty;
            root2.Components.Add(childEntry);

            //Tests
            Assert.AreEqual(root2, childEntry.Parent);
            Assert.AreEqual(root2, child.Parent);
            Assert.AreEqual(childEntry, child.ParentContainer);

            Assert.AreEqual(1, counter);

            Assert.IsTrue(root2.Components.Contains(childEntry));
            Assert.IsFalse(root.Components.Contains(childEntry));

            Assert.AreNotEqual(startId, child.Id);
            Assert.AreNotEqual(SimId.Empty, child.Id);
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimComponent>(startId));
        }

        [TestMethod]
        public void MoveSubEntry()
        {
            //Setup
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(SimDefaultSlots.Cost, "3");
            child.CurrentSlot = childSlot.SlotBase;

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            SimComponent root2 = new SimComponent();
            projectData.Components.Add(root2);

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            var startId = child.Id;

            //Action
            root2.Components.Add(childEntry);

            //Tests
            Assert.AreEqual(root2, childEntry.Parent);
            Assert.AreEqual(root2, child.Parent);
            Assert.AreEqual(childEntry, child.ParentContainer);

            Assert.AreEqual(0, counter);

            Assert.IsTrue(root2.Components.Contains(childEntry));
            Assert.IsFalse(root.Components.Contains(childEntry));

            Assert.AreEqual(startId, child.Id);
            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(startId));
        }

        [TestMethod]
        public void MoveSubComponenToExistingSubEntry()
        {
            //Setup
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(SimDefaultSlots.Cost, "3");
            child.CurrentSlot = childSlot.SlotBase;

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            SimComponent root2 = new SimComponent();
            projectData.Components.Add(root2);

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            var startId = child.Id;

            //Action
            var childEntry2 = new SimChildComponentEntry(new SimSlot(SimDefaultSlots.Cost, "4"));
            root2.Components.Add(childEntry2);
            childEntry2.Component = child;

            //Tests
            Assert.AreEqual(root, childEntry.Parent);
            Assert.AreEqual(root2, childEntry2.Parent);
            Assert.AreEqual(root2, child.Parent);
            Assert.AreEqual(childEntry2, child.ParentContainer);

            Assert.AreEqual(0, counter);

            Assert.IsTrue(root2.Components.Contains(childEntry2));
            Assert.IsTrue(root.Components.Contains(childEntry));

            Assert.AreEqual(startId, child.Id);
            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(startId));
        }

        [TestMethod]
        public void MoveSubComponentToNewSubEntry()
        {
            //Setup
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(SimDefaultSlots.Cost, "3");
            child.CurrentSlot = childSlot.SlotBase;

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            SimComponent root2 = new SimComponent();
            projectData.Components.Add(root2);

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            var startId = child.Id;

            //Action
            var childEntry2 = new SimChildComponentEntry(new SimSlot(SimDefaultSlots.Cost, "4"), child);
            root2.Components.Add(childEntry2);

            //Tests
            Assert.AreEqual(root, childEntry.Parent);
            Assert.AreEqual(root2, childEntry2.Parent);
            Assert.AreEqual(root2, child.Parent);
            Assert.AreEqual(childEntry2, child.ParentContainer);

            Assert.AreEqual(0, counter);

            Assert.IsTrue(root2.Components.Contains(childEntry2));
            Assert.IsTrue(root.Components.Contains(childEntry));

            Assert.AreEqual(startId, child.Id);
            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(startId));
        }


        [TestMethod]
        public void MoveSubToRoot()
        {
            //Setup
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            var childSlot = new SimSlot(SimDefaultSlots.Cost, "3");
            child.CurrentSlot = childSlot.SlotBase;

            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            var startId = child.Id;

            //Action
            projectData.Components.Add(child);

            //Tests
            Assert.AreEqual(root, childEntry.Parent);
            Assert.AreEqual(null, child.Parent);
            Assert.AreEqual(null, child.ParentContainer);

            Assert.AreEqual(0, counter);

            Assert.IsTrue(root.Components.Contains(childEntry));

            Assert.AreEqual(startId, child.Id);
            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(startId));
        }

        [TestMethod]
        public void MoveRootToExistingSubEntry()
        {
            //Setup
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            projectData.Components.Add(child);

            var childSlot = new SimSlot(SimDefaultSlots.Cost, "3");
            child.CurrentSlot = childSlot.SlotBase;

            var childEntry = new SimChildComponentEntry(childSlot);
            root.Components.Add(childEntry);

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            var startId = child.Id;

            //Action
            childEntry.Component = child;

            //Tests
            Assert.AreEqual(root, childEntry.Parent);
            Assert.AreEqual(root, child.Parent);
            Assert.AreEqual(childEntry, child.ParentContainer);

            Assert.AreEqual(0, counter);

            Assert.IsTrue(root.Components.Contains(childEntry));

            Assert.AreEqual(startId, child.Id);
            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(startId));
        }

        [TestMethod]
        public void MoveRootToNewSubEntry()
        {
            //Setup
            LoadProject(emptyProject);

            SimComponent root = new SimComponent();
            projectData.Components.Add(root);

            SimComponent child = new SimComponent();
            projectData.Components.Add(child);

            var childSlot = new SimSlot(SimDefaultSlots.Cost, "3");
            child.CurrentSlot = childSlot.SlotBase;

            int counter = 0;
            child.IsBeingDeleted += (s) => counter++;

            var startId = child.Id;

            //Action
            var childEntry = new SimChildComponentEntry(childSlot, child);
            root.Components.Add(childEntry);

            //Tests
            Assert.AreEqual(root, childEntry.Parent);
            Assert.AreEqual(root, child.Parent);
            Assert.AreEqual(childEntry, child.ParentContainer);

            Assert.AreEqual(0, counter);

            Assert.IsTrue(root.Components.Contains(childEntry));

            Assert.AreEqual(startId, child.Id);
            Assert.AreEqual(child, projectData.IdGenerator.GetById<SimComponent>(startId));
        }

        #endregion
    }
}
