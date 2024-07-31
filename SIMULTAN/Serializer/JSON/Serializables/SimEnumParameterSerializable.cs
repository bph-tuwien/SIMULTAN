using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Serializer.JSON
{

    /// <summary>
    /// JSON serializable of the <see cref="SimEnumParameter"/>
    /// </summary>
    public class SimEnumParameterSerializable : SimBaseParameterSerializable
    {
        /// <summary>
        /// Possible values of the enum
        /// </summary>
        public List<string> Items { get; set; }

        /// <summary>
        /// Creates a new instance of the SimEnumParameterSerializable
        /// </summary>
        /// <param name="param">The parameter which is serialized</param>
        public SimEnumParameterSerializable(SimEnumParameter param) : base(param)
        {
            if (param.Value != null)
            {
                this.Value = DXFDataConverter<string>.P.ToDXFString(param.Value.Target.Key);
            }
            else
            {
                this.Value = null;
            }

            this.Items = param.ParentTaxonomyEntryRef.Target.Children.Select(p => p.Key).ToList();
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimEnumParameterSerializable() { throw new NotImplementedException(); }
    }
}
