using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    /// <summary>
    /// Manages the connection between a <see cref="SimComponentInstance"/> of type <see cref="SimInstanceType.Entity3D"/>
    /// and a <see cref="Volume"/>
    /// Responsible for managing <see cref="VolumeFaceConnector"/> and <see cref="VolumeVolumeConnector"/>.
    /// </summary>
    internal class VolumeConnector : BaseGeometryConnector<Volume>
    {
        private GeometricAsset asset;
        private GeometryModelConnector geometryModelConnector;

        private List<VolumeConnector> neighbors = new List<VolumeConnector>();


        /// <summary>
        /// Initializes a new instance of the <see cref="FaceConnector"/> class
        /// </summary>
        /// <param name="volume">The volume</param>
        /// <param name="placement">The instance placement</param>
        /// <param name="geometryModelConnector">The connector of the <see cref="GeometryModel"/> the volume belongs to</param>
        internal VolumeConnector(Volume volume, SimInstancePlacementGeometry placement, GeometryModelConnector geometryModelConnector)
            : base(volume, placement)
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Factory))
            {
                var component = placement.Instance.Component;

                this.geometryModelConnector = geometryModelConnector;

                //Check if asset exists and add if not
                asset = ExchangeHelpers.CreateAssetIfNotExists(component, volume);

                //Check if subcomponent for geometric volume exists and if there is exactly one.
                //All others are deleted
                CreateVolumeComponent();
                CreateFaceComponents();

                //References to adjacent components
                UpdateNeighborReferences();
            }
        }


        #region BaseGeometryConnector

        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
            //Do nothing, the element doesn't have any parameters
        }
        /// <inheritdoc />
        internal override void OnGeometryRemoved()
        {
            base.OnGeometryRemoved();

            RemoveSubComponentsAndAsset();
            RemoveAllNeighbors();
        }
        /// <inheritdoc />
        internal override void OnPlacementRemoved()
        {
            //Remove sub components & asset
            RemoveSubComponentsAndAsset();

            RemoveAllNeighbors();
        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
            //Update subcomponents
            CreateFaceComponents();

            //Update neighbor references
            UpdateNeighborReferences();
        }
        /// <inheritdoc />
        protected override void OnTargetGeometryChanged(BaseGeometry oldGeometry, BaseGeometry newGeometry)
        { }
        /// <inheritdoc />
        internal override void OnConnectorsInitialized()
        {
            //Nothing to do, there is only one anyway
        }

        #endregion

        private void RemoveSubComponentsAndAsset()
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Factory))
            {
                if (asset != null)
                {
                    this.Placement.Instance.Component.RemoveAsset(asset);
                    this.Placement.Instance.Factory.ProjectData.AssetManager.RemoveAsset(asset);
                    asset = null;
                }

                var component = Placement.Instance.Component;
                for (int i = 0; i < component.Components.Count; ++i)
                {
                    var childComponent = component.Components[i];
                    //Assumes that there is never more than one volume attached
                    if (childComponent.Component.InstanceType == SimInstanceType.GeometricVolume ||
                        childComponent.Component.InstanceType == SimInstanceType.GeometricSurface)
                    {
                        component.Components.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    
        private void CreateVolumeComponent()
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Factory))
            {
                var component = Placement.Instance.Component;

                bool hasVolumeComponent = false;
                for (int i = 0; i < component.Components.Count; ++i)
                {
                    var childComponent = component.Components[i];

                    if (childComponent.Component.InstanceType == SimInstanceType.GeometricVolume)
                    {
                        bool needsRemove = true;

                        //Assumes that GeometricVolumes always have exactly one (or no) instance
                        var instance = childComponent.Component.Instances.FirstOrDefault();
                        if (instance != null)
                        {
                            //Assumes that instance always has one geometric placement
                            var geomPlacement = (SimInstancePlacementGeometry)instance.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                            if (geomPlacement != null && geomPlacement.FileId == TypedGeometry.ModelGeometry.Model.File.Key &&
                                geomPlacement.GeometryId == TypedGeometry.Id)
                            {
                                needsRemove = false;
                                hasVolumeComponent = true;
                            }
                        }

                        if (needsRemove)
                        {
                            component.Components.RemoveAt(i);
                            i--;
                        }
                    }
                }

                if (!hasVolumeComponent)
                {
                    SimComponent volumeComponent = new SimComponent();
                    volumeComponent.InstanceType = SimInstanceType.GeometricVolume;
                    volumeComponent.Name = TypedGeometry.Name + " 3D";
                    volumeComponent.Description = "Representation";
                    volumeComponent.CurrentSlot = new SimSlotBase(ComponentUtils.COMP_SLOT_VOLUMES);
                    volumeComponent.IsAutomaticallyGenerated = true;
                    volumeComponent.AccessLocal = new SimAccessProfile(component.AccessLocal);

                    SimComponentInstance volumeInstance = new SimComponentInstance(SimInstanceType.GeometricVolume,
                        TypedGeometry.ModelGeometry.Model.File.Key, TypedGeometry.Id, new ulong[] { });
                    volumeComponent.Instances.Add(volumeInstance);

                    var slot = component.Components.FindAvailableSlot(volumeComponent.CurrentSlot);
                    component.Components.Add(new SimChildComponentEntry(slot, volumeComponent));
                }
            }
        }
    
        private void CreateFaceComponents()
        {
            using (AccessCheckingDisabler.Disable(Placement.Instance.Factory))
            {
                var component = Placement.Instance.Component;

                Dictionary<ulong, Face> volumeFaces = TypedGeometry.Faces.ToDictionary(x => x.Face.Id, x => x.Face);

                //Delete subcomponents that do not match any face
                for (int i = 0; i < component.Components.Count; i++)
                {
                    var subComp = component.Components[i];

                    if (subComp.Component.InstanceType == SimInstanceType.GeometricSurface)
                    {
                        bool needsRemove = true;

                        var inst = subComp.Component.Instances.FirstOrDefault();
                        if (inst != null)
                        {
                            var pl = (SimInstancePlacementGeometry)inst.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                            if (pl.FileId == TypedGeometry.ModelGeometry.Model.File.Key && volumeFaces.ContainsKey(pl.GeometryId))
                            {
                                volumeFaces.Remove(pl.GeometryId);
                                needsRemove = false;
                            }
                        }

                        if (needsRemove)
                        {
                            component.Components.RemoveAt(i);
                            i--;
                        }
                    }
                }
            
                //Create subcomponents for all missing faces
                foreach (var face in volumeFaces)
                {
                    SimComponent faceComp = new SimComponent();
                    faceComp.InstanceType = SimInstanceType.GeometricSurface;
                    faceComp.Name = face.Value.Name + " 2D";
                    faceComp.Description = "Representation";
                    faceComp.CurrentSlot = new SimSlotBase(ComponentUtils.COMP_SLOT_AREAS);
                    faceComp.IsAutomaticallyGenerated = true;
                    faceComp.AccessLocal = new SimAccessProfile(component.AccessLocal);

                    SimComponentInstance faceInstance = new SimComponentInstance(SimInstanceType.GeometricSurface,
                        face.Value.ModelGeometry.Model.File.Key, face.Value.Id, new ulong[] { face.Value.Boundary.Id });
                    faceComp.Instances.Add(faceInstance);

                    var slot = component.Components.FindAvailableSlot(faceComp.CurrentSlot);
                    component.Components.Add(new SimChildComponentEntry(slot, faceComp));
                }
            }
        }
    
    
        private void UpdateNeighborReferences()
        {
            var oldNeighbors = neighbors.ToHashSet();
            neighbors.Clear();

            //Create references to neighbors
            foreach (var pface in TypedGeometry.Faces)
            {
                var otherVolume = pface.Face.PFaces.FirstOrDefault(x => x != pface)?.Volume;
                if (otherVolume != null)
                {
                    foreach (var otherVolumeConnector in geometryModelConnector.GetConnectors(otherVolume)
                        .Where(x => x.Placement.Instance.InstanceType == SimInstanceType.Entity3D).OfType<VolumeConnector>())
                    {
                        this.OnNeighborAdded(otherVolumeConnector);

                        if (otherVolumeConnector is VolumeConnector vc)
                            vc.OnNeighborAdded(this);
                    }
                }
            }

            //Remove neighbors that no longer exist
            foreach (var neighbor in oldNeighbors)
            {
                this.OnNeighborRemoved(neighbor);
            }
        }
    
        private void RemoveAllNeighbors()
        {
            foreach (var neighbor in neighbors)
            {
                this.OnNeighborRemoved(neighbor);
                neighbor.OnNeighborRemoved(this);
            }
        }

        private void OnNeighborAdded(VolumeConnector neighbor)
        {
            var otherComponent = neighbor.Placement.Instance.Component;

            neighbors.Add(neighbor);

            if (!Placement.Instance.Component.ReferencedComponents
                .Any(x => x.Target == otherComponent))
            {
                var nextSlot = Placement.Instance.Component.FindAvailableReferenceSlot(
                    new List<SimSlotBase> { otherComponent.CurrentSlot });

                Placement.Instance.Component.ReferencedComponents.Add(new SimComponentReference(
                    nextSlot, otherComponent)
                    );
            }

            //Inform face connectors
            //Find common faces
            foreach (var pface in TypedGeometry.Faces)
            {
                var otherVolume = pface.Face.PFaces.FirstOrDefault(x => x.Volume != TypedGeometry)?.Volume;
                if (otherVolume == neighbor.TypedGeometry)
                {
                    var connectors = geometryModelConnector.GetConnectors(pface.Face).OfType<VolumeFaceConnector>();
                    foreach (var connector in connectors)
                    {
                        if (connector.Placement.Instance.Component.Parent == this.Placement.Instance.Component)
                            connector.OnNeighborAdded(neighbor);
                    }
                }
            }
        }
        private void OnNeighborRemoved(VolumeConnector neighbor)
        {
            if (Placement.Instance != null && Placement.Instance.Component != null)
            {
                var reference = Placement.Instance.Component.ReferencedComponents
                        .FirstOrDefault(x => x.Target == neighbor.Placement.Instance.Component);

                if (reference != null)
                    Placement.Instance.Component.ReferencedComponents.Remove(reference);
            }

            //Inform face connectors
            //Find common faces
            foreach (var pface in TypedGeometry.Faces)
            {
                var otherVolume = pface.Face.PFaces.FirstOrDefault(x => x.Volume != TypedGeometry)?.Volume;
                if (otherVolume == neighbor.TypedGeometry)
                {
                    var connectors = geometryModelConnector.GetConnectors(pface.Face).OfType<VolumeFaceConnector>();
                    foreach (var connector in connectors)
                    {
                        if (connector.Placement.Instance.Component.Parent == this.Placement.Instance.Component)
                            connector.OnNeighborRemoved(neighbor);
                    }
                }
            }
        }
    }
}
