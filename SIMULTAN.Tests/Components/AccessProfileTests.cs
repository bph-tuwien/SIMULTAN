using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class AccessProfileTests
    {
        private void AssertProfile(SimAccessProfile toCheck, SimUserRole role, SimComponentAccessPrivilege access, DateTime lastWrite, DateTime lastSuper, DateTime lastRelease)
        {
            var tracker = toCheck[role];
            Assert.AreEqual(toCheck, tracker.AccessProfile);
            Assert.AreEqual(role, tracker.Role);
            Assert.AreEqual(access, tracker.Access);
            Assert.AreEqual(lastWrite, tracker.LastAccessWrite);
            Assert.AreEqual(lastSuper, tracker.LastAccessSupervize);
            Assert.AreEqual(lastRelease, tracker.LastAccessRelease);
        }

        [TestMethod]
        public void Ctor()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.BUILDING_OPERATOR);

            Assert.AreEqual(Enum.GetNames(typeof(SimUserRole)).Length, profile.Count());

            foreach (var tracker in profile)
            {
                Assert.AreEqual(tracker, profile[tracker.Role]);
                Assert.AreEqual(DateTime.MinValue, tracker.LastAccessWrite);
                Assert.AreEqual(DateTime.MinValue, tracker.LastAccessSupervize);
                Assert.AreEqual(DateTime.MinValue, tracker.LastAccessRelease);

                Assert.AreEqual(SimComponentValidity.Valid, profile.ProfileState);

                if (tracker.Role != SimUserRole.ADMINISTRATOR && tracker.Role != SimUserRole.BUILDING_OPERATOR)
                {
                    Assert.AreEqual(SimComponentAccessPrivilege.None, tracker.Access);
                }
                else
                {
                    Assert.AreEqual(SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Read, tracker.Access);
                }
            }
        }

        [TestMethod]
        public void CtorCopy()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { new SimAccessProfile((SimAccessProfile)null); });

            SimAccessProfile source = new SimAccessProfile(SimUserRole.ARCHITECTURE);
            source[SimUserRole.BUILDING_DEVELOPER].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize | SimComponentAccessPrivilege.Release;
            source[SimUserRole.BUILDING_DEVELOPER].LastAccessSupervize = new DateTime(2020, 10, 18);
            source[SimUserRole.BUILDING_DEVELOPER].LastAccessRelease = new DateTime(2020, 10, 19);
            source[SimUserRole.ARCHITECTURE].LastAccessWrite = new DateTime(2019, 10, 17);

            var copy = new SimAccessProfile(source);

            Assert.AreEqual(SimComponentValidity.Valid, copy.ProfileState);

            foreach (var tracker in copy)
            {
                Assert.AreEqual(tracker, copy[tracker.Role]);

                if (tracker.Role != SimUserRole.ADMINISTRATOR && tracker.Role != SimUserRole.BUILDING_DEVELOPER && tracker.Role != SimUserRole.ARCHITECTURE)
                {
                    Assert.AreEqual(DateTime.MinValue, tracker.LastAccessWrite);
                    Assert.AreEqual(DateTime.MinValue, tracker.LastAccessSupervize);
                    Assert.AreEqual(DateTime.MinValue, tracker.LastAccessRelease);

                    Assert.AreEqual(SimComponentAccessPrivilege.None, tracker.Access);
                }
            }

            AssertProfile(copy, SimUserRole.ADMINISTRATOR, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
            AssertProfile(copy, SimUserRole.BUILDING_DEVELOPER, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize | SimComponentAccessPrivilege.Release,
                DateTime.MinValue, new DateTime(2020, 10, 18), new DateTime(2020, 10, 19));
            AssertProfile(copy, SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write,
                new DateTime(2019, 10, 17), DateTime.MinValue, DateTime.MinValue);
        }

        [TestMethod]
        public void CtorParsing()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { new SimAccessProfile((IDictionary<SimUserRole, SimAccessProfileEntry>)null); });

            Dictionary<SimUserRole, SimAccessProfileEntry> input = new Dictionary<SimUserRole, SimAccessProfileEntry>
            {
                { SimUserRole.ADMINISTRATOR, new SimAccessProfileEntry(SimUserRole.ADMINISTRATOR, SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Read,
                    DateTime.MinValue, DateTime.MinValue, DateTime.MinValue) },
                { SimUserRole.ARCHITECTURE, new SimAccessProfileEntry(SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Read,
                    new DateTime(2019, 10, 17), DateTime.MinValue, DateTime.MinValue) },
            };

            var profile = new SimAccessProfile(input);

            foreach (SimUserRole role in Enum.GetValues(typeof(SimUserRole)))
            {
                if (role != SimUserRole.ADMINISTRATOR && role != SimUserRole.ARCHITECTURE)
                {
                    AssertProfile(profile, role, SimComponentAccessPrivilege.None, DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
                }
            }

            AssertProfile(profile, SimUserRole.ADMINISTRATOR, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write,
                DateTime.MinValue, DateTime.MinValue, DateTime.MinValue);
            AssertProfile(profile, SimUserRole.ARCHITECTURE, SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write,
                new DateTime(2019, 10, 17), DateTime.MinValue, DateTime.MinValue);
        }

        [TestMethod]
        public void Indexer()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.ARCHITECTURE);

            Assert.ThrowsException<ArgumentNullException>(() => { var test = profile[null]; });
        }

        [TestMethod]
        public void Reset()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.BUILDING_OPERATOR);

            profile[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.All;
            profile[SimUserRole.BUILDING_PHYSICS].LastAccessRelease = new DateTime(2017, 10, 17);

            profile.ResetAccessFlags(SimUserRole.ARCHITECTURE);

            Assert.AreEqual(Enum.GetNames(typeof(SimUserRole)).Length, profile.Count());
            Assert.AreEqual(SimComponentValidity.Valid, profile.ProfileState);

            foreach (var tracker in profile)
            {
                if (tracker.Role != SimUserRole.ADMINISTRATOR && tracker.Role != SimUserRole.ARCHITECTURE)
                {
                    Assert.AreEqual(SimComponentAccessPrivilege.None, tracker.Access);
                }
                else
                {
                    Assert.AreEqual(SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Read, tracker.Access);
                }
            }

            Assert.AreEqual(new DateTime(2017, 10, 17), profile[SimUserRole.BUILDING_PHYSICS].LastAccessRelease);
        }

        [TestMethod]
        public void ComponentState()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.BUILDING_DEVELOPER);
            PropertyChangedEventCounter cc = new PropertyChangedEventCounter(profile);

            profile[SimUserRole.FIRE_SAFETY].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release | SimComponentAccessPrivilege.Supervize;

            Assert.AreEqual(SimComponentValidity.Valid, profile.ProfileState);

            profile[SimUserRole.FIRE_SAFETY].LastAccessSupervize = new DateTime(2020, 10, 18);
            profile[SimUserRole.FIRE_SAFETY].LastAccessRelease = new DateTime(2020, 10, 19);
            profile[SimUserRole.BUILDING_DEVELOPER].LastAccessWrite = new DateTime(2020, 10, 22);
            Assert.AreEqual(SimComponentValidity.WriteAfterRelease, profile.ProfileState);
            Assert.IsTrue(cc.PropertyChangedArgs.Count >= 1); cc.Reset();

            profile[SimUserRole.FIRE_SAFETY].LastAccessSupervize = new DateTime(2020, 11, 18);
            profile[SimUserRole.BUILDING_DEVELOPER].LastAccessWrite = new DateTime(2020, 11, 19);
            profile[SimUserRole.FIRE_SAFETY].LastAccessRelease = new DateTime(2020, 11, 20);
            Assert.AreEqual(SimComponentValidity.WriteAfterSupervize, profile.ProfileState);
            Assert.IsTrue(cc.PropertyChangedArgs.Count >= 1); cc.Reset();

            profile[SimUserRole.BUILDING_DEVELOPER].LastAccessWrite = new DateTime(2020, 12, 17);
            profile[SimUserRole.FIRE_SAFETY].LastAccessRelease = new DateTime(2020, 12, 18);
            profile[SimUserRole.FIRE_SAFETY].LastAccessSupervize = new DateTime(2020, 12, 20);
            Assert.AreEqual(SimComponentValidity.SupervizeAfterRelease, profile.ProfileState);
            Assert.IsTrue(cc.PropertyChangedArgs.Count >= 1); cc.Reset();

            profile[SimUserRole.FIRE_SAFETY].LastAccessRelease = new DateTime(2020, 12, 22);
            Assert.AreEqual(SimComponentValidity.Valid, profile.ProfileState);
            Assert.IsTrue(cc.PropertyChangedArgs.Count >= 1); cc.Reset();
        }

        [TestMethod]
        public void SetWriteAccess()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.BUILDING_DEVELOPER);

            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write, profile[SimUserRole.BUILDING_DEVELOPER].Access);

            profile[SimUserRole.FIRE_SAFETY].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;

            Assert.AreEqual(SimComponentAccessPrivilege.Read, profile[SimUserRole.BUILDING_DEVELOPER].Access);
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write, profile[SimUserRole.FIRE_SAFETY].Access);
        }

        [TestMethod]
        public void AdministratorSpecialAccessChecks()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.BUILDING_DEVELOPER);
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write, profile[SimUserRole.ADMINISTRATOR].Access);

            profile[SimUserRole.FIRE_SAFETY].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write, profile[SimUserRole.ADMINISTRATOR].Access);

            profile[SimUserRole.ADMINISTRATOR].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Release;
            Assert.AreEqual(SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Release, profile[SimUserRole.ADMINISTRATOR].Access);
        }

        [TestMethod]
        public void AccessChangedEvent()
        {
            int counter = 0;

            SimAccessProfile profile = new SimAccessProfile(SimUserRole.ARCHITECTURE);
            profile.AccessChanged += (s, e) => counter++;

            profile[SimUserRole.ARCHITECTURE].Access |= SimComponentAccessPrivilege.Release;
            Assert.AreEqual(1, counter); counter = 0;

            profile[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Write;
            Assert.AreEqual(1, counter); counter = 0;
        }

        [TestMethod]
        public void LastAccess()
        {
            SimAccessProfile profile = new SimAccessProfile(SimUserRole.FIRE_SAFETY);
            var ftrack = profile[SimUserRole.FIRE_SAFETY];
            var archtrack = profile[SimUserRole.ARCHITECTURE];
            ftrack.Access = SimComponentAccessPrivilege.All;
            archtrack.Access = SimComponentAccessPrivilege.Release | SimComponentAccessPrivilege.Supervize | SimComponentAccessPrivilege.Read;

            Assert.AreEqual(DateTime.MinValue, profile.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
            Assert.AreEqual(DateTime.MinValue, profile.LastAccess(SimComponentAccessPrivilege.Supervize).lastAccess);
            Assert.AreEqual(DateTime.MinValue, profile.LastAccess(SimComponentAccessPrivilege.Release).lastAccess);

            Assert.ThrowsException<NotSupportedException>(() => { profile.LastAccess(SimComponentAccessPrivilege.Read); });
            Assert.ThrowsException<NotSupportedException>(() => { profile.LastAccess(SimComponentAccessPrivilege.All); });
            Assert.ThrowsException<NotSupportedException>(() => { profile.LastAccess(SimComponentAccessPrivilege.None); });

            ftrack.LastAccessWrite = new DateTime(2020, 10, 17);
            ftrack.LastAccessSupervize = new DateTime(2010, 10, 17);
            ftrack.LastAccessRelease = new DateTime(2020, 10, 18);

            archtrack.LastAccessSupervize = new DateTime(2021, 10, 17);
            archtrack.LastAccessRelease = new DateTime(2020, 10, 17);

            Assert.AreEqual(new DateTime(2020, 10, 17), profile.LastAccess(SimComponentAccessPrivilege.Write).lastAccess);
            Assert.AreEqual(SimUserRole.FIRE_SAFETY, profile.LastAccess(SimComponentAccessPrivilege.Write).role);
            Assert.AreEqual(new DateTime(2021, 10, 17), profile.LastAccess(SimComponentAccessPrivilege.Supervize).lastAccess);
            Assert.AreEqual(SimUserRole.ARCHITECTURE, profile.LastAccess(SimComponentAccessPrivilege.Supervize).role);
            Assert.AreEqual(new DateTime(2020, 10, 18), profile.LastAccess(SimComponentAccessPrivilege.Release).lastAccess);
            Assert.AreEqual(SimUserRole.FIRE_SAFETY, profile.LastAccess(SimComponentAccessPrivilege.Release).role);
        }
    }
}
