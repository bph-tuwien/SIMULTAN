using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.ElevationProvider
{
    /// <summary>
    /// Attribute to mark an ElevationProvider. Used to categorise ElevationProvider Addons.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ElevationProviderAttribute : Attribute
    {

    }
}
