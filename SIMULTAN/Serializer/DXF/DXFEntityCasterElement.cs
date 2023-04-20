using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Parser element for casting an object to another type. The input type has to be a subtype of the output type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    public class DXFEntityCasterElement<T, U> : DXFEntityParserElementBase<T>
        where U : T
    {
        private DXFEntityParserElementBase<U> content;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFEntityCasterElement{T, U}"/> class
        /// </summary>
        /// <param name="content">The parser element which produces the object that should be casted</param>
        public DXFEntityCasterElement(DXFEntityParserElementBase<U> content) : base(content.EntityName, content.Entries)
        {
            this.content = content;
            this.content.Parent = this;
        }

        /// <inheritdoc />
        internal override T Parse(DXFStreamReader reader, DXFParserInfo info)
        {
            var obj = content.Parse(reader, info);
            return (T)obj;
        }
    }
}
