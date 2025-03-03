using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.MVDXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;



namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class MultiValueDxfTests
    {
        #region File

        [TestMethod]
        public void ParseEmptyFile()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_EmptyV12)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(0, projectData.ValueManager.Count);
        }

        [TestMethod]
        public void WriteEmptyFile()
        {
            ExtendedProjectData data = new ExtendedProjectData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    MultiValueDxfIO.Write(writer, data.ValueManager);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_MVDXF_WriteEmpty, exportedString);
        }

        [TestMethod]
        public void ParseMultipleV3()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_MultipleV3)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(3, projectData.ValueManager.Count);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x is SimMultiValueBigTable);
            Assert.IsNotNull(table);
            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual(2, table.ColumnHeaders.Count);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(1074741826, table.Id.LocalId);

            var field = (SimMultiValueField3D)projectData.ValueManager.First(x => x is SimMultiValueField3D);
            Assert.IsNotNull(field);
            Assert.AreEqual(4, field.XAxis.Count);
            Assert.AreEqual(2, field.YAxis.Count);
            Assert.AreEqual(3, field.ZAxis.Count);
            Assert.AreEqual(Guid.Empty, field.Id.GlobalId);
            Assert.AreEqual(1074741824, field.Id.LocalId);

            var func = (SimMultiValueFunction)projectData.ValueManager.First(x => x is SimMultiValueFunction);
            Assert.IsNotNull(field);
            Assert.AreEqual(0.0, func.Range.Minimum.X);
            Assert.AreEqual(3.0, func.Range.Maximum.X);
            Assert.AreEqual(-1.0, func.Range.Minimum.Y);
            Assert.AreEqual(1.0, func.Range.Maximum.Y);
            Assert.AreEqual(3, func.ZAxis.Count);
            Assert.AreEqual(Guid.Empty, func.Id.GlobalId);
            Assert.AreEqual(1074741825, func.Id.LocalId);
        }

        [TestMethod]
        public void ParseMultipleV5()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_MultipleV5)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(3, projectData.ValueManager.Count);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x is SimMultiValueBigTable);
            Assert.IsNotNull(table);
            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual(2, table.ColumnHeaders.Count);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(1074741826, table.Id.LocalId);

            var field = (SimMultiValueField3D)projectData.ValueManager.First(x => x is SimMultiValueField3D);
            Assert.IsNotNull(field);
            Assert.AreEqual(4, field.XAxis.Count);
            Assert.AreEqual(2, field.YAxis.Count);
            Assert.AreEqual(3, field.ZAxis.Count);
            Assert.AreEqual(Guid.Empty, field.Id.GlobalId);
            Assert.AreEqual(1074741824, field.Id.LocalId);

            var func = (SimMultiValueFunction)projectData.ValueManager.First(x => x is SimMultiValueFunction);
            Assert.IsNotNull(field);
            Assert.AreEqual(0.0, func.Range.Minimum.X);
            Assert.AreEqual(3.0, func.Range.Maximum.X);
            Assert.AreEqual(-1.0, func.Range.Minimum.Y);
            Assert.AreEqual(1.0, func.Range.Maximum.Y);
            Assert.AreEqual(3, func.ZAxis.Count);
            Assert.AreEqual(Guid.Empty, func.Id.GlobalId);
            Assert.AreEqual(1074741825, func.Id.LocalId);
        }

        [TestMethod]
        public void ParseMultipleV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_MultipleV12)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(3, projectData.ValueManager.Count);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x is SimMultiValueBigTable);
            Assert.IsNotNull(table);
            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual(2, table.ColumnHeaders.Count);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(99, table.Id.LocalId);

            var field = (SimMultiValueField3D)projectData.ValueManager.First(x => x is SimMultiValueField3D);
            Assert.IsNotNull(field);
            Assert.AreEqual(4, field.XAxis.Count);
            Assert.AreEqual(2, field.YAxis.Count);
            Assert.AreEqual(3, field.ZAxis.Count);
            Assert.AreEqual(Guid.Empty, field.Id.GlobalId);
            Assert.AreEqual(101, field.Id.LocalId);

            var func = (SimMultiValueFunction)projectData.ValueManager.First(x => x is SimMultiValueFunction);
            Assert.IsNotNull(field);
            Assert.AreEqual(0.0, func.Range.Minimum.X);
            Assert.AreEqual(3.0, func.Range.Maximum.X);
            Assert.AreEqual(-1.0, func.Range.Minimum.Y);
            Assert.AreEqual(1.0, func.Range.Maximum.Y);
            Assert.AreEqual(3, func.ZAxis.Count);
            Assert.AreEqual(Guid.Empty, func.Id.GlobalId);
            Assert.AreEqual(103, func.Id.LocalId);
        }

        [TestMethod]
        public void ParseMultipleV30()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_MultipleV30)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(3, projectData.ValueManager.Count);

            var table = (SimMultiValueBigTable)projectData.ValueManager.First(x => x is SimMultiValueBigTable);
            Assert.IsNotNull(table);
            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual(5, table.ColumnHeaders.Count);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(99, table.Id.LocalId);

            var field = (SimMultiValueField3D)projectData.ValueManager.First(x => x is SimMultiValueField3D);
            Assert.IsNotNull(field);
            Assert.AreEqual(4, field.XAxis.Count);
            Assert.AreEqual(2, field.YAxis.Count);
            Assert.AreEqual(3, field.ZAxis.Count);
            Assert.AreEqual(Guid.Empty, field.Id.GlobalId);
            Assert.AreEqual(101, field.Id.LocalId);

            var func = (SimMultiValueFunction)projectData.ValueManager.First(x => x is SimMultiValueFunction);
            Assert.IsNotNull(field);
            Assert.AreEqual(0.0, func.Range.Minimum.X);
            Assert.AreEqual(3.0, func.Range.Maximum.X);
            Assert.AreEqual(-1.0, func.Range.Minimum.Y);
            Assert.AreEqual(1.0, func.Range.Maximum.Y);
            Assert.AreEqual(3, func.ZAxis.Count);
            Assert.AreEqual(Guid.Empty, func.Id.GlobalId);
            Assert.AreEqual(103, func.Id.LocalId);
        }

        [TestMethod]
        public void WriteMultiple()
        {
            ExtendedProjectData data = new ExtendedProjectData();
            data.ValueManager.StartLoading();
            data.ValueManager.Add(CreateField3D());
            data.ValueManager.Add(CreateFunction());
            data.ValueManager.Add(CreateBigTable());
            data.ValueManager.EndLoading();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    MultiValueDxfIO.Write(writer, data.ValueManager);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMVDXF_Multiple, exportedString);
        }

        #endregion

        #region BigTable

        internal static SimMultiValueBigTable CreateBigTable()
        {
            Guid guid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            SimMultiValueBigTable table = new SimMultiValueBigTable("table name", "unit column", "unit row",
                new SimMultiValueBigTableHeader[]
                {
                    new SimMultiValueBigTableHeader("column header 1", "column unit 1"),
                    new SimMultiValueBigTableHeader("column header 2", "column unit 2"),
                    new SimMultiValueBigTableHeader("column header 3", "column unit 3"),
                    new SimMultiValueBigTableHeader("column header 4", "column unit 4"),
                    new SimMultiValueBigTableHeader("column header 5", "column unit 5"),
                },
                new SimMultiValueBigTableHeader[]
                {
                    new SimMultiValueBigTableHeader("row header 1", "row unit 1"),
                    new SimMultiValueBigTableHeader("row header 2", "row unit 2"),
                    new SimMultiValueBigTableHeader("row header 3", "row unit 3"),
                },
                new object[,]
                {
                    { 1.0, 2, true, null, "abc" },
                    { (long)-7, (ulong)8, (uint)3, 4.0, false},
                    { -1, "a", "b\n\\\t;\nc", 5.0, 6 }
                });

            table.Id = new Data.SimId(guid, 99);
            table.AdditionalInfo = "Additional Text" + Environment.NewLine + "With New Line";

            return table;
        }

        [TestMethod]
        public void WriteBigTableBasic()
        {
            SimMultiValueBigTable table = CreateBigTable();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    MultiValueDxfIO.WriteBigTable(table, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteBigTable, exportedString);
        }


        [TestMethod]
        public void ParseBigTableV5()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_BigTableV5)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var table = projectData.ValueManager[0] as SimMultiValueBigTable;
            Assert.IsNotNull(table);

            Assert.AreEqual("table name", table.Name);
            Assert.AreEqual("unit column", table.UnitX);
            Assert.AreEqual("unit row", table.UnitY);

            Assert.AreEqual(2, table.ColumnHeaders.Count);
            Assert.AreEqual("column header 2", table.ColumnHeaders[0].Name);
            Assert.AreEqual("column unit 2", table.ColumnHeaders[0].Unit);
            Assert.AreEqual("column header 3", table.ColumnHeaders[1].Name);
            Assert.AreEqual("column unit 3", table.ColumnHeaders[1].Unit);

            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual("row header 1", table.RowHeaders[0].Name);
            Assert.AreEqual("row unit 1", table.RowHeaders[0].Unit);
            Assert.AreEqual("-", table.RowHeaders[1].Name);
            Assert.AreEqual("-", table.RowHeaders[1].Unit);
            Assert.AreEqual("-", table.RowHeaders[2].Name);
            Assert.AreEqual("-", table.RowHeaders[2].Unit);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { 1.0, 2.0 },
                    { 3.0, 4.0 },
                    { 5.0, 6.0 }
                },
                table);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(1074741824, table.Id.LocalId);

            Assert.AreEqual("Additional Text" + Environment.NewLine + "With New Line", table.AdditionalInfo);
        }

        [TestMethod]
        public void ParseBigTableV10()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_BigTableV10)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var table = projectData.ValueManager[0] as SimMultiValueBigTable;
            Assert.IsNotNull(table);

            Assert.AreEqual("table name", table.Name);
            Assert.AreEqual("unit column", table.UnitX);
            Assert.AreEqual("unit row", table.UnitY);

            Assert.AreEqual(2, table.ColumnHeaders.Count);
            Assert.AreEqual("column header 1", table.ColumnHeaders[0].Name);
            Assert.AreEqual("column unit 1", table.ColumnHeaders[0].Unit);
            Assert.AreEqual("column header 2", table.ColumnHeaders[1].Name);
            Assert.AreEqual("column unit 2", table.ColumnHeaders[1].Unit);

            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual("row header 1", table.RowHeaders[0].Name);
            Assert.AreEqual("row unit 1", table.RowHeaders[0].Unit);
            Assert.AreEqual("row header 2", table.RowHeaders[1].Name);
            Assert.AreEqual("row unit 2", table.RowHeaders[1].Unit);
            Assert.AreEqual("row header 3", table.RowHeaders[2].Name);
            Assert.AreEqual("row unit 3", table.RowHeaders[2].Unit);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { 1.0, 2.0 },
                    { 3.0, 4.0 },
                    { 5.0, 6.0 }
                },
                table);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(99, table.Id.LocalId);

            Assert.AreEqual("Additional Text" + Environment.NewLine + "With New Line", table.AdditionalInfo);
        }

        [TestMethod]
        public void ParseBigTableV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_BigTableV12)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var table = projectData.ValueManager[0] as SimMultiValueBigTable;
            Assert.IsNotNull(table);

            Assert.AreEqual("table name", table.Name);
            Assert.AreEqual("unit column", table.UnitX);
            Assert.AreEqual("unit row", table.UnitY);

            Assert.AreEqual(2, table.ColumnHeaders.Count);
            Assert.AreEqual("column header 1", table.ColumnHeaders[0].Name);
            Assert.AreEqual("column unit 1", table.ColumnHeaders[0].Unit);
            Assert.AreEqual("column header 2", table.ColumnHeaders[1].Name);
            Assert.AreEqual("column unit 2", table.ColumnHeaders[1].Unit);

            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual("row header 1", table.RowHeaders[0].Name);
            Assert.AreEqual("row unit 1", table.RowHeaders[0].Unit);
            Assert.AreEqual("row header 2", table.RowHeaders[1].Name);
            Assert.AreEqual("row unit 2", table.RowHeaders[1].Unit);
            Assert.AreEqual("row header 3", table.RowHeaders[2].Name);
            Assert.AreEqual("row unit 3", table.RowHeaders[2].Unit);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { 1.0, 2.0 },
                    { 3.0, 4.0 },
                    { 5.0, 6.0 }
                },
                table);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(99, table.Id.LocalId);

            Assert.AreEqual("Additional Text" + Environment.NewLine + "With New Line", table.AdditionalInfo);
        }

        [TestMethod]
        public void ParseBigTableV18()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_BigTableV18)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var table = projectData.ValueManager[0] as SimMultiValueBigTable;
            Assert.IsNotNull(table);

            Assert.AreEqual("table name", table.Name);
            Assert.AreEqual("unit column", table.UnitX);
            Assert.AreEqual("unit row", table.UnitY);

            Assert.AreEqual(5, table.ColumnHeaders.Count);
            Assert.AreEqual("column header 1", table.ColumnHeaders[0].Name);
            Assert.AreEqual("column unit 1", table.ColumnHeaders[0].Unit);
            Assert.AreEqual("column header 2", table.ColumnHeaders[1].Name);
            Assert.AreEqual("column unit 2", table.ColumnHeaders[1].Unit);
            Assert.AreEqual("column header 3", table.ColumnHeaders[2].Name);
            Assert.AreEqual("column unit 3", table.ColumnHeaders[2].Unit);
            Assert.AreEqual("column header 4", table.ColumnHeaders[3].Name);
            Assert.AreEqual("column unit 4", table.ColumnHeaders[3].Unit);
            Assert.AreEqual("column header 5", table.ColumnHeaders[4].Name);
            Assert.AreEqual("column unit 5", table.ColumnHeaders[4].Unit);

            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual("row header 1", table.RowHeaders[0].Name);
            Assert.AreEqual("row unit 1", table.RowHeaders[0].Unit);
            Assert.AreEqual("row header 2", table.RowHeaders[1].Name);
            Assert.AreEqual("row unit 2", table.RowHeaders[1].Unit);
            Assert.AreEqual("row header 3", table.RowHeaders[2].Name);
            Assert.AreEqual("row unit 3", table.RowHeaders[2].Unit);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { 1.0, 2, true, null, "abc" },
                    { null, null, 3, 4.0, false},
                    { -1, "a", "b\n\\\t;\nc", 5.0, 6 }
                },
                table);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(99, table.Id.LocalId);

            Assert.AreEqual("Additional Text" + Environment.NewLine + "With New Line", table.AdditionalInfo);
        }

        [TestMethod]
        public void ParseBigTableV30()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_BigTableV30)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var table = projectData.ValueManager[0] as SimMultiValueBigTable;
            Assert.IsNotNull(table);

            Assert.AreEqual("table name", table.Name);
            Assert.AreEqual("unit column", table.UnitX);
            Assert.AreEqual("unit row", table.UnitY);

            Assert.AreEqual(5, table.ColumnHeaders.Count);
            Assert.AreEqual("column header 1", table.ColumnHeaders[0].Name);
            Assert.AreEqual("column unit 1", table.ColumnHeaders[0].Unit);
            Assert.AreEqual("column header 2", table.ColumnHeaders[1].Name);
            Assert.AreEqual("column unit 2", table.ColumnHeaders[1].Unit);
            Assert.AreEqual("column header 3", table.ColumnHeaders[2].Name);
            Assert.AreEqual("column unit 3", table.ColumnHeaders[2].Unit);
            Assert.AreEqual("column header 4", table.ColumnHeaders[3].Name);
            Assert.AreEqual("column unit 4", table.ColumnHeaders[3].Unit);
            Assert.AreEqual("column header 5", table.ColumnHeaders[4].Name);
            Assert.AreEqual("column unit 5", table.ColumnHeaders[4].Unit);

            Assert.AreEqual(3, table.RowHeaders.Count);
            Assert.AreEqual("row header 1", table.RowHeaders[0].Name);
            Assert.AreEqual("row unit 1", table.RowHeaders[0].Unit);
            Assert.AreEqual("row header 2", table.RowHeaders[1].Name);
            Assert.AreEqual("row unit 2", table.RowHeaders[1].Unit);
            Assert.AreEqual("row header 3", table.RowHeaders[2].Name);
            Assert.AreEqual("row unit 3", table.RowHeaders[2].Unit);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { 1.0, 2, true, null, "abc" },
                    { (long)-7, (ulong)8, (uint)3, 4.0, false},
                    { -1, "a", "b\n\\\t;\nc", 5.0, 6 }
                },
                table);
            Assert.AreEqual(Guid.Empty, table.Id.GlobalId);
            Assert.AreEqual(99, table.Id.LocalId);

            Assert.AreEqual("Additional Text" + Environment.NewLine + "With New Line", table.AdditionalInfo);
        }

        #endregion

        #region Field3D

        internal static SimMultiValueField3D CreateField3D()
        {
            Guid guid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            SimMultiValueField3D field = new SimMultiValueField3D("field name",
                new double[] { 0.1, 0.2, 0.4, 0.5 }, "unit X",
                new double[] { 1.1, 4.4 }, "unit Y",
                new double[] { -1.5, -1.2, -1.1 }, "unit Z",
                Enumerable.Range(2, 24).Select(x => ((double)x) / 2.0), true);

            field.Id = new Data.SimId(guid, 101);
            return field;
        }

        [TestMethod]
        public void WriteField3DBasic()
        {
            SimMultiValueField3D field = CreateField3D();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    MultiValueDxfIO.WriteField3D(field, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteField3D, exportedString);
        }

        [TestMethod]
        public void ParseField3DV5()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_Field3DV5)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var field = projectData.ValueManager[0] as SimMultiValueField3D;
            Assert.IsNotNull(field);

            Assert.AreEqual("field name", field.Name);
            Assert.AreEqual("unit X", field.UnitX);
            Assert.AreEqual("unit Y", field.UnitY);
            Assert.AreEqual("unit Z", field.UnitZ);

            Assert.AreEqual(Guid.Empty, field.Id.GlobalId);
            Assert.AreEqual(1074741824, field.Id.LocalId);

            AssertUtil.ContainEqualValues(new double[] { 0.1, 0.2, 0.4, 0.5 }, field.XAxis);
            AssertUtil.ContainEqualValues(new double[] { 1.1, 4.4 }, field.YAxis);
            AssertUtil.ContainEqualValues(new double[] { -1.5, -1.2, -1.1 }, field.ZAxis);

            double check = 2.0;
            for (int z = 0; z < 3; z++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        Assert.AreEqual(check / 2.0, field[new IntIndex3D(x, y, z)]);
                        check++;
                    }
                }
            }

            Assert.IsTrue(field.CanInterpolate);
        }

        [TestMethod]
        public void ParseField3DV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_Field3DV12)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var field = projectData.ValueManager[0] as SimMultiValueField3D;
            Assert.IsNotNull(field);

            Assert.AreEqual("field name", field.Name);
            Assert.AreEqual("unit X", field.UnitX);
            Assert.AreEqual("unit Y", field.UnitY);
            Assert.AreEqual("unit Z", field.UnitZ);

            Assert.AreEqual(Guid.Empty, field.Id.GlobalId);
            Assert.AreEqual(101, field.Id.LocalId);

            AssertUtil.ContainEqualValues(new double[] { 0.1, 0.2, 0.4, 0.5 }, field.XAxis);
            AssertUtil.ContainEqualValues(new double[] { 1.1, 4.4 }, field.YAxis);
            AssertUtil.ContainEqualValues(new double[] { -1.5, -1.2, -1.1 }, field.ZAxis);

            double check = 2.0;
            for (int z = 0; z < 3; z++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        Assert.AreEqual(check / 2.0, field[new IntIndex3D(x, y, z)]);
                        check++;
                    }
                }
            }

            Assert.IsTrue(field.CanInterpolate);
        }

        #endregion

        #region Function Field

        internal static SimMultiValueFunction CreateFunction()
        {
            Guid guid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            SimMultiValueFunction function = new SimMultiValueFunction("field name", "unit X", "unit Y", "unit Z",
                new SimRect(0, -1, 3, 2), new double[] { 0.2, 0.5, 0.7 }, new SimMultiValueFunctionGraph[]
                {
                    new SimMultiValueFunctionGraph("graph 1", new SimPoint3D[]
                        { new SimPoint3D(0,-1, 0.2), new SimPoint3D(0, 1, 0.2), new SimPoint3D(2, 0, 0.2) }),
                    new SimMultiValueFunctionGraph("graph 2", new SimPoint3D[]
                        { new SimPoint3D(0.5,-1, 0.7), new SimPoint3D(0, -1, 0.7) })
                });
            function.Id = new Data.SimId(guid, 103);

            return function;
        }

        [TestMethod]
        public void WriteFunctionField()
        {
            SimMultiValueFunction function = CreateFunction();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    MultiValueDxfIO.WriteFunctionField(function, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteFunction, exportedString);
        }

        [TestMethod]
        public void ParseFunctionFieldV5()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_FunctionV5)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var field = projectData.ValueManager[0] as SimMultiValueFunction;
            Assert.IsNotNull(field);

            Assert.AreEqual("field name", field.Name);
            Assert.AreEqual("unit X", field.UnitX);
            Assert.AreEqual("unit Y", field.UnitY);
            Assert.AreEqual("unit Z", field.UnitZ);

            Assert.AreEqual(Guid.Empty, field.Id.GlobalId);
            Assert.AreEqual(1074741824, field.Id.LocalId);

            Assert.AreEqual(0.0, field.Range.Minimum.X);
            Assert.AreEqual(3.0, field.Range.Maximum.X);
            Assert.AreEqual(-1.0, field.Range.Minimum.Y);
            Assert.AreEqual(1.0, field.Range.Maximum.Y);

            AssertUtil.ContainEqualValues(new double[] { 0.2, 0.5, 0.7 }, field.ZAxis);

            Assert.AreEqual(2, field.Graphs.Count);

            var g = field.Graphs.First(x => x.Name == "graph 1");
            {
                AssertUtil.ContainEqualValues(new SimPoint3D[] { new SimPoint3D(0, -1, 0.2), new SimPoint3D(0, 1, 0.2), new SimPoint3D(2, 0, 0.2) }, g.Points);
            }
            g = field.Graphs.First(x => x.Name == "graph 2");
            {
                AssertUtil.ContainEqualValues(new SimPoint3D[] { new SimPoint3D(0.5, -1, 0.7), new SimPoint3D(0, -1, 0.7) }, g.Points);
            }

            Assert.IsTrue(field.CanInterpolate);
        }

        [TestMethod]
        public void ParseFunctionFieldV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVDXF_FunctionV12)))
            {
                MultiValueDxfIO.Read(reader, new DXFParserInfo(guid, projectData));
            }

            Assert.AreEqual(1, projectData.ValueManager.Count);

            var field = projectData.ValueManager[0] as SimMultiValueFunction;
            Assert.IsNotNull(field);

            Assert.AreEqual("field name", field.Name);
            Assert.AreEqual("unit X", field.UnitX);
            Assert.AreEqual("unit Y", field.UnitY);
            Assert.AreEqual("unit Z", field.UnitZ);

            Assert.AreEqual(Guid.Empty, field.Id.GlobalId);
            Assert.AreEqual(103, field.Id.LocalId);

            Assert.AreEqual(0.0, field.Range.Minimum.X);
            Assert.AreEqual(3.0, field.Range.Maximum.X);
            Assert.AreEqual(-1.0, field.Range.Minimum.Y);
            Assert.AreEqual(1.0, field.Range.Maximum.Y);

            AssertUtil.ContainEqualValues(new double[] { 0.2, 0.5, 0.7 }, field.ZAxis);

            Assert.AreEqual(2, field.Graphs.Count);

            var g = field.Graphs.First(x => x.Name == "graph 1");
            {
                AssertUtil.ContainEqualValues(new SimPoint3D[] { new SimPoint3D(0, -1, 0.2), new SimPoint3D(0, 1, 0.2), new SimPoint3D(2, 0, 0.2) }, g.Points);
            }
            g = field.Graphs.First(x => x.Name == "graph 2");
            {
                AssertUtil.ContainEqualValues(new SimPoint3D[] { new SimPoint3D(0.5, -1, 0.7), new SimPoint3D(0, -1, 0.7) }, g.Points);
            }

            Assert.IsTrue(field.CanInterpolate);
        }

        #endregion
    }
}
