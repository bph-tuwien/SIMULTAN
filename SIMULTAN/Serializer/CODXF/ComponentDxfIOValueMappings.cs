
using SIMULTAN.Data;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Serializer.CODXF
{
    /// <summary>
    /// Provides methods to serialize and deserialize ValueMappings in a CODXF file
    /// </summary>
    public static class ComponentDxfIOValueMappings
    {
        #region Syntax ColorMaps

        /// <summary>
        /// Syntax element for a <see cref="SimThresholdColorMap"/>
        /// </summary>
        public static DXFEntityParserElementBase<SimColorMap> ThresholdColorMapEntityElement =
            new DXFComplexEntityParserElement<SimColorMap>(
                new DXFEntityParserElement<SimColorMap>(ParamStructTypes.COLOR_MAP_MULTI_THRESHOLD, ParseThresholdColorMap,
                    new DXFEntryParserElement[]
                    {
                        new DXFStructArrayEntryParserElement<SimColorMarker>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER,
                            ParseColorMarker,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<double>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE),
                                new DXFSingleEntryParserElement<Color>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR),
                            }),
                    }));

        /// <summary>
        /// Syntax element for a <see cref="SimLinearGradientColorMap"/>
        /// </summary>
        public static DXFEntityParserElementBase<SimColorMap> LinearGradientColorMapEntityElement =
            new DXFComplexEntityParserElement<SimColorMap>(
                new DXFEntityParserElement<SimColorMap>(ParamStructTypes.COLOR_MAP_MULTI_LINEAR_GRADIENT, ParseLinearGradientColorMap,
                    new DXFEntryParserElement[]
                    {
                        new DXFStructArrayEntryParserElement<SimColorMarker>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER,
                            ParseColorMarker,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<double>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE),
                                new DXFSingleEntryParserElement<Color>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR),
                            }),
                    }));

        #endregion

        #region Syntax PreFilter

        /// <summary>
        /// Syntax element for a <see cref="SimDefaultPrefilter"/>
        /// </summary>
        public static DXFEntityParserElementBase<SimPrefilter> DefaultPrefilterEntityElement =
            new DXFComplexEntityParserElement<SimPrefilter>(
                new DXFEntityParserElement<SimPrefilter>(ParamStructTypes.PREFILTER_DEFAULT, ParseDefaultPrefilter, new DXFEntryParserElement[0]));
        /// <summary>
        /// Syntax element for a <see cref="SimMinimumPrefilter"/>
        /// </summary>
        public static DXFEntityParserElementBase<SimPrefilter> MinimumPrefilterEntityElement =
            new DXFComplexEntityParserElement<SimPrefilter>(
                new DXFEntityParserElement<SimPrefilter>(ParamStructTypes.PREFILTER_MINIMUM, ParseMinimumPrefilter, new DXFEntryParserElement[0]));
        /// <summary>
        /// Syntax element for a <see cref="SimMaximumPrefilter"/>
        /// </summary>
        public static DXFEntityParserElementBase<SimPrefilter> MaximumPrefilterEntityElement =
            new DXFComplexEntityParserElement<SimPrefilter>(
                new DXFEntityParserElement<SimPrefilter>(ParamStructTypes.PREFILTER_MAXIMUM, ParseMaximumPrefilter, new DXFEntryParserElement[0]));
        /// <summary>
        /// Syntax element for a <see cref="SimAveragePrefilter"/>
        /// </summary>
        public static DXFEntityParserElementBase<SimPrefilter> AveragePrefilterEntityElement =
            new DXFComplexEntityParserElement<SimPrefilter>(
                new DXFEntityParserElement<SimPrefilter>(ParamStructTypes.PREFILTER_AVERAGE, ParseAveragePrefilter, new DXFEntryParserElement[0]));

        #endregion

        #region Syntax ValueMapping

        /// <summary>
        /// Syntax element for a <see cref="SimValueMapping"/>
        /// </summary>
        public static DXFEntityParserElementBase<SimValueMapping> ValueMappingEntityElement =
            new DXFComplexEntityParserElement<SimValueMapping>(
                new DXFEntityParserElement<SimValueMapping>(ParamStructTypes.VALUEMAPPING, ParseValueMapping,
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                        new DXFSingleEntryParserElement<string>(ValueMappingSaveCode.VALUE_MAPPING_NAME),
                        new DXFSingleEntryParserElement<Guid>(ValueMappingSaveCode.VALUE_MAPPING_TABLE_GLOBALID),
                        new DXFSingleEntryParserElement<long>(ValueMappingSaveCode.VALUE_MAPPING_TABLE_LOCALID),
                        new DXFSingleEntryParserElement<SimComponentIndexUsage>(ValueMappingSaveCode.VALUE_MAPPING_INDEX_USAGE),
                        new DXFEntitySequenceEntryParserElement<SimColorMap>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP,
                            new DXFEntityParserElementBase<SimColorMap>[]
                            {
                                ThresholdColorMapEntityElement,
                                LinearGradientColorMapEntityElement,
                            }),
                        new DXFEntitySequenceEntryParserElement<SimPrefilter>(ValueMappingSaveCode.VALUE_MAPPING_PREFILTER,
                            new DXFEntityParserElementBase<SimPrefilter>[]
                            {
                                DefaultPrefilterEntityElement,
                                MinimumPrefilterEntityElement,
                                MaximumPrefilterEntityElement,
                                AveragePrefilterEntityElement,
                            }),
                    }));

        #endregion

        #region Syntax Section

        /// <summary>
        /// Syntax element for the ValueMapping section
        /// </summary>
        internal static DXFSectionParserElement<SimValueMapping> ValueMappingSectionEntityElement =
            new DXFSectionParserElement<SimValueMapping>(ParamStructTypes.SIMVALUEMAPPING_SECTION,
                new DXFEntityParserElementBase<SimValueMapping>[]
                {
                    ValueMappingEntityElement
                });

        #endregion


        #region Section

        /// <summary>
        /// Reads a value mapping section. The results are stored in <see cref="DXFParserInfo.ProjectData"/>
        /// </summary>
        /// <param name="reader">The DXF reader to read from</param>
        /// <param name="info">Info for the parser</param>
        internal static void ReadValueMappingSection(DXFStreamReader reader, DXFParserInfo info)
        {
            var valueMappings = ComponentDxfIOValueMappings.ValueMappingSectionEntityElement.Parse(reader, info);

            info.ProjectData.ValueMappings.StartLoading();
            foreach (var valueMapping in valueMappings)
            {
                if (valueMapping != null)
                    info.ProjectData.ValueMappings.Add(valueMapping);
            }
            info.ProjectData.ValueMappings.EndLoading();
        }
        /// <summary>
        /// Writes a ValueMapping section to the DXF stream
        /// </summary>
        /// <param name="collection">The value mappings to serialize</param>
        /// <param name="writer">The writer into which the data should be written</param>
        internal static void WriteValueMappingSection(IEnumerable<SimValueMapping> collection, DXFStreamWriter writer)
        {
            writer.StartSection(ParamStructTypes.SIMVALUEMAPPING_SECTION);

            foreach (var item in collection)
            {
                WriteValueMapping(item, writer);
            }

            writer.EndSection();
        }

        #endregion

        #region SimValueMapping

        private static SimValueMapping ParseValueMapping(DXFParserResultSet data, DXFParserInfo info)
        {
            long localId = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);
            string name = data.Get<string>(ValueMappingSaveCode.VALUE_MAPPING_NAME, string.Empty);
            SimComponentIndexUsage indexUsage = data.Get<SimComponentIndexUsage>(ValueMappingSaveCode.VALUE_MAPPING_INDEX_USAGE,
                SimComponentIndexUsage.Row);

            SimId tableId = data.GetSimId(ValueMappingSaveCode.VALUE_MAPPING_TABLE_GLOBALID, ValueMappingSaveCode.VALUE_MAPPING_TABLE_LOCALID,
                info.GlobalId);

            var colorMap = data.Get<SimColorMap[]>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP, new SimColorMap[0])
                .Where(x => x != null).FirstOrDefault();
            var prefilter = data.Get<SimPrefilter[]>(ValueMappingSaveCode.VALUE_MAPPING_PREFILTER, new SimPrefilter[0])
                .Where(x => x != null).FirstOrDefault();

            if (colorMap == null)
            {
                info.Log(String.Format("Unable to find colormap for ValueMapping {0} (Id={1})", name, localId));
                return null;
            }
            if (prefilter == null)
            {
                info.Log(String.Format("Unable to find prefilter for ValueMapping {0} (Id={1})", name, localId));
                return null;
            }

            //Try to find table
            var table = info.ProjectData.IdGenerator.GetById<SimMultiValueBigTable>(tableId);
            if (table == null)
            {
                info.Log(String.Format("Unable to find valuemapping table with Id {0}", tableId.LocalId));
                return null;
            }

            return new SimValueMapping(name, table, prefilter, colorMap)
            {
                ComponentIndexUsage = indexUsage,
                Id = new SimId(info.GlobalId, localId),
            };
        }

        internal static void WriteValueMapping(SimValueMapping valueMapping, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.VALUEMAPPING);

            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, valueMapping.Id.LocalId);
            writer.Write(ValueMappingSaveCode.VALUE_MAPPING_NAME, valueMapping.Name);

            writer.WriteGlobalId(ValueMappingSaveCode.VALUE_MAPPING_TABLE_GLOBALID, valueMapping.Table.Id.GlobalId, valueMapping.Id.GlobalId);
            writer.Write(ValueMappingSaveCode.VALUE_MAPPING_TABLE_LOCALID, valueMapping.Table.Id.LocalId);

            writer.Write(ValueMappingSaveCode.VALUE_MAPPING_INDEX_USAGE, valueMapping.ComponentIndexUsage);

            writer.WriteEntitySequence(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP, new SimColorMap[] { valueMapping.ColorMap },
                (colorMap, iwriter) =>
                {
                    if (colorMap is SimThresholdColorMap tcm)
                        WriteThresholdColorMap(tcm, writer);
                    else if (colorMap is SimLinearGradientColorMap lcm)
                        WriteLinearGradientColorMap(lcm, writer);
                    else
                    {
                        throw new Exception("Unsupported SimColorMap type " + colorMap.GetType().Name);
                    }
                });

            writer.WriteEntitySequence(ValueMappingSaveCode.VALUE_MAPPING_PREFILTER, new SimPrefilter[] { valueMapping.Prefilter },
                WriteSimplePrefilter);
            writer.EndComplexEntity();
        }

        #endregion

        #region Color Maps

        private static SimColorMap ParseThresholdColorMap(DXFParserResultSet data, DXFParserInfo info)
        {
            var marker = data.Get<SimColorMarker[]>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER, new SimColorMarker[0]);
            return new SimThresholdColorMap(marker);
        }
        private static SimColorMap ParseLinearGradientColorMap(DXFParserResultSet data, DXFParserInfo info)
        {
            var marker = data.Get<SimColorMarker[]>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER, new SimColorMarker[0]);
            return new SimLinearGradientColorMap(marker);
        }
        private static SimColorMarker ParseColorMarker(DXFParserResultSet data, DXFParserInfo info)
        {
            var value = data.Get<double>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE, 0.0);
            var color = data.Get<Color>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR, Colors.White);

            return new SimColorMarker(value, color);
        }

        internal static void WriteThresholdColorMap(SimThresholdColorMap colorMap, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.COLOR_MAP_MULTI_THRESHOLD);
            WriteColorMapMarkers(colorMap.ColorMarkers, writer);

            writer.EndComplexEntity();
        }
        internal static void WriteLinearGradientColorMap(SimLinearGradientColorMap colorMap, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.COLOR_MAP_MULTI_LINEAR_GRADIENT);
            WriteColorMapMarkers(colorMap.ColorMarkers, writer);

            writer.EndComplexEntity();
        }

        private static void WriteColorMapMarkers(IEnumerable<SimColorMarker> markers, DXFStreamWriter w)
        {
            w.WriteArray<SimColorMarker>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER, markers,
                (marker, mw) =>
                {
                    mw.Write<double>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_VALUE, marker.Value);
                    mw.Write<Color>(ValueMappingSaveCode.VALUE_MAPPING_COLOR_MAP_MARKER_COLOR, marker.Color);
                });
        }

        #endregion

        #region Prefilter

        private static SimPrefilter ParseDefaultPrefilter(DXFParserResultSet data, DXFParserInfo info)
        {
            return new SimDefaultPrefilter();
        }
        private static SimPrefilter ParseMinimumPrefilter(DXFParserResultSet data, DXFParserInfo info)
        {
            return new SimMinimumPrefilter();
        }
        private static SimPrefilter ParseMaximumPrefilter(DXFParserResultSet data, DXFParserInfo info)
        {
            return new SimMaximumPrefilter();
        }
        private static SimPrefilter ParseAveragePrefilter(DXFParserResultSet data, DXFParserInfo info)
        {
            return new SimAveragePrefilter();
        }

        private static Dictionary<Type, string> prefilterTypeNames = new Dictionary<Type, string>
        {
            { typeof(SimDefaultPrefilter), ParamStructTypes.PREFILTER_DEFAULT },
            { typeof(SimMinimumPrefilter), ParamStructTypes.PREFILTER_MINIMUM },
            { typeof(SimMaximumPrefilter), ParamStructTypes.PREFILTER_MAXIMUM },
            { typeof(SimAveragePrefilter), ParamStructTypes.PREFILTER_AVERAGE },
        };
        internal static void WriteSimplePrefilter(SimPrefilter prefilter, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            if (prefilterTypeNames.TryGetValue(prefilter.GetType(), out var typestr))
                writer.Write(ParamStructCommonSaveCode.ENTITY_START, typestr);
            else
                throw new Exception("Unsupported SimPrefilter Type: " + prefilter.GetType().FullName);

            writer.EndComplexEntity();
        }

        #endregion
    }
}
