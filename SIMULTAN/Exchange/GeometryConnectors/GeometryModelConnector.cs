using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    /// <summary>
    /// Manages the connections between a <see cref="GeometryModel"/> and components.
    /// Handles creation/removal of other geometry connectors
    /// </summary>
    internal class GeometryModelConnector
    {
        #region Properties & Fields

        private MultiDictionary<ulong, ParameterSourceConnector> parameterSources =
            new MultiDictionary<ulong, ParameterSourceConnector>();
        private MultiDictionary<ulong, (SimGeometryParameterSource source, SimInstancePlacementGeometry placement)> missingParameterSources =
            new MultiDictionary<ulong, (SimGeometryParameterSource source, SimInstancePlacementGeometry placement)>();

        private MultiDictionary<ulong, SimInstancePlacementGeometry> placements = new MultiDictionary<ulong, SimInstancePlacementGeometry>();

        internal ComponentGeometryExchange Exchange { get; }
        internal GeometryModel GeometryModel { get; }

        private HashSet<BaseGeometry> geometryChangedGeometries = new HashSet<BaseGeometry>();
        private HashSet<BaseGeometry> topologyChangedGeometries = new HashSet<BaseGeometry>();
        private HashSet<BaseGeometry> offsetSurfacesChangedGeometries = new HashSet<BaseGeometry>();

        #endregion

        internal GeometryModelConnector(GeometryModel model, ComponentGeometryExchange exchange)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (exchange == null)
                throw new ArgumentNullException(nameof(exchange));

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
            this.GeometryModel.Geometry.OffsetModel.OffsetSurfaceChanged += this.OffsetModel_OffsetSurfaceChanged;
        }

        private void OffsetModel_OffsetSurfaceChanged(object sender, IEnumerable<Face> modifiedFaces)
        {
            if (modifiedFaces != null)
            {
                foreach (var geom in modifiedFaces)
                {
                    this.offsetSurfacesChangedGeometries.Add(geom);
                    foreach (var f in geom.PFaces)
                        this.offsetSurfacesChangedGeometries.Add(f.Volume);
                }
            }
            else
            {
                foreach (var geom in GeometryModel.Geometry.OffsetModel.Faces)
                {
                    var face = geom.Key.Item1;
                    this.offsetSurfacesChangedGeometries.Add(face);
                    foreach (var f in face.PFaces)
                        this.offsetSurfacesChangedGeometries.Add(f.Volume);
                }
            }

            if (Exchange.EnableGeometryEvents)
                SynchronizeChanges();
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
                //Check which parameter sources are available now
                if (missingParameterSources.TryGetValues(geom.Id, out var sources))
                {
                    List<(SimGeometryParameterSource, SimInstancePlacementGeometry)> stillMissingSources
                        = new List<(SimGeometryParameterSource, SimInstancePlacementGeometry)>();

                    foreach (var source in sources)
                    {
                        if (ParameterSourceConnector.SourceMatchesGeometry(source.source.GeometryProperty, geom))
                        {
                            var valueSourceConnector = new ParameterSourceConnector(geom, source.source, source.placement);
                            this.parameterSources.Add(geom.Id,
                                valueSourceConnector);
                            valueSourceConnector.OnConnectorsInitialized();
                        }
                        else
                        {
                            //Id exists, but wrong geometry type
                            stillMissingSources.Add(source);
                        }
                    }

                    missingParameterSources.Remove(geom.Id);
                    if (stillMissingSources.Count > 0)
                        missingParameterSources.Add(geom.Id, stillMissingSources);
                }

                if (this.placements.TryGetValues(geom.Id, out var placements))
                {
                    foreach (var placement in placements)
                        InitGeometryConnection(placement, geom, false);
                }
            }
        }

        private void Geometry_GeometryRemoved(object sender, IEnumerable<BaseGeometry> geometry)
        {
            foreach (var geom in geometry)
            {
                if (placements.TryGetValues(geom.Id, out var conns))
                {
                    //Set state to invalid
                    foreach (var placement in conns.ToList())
                    {
                        placement.State = SimInstancePlacementState.InstanceTargetMissing;
                    }
                }

                //Parameter Sources
                if (parameterSources.TryGetValues(geom.Id, out var sources))
                {
                    sources.ForEach(x => x.OnGeometryRemoved());

                    //Push all sources to missing list
                    foreach (var source in sources)
                        missingParameterSources.Add(geom.Id, (source.ParameterSource, source.Placement));
                    parameterSources.Remove(geom.Id);
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
                e.OldGeometry.OffsetModel.OffsetSurfaceChanged -= this.OffsetModel_OffsetSurfaceChanged;
            }

            Exchange.ProjectData.Components.EnableReferencePropagation = false;

            //Update connectors
            if (e.NewGeometry == null) //There is no new Geometry -> Invalidate all placements
            {
                foreach (var cons in placements)
                {
                    foreach (var placement in cons.Value)
                    {
                        placement.State = SimInstancePlacementState.InstanceTargetMissing;
                    }
                }
            }
            else //Check which connectors have to be added/removed
            {
                //Parameter Sources
                {
                    var oldMissing = new MultiDictionary<ulong,
                        (SimGeometryParameterSource source, SimInstancePlacementGeometry placement)>(this.missingParameterSources);
                    var oldSources = new MultiDictionary<ulong, ParameterSourceConnector>(this.parameterSources);

                    missingParameterSources.Clear();
                    parameterSources.Clear();

                    //Check which existing connectors are missing and update the other ones
                    foreach (var cons in oldSources)
                    {
                        var geometry = e.NewGeometry.GeometryFromId(cons.Key);

                        foreach (var con in cons.Value)
                        {
                            if (geometry != null && ParameterSourceConnector.SourceMatchesGeometry(con.ParameterSource.GeometryProperty, geometry))
                            {
                                con.ChangeGeometry(geometry);
                                parameterSources.Add(cons.Key, con);
                            }
                            else
                            {
                                con.OnGeometryRemoved();
                                missingParameterSources.Add(cons.Key, (con.ParameterSource, con.Placement));
                            }
                        }
                    }

                    //Check which previously missing sources are now available
                    foreach (var missings in oldMissing)
                    {
                        var geometry = e.NewGeometry.GeometryFromId(missings.Key);

                        foreach (var missing in missings.Value)
                        {
                            if (geometry != null &&
                                ParameterSourceConnector.SourceMatchesGeometry(missing.source.GeometryProperty, geometry))
                            {
                                var valueSourceConnector = new ParameterSourceConnector(geometry, missing.source, missing.placement);
                                this.parameterSources.Add(geometry.Id,
                                    valueSourceConnector);
                                valueSourceConnector.OnConnectorsInitialized();
                            }
                            else
                                missingParameterSources.Add(missings.Key, missing);
                        }
                    }
                }

                //Geometry connectors
                {
                    foreach (var pls in placements)
                    {
                        var geometry = e.NewGeometry.GeometryFromId(pls.Key);
                        foreach (var pl in pls.Value)
                            InitGeometryConnection(pl, geometry, false);
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
                e.NewGeometry.OffsetModel.OffsetSurfaceChanged += this.OffsetModel_OffsetSurfaceChanged;
            }
        }


        internal void SynchronizeChanges()
        {
            var enableReferencePropagation = Exchange.ProjectData.Components.EnableReferencePropagation;
            Exchange.ProjectData.Components.EnableReferencePropagation = false;

            foreach (var geom in this.topologyChangedGeometries)
            {
                if (parameterSources.TryGetValues(geom.Id, out var sources))
                    sources.ForEach(x => x.OnGeometryChanged());
            }

            foreach (var geom in this.geometryChangedGeometries)
            {
                if (parameterSources.TryGetValues(geom.Id, out var sources))
                    sources.ForEach(x => x.OnGeometryChanged());
            }

            //Invalidate/Recalculate all references
            Exchange.ProjectData.Components.EnableReferencePropagation = enableReferencePropagation;

            this.topologyChangedGeometries.Clear();
            this.geometryChangedGeometries.Clear();
            this.offsetSurfacesChangedGeometries.Clear();
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
            foreach (var sources in this.parameterSources)
                sources.Value.ForEach(x => x.OnConnectorsInitialized());

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
            this.placements.Add(placement.GeometryId, placement);

            //Create connection
            var geometry = this.GeometryModel.Geometry.GeometryFromId(placement.GeometryId);
            InitGeometryConnection(placement, geometry, initialize);

            //Parameter source connectors
            foreach (var parameter in placement.Instance.Component.Parameters)
            {
                if (parameter.ValueSource is SimGeometryParameterSource gps)
                {
                    if (geometry != null && ParameterSourceConnector.SourceMatchesGeometry(gps.GeometryProperty, geometry))
                    {
                        var valueSourceConnector = new ParameterSourceConnector(geometry, gps, placement);
                        this.parameterSources.Add(placement.GeometryId,
                            valueSourceConnector);
                        if (initialize)
                            valueSourceConnector.OnConnectorsInitialized();
                    }
                    else //Either no geometry or wrong type
                    {
                        //Set parameter instance value
                        placement.Instance.InstanceParameterValuesPersistent[parameter] = double.NaN;
                        ((SimDoubleParameter)parameter).Value = double.NaN;
                        this.missingParameterSources.Add(placement.GeometryId, (gps, placement));
                    }
                }
            }
        }


        private bool InitGeometryConnection(SimInstancePlacementGeometry placement, BaseGeometry geometry, bool createNew)
        {
            bool isValid = false;

            //When adding a if here, also add it to ComponentGeometryExchangeNew.IsValidAssociation
            if (geometry is Vertex)
                isValid = placement.Instance.Component.InstanceType == SimInstanceType.AttributesPoint;
            else if (geometry is Edge)
                isValid = placement.Instance.Component.InstanceType == SimInstanceType.AttributesEdge;
            else if (geometry is Face)
            {
                isValid = placement.Instance.Component.InstanceType == SimInstanceType.AttributesFace;

                if (isValid)
                {
                    using (AccessCheckingDisabler.Disable(placement.Instance.Component.Factory))
                    {
                        //Create din/dout
                        ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component,
                            ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_IN, ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN,
                        SimParameterInstancePropagation.PropagateIfInstance, 0.0);
                        ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component,
                            ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_OUT, ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT,
                            SimParameterInstancePropagation.PropagateIfInstance, 0.0);
                    }
                }
            }
            if (geometry is Volume)
            {
                if (placement.Instance.Component.InstanceType == SimInstanceType.Entity3D)
                    isValid = true;
                else if (placement.Instance.Component.InstanceType == SimInstanceType.NetworkNode)
                {
                    return true; //This is a instance that connects a network node with it's parent
                }
            }

            if (isValid)
            {
                placement.State = SimInstancePlacementState.Valid;

                this.Exchange.NotifyAssociationChanged(new BaseGeometry[] { geometry });
                if (placement.Instance.InstanceType == SimInstanceType.AttributesFace)
                    this.Exchange.NotifyGeometryInvalidated(new BaseGeometry[] { geometry });
            }
            else
            {
                placement.State = SimInstancePlacementState.InstanceTargetMissing;
            }

            return isValid;
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
        }
        /// <summary>
        /// Called when a placement has been removed which references this <see cref="GeometryModel"/>
        /// </summary>
        /// <param name="placement">The removed placement</param>
        internal void OnPlacementRemoved(SimInstancePlacementGeometry placement)
        {
            if (placement.FileId != this.GeometryModel.File.Key)
                throw new ArgumentException("Placement does not belong to this GeometryModel");

            //Remove and detach connected parameter sources
            if (parameterSources.TryGetValues(placement.GeometryId, out var paramSources))
            {
                foreach (var sourceConnector in paramSources.ToList())
                {
                    if (sourceConnector.Placement == placement)
                    {
                        sourceConnector.OnPlacementRemoved();
                        parameterSources.Remove(placement.GeometryId, sourceConnector);
                    }
                }
            }

            //Remove from missing sources
            foreach (var param in placement.Instance.Component.Parameters)
            {
                if (param.ValueSource is SimGeometryParameterSource gps)
                    missingParameterSources.Remove(placement.GeometryId, (gps, placement));
            }

            this.placements.Remove(placement.GeometryId, placement);

            var geometry = GeometryModel.Geometry.GeometryFromId(placement.GeometryId);
            if (geometry != null)
            {
                this.Exchange.NotifyAssociationChanged(new BaseGeometry[] { geometry });

                if (placement.Instance.InstanceType == SimInstanceType.AttributesFace)
                    this.Exchange.NotifyGeometryInvalidated(new BaseGeometry[] { geometry });
            }
        }


        internal void OnParameterSourceAdded(SimGeometryParameterSource source, SimInstancePlacementGeometry placement)
        {
            var geometry = this.GeometryModel.Geometry.GeometryFromId(placement.GeometryId);

            if (geometry != null)
            {
                var valueSourceConnector = new ParameterSourceConnector(geometry, source, placement);
                this.parameterSources.Add(placement.GeometryId,
                    valueSourceConnector);
                valueSourceConnector.OnConnectorsInitialized();
            }
            else
            {
                this.missingParameterSources.Add(placement.GeometryId, (source, placement));
            }
        }
        internal void OnParameterSourceRemoved(SimGeometryParameterSource source, SimInstancePlacementGeometry placement)
        {
            if (parameterSources.TryGetValues(placement.GeometryId, out var removeConnectors))
            {
                //There is only one entry to remove
                var connector = removeConnectors.First(x => x.ParameterSource == source);
                connector.OnSourceRemoved();
                parameterSources.Remove(placement.GeometryId, connector);
            }

            missingParameterSources.Remove(placement.GeometryId, (source, placement));
        }

        internal void OnParameterSourceFilterChanged(SimGeometryParameterSource source)
        {
            var connectors = parameterSources.SelectMany(x => x.Value.Where(y => y.ParameterSource == source));
            connectors.ForEach(x => x.OnFilterChanged());
        }


        /// <summary>
        /// Notifies the connector that a parameters value has changed which is used in an instance that connects 
        /// to this <see cref="GeometryModel"/>
        /// </summary>
        /// <param name="placement">The placement in which the parameter has been changed</param>
        /// <param name="parameter">The modified parameter</param>
        /// <returns>A list of all Geometries that are affected by the parameter change</returns>
        internal IEnumerable<BaseGeometry> OnParameterValueChanged(SimInstancePlacementGeometry placement, SimBaseParameter parameter)
        {
            if (placement.FileId != this.GeometryModel.File.Key)
                throw new ArgumentException("Placement does not belong to this GeometryModel");

            if (placement.Instance.InstanceType == SimInstanceType.AttributesFace)
            {
                if (parameter.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_IN) ||
                    parameter.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_OUT))
                {
                    var geometry = GeometryModel.Geometry.GeometryFromId(placement.GeometryId);
                    if (geometry != null)
                        yield return geometry;
                }
            }
            else if (placement.Instance.InstanceType == SimInstanceType.Entity3D)
            {
                if (parameter.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_PARAM_TO_GEOMETRY))
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
            if (this.placements.TryGetValues(geometry.Id, out var cons))
            {
                foreach (var con in cons)
                    yield return con;
            }
        }

        public void Dispose()
        {
            this.GeometryModel.Geometry.GeometryChanged -= this.Geometry_GeometryChanged;
            this.GeometryModel.Geometry.TopologyChanged -= this.Geometry_TopologyChanged;
            this.GeometryModel.Geometry.GeometryAdded -= this.Geometry_GeometryAdded;
            this.GeometryModel.Geometry.GeometryRemoved -= this.Geometry_GeometryRemoved;
            this.GeometryModel.Geometry.OffsetModel.OffsetSurfaceChanged -= this.OffsetModel_OffsetSurfaceChanged;
        }
    }
}
