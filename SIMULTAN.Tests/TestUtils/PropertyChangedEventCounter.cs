using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.TestUtils
{
    public class PropertyChangedEventCounter
    {
        private INotifyPropertyChanged data;

        public List<string> PropertyChangedArgs { get; } = new List<string>();

        public PropertyChangedEventCounter(INotifyPropertyChanged data)
        {
            this.data = data;
            this.data.PropertyChanged += this.Data_PropertyChanged;
        }

        private void Data_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChangedArgs.Add(e.PropertyName);
        }

        public virtual void Reset()
        {
            this.PropertyChangedArgs.Clear();
        }

        public void AssertEventCount(int count)
        {
            Assert.AreEqual(count, this.PropertyChangedArgs.Count);
        }

        public void Release()
        {
            this.data.PropertyChanged -= Data_PropertyChanged;
        }
    }
}
