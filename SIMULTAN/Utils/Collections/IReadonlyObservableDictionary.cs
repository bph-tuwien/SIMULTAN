using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Collections
{
    /// <summary>
    /// A combination of IReadOnlyDictionary and INotifyCollectionChanged. Used to return readonly dictionaries while still giving change notifications
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface IReadonlyObservableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>, INotifyCollectionChanged
    {
    }
}
