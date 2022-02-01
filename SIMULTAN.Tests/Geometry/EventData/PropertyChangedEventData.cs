using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.EventData
{
    public class PropertyChangedEventData
    {
        public List<string> PropertyChangedData;

        public PropertyChangedEventData(INotifyPropertyChanged source)
        {
            this.PropertyChangedData = new List<string>();
            source.PropertyChanged += (object sender, PropertyChangedEventArgs args) =>
            {
                PropertyChangedData.Add(args.PropertyName);
            };
        }

        public virtual void Reset()
        {
            PropertyChangedData.Clear();
        }
    }
}
