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
    /// Entry element for a List of Lists element
    /// </summary>
    public abstract class DXFNestedListEntryParserElement : DXFEntryParserElement
    {
        /// <summary>
        /// A collection of entries in the list
        /// </summary>
        public IEnumerable<DXFEntryParserElement> Elements { get; }
        /// <summary>
        /// The continue code of the nested list
        /// </summary>
        public int ContinueCode { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFNestedListEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="continueCode">The code of the continue element</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFNestedListEntryParserElement(int code, int continueCode,
            IEnumerable<DXFEntryParserElement> elements)
            : base(code)
        {
            this.ContinueCode = continueCode;
            this.Elements = elements;
            this.Elements.ForEach(x => x.Parent = this);
        }
    }

    /// <summary>
    /// Entry element for a List of Lists element
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    public class DXFNestedListEntryParserElement<T> : DXFNestedListEntryParserElement
    {
        private Func<DXFParserResultSet, T> parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFNestedListEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="continueCode">The code of the continue element</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFNestedListEntryParserElement(int code, int continueCode, Func<DXFParserResultSet, T> parser,
            IEnumerable<DXFEntryParserElement> elements)
            : base(code, continueCode, elements)
        {
            this.parser = parser;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFNestedListEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        /// <param name="continueCode">The code of the continue element</param>
        /// <param name="parser">Method that converts the parsed data of a single element into the desired target type</param>
        /// <param name="elements">The elements in each entry</param>
        internal DXFNestedListEntryParserElement(MultiValueSaveCode code, ParamStructCommonSaveCode continueCode,
            Func<DXFParserResultSet, T> parser, IEnumerable<DXFEntryParserElement> elements)
            : this((int)code, (int)continueCode, parser, elements)
        { }

        /// <inheritdoc />
        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var value) = reader.GetLast();
            var count = DXFDataConverter<int>.P.FromDXFString(value, info);

            List<List<T>> result = new List<List<T>>(count);
            DXFParserResultSet elementSet = new DXFParserResultSet();

            for (int i = 0; i < count; i++)
            {
                List<T> currentList = new List<T>();
                int continueValue = ParamStructTypes.LIST_CONTINUE;

                while (continueValue == ParamStructTypes.LIST_CONTINUE)
                {
                    elementSet.Clear();

                    foreach (var element in Elements)
                    {
                        (key, value) = reader.Read();
                        if (key != element.Code)
                            throw new Exception(string.Format("Expected nested list element Code \"{0}\" but found Code \"{1}\"", element.Code, key));
                        elementSet.Add(element.Code, element.ParseInternal(reader, info));
                    }

                    currentList.Add(parser(elementSet));

                    (key, value) = reader.Read();
                    if (key != ContinueCode)
                        throw new Exception(String.Format("Expected nested list continue code \"{0}\" but found Code \"{1}\"", ContinueCode, key));
                    continueValue = DXFDataConverter<int>.P.FromDXFString(value, info);
                }

                result.Add(currentList);
            }

            return result;
        }
    }
}
