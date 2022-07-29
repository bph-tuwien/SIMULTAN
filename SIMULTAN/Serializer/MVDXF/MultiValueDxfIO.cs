using SIMULTAN.Data;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Serializer.MVDXF
{
    internal static class MultiValueDxfIO
    {
        #region Syntax

        private static DXFEntityParserElement<SimMultiValue> bigTableEntityElement =
            new DXFEntityParserElement<SimMultiValue>(ParamStructTypes.BIG_TABLE,
                (data, info) => ParseBigTable(data, info),
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_NAME),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_X),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_Y),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_Z),

                    new DXFArrayEntryParserElement<string>(MultiValueSaveCode.MV_COL_NAMES, ParamStructCommonSaveCode.STRING_VALUE) { MinVersion = 12 },
                    new DXFArrayEntryParserElement<string>(MultiValueSaveCode.MV_COL_NAMES, ParamStructCommonSaveCode.X_VALUE) { MaxVersion = 11 },
                    new DXFArrayEntryParserElement<string>(MultiValueSaveCode.MV_COL_UNITS, ParamStructCommonSaveCode.STRING_VALUE) { MinVersion = 12 },
                    new DXFArrayEntryParserElement<string>(MultiValueSaveCode.MV_COL_UNITS, ParamStructCommonSaveCode.X_VALUE) { MaxVersion = 11 },

                    new DXFArrayEntryParserElement<string>(MultiValueSaveCode.MV_ROW_NAMES, ParamStructCommonSaveCode.STRING_VALUE),
                    new DXFArrayEntryParserElement<string>(MultiValueSaveCode.MV_ROW_UNITS, ParamStructCommonSaveCode.STRING_VALUE),

                    new ParallelBigTableSerializerElement(),
                    new DXFMultiLineTextElement(MultiValueSaveCode.ADDITIONAL_INFO),
                });

        private static DXFEntityParserElement<SimMultiValue> field3DEntityElement =
            new DXFEntityParserElement<SimMultiValue>(ParamStructTypes.VALUE_FIELD,
                (data, info) => ParseField3D(data, info),
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_NAME),
                    new DXFSingleEntryParserElement<bool>(MultiValueSaveCode.MV_CANINTERPOLATE),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_X),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_Y),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_Z),

                    new DXFArrayEntryParserElement<double>(MultiValueSaveCode.MV_XAXIS, ParamStructCommonSaveCode.X_VALUE),
                    new DXFArrayEntryParserElement<double>(MultiValueSaveCode.MV_YAXIS, ParamStructCommonSaveCode.X_VALUE),
                    new DXFArrayEntryParserElement<double>(MultiValueSaveCode.MV_ZAXIS, ParamStructCommonSaveCode.X_VALUE),

                    new DXFStructArrayEntryParserElement<KeyValuePair<Point3D, double>>(MultiValueSaveCode.MVDATA_ROW_COUNT,
                        (data, info) => ParseField3DDataEntry(data, info),
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<double>(ParamStructCommonSaveCode.X_VALUE),
                            new DXFSingleEntryParserElement<double>(ParamStructCommonSaveCode.Y_VALUE),
                            new DXFSingleEntryParserElement<double>(ParamStructCommonSaveCode.Z_VALUE),
                            new DXFSingleEntryParserElement<double>(ParamStructCommonSaveCode.W_VALUE),
                        }),
                });

        private static DXFEntityParserElement<SimMultiValue> functionFieldEntityElement =
            new DXFEntityParserElement<SimMultiValue>(ParamStructTypes.FUNCTION_FIELD,
                (data, info) => ParseFunctionField(data, info),
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_NAME),
                    new DXFSingleEntryParserElement<bool>(MultiValueSaveCode.MV_CANINTERPOLATE),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_X),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_Y),
                    new DXFSingleEntryParserElement<string>(MultiValueSaveCode.MV_UNIT_Z),

                    new DXFSingleEntryParserElement<double>(MultiValueSaveCode.MV_MIN_X),
                    new DXFSingleEntryParserElement<double>(MultiValueSaveCode.MV_MAX_X),
                    new DXFSingleEntryParserElement<double>(MultiValueSaveCode.MV_MIN_Y),
                    new DXFSingleEntryParserElement<double>(MultiValueSaveCode.MV_MAX_Y),

                    new DXFArrayEntryParserElement<double>(MultiValueSaveCode.MV_ZAXIS, ParamStructCommonSaveCode.X_VALUE),
                    new DXFNestedListEntryParserElement<Point3D>(MultiValueSaveCode.MVDATA_ROW_COUNT, ParamStructCommonSaveCode.W_VALUE,
                        data =>
                        {
                            return new Point3D(
                                data.Get<double>(ParamStructCommonSaveCode.X_VALUE, 0.0),
                                data.Get<double>(ParamStructCommonSaveCode.Y_VALUE, 0.0),
                                data.Get<double>(ParamStructCommonSaveCode.Z_VALUE, 0.0)
                                );
                        }, new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<double>(ParamStructCommonSaveCode.X_VALUE),
                            new DXFSingleEntryParserElement<double>(ParamStructCommonSaveCode.Y_VALUE),
                            new DXFSingleEntryParserElement<double>(ParamStructCommonSaveCode.Z_VALUE),
                        }),
                    new DXFArrayEntryParserElement<string>(MultiValueSaveCode.MV_ROW_NAMES, ParamStructCommonSaveCode.STRING_VALUE),
                });


        private static DXFSectionParserElement<SimMultiValue> multiValueSection =
            new DXFSectionParserElement<SimMultiValue>(ParamStructTypes.ENTITY_SECTION, new DXFEntityParserElement<SimMultiValue>[]
            {
                bigTableEntityElement,
                field3DEntityElement,
                functionFieldEntityElement
            });

        #endregion

        internal static void Write(FileInfo file, SimMultiValueCollection collection)
        {
            Write(file, (IEnumerable<SimMultiValue>)collection);
        }
        internal static void Write(FileInfo file, IEnumerable<SimMultiValue> collection)
        {
            using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(fs))
                {
                    Write(writer, collection);
                }
            }
        }
        internal static void Write(DXFStreamWriter writer, IEnumerable<SimMultiValue> collection)
        {
            //File header
            writer.WriteVersionSection();

            //Data
            writer.StartSection(ParamStructTypes.ENTITY_SECTION);
            foreach (var mv in collection)
            {
                if (mv is SimMultiValueBigTable bt)
                    WriteBigTable(bt, writer);
                else if (mv is SimMultiValueField3D field)
                    WriteField3D(field, writer);
                else if (mv is SimMultiValueFunction func)
                    WriteFunctionField(func, writer);
                else
                    throw new NotSupportedException("Unsupported data type");
            }
            writer.EndSection();

            //EOF
            writer.WriteEOF();
        }

        internal static void Read(FileInfo file, DXFParserInfo parserInfo)
        {
            parserInfo.CurrentFile = file;
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
            catch (Exception) //Happens in very old version (< version 4) where the version section wasn't present
            {
                reader.Seek(0);
            }

            //Data section
            var multiValues = multiValueSection.Parse(reader, parserInfo);

            parserInfo.ProjectData.ValueManager.StartLoading();
            foreach (var mv in multiValues)
            {
                if (mv != null)
                    parserInfo.ProjectData.ValueManager.Add(mv);
            }
            parserInfo.ProjectData.ValueManager.EndLoading();

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.FinishLog();
        }


        internal static void WriteBigTable(SimMultiValueBigTable table, DXFStreamWriter sw)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            if (sw == null)
                throw new ArgumentNullException(nameof(sw));

            //ENTITY Header
            sw.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.BIG_TABLE);
            sw.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimMultiValueBigTable));
            sw.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, table.Id.LocalId);

            //MultiValue Header
            sw.Write(MultiValueSaveCode.MV_TYPE, table.MVType);
            sw.Write(MultiValueSaveCode.MV_NAME, table.Name);
            sw.Write(MultiValueSaveCode.MV_CANINTERPOLATE, table.CanInterpolate);

            //Axis Description
            sw.Write(MultiValueSaveCode.MV_UNIT_X, table.UnitX);
            sw.Write(MultiValueSaveCode.MV_UNIT_Y, table.UnitY);
            sw.Write(MultiValueSaveCode.MV_UNIT_Z, table.UnitZ);

            //Column Names
            sw.WriteArray(MultiValueSaveCode.MV_COL_NAMES, table.ColumnHeaders, (x, asw) =>
            {
                asw.Write(ParamStructCommonSaveCode.STRING_VALUE, x.Name);
            });

            //Column Units
            sw.WriteArray(MultiValueSaveCode.MV_COL_UNITS, table.ColumnHeaders, (x, asw) =>
            {
                asw.Write(ParamStructCommonSaveCode.STRING_VALUE, x.Unit);
            });

            //Row Names
            sw.WriteArray(MultiValueSaveCode.MV_ROW_NAMES, table.RowHeaders, (x, asw) =>
            {
                asw.Write(ParamStructCommonSaveCode.STRING_VALUE, x.Name);
            });

            //Row Units
            sw.WriteArray(MultiValueSaveCode.MV_ROW_UNITS, table.RowHeaders, (x, asw) =>
            {
                asw.Write(ParamStructCommonSaveCode.STRING_VALUE, x.Unit);
            });

            //Data
            sw.Write(MultiValueSaveCode.MVDATA_ROW_COUNT, table.Values.Count);
            sw.Write(MultiValueSaveCode.MVDATA_COLUMN_COUNT, table.Values.Count > 0 ? table.Values[0].Count : 0);

            ParallelBigTableSerializer ps = new ParallelBigTableSerializer(table.Values, 1000, 
                (int)MultiValueSaveCode.MV_DATA);
            Task value_task = Task.Run(async () => await ps.SerializeValuesAsync(sw));
            value_task.Wait();

            //Additional Info
            sw.WriteMultilineText(MultiValueSaveCode.ADDITIONAL_INFO, table.AdditionalInfo);
        }
        private static SimMultiValueBigTable ParseBigTable(DXFParserResultSet data, DXFParserInfo info)
        {
            var localId = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);

            string name = data.Get<string>(MultiValueSaveCode.MV_NAME, string.Empty);
            string unitX = data.Get<string>(MultiValueSaveCode.MV_UNIT_X, string.Empty);
            string unitY = data.Get<string>(MultiValueSaveCode.MV_UNIT_Y, string.Empty);
            string additionalInfo = data.Get<string>(MultiValueSaveCode.ADDITIONAL_INFO, string.Empty);

            //Translate Ids
            if (info.TranslationExists(typeof(SimMultiValue), localId))
            {
                info.Log(string.Format("Multiple ValueFields with Id {0} found. Name=\"{1}\" Original Name=\"{2}\"",
                        localId, name,
                        info.ProjectData.IdGenerator.GetById<SimMultiValue>(
                            new SimId(info.GlobalId, info.TranslateId(typeof(SimMultiValue), localId))
                            ).Name
                        ));
            }

            localId = info.TranslateId(typeof(SimMultiValue), localId);

            try
            {
                //Construct BigTable Headers
                var rowHeaders = ParseHeader(data, MultiValueSaveCode.MV_ROW_NAMES, MultiValueSaveCode.MV_ROW_UNITS);
                var columnHeaders = ParseHeader(data, MultiValueSaveCode.MV_COL_NAMES, MultiValueSaveCode.MV_COL_UNITS);

                //Data
                List<List<double>> values = new List<List<double>>(rowHeaders.Count);

                var valueTasks = data.Get<Task<List<List<double>>>[]>(MultiValueSaveCode.MVDATA_ROW_COUNT, null);
                if (valueTasks != null)
                {
                    Task.WaitAll(valueTasks);
                    foreach (var task in valueTasks)
                        values.AddRange(task.Result);
                }

                //Fixes that old tables store column headers for the row-headers column (column "-1")
                if (values.Count > 0)
                    if (values[0].Count == columnHeaders.Count - 1)
                        columnHeaders.RemoveAt(0);

                //Append missing row headers (happens with old versions)
                while (rowHeaders.Count < values.Count)
                {
                    rowHeaders.Add(new SimMultiValueBigTableHeader("-", "-"));
                }

                //Construct table
                var table = new SimMultiValueBigTable(localId, name, unitX, unitY, columnHeaders, rowHeaders, values, additionalInfo);

                return table;
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimMultiValueBigTable with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    localId, name, e.Message, e.StackTrace
                    ));
            }

            return null;
        }
        private static List<SimMultiValueBigTableHeader> ParseHeader(DXFParserResultSet data, MultiValueSaveCode nameCode, MultiValueSaveCode unitCode)
        {
            string[] names = data.Get<string[]>(nameCode, new string[] { });
            string[] units = data.Get<string[]>(unitCode, new string[] { });

            if (names.Length != units.Length)
                throw new Exception("Header Name and Unit count does not match");

            List<SimMultiValueBigTableHeader> header = new List<SimMultiValueBigTableHeader>(names.Length);

            for (int i = 0; i < names.Length; i++)
            {
                header.Add(new SimMultiValueBigTableHeader(names[i], units[i]));
            }

            return header;
        }

        internal static void WriteField3D(SimMultiValueField3D field, DXFStreamWriter sw)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (sw == null)
                throw new ArgumentNullException(nameof(sw));

            //ENTITY Header
            sw.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.VALUE_FIELD);
            sw.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimMultiValueField3D));
            sw.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, field.Id.LocalId);

            //MultiValue Header
            sw.Write(MultiValueSaveCode.MV_TYPE, field.MVType);
            sw.Write(MultiValueSaveCode.MV_NAME, field.Name);
            sw.Write(MultiValueSaveCode.MV_CANINTERPOLATE, field.CanInterpolate);

            //Axis Description
            sw.Write(MultiValueSaveCode.MV_UNIT_X, field.UnitX);
            sw.Write(MultiValueSaveCode.MV_UNIT_Y, field.UnitY);
            sw.Write(MultiValueSaveCode.MV_UNIT_Z, field.UnitZ);

            //Size
            sw.Write(MultiValueSaveCode.MV_NRX, field.XAxis.Count);
            sw.Write(MultiValueSaveCode.MV_NRY, field.YAxis.Count);
            sw.Write(MultiValueSaveCode.MV_NRZ, field.ZAxis.Count);

            //Axis
            sw.WriteArray(MultiValueSaveCode.MV_XAXIS, field.XAxis, (x, aws) =>
            {
                aws.Write(ParamStructCommonSaveCode.X_VALUE, x);
            });
            sw.WriteArray(MultiValueSaveCode.MV_YAXIS, field.YAxis, (x, aws) =>
            {
                aws.Write(ParamStructCommonSaveCode.X_VALUE, x);
            });
            sw.WriteArray(MultiValueSaveCode.MV_ZAXIS, field.ZAxis, (x, aws) =>
            {
                aws.Write(ParamStructCommonSaveCode.X_VALUE, x);
            });

            //Data
            sw.WriteArray(MultiValueSaveCode.MVDATA_ROW_COUNT, field.Field, (x, aws) =>
            {
                aws.Write(ParamStructCommonSaveCode.X_VALUE, (double)x.Key.X);
                aws.Write(ParamStructCommonSaveCode.Y_VALUE, (double)x.Key.Y);
                aws.Write(ParamStructCommonSaveCode.Z_VALUE, (double)x.Key.Z);
                aws.Write(ParamStructCommonSaveCode.W_VALUE, x.Value);
            });

        }
        private static SimMultiValue ParseField3D(DXFParserResultSet data, DXFParserInfo info)
        {
            var localId = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);

            string name = data.Get<string>(MultiValueSaveCode.MV_NAME, string.Empty);
            string unitX = data.Get<string>(MultiValueSaveCode.MV_UNIT_X, string.Empty);
            string unitY = data.Get<string>(MultiValueSaveCode.MV_UNIT_Y, string.Empty);
            string unitZ = data.Get<string>(MultiValueSaveCode.MV_UNIT_Z, string.Empty);
            bool canInterpolate = data.Get<bool>(MultiValueSaveCode.MV_CANINTERPOLATE, false);

            double[] xAxis = data.Get<double[]>(MultiValueSaveCode.MV_XAXIS, null);
            double[] yAxis = data.Get<double[]>(MultiValueSaveCode.MV_YAXIS, null);
            double[] zAxis = data.Get<double[]>(MultiValueSaveCode.MV_ZAXIS, null);

            KeyValuePair<Point3D, double>[] field = data.Get<KeyValuePair<Point3D, double>[]>(MultiValueSaveCode.MVDATA_ROW_COUNT, null);

            //Translate Ids
            if (info.TranslationExists(typeof(SimMultiValue), localId))
            {
                info.Log(string.Format("Multiple ValueFields with Id {0} found. Name=\"{1}\" Original Name=\"{2}\"",
                        localId, name,
                        info.ProjectData.IdGenerator.GetById<SimMultiValue>(
                            new SimId(info.GlobalId, info.TranslateId(typeof(SimMultiValue), localId))
                            ).Name
                        ));
            }

            localId = info.TranslateId(typeof(SimMultiValue), localId);

            try
            {
                var field3D = new SimMultiValueField3D(localId, name, xAxis, unitX,
                    yAxis, unitY, zAxis, unitZ, field.ToDictionary(x => x.Key, x => x.Value), canInterpolate);
                return field3D;
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimMultiValueField3D with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    localId, name, e.Message, e.StackTrace
                    ));
            }

            return null;
        }
        private static KeyValuePair<Point3D, double> ParseField3DDataEntry(DXFParserResultSet data, DXFParserInfo info)
        {
            double x = data.Get(ParamStructCommonSaveCode.X_VALUE, 0.0);
            double y = data.Get(ParamStructCommonSaveCode.Y_VALUE, 0.0);
            double z = data.Get(ParamStructCommonSaveCode.Z_VALUE, 0.0);
            double value = data.Get(ParamStructCommonSaveCode.W_VALUE, 0.0);

            return new KeyValuePair<Point3D, double>(new Point3D(x, y, z), value);
        }

        internal static void WriteFunctionField(SimMultiValueFunction field, DXFStreamWriter sw)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (sw == null)
                throw new ArgumentNullException(nameof(sw));

            //ENTITY Header
            sw.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.FUNCTION_FIELD);
            sw.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(SimMultiValueFunction));
            sw.Write(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, field.Id.LocalId);

            //MultiValue Header
            sw.Write(MultiValueSaveCode.MV_TYPE, field.MVType);
            sw.Write(MultiValueSaveCode.MV_NAME, field.Name);
            sw.Write(MultiValueSaveCode.MV_CANINTERPOLATE, field.CanInterpolate);

            //Axis Description
            sw.Write(MultiValueSaveCode.MV_UNIT_X, field.UnitX);
            sw.Write(MultiValueSaveCode.MV_UNIT_Y, field.UnitY);
            sw.Write(MultiValueSaveCode.MV_UNIT_Z, field.UnitZ);

            //Definition Space
            sw.Write(MultiValueSaveCode.MV_MIN_X, field.Range.Minimum.X);
            sw.Write(MultiValueSaveCode.MV_MAX_X, field.Range.Maximum.X);
            sw.Write(MultiValueSaveCode.MV_MIN_Y, field.Range.Minimum.Y);
            sw.Write(MultiValueSaveCode.MV_MAX_Y, field.Range.Maximum.Y);
            sw.Write(MultiValueSaveCode.MV_MIN_Z, field.Range.Minimum.Z);
            sw.Write(MultiValueSaveCode.MV_MAX_Z, field.Range.Maximum.Z);

            //Z-Axis
            sw.WriteArray(MultiValueSaveCode.MV_ZAXIS, field.ZAxis, (x, asw) =>
            {
                asw.Write(ParamStructCommonSaveCode.X_VALUE, x);
            });

            //Graphs positions
            sw.WriteNestedList<SimMultiValueFunctionPointList, Point3D>(MultiValueSaveCode.MVDATA_ROW_COUNT, 
                field.Graphs.Select(x => x.Points), (x, ccode, lsw) => 
                {
                    lsw.Write(ParamStructCommonSaveCode.X_VALUE, x.X);
                    lsw.Write(ParamStructCommonSaveCode.Y_VALUE, x.Y);
                    lsw.Write(ParamStructCommonSaveCode.Z_VALUE, x.Z);
                    lsw.Write(ParamStructCommonSaveCode.W_VALUE, ccode);
                });

            //Graph names
            sw.WriteArray(MultiValueSaveCode.MV_ROW_NAMES, field.Graphs, (x, asw) =>
            {
                sw.Write(ParamStructCommonSaveCode.STRING_VALUE, x.Name);
            });
        }
        private static SimMultiValueFunction ParseFunctionField(DXFParserResultSet data, DXFParserInfo info)
        {
            var localId = data.Get<long>(ParamStructCommonSaveCode.ENTITY_LOCAL_ID, 0);

            string name = data.Get<string>(MultiValueSaveCode.MV_NAME, string.Empty);
            string unitX = data.Get<string>(MultiValueSaveCode.MV_UNIT_X, string.Empty);
            string unitY = data.Get<string>(MultiValueSaveCode.MV_UNIT_Y, string.Empty);
            string unitZ = data.Get<string>(MultiValueSaveCode.MV_UNIT_Z, string.Empty);
            bool canInterpolate = data.Get<bool>(MultiValueSaveCode.MV_CANINTERPOLATE, false);

            double minX = data.Get<double>(MultiValueSaveCode.MV_MIN_X, 0.0);
            double maxX = data.Get<double>(MultiValueSaveCode.MV_MAX_X, 0.0);
            double minY = data.Get<double>(MultiValueSaveCode.MV_MIN_Y, 0.0);
            double maxY = data.Get<double>(MultiValueSaveCode.MV_MAX_Y, 0.0);

            Rect bounds = new Rect(minX, minY, maxX - minX, maxY - minY);

            double[] zaxis = data.Get<double[]>(MultiValueSaveCode.MV_ZAXIS, new double[] { });

            List<List<Point3D>> graphPoints = data.Get<List<List<Point3D>>>(MultiValueSaveCode.MVDATA_ROW_COUNT, new List<List<Point3D>>());
            string[] graphNames = data.Get<string[]>(MultiValueSaveCode.MV_ROW_NAMES, new string[] { });

            //Translate Ids
            if (info.TranslationExists(typeof(SimMultiValue), localId))
            {
                info.Log(string.Format("Multiple ValueFields with Id {0} found. Name=\"{1}\" Original Name=\"{2}\"",
                        localId, name,
                        info.ProjectData.IdGenerator.GetById<SimMultiValue>(
                            new SimId(info.GlobalId, info.TranslateId(typeof(SimMultiValue), localId))
                            ).Name
                        ));
            }

            localId = info.TranslateId(typeof(SimMultiValue), localId);

            try
            {
                List<SimMultiValueFunctionGraph> graphs = new List<SimMultiValueFunctionGraph>(graphPoints.Count);
                for (int i = 0; i < graphPoints.Count; i++)
                    graphs.Add(new SimMultiValueFunctionGraph(graphNames[i], graphPoints[i]));

                var field = new SimMultiValueFunction(localId, name, unitX, unitY, unitZ, bounds, zaxis, graphs);
                return field;
            }
            catch (Exception e)
            {
                info.Log(string.Format("Failed to load SimMultiValueFunction with Id={0}, Name=\"{1}\"\nException: {2}\nStackTrace:\n{3}",
                    localId, name, e.Message, e.StackTrace
                    ));
            }

            return null;
        }
    }
}
