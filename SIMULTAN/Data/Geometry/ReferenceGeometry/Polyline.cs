using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Represents a polyline (consisting of multiple connected edges).
    /// A polyline may not be closed. For closed loops have a look at the EdgeLoop class
    /// </summary>
    public class Polyline : BaseEdgeContainer
    {
        #region Properties

        /// <inheritdoc/>
        public override ObservableCollection<PEdge> Edges => edges;

        /// <inheritdoc />
        public override List<Face> Faces => new List<Face>();

        private ObservableCollection<PEdge> edges;

        #endregion

        /// <summary>
        /// Initializes a new Polyline class
        /// </summary>
        /// <param name="layer">The layer on which the polyline is created</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="edges">A list of edges for this polyline. The edges have to form a single path without branches.
        /// The order is not important.
        /// </param>
        public Polyline(Layer layer, string nameFormat, IEnumerable<Edge> edges)
            : this(layer != null ? layer.Model.GetFreeId() : ulong.MaxValue, layer, nameFormat, edges) { }
        /// <summary>
        /// Initializes a new Polyline class
        /// </summary>
        /// <param name="id">The unique id for this polyline</param>
        /// <param name="layer">The layer on which the polyline is created</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="edges">A list of edges for this polyline. The edges have to form a single path without branches.
        /// The order is not important.
        /// </param>
        public Polyline(ulong id, Layer layer, string nameFormat, IEnumerable<Edge> edges) 
            : base(id, layer)
        {
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));
            if (!edges.Any())
                throw new ArgumentException(string.Format("{0} may not be empty", nameof(edges)));
            if (nameFormat == null)
                throw new ArgumentNullException(nameof(nameFormat));

            this.Name = string.Format(nameFormat, id);

            this.edges = new ObservableCollection<PEdge>(edges.Select(x => new PEdge(x, GeometricOrientation.Forward, this)));

            this.ModelGeometry.Polylines.Add(this);

            MakeConsistent(true, true);
        }


        /// <inheritdoc/>
        public override void AddToModel()
        {
            if (this.ModelGeometry.Polylines.Contains(this))
                throw new Exception("Geometry is already present in the model");

            this.ModelGeometry.Polylines.Add(this);

            if (ModelGeometry.HandleConsistency)
            {
                this.Edges.ForEach(x => x.Edge.PEdges.Add(x));
            }

            if (this.Layer != null && !this.Layer.Elements.Contains(this))
                this.Layer.Elements.Add(this);
        }
        /// <inheritdoc/>
        public override bool RemoveFromModel()
        {
            bool result = this.ModelGeometry.Polylines.Remove(this);

            if (ModelGeometry.HandleConsistency)
            {
                this.Edges.ForEach(x => x.Edge.PEdges.Remove(x));
            }

            return result;
        }
        /// <inheritdoc/>
        public override void MakeConsistent(bool notifyGeometryChanged, bool hasTopologyChanged)
        {
            if (hasTopologyChanged)
            {
                MakePEdgeConsistent();
            }

            OnTopologyChanged();
            OnGeometryChanged(notifyGeometryChanged);
        }

        private void MakePEdgeConsistent()
        {
            var orderedEdges = PolylineAlgorithms.Order(edges);

            if (!orderedEdges.isConnected)
                throw new Exception("Edges do not form a valid polyline (Either unconnected or loops)");

            this.edges.CollectionChanged -= Edges_CollectionChanged;
            this.edges.Clear();
            this.edges.AddRange(orderedEdges.polyline);
            this.edges.CollectionChanged += Edges_CollectionChanged;

            //Find start vertex
            var commonVertex = edges[0].Edge.Vertices[0];
            if (edges.Count > 1 && edges[1].Edge.Vertices.Contains(commonVertex))
                commonVertex = edges[0].Edge.Vertices[1];

            foreach (var pe in edges)
            {
                if (pe.Edge.Vertices[0] == commonVertex)
                    pe.Orientation = GeometricOrientation.Forward;
                else
                    pe.Orientation = GeometricOrientation.Backward;

                commonVertex = pe.Edge.Vertices.First(x => x != commonVertex);
            }

            foreach (var e in this.Edges)
            {
                e.MakeConsistent();
                e.Parent = this;
                e.Edge.GeometryChanged -= Edge_GeometryChanged;
                e.Edge.GeometryChanged += Edge_GeometryChanged;
                e.Edge.TopologyChanged -= Edge_TopologyChanged;
                e.Edge.TopologyChanged += Edge_TopologyChanged;
            }

            //Set prev and next ptr
            for (int i = 0; i < this.Edges.Count; ++i)
            {
                if (i != 0)
                    this.Edges[i].Prev = this.Edges[i - 1];
                else
                    this.edges[i].Prev = null;

                if (i != this.Edges.Count - 1)
                    this.Edges[i].Next = this.Edges[(i + 1)];
                else
                    this.Edges[i].Next = null;
            }
        }

        private void Edge_TopologyChanged(object sender)
        {
            MakePEdgeConsistent();
            NotifyTopologyChanged();
        }

        private void Edge_GeometryChanged(object sender)
        {
            NotifyGeometryChanged();
        }

        private void Edges_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var edge in e.OldItems)
                {
                    ((PEdge)edge).Edge.GeometryChanged -= Edge_GeometryChanged;
                    ((PEdge)edge).Edge.TopologyChanged -= Edge_TopologyChanged;
                }
            }

            if (ModelGeometry.HandleConsistency)
            {
                if (e.OldItems != null)
                {
                    foreach (var edge in e.OldItems)
                    {
                        var pedge = (PEdge)edge;
                        pedge.Edge.PEdges.Remove(pedge);
                    }
                }

                MakePEdgeConsistent();
            }

            NotifyTopologyChanged();
        }
    }
}
