using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Exchange.NetworkConnectors
{
    /// <summary>
    /// Connector between a <see cref="SimFlowNetworkEdge"/> and a <see cref="Polyline"/>
    /// </summary>
    internal class NetworkEdgeConnector : BaseNetworkConnector
    {
        /// <summary>
        /// The polyline
        /// </summary>
        internal Polyline EdgeGeometry { get; private set; }
        /// <summary>
        /// The network edge
        /// </summary>
        internal SimFlowNetworkEdge Edge { get; }

        /// <inheritdoc />
        internal override BaseGeometry Geometry => EdgeGeometry;

        /// <summary>
        /// Initializes a new instance of the NetworkEdgeConnector class
        /// </summary>
        /// <param name="geometry">The polyline</param>
        /// <param name="edge">The edge</param>
        internal NetworkEdgeConnector(Polyline geometry, SimFlowNetworkEdge edge)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (edge == null)
                throw new ArgumentNullException(nameof(edge));

            this.EdgeGeometry = geometry;
            this.Edge = edge;
            this.Edge.PropertyChanged += this.Edge_PropertyChanged;

            this.Edge.RepresentationReference = new Data.GeometricReference(EdgeGeometry.ModelGeometry.Model.File.Key, EdgeGeometry.Id);

            EdgeGeometry.Name = Edge.Name;

            OrientEdgeLoop();
            UpdateInstancePath();
            UpdateColor();
        }

        

        #region BaseNetworkConnector

        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
            UpdateInstancePath();
        }
        /// <inheritdoc />
        internal override void ChangeBaseGeometry(BaseGeometry geometry)
        {
            EdgeGeometry = geometry as Polyline;

            OrientEdgeLoop();
            UpdateInstancePath();
        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
            UpdateInstancePath();
        }
        /// <inheritdoc />
        public override void Dispose()
        {
            this.Edge.PropertyChanged -= Edge_PropertyChanged;
        }

        #endregion

        private void Edge_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimFlowNetworkEdge.Content))
            {
                UpdateInstancePath();
                UpdateColor();
            }
            else if (e.PropertyName == nameof(SimFlowNetworkEdge.Name))
                EdgeGeometry.Name = Edge.Name;
        }


        private void OrientEdgeLoop()
        {
            var startNode = Edge.Start is SimFlowNetwork ? ((SimFlowNetwork)Edge.Start).ConnectionToParentExitNode : Edge.Start;
            var endNode = Edge.End is SimFlowNetwork ? ((SimFlowNetwork)Edge.End).ConnectionToParentEntryNode : Edge.End;

            var startVertex = (Vertex)EdgeGeometry.ModelGeometry.GeometryFromId(startNode.RepresentationReference.GeometryId);
            var endVertex = (Vertex)EdgeGeometry.ModelGeometry.GeometryFromId(endNode.RepresentationReference.GeometryId);

            //Make sure that the edge connects the correct vertices
            if (EdgeGeometry.Edges.First().StartVertex != startVertex)
            {
                var pe = EdgeGeometry.Edges.First();
                if (pe.Orientation == GeometricOrientation.Forward)
                    pe.Edge.Vertices[0] = startVertex;
                else
                    pe.Edge.Vertices[1] = startVertex;
            }

            if (EdgeGeometry.Edges.Last().EndVertex != endVertex)
            {
                var pe = EdgeGeometry.Edges.Last();
                if (pe.Orientation == GeometricOrientation.Forward)
                    pe.Edge.Vertices[1] = endVertex;
                else
                    pe.Edge.Vertices[0] = endVertex;
            }
        }

        private void UpdateInstancePath()
        {
            if (Edge.Content != null)
            {
                using (AccessCheckingDisabler.Disable(Edge.Content.Factory))
                {
                    Edge.Content.InstancePath = EdgeGeometry.Edges.Select(x => x.StartVertex.Position)
                        .Append(EdgeGeometry.Edges.Last().EndVertex.Position).ToList();
                }
            }
        }

        internal void OnEdgeRedirected()
        {
            OrientEdgeLoop();
            UpdateInstancePath();
        }

        private void UpdateColor()
        {
            bool fromParent = true;
            Color color = NetworkColors.COL_NEUTRAL;

            if (Edge.Content == null)
            {
                color = NetworkColors.COL_EMPTY;
                fromParent = false;
            }

            UpdateColor(EdgeGeometry, color, fromParent);

            for (int i = 0; i < EdgeGeometry.Edges.Count; i++)
            {
                UpdateColor(EdgeGeometry.Edges[i].Edge, color, fromParent);

                if (i != 0)
                    UpdateColor(EdgeGeometry.Edges[i].StartVertex, color, fromParent);
            }
        }
        private void UpdateColor(BaseGeometry geo, Color color, bool fromParent)
        {
            geo.Color.Color = color;
            geo.Color.IsFromParent = fromParent;
        }
    }
}
