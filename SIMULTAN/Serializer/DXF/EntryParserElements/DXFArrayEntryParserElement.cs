using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Entry element for a DXF array. Arrays consist of a count (with the code given in the element) and a number of entries.
    /// </summary>
    public abstract class DXFArrayEntryParserElement : DXFEntryParserElement
    {
        /// <summary>
        /// The code of the entry
        /// </summary>
        public int ElementCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFArrayEntryParserElement"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        /// <param name="elementCode">The code of the elements in the array</param>
        internal DXFArrayEntryParserElement(int code, int elementCode) : base(code)
        {
            this.ElementCode = elementCode;
        }
    }

    /// <summary>
    /// Entry element for a DXF array. Arrays consist of a count (with the code given in the element) and a number of entries.
    /// </summary>
    /// <typeparam name="T">Type of the array elements</typeparam>
    public class DXFArrayEntryParserElement<T> : DXFArrayEntryParserElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="elementCode">The code for the data elements</param>
        internal DXFArrayEntryParserElement(int code, int elementCode) : base(code, elementCode)
        { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="elementCode">The code for the data elements</param>
        internal DXFArrayEntryParserElement(MultiValueSaveCode code, ParamStructCommonSaveCode elementCode)
            : this((int)code, (int)elementCode) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="elementCode">The code for the data elements</param>
        internal DXFArrayEntryParserElement(ComponentSaveCode code, ParamStructCommonSaveCode elementCode)
            : this((int)code, (int)elementCode) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="elementCode">The code for the data elements</param>
        internal DXFArrayEntryParserElement(CalculationSaveCode code, ParamStructCommonSaveCode elementCode)
            : this((int)code, (int)elementCode) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="elementCode">The code for the data elements</param>
        internal DXFArrayEntryParserElement(ComponentInstanceSaveCode code, ParamStructCommonSaveCode elementCode)
            : this((int)code, (int)elementCode) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="elementCode">The code for the data elements</param>
        internal DXFArrayEntryParserElement(ChatItemSaveCode code, ParamStructCommonSaveCode elementCode)
            : this((int)code, (int)elementCode) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFArrayEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="elementCode">The code for the data elements</param>
        internal DXFArrayEntryParserElement(UserComponentListSaveCode code, ParamStructCommonSaveCode elementCode)
            : this((int)code, (int)elementCode) { }

        /// <inheritdoc />
        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var value) = reader.GetLast();
            int count = DXFDataConverter<int>.P.FromDXFString(value, info);

            T[] data = new T[count];

            for (int i = 0; i < count; i++)
            {
                (key, value) = reader.Read();
                if (key != ElementCode)
                {
                    throw new Exception(
                        String.Format("Failed to read array element of array {0}: Expected code {1} but found code {2} for index {3}",
                            Code, ElementCode, key, i
                            ));
                }

                data[i] = DXFDataConverter<T>.P.FromDXFString(value, info);
            }

            return data;
        }
    }
}
