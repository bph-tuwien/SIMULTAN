using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores the relation between an edge and it's EdgeContainer (EdgeLoop/Polyline)
    /// </summary>
    [DebuggerDisplay("PEdge ID={Edge.Id}")]
    public class PEdge
    {
        /// <summary>
        /// The container (EdgeLoop or Polyline)
        /// </summary>
        public BaseEdgeContainer Parent { get; set; }

        /// <summary>
        /// Returns the start vertex 
        /// </summary>
        public Vertex StartVertex { get { return (Orientation == GeometricOrientation.Forward) ? Edge.Vertices[0] : Edge.Vertices[1]; } }
        /// <summary>
        /// Returns the end vertex 
        /// </summary>
        public Vertex EndVertex { get { return (Orientation == GeometricOrientation.Forward) ? Edge.Vertices[1] : Edge.Vertices[0]; } }
        /// <summary>
        /// Returns the represented edge
        /// </summary>
        public Edge Edge { get; private set; }

        /// <summary>
        /// Returns the orientation to the original Edge (Forward means Vertex[0] to Vertex[1])
        /// </summary>
        public GeometricOrientation Orientation { get; set; }

        /// <summary>
        /// The next pedge in the container (might be null)
        /// </summary>
        public PEdge Next { get; set; }
        /// <summary>
        /// The previous pedge in the container (might be null)
        /// </summary>
        public PEdge Prev { get; set; }


        /// <summary>
        /// Initializes a new instance of the PEdge class
        /// </summary>
        /// <param name="edge">The edge</param>
        /// <param name="orientation">Orientation of the edge. Forward means from Vertices[0] to Vertices[1]</param>
        /// <param name="parent">The container</param>
        public PEdge(Edge edge, GeometricOrientation orientation, BaseEdgeContainer parent)
        {
            this.Edge = edge;
            this.Orientation = orientation;
            this.Parent = parent;
        }

        /// <inheritdoc />
        public virtual void MakeConsistent()
        {
            if (!Edge.PEdges.Contains(this))
                Edge.PEdges.Add(this);
        }
    }
}
