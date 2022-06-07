using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    /// <summary>
    /// Manages the connection between a <see cref="SimComponentInstance"/> of type <see cref="SimInstanceType.GeometricSurface"/>
    /// and a <see cref="Face"/>
    /// </summary>
    internal class VolumeFaceConnector : BaseGeometryConnector<Face>
    {
        private Volume volume;

        /// <summary>
        /// Initializes a new instance of the <see cref="VolumeFaceConnector"/> class
        /// </summary>
        /// <param name="face">The face</param>
        /// <param name="placement">The instance placement</param>
        /// <param name="modelConnector">The connector to the <see cref="GeometryModel"/></param>
        internal VolumeFaceConnector(Face face, SimInstancePlacementGeometry placement,
            GeometryModelConnector modelConnector) : base(face, placement)
        {
            if (modelConnector == null)
                throw new ArgumentNullException(nameof(modelConnector));

            //Called from the parent constructor
            //FindParentVolume();

            //Create parameters
            using (AccessCheckingDisabler.Disable(placement.Instance.Factory))
            {
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_AREA,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_AREA_MIN,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_AREA_MAX,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_WIDTH,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_WIDTH_MIN,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_WIDTH_MAX,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_HEIGHT,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_HEIGHT_MIN,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_HEIGHT_MAX,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_K_F_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);
                ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component, ReservedParameters.RP_K_D_AXES,
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

                ExchangeHelpers.CreateAssetIfNotExists(placement.Instance.Component, face);

                foreach (var attributesFacePlacement in modelConnector.GetPlacements(face)
                    .Where(x => x.Instance.InstanceType == SimInstanceType.AttributesFace))
                {
                    OnAttributesFacePlacementAdded(attributesFacePlacement);
                }

                this.Placement.Instance.Component.Name = face.Name;
            }

            //Create/remove holes
            UpdateSubComponents();
        }


        private void UpdateSubComponents()
        {
            var component = Placement.Instance.Component;

            using (AccessCheckingDisabler.Disable(component.Factory))
            {
                Dictionary<ulong, BaseGeometry> holes = new Dictionary<ulong, BaseGeometry>();
                //Key: Hole EdgeLoop Id for Face Holes, Value: Face Id
                Dictionary<ulong, ulong> holeLoopsForFaceHoles = new Dictionary<ulong, ulong>();

                foreach (var hole in TypedGeometry.Holes)
                {
                    var holeFace = hole.Faces.FirstOrDefault(hf => hf.Boundary == hole);
                    if (holeFace != null)
                    {
                        holes.Add(holeFace.Id, holeFace);
                        holeLoopsForFaceHoles.Add(hole.Id, holeFace.Id);
                    }
                    else
                    {
                        holes.Add(hole.Id, hole);
                    }
                }

                for (int i = 0; i < component.Components.Count; ++i)
                {
                    var subComp = component.Components[i];

                    if (subComp.Component != null && subComp.Component.InstanceType == SimInstanceType.GeometricSurface)
                    {
                        bool needsRemove = true;

                        var inst = subComp.Component.Instances.FirstOrDefault();
                        if (inst != null)
                        {
                            var pl = (SimInstancePlacementGeometry)inst.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                            if (pl.FileId == TypedGeometry.ModelGeometry.Model.File.Key)
                            {
                                if (holes.ContainsKey(pl.GeometryId))
                                {
                                    //Instance is valid
                                    holes.Remove(pl.GeometryId);
                                    needsRemove = false;
                                }
                                else if (holes.TryGetValue(pl.RelatedIds[0], out var newEdgeLoop))
                                {
                                    //Instance existed for the Face, but is now a EdgeLoop
                                    //Change the instance target, but keep the instance
                                    SetComponentName(subComp.Component, newEdgeLoop);

                                    inst.Placements.Remove(pl);
                                    inst.Placements.Add(new SimInstancePlacementGeometry(
                                        TypedGeometry.ModelGeometry.Model.File.Key,
                                        newEdgeLoop.Id, GetRelatedIds(newEdgeLoop)));

                                    holes.Remove(newEdgeLoop.Id);
                                    needsRemove = false;
                                }
                                else if (holeLoopsForFaceHoles.TryGetValue(pl.GeometryId, out var holeFaceId))
                                {
                                    //Instance existed for the EdgeLoop, but is now a Face
                                    //Change the instance target, but keep the instance
                                    var holeFace = holes[holeFaceId];

                                    SetComponentName(subComp.Component, holeFace);

                                    inst.Placements.Remove(pl);
                                    inst.Placements.Add(new SimInstancePlacementGeometry(
                                        TypedGeometry.ModelGeometry.Model.File.Key,
                                        holeFace.Id, GetRelatedIds(holeFace)));

                                    holes.Remove(holeFaceId);
                                    needsRemove = false;
                                }
                            }
                        }

                        if (needsRemove)
                        {
                            component.Components.RemoveAt(i);
                            i--;
                        }
                    }
                }

                foreach (var hole in holes)
                {
                    SimComponent faceComp = new SimComponent();
                    faceComp.InstanceType = SimInstanceType.GeometricSurface;
                    SetComponentName(faceComp, hole.Value);

                    faceComp.Description = "Representation";
                    faceComp.CurrentSlot = new SimSlotBase(ComponentUtils.COMP_SLOT_AREAS);
                    faceComp.IsAutomaticallyGenerated = true;
                    faceComp.AccessLocal = new SimAccessProfile(component.AccessLocal);

                    SimComponentInstance faceInstance = new SimComponentInstance(SimInstanceType.GeometricSurface,
                        hole.Value.ModelGeometry.Model.File.Key, hole.Value.Id, GetRelatedIds(hole.Value));
                    faceComp.Instances.Add(faceInstance);

                    var slot = component.Components.FindAvailableSlot(faceComp.CurrentSlot);
                    component.Components.Add(new SimChildComponentEntry(slot, faceComp));
                }
            }
        }

        private void SetComponentName(SimComponent component, BaseGeometry geometry)
        {
            component.Name = geometry.Name;

            if (geometry is Face)
                component.Name += " 2D";
            else
                component.Name += " Void";
        }

        private ulong[] GetRelatedIds(BaseGeometry geometry)
        {
            ulong[] relatedIds = null;
            if (geometry is Face hf)
                relatedIds = new ulong[] { hf.Boundary.Id };
            else if (geometry is EdgeLoop hel)
                relatedIds = hel.Faces.Select(x => x.Id).ToArray();
            return relatedIds;
        }

        private void FindParentVolume()
        {
            volume = null;

            //Try to find parent volume
            var parentComponent = Placement.Instance.Component.Parent;
            if (parentComponent != null && parentComponent.InstanceType == SimInstanceType.Entity3D && parentComponent.Instances.Count > 0)
            {
                var parentInstance = parentComponent.Instances[0];
                var parentGeometryPlacement = (SimInstancePlacementGeometry)parentInstance.Placements.FirstOrDefault(
                    x => x is SimInstancePlacementGeometry);
                if (parentGeometryPlacement != null && parentGeometryPlacement.FileId == Placement.FileId)
                {
                    volume = TypedGeometry.ModelGeometry.GeometryFromId(parentGeometryPlacement.GeometryId) as Volume;
                }
            }
        }

        private void UpdateParameters(bool geometryExists)
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Factory))
            {
                double area = 0.0, areaMin = 0.0, areaMax = 0.0;
                Size size = new Size(0, 0), sizeMin = new Size(0, 0), sizeMax = new Size(0, 0);
                double kfAxis = 0.0, kdAxis = 0.0;
                List<Point3D> boundary = new List<Point3D>();

                if (geometryExists)
                {
                    (area, areaMin, areaMax) = FaceAlgorithms.AreaMinMax(TypedGeometry);
                    (size, sizeMin, sizeMax) = FaceAlgorithms.Size(TypedGeometry);
                    (kfAxis, kdAxis) = FaceAlgorithms.HeightMinMax(TypedGeometry);

                    bool reverse = false;
                    if (volume != null)
                    {
                        var pface = TypedGeometry.PFaces.FirstOrDefault(x => x.Volume == volume);
                        if (pface != null)
                        {
                            if ((int)pface.Orientation * (int)TypedGeometry.Orientation == 1)
                                reverse = true;
                        }
                    }

                    if (reverse)
                    {
                        for (int i = TypedGeometry.Boundary.Edges.Count - 1; i >= 0; i--)
                            boundary.Add(TypedGeometry.Boundary.Edges[i].StartVertex.Position);
                    }
                    else
                    {
                        for (int i = 0; i < TypedGeometry.Boundary.Edges.Count; i++)
                            boundary.Add(TypedGeometry.Boundary.Edges[i].StartVertex.Position);
                    }
                }

                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_AREA, area);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_AREA_MIN, areaMin);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_AREA_MAX, areaMax);

                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_WIDTH, size.Width);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_WIDTH_MIN, sizeMin.Width);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_WIDTH_MAX, sizeMax.Width);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_HEIGHT, size.Height);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_HEIGHT_MIN, sizeMin.Height);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_HEIGHT_MAX, sizeMax.Height);

                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_K_F_AXES, kfAxis);
                ExchangeHelpers.SetParameterIfExists(Placement, ReservedParameters.RP_K_D_AXES, kdAxis);

                Placement.Instance.InstancePath = boundary;
            }
        }

        /// <summary>
        /// Notifies the connector that an instance placement of type <see cref="SimInstanceType.AttributesFace"/> has been added
        /// for the face represented by this connector.
        /// </summary>
        /// <param name="addedPlacement">The placement that has been added</param>
        internal void OnAttributesFacePlacementAdded(SimInstancePlacementGeometry addedPlacement)
        {
            var nextSlot = Placement.Instance.Component.FindAvailableReferenceSlot(
                new List<SimSlotBase> { addedPlacement.Instance.Component.CurrentSlot });

            Placement.Instance.Component.ReferencedComponents.Add(new SimComponentReference(nextSlot, addedPlacement.Instance.Component));
        }
        /// <summary>
        /// Notifies the connector that an instance placement of type <see cref="SimInstanceType.AttributesFace"/> has been removed
        /// from the face represented by this connector.
        /// </summary>
        /// <param name="removedPlacement">The placement that has been removed</param>
        internal void OnAttributesFacePlacementRemoved(SimInstancePlacementGeometry removedPlacement)
        {
            var index = Placement.Instance.Component.ReferencedComponents.RemoveFirst(
                x => x.Target == removedPlacement.Instance.Component
                );
        }

        /// <summary>
        /// Notifies the connector that a new neighboring volume has been added
        /// </summary>
        /// <param name="neighbor">The neightboring connector</param>
        internal void OnNeighborAdded(VolumeConnector neighbor)
        {
            var neighborComponent = neighbor.Placement.Instance.Component;
            //Create reference (unless it already exists)
            if (!Placement.Instance.Component.ReferencedComponents.Any(x => x.Target == neighborComponent))
            {
                var nextSlot = Placement.Instance.Component.FindAvailableReferenceSlot(new List<SimSlotBase> { neighborComponent.CurrentSlot });

                Placement.Instance.Component.ReferencedComponents.Add(new SimComponentReference(
                    nextSlot, neighborComponent));
            }
        }
        /// <summary>
        /// Notifies the connector that a neighboring volume has been removed
        /// </summary>
        /// <param name="neighbor">The neightboring connector</param>
        internal void OnNeighborRemoved(VolumeConnector neighbor)
        {
            var neighborComponent = neighbor.Placement.Instance.Component;

            for (int i = 0; i < Placement.Instance.Component.ReferencedComponents.Count; ++i)
            {
                var reference = Placement.Instance.Component.ReferencedComponents[i];
                if (reference.Target == neighborComponent)
                {
                    Placement.Instance.Component.ReferencedComponents.RemoveAt(i);
                    i--;
                }
            }
        }

        #region BaseGeometryConnector

        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
            UpdateParameters(true);
        }
        /// <inheritdoc />
        internal override void OnPlacementRemoved()
        {
            //No update needed, the component will be removed anyway
        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
            UpdateSubComponents();
            UpdateParameters(true);
        }
        /// <inheritdoc />
        internal override void OnGeometryRemoved()
        {
            base.OnGeometryRemoved();
            UpdateParameters(false);
        }
        /// <inheritdoc />
        protected override void OnTargetGeometryChanged(BaseGeometry oldGeometry, BaseGeometry newGeometry)
        {
            FindParentVolume();
            UpdateParameters(true);

            using (AccessCheckingDisabler.Disable(this.Placement.Instance.Component.Factory))
            {
                this.Placement.Instance.Component.Name = newGeometry.Name;
            }
        }
        /// <inheritdoc />
        internal override void OnConnectorsInitialized()
        {
            UpdateParameters(true);
        }
        /// <inheritdoc />
        internal override void OnGeometryNameChanged(BaseGeometry geometry)
        {
            base.OnGeometryNameChanged(geometry);
            using (AccessCheckingDisabler.Disable(this.Placement.Instance.Component.Factory))
            {
                this.Placement.Instance.Component.Name = geometry.Name;
            }
        }

        #endregion
    }
}
