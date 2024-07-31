using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.DataMapping;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.ETDXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SIMULTAN.Serializer
{
    /// <summary>
    /// Provides methods for serializing Excel Tools into ETDXF files
    /// </summary>
    public static class ExcelDxfIO
    {
        //Required because data mapping rules can't be specified otherwise
        static ExcelDxfIO()
        {
            ((List<DXFEntryParserElement>)ComponentRuleEntityElement.Entries).Add(
                new DXFEntitySequenceEntryParserElement<ISimDataMappingComponentRuleChild>(DataMappingSaveCode.RULE_CHILDREN,
                    new DXFEntityParserElementBase<ISimDataMappingComponentRuleChild>[]
                    {
                        new DXFEntityCasterElement<ISimDataMappingComponentRuleChild, SimDataMappingRuleComponent>(
                            new DXFComponentRuleDeconstructionElement(ComponentRuleEntityElement)),
                        new DXFEntityCasterElement<ISimDataMappingComponentRuleChild, SimDataMappingRuleParameter>(ParameterRuleEntityElement),
                        new DXFEntityCasterElement<ISimDataMappingComponentRuleChild, SimDataMappingRuleInstance>(InstanceRuleEntityElement),
                        new DXFEntityCasterElement<ISimDataMappingComponentRuleChild, SimDataMappingRuleFace>(FaceRuleEntityElement),
                        new DXFEntityCasterElement<ISimDataMappingComponentRuleChild, SimDataMappingRuleVolume>(VolumeRuleEntityElement),
                    })
                );

            ((List<DXFEntryParserElement>)InstanceRuleEntityElement.Entries).Add(
                new DXFEntitySequenceEntryParserElement<ISimDataMappingInstanceRuleChild>(DataMappingSaveCode.RULE_CHILDREN,
                    new DXFEntityParserElementBase<ISimDataMappingInstanceRuleChild>[]
                    {
                        new DXFEntityCasterElement<ISimDataMappingInstanceRuleChild, SimDataMappingRuleComponent>(
                            new DXFComponentRuleDeconstructionElement(ComponentRuleEntityElement)),
                        new DXFEntityCasterElement<ISimDataMappingInstanceRuleChild, SimDataMappingRuleParameter>(ParameterRuleEntityElement),
                        new DXFEntityCasterElement<ISimDataMappingInstanceRuleChild, SimDataMappingRuleFace>(FaceRuleEntityElement),
                        new DXFEntityCasterElement<ISimDataMappingInstanceRuleChild, SimDataMappingRuleVolume>(VolumeRuleEntityElement),
                    })
                );

            ((List<DXFEntryParserElement>)FaceRuleEntityElement.Entries).Add(
                new DXFEntitySequenceEntryParserElement<ISimDataMappingFaceRuleChild>(DataMappingSaveCode.RULE_CHILDREN,
                    new DXFEntityParserElementBase<ISimDataMappingFaceRuleChild>[]
                    {
                        new DXFEntityCasterElement<ISimDataMappingFaceRuleChild, SimDataMappingRuleComponent>(
                            new DXFComponentRuleDeconstructionElement(ComponentRuleEntityElement)),
                        new DXFEntityCasterElement<ISimDataMappingFaceRuleChild, SimDataMappingRuleInstance>(InstanceRuleEntityElement),
                        new DXFEntityCasterElement<ISimDataMappingFaceRuleChild, SimDataMappingRuleFace>(FaceRuleEntityElement),
                        new DXFEntityCasterElement<ISimDataMappingFaceRuleChild, SimDataMappingRuleVolume>(VolumeRuleEntityElement),
                    })
                );

            ((List<DXFEntryParserElement>)VolumeRuleEntityElement.Entries).Add(
                new DXFEntitySequenceEntryParserElement<ISimDataMappingVolumeRuleChild>(DataMappingSaveCode.RULE_CHILDREN,
                    new DXFEntityParserElementBase<ISimDataMappingVolumeRuleChild>[]
                    {
                        new DXFEntityCasterElement<ISimDataMappingVolumeRuleChild, SimDataMappingRuleComponent>(
                            new DXFComponentRuleDeconstructionElement(ComponentRuleEntityElement)),
                        new DXFEntityCasterElement<ISimDataMappingVolumeRuleChild, SimDataMappingRuleInstance>(InstanceRuleEntityElement),
                        new DXFEntityCasterElement<ISimDataMappingVolumeRuleChild, SimDataMappingRuleFace>(FaceRuleEntityElement),
                    })
                );
        }


        #region Syntax DataMapping Rule

        private static DXFEntryParserElement[] commonRuleProperties = new DXFEntryParserElement[]
        {
            new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_SHEETNAME),
            new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_NAME),
            new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_OFFSETPARENT_X),
            new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_OFFSETPARENT_Y),
            new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_OFFSETCONSECUTIVE_X),
            new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_OFFSETCONSECUTIVE_Y),
            new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_MAXMATCHES),
            new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_MAXDEPTH),
            new DXFSingleEntryParserElement<SimDataMappingDirection>(DataMappingSaveCode.RULE_DIRECTION),
            new DXFSingleEntryParserElement<SimDataMappingReferencePoint>(DataMappingSaveCode.RULE_REFERENCEPOSITIONPARENT),
            new DXFSingleEntryParserElement<SimDataMappingReferencePoint>(DataMappingSaveCode.RULE_REFERENCEPOSITIONCONSECUTIVE) { MinVersion = 22 },
        };

        internal static DXFEntityParserElementBase<SimDataMappingRuleParameter> ParameterRuleEntityElement =
            new DXFComplexEntityParserElement<SimDataMappingRuleParameter>(
                 new DXFEntityParserElement<SimDataMappingRuleParameter>(ParamStructTypes.DATAMAPPING_RULE_PARAMETER,
                     (data, info) => ParseDataMappingParameterRule(data, info),
                     new DXFEntryParserElement[]
                     {
                         new DXFSingleEntryParserElement<SimDataMappingParameterRange>(DataMappingSaveCode.RULE_MAPPINGRANGE),
                         new DXFArrayEntryParserElement<SimDataMappingParameterMappingProperties>(
                            DataMappingSaveCode.RULE_PROPERTIES, ParamStructCommonSaveCode.X_VALUE),
                         new DXFStructArrayEntryParserElement<SimDataMappingFilterParameter>(
                            DataMappingSaveCode.RULE_FILTER, ParseDataMappingParameterFilter,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<SimDataMappingParameterFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY),
                                new DXFSingleEntryParserElement<SimDataMappingFilterType>(DataMappingSaveCode.RULE_FILTER_TYPE),
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE2) { IsOptional = true },
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE),
                            }),
                     }.Concat(commonRuleProperties).ToList()
                ));

        internal static DXFEntityParserElementBase<SimDataMappingRuleFace> FaceRuleEntityElement =
            new DXFComplexEntityParserElement<SimDataMappingRuleFace>(
                 new DXFEntityParserElement<SimDataMappingRuleFace>(ParamStructTypes.DATAMAPPING_RULE_FACE,
                     (data, info) => ParseDataMappingFaceRule(data, info),
                     new DXFEntryParserElement[]
                     {
                         new DXFArrayEntryParserElement<SimDataMappingFaceMappingProperties>(
                            DataMappingSaveCode.RULE_PROPERTIES, ParamStructCommonSaveCode.X_VALUE),
                         new DXFStructArrayEntryParserElement<SimDataMappingFilterFace>(
                            DataMappingSaveCode.RULE_FILTER, ParseDataMappingFaceFilter,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<SimDataMappingFaceFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY),
                                new DXFSingleEntryParserElement<SimDataMappingFilterType>(DataMappingSaveCode.RULE_FILTER_TYPE),
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE2) { IsOptional = true },
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE),
                            }),
                     }.Concat(commonRuleProperties).ToList()
                ));

        internal static DXFEntityParserElementBase<SimDataMappingRuleInstance> InstanceRuleEntityElement =
            new DXFComplexEntityParserElement<SimDataMappingRuleInstance>(
                 new DXFEntityParserElement<SimDataMappingRuleInstance>(ParamStructTypes.DATAMAPPING_RULE_INSTANCE,
                     (data, info) => ParseDataMappingInstanceRule(data, info),
                     new DXFEntryParserElement[]
                     {
                         new DXFArrayEntryParserElement<SimDataMappingInstanceMappingProperties>(
                            DataMappingSaveCode.RULE_PROPERTIES, ParamStructCommonSaveCode.X_VALUE),
                         new DXFStructArrayEntryParserElement<SimDataMappingFilterInstance>(
                            DataMappingSaveCode.RULE_FILTER, ParseDataMappingInstanceFilter,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<SimDataMappingInstanceFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY),
                                new DXFSingleEntryParserElement<SimDataMappingFilterType>(DataMappingSaveCode.RULE_FILTER_TYPE),
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE2) { IsOptional = true },
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE),
                            }),
                     }.Concat(commonRuleProperties).ToList()
                ));

        internal static DXFEntityParserElementBase<SimDataMappingRuleVolume> VolumeRuleEntityElement =
            new DXFComplexEntityParserElement<SimDataMappingRuleVolume>(
                new DXFEntityParserElement<SimDataMappingRuleVolume>(ParamStructTypes.DATAMAPPING_RULE_VOLUME,
                    (data, info) => ParseDataMappingVolumeRule(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<SimDataMappingRuleTraversalStrategy>(DataMappingSaveCode.RULE_TRAVERSE_STRATEGY),
                        new DXFArrayEntryParserElement<SimDataMappingVolumeMappingProperties>(
                            DataMappingSaveCode.RULE_PROPERTIES, ParamStructCommonSaveCode.X_VALUE),
                        new DXFStructArrayEntryParserElement<SimDataMappingFilterVolume>(
                            DataMappingSaveCode.RULE_FILTER, ParseDataMappingVolumeFilter,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<SimDataMappingVolumeFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY),
                                new DXFSingleEntryParserElement<SimDataMappingFilterType>(DataMappingSaveCode.RULE_FILTER_TYPE),
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE2) { IsOptional = true },
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE),
                            }),

                    }.Concat(commonRuleProperties).ToList()
                ));

        internal static DXFEntityParserElementBase<(SimDataMappingRuleComponent, SimComponent[])> ComponentRuleEntityElement =
            new DXFComplexEntityParserElement<(SimDataMappingRuleComponent, SimComponent[])>(
                new DXFEntityParserElement<(SimDataMappingRuleComponent, SimComponent[])>(ParamStructTypes.DATAMAPPING_RULE_COMPONENT,
                    (data, info) => ParseDataMappingComponentRule(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<SimDataMappingRuleTraversalStrategy>(DataMappingSaveCode.RULE_TRAVERSE_STRATEGY),
                        new DXFArrayEntryParserElement<SimDataMappingComponentMappingProperties>(
                            DataMappingSaveCode.RULE_PROPERTIES, ParamStructCommonSaveCode.X_VALUE),
                        new DXFStructArrayEntryParserElement<SimDataMappingFilterComponent>(
                            DataMappingSaveCode.RULE_FILTER, ParseDataMappingComponentFilter,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<SimDataMappingComponentFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY),
                                new DXFSingleEntryParserElement<SimDataMappingFilterType>(DataMappingSaveCode.RULE_FILTER_TYPE),
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE2) { IsOptional = true },
                                new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_FILTER_VALUE),
                            }),
                        new DXFStructArrayEntryParserElement<SimComponent>(
                            DataMappingSaveCode.RULE_MAPPED_COMPONENTS, ParseMappedComponentId,
                            new DXFEntryParserElement[]
                            {
                                new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.X_VALUE),
                                new DXFSingleEntryParserElement<Guid>(ParamStructCommonSaveCode.Y_VALUE),
                            }),

                    }.Concat(commonRuleProperties).ToList()
                ));

        #endregion

        #region Syntax DataMapping Read Rule

        internal static DXFEntityParserElementBase<SimDataMappingReadRule> DataMappingReadRuleEntityElement =
            new DXFComplexEntityParserElement<SimDataMappingReadRule>(
                new DXFEntityParserElement<SimDataMappingReadRule>(ParamStructTypes.DATAMAPPING_RULE_READ,
                    (data, info) => ParseDataMappingReadRule(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<Guid>(DataMappingSaveCode.RULE_PARAMETER_GLOBALID),
                        new DXFSingleEntryParserElement<long>(DataMappingSaveCode.RULE_PARAMETER_LOCALID),
                        new DXFSingleEntryParserElement<string>(DataMappingSaveCode.RULE_SHEETNAME),
                        new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_RANGE_COLUMNSTART),
                        new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_RANGE_ROWSTART),
                        new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_RANGE_COLUMNCOUNT),
                        new DXFSingleEntryParserElement<int>(DataMappingSaveCode.RULE_RANGE_ROWCOUNT),
                    })
                );

        #endregion

        #region Syntax DataMapping Tool

        internal static DXFEntityParserElementBase<SimDataMappingTool> DataMappingToolEntityElement =
            new DXFComplexEntityParserElement<SimDataMappingTool>(
                new DXFEntityParserElement<SimDataMappingTool>(ParamStructTypes.DATAMAPPING_TOOL,
                    (data, info) => ParseDataMappingTool(data, info),
                    new DXFEntryParserElement[]
                    {
                        new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                        new DXFSingleEntryParserElement<string>(DataMappingSaveCode.TOOL_NAME),
                        new DXFSingleEntryParserElement<string>(DataMappingSaveCode.TOOL_MACRO_NAME),
                        new DXFEntitySequenceEntryParserElement<(SimDataMappingRuleComponent, SimComponent[])>(
                            DataMappingSaveCode.TOOL_MAPPINGRULES, ComponentRuleEntityElement),
                        new DXFEntitySequenceEntryParserElement<SimDataMappingReadRule>(
                            DataMappingSaveCode.TOOL_OUTPUTRULES, DataMappingReadRuleEntityElement),
                    })
                );

        #endregion

        #region Syntax DataMapping Section

        internal static DXFSectionParserElement<SimDataMappingTool> DataMappingToolSectionEntityElement =
            new DXFSectionParserElement<SimDataMappingTool>(ParamStructTypes.DATAMAPPINGTOOL_SECTION,
                new DXFEntityParserElementBase<SimDataMappingTool>[]
                {
                    DataMappingToolEntityElement
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

            writer.StartSection(ParamStructTypes.DATAMAPPINGTOOL_SECTION, -1);

            foreach (var tool in projectData.DataMappingTools)
                WriteDataMappingTool(tool, writer);

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
                    Read(reader, parserInfo, false);
                }
            }
        }
        /// <summary>
        /// Reads a ETDXF file and loads the Excel Tools into a project. Does not load parameter references.
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <param name="parserInfo">Additional info for parsing. Also includes the project data</param>
        public static void ReadLibrary(FileInfo file, DXFParserInfo parserInfo)
        {
            using (FileStream stream = file.OpenRead())
            {
                if (stream.Length == 0)
                    return;

                using (DXFStreamReader reader = new DXFStreamReader(stream))
                {
                    Read(reader, parserInfo, true);
                }
            }
        }

        internal static void Read(DXFStreamReader reader, DXFParserInfo parserInfo, bool isLibrary)
        {
            //Version section
            if (CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }

            //Data section
            if (parserInfo.FileVersion >= 19)
            {
                var tools = DataMappingToolSectionEntityElement.Parse(reader, parserInfo);

                parserInfo.ProjectData.DataMappingTools.StartLoading();

                foreach (var tool in tools.Where(x => x != null))
                {
                    if (isLibrary)
                    {
                        //Reset Id and parameter bindings
                        tool.Id = SimId.Empty;

                        foreach (var readRule in tool.ReadRules)
                            readRule.Parameter = null;
                    }

                    parserInfo.ProjectData.DataMappingTools.Add(tool);
                }

                parserInfo.ProjectData.DataMappingTools.EndLoading();

                //EOF
                EOFParserElement.Element.Parse(reader);
            }

            //Old excel mapping tools are no longer loaded!

            parserInfo.FinishLog();
        }


        #region DataMapping Tool

        private static SimDataMappingTool ParseDataMappingTool(DXFParserResultSet data, DXFParserInfo info)
        {
            var name = data.Get<string>(DataMappingSaveCode.TOOL_NAME, string.Empty);
            var macro = data.Get<string>(DataMappingSaveCode.TOOL_MACRO_NAME, string.Empty);
            var id = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, -1L);

            var tool = new SimDataMappingTool(name)
            {
                MacroName = macro,
                Id = new SimId(info.GlobalId, id),
            };

            var topLevelRules = data.Get<(SimDataMappingRuleComponent rule, SimComponent[] mappedComponents)[]>(DataMappingSaveCode.TOOL_MAPPINGRULES,
                    new (SimDataMappingRuleComponent rule, SimComponent[] mappedComponents)[0]).Where(x => x.rule != null);

            foreach (var ruleData in topLevelRules)
            {
                tool.Rules.Add(ruleData.rule);

                if (ruleData.mappedComponents != null)
                    tool.Rules.AddMappings(ruleData.rule, ruleData.mappedComponents);
            }

            var readRules = data.Get<SimDataMappingReadRule[]>(DataMappingSaveCode.TOOL_OUTPUTRULES,
                new SimDataMappingReadRule[0]).Where(x => x != null);
            foreach (var rrule in readRules)
            {
                tool.ReadRules.Add(rrule);
            }

            return tool;
        }

        internal static void WriteDataMappingTool(SimDataMappingTool tool, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.DATAMAPPING_TOOL);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimDataMappingTool));
            writer.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, tool.Id.LocalId);

            writer.Write(DataMappingSaveCode.TOOL_NAME, tool.Name);
            writer.Write(DataMappingSaveCode.TOOL_MACRO_NAME, tool.MacroName);

            writer.WriteEntitySequence(DataMappingSaveCode.TOOL_MAPPINGRULES, tool.Rules, WriteDataMappingRule);

            writer.WriteEntitySequence(DataMappingSaveCode.TOOL_OUTPUTRULES, tool.ReadRules, WriteReadRule);

            writer.EndComplexEntity();
        }

        #endregion

        #region DataMapping Rules

        internal static (SimDataMappingRuleComponent rule, SimComponent[] mappedComponents)
            ParseDataMappingComponentRule(DXFParserResultSet data, DXFParserInfo info)
        {
            string sheetName = data.Get<string>(DataMappingSaveCode.RULE_SHEETNAME, string.Empty);
            var rule = new SimDataMappingRuleComponent(sheetName);

            ParseRuleCommons(rule, data, info);

            rule.TraversalStrategy = data.Get<SimDataMappingRuleTraversalStrategy>(DataMappingSaveCode.RULE_TRAVERSE_STRATEGY,
                SimDataMappingRuleTraversalStrategy.Subtree);

            rule.Properties.AddRange(
                data.Get<SimDataMappingComponentMappingProperties[]>(DataMappingSaveCode.RULE_PROPERTIES,
                    new SimDataMappingComponentMappingProperties[0])
                );

            rule.Filter.AddRange(
                data.Get<SimDataMappingFilterComponent[]>(DataMappingSaveCode.RULE_FILTER, new SimDataMappingFilterComponent[0])
                .Where(x => x != null)
                );

            //Children
            rule.Rules.AddRange(
                data.Get<ISimDataMappingComponentRuleChild[]>(DataMappingSaveCode.RULE_CHILDREN, new ISimDataMappingComponentRuleChild[0])
                .Where(x => x != null)
                );

            //Mapped Components
            var components = data.Get<SimComponent[]>(DataMappingSaveCode.RULE_MAPPED_COMPONENTS, null);

            return (rule, components);
        }
        internal static SimDataMappingRuleParameter ParseDataMappingParameterRule(DXFParserResultSet data, DXFParserInfo info)
        {
            string sheetName = data.Get<string>(DataMappingSaveCode.RULE_SHEETNAME, string.Empty);
            var rule = new SimDataMappingRuleParameter(sheetName);
            var range = data.Get<SimDataMappingParameterRange>(DataMappingSaveCode.RULE_MAPPINGRANGE, SimDataMappingParameterRange.SingleValue);

            ParseRuleCommons(rule, data, info);
            rule.ParameterRange = range;

            rule.Properties.AddRange(
                data.Get<SimDataMappingParameterMappingProperties[]>(DataMappingSaveCode.RULE_PROPERTIES,
                    new SimDataMappingParameterMappingProperties[0])
                );

            rule.Filter.AddRange(
                data.Get<SimDataMappingFilterParameter[]>(DataMappingSaveCode.RULE_FILTER, new SimDataMappingFilterParameter[0])
                .Where(x => x != null)
                );

            return rule;
        }
        internal static SimDataMappingRuleInstance ParseDataMappingInstanceRule(DXFParserResultSet data, DXFParserInfo info)
        {
            string sheetName = data.Get<string>(DataMappingSaveCode.RULE_SHEETNAME, string.Empty);
            var rule = new SimDataMappingRuleInstance(sheetName);

            ParseRuleCommons(rule, data, info);

            rule.Properties.AddRange(
                data.Get<SimDataMappingInstanceMappingProperties[]>(DataMappingSaveCode.RULE_PROPERTIES,
                    new SimDataMappingInstanceMappingProperties[0])
                );

            rule.Filter.AddRange(
                data.Get<SimDataMappingFilterInstance[]>(DataMappingSaveCode.RULE_FILTER, new SimDataMappingFilterInstance[0])
                .Where(x => x != null)
                );

            //Children
            rule.Rules.AddRange(
                data.Get<ISimDataMappingInstanceRuleChild[]>(DataMappingSaveCode.RULE_CHILDREN, new ISimDataMappingInstanceRuleChild[0])
                .Where(x => x != null)
                );

            return rule;
        }
        internal static SimDataMappingRuleFace ParseDataMappingFaceRule(DXFParserResultSet data, DXFParserInfo info)
        {
            string sheetName = data.Get<string>(DataMappingSaveCode.RULE_SHEETNAME, string.Empty);
            var rule = new SimDataMappingRuleFace(sheetName);

            ParseRuleCommons(rule, data, info);

            rule.Properties.AddRange(
                data.Get<SimDataMappingFaceMappingProperties[]>(DataMappingSaveCode.RULE_PROPERTIES,
                    new SimDataMappingFaceMappingProperties[0])
                );

            rule.Filter.AddRange(
                data.Get<SimDataMappingFilterFace[]>(DataMappingSaveCode.RULE_FILTER, new SimDataMappingFilterFace[0])
                .Where(x => x != null)
                );

            //Children
            rule.Rules.AddRange(
                data.Get<ISimDataMappingFaceRuleChild[]>(DataMappingSaveCode.RULE_CHILDREN, new ISimDataMappingFaceRuleChild[0])
                .Where(x => x != null)
                );

            return rule;
        }
        internal static SimDataMappingRuleVolume ParseDataMappingVolumeRule(DXFParserResultSet data, DXFParserInfo info)
        {
            string sheetName = data.Get<string>(DataMappingSaveCode.RULE_SHEETNAME, string.Empty);
            var rule = new SimDataMappingRuleVolume(sheetName);

            ParseRuleCommons(rule, data, info);

            rule.Properties.AddRange(
                data.Get<SimDataMappingVolumeMappingProperties[]>(DataMappingSaveCode.RULE_PROPERTIES,
                    new SimDataMappingVolumeMappingProperties[0])
                );

            rule.Filter.AddRange(
                data.Get<SimDataMappingFilterVolume[]>(DataMappingSaveCode.RULE_FILTER, new SimDataMappingFilterVolume[0])
                .Where(x => x != null)
                );

            //Children
            rule.Rules.AddRange(
                data.Get<ISimDataMappingVolumeRuleChild[]>(DataMappingSaveCode.RULE_CHILDREN, new ISimDataMappingVolumeRuleChild[0])
                .Where(x => x != null)
                );

            return rule;
        }

        private static void ParseRuleCommons(ISimDataMappingRuleBase rule, DXFParserResultSet data, DXFParserInfo info)
        {
            rule.Name = data.Get<string>(DataMappingSaveCode.RULE_NAME, string.Empty);

            int offsetX = data.Get<int>(DataMappingSaveCode.RULE_OFFSETPARENT_X, 0);
            int offsetY = data.Get<int>(DataMappingSaveCode.RULE_OFFSETPARENT_Y, 1);
            rule.OffsetParent = new RowColumnIndex(offsetY, offsetX);

            int offsetConsecutiveX = data.Get<int>(DataMappingSaveCode.RULE_OFFSETCONSECUTIVE_X, 0);
            int offsetConsecutiveY = data.Get<int>(DataMappingSaveCode.RULE_OFFSETCONSECUTIVE_Y, 0);
            rule.OffsetConsecutive = new RowColumnIndex(offsetConsecutiveY, offsetConsecutiveX);

            rule.MaxMatches = data.Get<int>(DataMappingSaveCode.RULE_MAXMATCHES, 1);
            rule.MaxDepth = data.Get<int>(DataMappingSaveCode.RULE_MAXDEPTH, 1);
            rule.MappingDirection = data.Get<SimDataMappingDirection>(DataMappingSaveCode.RULE_DIRECTION,
                SimDataMappingDirection.Horizontal);
            rule.ReferencePointParent = data.Get<SimDataMappingReferencePoint>(DataMappingSaveCode.RULE_REFERENCEPOSITIONPARENT,
                SimDataMappingReferencePoint.BottomLeft);
            rule.ReferencePointConsecutive = data.Get<SimDataMappingReferencePoint>(DataMappingSaveCode.RULE_REFERENCEPOSITIONCONSECUTIVE,
                SimDataMappingReferencePoint.BottomLeft);
        }

        private static SimComponent ParseMappedComponentId(DXFParserResultSet data, DXFParserInfo info)
        {
            var componentId = data.GetSimId(ParamStructCommonSaveCode.Y_VALUE, ParamStructCommonSaveCode.X_VALUE, info.GlobalId);

            //Find component
            return info.ProjectData.IdGenerator.GetById<SimComponent>(componentId);
        }

        private static Dictionary<Type, string> typeToEntityName = new Dictionary<Type, string>
        {
            { typeof(SimDataMappingRuleComponent), ParamStructTypes.DATAMAPPING_RULE_COMPONENT },
            { typeof(SimDataMappingRuleParameter), ParamStructTypes.DATAMAPPING_RULE_PARAMETER },
            { typeof(SimDataMappingRuleInstance), ParamStructTypes.DATAMAPPING_RULE_INSTANCE },
            { typeof(SimDataMappingRuleFace), ParamStructTypes.DATAMAPPING_RULE_FACE },
            { typeof(SimDataMappingRuleVolume), ParamStructTypes.DATAMAPPING_RULE_VOLUME },
        };
        internal static void WriteDataMappingRule(ISimDataMappingRuleBase rule, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, typeToEntityName[rule.GetType()]);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, rule.GetType());

            writer.Write(DataMappingSaveCode.RULE_NAME, rule.Name);
            writer.Write(DataMappingSaveCode.RULE_SHEETNAME, rule.SheetName);

            writer.Write(DataMappingSaveCode.RULE_OFFSETPARENT_X, rule.OffsetParent.Column);
            writer.Write(DataMappingSaveCode.RULE_OFFSETPARENT_Y, rule.OffsetParent.Row);
            writer.Write(DataMappingSaveCode.RULE_OFFSETCONSECUTIVE_X, rule.OffsetConsecutive.Column);
            writer.Write(DataMappingSaveCode.RULE_OFFSETCONSECUTIVE_Y, rule.OffsetConsecutive.Row);

            writer.Write(DataMappingSaveCode.RULE_MAXMATCHES, rule.MaxMatches);
            writer.Write(DataMappingSaveCode.RULE_MAXDEPTH, rule.MaxDepth);

            writer.Write(DataMappingSaveCode.RULE_DIRECTION, rule.MappingDirection);
            writer.Write(DataMappingSaveCode.RULE_REFERENCEPOSITIONPARENT, rule.ReferencePointParent);
            writer.Write(DataMappingSaveCode.RULE_REFERENCEPOSITIONCONSECUTIVE, rule.ReferencePointConsecutive);

            //Type specific properties
            switch (rule)
            {
                case SimDataMappingRuleComponent compRule:
                    WriteComponentMappingRule(compRule, writer);
                    break;
                case SimDataMappingRuleParameter paramRule:
                    WriteParameterMappingRule(paramRule, writer);
                    break;
                case SimDataMappingRuleInstance instRule:
                    WriteInstanceMappingRule(instRule, writer);
                    break;
                case SimDataMappingRuleFace faceRule:
                    WriteFaceMappingRule(faceRule, writer);
                    break;
                case SimDataMappingRuleVolume volumeRule:
                    WriteVolumeMappingRule(volumeRule, writer);
                    break;
            }

            writer.EndComplexEntity();
        }

        private static void WriteComponentMappingRule(SimDataMappingRuleComponent rule, DXFStreamWriter writer)
        {
            writer.Write(DataMappingSaveCode.RULE_TRAVERSE_STRATEGY, rule.TraversalStrategy);

            writer.WriteArray(DataMappingSaveCode.RULE_PROPERTIES, rule.Properties, (item, iwriter) =>
            {
                iwriter.Write(ParamStructCommonSaveCode.X_VALUE, item);
            });

            writer.WriteArray(DataMappingSaveCode.RULE_FILTER, rule.Filter,
                (item, iwriter) =>
                {
                    writer.Write(DataMappingSaveCode.RULE_FILTER_PROPERTY, item.Property);
                    WriteFilterValue(item.Value, iwriter);
                });

            //Needs to be serialized here since Rules don't have ids. Otherwise the mappings could be written somewhere else
            IEnumerable<SimComponent> mappedComponents;
            if (rule.Tool != null)
                mappedComponents = rule.Tool.Rules.GetMappings(rule);
            else
                mappedComponents = Enumerable.Empty<SimComponent>();

            writer.WriteArray(DataMappingSaveCode.RULE_MAPPED_COMPONENTS, mappedComponents, (mc, iwriter) =>
                {
                    iwriter.Write(ParamStructCommonSaveCode.X_VALUE, mc.Id.LocalId);
                    iwriter.WriteGlobalId(ParamStructCommonSaveCode.Y_VALUE, mc.Id.GlobalId, mc.Factory.CalledFromLocation.GlobalID);
                });

            writer.WriteEntitySequence(DataMappingSaveCode.RULE_CHILDREN, rule.Rules, WriteDataMappingRule);
        }
        private static void WriteParameterMappingRule(SimDataMappingRuleParameter rule, DXFStreamWriter writer)
        {
            writer.Write(DataMappingSaveCode.RULE_MAPPINGRANGE, rule.ParameterRange);

            writer.WriteArray(DataMappingSaveCode.RULE_PROPERTIES, rule.Properties, (item, iwriter) =>
            {
                iwriter.Write(ParamStructCommonSaveCode.X_VALUE, item);
            });

            writer.WriteArray(DataMappingSaveCode.RULE_FILTER, rule.Filter, (item, iwriter) =>
            {
                writer.Write(DataMappingSaveCode.RULE_FILTER_PROPERTY, item.Property);
                WriteFilterValue(item.Value, iwriter);
            });
        }
        private static void WriteInstanceMappingRule(SimDataMappingRuleInstance rule, DXFStreamWriter writer)
        {
            writer.WriteArray(DataMappingSaveCode.RULE_PROPERTIES, rule.Properties, (item, iwriter) =>
            {
                iwriter.Write(ParamStructCommonSaveCode.X_VALUE, item);
            });

            writer.WriteArray(DataMappingSaveCode.RULE_FILTER, rule.Filter,
                (item, iwriter) =>
                {
                    writer.Write(DataMappingSaveCode.RULE_FILTER_PROPERTY, item.Property);
                    WriteFilterValue(item.Value, iwriter);
                });

            writer.WriteEntitySequence(DataMappingSaveCode.RULE_CHILDREN, rule.Rules, WriteDataMappingRule);
        }
        private static void WriteFaceMappingRule(SimDataMappingRuleFace rule, DXFStreamWriter writer)
        {
            writer.WriteArray(DataMappingSaveCode.RULE_PROPERTIES, rule.Properties, (item, iwriter) =>
            {
                iwriter.Write(ParamStructCommonSaveCode.X_VALUE, item);
            });

            writer.WriteArray(DataMappingSaveCode.RULE_FILTER, rule.Filter,
                (item, iwriter) =>
                {
                    writer.Write(DataMappingSaveCode.RULE_FILTER_PROPERTY, item.Property);
                    WriteFilterValue(item.Value, iwriter);
                });

            writer.WriteEntitySequence(DataMappingSaveCode.RULE_CHILDREN, rule.Rules, WriteDataMappingRule);
        }
        private static void WriteVolumeMappingRule(SimDataMappingRuleVolume rule, DXFStreamWriter writer)
        {
            writer.WriteArray(DataMappingSaveCode.RULE_PROPERTIES, rule.Properties, (item, iwriter) =>
            {
                iwriter.Write(ParamStructCommonSaveCode.X_VALUE, item);
            });

            writer.WriteArray(DataMappingSaveCode.RULE_FILTER, rule.Filter,
                (item, iwriter) =>
                {
                    writer.Write(DataMappingSaveCode.RULE_FILTER_PROPERTY, item.Property);
                    WriteFilterValue(item.Value, iwriter);
                });

            writer.WriteEntitySequence(DataMappingSaveCode.RULE_CHILDREN, rule.Rules, WriteDataMappingRule);
        }

        #endregion

        #region DataMapping Read Rules

        internal static void WriteReadRule(SimDataMappingReadRule rule, DXFStreamWriter writer)
        {
            writer.StartComplexEntity();

            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.DATAMAPPING_RULE_READ);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimDataMappingReadRule));

            var parameterId = SimId.Empty;
            if (rule.Parameter != null)
                parameterId = rule.Parameter.Id;
            writer.WriteGlobalId(DataMappingSaveCode.RULE_PARAMETER_GLOBALID, parameterId.GlobalId, rule.Tool.Factory.CalledFromLocation.GlobalID);
            writer.Write(DataMappingSaveCode.RULE_PARAMETER_LOCALID, parameterId.LocalId);

            writer.Write(DataMappingSaveCode.RULE_SHEETNAME, rule.SheetName);

            writer.Write(DataMappingSaveCode.RULE_RANGE_COLUMNSTART, rule.Range.ColumnStart);
            writer.Write(DataMappingSaveCode.RULE_RANGE_ROWSTART, rule.Range.RowStart);
            writer.Write(DataMappingSaveCode.RULE_RANGE_COLUMNCOUNT, rule.Range.ColumnCount);
            writer.Write(DataMappingSaveCode.RULE_RANGE_ROWCOUNT, rule.Range.RowCount);

            writer.EndComplexEntity();
        }

        private static SimDataMappingReadRule ParseDataMappingReadRule(DXFParserResultSet data, DXFParserInfo info)
        {
            var paramId = data.GetSimId(DataMappingSaveCode.RULE_PARAMETER_GLOBALID, DataMappingSaveCode.RULE_PARAMETER_LOCALID, info.GlobalId);
            var parameter = info.ProjectData.IdGenerator.GetById<SimBaseParameter>(paramId);

            var sheetName = data.Get<string>(DataMappingSaveCode.RULE_SHEETNAME, "");

            var columnStart = data.Get<int>(DataMappingSaveCode.RULE_RANGE_COLUMNSTART, 0);
            var rowStart = data.Get<int>(DataMappingSaveCode.RULE_RANGE_ROWSTART, 0);
            var columnCount = data.Get<int>(DataMappingSaveCode.RULE_RANGE_COLUMNCOUNT, 0);
            var rowCount = data.Get<int>(DataMappingSaveCode.RULE_RANGE_ROWCOUNT, 0);

            try
            {
                return new SimDataMappingReadRule()
                {
                    Parameter = parameter,
                    SheetName = sheetName,
                    Range = new RowColumnRange(rowStart, columnStart, rowCount, columnCount),
                };
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Data Mapping Filter

        internal static SimDataMappingFilterParameter ParseDataMappingParameterFilter(DXFParserResultSet data, DXFParserInfo info)
        {
            try
            {
                SimDataMappingFilterParameter filter = new SimDataMappingFilterParameter(
                    data.Get<SimDataMappingParameterFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY, SimDataMappingParameterFilterProperties.Name),
                    ParseFilterValue(data, info)
                    );
                return filter;
            }
            catch (Exception) { return null; }
        }
        internal static SimDataMappingFilterComponent ParseDataMappingComponentFilter(DXFParserResultSet data, DXFParserInfo info)
        {
            try
            {
                SimDataMappingFilterComponent filter = new SimDataMappingFilterComponent(
                    data.Get<SimDataMappingComponentFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY, SimDataMappingComponentFilterProperties.Name),
                    ParseFilterValue(data, info)
                    );
                return filter;
            }
            catch (Exception) { return null; }
        }
        internal static SimDataMappingFilterInstance ParseDataMappingInstanceFilter(DXFParserResultSet data, DXFParserInfo info)
        {
            try
            {
                var filter = new SimDataMappingFilterInstance(
                    data.Get<SimDataMappingInstanceFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY, SimDataMappingInstanceFilterProperties.Name),
                    ParseFilterValue(data, info)
                    );
                return filter;
            }
            catch (Exception) { return null; }
        }
        internal static SimDataMappingFilterFace ParseDataMappingFaceFilter(DXFParserResultSet data, DXFParserInfo info)
        {
            try
            {
                var filter = new SimDataMappingFilterFace(
                    data.Get<SimDataMappingFaceFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY, SimDataMappingFaceFilterProperties.Name),
                    ParseFilterValue(data, info)
                    );
                return filter;
            }
            catch (Exception) { return null; }
        }
        internal static SimDataMappingFilterVolume ParseDataMappingVolumeFilter(DXFParserResultSet data, DXFParserInfo info)
        {
            try
            {
                var filter = new SimDataMappingFilterVolume(
                    data.Get<SimDataMappingVolumeFilterProperties>(DataMappingSaveCode.RULE_FILTER_PROPERTY, SimDataMappingVolumeFilterProperties.Name),
                    ParseFilterValue(data, info)
                    );
                return filter;
            }
            catch (Exception) { return null; }
        }

        internal static object ParseFilterValue(DXFParserResultSet data, DXFParserInfo info)
        {
            SimDataMappingFilterType type = data.Get<SimDataMappingFilterType>(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.String);

            switch (type)
            {
                case SimDataMappingFilterType.String:
                    return data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, string.Empty);
                case SimDataMappingFilterType.Regex:
                    return new Regex(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, ".*"));
                case SimDataMappingFilterType.TaxonomyEntry:
                    {
                        var id = DXFDataConverter<long>.P.FromDXFString(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, ""), info);
                        var taxEntry = info.ProjectData.IdGenerator.GetById<SimTaxonomyEntry>(
                            new SimId(info.GlobalId, id)
                            );
                        return new SimTaxonomyEntryReference(taxEntry);
                    }
                case SimDataMappingFilterType.Slot:
                    {
                        var id = DXFDataConverter<long>.P.FromDXFString(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, ""), info);
                        var taxEntry = info.ProjectData.IdGenerator.GetById<SimTaxonomyEntry>(
                            new SimId(info.GlobalId, id)
                            );
                        var ext = data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE2, string.Empty);

                        return new SimSlot(taxEntry, ext);
                    }
                case SimDataMappingFilterType.InstanceType:
                    return DXFDataConverter<SimInstanceType>.P.FromDXFString(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, "0"), info);
                case SimDataMappingFilterType.Boolean:
                    return DXFDataConverter<bool>.P.FromDXFString(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, "0"), info);
                case SimDataMappingFilterType.InfoFlow:
                    return DXFDataConverter<SimInfoFlow>.P.FromDXFString(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, "0"), info);
                case SimDataMappingFilterType.Category:
                    return DXFDataConverter<SimCategory>.P.FromDXFString(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, "0"), info);
                case SimDataMappingFilterType.Integer:
                    return DXFDataConverter<int>.P.FromDXFString(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, "0"), info);
                case SimDataMappingFilterType.WallType:
                    return DXFDataConverter<SimDataMappingFaceType>.P.FromDXFString(data.Get<string>(DataMappingSaveCode.RULE_FILTER_VALUE, "0"), info);
                default:
                    throw new NotSupportedException("Unsupported filter type");
            }
        }
        internal static void WriteFilterValue(object value, DXFStreamWriter writer)
        {
            if (value == null)
            {
                writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.Null);
                writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, "");
                return;
            }
            switch (value)
            {
                case string s:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.String);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, s);
                    break;
                case Regex reg:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.Regex);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, reg.ToString());
                    break;
                case SimTaxonomyEntryReference tax:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.TaxonomyEntry);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, tax.Target.LocalID);
                    break;
                case SimSlot slot:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.Slot);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE2, slot.SlotExtension);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, slot.SlotBase.Target.LocalID);
                    break;
                case SimInstanceType instType:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.InstanceType);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, instType);
                    break;
                case bool b:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.Boolean);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, b);
                    break;
                case SimInfoFlow flow:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.InfoFlow);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, flow);
                    break;
                case SimCategory category:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.Category);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, category);
                    break;
                case int i:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.Integer);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, i);
                    break;
                case SimDataMappingFaceType wallType:
                    writer.Write(DataMappingSaveCode.RULE_FILTER_TYPE, SimDataMappingFilterType.WallType);
                    writer.Write(DataMappingSaveCode.RULE_FILTER_VALUE, wallType);
                    break;
                default:
                    throw new NotSupportedException("Unsupported filter value");
            }
        }

        #endregion
    }
}
