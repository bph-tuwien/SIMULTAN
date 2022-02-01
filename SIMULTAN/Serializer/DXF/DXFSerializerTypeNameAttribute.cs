using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Annotates a class with a text used for DXF serialization (mostly used by the Excel Mapping)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class DXFSerializerTypeNameAttribute : Attribute
    {
        /// <summary>
        /// The type name for the Serializer
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the DXFSerializerTypeName class
        /// </summary>
        /// <param name="name"></param>
        public DXFSerializerTypeNameAttribute(string name)
        {
            this.Name = name;
        }
    }
}
