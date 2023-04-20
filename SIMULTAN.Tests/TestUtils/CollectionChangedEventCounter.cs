using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.TestUtils
{
    public class CollectionChangedEventCounter
    {
        public List<NotifyCollectionChangedEventArgs> CollectionChangedArgs { get; } = new List<NotifyCollectionChangedEventArgs>();

        public CollectionChangedEventCounter(INotifyCollectionChanged data)
        {
            data.CollectionChanged += (s, e) => CollectionChangedArgs.Add(e);
        }

        public void Reset()
        {
            this.CollectionChangedArgs.Clear();
        }

        public void AssertEventCount(int count)
        {
            Assert.AreEqual(count, this.CollectionChangedArgs.Count);
        }
    }
}
