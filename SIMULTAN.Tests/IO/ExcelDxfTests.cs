using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Excel;
using SIMULTAN.Projects;
using SIMULTAN.Serializer;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ExcelDxfTests
    {
        private List<ExcelMappingNode> CreateInputRules()
        {
            ExcelMappingNode rootRule = new ExcelMappingNode(null, "Sheet A", new System.Windows.Point(1, 2), "Root Rule",
                MappingSubject.Component, new Dictionary<string, Type> { { "IsBoundInNetwork", typeof(bool) } },
                ExcelMappingRange.SingleValue, true,
                new List<(string, object)> {
                    ("Name", "NameFilterText"),
                    ("InstanceState", new InstanceStateFilter(SimInstanceType.GeometricSurface, true))
                },
                new System.Windows.Point(3, 4), 20, 4, TraversalStrategy.SUBTREE_AND_REFERENCES, true, 1
                )
            {
                PrependContentToChildren = false,
            };

            ExcelMappingNode childRule1 = new ExcelMappingNode(null, "Sheet A", new System.Windows.Point(1, 2), "Child Rule 1",
                MappingSubject.Parameter, new Dictionary<string, Type> { { "Name", typeof(string) } },
                ExcelMappingRange.SingleValue, true,
                new List<(string, object)> { },
                new System.Windows.Point(3, 4), 20, 4, TraversalStrategy.SUBTREE_AND_REFERENCES, true, 1
                )
            {
                PrependContentToChildren = true,
            };
            childRule1.Parent = rootRule;
            ExcelMappingNode childRule2 = new ExcelMappingNode(null, "Sheet A", new System.Windows.Point(1, 2), "Child Rule 2",
                MappingSubject.Parameter, new Dictionary<string, Type> { { "Name", typeof(string) } },
                ExcelMappingRange.SingleValue, true,
                new List<(string, object)> { },
                new System.Windows.Point(3, 4), 20, 4, TraversalStrategy.SUBTREE_AND_REFERENCES, true, 1
                )
            {
                PrependContentToChildren = true,
            };
            childRule2.Parent = rootRule;

            return new List<ExcelMappingNode> { rootRule };
        }

        private List<KeyValuePair<ExcelMappedData, Type>> CreateRangeOutputRules()
        {
            return new List<KeyValuePair<ExcelMappedData, Type>>
            {
                new KeyValuePair<ExcelMappedData, Type>(new ExcelMappedData("Sheet A", new Point4D(1, 2, 3, 4)), typeof(double))
            };
        }

        private List<ExcelUnmappingRule> CreateUnmappingRules()
        {
            SimComponent component = new SimComponent()
            {
                Id = new Data.SimId(Guid.Empty, 123)
            };
            SimParameter parameter = new SimParameter("Param", "Unit", 1234.56)
            {
                Id = new Data.SimId(Guid.Empty, 124)
            };
            component.Parameters.Add(parameter);

            var unmappingRule = new ExcelUnmappingRule("Unmap Rule A", typeof(double),
                new ExcelMappedData("Sheet X", new Point4D(1, 2, 3, 4)),
                false,
                new ObservableCollection<(string, object)>
                {
                    ("Name", "NameFilterText"),
                    ("InstanceState", new InstanceStateFilter(SimInstanceType.GeometricSurface, true))
                },
                new ObservableCollection<(string, object)>
                {
                    ("Name", "ParamName")
                },
                124,
                new Point(17, 18));
            unmappingRule.TargetParameter = parameter;

            return new List<ExcelUnmappingRule>
            {
                unmappingRule
            };
        }

        private void CheckInputRules(IEnumerable<ExcelMappingNode> rules)
        {
            Assert.AreEqual(2, rules.Count());
            var rule = rules.Skip(1).First(); //First one should be the empty rule

            //Root Component
            Assert.AreEqual("Root Rule", rule.NodeName);
            Assert.AreEqual("Sheet A", rule.SheetName);
            Assert.AreEqual(new Point(1, 2), rule.OffsetFromParent);
            Assert.AreEqual(MappingSubject.Component, rule.Subject);
            Assert.AreEqual(1, rule.Properties.Count);
            Assert.IsTrue(rule.Properties.ContainsKey("IsBoundInNetwork"));
            Assert.AreEqual(typeof(bool), rule.Properties["IsBoundInNetwork"]);
            Assert.AreEqual(ExcelMappingRange.SingleValue, rule.RangeOfValuesPerProperty);
            Assert.AreEqual(true, rule.OrderHorizontally);
            Assert.AreEqual(2, rule.PatternsToMatchInProperty.Count);
            Assert.AreEqual("Name", rule.PatternsToMatchInProperty[0].propertyName);
            Assert.AreEqual("NameFilterText", rule.PatternsToMatchInProperty[0].filter);
            Assert.AreEqual("InstanceState", rule.PatternsToMatchInProperty[1].propertyName);
            Assert.IsTrue(rule.PatternsToMatchInProperty[1].filter is InstanceStateFilter);
            Assert.AreEqual(SimInstanceType.GeometricSurface, ((InstanceStateFilter)rule.PatternsToMatchInProperty[1].filter).Type);
            Assert.AreEqual(true, ((InstanceStateFilter)rule.PatternsToMatchInProperty[1].filter).IsRealized);
            Assert.AreEqual(new Point(3, 4), rule.OffsetBtwApplications);
            Assert.AreEqual(20, rule.MaxElementsToMap);
            Assert.AreEqual(4, rule.MaxHierarchyLevelsToTraverse);
            Assert.AreEqual(TraversalStrategy.SUBTREE_AND_REFERENCES, rule.Strategy);
            Assert.AreEqual(true, rule.NodeIsActive);
            Assert.AreEqual(false, rule.PrependContentToChildren);
            Assert.AreEqual(null, rule.Parent);
            Assert.AreEqual(1, rule.Version);

            Assert.AreEqual(2, rule.Children.Count);

            //Child Rule 1
            var child1 = rule.Children.ElementAt(0);
            Assert.AreEqual("Child Rule 1", child1.NodeName);
            Assert.AreEqual("Sheet A", child1.SheetName);
            Assert.AreEqual(new Point(1, 2), child1.OffsetFromParent);
            Assert.AreEqual(MappingSubject.Parameter, child1.Subject);
            Assert.AreEqual(1, child1.Properties.Count);
            Assert.IsTrue(child1.Properties.ContainsKey("Name"));
            Assert.AreEqual(typeof(string), child1.Properties["Name"]);
            Assert.AreEqual(ExcelMappingRange.SingleValue, child1.RangeOfValuesPerProperty);
            Assert.AreEqual(true, child1.OrderHorizontally);
            Assert.AreEqual(0, child1.PatternsToMatchInProperty.Count);
            Assert.AreEqual(new Point(3, 4), child1.OffsetBtwApplications);
            Assert.AreEqual(20, child1.MaxElementsToMap);
            Assert.AreEqual(4, child1.MaxHierarchyLevelsToTraverse);
            Assert.AreEqual(TraversalStrategy.SUBTREE_AND_REFERENCES, child1.Strategy);
            Assert.AreEqual(true, child1.NodeIsActive);
            Assert.AreEqual(true, child1.PrependContentToChildren);
            Assert.AreEqual(rule, child1.Parent);
            Assert.AreEqual(1, child1.Version);
            Assert.AreEqual(0, child1.Children.Count);

            var child2 = rule.Children.ElementAt(1);
            Assert.AreEqual("Child Rule 2", child2.NodeName);
            Assert.AreEqual("Sheet A", child2.SheetName);
            Assert.AreEqual(new Point(1, 2), child2.OffsetFromParent);
            Assert.AreEqual(MappingSubject.Parameter, child2.Subject);
            Assert.AreEqual(1, child2.Properties.Count);
            Assert.IsTrue(child2.Properties.ContainsKey("Name"));
            Assert.AreEqual(typeof(string), child2.Properties["Name"]);
            Assert.AreEqual(ExcelMappingRange.SingleValue, child2.RangeOfValuesPerProperty);
            Assert.AreEqual(true, child2.OrderHorizontally);
            Assert.AreEqual(0, child2.PatternsToMatchInProperty.Count);
            Assert.AreEqual(new Point(3, 4), child2.OffsetBtwApplications);
            Assert.AreEqual(20, child2.MaxElementsToMap);
            Assert.AreEqual(4, child2.MaxHierarchyLevelsToTraverse);
            Assert.AreEqual(TraversalStrategy.SUBTREE_AND_REFERENCES, child2.Strategy);
            Assert.AreEqual(true, child2.NodeIsActive);
            Assert.AreEqual(true, child2.PrependContentToChildren);
            Assert.AreEqual(rule, child2.Parent);
            Assert.AreEqual(1, child2.Version);
            Assert.AreEqual(0, child2.Children.Count);
        }

        private void CheckOutputRangeRules(IEnumerable<KeyValuePair<ExcelMappedData, Type>> rules)
        {
            Assert.AreEqual(1, rules.Count());

            var rule = rules.First();

            //Root Component
            Assert.IsNotNull(rule.Key);
            Assert.IsNotNull(rule.Value);

            Assert.AreEqual("Sheet A", rule.Key.SheetName);
            Assert.AreEqual(1.0, rule.Key.Range.X);
            Assert.AreEqual(2.0, rule.Key.Range.Y);
            Assert.AreEqual(3.0, rule.Key.Range.Z);
            Assert.AreEqual(4.0, rule.Key.Range.W);
            Assert.AreEqual(typeof(double), rule.Value);
        }

        private void CheckOutputRules(IEnumerable<ExcelUnmappingRule> rules, bool checkParamId = true)
        {
            Assert.AreEqual(1, rules.Count());
            var rule = rules.First();

            Assert.AreEqual("Unmap Rule A", rule.NodeName);
            Assert.AreEqual(typeof(double), rule.DataType);
            Assert.AreEqual("Sheet X", rule.ExcelData.SheetName);
            Assert.AreEqual(new Point4D(1, 2, 3, 4), rule.ExcelData.Range);
            Assert.AreEqual(false, rule.UnmapByFilter);

            Assert.AreEqual(2, rule.PatternsToMatchInPropertyOfComp.Count);
            Assert.AreEqual("Name", rule.PatternsToMatchInPropertyOfComp[0].propertyName);
            Assert.AreEqual("NameFilterText", rule.PatternsToMatchInPropertyOfComp[0].filter);
            Assert.AreEqual("InstanceState", rule.PatternsToMatchInPropertyOfComp[1].propertyName);
            Assert.IsTrue(rule.PatternsToMatchInPropertyOfComp[1].filter is InstanceStateFilter);
            Assert.AreEqual(SimInstanceType.GeometricSurface, ((InstanceStateFilter)rule.PatternsToMatchInPropertyOfComp[1].filter).Type);
            Assert.AreEqual(true, ((InstanceStateFilter)rule.PatternsToMatchInPropertyOfComp[1].filter).IsRealized);

            Assert.AreEqual(1, rule.PatternsToMatchInPropertyOfParam.Count);
            Assert.AreEqual("Name", rule.PatternsToMatchInPropertyOfParam[0].propertyName);
            Assert.AreEqual("ParamName", rule.PatternsToMatchInPropertyOfParam[0].filter);

            if (checkParamId)
            {
                Assert.AreEqual(124, rule.TargetParameterID);
                Assert.AreEqual(null, rule.TargetParameter);
            }

            Assert.AreEqual(17.0, rule.TargetPointer.X);
            Assert.AreEqual(18.0, rule.TargetPointer.Y);
        }

        #region File

        [TestMethod]
        public void WriteExcelFile()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelTool tool = new ExcelTool("Tool B", CreateInputRules(), CreateRangeOutputRules(),
                CreateUnmappingRules(), "Macro Name");
            projectData.ExcelToolMappingManager.RegisteredTools.Add(tool);


            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.Write(writer, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_Write, exportedString);
        }

        [TestMethod]
        public void ReadExcelFileV12()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            var comp = new SimComponent();
            var param = new SimParameter("A", "unit", 3.5) { Id = new SimId(location, 124) };
            comp.Parameters.Add(param);

            projectData.Components.StartLoading();
            projectData.Components.Add(comp);
            projectData.Components.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ExcelDxfIO.Read(reader, info);
            }

            Assert.AreEqual(1, projectData.ExcelToolMappingManager.RegisteredTools.Count);
            var tool = projectData.ExcelToolMappingManager.RegisteredTools[0];

            Assert.AreEqual("Tool B", tool.Name);
            Assert.AreEqual("Macro Name", tool.MacroName);

            CheckInputRules(tool.InputRules);
            CheckOutputRangeRules(tool.OutputRangeRules);
            CheckOutputRules(tool.OutputRules, false);

            //Check parameter
            Assert.AreEqual(param, tool.OutputRules[0].TargetParameter);
        }

        [TestMethod]
        public void ReadExcelFileV11()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            var comp = new SimComponent();
            var param = new SimParameter("A", "unit", 3.5) { Id = new SimId(location, 124) };
            comp.Parameters.Add(param);

            projectData.Components.StartLoading();
            projectData.Components.Add(comp);
            projectData.Components.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;
                ExcelDxfIO.Read(reader, info);
            }

            Assert.AreEqual(1, projectData.ExcelToolMappingManager.RegisteredTools.Count);
            var tool = projectData.ExcelToolMappingManager.RegisteredTools[0];

            Assert.AreEqual("Tool B", tool.Name);
            Assert.AreEqual("Macro Name", tool.MacroName);

            CheckInputRules(tool.InputRules);
            CheckOutputRangeRules(tool.OutputRangeRules);
            CheckOutputRules(tool.OutputRules, false);

            //Check parameter
            Assert.AreEqual(param, tool.OutputRules[0].TargetParameter);
        }

        [TestMethod]
        public void ReadExcelFileV6()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            var comp = new SimComponent();
            var param = new SimParameter("A", "unit", 3.5) { Id = new SimId(location, 124) };
            comp.Parameters.Add(param);

            projectData.Components.StartLoading();
            projectData.Components.Add(comp);
            projectData.Components.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV6)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 6;
                ExcelDxfIO.Read(reader, info);
            }

            Assert.AreEqual(1, projectData.ExcelToolMappingManager.RegisteredTools.Count);
            var tool = projectData.ExcelToolMappingManager.RegisteredTools[0];

            Assert.AreEqual("Tool B", tool.Name);
            Assert.AreEqual("Macro Name", tool.MacroName);

            CheckInputRules(tool.InputRules);
            CheckOutputRangeRules(tool.OutputRangeRules);
            CheckOutputRules(tool.OutputRules, false);

            //Check parameter
            Assert.AreEqual(param, tool.OutputRules[0].TargetParameter);
        }

        [TestMethod]
        public void ReadExcelFileV4()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            var comp = new SimComponent();
            var param = new SimParameter("A", "unit", 3.5) { Id = new SimId(location, 1076741824) };
            comp.Parameters.Add(param);

            projectData.Components.StartLoading();
            projectData.Components.Add(comp);
            projectData.Components.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV4)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 4;
                ExcelDxfIO.Read(reader, info);
            }

            Assert.AreEqual(1, projectData.ExcelToolMappingManager.RegisteredTools.Count);
            var tool = projectData.ExcelToolMappingManager.RegisteredTools[0];

            Assert.AreEqual("Tool B", tool.Name);
            Assert.AreEqual("Macro Name", tool.MacroName);

            CheckInputRules(tool.InputRules);
            CheckOutputRangeRules(tool.OutputRangeRules);
            CheckOutputRules(tool.OutputRules, false);

            //Check parameter
            Assert.AreEqual(param, tool.OutputRules[0].TargetParameter);
        }

        #endregion

        #region Tool

        [TestMethod]
        public void WriteExcelTool()
        {
            ExcelTool tool = new ExcelTool("Tool B", CreateInputRules(), CreateRangeOutputRules(),
                CreateUnmappingRules(), "Macro Name");

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteExcelTool(tool, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_ETDXF_WriteTool, exportedString);
        }

        [TestMethod]
        public void ReadExcelToolV12()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelTool tool = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadToolV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                tool = ExcelDxfIO.ExcelToolEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(tool);
            Assert.AreEqual("Tool B", tool.Name);
            Assert.AreEqual("Macro Name", tool.MacroName);

            CheckInputRules(tool.InputRules);
            CheckOutputRangeRules(tool.OutputRangeRules);
            CheckOutputRules(tool.OutputRules);
        }

        [TestMethod]
        public void ReadExcelToolV11()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelTool tool = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadToolV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                tool = ExcelDxfIO.ExcelToolEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(tool);
            Assert.AreEqual("Tool B", tool.Name);
            Assert.AreEqual("Macro Name", tool.MacroName);

            CheckInputRules(tool.InputRules);
            CheckOutputRangeRules(tool.OutputRangeRules);
            CheckOutputRules(tool.OutputRules);
        }

        [TestMethod]
        public void ReadExcelToolV6()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelTool tool = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadToolV6)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 6;

                reader.Read();

                tool = ExcelDxfIO.ExcelToolEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(tool);
            Assert.AreEqual("Tool B", tool.Name);
            Assert.AreEqual("Macro Name", tool.MacroName);

            CheckInputRules(tool.InputRules);
            CheckOutputRangeRules(tool.OutputRangeRules);
            CheckOutputRules(tool.OutputRules);
        }

        [TestMethod]
        public void ReadExcelToolV4()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelTool tool = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadToolV4)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 4;

                reader.Read();

                tool = ExcelDxfIO.ExcelToolEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(tool);
            Assert.AreEqual("Tool B", tool.Name);
            Assert.AreEqual("Macro Name", tool.MacroName);

            CheckInputRules(tool.InputRules);
            CheckOutputRangeRules(tool.OutputRangeRules);

            Assert.AreEqual(1, tool.OutputRules.Count);
            var outputRule = tool.OutputRules.First();
            Assert.AreEqual("Unmap Rule A", outputRule.NodeName);
            Assert.AreEqual(typeof(double), outputRule.DataType);
            Assert.AreEqual("Sheet X", outputRule.ExcelData.SheetName);
            Assert.AreEqual(new Point4D(1, 2, 3, 4), outputRule.ExcelData.Range);
            Assert.AreEqual(false, outputRule.UnmapByFilter);

            Assert.AreEqual(2, outputRule.PatternsToMatchInPropertyOfComp.Count);
            Assert.AreEqual("Name", outputRule.PatternsToMatchInPropertyOfComp[0].propertyName);
            Assert.AreEqual("NameFilterText", outputRule.PatternsToMatchInPropertyOfComp[0].filter);
            Assert.AreEqual("InstanceState", outputRule.PatternsToMatchInPropertyOfComp[1].propertyName);
            Assert.IsTrue(outputRule.PatternsToMatchInPropertyOfComp[1].filter is InstanceStateFilter);
            Assert.AreEqual(SimInstanceType.GeometricSurface, ((InstanceStateFilter)outputRule.PatternsToMatchInPropertyOfComp[1].filter).Type);
            Assert.AreEqual(true, ((InstanceStateFilter)outputRule.PatternsToMatchInPropertyOfComp[1].filter).IsRealized);

            Assert.AreEqual(1, outputRule.PatternsToMatchInPropertyOfParam.Count);
            Assert.AreEqual("Name", outputRule.PatternsToMatchInPropertyOfParam[0].propertyName);
            Assert.AreEqual("ParamName", outputRule.PatternsToMatchInPropertyOfParam[0].filter);

            Assert.AreEqual(1076741824, outputRule.TargetParameterID);
            Assert.AreEqual(null, outputRule.TargetParameter);

            Assert.AreEqual(17.0, outputRule.TargetPointer.X);
            Assert.AreEqual(18.0, outputRule.TargetPointer.Y);
        }

        #endregion

        #region Rules

        [TestMethod]
        public void WriteExcelRule()
        {
            var rootRule = CreateInputRules()[0];

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteExcelRule(rootRule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_ETDXF_WriteRule, exportedString);
        }

        [TestMethod]
        public void ReadExcelRuleV12()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelMappingNode rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadRuleV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                rule = ExcelDxfIO.ExcelRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);

            CheckInputRules(new ExcelMappingNode[] { null, rule }); //Null because tools have an empty first rule
        }

        [TestMethod]
        public void ReadExcelRuleV11()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelMappingNode rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadRuleV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                rule = ExcelDxfIO.ExcelRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);

            CheckInputRules(new ExcelMappingNode[] { null, rule }); //Null because tools have an empty first rule
        }

        [TestMethod]
        public void ReadExcelRuleV6()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelMappingNode rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadRuleV6)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 6;

                reader.Read();

                rule = ExcelDxfIO.ExcelRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);

            CheckInputRules(new ExcelMappingNode[] { null, rule }); //Null because tools have an empty first rule
        }

        #endregion

        #region Range Unmapping Rules

        [TestMethod]
        public void WriteExcelRangeUnmappingRule()
        {
            var mappedData = CreateRangeOutputRules()[0];

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteRangeUnmappingRule(mappedData, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_ETDXF_WriteRangeUnmappingRule, exportedString);
        }

        [TestMethod]
        public void ReadExcelRangeUnmappingRuleV12()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            (ExcelMappedData data, Type type) rule = (null, null);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadRangeUnmappingRuleV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                rule = ExcelDxfIO.ExcelRangeUnmappingRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);

            CheckOutputRangeRules(new KeyValuePair<ExcelMappedData, Type>[] { new KeyValuePair<ExcelMappedData, Type>(rule.data, rule.type) });
        }

        [TestMethod]
        public void ReadExcelRangeUnmappingRuleV11()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            (ExcelMappedData data, Type type) rule = (null, null);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadRangeUnmappingRuleV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                rule = ExcelDxfIO.ExcelRangeUnmappingRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);

            CheckOutputRangeRules(new KeyValuePair<ExcelMappedData, Type>[] { new KeyValuePair<ExcelMappedData, Type>(rule.data, rule.type) });
        }

        #endregion

        #region Unmapping Rules

        [TestMethod]
        public void WriteExcelUnmappingRule()
        {
            var unmappingRule = CreateUnmappingRules()[0];

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteUnmappingRule(unmappingRule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_ETDXF_WriteUnmappingRule, exportedString);
        }

        [TestMethod]
        public void ReadExcelUnmappingRuleV12()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelUnmappingRule rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadUnmappingRuleV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                rule = ExcelDxfIO.ExcelUnmappingRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);

            CheckOutputRules(new ExcelUnmappingRule[] { rule });
        }

        [TestMethod]
        public void ReadExcelUnmappingRuleV11()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelUnmappingRule rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadUnmappingRuleV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                rule = ExcelDxfIO.ExcelUnmappingRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);

            CheckOutputRules(new ExcelUnmappingRule[] { rule });
        }

        [TestMethod]
        public void ReadExcelUnmappingRuleV4()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            ExcelUnmappingRule rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadUnmappingRuleV4)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 4;

                reader.Read();

                rule = ExcelDxfIO.ExcelUnmappingRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);

            Assert.AreEqual("Unmap Rule A", rule.NodeName);
            Assert.AreEqual(typeof(double), rule.DataType);
            Assert.AreEqual("Sheet X", rule.ExcelData.SheetName);
            Assert.AreEqual(new Point4D(1, 2, 3, 4), rule.ExcelData.Range);
            Assert.AreEqual(false, rule.UnmapByFilter);

            Assert.AreEqual(2, rule.PatternsToMatchInPropertyOfComp.Count);
            Assert.AreEqual("Name", rule.PatternsToMatchInPropertyOfComp[0].propertyName);
            Assert.AreEqual("NameFilterText", rule.PatternsToMatchInPropertyOfComp[0].filter);
            Assert.AreEqual("InstanceState", rule.PatternsToMatchInPropertyOfComp[1].propertyName);
            Assert.IsTrue(rule.PatternsToMatchInPropertyOfComp[1].filter is InstanceStateFilter);
            Assert.AreEqual(SimInstanceType.GeometricSurface, ((InstanceStateFilter)rule.PatternsToMatchInPropertyOfComp[1].filter).Type);
            Assert.AreEqual(true, ((InstanceStateFilter)rule.PatternsToMatchInPropertyOfComp[1].filter).IsRealized);

            Assert.AreEqual(1, rule.PatternsToMatchInPropertyOfParam.Count);
            Assert.AreEqual("Name", rule.PatternsToMatchInPropertyOfParam[0].propertyName);
            Assert.AreEqual("ParamName", rule.PatternsToMatchInPropertyOfParam[0].filter);

            Assert.AreEqual(1076741824, rule.TargetParameterID);
            Assert.AreEqual(null, rule.TargetParameter);

            Assert.AreEqual(17.0, rule.TargetPointer.X);
            Assert.AreEqual(18.0, rule.TargetPointer.Y);
        }

        #endregion

        #region Filter

        [TestMethod]
        public void StringFilterTest()
        {
            var filterText = ExcelDxfIO.SerializeFilterObject("Text");
            Assert.AreEqual("Text_|_System.String", filterText);
        }

        [TestMethod]
        public void Int32FilterTest()
        {
            var filterText = ExcelDxfIO.SerializeFilterObject((int)64);
            Assert.AreEqual("64_|_System.Int32", filterText);
        }

        [TestMethod]
        public void Int64FilterTest()
        {
            var filterText = ExcelDxfIO.SerializeFilterObject((long)64);
            Assert.AreEqual("64_|_System.Int64", filterText);
        }

        [TestMethod]
        public void DoubleFilterTest()
        {
            var filterText = ExcelDxfIO.SerializeFilterObject((double)17.3);
            Assert.AreEqual("17.30000000_|_System.Double", filterText);
        }

        [TestMethod]
        public void BoolFilterTest()
        {
            var filterText = ExcelDxfIO.SerializeFilterObject(true);
            Assert.AreEqual("True_|_System.Boolean", filterText);

            filterText = ExcelDxfIO.SerializeFilterObject(false);
            Assert.AreEqual("False_|_System.Boolean", filterText);
        }

        [TestMethod]
        public void SimCategoryFilterTest()
        {
            var filterText = ExcelDxfIO.SerializeFilterObject(SimCategory.Air);
            Assert.AreEqual("Air_|_ParameterStructure.Component.Category", filterText);
        }

        [TestMethod]
        public void SimInfoFlowFilterTest()
        {
            var filterText = ExcelDxfIO.SerializeFilterObject(SimInfoFlow.Mixed);
            Assert.AreEqual("Mixed_|_ParameterStructure.Component.InfoFlow", filterText);
        }

        [TestMethod]
        public void InstanceStateFilterTest()
        {
            var filterText = ExcelDxfIO.SerializeFilterObject(new InstanceStateFilter(SimInstanceType.GeometricSurface, true));
            Assert.AreEqual("1602_|_DESCRIBES_2DorLESS_|_1609_|_1_|_ParameterStructure.Instances.InstanceState", filterText);
        }

        #endregion
    }
}
