using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIMULTAN.Projects.Serializers
{
    /// <summary>
    /// For handling the serialization of large chunks of data.
    /// </summary>
    internal class ParallelBigTableSerializer
    {
        #region MEMBERS

        private List<List<double>> data_to_serialize;
        private string format;
        private int serialization_code;
        private int chunk_size;

        #endregion

        /// <summary>
        /// Initializes the serializer. If the chunk size is larger than the data, it gets reduced.
        /// </summary>
        /// <param name="_data">the data to be serialized</param>
        /// <param name="_chunk_size">the number of rows to pass to a single instance for serialization</param>
        /// <param name="_format">the formatting pattern for the ToString function, e.g. F8</param>
        /// <param name="_serialization_code">the DXF-style code to insert in front of the line of actual data</param>
        internal ParallelBigTableSerializer(List<List<double>> _data, int _chunk_size, string _format, int _serialization_code)
        {
            if (_data == null)
                throw new NullReferenceException("The data cannot be null!");
            if (_chunk_size < 1)
                throw new ArgumentException("The chunk size must be positive!");

            this.data_to_serialize = _data;
            this.format = (string.IsNullOrEmpty(_format)) ? "F8" : _format;
            this.serialization_code = _serialization_code;
            this.chunk_size = Math.Min(_chunk_size, _data.Count);
        }

        /// <summary>
        /// Performs the actual serialization.
        /// </summary>
        /// <returns>the serialized data</returns>
        internal async Task SerializeValuesDXFStyleAsync(StringBuilder _sb)
        {
            int nr_chunks = this.data_to_serialize.Count / this.chunk_size;
            int size_last_chunk = this.data_to_serialize.Count % this.chunk_size;
            if (size_last_chunk > 0)
                nr_chunks++;

            List<Task> tasks = new List<Task>();
            List<ParallelBigTableSerializerInstance> instances = new List<ParallelBigTableSerializerInstance>();
            for (int i = 0; i < nr_chunks; i++)
            {
                int start_row = i * this.chunk_size;
                int current_chunk_size = (i == (nr_chunks - 1) && size_last_chunk > 0) ? size_last_chunk : this.chunk_size;
                // Console.WriteLine("Instance {0}", i);
                ParallelBigTableSerializerInstance instance = new ParallelBigTableSerializerInstance(this.data_to_serialize, this.format, this.serialization_code, i, start_row, current_chunk_size);
                instances.Add(instance);
                Task t_serialize = Task.Run(() => instance.SerializeValueRangeDXFStyle());
                tasks.Add(t_serialize);
            }

            await Task.WhenAll(tasks);
            for (int i = 0; i < instances.Count; i++)
            {
                _sb.AppendLine(instances[i].GetResult());
                instances[i].Result = null;
            }
            // Console.WriteLine("Serialization done. Passing result along now...");
        }
    }

    /// <summary>
    /// Represents a single serialization instance.
    /// </summary>
    internal class ParallelBigTableSerializerInstance
    {
        #region MEMBERS

        private List<List<double>> data_to_serialize;
        private string format;
        private int serialization_code;

        private int index;
        private int row_start;
        private int nr_of_rows;

        /// <summary>
        /// The serialization result.
        /// </summary>
        internal StringBuilder Result { get; set; }

        #endregion

        /// <summary>
        /// Initializes a single serialization instance.
        /// </summary>
        /// <param name="_data">the data to be serialized</param>
        /// <param name="_format">the formatting pattern for the ToString function, e.g. F8</param>
        /// <param name="_serialization_code">the DXF-style code to insert in front of the line of actual data</param>
        /// <param name="_index">the index of the instance in the serialization context (see <see cref="ParallelBigTableSerializer"/>)</param>
        /// <param name="_row_start">the row in the data from which to start the serialization (inclusive)</param>
        /// <param name="_nr_of_rows">the number of rows to serialize</param>
        internal ParallelBigTableSerializerInstance(List<List<double>> _data, string _format, int _serialization_code, int _index, int _row_start, int _nr_of_rows)
        {
            if (_data == null)
                throw new NullReferenceException("The data cannot be null!");
            if (_row_start < 0 || _row_start >= _data.Count)
                throw new IndexOutOfRangeException("The first row index is out of range!");
            if (_row_start + _nr_of_rows < 0 || _row_start + _nr_of_rows > _data.Count)
                throw new IndexOutOfRangeException("The last row index is out of range!");

            this.data_to_serialize = _data;
            this.format = (string.IsNullOrEmpty(_format)) ? "F8" : _format;
            this.serialization_code = _serialization_code;

            this.index = _index;
            this.row_start = _row_start;
            this.nr_of_rows = _nr_of_rows;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void SerializeValueRangeDXFStyle()
        {
            this.Result = new StringBuilder();
            for (int i = this.row_start; i < this.row_start + this.nr_of_rows; i++)
            {
                this.Result.Append(this.serialization_code);
                this.Result.AppendLine();
                for (int n = 0; n < this.data_to_serialize[i].Count; n++)
                {
                    this.Result.Append(DXFDecoder.DoubleToString(this.data_to_serialize[i][n], this.format));
                    if (n < this.data_to_serialize[i].Count - 1)
                        this.Result.Append(ParamStructTypes.DELIMITER_WITHIN_ENTRY);
                }
                if (i < this.row_start + this.nr_of_rows - 1)
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
}
