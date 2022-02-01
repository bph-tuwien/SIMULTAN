using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Collections
{
    /// <summary>
    /// A combination of IReadOnlyCollection and INotifyCollectionChanged. Used to return readonly collections while still giving change notifications
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IReadOnlyObservableCollection<T> : IReadOnlyCollection<T>, INotifyCollectionChanged
    {
    }
}
