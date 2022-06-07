using SIMULTAN.Data.Assets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Projects
{
    /// <summary>
    /// Defines a reference to another resource file
    /// </summary>
    public class ResourceReference
    {
        /// <summary>
        /// GUID of project which contains the referenced resource
        /// </summary>
        public Guid ProjectId { get; private set; }

        /// <summary>
        /// Key (index) of resource in the asset manager of the target project
        /// </summary>
        public int ResourceIndex { get; private set; }

        /// <summary>
        /// Path to the referenced resource file
        /// </summary>
        public ResourceFileEntry ResourceFile { get; private set; }

        /// <summary>
        /// Returns whether the resource exists
        /// </summary>
        public bool Exists => ResourceFile != null;

        /// <summary>
        /// Initializes a new instance of the ResourceReference class
        /// </summary>
        /// <param name="projectId">GUID of project containing the referenced resource</param>
        /// <param name="resourceIndex">Key (index) of resource in the asset manager of the target project</param>
        public ResourceReference(Guid projectId, int resourceIndex)
        {
            this.ProjectId = projectId;
            this.ResourceIndex = resourceIndex;
        }

        /// <summary>
        /// Initializes a new instance of the ResourceReference class
        /// </summary>
        /// <param name="projectId">GUID of project containing the referenced resource</param>
        /// <param name="resourceIndex">Key (index) of resource in the asset manager of the target project</param>
        /// <param name="assetManager">Asset manager of target project which contains the referenced resource</param>
        public ResourceReference(Guid projectId, int resourceIndex, AssetManager assetManager)
            : this(projectId, resourceIndex)
        {
            LazyLoad(assetManager);
        }

        /// <summary>
        /// Initializes a new instance of the ResourceReference class
        /// </summary>
        /// <param name="projectId">GUID of project containing the referenced resource</param>
        /// <param name="res">ResourceFileEntry</param>
        public ResourceReference(Guid projectId, ResourceFileEntry res)
            : this(projectId, res.Key)
        {
            this.ResourceFile = res;
        }

        /// <summary>
        /// Loads the path to the referenced resource according to the given asset manager.
        /// This action can be performed in a lazy fashion if the asset manager is not available at construction time.
        /// </summary>
        /// <param name="assetManager">Asset manager of target project which contains the referenced resource</param>
        public void LazyLoad(AssetManager assetManager)
        {
            if (ResourceFile == null)
                ResourceFile = assetManager.GetResource(ResourceIndex) as ResourceFileEntry;
        }

        /// <summary>
        /// Converts this reference into a string for writing to a DXF file
        /// </summary>
        /// <param name="res">Reference to convert to string representation</param>
        /// <returns>Reference in string representation suitable for DXF export</returns>
        public static string ToDXF(ResourceReference res)
        {
            if (res == null)
                return string.Empty;

            return string.Format("{0} {1}", res.ProjectId.ToString(), res.ResourceIndex);
        }

        /// <summary>
        /// Converts a given string read from a DXF file to a resource reference.
        /// </summary>
        /// <param name="dxf">Reference in string representation read from DXF file</param>
        /// <param name="assetManager">AssetManager which contains</param>
        /// <returns>ResourceReference converted from DXF string or null if it is an empty reference</returns>
        public static ResourceReference FromDXF(string dxf, AssetManager assetManager)
        {
            if (dxf == string.Empty)
                return null;

            try
            {
                string[] parts = dxf.Split(' ');
                Guid projectId = new Guid(parts[0]);
                int resourceIndex = int.Parse(parts[1]);

                return new ResourceReference(projectId, resourceIndex, assetManager);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Compares resource reference using project ID and resource key.
        /// </summary>
        /// <param name="obj">Other ResourceReference</param>
        /// <returns>true, if obj represents the same resource</returns>
        public override bool Equals(object obj)
        {
            var reference = obj as ResourceReference;
            return reference != null &&
                   ProjectId.Equals(reference.ProjectId) &&
                   ResourceIndex == reference.ResourceIndex;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return ProjectId.GetHashCode() ^ ResourceIndex;
        }
    }
}
