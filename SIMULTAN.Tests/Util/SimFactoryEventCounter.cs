using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Utils
{
    public class SimFactoryEventCounter
    {
        public SimFactoryEventCounter(ISimManagedCollection factory)
        {
            factory.HasChangesChanged += (s, e) => HasChangesChangedCounter++;
        }

        public int HasChangesChangedCounter { get; private set; } = 0;

        public void AssertEventCount(int hasChangesChanged)
        {
            Assert.AreEqual(hasChangesChanged, HasChangesChangedCounter);
        }

        public void Reset()
        {
            HasChangesChangedCounter = 0;
        }
    }
}
