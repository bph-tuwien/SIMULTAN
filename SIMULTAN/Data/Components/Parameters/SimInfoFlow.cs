using SIMULTAN.Serializer.DXF;
using System;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Specifies the type of operations expected on a <see cref="SimBaseParameter"/>
    /// </summary>
    [DXFSerializerTypeNameAttribute("ParameterStructure.Component.InfoFlow")]
    public enum SimInfoFlow
    {
        /// <summary>
        /// The parameter is used for a user to input values
        /// </summary>
        Input = 0,
        /// <summary>
        /// The parameter is used for outputs of <see cref="SimCalculation"/> instances
        /// </summary>
        Output = 1,
        /// <summary>
        /// The parameter can be either used as <see cref="Input"/> or <see cref="Output"/>
        /// </summary>
        Mixed = 2,
        /// <summary>
        /// This parameter receives value from a referenced parameter
        /// </summary>
        FromReference = 3,
        /// <summary>
        /// The parameter's value is supplied by the data model itself. Used, for example, for parameters which store geometric parameters
        /// </summary>
        Automatic = 4,
        /// <summary>
        /// The parameter syncs its values with an external application, e.g., from an Excel tool or from a Simulation
        /// </summary>
        SyncedWithExternal = 6,
        [Obsolete("Please use SyncedWithExternal instead")]
        FromExternal = 6,

        //Do not use 5. This has been used for TYPE in ancient versions
    }
}
