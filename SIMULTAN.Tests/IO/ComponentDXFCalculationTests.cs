using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static SIMULTAN.Data.Components.CalculationParameterMetaData;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFCalculationTests
    {
        [TestMethod]
        public void WriteCalculation()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimDoubleParameter xparam = new SimDoubleParameter("XParam", "", 17.4)
            {
                Propagation = SimInfoFlow.Input,
                Id = new SimId(guid, 6667)
            };
            SimDoubleParameter yparam = new SimDoubleParameter("YParam", "", 17.4)
            {
                Propagation = SimInfoFlow.Input,
                Id = new SimId(otherGuid, 6668)
            };

            SimDoubleParameter r1param = new SimDoubleParameter("r1", "", 17.4)
            {
                Propagation = SimInfoFlow.Output,
                Id = new SimId(guid, 6669)
            };
            SimDoubleParameter r2param = new SimDoubleParameter("r2", "", 17.4)
            {
                Propagation = SimInfoFlow.Output,
                Id = new SimId(otherGuid, 6670)
            };

            Dictionary<string, SimDoubleParameter> inputParameters = new Dictionary<string, SimDoubleParameter>
            {
                { "x", xparam },
                { "y", yparam },
            };
            Dictionary<string, SimDoubleParameter> returnParameters = new Dictionary<string, SimDoubleParameter>
            {
                { "r1", r1param },
                { "r2", r2param },
                { "r3", null }
            };

            SimCalculation calculation = new SimCalculation("x+y*z", "My Calculation", inputParameters, returnParameters)
            {
                Id = new SimId(guid, 34),
            };

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteCalculation(calculation, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteCalculation, exportedString);
        }

        [TestMethod]
        public void WriteVectorCalculation()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimDoubleParameter xparam = new SimDoubleParameter("XParam", "", 17.4)
            {
                Propagation = SimInfoFlow.Input,
                Id = new SimId(guid, 6667)
            };
            SimDoubleParameter yparam = new SimDoubleParameter("YParam", "", 17.4)
            {
                Propagation = SimInfoFlow.Input,
                Id = new SimId(otherGuid, 6668)
            };

            SimDoubleParameter r1param = new SimDoubleParameter("r1", "", 17.4)
            {
                Propagation = SimInfoFlow.Output,
                Id = new SimId(guid, 6669)
            };
            SimDoubleParameter r2param = new SimDoubleParameter("r2", "", 17.4)
            {
                Propagation = SimInfoFlow.Output,
                Id = new SimId(otherGuid, 6670)
            };

            Dictionary<string, SimDoubleParameter> inputParameters = new Dictionary<string, SimDoubleParameter>
            {
                { "x", xparam },
                { "y", yparam },
            };
            Dictionary<string, SimDoubleParameter> returnParameters = new Dictionary<string, SimDoubleParameter>
            {
                { "r1", r1param },
                { "r2", r2param },
                { "r3", null }
            };

            SimCalculation calculation = new SimCalculation("x+y*z", "My Calculation", inputParameters, returnParameters)
            {
                Id = new SimId(guid, 34),
                IsMultiValueCalculation = true,
                IterationCount = 24,
                OverrideResult = false,
                ResultAggregation = SimResultAggregationMethod.Separate
            };
            ((SimMultiValueExpressionBinary)calculation.MultiValueCalculation).Operation
                = MultiValueCalculationBinaryOperation.MATRIX_SUM;

            var metaX = calculation.InputParams.GetMetaData("x");
            metaX.Range = new SIMULTAN.Utils.RowColumnRange(1, 3, 2, 1);
            metaX.IsRandomized = true;
            metaX.RandomizeIsClamping = true;
            metaX.RandomizeRelativeMean = 23.5;
            metaX.RandomizeDeviation = 17.3;
            metaX.RandomizeClampDeviation = 9.1;
            metaX.RandomizeDeviationMode = CalculationParameterMetaData.DeviationModeType.Relative;

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteCalculation(calculation, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteVectorCalculation, exportedString);
        }

        [TestMethod]
        public void ParseCalculationV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculationInitializationData calculation = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_CalculationV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                calculation = ComponentDxfIOComponents.CalculationEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(calculation);

            Assert.AreEqual(34, calculation.LocalID);
            Assert.AreEqual("My Calculation", calculation.Name);
            Assert.AreEqual("x+y*z", calculation.Expression);

            Assert.AreEqual(3, calculation.InputParameters.Count);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("x"));
            Assert.AreEqual(new SimId(guid, 6667), calculation.InputParameters["x"]);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("y"));
            Assert.AreEqual(new SimId(otherGuid, 6668), calculation.InputParameters["y"]);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("z"));
            Assert.AreEqual(SimId.Empty, calculation.InputParameters["z"]);

            Assert.AreEqual(3, calculation.ReturnParameters.Count);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r1"));
            Assert.AreEqual(new SimId(guid, 6669), calculation.ReturnParameters["r1"]);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r2"));
            Assert.AreEqual(new SimId(otherGuid, 6670), calculation.ReturnParameters["r2"]);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r3"));
            Assert.AreEqual(SimId.Empty, calculation.ReturnParameters["r3"]);

            Assert.AreEqual(1, calculation.NrExecutions);
            Assert.AreEqual(true, calculation.OverrideVectorResult);
            Assert.AreEqual(SimResultAggregationMethod.Average, calculation.AggregationMethod);
        }

        [TestMethod]
        public void ParseCalculationV5()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculationInitializationData calculation = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_CalculationV5)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 5;

                reader.Read();

                calculation = ComponentDxfIOComponents.CalculationEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(calculation);

            Assert.AreEqual(34, calculation.LocalID);
            Assert.AreEqual("My Calculation", calculation.Name);
            Assert.AreEqual("x+y*z", calculation.Expression);

            Assert.AreEqual(3, calculation.InputParameters.Count);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("x"));
            Assert.AreEqual(new SimId(guid, 6667), calculation.InputParameters["x"]);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("y"));
            Assert.AreEqual(new SimId(guid, 6668), calculation.InputParameters["y"]);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("z"));
            Assert.AreEqual(SimId.Empty, calculation.InputParameters["z"]);

            Assert.AreEqual(3, calculation.ReturnParameters.Count);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r1"));
            Assert.AreEqual(new SimId(guid, 6669), calculation.ReturnParameters["r1"]);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r2"));
            Assert.AreEqual(new SimId(guid, 6670), calculation.ReturnParameters["r2"]);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r3"));
            Assert.AreEqual(SimId.Empty, calculation.ReturnParameters["r3"]);

            Assert.AreEqual(1, calculation.NrExecutions);
            Assert.AreEqual(true, calculation.OverrideVectorResult);
            Assert.AreEqual(SimResultAggregationMethod.Average, calculation.AggregationMethod);
        }

        [TestMethod]
        public void ParseCalculationV0()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculationInitializationData calculation = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_CalculationV0)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 0;

                reader.Read();

                calculation = ComponentDxfIOComponents.CalculationEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(calculation);

            Assert.AreEqual(1075741824, calculation.LocalID);
            Assert.AreEqual("My Calculation", calculation.Name);
            Assert.AreEqual("x+y*z", calculation.Expression);

            Assert.AreEqual(3, calculation.InputParameters.Count);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("x"));
            Assert.AreEqual(new SimId(guid, 1076741824), calculation.InputParameters["x"]);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("y"));
            Assert.AreEqual(new SimId(guid, 1076741825), calculation.InputParameters["y"]);
            Assert.IsTrue(calculation.InputParameters.ContainsKey("z"));
            Assert.AreEqual(SimId.Empty, calculation.InputParameters["z"]);

            Assert.AreEqual(3, calculation.ReturnParameters.Count);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r1"));
            Assert.AreEqual(new SimId(guid, 1076741826), calculation.ReturnParameters["r1"]);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r2"));
            Assert.AreEqual(new SimId(guid, 1076741827), calculation.ReturnParameters["r2"]);
            Assert.IsTrue(calculation.ReturnParameters.ContainsKey("r3"));
            Assert.AreEqual(SimId.Empty, calculation.ReturnParameters["r3"]);

            Assert.AreEqual(1, calculation.NrExecutions);
            Assert.AreEqual(true, calculation.OverrideVectorResult);
            Assert.AreEqual(SimResultAggregationMethod.Average, calculation.AggregationMethod);
        }


        [TestMethod]
        public void ParseVectorCalculationV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculationInitializationData calculation = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_VectorCalculationV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                calculation = ComponentDxfIOComponents.CalculationEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(calculation);

            Assert.AreEqual(2, calculation.VectorOperationList.Count);
            Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_SUM, calculation.VectorOperationList[0]);
            Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, calculation.VectorOperationList[1]);

            Assert.AreEqual(3, calculation.MetaData.Count);
            CheckMetaData(calculation, "x", 1, 3, 2, 1, 23.5, 17.3, DeviationModeType.Relative, true, true, 9.1);
            CheckMetaData(calculation, "y", 0, 0, int.MaxValue, int.MaxValue, 1.0, 1.0, DeviationModeType.Absolute, false, false, 1.0);
            CheckMetaData(calculation, "z", 0, 0, int.MaxValue, int.MaxValue, 1.0, 1.0, DeviationModeType.Absolute, false, false, 1.0);

            Assert.AreEqual(24, calculation.NrExecutions);
            Assert.AreEqual(false, calculation.OverrideVectorResult);
            Assert.AreEqual(SimResultAggregationMethod.Separate, calculation.AggregationMethod);
        }

        [TestMethod]
        public void ParseVectorCalculationV4()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculationInitializationData calculation = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_VectorCalculationV4)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 4;

                reader.Read();

                calculation = ComponentDxfIOComponents.CalculationEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(calculation);

            Assert.AreEqual(2, calculation.VectorOperationList.Count);
            Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_SUM, calculation.VectorOperationList[0]);
            Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, calculation.VectorOperationList[1]);

            Assert.AreEqual(3, calculation.MetaData.Count);
            CheckMetaData(calculation, "x", 1, 3, 2, 1, 23.5, 17.3, DeviationModeType.Relative, true, true, 9.1);
            CheckMetaData(calculation, "y", 0, 0, int.MaxValue, int.MaxValue, 1.0, 1.0, DeviationModeType.Absolute, false, false, 1.0);
            CheckMetaData(calculation, "z", 0, 0, int.MaxValue, int.MaxValue, 1.0, 1.0, DeviationModeType.Absolute, false, false, 1.0);

            Assert.AreEqual(24, calculation.NrExecutions);
            Assert.AreEqual(false, calculation.OverrideVectorResult);
            Assert.AreEqual(SimResultAggregationMethod.Separate, calculation.AggregationMethod);
        }

        [TestMethod]
        public void ParseVectorCalculationV0()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            CalculationInitializationData calculation = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_VectorCalculationV0)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 0;

                reader.Read();

                calculation = ComponentDxfIOComponents.CalculationEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(calculation);

            Assert.AreEqual(2, calculation.VectorOperationList.Count);
            Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_SUM, calculation.VectorOperationList[0]);
            Assert.AreEqual(MultiValueCalculationBinaryOperation.MATRIX_PRODUCT, calculation.VectorOperationList[1]);

            Assert.AreEqual(3, calculation.MetaData.Count);
            CheckMetaData(calculation, "x", 1, 3, 2, 1, 23.5, 17.3, DeviationModeType.Relative, true, true, 9.1);
            CheckMetaData(calculation, "y", 0, 0, int.MaxValue, int.MaxValue, 1.0, 1.0, DeviationModeType.Absolute, false, false, 1.0);
            CheckMetaData(calculation, "z", 0, 0, int.MaxValue, int.MaxValue, 1.0, 1.0, DeviationModeType.Relative, false, true, 1.0);

            Assert.AreEqual(24, calculation.NrExecutions);
            Assert.AreEqual(false, calculation.OverrideVectorResult);
            Assert.AreEqual(SimResultAggregationMethod.Separate, calculation.AggregationMethod);
        }


        private static void CheckMetaData(CalculationInitializationData data, string key,
            int rowStart, int columnStart, int rowCount, int columnCount,
            double mean, double deviation, DeviationModeType mode, bool isRandom,
            bool isClamping, double clampDeviation)
        {
            Assert.IsTrue(data.MetaData.ContainsKey(key));
            Assert.AreEqual(rowStart, data.MetaData[key].Range.RowStart);
            Assert.AreEqual(rowCount, data.MetaData[key].Range.RowCount);
            Assert.AreEqual(columnStart, data.MetaData[key].Range.ColumnStart);
            Assert.AreEqual(columnCount, data.MetaData[key].Range.ColumnCount);

            Assert.AreEqual(mean, data.MetaData[key].RandomizeRelativeMean);
            Assert.AreEqual(deviation, data.MetaData[key].RandomizeDeviation);
            Assert.AreEqual(mode, data.MetaData[key].RandomizeDeviationMode);
            Assert.AreEqual(isRandom, data.MetaData[key].IsRandomized);
            Assert.AreEqual(isClamping, data.MetaData[key].RandomizeIsClamping);
            Assert.AreEqual(clampDeviation, data.MetaData[key].RandomizeClampDeviation);
        }
    }
}
