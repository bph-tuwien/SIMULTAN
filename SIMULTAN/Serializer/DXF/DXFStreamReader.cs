using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Reads from a stream containing DXF formatted data
    /// </summary>
    public class DXFStreamReader : IDisposable
    {
        private int lastCode = -1;
        private string lastValue = null;

        private StreamReader reader;

        private Queue<(int, string)> peekQueue = new Queue<(int, string)>();

        private Stream Stream { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStreamReader"/> class
        /// </summary>
        /// <param name="stream">The stream to read from</param>
        internal DXFStreamReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            this.Stream = stream;
            reader = new StreamReader(stream, Encoding.UTF8, true);
        }

        private bool isDisposed = false;
        /// <inheritdoc />
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                reader.Dispose();
            }
        }

        /// <summary>
        /// Reads a new key-value pair from the stream
        /// </summary>
        /// <returns>Returns the read entry consisting of a code and a string value that can be parsed later on</returns>
        public (int code, string value) Read()
        {
            this.lastCode = -1;
            this.lastValue = null;

            if (peekQueue.Count > 0)
            {
                (this.lastCode, this.lastValue) = peekQueue.Dequeue();
            }
            else
            {
                //Key
                string line = reader.ReadLine();
                if (line == null)
                    return (-1, null);

                if (!Int32.TryParse(line, out this.lastCode))
                    throw new Exception(String.Format("Failed to parse DXF code of line \"{0}\"", line));

                //Value
                this.lastValue = reader.ReadLine();
                if (this.lastValue == null)
                    return (-1, null);

                //Debug.WriteLine(this.lastCode);
                //Debug.WriteLine(this.lastValue);
            }

            return (this.lastCode, this.lastValue);
        }

        /// <summary>
        /// Returns the last code-value pair read by the <see cref="Read"/> method
        /// </summary>
        /// <returns>The last code/value that has been read by the <see cref="Read"/> method</returns>
        public (int code, string value) GetLast()
        {
            return (this.lastCode, this.lastValue);
        }

        /// <summary>
        /// Reads an entry without consuming it.
        /// <see cref="Read"/> will again return this data when called
        /// </summary>
        /// <returns>Returns the read entry consisting of a code and a string value that can be parsed later on</returns>
        public (int code, string value) Peek()
        {
            this.lastCode = -1;

            string line = reader.ReadLine();
            if (line == null)
                return (-1, null);

            if (!Int32.TryParse(line, out this.lastCode))
                throw new Exception(String.Format("Failed to parse DXF code of line \"{0}\"", line));

            this.lastValue = reader.ReadLine();
            if (this.lastValue == null)
                return (-1, null);

            peekQueue.Enqueue((this.lastCode, this.lastValue));

            return (this.lastCode, this.lastValue);
        }

        /// <summary>
        /// Clears the peek pool. Useful when checking if an element exists and optionally consuming everything
        /// </summary>
        public void ClearPeek()
        {
            peekQueue.Clear();
        }

    }
}
