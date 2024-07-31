using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using System;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// JSON serializable of the <see cref="SimBoolParameter"/>
    /// </summary>
    public class SimBoolParameterSerializable : SimBaseParameterSerializable
    {
        /// <summary>
        /// Creates a new instance of the SimBoolParameterSerializable
        /// </summary>
        /// <param name="param">The parameter which is serialized</param>
        public SimBoolParameterSerializable(SimBoolParameter param) : base(param)
        {
            this.Value = DXFDataConverter<bool>.P.ToDXFString(param.Value);
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimBoolParameterSerializable() { throw new NotImplementedException(); }
    }
}
