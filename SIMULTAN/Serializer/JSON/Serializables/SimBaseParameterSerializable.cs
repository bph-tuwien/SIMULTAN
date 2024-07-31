using SIMULTAN.Data.Components;
using System;
using System.Text.Json.Serialization;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// JSON serializable for parameters <see cref="SimBaseParameter"/> each type has it´s own subtype:
    /// <see cref="SimDoubleParameterSerializable"/> for <see cref="SimDoubleParameter"/>
    /// <see cref="SimIntegerParameterSerializable"/> for <see cref="SimIntegerParameter"/>
    /// <see cref="SimStringParameterSerializable"/> for <see cref="SimStringParameter"/>
    /// <see cref="SimBoolParameterSerializable"/> for <see cref="SimBoolParameter"/>
    /// <see cref="SimEnumParameterSerializable"/> for <see cref="SimEnumParameter"/>
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SimBoolParameterSerializable), typeDiscriminator: "Boolean")]
    [JsonDerivedType(typeof(SimDoubleParameterSerializable), typeDiscriminator: "Double")]
    [JsonDerivedType(typeof(SimEnumParameterSerializable), typeDiscriminator: "Enum")]
    [JsonDerivedType(typeof(SimIntegerParameterSerializable), typeDiscriminator: "Integer")]
    [JsonDerivedType(typeof(SimStringParameterSerializable), typeDiscriminator: "String")]
    public abstract class SimBaseParameterSerializable
    {
        /// <summary>
        /// ID of the parameter
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Name of the parameter
        /// </summary>
        public SimTaxonomyEntryOrStringSerializable Name { get; set; }
        /// <summary>
        /// Category of the parameter
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Whether the parameter was generated automatically
        /// </summary>
        public bool IsAutomaticallyGenerated { get; set; }
        /// <summary>
        /// Description of the parameter
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Value of the parameter
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Creates a new instance of SimBaseParameterSerializable
        /// </summary>
        /// <param name="param">The parameter which is serialized</param>
        public SimBaseParameterSerializable(SimBaseParameter param)
        {
            this.Id = param.LocalID;
            this.Name = new SimTaxonomyEntryOrStringSerializable(param.NameTaxonomyEntry);
            this.Category = param.Category.ToString();
            this.IsAutomaticallyGenerated = param.IsAutomaticallyGenerated;
            this.Description = param.Description;
        }

        /// <summary>
        /// DO NOT USE. Only required for the XMLSerializer class to operate on this type
        /// </summary>
        protected SimBaseParameterSerializable() { throw new NotImplementedException(); }
    }
}
