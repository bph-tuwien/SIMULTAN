using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Exchange.GeometryConnectors
{
    internal class ParameterSourceConnector
    {
        internal BaseGeometry Geometry { get; private set; }

        internal SimGeometryParameterSource ParameterSource { get; }

        internal SimInstancePlacementGeometry Placement { get; }

        internal ParameterSourceConnector(BaseGeometry geometry, SimGeometryParameterSource parameterSource, SimInstancePlacementGeometry placement)
        {
            this.Geometry = geometry;
            this.ParameterSource = parameterSource;
            this.Placement = placement;
        }


        private static Dictionary<SimGeometrySourceProperty, Func<IEnumerable<double>, double>> aggregationMethod =
            new Dictionary<SimGeometrySourceProperty, Func<IEnumerable<double>, double>>
            {
                { SimGeometrySourceProperty.FaceArea, c => c.Sum() },
                { SimGeometrySourceProperty.FaceIncline, c => c.Average() },
                { SimGeometrySourceProperty.FaceOrientation, c => c.Average() },

                { SimGeometrySourceProperty.VolumeFloorElevation, c => c.Average() },
                { SimGeometrySourceProperty.VolumeCeilingElevation, c => c.Average() },
                { SimGeometrySourceProperty.VolumeHeight, c => c.Average() },
                { SimGeometrySourceProperty.VolumeFloorArea, c => c.Sum() },
                { SimGeometrySourceProperty.VolumeVolume, c => c.Sum() },

                { SimGeometrySourceProperty.EdgeLength, c => c.Sum() },
            };
        private static double Aggregate(SimGeometrySourceProperty property, IEnumerable<double> values)
        {
            if (!values.Any())
                return 0.0;

            var aggregator = aggregationMethod[property];
            return aggregator(values);
        }


        internal void UpdateComponent(bool placementDeleted, bool geometryRemoved)
        {
            if (placementDeleted)
            {
                Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter] = double.NaN;
                if (ParameterSource.TargetParameter is SimDoubleParameter dParam)
                {
                    dParam.Value = Aggregate(
                     ParameterSource.GeometryProperty,
                     ParameterSource.TargetParameter.Component.Instances
                         .Where(x => x != Placement.Instance && x.Placements.Any(p => p is SimInstancePlacementGeometry))
                         .Select(x => x.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]).OfType<double>().ToList());
                }

            }
            else if (geometryRemoved)
            {
                Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter] = double.NaN;
                if (ParameterSource.TargetParameter is SimDoubleParameter dParam)
                {
                    dParam.Value = Aggregate(
                      ParameterSource.GeometryProperty,
                      ParameterSource.TargetParameter.Component.Instances
                          .Where(x => x.Placements.Any(p => p is SimInstancePlacementGeometry))
                          .Select(x => x.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]).OfType<double>().ToList());
                }

            }
            else
            {
                var resourceEntry = ParameterSource.TargetParameter.Factory.ProjectData.AssetManager.GetResource(Placement.FileId);
                var resourceTags = resourceEntry.Tags.ToHashSet();
                var filterTags = ParameterSource.FilterTags;
                if (filterTags.All(x => resourceTags.Contains(x)))
                {
                    switch (ParameterSource.GeometryProperty)
                    {
                        case SimGeometrySourceProperty.FaceArea:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter] = FaceAlgorithms.Area((Face)Geometry);
                            break;
                        case SimGeometrySourceProperty.FaceIncline:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]
                                = FaceAlgorithms.OrientationIncline(((Face)Geometry).Normal).incline * 180 / Math.PI;
                            break;
                        case SimGeometrySourceProperty.FaceOrientation:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]
                                = FaceAlgorithms.OrientationIncline(((Face)Geometry).Normal).orientation * 180 / Math.PI;
                            break;

                        case SimGeometrySourceProperty.VolumeFloorElevation:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]
                                = VolumeAlgorithms.ElevationReference((Volume)Geometry).floor;
                            break;
                        case SimGeometrySourceProperty.VolumeCeilingElevation:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]
                                = VolumeAlgorithms.ElevationReference((Volume)Geometry).ceiling;
                            break;
                        case SimGeometrySourceProperty.VolumeHeight:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]
                                = VolumeAlgorithms.Height((Volume)Geometry).reference;
                            break;
                        case SimGeometrySourceProperty.VolumeFloorArea:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]
                                = VolumeAlgorithms.AreaBruttoNetto((Volume)Geometry).areaReference;
                            break;
                        case SimGeometrySourceProperty.VolumeVolume:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]
                                = VolumeAlgorithms.Volume((Volume)Geometry);
                            break;

                        case SimGeometrySourceProperty.EdgeLength:
                            Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]
                                = EdgeAlgorithms.Length((Edge)Geometry);
                            break;

                    }

                    if (ParameterSource.TargetParameter is SimDoubleParameter dParam)
                    {
                        dParam.Value = Aggregate(
             ParameterSource.GeometryProperty,
             ParameterSource.TargetParameter.Component.Instances
                     .Where(x => x.Placements.Any(p => p is SimInstancePlacementGeometry) && ParameterSource.InstancePassesFilter(x))
                 .Select(x => x.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]).OfType<double>().ToList());
                    }

                }
                else
                {

                    Placement.Instance.InstanceParameterValuesPersistent[ParameterSource.TargetParameter] = 0;
                    if (ParameterSource.TargetParameter is SimDoubleParameter dParam)
                    {
                        dParam.Value = Aggregate(
                      ParameterSource.GeometryProperty,
                      ParameterSource.TargetParameter.Component.Instances
                          .Where(x => x.Placements.Any(p => p is SimInstancePlacementGeometry) && ParameterSource.InstancePassesFilter(x))
                          .Select(x => x.InstanceParameterValuesPersistent[ParameterSource.TargetParameter]).OfType<double>().ToList());
                    }

                }
            }
        }


        internal void OnConnectorsInitialized()
        {
            // update all instance filters here cause component may not have been in the component list yet, therefore the instances could not be filtered yet
            // (cause no resources available without the factory/projectData)
            ParameterSource.UpdateAllInstanceFilters();
            UpdateComponent(false, false);
        }

        internal static bool SourceMatchesGeometry(SimGeometrySourceProperty property, BaseGeometry geometry)
        {
            switch (property)
            {
                case SimGeometrySourceProperty.FaceArea:
                case SimGeometrySourceProperty.FaceIncline:
                case SimGeometrySourceProperty.FaceOrientation:
                    return geometry is Face;
                case SimGeometrySourceProperty.VolumeFloorElevation:
                case SimGeometrySourceProperty.VolumeCeilingElevation:
                case SimGeometrySourceProperty.VolumeHeight:
                case SimGeometrySourceProperty.VolumeFloorArea:
                case SimGeometrySourceProperty.VolumeVolume:
                    return geometry is Volume;
                case SimGeometrySourceProperty.EdgeLength:
                    return geometry is Edge;
                default:
                    throw new NotImplementedException("Did you forget to implement a new property type here?");
            }
        }


        internal void OnPlacementRemoved()
        {
            UpdateComponent(true, false);
        }
        internal void OnSourceRemoved()
        {
            UpdateComponent(true, false);
        }

        internal void OnGeometryChanged()
        {
            UpdateComponent(false, false);
        }
        internal void OnGeometryRemoved()
        {
            UpdateComponent(false, true);
        }

        internal void ChangeGeometry(BaseGeometry geometry)
        {
            this.Geometry = geometry;
            UpdateComponent(false, false);
        }

        internal void OnFilterChanged()
        {
            UpdateComponent(false, false);
        }
    }
}
