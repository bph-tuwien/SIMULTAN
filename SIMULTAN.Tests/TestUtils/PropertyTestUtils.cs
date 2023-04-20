using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace SIMULTAN.Tests.TestUtils
{
    public static class PropertyTestUtils
    {
        public static void CheckProperty<T>(INotifyPropertyChanged obj, string prop, T value)
        {
            CheckProperty(obj, prop, value, new List<string> { prop });

        }

        public static void CheckProperty<T>(INotifyPropertyChanged obj, string prop, T value, List<string> expectedEvents)
        {
            var propInfo = obj.GetType().GetProperties().FirstOrDefault(t => t.Name == prop && t.PropertyType.IsAssignableFrom(typeof(T)));
            Assert.AreNotEqual(null, propInfo);

            PropertyChangedEventCounter cc = new PropertyChangedEventCounter(obj);

            propInfo.SetValue(obj, value);

            Assert.AreEqual(value, propInfo.GetValue(obj));
            cc.AssertEventCount(expectedEvents.Count);

            foreach (var eventProp in expectedEvents)
                Assert.IsTrue(cc.PropertyChangedArgs.Contains(eventProp));
            cc.Release();
        }

        public static void CheckPropertyAccess<Tobj, Tvalue>(Tobj workingObject, Tobj failingObject, string prop, Tvalue value)
        {
            // var propInfo = typeof(Tobj).GetProperty(prop);
            var propInfo = typeof(Tobj).GetProperties().FirstOrDefault(t => t.Name == prop && t.PropertyType.IsAssignableFrom(typeof(Tvalue)));

            Assert.AreNotEqual(null, propInfo);

            //Working
            propInfo.SetValue(workingObject, value);

            //Failing
            var exception = Assert.ThrowsException<TargetInvocationException>(() => propInfo.SetValue(failingObject, value));
            Assert.IsInstanceOfType(exception.InnerException, typeof(AccessDeniedException));
        }

        public static void CheckPropertyChanges<T>(object obj, string prop, T value, SimUserRole writeRole,
            SimComponent owningComponent, SimComponentCollection collection)
        {
            //Property
            var propInfo = obj.GetType().GetProperties().FirstOrDefault(t => t.Name == prop && t.PropertyType.IsAssignableFrom(typeof(T)));
            Assert.AreNotEqual(null, prop);


            //Setup
            var startAccess = owningComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            var startCollectionAccess = collection.LastChange;

            Assert.AreNotEqual(writeRole, startAccess.role);
            Assert.IsFalse(collection.HasChanges);

            Thread.Sleep(5);

            //Action
            propInfo.SetValue(obj, value);

            //Checks
            Assert.IsTrue(collection.HasChanges);
            Assert.IsTrue(collection.LastChange > startCollectionAccess);
            Assert.IsTrue(collection.LastChange <= DateTime.Now);

            var endAccess = owningComponent.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Assert.IsTrue(endAccess.lastAccess > startAccess.lastAccess);
            Assert.IsTrue(endAccess.lastAccess <= DateTime.Now);
            Assert.AreEqual(writeRole, endAccess.role);
        }
    }
}
