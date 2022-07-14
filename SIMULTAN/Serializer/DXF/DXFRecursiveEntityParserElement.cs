using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Parser element to parse recursive structures. When this element is hit, it searches the parent chain to find an element
    /// with the given identifier and continues parsing from there
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public class DXFRecursiveEntityParserElement<T> : DXFEntityParserElementBase<T>
    {
        private string recursiveElementIdentifier;
        private DXFEntityParserElementBase<T> recursiveElement = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFRecursiveEntityParserElement{T}"/> class
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="recursiveElementIdentifier"></param>
        internal DXFRecursiveEntityParserElement(string entityName, string recursiveElementIdentifier) 
            : base(entityName, new DXFEntryParserElement[] { })
        {
            this.recursiveElementIdentifier = recursiveElementIdentifier;
        }

        /// <inheritdoc/>
        internal override T Parse(DXFStreamReader reader, DXFParserInfo info)
        {
            if (recursiveElement == null)
            {
                DXFParserElement element = this;
                while (element != null && element.Identifier != this.recursiveElementIdentifier)
                    element = element.Parent;

                if (element != null)
                    this.recursiveElement = element as DXFEntityParserElementBase<T>;
            }

            if (recursiveElement != null)
            {
                return this.recursiveElement.Parse(reader, info);
            }

            return default(T);
        }
    }
}
