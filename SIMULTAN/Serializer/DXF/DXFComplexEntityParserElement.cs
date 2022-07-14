using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Entity element wrapper for complex entities (entities which end on 0: ENDSEC)
    /// </summary>
    /// <typeparam name="T">The element type to parse</typeparam>
    public class DXFComplexEntityParserElement<T> : DXFEntityParserElementBase<T>
    {
        private DXFEntityParserElementBase<T> content;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFComplexEntityParserElement{T}"/> class
        /// </summary>
        /// <param name="content">The entity which is wrapped by this element</param>
        internal DXFComplexEntityParserElement(DXFEntityParserElementBase<T> content) : base(content.EntityName, content.Entries)
        {
            this.content = content;
            this.content.Parent = this;
        }

        /// <inheritdoc/>
        internal override T Parse(DXFStreamReader reader, DXFParserInfo info)
        {
            var obj = this.content.Parse(reader, info);

            (var key, var value) = reader.GetLast();
            if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
                throw new Exception(string.Format("Expected Code \"{0}\" but found Code \"{1}\" while parsing Complex Entity",
                    ParamStructCommonSaveCode.ENTITY_START, key));
            if (value != ParamStructTypes.SEQUENCE_END)
                throw new Exception(string.Format("Expected Entity Name \"{0}\" but found \"{1}\" while parsing Complex Entity",
                    ParamStructTypes.SEQUENCE_END, value));

            //Move to next entry because thats what automatically happens in the normal entities
            reader.Read();

            return obj;
        }
    }
}
