using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Interface used to identify possible child rules for the <see cref="SimDataMappingRuleFace"/> class
    /// </summary>
    public interface ISimDataMappingFaceRuleChild : ISimDataMappingRuleBase
    {
        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A deep copy of the rule</returns>
        ISimDataMappingFaceRuleChild Clone();
    }

    /// <summary>
    /// The properties of a <see cref="Face"/> that can be mapped
    /// </summary>
    public enum SimDataMappingFaceMappingProperties
    {
        /// <summary>
        /// The name of the face. (string)
        /// </summary>
        Name, 
        /// <summary>
        /// The local id of the face. (int)
        /// </summary>
        Id,
        /// <summary>
        /// The area of the face. (double)
        /// </summary>
        Area,
        /// <summary>
        /// The incline of the face, see <see cref="FaceAlgorithms.OrientationIncline(PFace, double)"/>. (double)
        /// </summary>
        Incline,
        /// <summary>
        /// The orientation of the face, see <see cref="FaceAlgorithms.OrientationIncline(PFace, double)"/>. (double)
        /// </summary>
        Orientation
    }

    /// <summary>
    /// Mapping rule for <see cref="Face"/>
    /// </summary>
    public class SimDataMappingRuleFace
        : SimDataMappingRuleBase<SimDataMappingFaceMappingProperties, SimDataMappingFilterFace>,
        ISimDataMappingComponentRuleChild, ISimDataMappingInstanceRuleChild, ISimDataMappingVolumeRuleChild, ISimDataMappingFaceRuleChild
    {
        /// <summary>
        /// The child rules
        /// </summary>
        public ObservableCollection<ISimDataMappingFaceRuleChild> Rules { get; } = new ObservableCollection<ISimDataMappingFaceRuleChild>();


        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingRuleFace"/> class
        /// </summary>
        /// <param name="sheetName">The name of the worksheet</param>
        public SimDataMappingRuleFace(string sheetName) : base(sheetName) { }

        /// <inheritdoc />
        public override void Execute(object rootObject, SimTraversalState state, SimMappedData data)
        {
            if (rootObject is SimComponent rootComponent)
            {
                var projectData = rootComponent.Factory.ProjectData;

                foreach (var inst in rootComponent.Instances)
                {
                    if (state.MatchCount >= this.MaxMatches)
                        break;

                    if (!state.VisitedObjects.Contains(inst))
                    {
                        state.VisitedObjects.Add(inst);

                        ExecuteInstance(inst, projectData, state, data);

                        state.VisitedObjects.Remove(inst);
                    }
                }
            }
            else if (rootObject is SimComponentInstance instance)
            {
                var projectData = instance.Factory.ProjectData;
                ExecuteInstance(instance, projectData, state, data);
            }
            else if (rootObject is Volume volume)
            {
                foreach (var pface in volume.Faces)
                {
                    if (state.MatchCount >= this.MaxMatches)
                        break;

                    if (!state.VisitedObjects.Contains(pface.Face))
                    {
                        state.VisitedObjects.Add(pface.Face);

                        if (Filter.All(f => f.Match(pface.Face)))
                            HandleMatch(pface, state, data);

                        state.VisitedObjects.Remove(pface.Face);
                    }
                }
            }
            else if (rootObject is Face face)
            {
                foreach (var hole in face.Holes)
                {
                    var holeFace = hole.Faces.FirstOrDefault(x => x.Boundary == hole);
                    if (holeFace != null && !state.VisitedObjects.Contains(holeFace) &&
                        Filter.All(f => f.Match(holeFace)))
                    {
                        state.VisitedObjects.Add(holeFace);

                        HandleMatch(holeFace, state, data);

                        state.VisitedObjects.Remove(holeFace);
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

                //Check if there are any filter for resource file id
                bool matchesFile = true;
                foreach (var fileFilter in Filter.Where(x => x.Property == SimDataMappingFaceFilterProperties.FileKey))
                {
                    if (fileFilter.Value is int ikey)
                        matchesFile &= ikey == geoPlacement.FileId;
                }

                if (matchesFile)
                {
                    //Make sure that the GeometryModel is loaded
                    var resourceFile = projectData.AssetManager.GetResource(geoPlacement.FileId) as ResourceFileEntry;
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
                        var face = gm.Geometry.GeometryFromId(geoPlacement.GeometryId) as Face;
                        if (face != null && !state.VisitedObjects.Contains(face))
                        {
                            state.VisitedObjects.Add(face);

                            if (Filter.All(f => f.Match(face)))
                                HandleMatch(face, state, data);

                            state.VisitedObjects.Remove(face);
                        }
                    }
                }
            }
        }

        private void HandleMatch(Face face, SimTraversalState state, SimMappedData data)
        {
            WriteProperties(state, property => WriteProperty(property, face, state, data));

            ExecuteChildRules(this.Rules, face, state, data);

            //Advance position for next rule
            AdvanceReferencePoint(state);
        }

        private void HandleMatch(PFace face, SimTraversalState state, SimMappedData data)
        {
            WriteProperties(state, property => WriteProperty(property, face, state, data));

            ExecuteChildRules(this.Rules, face.Face, state, data);

            //Advance position for next rule
            AdvanceReferencePoint(state);
        }

        private void WriteProperty(SimDataMappingFaceMappingProperties property, Face face, SimTraversalState state, SimMappedData data)
        {
            //Store property
            switch (property)
            {
                case SimDataMappingFaceMappingProperties.Name:
                    data.AddData(this.SheetName, state.CurrentPosition, face.Name);
                    break;
                case SimDataMappingFaceMappingProperties.Id:
                    data.AddData(this.SheetName, state.CurrentPosition, (int)face.Id);
                    break;
                case SimDataMappingFaceMappingProperties.Area:
                    data.AddData(this.SheetName, state.CurrentPosition, FaceAlgorithms.Area(face));
                    break;
                case SimDataMappingFaceMappingProperties.Incline:
                    data.AddData(this.SheetName, state.CurrentPosition, FaceAlgorithms.OrientationIncline(face.Normal).incline * 180.0 / Math.PI);
                    break;
                case SimDataMappingFaceMappingProperties.Orientation:
                    data.AddData(this.SheetName, state.CurrentPosition, FaceAlgorithms.OrientationIncline(face.Normal).orientation * 180.0 / Math.PI);
                    break;
                default:
                    throw new NotSupportedException("Unsupported property");
            }
        }
    
        private void WriteProperty(SimDataMappingFaceMappingProperties property, PFace face, SimTraversalState state, SimMappedData data)
        {
            //Store property
            switch (property)
            {
                case SimDataMappingFaceMappingProperties.Incline:
                    data.AddData(this.SheetName, state.CurrentPosition, FaceAlgorithms.OrientationIncline(face).incline * 180.0 / Math.PI);
                    break;
                case SimDataMappingFaceMappingProperties.Orientation:
                    data.AddData(this.SheetName, state.CurrentPosition, FaceAlgorithms.OrientationIncline(face).orientation * 180.0 / Math.PI);
                    break;
                default:
                    WriteProperty(property, face.Face, state, data);
                    break;
            }
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
        public SimDataMappingRuleFace Clone()
        {
            var copy = new SimDataMappingRuleFace(this.SheetName)
            {
                Name = this.Name,
                MaxMatches = this.MaxMatches,
                MaxDepth = this.MaxDepth,
                OffsetParent = this.OffsetParent,
                OffsetConsecutive = this.OffsetConsecutive,
                MappingDirection = this.MappingDirection,
                ReferencePoint = this.ReferencePoint,
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
        ISimDataMappingFaceRuleChild ISimDataMappingFaceRuleChild.Clone()
        {
            return this.Clone();
        }
        /// <inheritdoc />
        ISimDataMappingInstanceRuleChild ISimDataMappingInstanceRuleChild.Clone()
        {
            return this.Clone();
        }
        /// <inheritdoc />
        ISimDataMappingVolumeRuleChild ISimDataMappingVolumeRuleChild.Clone()
        {
            return this.Clone();
        }

        #endregion
    }
}
