using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using System;

namespace SIMULTAN.Serializer.JSON
{


    /// <summary>
    /// Serializable class of <see cref="SimDoubleParameter"/>
    /// </summary>
    public class SimDoubleParameterSerializable : SimBaseParameterSerializable
    {
        /// <summary>
        /// Unit of the parameter
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
        /// Creates a new instance of SimDoubleParameterSerializable
        /// </summary>
        /// <param name="param">The parameter which is serialized</param>
        public SimDoubleParameterSerializable(SimDoubleParameter param) : base(param)
        {
            this.Unit = param.Unit;
            this.ValueMin = DXFDataConverter<double>.P.ToDXFString(param.ValueMin);
            this.ValueMax = DXFDataConverter<double>.P.ToDXFString(param.ValueMax);
            this.Value = DXFDataConverter<double>.P.ToDXFString(param.Value);
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimDoubleParameterSerializable() { throw new NotImplementedException(); }
    }
}
