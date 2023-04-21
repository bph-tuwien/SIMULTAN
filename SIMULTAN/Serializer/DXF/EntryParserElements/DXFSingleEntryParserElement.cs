using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// DXF entry for a single key-value pair data. Uses the <see cref="DXFDataConverter"/> to parse the data
    /// </summary>
    /// <typeparam name="T">Type of data in this entry</typeparam>
    public class DXFSingleEntryParserElement<T> : DXFEntryParserElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ComponentSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(UserSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ProjectSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(CalculatorMappingSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ComponentInstanceSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ParamStructCommonSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(MultiValueSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ParameterSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(CalculationSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ComponentAccessTrackerSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(GeoMapSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(SitePlannerSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(DataMappingSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ChatItemSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ResourceSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(AssetSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(UserComponentListSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(FlowNetworkSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(SimNetworkSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(ValueMappingSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(TaxonomySaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFSingleEntryParserElement(GeometryRelationSaveCode code) : base((int)code) { }

        /// <inheritdoc />
        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var value) = reader.GetLast();
            return DXFDataConverter<T>.P.FromDXFString(value, info);
        }
    }
}
