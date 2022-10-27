using Assimp.Unmanaged;
using SIMULTAN.Data;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace SIMULTAN.Serializer.SPDXF
{
    /// <summary>
    /// IO Class for reading and writing SitePlanner (.spdxf) files.
    /// </summary>
    public static class SiteplannerDxfIO
    {

        #region Helper Classes

        private class PrefilterData { }

        private class AggregationPrefilterData : PrefilterData
        {
            public enum AggregationPrefilterType
            {
                Maximum, Minimum, Average
            }

            public AggregationPrefilterType PrefilterType { get; set; }
        }

        private class TimelinePrefilterData : PrefilterData
        {
            public int Current { get; set; }
        }

        private class ColorMapData { }

        private class MultiThresholdColorMapData : ColorMapData
        {
            public IEnumerable<SimColorMarker> ColorMapMarkers { get; set; }

            public MultiThresholdColorMapData(IEnumerable<SimColorMarker> colorMapMarkers)
            {
                this.ColorMapMarkers = colorMapMarkers;
            }
        }

        private class MultiLinearGradientColorMapData : ColorMapData
        {
            public IEnumerable<SimColorMarker> ColorMapMarkers { get; set; }

            public MultiLinearGradientColorMapData(IEnumerable<SimColorMarker> colorMapMarkers)
            {
                this.ColorMapMarkers = colorMapMarkers;
            }
        }

        #endregion

        #region Syntax

        private static DXFEntityParserElement<SitePlannerProject> siteplannerEntityElement =
            new DXFEntityParserElement<SitePlannerProject>(ParamStructTypes.SITEPLANNER, ParseSiteplannerProject,
                new DXFEntryParserElement[]
                {
                    new DXFStructArrayEntryParserElement<SitePlannerMap>(SitePlannerSaveCode.GEOMAPS, ParseSitePlannerMap,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<ResourceReference>(SitePlannerSaveCode.GEOMAP_PATH){MaxVersion = 11},
                            new DXFSingleEntryParserElement<Guid>(SitePlannerSaveCode.GEOMAP_PROJECT_ID){MinVersion = 12},
                            new DXFSingleEntryParserElement<long>(SitePlannerSaveCode.GEOMAP_RESOURCE_ID){MinVersion = 12},
                            // Elevation provider has to be string cause it uses the name to look it up later from the addons
                            new DXFSingleEntryParserElement<String>(SitePlannerSaveCode.ELEVATION_PROVIDER_TYPE),
                            new DXFSingleEntryParserElement<int>(SitePlannerSaveCode.GRID_CELL_SIZE),
                        }),
                    new DXFStructArrayEntryParserElement<SitePlannerBuilding>(SitePlannerSaveCode.BUILDINGS, ParseBuildings,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<int>(SitePlannerSaveCode.BUILDING_INDEX){ MaxVersion = 11, IsOptional = true},
                            new DXFSingleEntryParserElement<ulong>(SitePlannerSaveCode.BUILDING_ID),
                            new DXFSingleEntryParserElement<ResourceReference>(SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_PATH){MaxVersion = 11},
                            new DXFSingleEntryParserElement<Guid>(SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_PROJECT_ID){MinVersion = 12},
                            new DXFSingleEntryParserElement<long>(SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_RESOURCE_ID){MinVersion = 12},
                            new DXFSingleEntryParserElement<String>(SitePlannerSaveCode.BUILDING_CUSTOM_COLOR){MaxVersion = 11},
                            new DXFSingleEntryParserElement<Color>(SitePlannerSaveCode.BUILDING_CUSTOM_COLOR){MinVersion = 12},
                        }),
                    new DXFStructArrayEntryParserElement<SimId>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS, ParseValueMappingV12,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<int>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_INDEX){ MaxVersion = 11, IsOptional = true},
                            new DXFSingleEntryParserElement<string>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_NAME),
                            new DXFSingleEntryParserElement<Guid>(SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_LOCATION),
                            new DXFSingleEntryParserElement<long>(SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_KEY),
                            new DXFSingleEntryParserElement<SimComponentIndexUsage>(SitePlannerSaveCode.VALUE_MAPPING_INDEX_USAGE),
                            new DXFSingleEntryParserElement<String>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_TYPE) {MaxVersion = 11},
                            new DXFSingleEntryParserElement<String>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_PARAMS) {MaxVersion = 11},
                            new DXFSingleEntryParserElement<String>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_TYPE) {MaxVersion = 11},
                            new DXFSingleEntryParserElement<String>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_PARAMS) {MaxVersion = 11},
                            // Color Maps
                            new DXFEntitySequenceEntryParserElement<ColorMapData>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP,
                                new DXFComplexEntityParserElement<ColorMapData> []{
                                    // MultiThresholdColorMap
                                    new DXFComplexEntityParserElement<ColorMapData>(new DXFEntityParserElement<ColorMapData>(ParamStructTypes.COLOR_MAP_MULTI_THRESHOLD,
                                        ParseMultiThresholdColorMap,
                                        new DXFEntryParserElement[]{
                                            new DXFStructArrayEntryParserElement<SimColorMarker>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER,
                                                ParseColorMapMarker,
                                                new DXFEntryParserElement[]
                                                {
                                                    new DXFSingleEntryParserElement<double>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE),
                                                    new DXFSingleEntryParserElement<Color>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR),
                                                })
                                        })),
                                    // MultiLinearGradientColorMap
                                    new DXFComplexEntityParserElement<ColorMapData>(new DXFEntityParserElement<ColorMapData>(ParamStructTypes.COLOR_MAP_MULTI_LINEAR_GRADIENT,
                                        ParseMultiLinearGradientColorMap,
                                        new DXFEntryParserElement[]{
                                            new DXFStructArrayEntryParserElement<SimColorMarker>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER,
                                                ParseColorMapMarker,
                                                new DXFEntryParserElement[]
                                                {
                                                    new DXFSingleEntryParserElement<double>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE),
                                                    new DXFSingleEntryParserElement<Color>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR),
                                                })
                                        })),
                                }) {MinVersion = 12}, // End of Color Maps
                            // Prefilters
                            new DXFEntitySequenceEntryParserElement<PrefilterData>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER,
                                new DXFComplexEntityParserElement<PrefilterData> []{
                                    // MaximumPrefilter
                                    new DXFComplexEntityParserElement<PrefilterData>(new DXFEntityParserElement<PrefilterData>(ParamStructTypes.PREFILTER_MAXIMUM,
                                        ParseMaximumPrefilter,
                                        new DXFEntryParserElement[]{
                                            // nothing in here, parameters are empty
                                        })),
                                    // MinimumPrefilter
                                    new DXFComplexEntityParserElement<PrefilterData>(new DXFEntityParserElement<PrefilterData>(ParamStructTypes.PREFILTER_MINIMUM,
                                        ParseMinimumPrefilter,
                                        new DXFEntryParserElement[]{
                                            // nothing in here, parameters are empty
                                        })),
                                    // AveragePrefilter
                                    new DXFComplexEntityParserElement<PrefilterData>(new DXFEntityParserElement<PrefilterData>(ParamStructTypes.PREFILTER_AVERAGE,
                                        ParseAveragePrefilter,
                                        new DXFEntryParserElement[]{
                                            // nothing in here, parameters are empty
                                        })),
                                    new DXFComplexEntityParserElement<PrefilterData>(new DXFEntityParserElement<PrefilterData>(ParamStructTypes.PREFILTER_TIMELINE,
                                        ParseTimelinePrefilter,
                                        new DXFEntryParserElement[]{
                                            new DXFSingleEntryParserElement<int>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_TIMELINE_CURRENT)
                                        })),
                                }) {MinVersion = 12} // End of Color Maps

                        }) { MaxVersion = 12 } , // End of Value Mapping Associations
                    new DXFStructArrayEntryParserElement<SimId>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS, ParseValueMapping,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<Guid>(SitePlannerSaveCode.VALUE_MAPPING_GLOBAL_ID),
                            new DXFSingleEntryParserElement<long>(SitePlannerSaveCode.VALUE_MAPPING_LOCAL_ID),

                        }) { MinVersion = 13 },
                    new DXFSingleEntryParserElement<int>(SitePlannerSaveCode.VALUE_MAPPING_ACTIVE_ASSOCIATION_INDEX),
                });


        /// <summary>
        /// The root siteplanner section parser element.
        /// </summary>
        public static DXFSectionParserElement<SitePlannerProject> SiteplannerSectionElement { get; } =
            new DXFSectionParserElement<SitePlannerProject>(ParamStructTypes.SITEPLANNER_SECTION,
                new DXFEntityParserElement<SitePlannerProject>[]
                {
                    siteplannerEntityElement
                });

        private static Dictionary<String, Type> typeLookup= null;
        private static Type LookupType(string typename)
        {
            if (typeLookup == null)
            {
                //V < 12
                typeLookup = new Dictionary<string, Type>();
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Maps.MultiLinearGradientColorMap", typeof(SimLinearGradientColorMap));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Maps.MultiThresholdColorMap", typeof(SimThresholdColorMap));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Prefilters.TimelinePrefilter", typeof(SimDefaultPrefilter));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Prefilters.MinimumPrefilter", typeof(SimMinimumPrefilter));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Prefilters.MaximumPrefilter", typeof(SimMaximumPrefilter));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Prefilters.AveragePrefilter", typeof(SimAveragePrefilter));
                //V12
                typeLookup.Add("SIMULTAN.Data.SitePlanner.MultiLinearGradientColorMap", typeof(SimLinearGradientColorMap));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.MultiThresholdColorMap", typeof(SimThresholdColorMap));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.TimelinePrefilter", typeof(SimDefaultPrefilter));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.MinimumPrefilter", typeof(SimMinimumPrefilter));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.MaximumPrefilter", typeof(SimMaximumPrefilter));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.AveragePrefilter", typeof(SimAveragePrefilter));
            }

            if(typeLookup.TryGetValue(typename, out var type))
            {
                return type;
            }

            return null;
        }

        #endregion

        #region Read Write

        internal static void Read(FileInfo file, DXFParserInfo info)
        {
            info.CurrentFile = file;
            using (var fs = file.OpenRead())
            {
                if (fs.Length == 0)
                    return;
                using (var reader = new DXFStreamReader(fs))
                {
                    Read(reader, info);
                }
            }
        }
        internal static void Read(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            if (parserInfo.CurrentFile == null)
            {
                throw new ArgumentException("DXFParserInfo has no CurrentFile set but is needed for loading SitePlanner projects.");
            }

            //Version section
            if(CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }

            //Data section
            var siteplanners = SiteplannerSectionElement.Parse(reader, parserInfo);

            foreach (var sp in siteplanners)
            {
                if (sp != null)
                {
                    parserInfo.ProjectData.SitePlannerManager.SitePlannerProjects.Add(sp);
                    parserInfo.ProjectData.ComponentGeometryExchange.AddSiteplannerProject(sp);
                }
            }

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.FinishLog();
        }

        internal static void Write(FileInfo file, SitePlannerProject sitePlannerProject, ProjectData projectData)
        {
            using (var fs = file.Open(FileMode.Create, FileAccess.Write))
            {
                using (var writer = new DXFStreamWriter(fs))
                {
                    Write(writer, sitePlannerProject, projectData);
                }
            }
        }

        internal static void Write(DXFStreamWriter writer, SitePlannerProject sitePlannerProject, ProjectData projectData)
        {
            // File header
            writer.WriteVersionSection();

            // Data
            writer.StartSection(ParamStructTypes.SITEPLANNER_SECTION);

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SITEPLANNER);

            writer.WriteArray<SitePlannerMap>(SitePlannerSaveCode.GEOMAPS, sitePlannerProject.Maps,
                (map, w) =>
            {
                w.WriteGlobalId(SitePlannerSaveCode.GEOMAP_PROJECT_ID, map.GeoMapRes.ProjectId, projectData.SitePlannerManager.CalledFromLocation.GlobalID);
                w.Write<long>(SitePlannerSaveCode.GEOMAP_RESOURCE_ID, map.GeoMapRes.ResourceIndex);
                w.Write(SitePlannerSaveCode.ELEVATION_PROVIDER_TYPE, map.ElevationProviderTypeName);
                w.Write(SitePlannerSaveCode.GRID_CELL_SIZE, map.GridCellSize);
            });

            writer.WriteArray<SitePlannerBuilding>(SitePlannerSaveCode.BUILDINGS, sitePlannerProject.Buildings,
                (building, w) =>
            {
                w.Write(SitePlannerSaveCode.BUILDING_ID, building.ID);
                w.WriteGlobalId(SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_PROJECT_ID, building.GeometryModelRes.ProjectId, projectData.SitePlannerManager.CalledFromLocation.GlobalID);
                w.Write<long>(SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_RESOURCE_ID, building.GeometryModelRes.ResourceIndex);
                w.Write(SitePlannerSaveCode.BUILDING_CUSTOM_COLOR, building.CustomColor);
            });

            writer.WriteArray<SimValueMapping>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS, sitePlannerProject.ValueMappings,
                (vm, w) =>
                {
                    w.WriteGlobalId(SitePlannerSaveCode.VALUE_MAPPING_GLOBAL_ID, vm.Id.GlobalId, 
                        projectData.SitePlannerManager.CalledFromLocation.GlobalID);
                    w.Write<long>(SitePlannerSaveCode.VALUE_MAPPING_LOCAL_ID, vm.Id.LocalId);
                });

            if (sitePlannerProject.ActiveValueMapping != null)
            {
                writer.Write(SitePlannerSaveCode.VALUE_MAPPING_ACTIVE_ASSOCIATION_INDEX,
                    sitePlannerProject.ValueMappings.IndexOf(sitePlannerProject.ActiveValueMapping));
            }

            writer.EndSection();

            // EOF
            writer.WriteEOF();
        }

        /// <summary>
        /// Creates an empty SitePlannerProject as DXF (.spdxf) at the specified file path.
        /// </summary>
        /// <param name="file">Path to save the empty SitePlannerProject to</param>
        /// <param name="projectData">The project data to create the siteplanner project for</param>
        public static void CreateEmptySitePlannerProject(FileInfo file, ProjectData projectData)
        {
            Write(file, new SitePlannerProject(null), projectData);
        }
        #endregion

        #region Parsing
        private static IEnumerable<SimColorMarker> DeserializeMarkerColorMapParametersV11(string obj)
        {
            var colorMapMarkers = new List<SimColorMarker>();
            string[] markers = obj.Split(';');
            ColorConverter colorConverter = new ColorConverter();
            foreach (var m in markers)
            {
                var parameters = m.Split('|');
                var val = double.Parse(parameters[0], System.Globalization.CultureInfo.InvariantCulture);
                var col = (Color)colorConverter.ConvertFromInvariantString(parameters[1]);
                colorMapMarkers.Add(new SimColorMarker(val, col));
            }
            return colorMapMarkers;
        }

        private static SitePlannerProject ParseSiteplannerProject(DXFParserResultSet data, DXFParserInfo info)
        {
            var resource = info.ProjectData.AssetManager.GetResource(info.CurrentFile);
            if (resource == null)
            {
                throw new Exception("Could not find SitePlanner resource file in Assets. Are the Assets already loaded?");
            }

            var maps = data.Get<SitePlannerMap[]>(SitePlannerSaveCode.GEOMAPS, new SitePlannerMap[0]);
            var buildings = data.Get<SitePlannerBuilding[]>(SitePlannerSaveCode.BUILDINGS, new SitePlannerBuilding[0]);
            var valueMappingIds = data.Get<SimId[]>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS, new SimId[0]);

            var spp = new SitePlannerProject(resource);

            // SitePlannerMaps
            spp.Maps.AddRange(maps);
            // SitePlannerBuildings
            spp.Buildings.AddRange(buildings);

            // ValueMappings
            foreach (var valueMappingId in valueMappingIds)
            {
                if (valueMappingId != SimId.Empty)
                {
                    var valueMapping = info.ProjectData.IdGenerator.GetById<SimValueMapping>(valueMappingId);

                    if (valueMapping != null)
                        spp.ValueMappings.Add(valueMapping);
                    else
                        info.Log(String.Format("Could not find ValueMapping with Id {0}:{1}", valueMappingId.GlobalId, valueMappingId.LocalId));
                }
            }

            var activeAssociationIndex = data.Get<int>(SitePlannerSaveCode.VALUE_MAPPING_ACTIVE_ASSOCIATION_INDEX, -1);
            if (activeAssociationIndex >= 0 && activeAssociationIndex < spp.ValueMappings.Count)
            {
                spp.ActiveValueMapping = spp.ValueMappings[activeAssociationIndex];
            }

            return spp;
        }

        private static SimId ParseValueMappingV12(DXFParserResultSet result, DXFParserInfo info)
        {
            //Only relevant for version < 13

            var name = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_NAME, "");
            var id = result.GetSimId(SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_LOCATION, SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_KEY, info.ProjectData.SitePlannerManager.CalledFromLocation.GlobalID);
            var usage = result.Get<SimComponentIndexUsage>(SitePlannerSaveCode.VALUE_MAPPING_INDEX_USAGE, SimComponentIndexUsage.Column);

            id = new Data.SimId(id.GlobalId, info.TranslateId(typeof(SimMultiValue), id.LocalId)); // translates if version < 6

            var table = info.ProjectData.ValueManager.GetByID(id.GlobalId, id.LocalId) as SimMultiValueBigTable;
            if(table == null)
            {
                throw new Exception("Could not find Multi Value Table for ValueAssociation");
            }

            SimColorMap colorMap = null;
            SimPrefilter prefilter = null;
            SimId mappingId = SimId.Empty;

            if (info.FileVersion >= 13)
            {
                throw new NotImplementedException("TODO");
            }
            else if (info.FileVersion == 12)
            {
                // Color Maps
                var colormaps = result.Get<ColorMapData[]>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP, new ColorMapData[0]);
                if (colormaps.Count() > 0)
                {
                    // There is only one
                    var colorMapData = colormaps[0];
                    if (colorMapData is MultiThresholdColorMapData mtcmd)
                    {
                        colorMap = new SimThresholdColorMap(mtcmd.ColorMapMarkers);
                    }
                    else if (colorMapData is MultiLinearGradientColorMapData mlgcmd)
                    {
                        colorMap = new SimLinearGradientColorMap(mlgcmd.ColorMapMarkers);
                    }
                    else
                    {
                        throw new Exception(String.Format("Color map data type {0} not supported", colorMapData.GetType().Name));
                    }
                }
                else
                {
                    throw new Exception("Could not find any Color Maps.");
                }
                // Prefilters
                var prefilters = result.Get<PrefilterData[]>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER, new PrefilterData[0]);
                if (prefilters.Count() > 0)
                {
                    var loadingprefilter = prefilters[0];
                    if (loadingprefilter is AggregationPrefilterData ap)
                    {
                        switch (ap.PrefilterType)
                        {
                            case AggregationPrefilterData.AggregationPrefilterType.Maximum:
                                prefilter = new SimMaximumPrefilter();
                                break;
                            case AggregationPrefilterData.AggregationPrefilterType.Minimum:
                                prefilter = new SimMinimumPrefilter();
                                break;
                            case AggregationPrefilterData.AggregationPrefilterType.Average:
                                prefilter = new SimAveragePrefilter();
                                break;
                        }
                    }
                    else if (loadingprefilter is TimelinePrefilterData tp)
                    {
                        prefilter = new SimDefaultPrefilter();
                    }
                    else
                    {
                        throw new Exception(String.Format("Prefilter data type {0} not supported", prefilter.GetType().Name));
                    }
                }
                else
                {
                    throw new Exception("Could not find any Prefilters.");
                }

                var mapping = new SimValueMapping(name, table, prefilter, colorMap)
                {
                    ComponentIndexUsage = usage,
                };
                info.ProjectData.ValueMappings.Add(mapping);
                mappingId = mapping.Id;
            }
            else// info.FileVersion < 12
            {
                //Color Maps
                var colorMapTypeString = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_TYPE, null);
                var colorMapType = LookupType(colorMapTypeString);
                if(!String.IsNullOrEmpty(colorMapTypeString) && colorMapType == null)
                {
                    throw new Exception("Could not find type of color map: " + colorMapTypeString);
                }

                var colorMapString = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_PARAMS, "");
                if (colorMapType == typeof(SimThresholdColorMap))
                {
                    var markers = DeserializeMarkerColorMapParametersV11(colorMapString);
                    colorMap = new SimThresholdColorMap(markers);
                }
                else if (colorMapType == typeof(SimLinearGradientColorMap))
                {
                    var markers = DeserializeMarkerColorMapParametersV11(colorMapString);
                    colorMap = new SimLinearGradientColorMap(markers);
                }
                else
                    throw new NotSupportedException("Unsupported color map type");

                //Pre Filter
                var prefilterTypeString = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_TYPE, null);
                var prefilterType = LookupType(prefilterTypeString);
                if(!String.IsNullOrEmpty(prefilterTypeString) && prefilterType == null)
                {
                    throw new Exception("Could not find type of value prefilter: " + prefilterTypeString);
                }

                var prefilterString = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_PARAMS, "");
                if (prefilterType == typeof(SimMaximumPrefilter))
                {
                    prefilter = new SimMaximumPrefilter();
                }
                else if (prefilterType == typeof(SimMinimumPrefilter))
                {
                    prefilter = new SimMinimumPrefilter();
                }
                else if (prefilterType == typeof(SimAveragePrefilter))
                {
                    prefilter = new SimAveragePrefilter();
                }
                else if (prefilterType == typeof(SimDefaultPrefilter))
                {
                    prefilter = new SimDefaultPrefilter();
                }
                else
                    throw new NotSupportedException("Unsupported prefilter type");

                var mapping = new SimValueMapping(name, table, prefilter, colorMap)
                {
                    ComponentIndexUsage = usage,
                };
                info.ProjectData.ValueMappings.Add(mapping);
                mappingId = mapping.Id;
            }

            return mappingId;
        }
        private static SimId ParseValueMapping(DXFParserResultSet result, DXFParserInfo info)
        {
            return result.GetSimId(SitePlannerSaveCode.VALUE_MAPPING_GLOBAL_ID, SitePlannerSaveCode.VALUE_MAPPING_LOCAL_ID, info.GlobalId);
        }

        private static SitePlannerBuilding ParseBuildings(DXFParserResultSet result, DXFParserInfo info)
        {
            ResourceReference geoRes = null;
            ulong id = result.Get<ulong>(SitePlannerSaveCode.BUILDING_ID, 0L);
            Color color;
            if (info.FileVersion < 12)
            {
                geoRes = result.Get<ResourceReference>(SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_PATH, null);
                var colorString = result.Get<String>(SitePlannerSaveCode.BUILDING_CUSTOM_COLOR, "0 0 0");
                var splitColor = colorString.Split(' ');
                color = Color.FromRgb(byte.Parse(splitColor[0]), byte.Parse(splitColor[1]), byte.Parse(splitColor[2]));
            }
            else
            {
                geoRes = ParseResourceReference(result, SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_PROJECT_ID, SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_RESOURCE_ID, info);
                color = result.Get<Color>(SitePlannerSaveCode.BUILDING_CUSTOM_COLOR, Colors.Black);
            }

            var building = new SitePlannerBuilding(id, geoRes)
            {
                CustomColor = color,
            };

            return building;
        }

        private static SitePlannerMap ParseSitePlannerMap(DXFParserResultSet data, DXFParserInfo info)
        {
            ResourceReference mapRes = null;
            if (info.FileVersion < 12)
            {
                mapRes = data.Get<ResourceReference>(SitePlannerSaveCode.GEOMAP_PATH, null);
            }
            else
            {
                mapRes = ParseResourceReference(data, SitePlannerSaveCode.GEOMAP_PROJECT_ID, SitePlannerSaveCode.GEOMAP_RESOURCE_ID, info);
            }

            SitePlannerMap map = new SitePlannerMap(mapRes);
            var elevationProviderType = data.Get<String>(SitePlannerSaveCode.ELEVATION_PROVIDER_TYPE, null);
            if (elevationProviderType != null)
            {
                map.ElevationProviderTypeName = elevationProviderType;
            }
            map.GridCellSize = data.Get<int>(SitePlannerSaveCode.GRID_CELL_SIZE, 0);
            return map;
        }

        private static ResourceReference ParseResourceReference(DXFParserResultSet data, SitePlannerSaveCode projectIdSaveCode, SitePlannerSaveCode resourceIdSaveCode, DXFParserInfo info)
        {
            Guid projectId = data.Get<Guid>(projectIdSaveCode, new Guid());
            int resourceId = (int)data.Get<long>(resourceIdSaveCode, -1L);
            // resourceId is negative if there is none
            if (resourceId >= 0)
            {
                if (projectId == Guid.Empty)
                {
                    projectId = info.ProjectData.SitePlannerManager.CalledFromLocation.GlobalID;
                }
                return new ResourceReference(projectId, resourceId, info.ProjectData.AssetManager);
            }

            return null;
        }

        private static PrefilterData ParseTimelinePrefilter(DXFParserResultSet data, DXFParserInfo info)
        {
            int current = data.Get<int>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_TIMELINE_CURRENT, -1);
            return new TimelinePrefilterData() { Current = current };
        }

        private static PrefilterData ParseMaximumPrefilter(DXFParserResultSet arg1, DXFParserInfo arg2)
        {
            return new AggregationPrefilterData() { PrefilterType = AggregationPrefilterData.AggregationPrefilterType.Maximum };
        }
        private static PrefilterData ParseMinimumPrefilter(DXFParserResultSet arg1, DXFParserInfo arg2)
        {
            return new AggregationPrefilterData() { PrefilterType = AggregationPrefilterData.AggregationPrefilterType.Minimum };
        }
        private static PrefilterData ParseAveragePrefilter(DXFParserResultSet arg1, DXFParserInfo arg2)
        {
            return new AggregationPrefilterData() { PrefilterType = AggregationPrefilterData.AggregationPrefilterType.Average };
        }

        private static SimColorMarker ParseColorMapMarker(DXFParserResultSet data, DXFParserInfo info)
        {
            double value = data.Get<double>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE, 0);
            Color color = data.Get<Color>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR, Colors.Black);
            return new SimColorMarker(value, color);
        }

        private static ColorMapData ParseMultiThresholdColorMap(DXFParserResultSet data, DXFParserInfo info)
        {
            var markers = data.Get<SimColorMarker[]>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER, null);
            return new MultiThresholdColorMapData(markers);
        }
        private static ColorMapData ParseMultiLinearGradientColorMap(DXFParserResultSet data, DXFParserInfo info)
        {
            var markers = data.Get<SimColorMarker[]>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER, null);
            return new MultiLinearGradientColorMapData(markers);
        }
        #endregion
    }
}
