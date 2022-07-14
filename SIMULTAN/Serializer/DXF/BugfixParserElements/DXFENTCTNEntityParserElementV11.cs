using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Wrapper element for object which end on 0: ENTCTN. This was actually a bug in pre-version 12 files
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DXFENTCTNEntityParserElementV11<T> : DXFEntityParserElementBase<T>
    {
        private DXFEntityParserElementBase<T> content;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFENTCTNEntityParserElementV11{T}"/> class
        /// </summary>
        /// <param name="content">The element to wrap</param>
        internal DXFENTCTNEntityParserElementV11(DXFEntityParserElementBase<T> content) : base(content.EntityName, content.Entries)
        {
            this.content = content;
            this.content.Parent = this;
        }

        /// <inheritdoc />
        internal override T Parse(DXFStreamReader reader, DXFParserInfo info)
        {
            var obj = this.content.Parse(reader, info);

            if (info.FileVersion <= 11)
            {
                (var key, var value) = reader.GetLast();
                if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
                    throw new Exception(string.Format("Expected Code \"{0}\" but found Code \"{1}\" while parsing ENTITY_CONTINUE Entity",
                        ParamStructCommonSaveCode.ENTITY_START, key));
                if (value != ParamStructTypes.ENTITY_CONTINUE)
                    throw new Exception(string.Format("Expected Entity Name \"{0}\" but found \"{1}\" while parsing ENTITY_CONTINUE Entity",
                        ParamStructTypes.ENTITY_CONTINUE, value));


                //Move to next entry because thats what automatically happens in the normal entities
                reader.Read();
            }

            return obj;
        }
    }
}
