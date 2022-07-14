using SIMULTAN.Data.Components;
using SIMULTAN.Excel;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Serializer
{
    /// <summary>
    /// Provides methods for serializing Excel Tools into ETDXF files
    /// </summary>
    public static class ExcelDxfIO
    {
        #region Type Lookup

        private static Dictionary<string, SimInstanceType> instanceTypeTranslation = new Dictionary<string, SimInstanceType>
        {
            { "NONE", SimInstanceType.None },
            { "DESCRIBES", SimInstanceType.Entity3D },
            { "DESCRIBES_3D", SimInstanceType.GeometricVolume },
            { "DESCRIBES_2DorLESS", SimInstanceType.GeometricSurface },
            { "ALIGNED_WITH", SimInstanceType.AttributesFace },
            { "CONTAINED_IN", SimInstanceType.NetworkNode },
            { "CONNECTS", SimInstanceType.NetworkEdge },
            { "GROUPS", SimInstanceType.Group },
            { "PARAMETERIZES", SimInstanceType.BuiltStructure },
            { "InPort", SimInstanceType.InPort },
            { "SimNetworkBlock", SimInstanceType.SimNetworkBlock },
            { "OutPort", SimInstanceType.OutPort },
            { "ATTRIBUTES_EDGE", SimInstanceType.AttributesEdge },
            { "ATTRIBUTES_POINT", SimInstanceType.AttributesPoint },
        };

        private static Dictionary<SimInstanceType, string> instanceTypeUntranslation = new Dictionary<SimInstanceType, string>
        {
            { SimInstanceType.None, "NONE" },
            { SimInstanceType.Entity3D, "DESCRIBES" },
            { SimInstanceType.GeometricVolume, "DESCRIBES_3D" },
            { SimInstanceType.GeometricSurface, "DESCRIBES_2DorLESS" },
            { SimInstanceType.AttributesFace, "ALIGNED_WITH" },
            { SimInstanceType.NetworkNode, "CONTAINED_IN" },
            { SimInstanceType.NetworkEdge, "CONNECTS" },
            { SimInstanceType.Group, "GROUPS" },
            { SimInstanceType.BuiltStructure, "PARAMETERIZES" },
            { SimInstanceType.InPort, "InPort" },
            { SimInstanceType.SimNetworkBlock, "SimNetworkBlock" },
            { SimInstanceType.OutPort, "OutPort" },
            { SimInstanceType.AttributesEdge, "ATTRIBUTES_EDGE" },
            { SimInstanceType.AttributesPoint, "ATTRIBUTES_POINT" },
        };

        internal static Dictionary<string, Type> DeserializerTypename { get; } = new Dictionary<string, Type>();

        static ExcelDxfIO()
        {
            foreach (var type in typeof(ExcelDxfIO).Assembly.GetTypes())
            {
                var serializerNameAttrib = type.GetCustomAttribute<DXFSerializerTypeNameAttribute>();
                if (serializerNameAttrib != null)
                {
                    DeserializerTypename.Add(serializerNameAttrib.Name, type);
                }
            }
        }

        #endregion


        #region Syntax Rule

        /// <summary>
        /// Syntax for a <see cref="ExcelMappingNode"/>
        /// </summary>
        internal static DXFEntityParserElementBase<ExcelMappingNode> ExcelRuleEntityElement =
            new DXFComplexEntityParserElement<ExcelMappingNode>(
                new DXFEntityParserElement<ExcelMappingNode>(ParamStructTypes.EXCEL_RULE,
                    (data, info) => ParseExcelRule(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<string>(ExcelMappingSaveCode.RULE_NODE_NAME),
                        new DXFSingleEntryParserElement<string>(ExcelMappingSaveCode.RULE_SHEET_NAME),
                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.RULE_OFFSET_FROM_PARENT_X),
                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.RULE_OFFSET_FROM_PARENT_Y),
                        new DXFSingleEntryParserElement<MappingSubject>(ExcelMappingSaveCode.RULE_SUBJECT),

                        new DXFStructArrayEntryParserElement<KeyValuePair<string, Type>>(ExcelMappingSaveCode.RULE_PROPERTIES,
                            ParseExcelRuleProperty,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.STRING_VALUE),
                                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.V10_VALUE),
                            }),

                        new DXFSingleEntryParserElement<ExcelMappingRange>(ExcelMappingSaveCode.RULE_MAPPINGRANGE),
                        new DXFSingleEntryParserElement<bool>(ExcelMappingSaveCode.RULE_ORDER_HORIZONTALLY),

                        new DXFStructArrayEntryParserElement<(string, object)>(ExcelMappingSaveCode.RULE_FILTER,
                            ParseFilterObject,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.V10_VALUE),
                                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.STRING_VALUE),
                            }),

                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.RULE_OFFSET_BTW_APPLICATIONS_X),
                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.RULE_OFFSET_BTW_APPLICATIONS_Y),

                        new DXFEntitySequenceEntryParserElement<ExcelMappingNode>(ExcelMappingSaveCode.RULE_CHILDREN,
                            new DXFRecursiveEntityParserElement<ExcelMappingNode>(ParamStructTypes.EXCEL_RULE, "Excel Rule")
                            ),

                        new DXFSingleEntryParserElement<bool>(ExcelMappingSaveCode.RULE_PREPEND_CONTENT_TO_CHILDREN),
                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.TRAVERSE_MAX_ELEM),
                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.TRAVERSE_MAX_LEVELS),
                        new DXFSingleEntryParserElement<TraversalStrategy>(ExcelMappingSaveCode.TRAVERSE_STRATEGY),
                        new DXFSingleEntryParserElement<bool>(ExcelMappingSaveCode.TRAVERSAL_ACTIVATED),
                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.VERSION),
                    })
                )
            {
                Identifier = "Excel Rule"
            };

        #endregion

        #region Syntax Range Unmapping Rule

        internal static DXFEntityParserElementBase<(ExcelMappedData, Type)> ExcelRangeUnmappingRuleEntityElement =
            new DXFEntityParserElement<(ExcelMappedData, Type)>(ParamStructTypes.EXCEL_DATA_RESULT, ParseRangeUnmappingRule,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<string>(ExcelMappingSaveCode.DATA_MAP_SHEET_NAME),
                    new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.DATA_MAP_RANGE_X) { MinVersion = 12 },
                    new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.DATA_MAP_RANGE_Y) { MinVersion = 12 },
                    new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.DATA_MAP_RANGE_Z) { MinVersion = 12 },
                    new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.DATA_MAP_RANGE_W) { MinVersion = 12 },
                    new DXFSingleEntryParserElement<Type>(ExcelMappingSaveCode.DATA_MAP_TYPE),

                    new DXFSingleEntryParserElement<double>(ExcelMappingSaveCode.DATA_MAP_RANGE_X) { MaxVersion = 11 },
                    new DXFSingleEntryParserElement<double>(ExcelMappingSaveCode.DATA_MAP_RANGE_Y) { MaxVersion = 11 },
                    new DXFSingleEntryParserElement<double>(ExcelMappingSaveCode.DATA_MAP_RANGE_Z) { MaxVersion = 11 },
                    new DXFSingleEntryParserElement<double>(ExcelMappingSaveCode.DATA_MAP_RANGE_W) { MaxVersion = 11 },
                });

        #endregion

        #region Syntax Unmapping Rule

        internal static DXFEntityParserElementBase<ExcelUnmappingRule> ExcelUnmappingRuleEntityElement =
            new DXFComplexEntityParserElement<ExcelUnmappingRule>(
                new DXFEntityParserElement<ExcelUnmappingRule>(ParamStructTypes.EXCEL_UNMAPPING, ParseUnmappingRule,
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<string>(ExcelMappingSaveCode.RULE_NODE_NAME),

                        new DXFEntitySequenceEntryParserElement<(ExcelMappedData, Type)>(
                            ExcelMappingSaveCode.UNMAP_DATA,
                            ExcelRangeUnmappingRuleEntityElement
                            ),

                        new DXFSingleEntryParserElement<bool>(ExcelMappingSaveCode.UNMAP_FILTER_SWITCH),

                        new DXFStructArrayEntryParserElement<(string, object)>(ExcelMappingSaveCode.UNMAP_FILTER_COMP,
                            ParseFilterObject,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.V10_VALUE),
                                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.STRING_VALUE),
                            }),
                        new DXFStructArrayEntryParserElement<(string, object)>(ExcelMappingSaveCode.UNMAP_FILTER_PARAM,
                            ParseFilterObject,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.V10_VALUE),
                                new DXFSingleEntryParserElement<string>(ParamStructCommonSaveCode.STRING_VALUE),
                            }),

                        new DXFSingleEntryParserElement<long>(ExcelMappingSaveCode.UNMAP_TARGET_PARAM),

                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.UNMAP_PARAM_POINTER_X),
                        new DXFSingleEntryParserElement<int>(ExcelMappingSaveCode.UNMAP_PARAM_POINTER_Y),
                    }));

        #endregion

        #region Syntax Tools

        internal static DXFEntityParserElementBase<ExcelTool> ExcelToolEntityElement =
            new DXFComplexEntityParserElement<ExcelTool>(
                new DXFEntityParserElement<ExcelTool>(ParamStructTypes.EXCEL_TOOL,
                    (data, info) => ParseExcelTool(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<string>(ExcelMappingSaveCode.TOOL_NAME),
                        new DXFEntitySequenceEntryParserElement<ExcelMappingNode>(ExcelMappingSaveCode.TOOL_INPUTRULES, ExcelRuleEntityElement),
                        new DXFSingleEntryParserElement<string>(ExcelMappingSaveCode.TOOL_MACRO_NAME),
                        new DXFEntitySequenceEntryParserElement<(ExcelMappedData, Type)>(ExcelMappingSaveCode.TOOL_OUTPUTRANGERULES, 
                            ExcelRangeUnmappingRuleEntityElement),
                        new DXFEntitySequenceEntryParserElement<ExcelUnmappingRule>(ExcelMappingSaveCode.TOOL_OUTPUTRULES,
                            ExcelUnmappingRuleEntityElement),
                    }));

        #endregion

        #region Syntax Section

        internal static DXFSectionParserElement<ExcelTool> ExcelToolSectionEntityElement =
            new DXFSectionParserElement<ExcelTool>(ParamStructTypes.EXCEL_SECTION,
                new DXFEntityParserElementBase<ExcelTool>[]
                {
                    ExcelToolEntityElement
                });

        #endregion


        /// <summary>
        /// Writes all Excel Tool in a project into a file
        /// </summary>
        /// <param name="file">The target file</param>
        /// <param name="projectData">The project data which includes the Excel Tools</param>
        public static void Write(FileInfo file, ProjectData projectData)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(fs))
                {
                    Write(writer, projectData);
                }
            }
        }
        
        internal static void Write(DXFStreamWriter writer, ProjectData projectData)
        {
            //File header
            writer.WriteVersionSection();

            //Data
            writer.StartSection(ParamStructTypes.EXCEL_SECTION);

            foreach (var tool in projectData.ExcelToolMappingManager.RegisteredTools)
            {
                WriteExcelTool(tool, writer);
            }

            writer.EndSection();

            //EOF
            writer.WriteEOF();
        }


        /// <summary>
        /// Reads a ETDXF file and loads the Excel Tools into a project
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <param name="parserInfo">Additional info for parsing. Also includes the project data</param>
        public static void Read(FileInfo file, DXFParserInfo parserInfo)
        {
            using (FileStream stream = file.OpenRead())
            {
                if (stream.Length == 0)
                    return;

                using (DXFStreamReader reader = new DXFStreamReader(stream))
                {
                    Read(reader, parserInfo);
                }
            }
        }

        internal static void Read(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            //Version section
            try
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }
            catch (Exception) //Happens in old version (< version 12) where the version section wasn't present
            {
                reader.Seek(0);
            }

            //Data section
            var tools = ExcelToolSectionEntityElement.Parse(reader, parserInfo);

            foreach (var tool in tools.Where(x => x != null))
                parserInfo.ProjectData.ExcelToolMappingManager.RegisteredTools.Add(tool);

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.ProjectData.ExcelToolMappingManager.RestoreDependencies(parserInfo.ProjectData);
        }


        #region Excel Tool

        internal static void WriteExcelTool(ExcelTool tool, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.EXCEL_TOOL);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(ExcelTool));
            writer.Write(ExcelMappingSaveCode.TOOL_NAME, tool.Name);
            writer.WriteEntitySequence(ExcelMappingSaveCode.TOOL_INPUTRULES, tool.InputRules.Skip(1), WriteExcelRule);
            writer.Write(ExcelMappingSaveCode.TOOL_MACRO_NAME, tool.MacroName);
            writer.WriteEntitySequence(ExcelMappingSaveCode.TOOL_OUTPUTRANGERULES, tool.OutputRangeRules, WriteRangeUnmappingRule);
            writer.WriteEntitySequence(ExcelMappingSaveCode.TOOL_OUTPUTRULES, tool.OutputRules, WriteUnmappingRule);

            writer.EndComplexEntity();
        }

        private static ExcelTool ParseExcelTool(DXFParserResultSet data, DXFParserInfo info)
        {
            var name = data.Get<string>(ExcelMappingSaveCode.TOOL_NAME, string.Empty);
            var inputRules = data.Get<ExcelMappingNode[]>(ExcelMappingSaveCode.TOOL_INPUTRULES, new ExcelMappingNode[0]);
            var outputRangeRules = data.Get<(ExcelMappedData data, Type type)[]>(ExcelMappingSaveCode.TOOL_OUTPUTRANGERULES,
                new (ExcelMappedData, Type)[0]);
            var outputRules = data.Get<ExcelUnmappingRule[]>(ExcelMappingSaveCode.TOOL_OUTPUTRULES, new ExcelUnmappingRule[0]);
            var macroName = data.Get<string>(ExcelMappingSaveCode.TOOL_MACRO_NAME, string.Empty);

            try
            {
                var tool = new ExcelTool(name,
                    inputRules.Where(x => x != null),
                    outputRangeRules.Where(x => x.data != null || x.type != null).Select(x => new KeyValuePair<ExcelMappedData, Type>(x.data, x.type)),
                    outputRules.Where(x => x != null),
                    macroName);
                return tool;
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load Excel Tool with Name={0}\"\nException: {1}\nStackTrace:\n{2}",
                    name, e.Message, e.StackTrace
                    ));
            }

            return null;
        }

        #endregion

        #region Rules

        internal static void WriteExcelRule(ExcelMappingNode rule, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.EXCEL_RULE);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(ExcelMappingNode));
            writer.Write(ExcelMappingSaveCode.RULE_NODE_NAME, rule.NodeName);
            writer.Write(ExcelMappingSaveCode.RULE_SHEET_NAME, rule.SheetName);
            writer.Write(ExcelMappingSaveCode.RULE_OFFSET_FROM_PARENT_X, (int)rule.OffsetFromParent.X);
            writer.Write(ExcelMappingSaveCode.RULE_OFFSET_FROM_PARENT_Y, (int)rule.OffsetFromParent.Y);
            writer.Write(ExcelMappingSaveCode.RULE_SUBJECT, rule.Subject);

            writer.WriteArray(ExcelMappingSaveCode.RULE_PROPERTIES, rule.Properties, (item, iwriter) => 
            {
                iwriter.Write(ParamStructCommonSaveCode.STRING_VALUE, item.Key);

                var typeNameAttrib = item.Value.GetCustomAttribute<DXFSerializerTypeNameAttribute>();
                if (typeNameAttrib == null)
                    iwriter.Write(ParamStructCommonSaveCode.V10_VALUE, item.Value);
                else
                    iwriter.Write(ParamStructCommonSaveCode.V10_VALUE, typeNameAttrib.Name);
            });

            writer.Write(ExcelMappingSaveCode.RULE_MAPPINGRANGE, rule.RangeOfValuesPerProperty);
            writer.Write(ExcelMappingSaveCode.RULE_ORDER_HORIZONTALLY, rule.OrderHorizontally);

            writer.WriteArray(ExcelMappingSaveCode.RULE_FILTER, rule.PatternsToMatchInProperty, (item, iwriter) =>
            {
                iwriter.Write(ParamStructCommonSaveCode.V10_VALUE, SerializeFilterObject(item.filter));
                iwriter.Write(ParamStructCommonSaveCode.STRING_VALUE, item.propertyName);
            });

            writer.Write(ExcelMappingSaveCode.RULE_OFFSET_BTW_APPLICATIONS_X, (int)rule.OffsetBtwApplications.X);
            writer.Write(ExcelMappingSaveCode.RULE_OFFSET_BTW_APPLICATIONS_Y, (int)rule.OffsetBtwApplications.Y);

            writer.WriteEntitySequence(ExcelMappingSaveCode.RULE_CHILDREN, rule.Children, WriteExcelRule);

            writer.Write(ExcelMappingSaveCode.RULE_PREPEND_CONTENT_TO_CHILDREN, rule.PrependContentToChildren);
            writer.Write(ExcelMappingSaveCode.TRAVERSE_MAX_ELEM, rule.MaxElementsToMap);
            writer.Write(ExcelMappingSaveCode.TRAVERSE_MAX_LEVELS, rule.MaxHierarchyLevelsToTraverse);
            writer.Write(ExcelMappingSaveCode.TRAVERSE_STRATEGY, rule.Strategy);
            writer.Write(ExcelMappingSaveCode.TRAVERSAL_ACTIVATED, rule.NodeIsActive);
            writer.Write(ExcelMappingSaveCode.VERSION, rule.Version);

            writer.EndComplexEntity();
        }

        private static ExcelMappingNode ParseExcelRule(DXFParserResultSet data, DXFParserInfo info)
        {
            var name = data.Get<string>(ExcelMappingSaveCode.RULE_NODE_NAME, string.Empty);
            var sheetName = data.Get<string>(ExcelMappingSaveCode.RULE_SHEET_NAME, string.Empty);

            var parentOffsetX = data.Get<int>(ExcelMappingSaveCode.RULE_OFFSET_FROM_PARENT_X, 0);
            var parentOffsetY = data.Get<int>(ExcelMappingSaveCode.RULE_OFFSET_FROM_PARENT_Y, 0);

            var subject = data.Get<MappingSubject>(ExcelMappingSaveCode.RULE_SUBJECT, MappingSubject.Component);
            var properties = data.Get<KeyValuePair<string, Type>[]>(ExcelMappingSaveCode.RULE_PROPERTIES, new KeyValuePair<string, Type>[0])
                    .ToDictionary(x => x.Key, x => x.Value);

            var range = data.Get<ExcelMappingRange>(ExcelMappingSaveCode.RULE_MAPPINGRANGE, ExcelMappingRange.SingleValue);
            var orderHorizontally = data.Get<bool>(ExcelMappingSaveCode.RULE_ORDER_HORIZONTALLY, true);

            var filter = data.Get<(string, object)[]>(ExcelMappingSaveCode.RULE_FILTER, new (string, object)[0]).ToList();
            var offsetBetweenX = data.Get<int>(ExcelMappingSaveCode.RULE_OFFSET_BTW_APPLICATIONS_X, 0);
            var offsetBetweenY = data.Get<int>(ExcelMappingSaveCode.RULE_OFFSET_BTW_APPLICATIONS_Y, 0);

            var childRules = data.Get<ExcelMappingNode[]>(ExcelMappingSaveCode.RULE_CHILDREN, new ExcelMappingNode[0]);
            
            var prependToChildren = data.Get<bool>(ExcelMappingSaveCode.RULE_PREPEND_CONTENT_TO_CHILDREN, false);
            var traverseMaxElements = data.Get<int>(ExcelMappingSaveCode.TRAVERSE_MAX_ELEM, int.MaxValue);
            var traverseMaxLevels = data.Get<int>(ExcelMappingSaveCode.TRAVERSE_MAX_LEVELS, int.MaxValue);
            var traverseStrategy = data.Get<TraversalStrategy>(ExcelMappingSaveCode.TRAVERSE_STRATEGY, TraversalStrategy.SUBTREE_AND_REFERENCES);
            var isActive = data.Get<bool>(ExcelMappingSaveCode.TRAVERSAL_ACTIVATED, true);
            var version = data.Get<int>(ExcelMappingSaveCode.VERSION, 0);

            if (info.FileVersion < 7)
            {
                //Fix filters
                FixV7FilterNames(filter, subject);

                //Fix properties
                FixV7PropertyNames(properties, subject);
            }

            try
            {
                var rule = new ExcelMappingNode(null, sheetName, new Point(parentOffsetX, parentOffsetY), name,
                    subject, properties, range, orderHorizontally,
                    filter.Where(x => x.Item2 != null), new Point(offsetBetweenX, offsetBetweenY), traverseMaxElements,
                    traverseMaxLevels, traverseStrategy, isActive, version)
                {
                    PrependContentToChildren = prependToChildren
                };

                foreach (var child in childRules)
                    child.Parent = rule;

                return rule;
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load ExcelMappingNode with Name={0}\"\nException: {1}\nStackTrace:\n{2}",
                    name, e.Message, e.StackTrace
                    ));
            }

            return null;
        }

        private static KeyValuePair<string, Type> ParseExcelRuleProperty(DXFParserResultSet data, DXFParserInfo info)
        {
            var key = data.Get<string>(ParamStructCommonSaveCode.STRING_VALUE, "");
            var type_name = data.Get<string>(ParamStructCommonSaveCode.V10_VALUE, "");

            if (!DeserializerTypename.TryGetValue(type_name, out var propertyValue))
            {
                propertyValue = Type.GetType(type_name, false); // search mscorelib
                if (propertyValue == null)
                    propertyValue = Type.GetType(type_name + ", " + System.Reflection.Assembly.GetExecutingAssembly().FullName, false);
            }

            if (propertyValue == null)
            {
                info.Log(String.Format("Failed to find type \"{0}\" while parsing an Excel Mapping Rule Property", type_name));
                return new KeyValuePair<string, Type>(null, null);
            }

            return new KeyValuePair<string, Type>(key, propertyValue);
        }

        private static void FixV7FilterNames(List<(string property, object filter)> filter, MappingSubject subject)
        {
            for (int i = 0; i < filter.Count; ++i)
            {
                if (subject == MappingSubject.Component)
                {
                    if (filter[i].property == "IsBoundInNW")
                        filter[i] = ("IsBoundInNetwork", filter[i].filter);
                    else if (filter[i].property == "GeometryRelationState")
                        filter[i] = ("InstanceState", filter[i].filter);
                    else if (filter[i].property == "LastChangeToSave")
                    {
                        filter.RemoveAt(i);
                        i--;
                    }
                }

                if (filter[i].filter is string strFilter && strFilter.StartsWith("ERROR") && strFilter.EndsWith("!"))
                {
                    filter.RemoveAt(i);
                    i--;
                }
            }
        }

        private static void FixV7PropertyNames(Dictionary<string, Type> properties, MappingSubject subject)
        {
            if (properties.ContainsKey("IsBoundInNW"))
            {
                properties.Remove("IsBoundInNW");
                properties["IsBoundInNetwork"] = typeof(bool);
            }
            if (properties.ContainsKey("InstanceParamValues"))
            {
                properties.Remove("InstanceParamValues");
                properties["InstanceParameterValuesTemporary"] = typeof(SimInstanceParameterCollection);
            }
            if (properties.ContainsKey("InstanceParamValues"))
            {
                properties.Remove("InstanceParameterValuesPersistent");
                properties["InstanceParameterValuesPersistent"] = typeof(SimInstanceParameterCollection);
            }
            if (subject == MappingSubject.Component && properties.ContainsKey("ID"))
            {
                var type = properties["ID"];
                properties.Remove("InstanceParameterValuesPersistent");
                properties[nameof(SimComponent.LocalID)] = type;
            }
            if (properties.ContainsKey("LastChangeToSave"))
            {
                properties.Remove("LastChangeToSave");
            }
        }

        #endregion

        #region Range Unmapping Rules

        internal static void WriteRangeUnmappingRule(KeyValuePair<ExcelMappedData, Type> data, DXFStreamWriter writer)
        {
            var rule = data.Key;

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.EXCEL_DATA_RESULT);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(ExcelMappedData));

            writer.Write(ExcelMappingSaveCode.DATA_MAP_SHEET_NAME, rule.SheetName);
            writer.Write(ExcelMappingSaveCode.DATA_MAP_RANGE_X, (int)rule.Range.X);
            writer.Write(ExcelMappingSaveCode.DATA_MAP_RANGE_Y, (int)rule.Range.Y);
            writer.Write(ExcelMappingSaveCode.DATA_MAP_RANGE_Z, (int)rule.Range.Z);
            writer.Write(ExcelMappingSaveCode.DATA_MAP_RANGE_W, (int)rule.Range.W);

            writer.Write(ExcelMappingSaveCode.DATA_MAP_TYPE, data.Value);

        }

        private static (ExcelMappedData data, Type type) ParseRangeUnmappingRule(DXFParserResultSet data, DXFParserInfo info)
        {
            var sheetName = data.Get<string>(ExcelMappingSaveCode.DATA_MAP_SHEET_NAME, string.Empty);

            int x, y, z, w;
            if (info.FileVersion >= 12)
            {
                x = data.Get<int>(ExcelMappingSaveCode.DATA_MAP_RANGE_X, 0);
                y = data.Get<int>(ExcelMappingSaveCode.DATA_MAP_RANGE_Y, 0);
                z = data.Get<int>(ExcelMappingSaveCode.DATA_MAP_RANGE_Z, 0);
                w = data.Get<int>(ExcelMappingSaveCode.DATA_MAP_RANGE_W, 0);
            }
            else
            {
                x = (int)data.Get<double>(ExcelMappingSaveCode.DATA_MAP_RANGE_X, 0.0);
                y = (int)data.Get<double>(ExcelMappingSaveCode.DATA_MAP_RANGE_Y, 0.0);
                z = (int)data.Get<double>(ExcelMappingSaveCode.DATA_MAP_RANGE_Z, 0.0);
                w = (int)data.Get<double>(ExcelMappingSaveCode.DATA_MAP_RANGE_W, 0.0);
            }

            var type = data.Get<Type>(ExcelMappingSaveCode.DATA_MAP_TYPE, null);

            try
            {
                var mappedData = new ExcelMappedData(sheetName, new Point4D(x, y, z, w));
                return (mappedData, type);
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load Excel Range Unmapping Rule for Sheet={0}\"\nException: {1}\nStackTrace:\n{2}",
                    sheetName, e.Message, e.StackTrace
                    ));
            }

            return (null, null);
        }

        #endregion

        #region Unmapping Rules

        internal static void WriteUnmappingRule(ExcelUnmappingRule rule, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.EXCEL_UNMAPPING);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(ExcelUnmappingRule));

            writer.Write(ExcelMappingSaveCode.RULE_NODE_NAME, rule.NodeName);
            writer.WriteEntitySequence(ExcelMappingSaveCode.UNMAP_DATA,
                new KeyValuePair<ExcelMappedData, Type>[]
                {
                   new KeyValuePair<ExcelMappedData, Type>(rule.ExcelData, rule.DataType)
                }, WriteRangeUnmappingRule);

            writer.Write(ExcelMappingSaveCode.UNMAP_FILTER_SWITCH, rule.UnmapByFilter);
            writer.WriteArray(ExcelMappingSaveCode.UNMAP_FILTER_COMP, rule.PatternsToMatchInPropertyOfComp,
                (item, iwriter) =>
                {
                    iwriter.Write(ParamStructCommonSaveCode.V10_VALUE, SerializeFilterObject(item.filter));
                    iwriter.Write(ParamStructCommonSaveCode.STRING_VALUE, item.propertyName);
                });
            writer.WriteArray(ExcelMappingSaveCode.UNMAP_FILTER_PARAM, rule.PatternsToMatchInPropertyOfParam,
                (item, iwriter) =>
                {
                    iwriter.Write(ParamStructCommonSaveCode.V10_VALUE, SerializeFilterObject(item.filter));
                    iwriter.Write(ParamStructCommonSaveCode.STRING_VALUE, item.propertyName);
                });

            writer.Write<long>(ExcelMappingSaveCode.UNMAP_TARGET_COMP_ID,
                rule.TargetParameter != null ? rule.TargetParameter.Component.Id.LocalId : -1);
            writer.Write(ExcelMappingSaveCode.UNMAP_TARGET_COMP_LOCATION, Guid.Empty);
            writer.Write<long>(ExcelMappingSaveCode.UNMAP_TARGET_PARAM,
                rule.TargetParameter != null ? rule.TargetParameter.Id.LocalId : -1);

            writer.Write(ExcelMappingSaveCode.UNMAP_PARAM_POINTER_X, (int)rule.TargetPointer.X);
            writer.Write(ExcelMappingSaveCode.UNMAP_PARAM_POINTER_Y, (int)rule.TargetPointer.Y);

            writer.EndComplexEntity();

        }

        private static ExcelUnmappingRule ParseUnmappingRule(DXFParserResultSet data, DXFParserInfo info)
        {
            var name = data.Get(ExcelMappingSaveCode.RULE_NODE_NAME, string.Empty);
            var unmapData = data.Get<(ExcelMappedData data, Type type)[]>(ExcelMappingSaveCode.UNMAP_DATA, new (ExcelMappedData, Type)[0]);
            
            var filterSwitch = data.Get<bool>(ExcelMappingSaveCode.UNMAP_FILTER_SWITCH, true);
            var filterComp = data.Get<(string, object)[]>(ExcelMappingSaveCode.UNMAP_FILTER_COMP, new (string, object)[0]).ToList();
            var filterParam = data.Get<(string, object)[]>(ExcelMappingSaveCode.UNMAP_FILTER_PARAM, new (string, object)[0]).ToList();

            var paramId = data.Get<long>(ExcelMappingSaveCode.UNMAP_TARGET_PARAM, -1);
            //Id translation
            paramId = info.TranslateId(typeof(SimParameter), paramId);

            var pointerX = data.Get<int>(ExcelMappingSaveCode.UNMAP_PARAM_POINTER_X, 0);
            var pointerY = data.Get<int>(ExcelMappingSaveCode.UNMAP_PARAM_POINTER_Y, 0);

            try
            {
                var rule = new ExcelUnmappingRule(name, unmapData[0].type, unmapData[0].data, filterSwitch,
                    new ObservableCollection<(string, object)>(filterComp),
                    new ObservableCollection<(string, object)>(filterParam),
                    paramId, new Point(pointerX, pointerY)
                    );
                return rule;
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load Excel Unmapping Rule with Name={0}\"\nException: {1}\nStackTrace:\n{2}",
                    name, e.Message, e.StackTrace
                    ));
            }

            return null;
        }

        #endregion

        #region Filter

        internal static string SerializeFilterObject(object _o)
        {
            if (_o == null) return string.Empty;

            if (_o is string) return _o.ToString() + ParamStructTypes.DELIMITER_WITHIN_ENTRY + typeof(string).ToString();
            if (_o is int) return _o.ToString() + ParamStructTypes.DELIMITER_WITHIN_ENTRY + typeof(int).ToString();
            if (_o is long) return _o.ToString() + ParamStructTypes.DELIMITER_WITHIN_ENTRY + typeof(long).ToString();
            if (_o is double) return DXFDataConverter<double>.P.ToDXFString((double)_o) + ParamStructTypes.DELIMITER_WITHIN_ENTRY + typeof(double).ToString();
            if (_o is bool b) return b.ToString() + ParamStructTypes.DELIMITER_WITHIN_ENTRY + typeof(bool).ToString();

            if (_o is SimCategory)
                return ((SimCategory)_o).ToString() + ParamStructTypes.DELIMITER_WITHIN_ENTRY
                    + typeof(SimCategory).GetCustomAttribute<DXFSerializerTypeNameAttribute>().Name;
            if (_o is SimInfoFlow)
                return ((SimInfoFlow)_o).ToString() + ParamStructTypes.DELIMITER_WITHIN_ENTRY
                    + typeof(SimInfoFlow).GetCustomAttribute<DXFSerializerTypeNameAttribute>().Name;

            StringBuilder sb_o = new StringBuilder();
            if (_o is InstanceStateFilter _of)
            {
                sb_o.Append(((int)ComponentInstanceSaveCode.INSTANCE_TYPE).ToString()); sb_o.Append(ParamStructTypes.DELIMITER_WITHIN_ENTRY);
                sb_o.Append(instanceTypeUntranslation[_of.Type]); sb_o.Append(ParamStructTypes.DELIMITER_WITHIN_ENTRY);

                sb_o.Append(((int)ComponentInstanceSaveCode.STATE_ISREALIZED).ToString()); sb_o.Append(ParamStructTypes.DELIMITER_WITHIN_ENTRY);
                string tmp = (_of.IsRealized) ? "1" : "0";
                sb_o.Append(tmp); sb_o.Append(ParamStructTypes.DELIMITER_WITHIN_ENTRY);

                sb_o.Append(typeof(SimInstanceState).GetCustomAttribute<DXFSerializerTypeNameAttribute>().Name);

                return sb_o.ToString();
            }

            return string.Empty;
        }

        internal static object DeserializeFilterObject(string _record)
        {
            if (string.IsNullOrEmpty(_record)) return null;

            string[] to_parse = _record.Split(new string[] { ParamStructTypes.DELIMITER_WITHIN_ENTRY }, StringSplitOptions.RemoveEmptyEntries);
            if (to_parse == null) return null;
            if (to_parse.Length == 0) return null;

            // reconstruct the type:
            string type_str = to_parse[to_parse.Length - 1];

            if (type_str == "ParameterStructure.Geometry.GeometryRelationState" || type_str == "ParameterStructure.EXCEL.InstanceStateFilter")
                type_str = "SIMULTAN.Excel.InstanceStateFilter";

            Type type = null;

            if (!ExcelDxfIO.DeserializerTypename.TryGetValue(type_str, out type))
            {
                type = Type.GetType(type_str, false);
                if (type == null)
                    type = Type.GetType(type_str + ", " + Assembly.GetExecutingAssembly().FullName);
            }

            // reconstruct the object
            if (type == typeof(string))
            {
                return to_parse[0];
            }
            else if (type == typeof(int))
            {
                int i = 0;
                int.TryParse(to_parse[0], out i);
                return i;
            }
            else if (type == typeof(long))
            {
                long l = 0;
                long.TryParse(to_parse[0], out l);
                return l;
            }
            else if (type == typeof(double))
            {
                return DXFDataConverter<double>.P.FromDXFString(to_parse[0], new DXFParserInfo(Guid.Empty, null) { FileVersion = 0 });
            }
            else if (type == typeof(bool))
            {
                bool b = false;
                bool.TryParse(to_parse[0], out b);
                return b;
            }
            else if (type == typeof(SimCategory))
            {
                return (SimCategory)(Enum.Parse(typeof(SimCategory), to_parse[0]));
            }
            else if (type == typeof(SimInfoFlow))
            {
                return (SimInfoFlow)(Enum.Parse(typeof(SimInfoFlow), to_parse[0]));
            }
            else if ((type == typeof(SimInstanceState) || type == typeof(InstanceStateFilter))
                && to_parse.Length > 4)
            {
                SimInstanceType instanceType;

                bool success1 = instanceTypeTranslation.TryGetValue(to_parse[1], out instanceType);
                if (!success1)
                    success1 = Enum.TryParse<SimInstanceType>(to_parse[1], out instanceType);

                bool success2 = int.TryParse(to_parse[3], out var r2gsr_as_int);
                bool r2gsr = (r2gsr_as_int == 1);
                if (success1 && success2)
                    return new InstanceStateFilter(instanceType, r2gsr);
            }

            return null;
        }

        private static (string property, object filter) ParseFilterObject(DXFParserResultSet data, DXFParserInfo info)
        {
            var property = data.Get<string>(ParamStructCommonSaveCode.STRING_VALUE, "");
            var filter = data.Get<string>(ParamStructCommonSaveCode.V10_VALUE, "");

            var filterParsed = DeserializeFilterObject(filter);

            return (property, filterParsed);
        }

        #endregion

    }
}
