using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Base class for all edge containing geometries (Polyline, EdgeLoop)
    /// </summary>
    public abstract class BaseEdgeContainer : BaseGeometry
    {
        /// <summary>
        /// Stores a list of edges in this container
        /// </summary>
        public abstract ObservableCollection<PEdge> Edges { get; }

        /// <summary>
        /// Initializes a new instance of the BaseEdgeContainer class
        /// </summary>
        /// <param name="id">The unique identifier for this object</param>
        /// <param name="layer">The layer this object is placed on</param>
        public BaseEdgeContainer(ulong id, Layer layer) : base(id, layer) { }

        /// <summary>
        /// Returns a list of all faces that use this Container
        /// </summary>
        public abstract List<Face> Faces { get; }
    }
}
