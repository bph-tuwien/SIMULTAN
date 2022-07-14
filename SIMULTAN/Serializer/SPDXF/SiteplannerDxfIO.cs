using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
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
            public IEnumerable<ColorMapMarker> ColorMapMarkers { get; set; }

            public MultiThresholdColorMapData(IEnumerable<ColorMapMarker> colorMapMarkers)
            {
                this.ColorMapMarkers = colorMapMarkers;
            }
        }

        private class MultiLinearGradientColorMapData : ColorMapData
        {
            public IEnumerable<ColorMapMarker> ColorMapMarkers { get; set; }

            public MultiLinearGradientColorMapData(IEnumerable<ColorMapMarker> colorMapMarkers)
            {
                this.ColorMapMarkers = colorMapMarkers;
            }
        }

        #endregion

        #region Syntax

        private static DXFEntityParserElement<SitePlannerProject> siteplannerEntityElement =
            new DXFEntityParserElement<SitePlannerProject>(ParamStructTypes.SITEPLANNER, ParseSiteplannerProjcet,
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
                    new DXFStructArrayEntryParserElement<ValueMappingAssociation>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS, ParseValueMappingAssociations,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<int>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_INDEX){ MaxVersion = 11, IsOptional = true},
                            new DXFSingleEntryParserElement<string>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_NAME),
                            new DXFSingleEntryParserElement<Guid>(SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_LOCATION),
                            new DXFSingleEntryParserElement<long>(SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_KEY),
                            new DXFSingleEntryParserElement<ComponentIndexUsage>(SitePlannerSaveCode.VALUE_MAPPING_INDEX_USAGE),
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
                                            new DXFStructArrayEntryParserElement<ColorMapMarker>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER,
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
                                            new DXFStructArrayEntryParserElement<ColorMapMarker>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER,
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

                        }), // End of Value Mapping Associations
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
                typeLookup = new Dictionary<string, Type>();
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Maps.MultiLinearGradientColorMap", typeof(MultiLinearGradientColorMap));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Maps.MultiThresholdColorMap", typeof(MultiThresholdColorMap));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Prefilters.TimelinePrefilter", typeof(TimelinePrefilter));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Prefilters.MinimumPrefilter", typeof(MinimumPrefilter));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Prefilters.MaximumPrefilter", typeof(MaximumPrefilter));
                typeLookup.Add("ParameterStructure.SitePlanner.ValueMapping.Prefilters.AveragePrefilter", typeof(AveragePrefilter));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.MultiLinearGradientColorMap", typeof(MultiLinearGradientColorMap));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.MultiThresholdColorMap", typeof(MultiThresholdColorMap));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.TimelinePrefilter", typeof(TimelinePrefilter));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.MinimumPrefilter", typeof(MinimumPrefilter));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.MaximumPrefilter", typeof(MaximumPrefilter));
                typeLookup.Add("SIMULTAN.Data.SitePlanner.AveragePrefilter", typeof(AveragePrefilter));
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
            try
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }
            catch (Exception) //Happens in very old version (< version 4) where the version section wasn't present
            {
                reader.Seek(0);
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

            writer.WriteArray<ValueMappingAssociation>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS, sitePlannerProject.ValueMap.ParametersAssociations,
                (assoc, w) =>
            {
                w.Write(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_NAME, assoc.Name);
                w.WriteGlobalId(SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_LOCATION, assoc.Parameters.ValueTable.GlobalID, projectData.SitePlannerManager.CalledFromLocation.GlobalID);
                w.Write(SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_KEY, assoc.Parameters.ValueTable.LocalID);
                w.Write(SitePlannerSaveCode.VALUE_MAPPING_INDEX_USAGE, assoc.Parameters.ComponentIndexUsage);

                w.WriteEntitySequence<IValueToColorMap>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP, new IValueToColorMap[] { assoc.Parameters.ValueToColorMap },
                    (map, wr) =>
                    {
                        if (map is MultiThresholdColorMap mtcm)
                        {
                            wr.StartComplexEntity();

                            wr.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.COLOR_MAP_MULTI_THRESHOLD);

                            var cmparams = (MarkerColorMapParameters)mtcm.Parameters;
                            WriteColorMapMarkers(cmparams.Markers, wr);

                            wr.EndComplexEntity();
                        }
                        else if (map is MultiLinearGradientColorMap mlgcm)
                        {
                            wr.StartComplexEntity();

                            wr.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.COLOR_MAP_MULTI_LINEAR_GRADIENT);

                            var cmparams = (MarkerColorMapParameters)mlgcm.Parameters;
                            WriteColorMapMarkers(cmparams.Markers, wr);

                            wr.EndComplexEntity();
                        }
                        else
                        {
                            throw new Exception("Unsupported IValueColorMap type " + map.GetType().Name);
                        }
                    });


                w.WriteEntitySequence<IValuePrefilter>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER, new IValuePrefilter[] { assoc.Parameters.ValuePreFilter },
                    (prefilter, wr) =>
                    {
                        if (prefilter is MaximumPrefilter)
                        {
                            wr.StartComplexEntity();

                            wr.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.PREFILTER_MAXIMUM);

                            wr.EndComplexEntity();
                        }
                        else if (prefilter is MinimumPrefilter)
                        {
                            wr.StartComplexEntity();

                            wr.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.PREFILTER_MINIMUM);

                            wr.EndComplexEntity();
                        }
                        else if (prefilter is AveragePrefilter)
                        {
                            wr.StartComplexEntity();

                            wr.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.PREFILTER_AVERAGE);

                            wr.EndComplexEntity();
                        }
                        else if (prefilter is TimelinePrefilter tp)
                        {
                            wr.StartComplexEntity();

                            wr.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.PREFILTER_TIMELINE);

                            var tpParams = tp.Parameters as TimelinePrefilterParameters;
                            wr.Write(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_TIMELINE_CURRENT, tpParams.Current);

                            wr.EndComplexEntity();
                        }
                        else
                        {
                            throw new Exception("Unsupported IValuePrefilter type " + prefilter.GetType().Name);
                        }

                    });

            });


            if (sitePlannerProject.ValueMap.ActiveParametersAssociation != null)
            {
                writer.Write(SitePlannerSaveCode.VALUE_MAPPING_ACTIVE_ASSOCIATION_INDEX, sitePlannerProject.ValueMap.ActiveParametersAssociationIndex);
            }

            writer.EndSection();

            // EOF
            writer.WriteEOF();
        }

        private static void WriteColorMapMarkers(IEnumerable<ColorMapMarker> markers, DXFStreamWriter w)
        {
            w.WriteArray<ColorMapMarker>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER, markers,
                (marker, mw) =>
                {
                    mw.Write<double>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE, marker.Value);
                    mw.Write<Color>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR, marker.Color);
                });
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
        private static IEnumerable<ColorMapMarker> DeserializeMarkerColorMapParametersV11(string obj)
        {
            var colorMapMarkers = new List<ColorMapMarker>();
            string[] markers = obj.Split(';');
            ColorConverter colorConverter = new ColorConverter();
            foreach (var m in markers)
            {
                var parameters = m.Split('|');
                var val = double.Parse(parameters[0], System.Globalization.CultureInfo.InvariantCulture);
                var col = (Color)colorConverter.ConvertFromInvariantString(parameters[1]);
                colorMapMarkers.Add(new ColorMapMarker(val, col));
            }
            return colorMapMarkers;
        }

        private static int DeserializeTimelinePrefilterV11(string obj)
        {
            string[] parameters = obj.Split(';');
            return int.Parse(parameters[0]);
        }

        private static SitePlannerProject ParseSiteplannerProjcet(DXFParserResultSet data, DXFParserInfo info)
        {
            var resource = info.ProjectData.AssetManager.GetResource(info.CurrentFile);
            if (resource == null)
            {
                throw new Exception("Could not find SitePlanner resource file in Assets. Are the Assets already loaded?");
            }

            var maps = data.Get<SitePlannerMap[]>(SitePlannerSaveCode.GEOMAPS, new SitePlannerMap[0]);
            var buildings = data.Get<SitePlannerBuilding[]>(SitePlannerSaveCode.BUILDINGS, new SitePlannerBuilding[0]);
            var associations = data.Get<ValueMappingAssociation[]>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS, new ValueMappingAssociation[0]);

            var spp = new SitePlannerProject(resource);

            // SitePlannerMaps
            spp.Maps.AddRange(maps);
            // SitePlannerBuildings
            spp.Buildings.AddRange(buildings);

            // ValueMappingAssoctiaions
            var valueMap = new ValueMap();
            valueMap.ParametersAssociations.AddRange(associations);

            valueMap.EstablishValueFieldConnection(info.ProjectData.ValueManager);

            var activeAssociationIndex = data.Get<int>(SitePlannerSaveCode.VALUE_MAPPING_ACTIVE_ASSOCIATION_INDEX, -1);
            if (activeAssociationIndex >= 0)
            {
                valueMap.ActiveParametersAssociationIndex = activeAssociationIndex;
            }

            spp.ValueMap = valueMap;

            return spp;
        }
        private static ValueMappingAssociation ParseValueMappingAssociations(DXFParserResultSet result, DXFParserInfo info)
        {
            var name = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_NAME, "");
            var id = result.GetSimId(SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_LOCATION, SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_KEY, info.ProjectData.SitePlannerManager.CalledFromLocation.GlobalID);
            var usage = result.Get<ComponentIndexUsage>(SitePlannerSaveCode.VALUE_MAPPING_INDEX_USAGE, ComponentIndexUsage.Column);

            id = new Data.SimId(id.GlobalId, info.TranslateId(typeof(SimMultiValue), id.LocalId)); // translates if version < 6

            var table = info.ProjectData.ValueManager.GetByID(id.GlobalId, id.LocalId) as SimMultiValueBigTable;
            if(table == null)
            {
                throw new Exception("Could not find Multi Value Table for ValueAssociation");
            }
            var vmp = new ValueMappingParameters(table);
            vmp.ComponentIndexUsage = usage;

            if (info.FileVersion < 12)
            {
                var colorMapTypeString = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_TYPE, null);
                var colorMapType = LookupType(colorMapTypeString);
                if(!String.IsNullOrEmpty(colorMapTypeString) && colorMapType == null)
                {
                    throw new Exception("Could not find type of color map: " + colorMapTypeString);
                }
                var colormap = vmp.RegisteredColorMaps.FirstOrDefault(x => x.GetType() == colorMapType);
                if (colormap != null)
                {
                    var colorMapString = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_PARAMS, "");
                    var markerParams = colormap.Parameters as MarkerColorMapParameters;
                    if (markerParams != null)
                    {
                        var markes = DeserializeMarkerColorMapParametersV11(colorMapString);
                        markerParams.Markers.Clear();
                        markerParams.Markers.AddRange(markes);
                    }
                    else
                    {
                        throw new Exception("Unsupported ColorMapParamerters Type " + colormap.Parameters.GetType().Name);
                    }
                    vmp.ValueToColorMap = colormap;
                }

                var prefilterTypeString = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_TYPE, null);
                var prefilterType = LookupType(prefilterTypeString);
                if(!String.IsNullOrEmpty(prefilterTypeString) && prefilterType == null)
                {
                    throw new Exception("Could not find type of value prefilter: " + prefilterTypeString);
                }
                var prefilter = vmp.RegisteredValuePrefilters.FirstOrDefault(x => x.GetType() == prefilterType);
                if (prefilter != null)
                {
                    var prefilterString = result.Get<String>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_PARAMS, "");
                    if (prefilterType == typeof(TimelinePrefilter))
                    {
                        int current = DeserializeTimelinePrefilterV11(prefilterString);
                        ((TimelinePrefilterParameters)prefilter.Parameters).Current = current;
                    }

                    vmp.ValuePreFilter = prefilter;
                }
            }
            else
            {
                // Color Maps
                var colormaps = result.Get<ColorMapData[]>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP, new ColorMapData[0]);
                if (colormaps.Count() > 0)
                {
                    // There is only one
                    var colorMapData = colormaps[0];
                    Type colorMapType = null;
                    IEnumerable<ColorMapMarker> markers = null;
                    if (colorMapData is MultiThresholdColorMapData mtcmd)
                    {
                        colorMapType = typeof(MultiThresholdColorMap);
                        markers = mtcmd.ColorMapMarkers;
                    }
                    else if (colorMapData is MultiLinearGradientColorMapData mlgcmd)
                    {
                        colorMapType = typeof(MultiLinearGradientColorMap);
                        markers = mlgcmd.ColorMapMarkers;
                    }
                    else
                    {
                        throw new Exception(String.Format("Color map data type {0} not supported", colorMapData.GetType().Name));
                    }
                    var colormap = vmp.RegisteredColorMaps.FirstOrDefault(x => x.GetType() == colorMapType);
                    var parameters = (MarkerColorMapParameters)colormap.Parameters;
                    parameters.Markers.Clear();
                    parameters.Markers.AddRange(markers);
                    vmp.ValueToColorMap = colormap;
                }
                else
                {
                    throw new Exception("Could not find any Color Maps.");
                }
                // Prefilters
                var prefilters = result.Get<PrefilterData[]>(SitePlannerSaveCode.VALUE_MAPPING_PREFILTER, new PrefilterData[0]);
                if (prefilters.Count() > 0)
                {
                    var prefilter = prefilters[0];
                    if (prefilter is AggregationPrefilterData ap)
                    {
                        Type prefType = null;
                        switch (ap.PrefilterType)
                        {
                            case AggregationPrefilterData.AggregationPrefilterType.Maximum:
                                prefType = typeof(MaximumPrefilter);
                                break;
                            case AggregationPrefilterData.AggregationPrefilterType.Minimum:
                                prefType = typeof(MinimumPrefilter);
                                break;
                            case AggregationPrefilterData.AggregationPrefilterType.Average:
                                prefType = typeof(AveragePrefilter);
                                break;
                        }

                        var valuePrefilter = vmp.RegisteredValuePrefilters.FirstOrDefault(x => x.GetType() == prefType);
                        if(valuePrefilter != null)
                        {
                            vmp.ValuePreFilter = valuePrefilter;
                        }
                    }
                    else if (prefilter is TimelinePrefilterData tp)
                    {

                        var valuePrefilter = vmp.RegisteredValuePrefilters.FirstOrDefault(x => x.GetType() == typeof(TimelinePrefilter));
                        if(valuePrefilter != null)
                        {
                            ((TimelinePrefilterParameters)valuePrefilter.Parameters).Current = tp.Current;
                            vmp.ValuePreFilter = valuePrefilter;
                        }
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
            }

            var vma = new ValueMappingAssociation(name, vmp);
            return vma;
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

        private static ColorMapMarker ParseColorMapMarker(DXFParserResultSet data, DXFParserInfo info)
        {
            double value = data.Get<double>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE, 0);
            Color color = data.Get<Color>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR, Colors.Black);
            return new ColorMapMarker(value, color);
        }

        private static ColorMapData ParseMultiThresholdColorMap(DXFParserResultSet data, DXFParserInfo info)
        {
            var markers = data.Get<ColorMapMarker[]>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER, null);
            return new MultiThresholdColorMapData(markers);
        }
        private static ColorMapData ParseMultiLinearGradientColorMap(DXFParserResultSet data, DXFParserInfo info)
        {
            var markers = data.Get<ColorMapMarker[]>(SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER, null);
            return new MultiLinearGradientColorMapData(markers);
        }
        #endregion
    }
}
