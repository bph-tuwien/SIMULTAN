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
    /// Writes delimiter (';') separated values to a stream
    /// </summary>
    public class DelimiterStreamWriter : IDisposable
    {
        private Stream stream;
        private StreamWriter sw;

        /// <summary>
        /// Initializes a new instance of the DelimiterStreamWriter class
        /// </summary>
        /// <param name="stream">The stream</param>
        /// <param name="encoding">Encoding of the target</param>
        public DelimiterStreamWriter(Stream stream, Encoding encoding)
        {
            this.stream = stream;
            if (!stream.CanWrite)
                throw new ArgumentException("Stream does not support write operations");

            sw = new StreamWriter(stream, encoding);
        }
        /// <summary>
        /// Disposes the stream
        /// </summary>
        ~DelimiterStreamWriter()
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
        /// Closes the stream. Also closes the target stream.
        /// </summary>
        public void Close()
        {
            if (sw != null)
            {
                sw.Close();
                sw = null;
            }
        }


        /// <summary>
        /// Writes to the stream. Strings are stored with [Length];[Content]
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(string value)
        {
            WriteInternal("{0};{1};", value.Length.ToString(CultureInfo.InvariantCulture), value);
        }
        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(char value)
        {
            WriteInternal("{0};", value);
        }

        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(Decimal value)
        {
            WriteNumber(value);
        }
        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(Double value)
        {
            WriteNumber(value);
        }
        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(Int32 value)
        {
            WriteNumber(value);
        }
        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(Int64 value)
        {
            WriteNumber(value);
        }
        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(Single value)
        {
            WriteNumber(value);
        }
        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(UInt32 value)
        {
            WriteNumber(value);
        }
        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(UInt64 value)
        {
            WriteNumber(value);
        }
        /// <summary>
        /// Writes to the stream
        /// </summary>
        /// <param name="value">The value to be written</param>
        public void Write(bool value)
        {
            sw.Write(value ? "1;" : "0;");
        }

        private void WriteNumber<T>(T value) where T : IConvertible
        {
            WriteInternal("{0};", value.ToString(CultureInfo.InvariantCulture));
        }
        private void WriteInternal(string format, params object[] arg)
        {
            if (sw != null)
            {
                sw.Write(format, arg);
            }
            else
                throw new IOException("Stream is already closed");
        }

        /// <summary>
        /// Writes a linebreak to the stream
        /// </summary>
        public void WriteLine()
        {
            if (sw != null)
            {
                sw.WriteLine();
            }
            else
                throw new IOException("Stream is already closed");
        }
    }
}
