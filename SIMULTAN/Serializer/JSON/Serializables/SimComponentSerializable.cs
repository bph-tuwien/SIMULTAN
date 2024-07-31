using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="SimComponent"/>
    /// </summary>
    public class SimComponentSerializable
    {
        /// <summary>
        /// The ID of the components
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// The name of the Component
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Description of the component
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The type of the instance
        /// </summary>
        public string InstanceType { get; set; }
        /// <summary>
        /// The slots of the component
        /// </summary>
        public IEnumerable<SimTaxonomyEntryReferenceSerializable> Slots { get; set; }
        /// <summary>
        /// Contained components
        /// </summary>
        public List<SimChildComponentSerializable> Components { get; set; } = new List<SimChildComponentSerializable>();
        /// <summary>
        /// Contained parameters
        /// </summary>
        public List<SimBaseParameterSerializable> Parameters { get; set; } = new List<SimBaseParameterSerializable>();
        /// <summary>
        /// Instances of the Component
        /// </summary>
        public List<SimInstanceSerializable> Instances { get; set; } = new List<SimInstanceSerializable>();

        /// <summary>
        /// Creates a new instance of SimComponentSerializable
        /// </summary>
        /// <param name="component">The component </param>
        public SimComponentSerializable(SimComponent component)
        {
            this.Id = component.LocalID;
            this.Name = component.Name;
            this.Description = component.Description;
            this.InstanceType = component.InstanceType.ToString();
            this.Slots = component.Slots.Select(t => new SimTaxonomyEntryReferenceSerializable(t)).ToList();
            for (int i = 0; i < component.Components.Count; i++)
            {
                this.Components.Add(new SimChildComponentSerializable(component.Components[i]));
            }

            foreach (var item in component.Parameters)
            {
                if (item is SimDoubleParameter dParam)
                {
                    this.Parameters.Add(new SimDoubleParameterSerializable(dParam));
                }
                if (item is SimIntegerParameter iParam)
                {
                    this.Parameters.Add(new SimIntegerParameterSerializable(iParam));
                }
                if (item is SimStringParameter sParam)
                {
                    this.Parameters.Add(new SimStringParameterSerializable(sParam));
                }
                if (item is SimBoolParameter bParam)
                {
                    this.Parameters.Add(new SimBoolParameterSerializable(bParam));
                }
                if (item is SimEnumParameter eParam)
                {
                    this.Parameters.Add(new SimEnumParameterSerializable(eParam));
                }
            }

            for (int i = 0; i < component.Instances.Count; i++)
            {
                this.Instances.Add(new SimInstanceSerializable(component.Instances[i]));
            }
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimComponentSerializable() { throw new NotImplementedException(); }
    }
}
