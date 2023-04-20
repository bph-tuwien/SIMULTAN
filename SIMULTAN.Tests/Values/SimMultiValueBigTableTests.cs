using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Values
{
    [TestClass]
    public class SimMultiValueBigTableTests
    {
        internal class BigTableEventCounter : PropertyChangedEventCounter
        {
            internal BigTableEventCounter(SimMultiValueBigTable table) : base(table)
            {
                table.ValueChanged += (s, e) => ValueChangedArgs.Add(e);
                table.Resized += (s, e) => ResizedArgs.Add(e);
                table.HeaderValueChanged += (s, e) => HeaderValueChangedArgs.Add(e);
            }

            internal List<SimMultiValueBigTable.ResizeEventArgs> ResizedArgs { get; } = new List<SimMultiValueBigTable.ResizeEventArgs>();

            internal List<SimMultiValueBigTable.HeaderValueChangedEventArgs> HeaderValueChangedArgs { get; } = new List<SimMultiValueBigTable.HeaderValueChangedEventArgs>();

            internal List<SimMultiValueBigTable.ValueChangedEventArgs> ValueChangedArgs { get; } = new List<SimMultiValueBigTable.ValueChangedEventArgs>();

            internal void AssertEventCount(int propertyChangedCount, int resizedCount, int headerValueChangedCount, int valueChangedCount)
            {
                base.AssertEventCount(propertyChangedCount);
                Assert.AreEqual(resizedCount, this.ResizedArgs.Count);
                Assert.AreEqual(headerValueChangedCount, this.HeaderValueChangedArgs.Count);
                Assert.AreEqual(valueChangedCount, this.ValueChangedArgs.Count);
            }
        }

        internal static (string name, string unitRow, string unitColumn,
            List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values)
            DoubleTestData(int rows, int columns)
        {
            List<SimMultiValueBigTableHeader> rowHeaders = new List<SimMultiValueBigTableHeader>(rows);
            for (int i = 0; i < rows; ++i)
                rowHeaders.Add(new SimMultiValueBigTableHeader(string.Format("Row {0}", i), string.Format("RowUnit {0}", i)));

            List<SimMultiValueBigTableHeader> columnHeaders = new List<SimMultiValueBigTableHeader>(columns);
            for (int i = 0; i < columns; ++i)
                columnHeaders.Add(new SimMultiValueBigTableHeader(string.Format("Column {0}", i), string.Format("ColUnit {0}", i)));

            List<List<object>> values = new List<List<object>>(rows);
            for (int r = 0; r < rows; ++r)
            {
                List<object> rowValues = new List<object>(columns);
                for (int c = 0; c < columns; ++c)
                {
                    rowValues.Add((double)(r * 5000 + c));
                }
                values.Add(rowValues);
            }

            return ("BigTable", "UnitRows", "UnitColumns", rowHeaders, columnHeaders, values);
        }



        internal static (string name, string unitRow, string unitColumn,
            List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values)
            BoolTestData(int rows, int columns)
        {
            List<SimMultiValueBigTableHeader> rowHeaders = new List<SimMultiValueBigTableHeader>(rows);
            for (int i = 0; i < rows; ++i)
                rowHeaders.Add(new SimMultiValueBigTableHeader(string.Format("Row {0}", i), string.Format("RowUnit {0}", i)));

            List<SimMultiValueBigTableHeader> columnHeaders = new List<SimMultiValueBigTableHeader>(columns);
            for (int i = 0; i < columns; ++i)
                columnHeaders.Add(new SimMultiValueBigTableHeader(string.Format("Column {0}", i), string.Format("ColUnit {0}", i)));

            List<List<object>> values = new List<List<object>>(rows);
            for (int r = 0; r < rows; ++r)
            {
                List<object> rowValues = new List<object>(columns);
                for (int c = 0; c < columns; ++c)
                {
                    rowValues.Add(true);
                }
                values.Add(rowValues);
            }

            return ("BigTable", "UnitRows", "UnitColumns", rowHeaders, columnHeaders, values);
        }


        internal static (string name, string unitRow, string unitColumn,
    List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values)
    IntTestData(int rows, int columns)
        {
            List<SimMultiValueBigTableHeader> rowHeaders = new List<SimMultiValueBigTableHeader>(rows);
            for (int i = 0; i < rows; ++i)
                rowHeaders.Add(new SimMultiValueBigTableHeader(string.Format("Row {0}", i), string.Format("RowUnit {0}", i)));

            List<SimMultiValueBigTableHeader> columnHeaders = new List<SimMultiValueBigTableHeader>(columns);
            for (int i = 0; i < columns; ++i)
                columnHeaders.Add(new SimMultiValueBigTableHeader(string.Format("Column {0}", i), string.Format("ColUnit {0}", i)));

            List<List<object>> values = new List<List<object>>(rows);
            for (int r = 0; r < rows; ++r)
            {
                List<object> rowValues = new List<object>(columns);
                for (int c = 0; c < columns; ++c)
                {
                    int val = r * 5000 + c;
                    rowValues.Add(val);
                }
                values.Add(rowValues);
            }

            return ("BigTable", "UnitRows", "UnitColumns", rowHeaders, columnHeaders, values);
        }


        internal static (string name, string unitRow, string unitColumn,
        List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values)
        StringTestData(int rows, int columns)
        {
            List<SimMultiValueBigTableHeader> rowHeaders = new List<SimMultiValueBigTableHeader>(rows);
            for (int i = 0; i < rows; ++i)
                rowHeaders.Add(new SimMultiValueBigTableHeader(string.Format("Row {0}", i), string.Format("RowUnit {0}", i)));

            List<SimMultiValueBigTableHeader> columnHeaders = new List<SimMultiValueBigTableHeader>(columns);
            for (int i = 0; i < columns; ++i)
                columnHeaders.Add(new SimMultiValueBigTableHeader(string.Format("Column {0}", i), string.Format("ColUnit {0}", i)));

            List<List<object>> values = new List<List<object>>(rows);
            for (int r = 0; r < rows; ++r)
            {
                List<object> rowValues = new List<object>(columns);
                for (int c = 0; c < columns; ++c)
                {
                    rowValues.Add("ASD" + c.ToString());
                }
                values.Add(rowValues);
            }

            return ("BigTable", "UnitRows", "UnitColumns", rowHeaders, columnHeaders, values);
        }



        internal static ((string name, string unitRow, string unitColumn,
                    List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values) data,
                    SimMultiValueBigTable table, ExtendedProjectData projectData)
                    DoubleTestDataTable(int rows, int columns)
        {
            ExtendedProjectData projectData = new ExtendedProjectData();

            var data = DoubleTestData(rows, columns);
            var table = new SimMultiValueBigTable(
                data.name, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders, data.values
                );
            projectData.ValueManager.Add(table);

            return (data, table, projectData);
        }



        internal static ((string name, string unitRow, string unitColumn,
                    List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values) data,
                    SimMultiValueBigTable table, ExtendedProjectData projectData)
                  IntTestDataTable(int rows, int columns)
        {
            ExtendedProjectData projectData = new ExtendedProjectData();

            var data = IntTestData(rows, columns);
            var table = new SimMultiValueBigTable(
                data.name, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders, data.values
                );
            projectData.ValueManager.Add(table);

            return (data, table, projectData);
        }


        internal static ((string name, string unitRow, string unitColumn,
               List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values) data,
               SimMultiValueBigTable table, ExtendedProjectData projectData)
            StringTestDataTable(int rows, int columns)
        {
            ExtendedProjectData projectData = new ExtendedProjectData();

            var data = StringTestData(rows, columns);
            var table = new SimMultiValueBigTable(
                data.name, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders, data.values
                );
            projectData.ValueManager.Add(table);

            return (data, table, projectData);
        }



        internal static ((string name, string unitRow, string unitColumn,
               List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values) data,
               SimMultiValueBigTable table, ExtendedProjectData projectData)
               BoolTestDataTable(int rows, int columns)
        {
            ExtendedProjectData projectData = new ExtendedProjectData();

            var data = BoolTestData(rows, columns);
            var table = new SimMultiValueBigTable(
                data.name, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders, data.values
                );
            projectData.ValueManager.Add(table);

            return (data, table, projectData);
        }

        internal static SimMultiValueBigTable TestDataTableAggregate()
        {
            var columnHeaders = new List<SimMultiValueBigTableHeader>
            {
                new SimMultiValueBigTableHeader("c2", "u2"),
                new SimMultiValueBigTableHeader("c1", "u1"),
                new SimMultiValueBigTableHeader("c2", "u2"),
                new SimMultiValueBigTableHeader("c2", "u2"),
                new SimMultiValueBigTableHeader("c1", "u1"),
            };

            var rowHeaders = new List<SimMultiValueBigTableHeader>
            {
                new SimMultiValueBigTableHeader("r3", "ru3"),
                new SimMultiValueBigTableHeader("r1", "ru1"),
                new SimMultiValueBigTableHeader("r3", "ru3"),
                new SimMultiValueBigTableHeader("r2", "ru2"),
                new SimMultiValueBigTableHeader("r1", "ru1"),
                new SimMultiValueBigTableHeader("r1", "ru1"),
            };

            var data = new List<List<double>>
            {
                new List<double>{ 1, 2, 3, 4, 5 },
                new List<double>{ 6, 7, 8, 9, 10 },
                new List<double>{ 11, 12, 13, 14, 15 },
                new List<double>{ 16, 17, 18, 19, 20 },
                new List<double>{ 21, 22, 23, 24, 25 },
                new List<double>{ 26, 27, 28, 29, 30 },
            };

            return new SimMultiValueBigTable(
                "tab", "unitcolumn", "unitrow", columnHeaders, rowHeaders, data
                );
        }

        internal static void CheckTestData(SimMultiValueBigTable table, (string name, string unitRow, string unitColumn,
            List<SimMultiValueBigTableHeader> rowHeaders, List<SimMultiValueBigTableHeader> columnHeaders, List<List<object>> values) testData)
        {
            if (testData.name != null)
                Assert.AreEqual(testData.name, table.Name);

            Assert.AreEqual(false, table.CanInterpolate);

            Assert.AreEqual(testData.unitColumn, table.UnitX);
            Assert.AreEqual(testData.unitRow, table.UnitY);
            Assert.AreEqual(SimMultiValueType.BigTable, table.MVType);

            Assert.AreEqual(testData.rowHeaders.Count, table.RowHeaders.Count);
            for (int i = 0; i < testData.rowHeaders.Count; ++i)
            {
                Assert.AreEqual(testData.rowHeaders[i].Name, table.RowHeaders[i].Name);
                Assert.AreEqual(testData.rowHeaders[i].Unit, table.RowHeaders[i].Unit);
                Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Rows, table.RowHeaders[i].Axis);
                Assert.AreEqual(i, table.RowHeaders[i].Index);
                Assert.AreEqual(table, table.RowHeaders[i].Table);
            }

            Assert.AreEqual(testData.columnHeaders.Count, table.ColumnHeaders.Count);
            for (int i = 0; i < testData.columnHeaders.Count; ++i)
            {
                Assert.AreEqual(testData.columnHeaders[i].Name, table.ColumnHeaders[i].Name);
                Assert.AreEqual(testData.columnHeaders[i].Unit, table.ColumnHeaders[i].Unit);
                Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Columns, table.ColumnHeaders[i].Axis);
                Assert.AreEqual(i, table.ColumnHeaders[i].Index);
                Assert.AreEqual(table, table.ColumnHeaders[i].Table);
            }

            AssertUtil.ContainEqualValues(testData.values, table);
        }


        [TestMethod]
        public void Ctor()
        {
            var data = DoubleTestData(3, 4);

            //Argument Null
            Assert.ThrowsException<ArgumentNullException>(() =>
                { var table = new SimMultiValueBigTable(null, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders, data.values); });
            Assert.ThrowsException<ArgumentNullException>(() =>
                { var table = new SimMultiValueBigTable(data.name, data.unitColumn, data.unitRow, null, data.rowHeaders, data.values); });
            Assert.ThrowsException<ArgumentNullException>(() =>
                { var table = new SimMultiValueBigTable(data.name, data.unitColumn, data.unitRow, data.columnHeaders, null, data.values); });
            Assert.ThrowsException<ArgumentNullException>(() =>
                {
                    var table = new SimMultiValueBigTable(data.name, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders,
                      (List<List<double>>)null);
                });

            //Argument wrong size
            List<List<double>> smallData = new List<List<double>> { new List<double>() { 1, 2, 3, 4 }, new List<double> { 5, 6, 7, 8 } };
            Assert.ThrowsException<ArgumentException>(() =>
                { var table = new SimMultiValueBigTable(data.name, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders, smallData); });
            smallData = new List<List<double>> { new List<double>() { 1, 2, 3 }, new List<double> { 5, 6, 7 }, new List<double> { 9, 10, 11 } };
            Assert.ThrowsException<ArgumentException>(() =>
                { var table = new SimMultiValueBigTable(data.name, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders, smallData); });


            var bigTable = new SimMultiValueBigTable(data.name, data.unitColumn, data.unitRow, data.columnHeaders, data.rowHeaders, data.values);
            CheckTestData(bigTable, data);
            Assert.AreEqual(SimId.Empty, bigTable.Id);
            Assert.AreEqual(null, bigTable.Factory);
        }

        [TestMethod]
        public void PropertyChanged()
        {
            var data = DoubleTestDataTable(2, 3);
            var events = new BigTableEventCounter(data.table);

            data.table.UnitX = "asdf";
            Assert.AreEqual("asdf", data.table.UnitX);
            events.AssertEventCount(1, 0, 0, 0);
            Assert.AreEqual(nameof(SimMultiValueBigTable.UnitX), events.PropertyChangedArgs[0]);

            data.table.UnitY = "asdf";
            Assert.AreEqual("asdf", data.table.UnitY);
            events.AssertEventCount(2, 0, 0, 0);
            Assert.AreEqual(nameof(SimMultiValueBigTable.UnitY), events.PropertyChangedArgs[1]);

            data.table.Name = "asdf";
            Assert.AreEqual("asdf", data.table.Name);
            events.AssertEventCount(3, 0, 0, 0);
            Assert.AreEqual(nameof(SimMultiValueBigTable.Name), events.PropertyChangedArgs[2]);

            data.table.CanInterpolate = true; //Can't be changed
            Assert.AreEqual(false, data.table.CanInterpolate);
            events.AssertEventCount(3, 0, 0, 0);

            data.table.AdditionalInfo = "asdf2";
            Assert.AreEqual("asdf2", data.table.AdditionalInfo);
            events.AssertEventCount(4, 0, 0, 0);
            Assert.AreEqual(nameof(SimMultiValueBigTable.AdditionalInfo), events.PropertyChangedArgs[3]);
        }

        [TestMethod]
        public void CtorParsing()
        {
            var data = DoubleTestData(3, 4);
            long id = 99;
            string additionalText = "hello world";

            //General exceptions
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueBigTable(id, null, data.unitColumn, data.unitRow,
                    data.columnHeaders, data.rowHeaders, data.values, additionalText);
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                new SimMultiValueBigTable(id, string.Empty, data.unitColumn, data.unitRow,
                    data.columnHeaders, data.rowHeaders, data.values, additionalText);
            });
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueBigTable(id, data.name, data.unitColumn, data.unitRow,
                    null, data.rowHeaders, data.values, additionalText);
            });
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueBigTable(id, data.name, data.unitColumn, data.unitRow,
                    data.columnHeaders, null, data.values, additionalText);
            });
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new SimMultiValueBigTable(id, data.name, data.unitColumn, data.unitRow,
                    data.columnHeaders, data.rowHeaders, null, additionalText);
            });

            //Size missmatches
            var rowHeadersReduced = data.rowHeaders.ToList();
            rowHeadersReduced.RemoveAt(0);
            Assert.ThrowsException<ArgumentException>(() =>
            {
                new SimMultiValueBigTable(id, data.name, data.unitColumn, data.unitRow,
                    data.columnHeaders, rowHeadersReduced, data.values, additionalText);
            });

            var columnHeadersReduced = data.columnHeaders.ToList();
            columnHeadersReduced.RemoveAt(0);
            Assert.ThrowsException<ArgumentException>(() =>
            {
                new SimMultiValueBigTable(id, data.name, data.unitColumn, data.unitRow,
                    columnHeadersReduced, data.rowHeaders, data.values, additionalText);
            });

            SimMultiValueBigTable table = new SimMultiValueBigTable(id, data.name, data.unitColumn, data.unitRow,
                data.columnHeaders, data.rowHeaders, data.values, additionalText);

            Assert.AreEqual(id, table.Id.LocalId);
            CheckTestData(table, data);
        }

        [TestMethod]
        public void Clone()
        {
            var data = DoubleTestData(3, 4);
            long id = 99;
            string additionalText = "hello world";
            Guid guid = Guid.NewGuid();

            SimMultiValueBigTable table = new SimMultiValueBigTable(id, data.name, data.unitColumn, data.unitRow,
                data.columnHeaders, data.rowHeaders, data.values, additionalText);

            var clonedTable = table.Clone() as SimMultiValueBigTable;

            CheckTestData(clonedTable, data);
            Assert.AreEqual(SimId.Empty, clonedTable.Id);
            Assert.AreEqual(null, clonedTable.Factory);
        }

        [TestMethod]
        public void RemoveRow()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            CheckTestData(data.table, data.data);

            data.table.RowHeaders.RemoveAt(1);
            data.data.rowHeaders.RemoveAt(1);
            data.data.values.RemoveAt(1);

            events.AssertEventCount(0, 1, 0, 0);
            Assert.AreEqual(-1, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(1, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Rows, events.ResizedArgs[0].ResizeDirection);

            CheckTestData(data.table, data.data);
        }

        [TestMethod]
        public void AddRow()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            CheckTestData(data.table, data.data);

            var newRowHeader = new SimMultiValueBigTableHeader("newHeader", "newUnit");

            data.table.RowHeaders.Insert(1, newRowHeader);
            data.data.rowHeaders.Insert(1, newRowHeader);
            data.data.values.Insert(1, Enumerable.Repeat<object>(null, data.data.values[0].Count).ToList());

            events.AssertEventCount(0, 1, 0, 0);
            Assert.AreEqual(-1, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(1, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Rows, events.ResizedArgs[0].ResizeDirection);

            CheckTestData(data.table, data.data);
        }

        [TestMethod]
        public void ClearRows()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            CheckTestData(data.table, data.data);

            data.table.RowHeaders.Clear();
            data.data.rowHeaders.Clear();
            data.data.values.Clear();

            events.AssertEventCount(0, 1, 0, 0);
            Assert.AreEqual(-1, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Rows, events.ResizedArgs[0].ResizeDirection);

            CheckTestData(data.table, data.data);
        }

        [TestMethod]
        public void RemoveColumn()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            CheckTestData(data.table, data.data);

            data.table.ColumnHeaders.RemoveAt(1);
            data.data.columnHeaders.RemoveAt(1);
            data.data.values.ForEach(x => x.RemoveAt(1));

            events.AssertEventCount(0, 1, 0, 0);
            Assert.AreEqual(1, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(-1, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Columns, events.ResizedArgs[0].ResizeDirection);

            CheckTestData(data.table, data.data);
        }

        [TestMethod]
        public void AddColumn()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            CheckTestData(data.table, data.data);

            var newColumnHeader = new SimMultiValueBigTableHeader("newHeader", "newUnit");

            data.table.ColumnHeaders.Insert(1, newColumnHeader);
            data.data.columnHeaders.Insert(1, newColumnHeader);

            data.data.values.ForEach(x => x.Insert(1, null));

            events.AssertEventCount(0, 1, 0, 0);
            Assert.AreEqual(1, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(-1, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Columns, events.ResizedArgs[0].ResizeDirection);

            CheckTestData(data.table, data.data);
        }

        [TestMethod]
        public void ClearColumns()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            CheckTestData(data.table, data.data);

            data.table.ColumnHeaders.Clear();
            data.data.columnHeaders.Clear();
            data.data.values.Clear();

            events.AssertEventCount(0, 1, 0, 0);
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(-1, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Columns, events.ResizedArgs[0].ResizeDirection);

            CheckTestData(data.table, data.data);
        }

        [TestMethod]
        public void ReplaceColumnHeader()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            var oldHeader = data.table.ColumnHeaders[1];
            data.data.columnHeaders[1] = new SimMultiValueBigTableHeader("newname", "newunit");
            data.table.ColumnHeaders[1] = data.data.columnHeaders[1];

            CheckTestData(data.table, data.data);
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Undefined, oldHeader.Axis);
            Assert.AreEqual(null, oldHeader.Table);
        }

        [TestMethod]
        public void ReplaceRowHeader()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            var oldHeader = data.table.RowHeaders[1];
            data.data.rowHeaders[1] = new SimMultiValueBigTableHeader("newname", "newunit");
            data.table.RowHeaders[1] = data.data.rowHeaders[1];

            CheckTestData(data.table, data.data);
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Undefined, oldHeader.Axis);
            Assert.AreEqual(null, oldHeader.Table);
        }

        [TestMethod]
        public void ReplaceData()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);
            CheckTestData(data.table, data.data);

            //Exceptions
            Assert.ThrowsException<ArgumentNullException>(() => { data.table.ReplaceData(null, data.data.rowHeaders, data.data.values); });
            Assert.ThrowsException<ArgumentNullException>(() => { data.table.ReplaceData(data.data.columnHeaders, null, data.data.values); });
            Assert.ThrowsException<ArgumentNullException>(() => { data.table.ReplaceData(data.data.columnHeaders, data.data.rowHeaders, null); });

            Assert.ThrowsException<ArgumentException>(() => { data.table.ReplaceData(new List<SimMultiValueBigTableHeader> { }, data.data.rowHeaders, data.data.values); });
            Assert.ThrowsException<ArgumentException>(() => { data.table.ReplaceData(data.data.columnHeaders, new List<SimMultiValueBigTableHeader> { }, data.data.values); });

            //Working example
            var data2 = DoubleTestData(4, 5);
            data.table.ReplaceData(data2.columnHeaders, data2.rowHeaders, data2.values);

            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);

            CheckTestData(data.table, data2);
        }

        [TestMethod]
        public void ReplaceDataWithTable()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);
            CheckTestData(data.table, data.data);

            var replaceData = DoubleTestDataTable(5, 1);
            data.table.ReplaceData(replaceData.table);

            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);

            CheckTestData(data.table, replaceData.data);
        }

        [TestMethod]
        public void Resize()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);
            CheckTestData(data.table, data.data);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { data.table.Resize(-1, 4); });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { data.table.Resize(3, -1); });


            var compareData1 = DoubleTestData(1, 2);
            data.table.Resize(1, 2);

            events.AssertEventCount(0, 1, 0, 0);
            Assert.AreEqual(2, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(1, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
            CheckTestData(data.table, compareData1);


            var compareData2 = DoubleTestData(3, 4);
            for (int i = 1; i < 3; ++i)
            {
                compareData2.rowHeaders[i].Name = "";
                compareData2.rowHeaders[i].Unit = "";
            }
            for (int i = 2; i < 4; ++i)
            {
                compareData2.columnHeaders[i].Name = "";
                compareData2.columnHeaders[i].Unit = "";
            }
            for (int r = 0; r < 3; ++r)
                for (int c = 2; c < 4; ++c)
                    compareData2.values[r][c] = null;
            for (int r = 1; r < 3; ++r)
                for (int c = 0; c < 4; ++c)
                    compareData2.values[r][c] = null;

            data.table.Resize(3, 4);

            events.AssertEventCount(0, 2, 0, 0);
            Assert.AreEqual(2, events.ResizedArgs[1].ColumnStartIndex);
            Assert.AreEqual(1, events.ResizedArgs[1].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
            CheckTestData(data.table, compareData2);
        }

        [TestMethod]
        public void Count()
        {
            var data = DoubleTestDataTable(3, 4);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { data.table.Count(-1); });
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { data.table.Count(2); });

            Assert.AreEqual(3, data.table.Count(0));
            Assert.AreEqual(4, data.table.Count(1));
        }

        [TestMethod]
        public void GetRange()
        {
            var data = DoubleTestDataTable(3, 4);

            var emptyData = DoubleTestDataTable(0, 0);

            //Out-of-range cases
            Assert.AreEqual(0, emptyData.table.GetRange(new Point4D(-1, 3, 1, 3)).Count);
            Assert.AreEqual(0, emptyData.table.GetRange(new Point4D(1, 3, -1, 3)).Count);
            Assert.AreEqual(0, emptyData.table.GetRange(new Point4D(1, 100, 1, 3)).Count);
            Assert.AreEqual(0, emptyData.table.GetRange(new Point4D(1, 3, 1, 100)).Count);
            Assert.AreEqual(0, emptyData.table.GetRange(new Point4D(3, 1, 1, 3)).Count);
            Assert.AreEqual(0, emptyData.table.GetRange(new Point4D(1, 3, 3, 1)).Count);

            Assert.AreEqual(0, data.table.GetRange(new Point4D(-1, 3, 1, 3)).Count);
            Assert.AreEqual(0, data.table.GetRange(new Point4D(1, 3, -1, 3)).Count);
            Assert.AreEqual(0, data.table.GetRange(new Point4D(1, 100, 1, 3)).Count);
            Assert.AreEqual(0, data.table.GetRange(new Point4D(1, 3, 1, 100)).Count);
            Assert.AreEqual(0, data.table.GetRange(new Point4D(3, 1, 1, 3)).Count);
            Assert.AreEqual(0, data.table.GetRange(new Point4D(1, 3, 3, 1)).Count);

            var range = data.table.GetRange(new Point4D(2, 3, 2, 3));
            double[,] compare = new double[2, 2]
            {
                { 5001, 5002 },
                { 10001, 10002 },
            };

            Assert.AreEqual(2, range.Count);
            Assert.AreEqual(2, range[0].Count);

            for (int r = 0; r < 2; ++r)
                for (int c = 0; c < 2; ++c)
                    Assert.AreEqual(compare[r, c], range[r][c]);

        }

        [TestMethod]
        public void GetRow()
        {
            var data = DoubleTestDataTable(3, 4);

            Assert.ThrowsException<IndexOutOfRangeException>(() => { data.table.GetRow(-1); });
            Assert.ThrowsException<IndexOutOfRangeException>(() => { data.table.GetRow(100); });

            var row = data.table.GetRow(1).ToList();
            double[] compare = new double[] { 5000, 5001, 5002, 5003 };

            Assert.AreEqual(compare.Length, row.Count);
            for (int i = 0; i < row.Count; ++i)
                Assert.AreEqual(compare[i], row[i]);
        }

        [TestMethod]
        public void GetColumn()
        {
            var data = DoubleTestDataTable(3, 4);

            Assert.ThrowsException<IndexOutOfRangeException>(() => { data.table.GetColumn(-1).Count(); });
            Assert.ThrowsException<IndexOutOfRangeException>(() => { data.table.GetColumn(100).Count(); });

            var col = data.table.GetColumn(1).ToList();
            double[] compare = new double[] { 1, 5001, 10001 };

            Assert.AreEqual(compare.Length, col.Count);
            for (int i = 0; i < col.Count; ++i)
                Assert.AreEqual(compare[i], col[i]);
        }

        [TestMethod]
        public void Index()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            Assert.ThrowsException<IndexOutOfRangeException>(() => data.table[-1, 1]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => data.table[1, -1]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => data.table[100, 1]);
            Assert.ThrowsException<IndexOutOfRangeException>(() => data.table[1, 100]);

            Assert.ThrowsException<IndexOutOfRangeException>(() => data.table[-1, 1] = 0);
            Assert.ThrowsException<IndexOutOfRangeException>(() => data.table[1, -1] = 0);
            Assert.ThrowsException<IndexOutOfRangeException>(() => data.table[100, 1] = 0);
            Assert.ThrowsException<IndexOutOfRangeException>(() => data.table[1, 100] = 0);

            data.table[1, 1] = 99999;
            events.AssertEventCount(0, 0, 0, 1);
            Assert.AreEqual(1, events.ValueChangedArgs[0].Column);
            Assert.AreEqual(1, events.ValueChangedArgs[0].Row);

            Assert.AreEqual(99999, data.table[1, 1]);
        }

        [TestMethod]
        public void AggregateSumRows()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Sum, true);

            //verify
            Assert.AreEqual(6, data.RowHeaders.Count);
            Assert.AreEqual(2, data.ColumnHeaders.Count);

            Assert.AreEqual("r3", data.RowHeaders[0].Name);
            Assert.AreEqual("r1", data.RowHeaders[1].Name);
            Assert.AreEqual("r3", data.RowHeaders[2].Name);
            Assert.AreEqual("r2", data.RowHeaders[3].Name);
            Assert.AreEqual("r1", data.RowHeaders[4].Name);
            Assert.AreEqual("r1", data.RowHeaders[5].Name);

            Assert.AreEqual("ru3", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[3].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[4].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[5].Unit);

            Assert.AreEqual("SUM c1", data.ColumnHeaders[0].Name);
            Assert.AreEqual("SUM c2", data.ColumnHeaders[1].Name);

            Assert.AreEqual("u1", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[1].Unit);

            object[,] expected = new object[6, 2]
            {
                { 7.0, 8.0 },
                { 17.0, 23.0 },
                { 27.0, 38.0 },
                { 37.0, 53.0 },
                { 47.0, 68.0 },
                { 57.0, 83.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);

            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateSumColumns()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Sum, false);

            //verify
            Assert.AreEqual(3, data.RowHeaders.Count);
            Assert.AreEqual(5, data.ColumnHeaders.Count);

            Assert.AreEqual("SUM r1", data.RowHeaders[0].Name);
            Assert.AreEqual("SUM r2", data.RowHeaders[1].Name);
            Assert.AreEqual("SUM r3", data.RowHeaders[2].Name);

            Assert.AreEqual("ru1", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);

            Assert.AreEqual("c2", data.ColumnHeaders[0].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[1].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[2].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[3].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[4].Name);

            Assert.AreEqual("u2", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[1].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[2].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[3].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[4].Unit);

            object[,] expected = new object[3, 5]
            {
                { 53.0, 56.0, 59.0, 62.0, 65.0 },
                { 16.0, 17.0, 18.0, 19.0, 20.0 },
                { 12.0, 14.0, 16.0, 18.0, 20.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);
            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateAvgRows()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Average, true);

            //verify
            Assert.AreEqual(6, data.RowHeaders.Count);
            Assert.AreEqual(2, data.ColumnHeaders.Count);

            Assert.AreEqual("r3", data.RowHeaders[0].Name);
            Assert.AreEqual("r1", data.RowHeaders[1].Name);
            Assert.AreEqual("r3", data.RowHeaders[2].Name);
            Assert.AreEqual("r2", data.RowHeaders[3].Name);
            Assert.AreEqual("r1", data.RowHeaders[4].Name);
            Assert.AreEqual("r1", data.RowHeaders[5].Name);

            Assert.AreEqual("ru3", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[3].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[4].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[5].Unit);

            Assert.AreEqual("AVG c1", data.ColumnHeaders[0].Name);
            Assert.AreEqual("AVG c2", data.ColumnHeaders[1].Name);

            Assert.AreEqual("u1", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[1].Unit);

            object[,] expected = new object[6, 2]
            {
                { 3.5, 8.0/3.0 },
                { 8.5, 23/3.0 },
                { 13.5, 38/3.0 },
                { 18.5, 53/3.0 },
                { 23.5, 68/3.0 },
                { 28.5, 83/3.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);

            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateAvgColumns()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Average, false);

            //verify
            Assert.AreEqual(3, data.RowHeaders.Count);
            Assert.AreEqual(5, data.ColumnHeaders.Count);

            Assert.AreEqual("AVG r1", data.RowHeaders[0].Name);
            Assert.AreEqual("AVG r2", data.RowHeaders[1].Name);
            Assert.AreEqual("AVG r3", data.RowHeaders[2].Name);

            Assert.AreEqual("ru1", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);

            Assert.AreEqual("c2", data.ColumnHeaders[0].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[1].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[2].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[3].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[4].Name);

            Assert.AreEqual("u2", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[1].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[2].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[3].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[4].Unit);

            object[,] expected = new object[3, 5]
            {
                { 53/3.0, 56/3.0, 59/3.0, 62/3.0, 65/3.0 },
                { 16.0, 17.0, 18.0, 19.0, 20.0 },
                { 6.0, 7.0, 8.0, 9.0, 10.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);
            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateCountRows()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Count, true);

            //verify
            Assert.AreEqual(6, data.RowHeaders.Count);
            Assert.AreEqual(2, data.ColumnHeaders.Count);

            Assert.AreEqual("r3", data.RowHeaders[0].Name);
            Assert.AreEqual("r1", data.RowHeaders[1].Name);
            Assert.AreEqual("r3", data.RowHeaders[2].Name);
            Assert.AreEqual("r2", data.RowHeaders[3].Name);
            Assert.AreEqual("r1", data.RowHeaders[4].Name);
            Assert.AreEqual("r1", data.RowHeaders[5].Name);

            Assert.AreEqual("ru3", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[3].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[4].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[5].Unit);

            Assert.AreEqual("COUNT c1", data.ColumnHeaders[0].Name);
            Assert.AreEqual("COUNT c2", data.ColumnHeaders[1].Name);

            Assert.AreEqual("u1", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[1].Unit);

            object[,] expected = new object[6, 2]
            {
                { 2.0, 3.0 },
                { 2.0, 3.0 },
                { 2.0, 3.0 },
                { 2.0, 3.0 },
                { 2.0, 3.0 },
                { 2.0, 3.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);

            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateCountColumns()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Count, false);

            //verify
            Assert.AreEqual(3, data.RowHeaders.Count);
            Assert.AreEqual(5, data.ColumnHeaders.Count);

            Assert.AreEqual("COUNT r1", data.RowHeaders[0].Name);
            Assert.AreEqual("COUNT r2", data.RowHeaders[1].Name);
            Assert.AreEqual("COUNT r3", data.RowHeaders[2].Name);

            Assert.AreEqual("ru1", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);

            Assert.AreEqual("c2", data.ColumnHeaders[0].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[1].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[2].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[3].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[4].Name);

            Assert.AreEqual("u2", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[1].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[2].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[3].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[4].Unit);

            object[,] expected = new object[3, 5]
            {
                { 3.0, 3.0, 3.0, 3.0, 3.0 },
                { 1.0, 1.0, 1.0, 1.0, 1.0 },
                { 2.0, 2.0, 2.0, 2.0, 2.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);
            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateMinRows()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Min, true);

            //verify
            Assert.AreEqual(6, data.RowHeaders.Count);
            Assert.AreEqual(2, data.ColumnHeaders.Count);

            Assert.AreEqual("r3", data.RowHeaders[0].Name);
            Assert.AreEqual("r1", data.RowHeaders[1].Name);
            Assert.AreEqual("r3", data.RowHeaders[2].Name);
            Assert.AreEqual("r2", data.RowHeaders[3].Name);
            Assert.AreEqual("r1", data.RowHeaders[4].Name);
            Assert.AreEqual("r1", data.RowHeaders[5].Name);

            Assert.AreEqual("ru3", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[3].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[4].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[5].Unit);

            Assert.AreEqual("MIN c1", data.ColumnHeaders[0].Name);
            Assert.AreEqual("MIN c2", data.ColumnHeaders[1].Name);

            Assert.AreEqual("u1", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[1].Unit);

            object[,] expected = new object[6, 2]
            {
                { 2.0, 1.0 },
                { 7.0, 6.0 },
                { 12.0, 11.0 },
                { 17.0, 16.0 },
                { 22.0, 21.0 },
                { 27.0, 26.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);

            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateMinColumns()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Min, false);

            //verify
            Assert.AreEqual(3, data.RowHeaders.Count);
            Assert.AreEqual(5, data.ColumnHeaders.Count);

            Assert.AreEqual("MIN r1", data.RowHeaders[0].Name);
            Assert.AreEqual("MIN r2", data.RowHeaders[1].Name);
            Assert.AreEqual("MIN r3", data.RowHeaders[2].Name);

            Assert.AreEqual("ru1", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);

            Assert.AreEqual("c2", data.ColumnHeaders[0].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[1].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[2].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[3].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[4].Name);

            Assert.AreEqual("u2", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[1].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[2].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[3].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[4].Unit);

            object[,] expected = new object[3, 5]
            {
                { 6.0, 7.0, 8.0, 9.0, 10.0 },
                { 16.0, 17.0, 18.0, 19.0, 20.0 },
                { 1.0, 2.0, 3.0, 4.0, 5.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);
            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateMaxRows()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Max, true);

            //verify
            Assert.AreEqual(6, data.RowHeaders.Count);
            Assert.AreEqual(2, data.ColumnHeaders.Count);

            Assert.AreEqual("r3", data.RowHeaders[0].Name);
            Assert.AreEqual("r1", data.RowHeaders[1].Name);
            Assert.AreEqual("r3", data.RowHeaders[2].Name);
            Assert.AreEqual("r2", data.RowHeaders[3].Name);
            Assert.AreEqual("r1", data.RowHeaders[4].Name);
            Assert.AreEqual("r1", data.RowHeaders[5].Name);

            Assert.AreEqual("ru3", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[3].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[4].Unit);
            Assert.AreEqual("ru1", data.RowHeaders[5].Unit);

            Assert.AreEqual("MAX c1", data.ColumnHeaders[0].Name);
            Assert.AreEqual("MAX c2", data.ColumnHeaders[1].Name);

            Assert.AreEqual("u1", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[1].Unit);

            object[,] expected = new object[6, 2]
            {
                { 5.0, 4.0 },
                { 10.0, 9.0 },
                { 15.0, 14.0 },
                { 20.0, 19.0 },
                { 25.0, 24.0 },
                { 30.0, 29.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);

            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void AggregateMaxColumns()
        {
            var data = TestDataTableAggregate();
            var events = new BigTableEventCounter(data);

            //Apply
            ComponentParameters.ApplyAggregationFunction(data, SimAggregationFunction.Max, false);

            //verify
            Assert.AreEqual(3, data.RowHeaders.Count);
            Assert.AreEqual(5, data.ColumnHeaders.Count);

            Assert.AreEqual("MAX r1", data.RowHeaders[0].Name);
            Assert.AreEqual("MAX r2", data.RowHeaders[1].Name);
            Assert.AreEqual("MAX r3", data.RowHeaders[2].Name);

            Assert.AreEqual("ru1", data.RowHeaders[0].Unit);
            Assert.AreEqual("ru2", data.RowHeaders[1].Unit);
            Assert.AreEqual("ru3", data.RowHeaders[2].Unit);

            Assert.AreEqual("c2", data.ColumnHeaders[0].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[1].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[2].Name);
            Assert.AreEqual("c2", data.ColumnHeaders[3].Name);
            Assert.AreEqual("c1", data.ColumnHeaders[4].Name);

            Assert.AreEqual("u2", data.ColumnHeaders[0].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[1].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[2].Unit);
            Assert.AreEqual("u2", data.ColumnHeaders[3].Unit);
            Assert.AreEqual("u1", data.ColumnHeaders[4].Unit);

            object[,] expected = new object[3, 5]
            {
                { 26.0, 27.0, 28.0, 29.0, 30.0 },
                { 16.0, 17.0, 18.0, 19.0, 20.0 },
                { 11.0, 12.0, 13.0, 14.0, 15.0 },
            };

            AssertUtil.ContainEqualValues(expected, data);
            events.AssertEventCount(2, 1, 0, 0);
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.RowHeaders)));
            Assert.IsTrue(events.PropertyChangedArgs.Contains(nameof(SimMultiValueBigTable.ColumnHeaders)));
            Assert.AreEqual(0, events.ResizedArgs[0].ColumnStartIndex);
            Assert.AreEqual(0, events.ResizedArgs[0].RowStartIndex);
            Assert.AreEqual(SimMultiValueBigTable.ResizeDirection.Both, events.ResizedArgs[0].ResizeDirection);
        }

        [TestMethod]
        public void ModifyHeader()
        {
            var data = DoubleTestDataTable(3, 4);
            var events = new BigTableEventCounter(data.table);

            data.table.RowHeaders[1].Name = "NewName";
            Assert.AreEqual("NewName", data.table.RowHeaders[1].Name);
            events.AssertEventCount(0, 0, 1, 0);
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Rows, events.HeaderValueChangedArgs[0].Axis);
            Assert.AreEqual(1, events.HeaderValueChangedArgs[0].Index);

            data.table.RowHeaders[1].Unit = "NewUnit";
            Assert.AreEqual("NewUnit", data.table.RowHeaders[1].Unit);
            events.AssertEventCount(0, 0, 2, 0);
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Rows, events.HeaderValueChangedArgs[1].Axis);
            Assert.AreEqual(1, events.HeaderValueChangedArgs[1].Index);


            data.table.ColumnHeaders[1].Name = "NewName";
            Assert.AreEqual("NewName", data.table.ColumnHeaders[1].Name);
            events.AssertEventCount(0, 0, 3, 0);
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Columns, events.HeaderValueChangedArgs[2].Axis);
            Assert.AreEqual(1, events.HeaderValueChangedArgs[2].Index);

            data.table.ColumnHeaders[1].Unit = "NewUnit";
            Assert.AreEqual("NewUnit", data.table.ColumnHeaders[1].Unit);
            events.AssertEventCount(0, 0, 4, 0);
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Columns, events.HeaderValueChangedArgs[3].Axis);
            Assert.AreEqual(1, events.HeaderValueChangedArgs[3].Index);
        }
    }
}