using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class UserRootComponentCollectionTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\UserComponentListTest.simultan");

        [TestMethod]
        public void ConstructorTest()
        {
            LoadProject(testProject);


            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            var col = new ObservableCollection<SimComponent>();
            col.Add(comp1);
            col.Add(comp2);
            col.Add(subcomp1);

            var complist = new SimUserComponentList("TEST", col);
            projectData.UserComponentLists.Add(complist);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));
        }

        [TestMethod]
        public void ConstructorFailTest()
        {
            Assert.ThrowsException<ArgumentNullException>(() => new SimUserRootComponentCollection(null));
            Assert.ThrowsException<ArgumentNullException>(() => new SimUserRootComponentCollection(null, null));
            Assert.ThrowsException<ArgumentNullException>(() => new SimUserComponentList(null));
            Assert.ThrowsException<ArgumentNullException>(() => new SimUserComponentList("TEST", null));
        }

        [TestMethod]
        public void AddTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));
        }

        [TestMethod]
        public void AddFailTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            Assert.ThrowsException<ArgumentNullException>(() => complist.RootComponents.Add(null));
        }

        [TestMethod]
        public void CollectionChangedAddTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];

            NotifyCollectionChangedEventHandler handler = (sender, e) =>
            {
                Assert.IsTrue(e.Action == NotifyCollectionChangedAction.Add);
                Assert.IsTrue(e.NewItems.Count == 1);
                Assert.IsTrue(e.NewItems.Contains(comp1));
                Assert.IsTrue(e.OldItems == null);
            };

            complist.RootComponents.CollectionChanged += handler;

            complist.RootComponents.Add(comp1);

            complist.RootComponents.CollectionChanged -= handler;
        }

        [TestMethod]
        public void RemoveTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            Assert.IsTrue(complist.RootComponents.Remove(comp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsTrue(complist.RootComponents.Remove(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp1));
        }

        [TestMethod]
        public void RemoveFailTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            Assert.IsFalse(complist.RootComponents.Remove(comp3));
        }

        [TestMethod]
        public void CollectionChangedRemoveTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            NotifyCollectionChangedEventHandler handler = (sender, e) =>
            {
                Assert.IsTrue(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove);
                Assert.IsTrue(e.OldItems.Count == 1);
                Assert.IsTrue(e.OldItems.Contains(comp1));
                Assert.IsTrue(e.NewItems == null);
            };

            complist.RootComponents.CollectionChanged += handler;

            complist.RootComponents.Remove(comp1);

            complist.RootComponents.CollectionChanged -= handler;
        }

        private WeakReference MemLeakRemoveTest_Action(SimUserComponentList complist)
        {
            var comp1 = projectData.Components[0];

            complist.RootComponents.Add(comp1);

            WeakReference compref = new WeakReference(complist.RootComponents.RootComponents[0]);

            complist.RootComponents.Remove(comp1);
            return compref;
        }
        [TestMethod]
        public void MemLeakRemoveTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var compref = MemLeakRemoveTest_Action(complist);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(compref.IsAlive);
        }

        [TestMethod]
        public void RemoveAtTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            complist.RootComponents.RemoveAt(2);
            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp1));
            complist.RootComponents.RemoveAt(0);
            Assert.IsFalse(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp1));
        }

        [TestMethod]
        public void ClearTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            complist.RootComponents.Clear();

            Assert.IsTrue(complist.RootComponents.Count == 0);
        }

        private (WeakReference ref1, WeakReference ref2, WeakReference ref3) MemLeakClearTest_Action(SimUserComponentList complist)
        {
            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var subcomp1 = comp1.Components[0].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            var ref1 = new WeakReference(complist.RootComponents.RootComponents[0]);
            var ref2 = new WeakReference(complist.RootComponents.RootComponents[1]);
            var ref3 = new WeakReference(complist.RootComponents.RootComponents[2]);

            return (ref1, ref2, ref3);
        }
        [TestMethod]
        public void MemLeakClearTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            (var ref1, var ref2, var ref3) = MemLeakClearTest_Action(complist);

            Assert.IsTrue(ref1.IsAlive);
            Assert.IsTrue(ref2.IsAlive);
            Assert.IsTrue(ref3.IsAlive);

            complist.RootComponents.Clear();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(ref1.IsAlive);
            Assert.IsFalse(ref2.IsAlive);
            Assert.IsFalse(ref3.IsAlive);
        }

        [TestMethod]
        public void CollectionChangedClearTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            complist.RootComponents.CollectionChanged += (sender, e) =>
            {
                Assert.IsTrue(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset);
                Assert.IsTrue(e.OldItems == null);
                Assert.IsTrue(e.NewItems == null);
            };
            complist.RootComponents.Clear();
        }

        [TestMethod]
        public void IndexTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            Assert.IsTrue(complist.RootComponents[0] == comp1);
            Assert.IsTrue(complist.RootComponents[1] == subcomp1);

            complist.RootComponents[0] = subcomp2;
            complist.RootComponents[1] = comp2;

            Assert.IsTrue(complist.RootComponents[0] == subcomp2);
            Assert.IsTrue(complist.RootComponents[1] == comp2);
        }

        [TestMethod]
        public void CollectionChangedIndexTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(subcomp1);

            NotifyCollectionChangedEventHandler handler = (sender, e) =>
            {
                Assert.IsTrue(e.Action == NotifyCollectionChangedAction.Replace);
                Assert.IsTrue(e.OldItems.Count == 1);
                Assert.IsTrue(e.OldItems.Contains(subcomp1));
                Assert.IsTrue(e.NewItems.Count == 1);
                Assert.IsTrue(e.NewItems.Contains(subcomp2));
                Assert.IsTrue(e.OldStartingIndex == 1);
                Assert.IsTrue(e.NewStartingIndex == 1);
            };

            complist.RootComponents.CollectionChanged += handler;

            //Action
            complist.RootComponents[1] = subcomp2;

            //Detach before cleanup
            complist.RootComponents.CollectionChanged -= handler;
        }

        private WeakReference MemLeakIndexTest_Action(SimUserComponentList complist)
        {
            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(subcomp1);

            var compref = new WeakReference(complist.RootComponents.RootComponents[0]);

            complist.RootComponents[0] = subcomp2;

            return compref;
        }
        [TestMethod]
        public void MemLeakIndexTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var compref = MemLeakIndexTest_Action(complist);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(compref.IsAlive);
        }

        [TestMethod]
        public void IndexOfTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            Assert.IsTrue(complist.RootComponents.IndexOf(comp1) == 0);
            Assert.IsTrue(complist.RootComponents.IndexOf(comp2) == 1);
            Assert.IsTrue(complist.RootComponents.IndexOf(subcomp1) == 2);
        }

        [TestMethod]
        public void IndexOfFailTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.IndexOf(subcomp2) == -1);
        }

        [TestMethod]
        public void InsertTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            complist.RootComponents.Insert(1, comp3);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));
            Assert.IsTrue(complist.RootComponents[0] == comp1);
            Assert.IsTrue(complist.RootComponents[1] == comp3);
            Assert.IsTrue(complist.RootComponents[2] == comp2);
            Assert.IsTrue(complist.RootComponents[3] == subcomp1);
        }

        [TestMethod]
        public void CollectionChangedInsertTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            NotifyCollectionChangedEventHandler handler = (sender, e) =>
            {
                Assert.IsTrue(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add);
                Assert.IsTrue(e.OldItems == null);
                Assert.IsTrue(e.NewItems.Count == 1);
                Assert.IsTrue(e.NewItems.Contains(comp3));
                Assert.IsTrue(e.OldStartingIndex == -1);
                Assert.IsTrue(e.NewStartingIndex == 2);
            };
            complist.RootComponents.CollectionChanged += handler;

            complist.RootComponents.Insert(2, comp3);

            complist.RootComponents.CollectionChanged -= handler;
        }

        [TestMethod]
        public void RootComponentsRemoveTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            projectData.Components.Remove(comp1);
            Assert.IsFalse(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp1));
        }

        private WeakReference MemLeakRootComponentsRemoveTest_Action(SimUserComponentList complist)
        {
            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            var compref = new WeakReference(complist.RootComponents.RootComponents[0]);

            Assert.IsTrue(complist.RootComponents.RootComponents[0].Component == comp1);
            Assert.IsTrue(compref.IsAlive);

            projectData.Components.Remove(comp1);

            return compref;
        }
        [TestMethod]
        public void MemLeakRootComponentsRemoveTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var compref = MemLeakRootComponentsRemoveTest_Action(complist);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(compref.IsAlive);
        }

        [TestMethod]
        public void SubComponentRemoveTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsTrue(complist.RootComponents.Contains(subcomp1));
            Assert.IsFalse(complist.RootComponents.Contains(comp3));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp2));

            comp1.Components.RemoveAt(0);
            Assert.IsTrue(complist.RootComponents.Contains(comp1));
            Assert.IsTrue(complist.RootComponents.Contains(comp2));
            Assert.IsFalse(complist.RootComponents.Contains(subcomp1));
        }


        private WeakReference MemLeakSubComponentRemoveTest_Action(SimUserComponentList complist)
        {
            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            var compref = new WeakReference(complist.RootComponents.RootComponents[2]);

            Assert.IsTrue(compref.IsAlive);

            comp1.Components.RemoveAt(0);

            return compref;
        }
        [TestMethod]
        public void MemLeakSubComponentRemoveTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var compref = MemLeakSubComponentRemoveTest_Action(complist);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(compref.IsAlive);
        }

        [TestMethod]
        public void EnumeratorTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            var enumerator = complist.RootComponents.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.Current == comp1);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.Current == comp2);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.Current == subcomp1);
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void Enumerator2Test()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];
            var comp2 = projectData.Components[1];
            var comp3 = projectData.Components[2];
            var subcomp1 = comp1.Components[0].Component;
            var subcomp2 = comp1.Components[1].Component;

            complist.RootComponents.Add(comp1);
            complist.RootComponents.Add(comp2);
            complist.RootComponents.Add(subcomp1);

            IEnumerator enumerator = complist.RootComponents.GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.Current == comp1);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.Current == comp2);
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.Current == subcomp1);
            Assert.IsFalse(enumerator.MoveNext());
        }

        [TestMethod]
        public void FactoryAttachTest()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");

            var comp1 = projectData.Components[0];

            complist.RootComponents.Add(comp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));

            projectData.Components.Remove(comp1);

            projectData.UserComponentLists.Add(complist);

            Assert.IsFalse(complist.RootComponents.Contains(comp1));
        }

        [TestMethod]
        public void FactoryAttach2Test()
        {
            LoadProject(testProject);

            var complist = new SimUserComponentList("TEST");
            projectData.UserComponentLists.Add(complist);

            var comp1 = projectData.Components[0];

            complist.RootComponents.Add(comp1);

            Assert.IsTrue(complist.RootComponents.Contains(comp1));

            projectData.UserComponentLists.Remove(complist);

            projectData.Components.Remove(comp1);

            projectData.UserComponentLists.Add(complist);

            Assert.IsFalse(complist.RootComponents.Contains(comp1));
        }
    }
}
