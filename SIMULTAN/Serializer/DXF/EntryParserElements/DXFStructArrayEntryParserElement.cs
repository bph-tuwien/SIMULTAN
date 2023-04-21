using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Interface for struct array parser entries
    /// </summary>
    public interface IDXFStructArrayEntryParserElement
    {
        /// <summary>
        /// A collection of entries in the struct array
        /// </summary>
        IEnumerable<DXFEntryParserElement> Elements { get; }
    }

    /// <summary>
    /// DXF entry for arrays of structs (multi-data elements)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DXFStructArrayEntryParserElement<T> : DXFEntryParserElement, IDXFStructArrayEntryParserElement
    {
        /// <inheritdoc/>
        public IEnumerable<DXFEntryParserElement> Elements { get; }
        private Func<DXFParserResultSet, DXFParserInfo, T> parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(int code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : base(code)
        {
            if (elements.Last().IsOptional)
                throw new ArgumentException("The last array element may not be optional");

            this.Elements = elements;
            this.Elements.ForEach(x => x.Parent = this);
            this.parser = parser;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(GeometryRelationSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(ResourceSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(CalculatorMappingSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(MultiValueSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(ComponentSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(CalculationSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(ComponentInstanceSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(GeoMapSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(SitePlannerSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(DataMappingSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(ChatItemSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(AssetSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(UserComponentListSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(FlowNetworkSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(ParamStructCommonSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFStructArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFStructArrayEntryParserElement(ValueMappingSaveCode code, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, parser, elements) { }

        /// <inheritdoc />
        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var value) = reader.GetLast();
            var count = DXFDataConverter<int>.P.FromDXFString(value, info);

            DXFParserResultSet elementSet = new DXFParserResultSet();
            T[] data = new T[count];

            bool skipped = false;

            for (int i = 0; i < count; ++i)
            {
                elementSet.Clear();

                foreach (var element in Elements)
                {
                    if (info.FileVersion >= element.MinVersion && info.FileVersion <= element.MaxVersion)
                    {
                        if (!skipped)
                        {
                            (key, value) = reader.Read();
                            skipped = true;
                        }
                        else
                            (key, value) = reader.GetLast();


                        if (key == element.Code)
                        {
                            elementSet.Add(element.Code, element.ParseInternal(reader, info));
                            skipped = false;
                        }
                        else
                        {
                            if (!element.IsOptional)
                                throw new Exception(string.Format("Expected struct element Code \"{0}\" but found Code \"{1}\"", element.Code, key));
                        }
                    }
                }

                data[i] = parser(elementSet, info);
            }

            return data;
        }
    }
}
