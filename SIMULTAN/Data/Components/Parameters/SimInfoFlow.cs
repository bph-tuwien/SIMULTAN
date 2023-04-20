using SIMULTAN.Serializer.DXF;

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
        Input = 0,      // pure input !
        /// <summary>
        /// The parameter is used for outputs of <see cref="SimCalculation"/> instances
        /// </summary>
        Output = 1,      // pure output ?
        /// <summary>
        /// The parameter can be either used as <see cref="Input"/> or <see cref="Output"/>
        /// </summary>
        Mixed = 2,      // can be both input and output @
        /// <summary>
        /// This parameter receives value from a referenced parameter
        /// </summary>
        FromReference = 3,     // takes input from a referenced component "
        /// <summary>
        /// The parameter's value is supplied by the data model itself. Used, for example, for parameters which store geometric parameters
        /// </summary>
        Automatic = 4,    // cumulative values from network-bound instances &
        /// <summary>
        /// The parameter receives values from an external application, e.g., from an Excel tool
        /// </summary>
        FromExternal = 6, // values coming from external tools (e.g. Excel) /

        //Do not use 5. This has been used for TYPE in ancient versions
    }
}
