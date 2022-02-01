using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Streams
{
    /// <summary>
    /// Reads delimiter separated (';') values from a stream
    /// </summary>
    public class DelimiterStreamReader : IDisposable
    {
        private Stream stream;
        private StreamReader sr;

        /// <summary>
        /// The current row
        /// </summary>
        public int Column { get; private set; }
        /// <summary>
        /// The current column
        /// </summary>
        public int Row { get; private set; }

        /// <summary>
        /// Initializes a new instance of the DelimiterStreamReader class
        /// </summary>
        /// <param name="stream">The stream</param>
        /// <param name="encoding">Encoding of the string</param>
        public DelimiterStreamReader(Stream stream, Encoding encoding)
        {
            this.stream = stream;
            if (!stream.CanRead)
                throw new ArgumentException("Stream does not support read operations");

            sr = new StreamReader(stream, encoding);

            Column = 1;
            Row = 1;
        }
        /// <summary>
        /// Disposes the stream
        /// </summary>
        ~DelimiterStreamReader()
        {
            Close();
        }
        /// <summary>
        /// Disposes the stream
        /// </summary>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// Closes the stream. Also closes the source stream.
        /// </summary>
        public void Close()
        {
            if (sr != null)
            {
                sr.Close();
                sr = null;
                Row = -1;
                Column = -1;
            }
        }

        /// <summary>
        /// Reads until the next delimiter
        /// </summary>
        /// <returns></returns>
        public string ReadToDelimiter()
        {
            StringBuilder str = new StringBuilder();

            int intVal = sr.Read();
            Column++;
            char val = (char)intVal;
            while (val != ';')
            {
                if (intVal == -1)
                    throw new IOException("Unexpected end of file");
                else if (val == '\n')
                {
                    Column = 1;
                    Row++;
                }
                else if (val != '\r')
                    str.Append(val);

                intVal = sr.Read();
                Column++;
                val = (char)intVal;
            }

            return str.ToString();
        }

        /// <summary>
        /// Reads a string from the stream
        /// </summary>
        /// <returns></returns>
        public string ReadString()
        {
            int length, readCount;
            char[] buffer;

            try
            {
                length = ReadNumber<Int32>();
                buffer = new char[length];
                readCount = sr.ReadBlock(buffer, 0, length);
                sr.Read(); //Skip ;
            }
            catch (Exception e)
            {
                throw new IOException(String.Format("Failed to read string: {0}", e.Message), e);
            }

            if (readCount < length)
                throw new IOException(String.Format("Failed to read string: Unexpected end of file"));

            String result = new String(buffer);

            //Update column and row
            int rowCount = buffer.Count(x => x == '\n');
            Row += rowCount;

            if (rowCount == 0)
                Column += length;
            else
                Column = result.Length - result.LastIndexOf('\n');

            return result;
        }
        /// <summary>
        /// Reads a number from the stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ReadNumber<T>() where T : IConvertible
        {
            var read = ReadToDelimiter();

            if (read.Length == 0)
                throw new FormatException(String.Format("Expected number around row {0}, column {1}", Row, Column));

            return (T)Convert.ChangeType(read, typeof(T), CultureInfo.InvariantCulture);
        }
        /// <summary>
        /// Reads a boolean from the stream
        /// </summary>
        /// <returns></returns>
        public bool ReadBool()
        {
            string content = ReadToDelimiter();

            if (content == "0")
                return false;
            else if (content == "1")
                return true;
            else
                throw new FormatException(String.Format("Expected boolean around row {0}, column {1}", Row, Column));
        }

        /// <summary>
        /// Skips the next string. Has the same behaviour as readstring but doesn't store anything in memory.
        /// Use this method when reading through a file with passwords to skip them when not needed
        /// </summary>
        public void SkipString()
        {
            try
            {
                int length = ReadNumber<Int32>();
                sr.BaseStream.Seek(length + 1, SeekOrigin.Current);
            }
            catch (Exception e)
            {
                throw new IOException(String.Format("Failed to skip string: {0}", e.Message), e);
            }
        }
    }
}
