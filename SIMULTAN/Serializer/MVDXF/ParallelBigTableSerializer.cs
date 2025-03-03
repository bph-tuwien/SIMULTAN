using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.MVDXF
{
    /// <summary>
    /// For handling the serialization of large chunks of data.
    /// </summary>
    internal class ParallelBigTableSerializer
    {
        #region MEMBERS

        private SimMultiValueBigTable data;
        private int serializationCode;
        private int chunkSize;

        #endregion

        /// <summary>
        /// Initializes the serializer. If the chunk size is larger than the data, it gets reduced.
        /// </summary>
        /// <param name="data">the data to be serialized</param>
        /// <param name="chunkSize">the number of rows to pass to a single instance for serialization</param>
        /// <param name="serializationCode">the DXF-style code to insert in front of the line of actual data</param>
        internal ParallelBigTableSerializer(SimMultiValueBigTable data, int chunkSize, int serializationCode)
        {
            if (data == null)
                throw new NullReferenceException("The data cannot be null!");
            if (chunkSize < 1)
                throw new ArgumentException("The chunk size must be positive!");

            this.data = data;
            this.serializationCode = serializationCode;
            this.chunkSize = Math.Min(chunkSize, data.Count(0));
        }

        /// <summary>
        /// Performs the actual serialization.
        /// </summary>
        /// <returns>the serialized data</returns>
        internal async Task SerializeValuesAsync(DXFStreamWriter sw)
        {
            if (this.data.Count(0) > 0)
            {
                int nr_chunks = this.data.Count(0) / this.chunkSize;
                int size_last_chunk = this.data.Count(0) % this.chunkSize;
                if (size_last_chunk > 0)
                    nr_chunks++;

                List<Task> tasks = new List<Task>();
                List<ParallelBigTableSerializerInstance> instances = new List<ParallelBigTableSerializerInstance>();
                for (int i = 0; i < nr_chunks; i++)
                {
                    int start_row = i * this.chunkSize;
                    int current_chunk_size = (i == (nr_chunks - 1) && size_last_chunk > 0) ? size_last_chunk : this.chunkSize;

                    ParallelBigTableSerializerInstance instance = new ParallelBigTableSerializerInstance(this.data, this.serializationCode,
                        i, start_row, current_chunk_size);
                    instances.Add(instance);
                    Task t_serialize = Task.Run(() => instance.SerializeValueRangeDXFStyle());
                    tasks.Add(t_serialize);
                }

                await Task.WhenAll(tasks);
                for (int i = 0; i < instances.Count; i++)
                {
                    sw.WriteUnstructured(instances[i].GetResult());
                    instances[i].Result = null;
                }
            }
        }
    }

    /// <summary>
    /// Represents a single serialization instance.
    /// </summary>
    internal class ParallelBigTableSerializerInstance
    {
        #region MEMBERS

        private SimMultiValueBigTable data;
        private int serializationCode;

        private int index;
        private int rowStart;
        private int rowCount;

        /// <summary>
        /// The serialization result.
        /// </summary>
        internal StringBuilder Result { get; set; }

        #endregion

        /// <summary>
        /// Initializes a single serialization instance.
        /// </summary>
        /// <param name="data">the data to be serialized</param>
        /// <param name="serializationCode">the DXF-style code to insert in front of the line of actual data</param>
        /// <param name="index">the index of the instance in the serialization context (see <see cref="ParallelBigTableSerializer"/>)</param>
        /// <param name="rowStart">the row in the data from which to start the serialization (inclusive)</param>
        /// <param name="rowCount">the number of rows to serialize</param>
        internal ParallelBigTableSerializerInstance(SimMultiValueBigTable data,
            int serializationCode, int index, int rowStart, int rowCount)
        {
            if (data == null)
                throw new NullReferenceException("The data cannot be null!");
            if (rowStart < 0 || rowStart >= data.Count(0))
                throw new IndexOutOfRangeException("The first row index is out of range!");
            if (rowStart + rowCount < 0 || rowStart + rowCount > data.Count(0))
                throw new IndexOutOfRangeException("The last row index is out of range!");

            this.data = data;
            this.serializationCode = serializationCode;

            this.index = index;
            this.rowStart = rowStart;
            this.rowCount = rowCount;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void SerializeValueRangeDXFStyle()
        {
            this.Result = new StringBuilder();
            for (int row = this.rowStart; row < this.rowStart + this.rowCount; row++)
            {
                this.Result.Append(this.serializationCode);
                this.Result.AppendLine();

                for (int n = 0; n < this.data.Count(1); n++)
                {
                    if (data[row, n] != null) //Null doesn't need to be written
                    {
                        switch (data[row, n])
                        {
                            case double d:
                                this.Result.Append("d");
                                this.Result.Append(DXFDataConverter<double>.P.ToDXFString(d));
                                break;
                            case int i:
                                this.Result.Append("n");
                                this.Result.Append(DXFDataConverter<int>.P.ToDXFString(i));
                                break;
                            case uint ui:
                                this.Result.Append("u");
                                this.Result.Append(DXFDataConverter<uint>.P.ToDXFString(ui));
                                break;
                            case bool b:
                                this.Result.Append("b");
                                this.Result.Append(DXFDataConverter<bool>.P.ToDXFString(b));
                                break;
                            case long l:
                                this.Result.Append("l");
                                this.Result.Append(DXFDataConverter<long>.P.ToDXFString(l));
                                break;
                            case ulong ul:
                                this.Result.Append("m");
                                this.Result.Append(DXFDataConverter<ulong>.P.ToDXFString(ul));
                                break;
                            case string s:
                                {
                                    this.Result.Append("s");

                                    /* Replace the following characters 
                                     * ; with \;
                                     * line break with \n
                                     * \ with \\
                                     */
                                    foreach (var c in s)
                                    {
                                        if (c == '\\')
                                            this.Result.Append(@"\\");
                                        else if (c == ParamStructTypes.DELIMITER_WITHIN_ENTRY)
                                        {
                                            this.Result.Append('\\');
                                            this.Result.Append(ParamStructTypes.DELIMITER_WITHIN_ENTRY);
                                        }
                                        else if (c == '\n')
                                            this.Result.Append(@"\n");
                                        else if (c == '\r')
                                        { }
                                        else
                                            this.Result.Append(c);
                                    }
                                }
                                break;
                            default:
                                throw new NotSupportedException("Datatype not supported");
                        }
                    }

                    this.Result.Append(ParamStructTypes.DELIMITER_WITHIN_ENTRY);
                }

                if (row < this.rowStart + this.rowCount - 1)
                    this.Result.AppendLine();
            }
        }

        /// <summary>
        /// Gets the content of the <see cref="Result"/> property.
        /// </summary>
        /// <returns>the serialized content as a string including newline</returns>
        internal string GetResult()
        {
            if (this.Result != null)
                return this.Result.ToString();
            else
                return string.Empty;
        }
    }


    internal class ParallelBigTableSerializerElement : DXFEntryParserElement
    {
        internal ParallelBigTableSerializerElement() : base((int)MultiValueSaveCode.MVDATA_ROW_COUNT) { }

        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var value) = reader.GetLast();
            if (key != (int)MultiValueSaveCode.MVDATA_ROW_COUNT)
            {
                throw new Exception(
                    string.Format("Expected key {0}, but found key {1} while parsing BigTable data",
                        MultiValueSaveCode.MVDATA_ROW_COUNT, key));
            }

            int rowCount = DXFDataConverter<int>.P.FromDXFString(value, info);

            (key, value) = reader.Read();
            if (key != (int)MultiValueSaveCode.MVDATA_COLUMN_COUNT)
            {
                throw new Exception(
                    string.Format("Expected key {0}, but found key {1} while parsing BigTable data",
                        MultiValueSaveCode.MVDATA_COLUMN_COUNT, key));
            }

            int columnCount = DXFDataConverter<int>.P.FromDXFString(value, info);

            //Rows

            int maxChunkSize = (365 * 24 * 4);
            int maxNumberOfChunks = int.MaxValue;
            int numberOfChunks = Math.Max(1,
                Math.Min(Math.Min(maxNumberOfChunks - 1, (int)Math.Ceiling(rowCount * columnCount / (double)maxChunkSize)), rowCount));
            double rowsPerChunk = rowCount / (double)numberOfChunks;

            Task<List<List<object>>>[] tasks = new Task<List<List<object>>>[numberOfChunks];
            for (int i = 0; i < numberOfChunks; i++)
            {
                List<string> rows = new List<string>();

                int startRow = (int)Math.Floor(i * rowsPerChunk);
                int endRow = Math.Min((int)Math.Floor((i + 1) * rowsPerChunk), rowCount);
                int rowsInChunk = endRow - startRow;

                for (int r = 0; r < rowsInChunk; r++)
                {
                    (key, value) = reader.Read();
                    if (key != (int)MultiValueSaveCode.MV_DATA)
                    {
                        throw new Exception(
                            string.Format("Expected key {0}, but found key {1} while parsing BigTable data",
                                MultiValueSaveCode.MV_DATA, key));
                    }

                    rows.Add(value);
                }

                tasks[i] = Task.Run(() => ParseRowsAsync(rows, columnCount, info));
            }

            return tasks;
        }

        private List<object> ParseRow(string row, int columnCount, DXFParserInfo info)
        {
            List<object> values = new List<object>(columnCount);

            if (info.FileVersion >= 18)
            {
                int lastMatchEnd = 0;
                Type type = null;

                for (int i = 0; i < row.Length; i++)
                {
                    if (type == null && row[i] != ParamStructTypes.DELIMITER_WITHIN_ENTRY) //Type identifier
                    {
                        switch (row[i])
                        {
                            case 'd':
                                type = typeof(double);
                                break;
                            case 'n':
                                type = typeof(int);
                                break;
                            case 'u':
                                type = typeof(uint);
                                break;
                            case 'l':
                                type = typeof(long);
                                break;
                            case 'm':
                                type = typeof(ulong);
                                break;
                            case 'b':
                                type = typeof(bool);
                                break;
                            case 's':
                                type = typeof(string);
                                break;
                            default:
                                throw new Exception("Invalid type identifier");
                        }
                    }
                    else if (row[i] == '\\') //Escape character
                    {
                        i++; //skip next char
                    }
                    else if (row[i] == ParamStructTypes.DELIMITER_WITHIN_ENTRY) //Delimiter -> end of cell
                    {
                        if (type == null)
                        {
                            values.Add(null);
                        }
                        else if (type == typeof(double))
                        {
                            values.Add(DXFDataConverter<double>.P.FromDXFString(row.Substring(lastMatchEnd + 1, i - lastMatchEnd - 1), info));
                        }
                        else if (type == typeof(int))
                        {
                            values.Add(DXFDataConverter<int>.P.FromDXFString(row.Substring(lastMatchEnd + 1, i - lastMatchEnd - 1), info));
                        }
                        else if (type == typeof(uint))
                        {
                            values.Add(DXFDataConverter<uint>.P.FromDXFString(row.Substring(lastMatchEnd + 1, i - lastMatchEnd - 1), info));
                        }
                        else if (type == typeof(long))
                        {
                            values.Add(DXFDataConverter<long>.P.FromDXFString(row.Substring(lastMatchEnd + 1, i - lastMatchEnd - 1), info));
                        }
                        else if (type == typeof(ulong))
                        {
                            values.Add(DXFDataConverter<ulong>.P.FromDXFString(row.Substring(lastMatchEnd + 1, i - lastMatchEnd - 1), info));
                        }
                        else if (type == typeof(bool))
                        {
                            values.Add(DXFDataConverter<bool>.P.FromDXFString(row.Substring(lastMatchEnd + 1, i - lastMatchEnd - 1), info));
                        }
                        else if (type == typeof(string))
                        {
                            //Unescape string
                            StringBuilder sb = new StringBuilder(i - lastMatchEnd);
                            for (int si = lastMatchEnd + 1; si < i; ++si)
                            {
                                if (row[si] == '\\') //Escape sign
                                {
                                    if (row[si + 1] == 'n')
                                    {
                                        sb.Append('\n');
                                        si++;
                                    }
                                    else if (row[si + 1] == '\\')
                                    {
                                        sb.Append('\\');
                                        si++;
                                    }
                                    //else ignore and just add the next char

                                }
                                else
                                {
                                    sb.Append(row[si]);
                                }
                            }

                            values.Add(sb.ToString());
                        }

                        lastMatchEnd = i + 1;
                        type = null;
                    }
                }
            }
            else //if (info.FileVersion < 17)
            {
                string delimiter = ParamStructTypes.DELIMITER_WITHIN_ENTRY_BEFORE_V18;

                int match = row.IndexOf(delimiter);
                int lastMatchEnd = 0;
                while (match != -1)
                {
                    string cell = row.Substring(lastMatchEnd, match - lastMatchEnd);
                    values.Add(DXFDataConverter<double>.P.FromDXFString(cell, info));
                    lastMatchEnd = match + delimiter.Length;
                    match = row.IndexOf(delimiter, lastMatchEnd);
                }
                //last part
                if (lastMatchEnd < row.Length)
                    values.Add(DXFDataConverter<double>.P.FromDXFString(row.Substring(lastMatchEnd), info));
            }

            return values;
        }

        private List<List<object>> ParseRowsAsync(List<string> rows, int columnCount, DXFParserInfo info)
        {
            List<List<object>> result = new List<List<object>>(rows.Count);

            for (int i = 0; i < rows.Count; ++i)
                result.Add(ParseRow(rows[i], columnCount, info));

            return result;
        }
    }
}
