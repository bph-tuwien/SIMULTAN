using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.SimGeo
{
    /// <summary>
    /// Interface used by the SimGeoIO to communicate with the ILoaderGeometryExchange
    /// </summary>
    public interface ILoaderGeometryViewerInstance
    {
        /// <summary>
        /// The ILoaderGeometryExchange of this instance
        /// </summary>
        ILoaderGeometryExchange LoaderGeometryExchange { get; }
    }
}
