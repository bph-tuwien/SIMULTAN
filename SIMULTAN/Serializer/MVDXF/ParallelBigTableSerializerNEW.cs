using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private List<List<double>> data;
        private int serializationCode;
        private int chunkSize;

        #endregion

        /// <summary>
        /// Initializes the serializer. If the chunk size is larger than the data, it gets reduced.
        /// </summary>
        /// <param name="data">the data to be serialized</param>
        /// <param name="chunkSize">the number of rows to pass to a single instance for serialization</param>
        /// <param name="serializationCode">the DXF-style code to insert in front of the line of actual data</param>
        internal ParallelBigTableSerializer(List<List<double>> data, int chunkSize, int serializationCode)
        {
            if (data == null)
                throw new NullReferenceException("The data cannot be null!");
            if (chunkSize < 1)
                throw new ArgumentException("The chunk size must be positive!");

            this.data = data;
            this.serializationCode = serializationCode;
            this.chunkSize = Math.Min(chunkSize, data.Count);
        }

        /// <summary>
        /// Performs the actual serialization.
        /// </summary>
        /// <returns>the serialized data</returns>
        internal async Task SerializeValuesAsync(DXFStreamWriter sw)
        {
            int nr_chunks = this.data.Count / this.chunkSize;
            int size_last_chunk = this.data.Count % this.chunkSize;
            if (size_last_chunk > 0)
                nr_chunks++;

            List<Task> tasks = new List<Task>();
            List<ParallelBigTableSerializerInstance> instances = new List<ParallelBigTableSerializerInstance>();
            for (int i = 0; i < nr_chunks; i++)
            {
                int start_row = i * this.chunkSize;
                int current_chunk_size = (i == (nr_chunks - 1) && size_last_chunk > 0) ? size_last_chunk : this.chunkSize;

                ParallelBigTableSerializerInstance instance = new ParallelBigTableSerializerInstance(this.data, this.serializationCode, i, start_row, current_chunk_size);
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

    /// <summary>
    /// Represents a single serialization instance.
    /// </summary>
    internal class ParallelBigTableSerializerInstance
    {
        #region MEMBERS

        private List<List<double>> data;
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
        internal ParallelBigTableSerializerInstance(List<List<double>> data, 
            int serializationCode, int index, int rowStart, int rowCount)
        {
            if (data == null)
                throw new NullReferenceException("The data cannot be null!");
            if (rowStart < 0 || rowStart >= data.Count)
                throw new IndexOutOfRangeException("The first row index is out of range!");
            if (rowStart + rowCount < 0 || rowStart + rowCount > data.Count)
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
            for (int i = this.rowStart; i < this.rowStart + this.rowCount; i++)
            {
                this.Result.Append(this.serializationCode);
                this.Result.AppendLine();
                for (int n = 0; n < this.data[i].Count; n++)
                {
                    this.Result.Append(DXFDataConverter<double>.P.ToDXFString(this.data[i][n]));
                    if (n < this.data[i].Count - 1)
                        this.Result.Append(ParamStructTypes.DELIMITER_WITHIN_ENTRY);
                }
                if (i < this.rowStart + this.rowCount - 1)
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
            int numberOfChunks = Math.Min(Math.Min(maxNumberOfChunks - 1, (int)Math.Ceiling(rowCount * columnCount / (double)maxChunkSize)), rowCount);
            double rowsPerChunk = rowCount / (double)numberOfChunks;

            Task<List<List<double>>>[] tasks = new Task<List<List<double>>>[numberOfChunks];
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

        private List<double> ParseRow(string row, int columnCount, string delimiter, DXFParserInfo info)
        {
            List<double> values = new List<double>(columnCount);

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

            return values;
        }

        private List<List<double>> ParseRowsAsync(List<string> rows, int columnCount, DXFParserInfo info)
        {
            List<List<double>> result = new List<List<double>>(rows.Count);

            for (int i = 0; i < rows.Count; ++i)
                result.Add(ParseRow(rows[i], columnCount, ParamStructTypes.DELIMITER_WITHIN_ENTRY, info));

            return result;
        }
    }
}
