using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Interface used to identify possible child rules for the <see cref="SimDataMappingRuleVolume"/> class
    /// </summary>
    public interface ISimDataMappingVolumeRuleChild : ISimDataMappingRuleBase 
    {
        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A deep copy of the rule</returns>
        ISimDataMappingVolumeRuleChild Clone();
    }

    /// <summary>
    /// The properties of a <see cref="Volume"/> that can be mapped
    /// </summary>
    public enum SimDataMappingVolumeMappingProperties
    {
        /// <summary>
        /// The name of the volume. (string)
        /// </summary>
        Name = 0,
        /// <summary>
        /// The local Id of the volume. (int)
        /// </summary>
        Id = 1,
        /// <summary>
        /// The volume of the volume. (double)
        /// </summary>
        Volume = 2,
        /// <summary>
        /// The floor area of the volume. (double)
        /// </summary>
        FloorArea = 3,
        /// <summary>
        /// The height of the volume. (double)
        /// </summary>
        Height = 4,
        /// <summary>
        /// The elevation of the ceiling of the volume. (double)
        /// </summary>
        CeilingElevation = 5,
        /// <summary>
        /// The elevation of the floor of the volume. (double)
        /// </summary>
        FloorElevation = 6
    }

    /// <summary>
    /// Mapping rule for <see cref="Volume"/>
    /// </summary>
    public class SimDataMappingRuleVolume 
        : SimDataMappingRuleBase<SimDataMappingVolumeMappingProperties, SimDataMappingFilterVolume>,
        ISimDataMappingComponentRuleChild, ISimDataMappingInstanceRuleChild, ISimDataMappingFaceRuleChild
    {
        /// <summary>
        /// The child rules
        /// </summary>
        public ObservableCollection<ISimDataMappingVolumeRuleChild> Rules { get; } = new ObservableCollection<ISimDataMappingVolumeRuleChild>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingRuleVolume"/> class
        /// </summary>
        /// <param name="sheetName">The name of the worksheet</param>
        public SimDataMappingRuleVolume(string sheetName) : base(sheetName) { }

        /// <inheritdoc />
        public override void Execute(object rootObject, SimTraversalState state, SimMappedData data)
        {
            if (rootObject is SimComponent rootComponent) //Child of component rule
            {
                var projectData = rootComponent.Factory.ProjectData;

                foreach (var inst in rootComponent.Instances)
                {
                    if (!state.VisitedObjects.Contains(inst))
                    {
                        state.VisitedObjects.Add(inst);

                        ExecuteInstance(inst, projectData, state, data);

                        state.VisitedObjects.Remove(inst);
                    }
                }
            }
            else if (rootObject is SimComponentInstance instance) //Child of instance rule
            {
                var projectData = instance.Factory.ProjectData;
                ExecuteInstance(instance, projectData, state, data);
            }
            else if (rootObject is Face face)
            {
                foreach (var pface in face.PFaces)
                {
                    if (state.MatchCount >= this.MaxMatches)
                        break;

                    if (!state.VisitedObjects.Contains(pface.Volume))
                    {
                        state.VisitedObjects.Add(pface.Volume);

                        HandleMatch(pface.Volume, state, data);

                        state.VisitedObjects.Remove(pface.Volume);
                    }
                }
            }
        }

        private void ExecuteInstance(SimComponentInstance instance, ProjectData projectData, SimTraversalState state, SimMappedData data)
        {
            foreach (var geoPlacement in instance.Placements.OfType<SimInstancePlacementGeometry>())
            {
                if (state.MatchCount >= this.MaxMatches)
                    break;

                var resourceFile = projectData.AssetManager.GetResource(geoPlacement.FileId) as ResourceFileEntry;

                //Check if there are any filter for resource file id
                bool matchesFile = true;
                foreach (var fileFilter in Filter.Where(x => x.Property == SimDataMappingVolumeFilterProperties.FileKey))
                {
                    if (fileFilter.Value is int ikey)
                        matchesFile &= ikey == geoPlacement.FileId;
                }
                foreach (var fileFilter in Filter.Where(x => x.Property == SimDataMappingVolumeFilterProperties.FileTags))
                {
                    if (fileFilter.Value is SimTaxonomyEntryReference tref)
                    {
                        matchesFile &= resourceFile.Tags.Any(t => t.Target == tref.Target);
                    }
                }

                if (matchesFile)
                {
                    //Make sure that the GeometryModel is loaded
                    if (resourceFile != null)
                    {
                        if (!projectData.GeometryModels.TryGetGeometryModel(resourceFile, out var gm, false))
                        {
                            List<SimGeoIOError> errors = new List<SimGeoIOError>();
                            gm = SimGeoIO.Load(resourceFile, projectData, errors, OffsetAlgorithm.Disabled);
                            projectData.GeometryModels.AddGeometryModel(gm);
                            state.ModelsToRelease.Add(gm);
                        }

                        //Find geometry
                        var volume = gm.Geometry.GeometryFromId(geoPlacement.GeometryId) as Volume;
                        if (volume != null && !state.VisitedObjects.Contains(volume))
                        {
                            state.VisitedObjects.Add(volume);

                            if (Filter.All(f => f.Match(volume)))
                                HandleMatch(volume, state, data);

                            state.VisitedObjects.Remove(volume);
                        }
                    }
                }
            }
        }

        private void HandleMatch(Volume volume, SimTraversalState state, SimMappedData data)
        {
            //Advance position for this rule
            AdvanceReferencePoint(state);

            WriteProperties(state, property =>
            {
                //Store property
                switch (property)
                {
                    case SimDataMappingVolumeMappingProperties.Name:
                        data.AddData(this.SheetName, state.CurrentPosition, volume.Name, this);
                        break;
                    case SimDataMappingVolumeMappingProperties.Id:
                        data.AddData(this.SheetName, state.CurrentPosition, (int)volume.Id, this);
                        break;
                    case SimDataMappingVolumeMappingProperties.Volume:
                        data.AddData(this.SheetName, state.CurrentPosition,
                            VolumeAlgorithms.Volume(volume), this);
                        break;
                    case SimDataMappingVolumeMappingProperties.FloorArea:
                        data.AddData(this.SheetName, state.CurrentPosition,
                            VolumeAlgorithms.AreaBruttoNetto(volume).areaReference, this);
                        break;
                    case SimDataMappingVolumeMappingProperties.Height:
                        data.AddData(this.SheetName, state.CurrentPosition,
                            VolumeAlgorithms.Height(volume).reference, this);
                        break;
                    case SimDataMappingVolumeMappingProperties.CeilingElevation:
                        data.AddData(this.SheetName, state.CurrentPosition,
                            VolumeAlgorithms.ElevationReference(volume).ceiling, this);
                        break;
                    case SimDataMappingVolumeMappingProperties.FloorElevation:
                        data.AddData(this.SheetName, state.CurrentPosition,
                            VolumeAlgorithms.ElevationReference(volume).floor, this);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported property");
                }
            });

            //Handle child rules
            ExecuteChildRules(this.Rules, volume, state, data);
        }
        /// <inheritdoc />
        protected override void OnToolChanged()
        {
            foreach (var r in this.Rules)
                r.Tool = Tool;
        }

        #region Clone

        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A deep copy of the rule</returns>
        public SimDataMappingRuleVolume Clone()
        {
            var copy = new SimDataMappingRuleVolume(this.SheetName)
            {
                Name = this.Name,
                MaxMatches = this.MaxMatches,
                MaxDepth = this.MaxDepth,
                OffsetParent = this.OffsetParent,
                OffsetConsecutive = this.OffsetConsecutive,
                MappingDirection = this.MappingDirection,
                ReferencePointParent = this.ReferencePointParent,
                ReferencePointConsecutive = this.ReferencePointConsecutive,
            };

            copy.Properties.AddRange(this.Properties);
            copy.Filter.AddRange(this.Filter.Select(x => x.Clone()));

            copy.Rules.AddRange(this.Rules.Select(x => x.Clone()));

            return copy;
        }

        /// <inheritdoc />
        ISimDataMappingComponentRuleChild ISimDataMappingComponentRuleChild.Clone()
        {
            return this.Clone();
        }
        /// <inheritdoc />
        ISimDataMappingInstanceRuleChild ISimDataMappingInstanceRuleChild.Clone()
        {
            return this.Clone();
        }
        /// <inheritdoc />
        ISimDataMappingFaceRuleChild ISimDataMappingFaceRuleChild.Clone()
        {
            return this.Clone();
        }

        #endregion

        /// <inheritdoc />
        public override void RestoreDefaultTaxonomyReferences()
        {
            base.RestoreDefaultTaxonomyReferences();

            foreach (var childRule in Rules)
                childRule.RestoreDefaultTaxonomyReferences();
        }
    }
}
