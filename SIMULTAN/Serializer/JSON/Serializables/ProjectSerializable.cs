using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="ProjectData"/>
    /// </summary>
    public class ProjectSerializable
    {
        /// <summary>
        /// The global ID of the project
        /// </summary>
        public string GlobalId { get; set; }

        /// <summary>
        /// Version of the SIMULTAN file
        /// </summary>
        public ulong FileVersion { get; } = 0;

        /// <summary>
        /// Components contained in the project
        /// </summary>
        public List<SimComponentSerializable> Components { get; set; } = new List<SimComponentSerializable>();
        /// <summary>
        /// SimNetworks contained in the project
        /// </summary>
        public List<SimNetworkSerializable> SimNetworks { get; set; } = new List<SimNetworkSerializable>();


        /// <summary>
        /// To JSON serializable of <see cref="ProjectData"/>
        /// </summary>
        /// <param name="projectData">The project to export</param>
        public ProjectSerializable(ProjectData projectData)
        {
            this.GlobalId = projectData.Owner.GlobalID.ToString();
            for (int i = 0; i < projectData.Components.Count; i++)
            {
                this.Components.Add(new SimComponentSerializable(projectData.Components[i]));
            }
            for (int i = 0; i < projectData.SimNetworks.Count; i++)
            {
                this.SimNetworks.Add(new SimNetworkSerializable(projectData.SimNetworks[i]));
            }
        }
        /// <summary>
        /// Creates a new instance of ProjectSerializable
        /// </summary>
        /// <param name="projectData">The project to export</param>
        /// <param name="components">Components</param>
        /// <param name="networks">SimNetworks</param>
        public ProjectSerializable(ProjectData projectData, List<SimComponentSerializable> components, List<SimNetworkSerializable> networks)
        {
            this.GlobalId = projectData.Owner.GlobalID.ToString();
            this.Components.AddRange(components);
            this.SimNetworks.AddRange(networks);
        }


        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private ProjectSerializable() { throw new NotImplementedException(); }
    }
}
