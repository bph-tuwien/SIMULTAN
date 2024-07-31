using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using System;

namespace SIMULTAN.Serializer.JSON
{

    /// <summary>
    /// JSON serializable of <see cref="SimIntegerParameter"/> 
    /// </summary>
    public class SimIntegerParameterSerializable : SimBaseParameterSerializable
    {
        /// <summary>
        /// The unit of the parameter
        /// </summary>
        public string Unit { get; set; }
        /// <summary>
        /// Maximum value
        /// </summary>
        public string ValueMax { get; set; }
        /// <summary>
        /// Minimum value
        /// </summary>
        public string ValueMin { get; set; }
        /// <summary>
        /// Creates a new instance of SimIntegerParameterSerializable
        /// </summary>
        /// <param name="param">The parameter which is serialized</param>
        public SimIntegerParameterSerializable(SimIntegerParameter param) : base(param)
        {
            this.Unit = DXFDataConverter<string>.P.ToDXFString(param.Unit);
            this.ValueMin = DXFDataConverter<int>.P.ToDXFString(param.ValueMin);
            this.ValueMax = DXFDataConverter<int>.P.ToDXFString(param.ValueMax);
            this.Value = DXFDataConverter<int>.P.ToDXFString(param.Value);
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimIntegerParameterSerializable() { throw new NotImplementedException(); }
    }
}
