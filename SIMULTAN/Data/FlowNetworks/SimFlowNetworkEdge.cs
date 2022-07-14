using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.FlowNetworks
{
    public class SimFlowNetworkEdge : SimFlowNetworkElement
    {
        private long loadingStartId = -1;
        private long loadingEndId = -1;

        #region PROPERTIES: Specific (Start, End)

        protected SimFlowNetworkNode start;
        public SimFlowNetworkNode Start
        {
            get { return this.start; }
            set
            {
                bool no_change = (this.start == null && value == null) || (this.start != null && value != null && this.start.ID == value.ID);
                if (no_change)
                    return;

                var old_value = (this.start == null) ? null : new SimFlowNetworkNode(this.start);
                this.start = value;
                var new_value = (this.start == null) ? null : new SimFlowNetworkNode(this.start);
                this.SetValidity();
                this.NotifyPropertyChanged(nameof(Start));
            }
        }

        protected SimFlowNetworkNode end;
        public SimFlowNetworkNode End
        {
            get { return this.end; }
            set
            {
                bool no_change = (this.end == null && value == null) || (this.end != null && value != null && this.end.ID == value.ID);
                if (no_change)
                    return;

                var old_value = (this.end == null) ? null : new SimFlowNetworkNode(this.end);
                this.end = value;
                var new_value = (this.end == null) ? null : new SimFlowNetworkNode(this.end);
                this.SetValidity();
                this.NotifyPropertyChanged(nameof(End));
            }
        }
        #endregion

        #region METHODS: General overrides

        internal override void SetValidity()
        {
            if (this.start == null || this.end == null)
                this.IsValid = false;
            else
                this.IsValid = true;
        }

        #endregion

        #region .CTOR
        internal SimFlowNetworkEdge(IReferenceLocation _location, SimFlowNetworkNode _start, SimFlowNetworkNode _end)
            : base(_location)
        {
            this.name = "Edge " + this.ID.LocalId.ToString();
            this.start = _start;
            this.end = _end;
            this.SetValidity();
        }
        #endregion

        #region COPY .CTOR

        internal SimFlowNetworkEdge(SimFlowNetworkEdge _original, SimFlowNetworkNode _start_copy, SimFlowNetworkNode _end_copy)
            : base(_original)
        {
            this.name = _original.Name;
            this.description = _original.description;
            // do not copy start and end !!!
            this.start = _start_copy;
            this.end = _end_copy;
            this.SetValidity();
        }

        #endregion

        #region PARSING .CTOR

        // for parsing
        // the content component has to be parsed FIRST
        internal SimFlowNetworkEdge(Guid _location, long _id, string _name, string _description, bool _is_valid,
                            SimFlowNetworkNode _start, SimFlowNetworkNode _end)
            : base(_location, _id, _name, _description)
        {
            this.is_valid = _is_valid;

            this.start = _start;
            this.end = _end;
            this.SetValidity();
        }

        internal SimFlowNetworkEdge(Guid location, long id, string name, string description, bool isValid,
            long startId, long endId)
            : base(location, id, name, description)
        {
            this.is_valid = isValid;

            this.loadingStartId = startId;
            this.loadingEndId = endId;
        }

        #endregion

        #region METHODS: ToString

        public override string ToString()
        {
            return "Edge " + this.ID.ToString() + " " + this.ContentToString();
        }

        #endregion

        #region METHODS: Content Check

        internal bool CanCalculateFlow(bool _in_flow_dir)
        {
            if (this.Content == null) return false;

            if (_in_flow_dir)
            {
                if (this.Start == null) return false;
                if (this.Start.Content == null && !(this.Start is SimFlowNetwork)) return false;
                return true;
            }
            else
            {
                if (this.End == null) return false;
                if (this.End.Content == null && !(this.End is SimFlowNetwork)) return false;
                return true;
            }
        }

        #endregion

        public void RestoreReferences(IDictionary<long, SimFlowNetworkNode> nodes, IDictionary<long, SimFlowNetwork> networks)
        {
            this.start = FindNode(this.loadingStartId, nodes, networks);
            this.end = FindNode(this.loadingEndId, nodes, networks);

            SetValidity();

            this.loadingStartId = -1;
            this.loadingEndId = -1;
        }
        private SimFlowNetworkNode FindNode(long id, IDictionary<long, SimFlowNetworkNode> nodes, IDictionary<long, SimFlowNetwork> networks)
        {
            if (nodes.TryGetValue(id, out var node))
                return node;
            if (networks.TryGetValue(id, out var nw))
                return nw;

            return null;
        }

        public void UpdateRealization()
        {
        }

        internal override void CommunicatePositionUpdateToContent()
        {
            if (this.Content != null && this.RepresentationReference == GeometricReference.Empty)
            {
                var placement = (SimInstancePlacementNetwork)this.Content.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork p && p.NetworkElement == this);
                if (placement != null)
                {
                    var nwOffset = GetOffset();
                    var startPos = new Point3D((Start.Position.X + nwOffset.X) * placement.PathScale, 0, (Start.Position.Y + nwOffset.Y) * placement.PathScale);
                    var endPos = new Point3D((End.Position.X + nwOffset.X) * placement.PathScale, 0, (End.Position.Y + nwOffset.Y) * placement.PathScale);

                    using (AccessCheckingDisabler.Disable(this.Content.Component.Factory))
                    {
                        this.Content.InstancePath = new List<Point3D> { startPos, endPos };
                    }
                }
            }
        }
    }
}
