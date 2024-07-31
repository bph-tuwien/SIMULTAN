using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using System;

namespace SIMULTAN.Serializer.JSON
{


    /// <summary>
    /// JSON serializable of <see cref="SimStringParameter"/>
    /// </summary>
    public class SimStringParameterSerializable : SimBaseParameterSerializable
    {
        /// <summary>
        /// Creates a new instance of the SimStringParameterSerializable
        /// </summary>
        /// <param name="param">The parameter which is serialized</param>
        public SimStringParameterSerializable(SimStringParameter param) : base(param)
        {
            this.Value = DXFDataConverter<string>.P.ToDXFString(param.Value);
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimStringParameterSerializable() { throw new NotImplementedException(); }
    }
}
