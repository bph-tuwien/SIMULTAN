using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static SIMULTAN.Data.Components.CalculatorMapping;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFMappingTests
    {
        [TestMethod]
        public void WriteMapping()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimComponent component1 = new SimComponent()
            {
                Id = new Data.SimId(guid, 123)
            };
            component1.Parameters.Add(new SimParameter("I1", "", 15.0) 
            {
                Propagation = SimInfoFlow.Input,
                Id = new Data.SimId(guid, 200)
            });
            component1.Parameters.Add(new SimParameter("O1", "", 0.0)
            {
                Propagation = SimInfoFlow.Output,
                Id = new Data.SimId(guid, 201)
            });

            SimComponent component2 = new SimComponent()
            {
                Id = new Data.SimId(guid, 124)
            };
            component2.Parameters.Add(new SimParameter("I2", "", 20.0) 
            {
                Propagation = SimInfoFlow.Input,
                Id = new Data.SimId(guid, 202)
            });
            component2.Parameters.Add(new SimParameter("O2", "", 0.0) 
            { 
                Propagation = SimInfoFlow.Output,
                Id = new Data.SimId(guid, 203)
            });

            var mapping = component2.CreateMappingTo("My Custom Mapping", component1, 
                new MappingParameterTuple[] { new MappingParameterTuple(component2.Parameters[0], component1.Parameters[0]) },
                new MappingParameterTuple[] { new MappingParameterTuple(component2.Parameters[1], component1.Parameters[1]) });

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteMapping(mapping, writer, component2);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMapping, exportedString);
        }

        [TestMethod]
        public void ReadMappingV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculatorMapping mapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_MappingV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                mapping = ComponentDxfIOComponents.CalculatorMappingElement.Parse(reader, info);
            }

            Assert.IsNotNull(mapping);
            Assert.AreEqual("My Custom Mapping", mapping.Name);

            var calculatorId = typeof(CalculatorMapping).GetField("parsingCalculatorID", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(new SimId(guid, 123), calculatorId.GetValue(mapping));

            var inputProp = typeof(CalculatorMapping).GetField("parsingInputMappings", BindingFlags.Instance | BindingFlags.NonPublic);
            var input = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)inputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, input.Count());
            Assert.AreEqual(new SimId(guid, 202), input[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 200), input[0].calculatorParameterId);

            var outputProp = typeof(CalculatorMapping).GetField("parsingOutputMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            var output = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)outputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, output.Count());
            Assert.AreEqual(new SimId(guid, 203), output[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 201), output[0].calculatorParameterId);
        }

        [TestMethod]
        public void ReadMappingV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculatorMapping mapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_MappingV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();

                mapping = ComponentDxfIOComponents.CalculatorMappingElement.Parse(reader, info);
            }

            Assert.IsNotNull(mapping);
            Assert.AreEqual("My Custom Mapping", mapping.Name);

            var calculatorId = typeof(CalculatorMapping).GetField("parsingCalculatorID", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(new SimId(otherGuid, 123), calculatorId.GetValue(mapping));

            var inputProp = typeof(CalculatorMapping).GetField("parsingInputMappings", BindingFlags.Instance | BindingFlags.NonPublic);
            var input = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)inputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, input.Count());
            Assert.AreEqual(new SimId(guid, 202), input[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 200), input[0].calculatorParameterId);

            var outputProp = typeof(CalculatorMapping).GetField("parsingOutputMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            var output = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)outputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, output.Count());
            Assert.AreEqual(new SimId(guid, 203), output[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 201), output[0].calculatorParameterId);
        }

        [TestMethod]
        public void ReadMappingV7()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculatorMapping mapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_MappingV7)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 7;

                reader.Read();

                mapping = ComponentDxfIOComponents.CalculatorMappingElement.Parse(reader, info);
            }

            Assert.IsNotNull(mapping);
            Assert.AreEqual("My Custom Mapping", mapping.Name);

            var calculatorId = typeof(CalculatorMapping).GetField("parsingCalculatorID", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(new SimId(otherGuid, 1), calculatorId.GetValue(mapping));

            var inputProp = typeof(CalculatorMapping).GetField("parsingInputMappings", BindingFlags.Instance | BindingFlags.NonPublic);
            var input = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)inputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, input.Count());
            Assert.AreEqual(new SimId(guid, 202), input[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 200), input[0].calculatorParameterId);

            var outputProp = typeof(CalculatorMapping).GetField("parsingOutputMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            var output = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)outputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, output.Count());
            Assert.AreEqual(new SimId(guid, 203), output[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 201), output[0].calculatorParameterId);
        }

        [TestMethod]
        public void ReadMappingV3()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculatorMapping mapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_MappingV3)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 3;

                reader.Read();

                mapping = ComponentDxfIOComponents.CalculatorMappingElement.Parse(reader, info);
            }

            Assert.IsNotNull(mapping);
            Assert.AreEqual("My Custom Mapping", mapping.Name);

            var calculatorId = typeof(CalculatorMapping).GetField("parsingCalculatorID", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(new SimId(otherGuid, 1), calculatorId.GetValue(mapping));

            var inputProp = typeof(CalculatorMapping).GetField("parsingInputMappings", BindingFlags.Instance | BindingFlags.NonPublic);
            var input = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)inputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, input.Count());
            Assert.AreEqual(new SimId(guid, 1076741824), input[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 1076741825), input[0].calculatorParameterId);

            var outputProp = typeof(CalculatorMapping).GetField("parsingOutputMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            var output = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)outputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, output.Count());
            Assert.AreEqual(new SimId(guid, 1076741826), output[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 1076741827), output[0].calculatorParameterId);
        }

        [TestMethod]
        public void ReadMappingV0()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculatorMapping mapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_MappingV0)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 0;

                reader.Read();

                mapping = ComponentDxfIOComponents.CalculatorMappingElement.Parse(reader, info);
            }

            Assert.IsNotNull(mapping);
            Assert.AreEqual("My Custom Mapping", mapping.Name);

            var calculatorId = typeof(CalculatorMapping).GetField("parsingCalculatorID", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(new SimId(guid, 1), calculatorId.GetValue(mapping));

            var inputProp = typeof(CalculatorMapping).GetField("parsingInputMappings", BindingFlags.Instance | BindingFlags.NonPublic);
            var input = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)inputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, input.Count());
            Assert.AreEqual(new SimId(guid, 1076741824), input[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 1076741825), input[0].calculatorParameterId);

            var outputProp = typeof(CalculatorMapping).GetField("parsingOutputMapping", BindingFlags.Instance | BindingFlags.NonPublic);
            var output = ((IEnumerable<(SimId dataParameterId, SimId calculatorParameterId)>)outputProp.GetValue(mapping)).ToList();
            Assert.AreEqual(1, output.Count());
            Assert.AreEqual(new SimId(guid, 1076741826), output[0].dataParameterId);
            Assert.AreEqual(new SimId(guid, 1076741827), output[0].calculatorParameterId);
        }
    }
}
