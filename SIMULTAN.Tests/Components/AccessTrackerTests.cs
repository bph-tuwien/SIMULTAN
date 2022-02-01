using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class AccessTrackerTests
    {
        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, null); });

            var profile = new SimAccessProfile(SimUserRole.ADMINISTRATOR);

            var tracker = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, profile);

            Assert.AreEqual(SimUserRole.ARCHITECTURE, tracker.Role);
            Assert.AreEqual(profile, tracker.AccessProfile);
            Assert.AreEqual(SimComponentAccessPrivilege.None, tracker.Access);
            Assert.AreEqual(DateTime.MinValue, tracker.LastAccessWrite);
            Assert.AreEqual(DateTime.MinValue, tracker.LastAccessRelease);
            Assert.AreEqual(DateTime.MinValue, tracker.LastAccessSupervize);
        }

        [TestMethod]
        public void CtorCopy()
        {
            SimAccessProfile source = new SimAccessProfile(SimUserRole.BUILDING_DEVELOPER);
            source[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.All; //Required because dates can only be set when the according access is granted
            source[SimUserRole.ARCHITECTURE].LastAccessWrite = new DateTime(2018, 10, 17);
            source[SimUserRole.ARCHITECTURE].LastAccessSupervize = new DateTime(2019, 11, 16);
            source[SimUserRole.ARCHITECTURE].LastAccessRelease = new DateTime(2020, 12, 15);
            source[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release;

            SimAccessProfile target = new SimAccessProfile(SimUserRole.BUILDING_DEVELOPER);

            Assert.ThrowsException<ArgumentNullException>(() => { new SimAccessProfileEntry(null, target); });
            Assert.ThrowsException<ArgumentNullException>(() => { new SimAccessProfileEntry(source[SimUserRole.ARCHITECTURE], null); });

            var copy = new SimAccessProfileEntry(source[SimUserRole.ARCHITECTURE], target);

            Assert.AreEqual(SimUserRole.ARCHITECTURE, copy.Role);
            Assert.AreEqual(target, copy.AccessProfile);
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release, copy.Access);
            Assert.AreEqual(new DateTime(2018, 10, 17), copy.LastAccessWrite);
            Assert.AreEqual(new DateTime(2019, 11, 16), copy.LastAccessSupervize);
            Assert.AreEqual(new DateTime(2020, 12, 15), copy.LastAccessRelease);
        }

        [TestMethod]
        public void CtorParsing()
        {
            var tracker = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize,
                new DateTime(2018, 10, 17), new DateTime(2019, 11, 16), new DateTime(2020, 12, 15));

            Assert.AreEqual(SimUserRole.ARCHITECTURE, tracker.Role);
            Assert.AreEqual(null, tracker.AccessProfile);
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize, tracker.Access);
            Assert.AreEqual(new DateTime(2018, 10, 17), tracker.LastAccessWrite);
            Assert.AreEqual(new DateTime(2019, 11, 16), tracker.LastAccessSupervize);
            Assert.AreEqual(new DateTime(2020, 12, 15), tracker.LastAccessRelease);
        }


        [TestMethod]
        public void Properties()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.ARCHITECTURE);
            var tracker = profile[SimUserRole.ARCHITECTURE];

            PropertyChangedEventCounter cc = new PropertyChangedEventCounter(tracker);

            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write, tracker.Access);
            tracker.Access = SimComponentAccessPrivilege.All;
            Assert.AreEqual(SimComponentAccessPrivilege.All, tracker.Access);
            cc.AssertEventCount(1);
            cc.Reset();

            Assert.AreEqual(DateTime.MinValue, tracker.LastAccessWrite);
            tracker.LastAccessWrite = new DateTime(2018, 10, 17);
            Assert.AreEqual(new DateTime(2018, 10, 17), tracker.LastAccessWrite);
            cc.AssertEventCount(1);
            cc.Reset();

            Assert.AreEqual(DateTime.MinValue, tracker.LastAccessSupervize);
            tracker.LastAccessSupervize = new DateTime(2019, 10, 17);
            Assert.AreEqual(new DateTime(2019, 10, 17), tracker.LastAccessSupervize);
            cc.AssertEventCount(1);
            cc.Reset();

            Assert.AreEqual(DateTime.MinValue, tracker.LastAccessRelease);
            tracker.LastAccessRelease = new DateTime(2020, 10, 17);
            Assert.AreEqual(new DateTime(2020, 10, 17), tracker.LastAccessRelease);
            cc.AssertEventCount(1);
            cc.Reset();
        }

        [TestMethod]
        public void InvalidAccess()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.ADMINISTRATOR);
            var tracker = profile[SimUserRole.ARCHITECTURE];

            Assert.ThrowsException<AccessDeniedException>(() => { tracker.LastAccessWrite = new DateTime(2020, 10, 17); });
            tracker.Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            tracker.LastAccessWrite = new DateTime(2020, 10, 17);

            Assert.ThrowsException<AccessDeniedException>(() => { tracker.LastAccessSupervize = new DateTime(2020, 10, 18); });
            tracker.Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Supervize;
            tracker.LastAccessSupervize = new DateTime(2020, 10, 18);

            Assert.ThrowsException<AccessDeniedException>(() => { tracker.LastAccessRelease = new DateTime(2020, 10, 19); });
            tracker.Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Supervize | SimComponentAccessPrivilege.Release;
            tracker.LastAccessRelease = new DateTime(2020, 10, 19);
        }

        [TestMethod]
        public void InvalidDate()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.ADMINISTRATOR);
            var tracker = profile[SimUserRole.ARCHITECTURE];
            tracker.Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Supervize | SimComponentAccessPrivilege.Release;
            tracker.LastAccessWrite = new DateTime(2020, 10, 17);
            tracker.LastAccessSupervize = new DateTime(2020, 10, 19);
            tracker.LastAccessRelease = new DateTime(2020, 10, 21);

            Assert.ThrowsException<ArgumentException>(() => { tracker.LastAccessWrite = new DateTime(2020, 10, 16); });
            Assert.ThrowsException<ArgumentException>(() => { tracker.LastAccessSupervize = new DateTime(2020, 10, 18); });
            Assert.ThrowsException<ArgumentException>(() => { tracker.LastAccessRelease = new DateTime(2020, 10, 20); });
        }

        [TestMethod]
        public void Equals()
        {
            SimAccessProfileEntry t1 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            SimAccessProfileEntry t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            SimAccessProfileEntry t3 = new SimAccessProfileEntry(SimUserRole.FIRE_SAFETY, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));

            Assert.IsFalse(t1 == null);
            Assert.IsFalse(null == t1);

            Assert.IsTrue(t1 == t2);
            Assert.IsFalse(t1 == t3);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            Assert.IsFalse(t1 == t2);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2021, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            Assert.IsFalse(t1 == t2);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2021, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            Assert.IsFalse(t1 == t2);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2021, 10, 18), new DateTime(2020, 10, 19));
            Assert.IsFalse(t1 == t2);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2021, 10, 19));
            Assert.IsFalse(t1 == t2);

            t1 = null;
            t2 = null;
            Assert.IsTrue(t1 == t2);
        }

        [TestMethod]
        public void NotEquals()
        {
            SimAccessProfileEntry t1 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            SimAccessProfileEntry t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            SimAccessProfileEntry t3 = new SimAccessProfileEntry(SimUserRole.FIRE_SAFETY, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));

            Assert.IsTrue(t1 != null);
            Assert.IsTrue(null != t1);

            Assert.IsFalse(t1 != t2);
            Assert.IsTrue(t1 != t3);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            Assert.IsTrue(t1 != t2);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2021, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            Assert.IsTrue(t1 != t2);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2021, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            Assert.IsTrue(t1 != t2);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2021, 10, 18), new DateTime(2020, 10, 19));
            Assert.IsTrue(t1 != t2);

            t2 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2021, 10, 19));
            Assert.IsTrue(t1 != t2);
        }

        [TestMethod]
        public void HasAccess()
        {
            SimAccessProfileEntry t1 = new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.None,
                new DateTime(2020, 10, 17), new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));

            Assert.IsFalse(t1.HasAccess(SimComponentAccessPrivilege.Read));

            t1.Access = SimComponentAccessPrivilege.Read;

            Assert.IsTrue(t1.HasAccess(SimComponentAccessPrivilege.Read));
            Assert.IsFalse(t1.HasAccess(SimComponentAccessPrivilege.Write));

            t1.Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;

            Assert.IsTrue(t1.HasAccess(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write));
        }
    }
}
