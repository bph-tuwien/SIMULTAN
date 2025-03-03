using SIMULTAN.Exceptions;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Writes DXF formatted data to a stream
    /// </summary>
    public sealed class DXFStreamWriter : IDisposable
    {
        /// <summary>
        /// The DXF file version ( FileVersion )
        /// </summary>
        public static ulong CurrentFileFormatVersion { get => 30L; }

        internal static readonly string NEWLINE_PLACEHOLDER = "[NewLine]";

        private StreamWriter writer;

        private bool isInSection = false;
        private int complexRecursion = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStreamWriter"/> class
        /// </summary>
        /// <param name="stream">The stream into which the writer should write</param>
        /// <param name="leaveOpen">When set to True, the stream will stay open after the DXFStreamWriter is disposed.
        /// Otherwise the stream is also disposed</param>
        internal DXFStreamWriter(Stream stream, bool leaveOpen = false)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite)
                throw new ArgumentException("Stream cannot be written to");

            writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen);
        }


        private bool isDisposed = false;
        /// <inheritdoc />
        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                writer.Dispose();
            }
        }

        #region Write

        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(UserComponentListSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(DataMappingSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(GeometryRelationSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(UserSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ComponentAccessTrackerSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(CalculatorMappingSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ComponentInstanceSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ComponentSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ParameterSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ParamStructCommonSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(MultiValueSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(GeoMapSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(SitePlannerSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(CalculationSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ChatItemSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ResourceSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(AssetSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(FlowNetworkSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(SimNetworkSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ProjectSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(ValueMappingSaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void Write<T>(TaxonomySaveCode code, T content)
        {
            Write((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        private void Write<T>(int code, T content)
        {
            writer.WriteLine(code);
            writer.WriteLine(DXFDataConverter<T>.P.ToDXFString(content));
        }

        /// <summary>
        /// Writes a code-value pair to the stream. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <param name="code">The code</param>
        /// <param name="content">The value</param>
        public void WritePath(ResourceSaveCode code, string content)
        {
            Write((int)code, FileSystemNavigation.SanitizeWritePath(content));
        }

        #endregion

        #region WriteBase64

        /// <summary>
        /// Writes a code-value pair to the stream with Base64 encoding. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <param name="code">The code</param>
        /// <param name="content">the binary data</param>
        public void WriteBase64(UserSaveCode code, byte[] content)
        {
            WriteBase64((int)code, content);
        }
        /// <summary>
        /// Writes a code-value pair to the stream with Base64 encoding. This overload uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <param name="code">The code</param>
        /// <param name="content">the binary data</param>
        private void WriteBase64(int code, byte[] content)
        {
            writer.WriteLine(code);
            writer.WriteLine(Convert.ToBase64String(content));
        }

        #endregion

        #region WriteArray

        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(TaxonomySaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(UserComponentListSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(GeometryRelationSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(ResourceSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(ChatItemSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(DataMappingSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(CalculatorMappingSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(ComponentInstanceSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(CalculationSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(ComponentSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(ParamStructCommonSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(GeoMapSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(MultiValueSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(SitePlannerSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(AssetSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(FlowNetworkSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(ValueMappingSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        public void WriteArray<T>(SimNetworkSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteArray((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes an array to the stream. The first entry is the count, followed by the entries in the array.
        /// Uses the <see cref="DXFDataConverter"/> to serialize the data
        /// </summary>
        /// <typeparam name="T">Type of the data entry</typeparam>
        /// <param name="code">The code</param>
        /// <param name="collection">The collection to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream</param>
        private void WriteArray<T>(int code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            Write(code, collection.Count());
            foreach (var item in collection)
                itemSerializer(item, this);
        }

        #endregion

        #region WriteNestedList

        /// <summary>
        /// Writes a nested list to the stream.
        /// Nested lists consist of a count entry, followed by the items. After each item a continue code entry has to be present which
        /// tells when each sublist ends.
        /// </summary>
        /// <typeparam name="T">Type of the inner collection</typeparam>
        /// <typeparam name="U">Type of the items in the inner collection</typeparam>
        /// <param name="code">DXF code</param>
        /// <param name="lists">The lists to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream.
        /// The first parameter is the item, the second one the value of then continue entry.</param>
        public void WriteNestedList<T, U>(MultiValueSaveCode code, IEnumerable<T> lists, Action<U, int, DXFStreamWriter> itemSerializer)
            where T : IEnumerable<U>
        {
            WriteNestedList<T, U>((int)code, lists, itemSerializer);
        }
        /// <summary>
        /// Writes a nested list to the stream.
        /// Nested lists consist of a count entry, followed by the items. After each item a continue code entry has to be present which
        /// tells when each sublist ends.
        /// </summary>
        /// <typeparam name="T">Type of the inner collection</typeparam>
        /// <typeparam name="U">Type of the items in the inner collection</typeparam>
        /// <param name="code">DXF code</param>
        /// <param name="lists">The lists to serialize</param>
        /// <param name="itemSerializer">A function that writes an item of the collection to the stream.
        /// The first parameter is the item, the second one the value of then continue entry.</param>
        private void WriteNestedList<T, U>(int code, IEnumerable<T> lists, Action<U, int, DXFStreamWriter> itemSerializer) where T : IEnumerable<U>
        {
            //Count
            Write(code, lists.Count());

            foreach (var sublist in lists)
            {
                var enumerator = sublist.GetEnumerator();
                var last = !enumerator.MoveNext();

                while (!last)
                {
                    var current = enumerator.Current;
                    last = !enumerator.MoveNext();

                    itemSerializer(current, last ? ParamStructTypes.LIST_END : ParamStructTypes.LIST_CONTINUE, this);
                }
            }
        }

        #endregion

        #region Complex Entities

        /// <summary>
        /// Starts a complex entity
        /// </summary>
        public void StartComplexEntity()
        {
            this.complexRecursion++;
        }
        /// <summary>
        /// Writes the end of a complex entity. Throws an exception in case no complex entity has been started before.
        /// </summary>
        public void EndComplexEntity()
        {
            if (this.complexRecursion == 0)
                throw new Exception("No complex object has been started");

            this.complexRecursion--;
            Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SEQUENCE_END);
        }

        #endregion

        #region WriteEntitySequence 

        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(DataMappingSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(ChatItemSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(ComponentInstanceSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(ComponentSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(SitePlannerSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(ResourceSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(AssetSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(FlowNetworkSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(SimNetworkSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(ValueMappingSaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }

        /// <summary>
        /// Writes a collection into an entity sequence
        /// </summary>
        /// <typeparam name="T">The item type of the collection</typeparam>
        /// <param name="code">The element code of the entity sequence</param>
        /// <param name="collection">The items to serialize</param>
        /// <param name="itemSerializer">Method to serialize entries in the collection</param>
        public void WriteEntitySequence<T>(TaxonomySaveCode code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            WriteEntitySequence((int)code, collection, itemSerializer);
        }
        private void WriteEntitySequence<T>(int code, IEnumerable<T> collection, Action<T, DXFStreamWriter> itemSerializer)
        {
            int count = collection.Count();
            Write(code, count);

            if (count > 0)
            {
                Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.ENTITY_SEQUENCE);

                foreach (var item in collection)
                    itemSerializer(item, this);

                Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SEQUENCE_END);
                Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.ENTITY_CONTINUE);
            }
        }

        #endregion

        #region WriteMultilineText

        /// <summary>
        /// Writes a multi-line text to the stream
        /// </summary>
        /// <param name="code">The code</param>
        /// <param name="text">The text</param>
        public void WriteMultilineText(ComponentSaveCode code, string text)
        {
            if (text == null)
                Write(code, string.Empty);
            else
                Write(code, text.Replace(Environment.NewLine, NEWLINE_PLACEHOLDER));
        }
        /// <summary>
        /// Writes a multi-line text to the stream
        /// </summary>
        /// <param name="code">The code</param>
        /// <param name="text">The text</param>
        public void WriteMultilineText(MultiValueSaveCode code, string text)
        {
            if (text == null)
                Write(code, string.Empty);
            else
                Write(code, text.Replace(Environment.NewLine, NEWLINE_PLACEHOLDER));
        }
        /// <summary>
        /// Writes a multi-line text to the stream
        /// </summary>
        /// <param name="code">The code</param>
        /// <param name="text">The text</param>
        public void WriteMultilineText(TaxonomySaveCode code, string text)
        {
            if (text == null)
                Write(code, string.Empty);
            else
                Write(code, text.Replace(Environment.NewLine, NEWLINE_PLACEHOLDER));
        }

        #endregion

        #region WriteGlobalId

        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(CalculatorMappingSaveCode code, Guid globalId, Guid currentProject)
        {
            WriteGlobalId((int)code, globalId, currentProject);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(DataMappingSaveCode code, Guid globalId, Guid currentProject)
        {
            WriteGlobalId((int)code, globalId, currentProject);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(ComponentInstanceSaveCode code, Guid globalId, Guid currentProject)
        {
            WriteGlobalId((int)code, globalId, currentProject);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(GeometryRelationSaveCode code, Guid globalId, Guid currentProject)
        {
            WriteGlobalId((int)code, globalId, currentProject);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(MultiValueSaveCode code, Guid globalId, Guid currentProject)
        {
            WriteGlobalId((int)code, globalId, currentProject);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(ParameterSaveCode code, Guid globalId, Guid currentProject)
        {
            WriteGlobalId((int)code, globalId, currentProject);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(SitePlannerSaveCode code, Guid globalId, Guid currentProject)
        {
            if (globalId == currentProject)
                Write(code, Guid.Empty);
            else
                Write(code, globalId);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(GeoMapSaveCode code, Guid globalId, Guid currentProject)
        {
            if (globalId == currentProject)
                Write(code, Guid.Empty);
            else
                Write(code, globalId);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(ParamStructCommonSaveCode code, Guid globalId, Guid currentProject)
        {
            WriteGlobalId((int)code, globalId, currentProject);
        }
        /// <summary>
        /// Writes a global id GUID to the stream. Writes <see cref="Guid.Empty"/> when the id is the same as the current project's global id.
        /// </summary>
        /// <param name="code">The DXF code</param>
        /// <param name="globalId">The id to serialize</param>
        /// <param name="currentProject">The current project</param>
        public void WriteGlobalId(ValueMappingSaveCode code, Guid globalId, Guid currentProject)
        {
            WriteGlobalId((int)code, globalId, currentProject);
        }
        private void WriteGlobalId(int code, Guid globalId, Guid currentProject)
        {
            if (globalId == currentProject)
                Write(code, Guid.Empty);
            else
                Write(code, globalId);
        }

        #endregion

        /// <summary>
        /// Writes unstructured text to the stream
        /// </summary>
        /// <param name="content">The text</param>
        public void WriteUnstructured(string content)
        {
            writer.WriteLine(content);
        }

        #region Sections & EOF

        /// <summary>
        /// Writes a version section to the stream.
        /// </summary>
        /// 
        /// <code>
        /// 0
        /// SECTION
        /// 2
        /// VERSION_SECTION
        /// 0
        /// FILE_VERSION
        /// 10
        /// #version#
        /// 0
        /// ENDSEC
        /// </code>
        public void WriteVersionSection()
        {
            StartSection(ParamStructTypes.VERSION_SECTION, -1);

            Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.FILE_VERSION);
            Write(ParamStructCommonSaveCode.COORDS_X, CurrentFileFormatVersion);

            EndSection();
        }

        /// <summary>
        /// Writes a section start entry. Only possible when not inside another section
        /// 
        /// <code>
        /// 0
        /// SECTION
        /// 2
        /// #sectionName#
        /// </code>
        /// </summary>
        /// <param name="sectionName">Name of the section</param>
        /// <param name="numberOfElements">Number of elements in the section</param>
        public void StartSection(string sectionName, int numberOfElements)
        {
            if (isInSection)
                throw new InvalidStateException("Section inside another Section is not allowed. Did you forget to end the previous Section?");

            isInSection = true;
            Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SECTION_START);
            Write(ParamStructCommonSaveCode.ENTITY_NAME, sectionName);
            Write(ParamStructCommonSaveCode.NUMBER_OF, numberOfElements);
        }
        /// <summary>
        /// Writes an end section entry
        /// 
        /// <code>
        /// 0
        /// ENDSEC
        /// </code>
        /// </summary>
        public void EndSection()
        {
            if (!isInSection)
                throw new InvalidStateException("No section has been started.");

            isInSection = false;
            Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.SECTION_END);
        }

        /// <summary>
        /// Writes an end of file entry to the file
        /// 
        /// <code>
        /// 0
        /// EOF
        /// </code>
        /// </summary>
        public void WriteEOF()
        {
            Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.EOF);
        }

        #endregion
    }
}
