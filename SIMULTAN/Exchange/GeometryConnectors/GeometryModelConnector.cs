using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    /// <summary>
    /// Manages the connections between a <see cref="GeometryModel"/> and components.
    /// Handles creation/removal of other geometry connectors
    /// </summary>
    internal class GeometryModelConnector
    {
        #region Properties & Fields

        private MultiDictionary<ulong, BaseGeometryConnector> connectors = 
            new MultiDictionary<ulong, BaseGeometryConnector>();

        private MultiDictionary<ulong, SimInstancePlacementGeometry> missingGeometryPlacements = 
            new MultiDictionary<ulong, SimInstancePlacementGeometry>();

        internal ComponentGeometryExchange Exchange { get; }
        internal GeometryModel GeometryModel { get; }

        private HashSet<BaseGeometry> geometryChangedGeometries = new HashSet<BaseGeometry>();
        private HashSet<BaseGeometry> topologyChangedGeometries = new HashSet<BaseGeometry>();

        #endregion

        internal GeometryModelConnector(GeometryModel model, ComponentGeometryExchange exchange)
        {
            if (model == null) 
                throw new ArgumentNullException(nameof(model));
            if (exchange == null)
                throw new ArgumentNullException(nameof (exchange));

            this.Exchange = exchange;
            this.GeometryModel = model;
        }

        internal void Initialize()
        {
            CreateConnectors();

            this.GeometryModel.Replaced += this.GeometryModel_Replaced;
            this.GeometryModel.Geometry.GeometryChanged += this.Geometry_GeometryChanged;
            this.GeometryModel.Geometry.TopologyChanged += this.Geometry_TopologyChanged;
            this.GeometryModel.Geometry.GeometryAdded += this.Geometry_GeometryAdded;
            this.GeometryModel.Geometry.GeometryRemoved += this.Geometry_GeometryRemoved;
        }


        #region Geometry Event Handler

        private void Geometry_TopologyChanged(object sender, IEnumerable<BaseGeometry> geometries)
        {
            foreach (var geom in geometries)
                this.topologyChangedGeometries.Add(geom);

            if (Exchange.EnableGeometryEvents)
                SynchronizeChanges();
        }

        private void Geometry_GeometryChanged(object sender, IEnumerable<BaseGeometry> geometries)
        {
            foreach (var geom in geometries)
                this.geometryChangedGeometries.Add(geom);

            if (Exchange.EnableGeometryEvents)
                SynchronizeChanges();
        }

        private void Geometry_GeometryAdded(object sender, IEnumerable<BaseGeometry> geometry)
        {
            foreach (var geom in geometry)
            {
                if (missingGeometryPlacements.TryGetValues(geom.Id, out var placements))
                {
                    foreach (var placement in placements)
                        AddConnector(placement, geom, true);

                    missingGeometryPlacements.Remove(geom.Id);
                }

                //When a face gets added which covers a hole: Inform all faces that have this hole
                if (geom is Face face)
                {
                    foreach (var otherFace in face.Boundary.Faces)
                    {
                        if (otherFace != face)
                        {
                            if (connectors.TryGetValues(otherFace.Id, out var otherFaceCons))
                            {
                                foreach (var con in otherFaceCons)
                                {
                                    con.OnTopologyChanged();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void Geometry_GeometryRemoved(object sender, IEnumerable<BaseGeometry> geometry)
        {
            foreach (var geom in geometry)
            {
                if (connectors.TryGetValues(geom.Id, out var conns))
                {
                    //Notify
                    foreach (var con in conns.ToList())
                    {
                        con.OnGeometryRemoved();
                    }

                    //Push all remaining connectors to missing list. This is needed because OnGeometryRemoved might remove sub connectors
                    conns = connectors[geom.Id];
                    foreach (var con in conns)
                        missingGeometryPlacements.Add(geom.Id, con.Placement);
                    connectors.Remove(geom.Id);
                }

                //When a face is deleted which covers a hole: Inform all face connectors of the faces that have this hole inside
                if (geom is Face face)
                {
                    foreach (var otherFace in face.Boundary.Faces)
                    {
                        if (otherFace != face)
                        {
                            if (connectors.TryGetValues(otherFace.Id, out var otherFaceCons))
                            {
                                foreach (var con in otherFaceCons)
                                {
                                    con.OnTopologyChanged();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GeometryModel_Replaced(object sender, GeometryModelReplacedEventArgs e)
        {
            if (e.OldGeometry != null)
            {
                e.OldGeometry.GeometryChanged -= this.Geometry_GeometryChanged;
                e.OldGeometry.TopologyChanged -= this.Geometry_TopologyChanged;
                e.OldGeometry.GeometryAdded -= this.Geometry_GeometryAdded;
                e.OldGeometry.GeometryRemoved -= this.Geometry_GeometryRemoved;
            }

            Exchange.ProjectData.Components.EnableReferencePropagation = false;

            //Update connectors
            if (e.NewGeometry == null) //There is no new Geometry -> Invalidate all placements
            {
                foreach (var cons in connectors)
                {
                    foreach (var con in cons.Value)
                    {
                        con.OnGeometryRemoved();
                        missingGeometryPlacements.Add(cons.Key, con.Placement);
                    }
                }

                connectors.Clear();
            }
            else //Check which connectors have to be added/removed
            {
                //Copy missing items (to check them later)
                var oldMissings = new MultiDictionary<ulong, SimInstancePlacementGeometry>(missingGeometryPlacements);
                var oldConnectors = new MultiDictionary<ulong, BaseGeometryConnector>(connectors);

                missingGeometryPlacements.Clear();
                connectors.Clear();

                //Check which connectors are missing and update their geometry references
                foreach (var cons in oldConnectors)
                {
                    var geometry = e.NewGeometry.GeometryFromId(cons.Key);

                    foreach (var con in cons.Value)
                    {
                        bool success = false;

                        if (geometry != null) //Try to update connectors to new geometry
                        {
                            success = con.ChangeBaseGeometry(geometry);
                            if (!success)
                                this.connectors.Add(geometry.Id, con);
                        }

                        if (!success) //Either geometry missing or wrong type -> delete connector
                        {
                            con.OnGeometryRemoved();
                            this.missingGeometryPlacements.Add(cons.Key, con.Placement);
                        }
                    }
                }

                //Check which missing connectors are now available
                foreach (var missing in oldMissings)
                {
                    var geometry = e.NewGeometry.GeometryFromId(missing.Key);

                    foreach (var placement in missing.Value)
                    {
                        bool success = false;

                        if (geometry != null)
                            success = AddConnector(placement, geometry, true);

                        if (!success) //Either geometry not found or type mismatch
                            missingGeometryPlacements.Add(missing.Key, placement);
                    }
                }
            }

            Exchange.ProjectData.Components.EnableReferencePropagation = true;

            if (e.NewGeometry != null)
            {
                e.NewGeometry.GeometryChanged += this.Geometry_GeometryChanged;
                e.NewGeometry.TopologyChanged += this.Geometry_TopologyChanged;
                e.NewGeometry.GeometryAdded += this.Geometry_GeometryAdded;
                e.NewGeometry.GeometryRemoved += this.Geometry_GeometryRemoved;
            }
        }


        internal void SynchronizeChanges()
        {
            var enableReferencePropagation = Exchange.ProjectData.Components.EnableReferencePropagation;
            Exchange.ProjectData.Components.EnableReferencePropagation = false;

            foreach (var geom in this.topologyChangedGeometries)
            {
                if (connectors.TryGetValues(geom.Id, out var cons))
                    cons.ForEach(x => x.OnTopologyChanged());
            }

            foreach (var geom in this.geometryChangedGeometries)
            {
                if (connectors.TryGetValues(geom.Id, out var cons))
                    cons.ForEach(x => x.OnGeometryChanged());
            }

            //Invalidate/Recalculate all references
            Exchange.ProjectData.Components.EnableReferencePropagation = enableReferencePropagation;
        }

        #endregion


        private void CreateConnectors()
        {
            //Performance disabling
            var enableReferencePropagation = Exchange.ProjectData.Components.EnableReferencePropagation;
            Exchange.ProjectData.Components.EnableReferencePropagation = false;
            Exchange.EnableNotifyGeometryInvalidated = false;

            //Check the components to find instances that attach to this geometryModel
            CreateConnectors(GeometryModel.File.Key, Exchange.ProjectData.Components);

            Exchange.EnableNotifyGeometryInvalidated = true;
            Exchange.NotifyGeometryInvalidated(null);

            //Initialize all connectors
            foreach (var con in this.connectors)
                con.Value.ForEach(x => x.OnConnectorsInitialized());

            //Invalidate/Recalculate all references
            Exchange.ProjectData.Components.EnableReferencePropagation = enableReferencePropagation;
        }

        private void CreateConnectors(int modelKey, IEnumerable<SimComponent> components)
        {
            foreach (var component in components)
            {
                if (component != null)
                {
                    foreach (var inst in component.Instances)
                    {
                        foreach (var placement in inst.Placements.Where(x => x is SimInstancePlacementGeometry gp && gp.FileId == modelKey))
                        {
                            CreateConnector((SimInstancePlacementGeometry)placement, false);
                        }
                    }

                    //Child components
                    CreateConnectors(modelKey, component.Components.Select(x => x.Component));
                }
            }
        }

        private void CreateConnector(SimInstancePlacementGeometry placement, bool initialize)
        {
            //Create connector
            var geometry = this.GeometryModel.Geometry.GeometryFromId(placement.GeometryId);
            bool isGeometryValid = false;
            if (geometry != null)
            {
                isGeometryValid = AddConnector(placement, geometry, initialize); //False when the geometry type doesn't match the connector
            }

            if (!isGeometryValid) //Geometry doesn't exist
            {
                missingGeometryPlacements.Add(placement.GeometryId, placement);
                placement.State = SimInstancePlacementState.InstanceTargetMissing;
            }
        }
    
        private bool AddConnector(SimInstancePlacementGeometry placement, BaseGeometry geometry, bool initialize)
        {
            BaseGeometryConnector connector = null;

            //When adding a if here, also add it to ComponentGeometryExchangeNew.IsValidAssociation
            if (geometry is Face face)
            {
                if (placement.Instance.Component.InstanceType == SimInstanceType.AttributesFace)
                {
                    connector = new FaceConnector(face, placement);
                }
                else if (placement.Instance.Component.InstanceType == SimInstanceType.GeometricSurface)
                {
                    connector = new VolumeFaceConnector(face, placement, this);
                }
            }
            else if (geometry is Volume volume)
            {
                if (placement.Instance.Component.InstanceType == SimInstanceType.Entity3D)
                {
                    connector = new VolumeConnector(volume, placement, this);
                }
                else if (placement.Instance.Component.InstanceType == SimInstanceType.GeometricVolume)
                {
                    connector = new VolumeVolumeConnector(volume, placement);
                }
                else if (placement.Instance.Component.InstanceType == SimInstanceType.NetworkNode)
                {
                    return true; //This is a instance that connects a network node with it's parent
                }
            }
            else if (geometry is EdgeLoop loop)
            {
                if (placement.Instance.Component.InstanceType == SimInstanceType.GeometricSurface)
                    connector = new VolumeFaceHoleConnector(loop, placement);
            }
            else if(geometry is Edge edge)
            {
                if(placement.Instance.Component.InstanceType == SimInstanceType.AttributesEdge)
                    connector = new EdgeConnector(edge, placement);
            }
            else if(geometry is Vertex vertex)
            {
                if(placement.Instance.Component.InstanceType == SimInstanceType.AttributesPoint)
                    connector = new VertexConnector(vertex, placement);
            }

            if (connector != null)
            {
                connectors.Add(geometry.Id, connector);

                if (initialize)
                    connector.OnConnectorsInitialized();

                this.Exchange.NotifyAssociationChanged(new BaseGeometry[] { geometry });
                if (placement.Instance.InstanceType == SimInstanceType.AttributesFace)
                    this.Exchange.NotifyGeometryInvalidated(new BaseGeometry[] { geometry });
                return true;
            }

            return false;
        }

        /// <summary>
        /// Called when a placement has been added which references this <see cref="GeometryModel"/>
        /// </summary>
        /// <param name="placement">The added placement</param>
        internal void OnPlacementAdded(SimInstancePlacementGeometry placement)
        {
            if (placement.FileId != this.GeometryModel.File.Key)
                throw new ArgumentException("Placement does not belong to this GeometryModel");

            CreateConnector(placement, true);

            //If AttributesFace, inform potential volume components
            if (placement.Instance.InstanceType == SimInstanceType.AttributesFace)
            {
                if (connectors.TryGetValues(placement.GeometryId, out var cons))
                {
                    foreach (var con in cons.OfType<VolumeFaceConnector>())
                    {
                        con.OnAttributesFacePlacementAdded(placement);
                    }
                }
            }
        }
        /// <summary>
        /// Called when a placement has been removed which references this <see cref="GeometryModel"/>
        /// </summary>
        /// <param name="placement">The removed placement</param>
        internal void OnPlacementRemoved(SimInstancePlacementGeometry placement)
        {
            if (placement.FileId != this.GeometryModel.File.Key)
                throw new ArgumentException("Placement does not belong to this GeometryModel");

            if (connectors.TryGetValues(placement.GeometryId, out var cons))
            {
                var con = cons.FirstOrDefault(x => x.Placement == placement);
                if (con != null)
                {
                    connectors.Remove(placement.GeometryId, con);
                    con.OnPlacementRemoved();

                    var geometry = GeometryModel.Geometry.GeometryFromId(placement.GeometryId);
                    if (geometry != null)
                    {
                        this.Exchange.NotifyAssociationChanged(new BaseGeometry[] { geometry });

                        if (placement.Instance.InstanceType == SimInstanceType.AttributesFace)
                            this.Exchange.NotifyGeometryInvalidated(new BaseGeometry[] { geometry });
                    }
                }

                //If AttributesFace, inform potential volume components
                if (placement.Instance.InstanceType == SimInstanceType.AttributesFace)
                {
                    foreach (var volFaceCon in cons.OfType<VolumeFaceConnector>())
                    {
                        volFaceCon.OnAttributesFacePlacementRemoved(placement);
                    }
                }
            }
            else //Geometry missing
            {
                missingGeometryPlacements.Remove(placement.GeometryId, placement);
            }
        }

        /// <summary>
        /// Called when the name of a BaseGeometry has changed
        /// </summary>
        /// <param name="geometry">The geometry in which the name has changed</param>
        internal void OnGeometryNameChanged(BaseGeometry geometry)
        {
            if (this.connectors.TryGetValues(geometry.Id, out var cons))
                cons.ForEach(x => x.OnGeometryNameChanged(geometry));
        }

        /// <summary>
        /// Notifies the connector that a parameters value has changed which is used in an instance that connects 
        /// to this <see cref="GeometryModel"/>
        /// </summary>
        /// <param name="placement">The placement in which the parameter has been changed</param>
        /// <param name="parameter">The modified parameter</param>
        /// <returns>A list of all Geometries that are affected by the parameter change</returns>
        internal IEnumerable<BaseGeometry> OnParameterValueChanged(SimInstancePlacementGeometry placement, SimParameter parameter)
        {
            if (placement.FileId != this.GeometryModel.File.Key)
                throw new ArgumentException("Placement does not belong to this GeometryModel");
            
            if (placement.Instance.InstanceType == SimInstanceType.AttributesFace)
            {
                if (parameter.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN ||
                    parameter.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT)
                {
                    var geometry = GeometryModel.Geometry.GeometryFromId(placement.GeometryId);
                    if (geometry != null)
                        yield return geometry;
                }
            }
        }
        /// <summary>
        /// Returns all placements for a specific geometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>A list of all placements connecting to the Geometry</returns>
        internal IEnumerable<SimInstancePlacementGeometry> GetPlacements(BaseGeometry geometry)
        {
            //This method assumes that geometry is part of the connected model
            if (this.connectors.TryGetValues(geometry.Id, out var cons))
            {
                foreach (var con in cons)
                    yield return con.Placement;
            }
        }
        /// <summary>
        /// Returns all connectors for a specific geometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>A list of all connectors connecting to the Geometry</returns>
        internal IEnumerable<BaseGeometryConnector> GetConnectors(BaseGeometry geometry)
        {
            if (connectors.TryGetValues(geometry.Id, out var cons))
                return cons;
            return Enumerable.Empty<BaseGeometryConnector>();
        }

        public void Dispose()
        {
            this.GeometryModel.Geometry.GeometryChanged -= this.Geometry_GeometryChanged;
            this.GeometryModel.Geometry.TopologyChanged -= this.Geometry_TopologyChanged;
            this.GeometryModel.Geometry.GeometryAdded -= this.Geometry_GeometryAdded;
            this.GeometryModel.Geometry.GeometryRemoved -= this.Geometry_GeometryRemoved;
        }
    }
}
