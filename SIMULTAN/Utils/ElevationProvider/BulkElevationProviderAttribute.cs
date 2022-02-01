using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.ElevationProvider
{
    /// <summary>
    /// Attribute to mark an BulkElevationProvider. Used to categorise BulkElevationProvider Addons.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class BulkElevationProviderAttribute : Attribute
    {

    }
}
