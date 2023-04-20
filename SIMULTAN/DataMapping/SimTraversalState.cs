using SIMULTAN.Data.Geometry;
using SIMULTAN.Projects;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Stores the state during a mapping operation. Stores the current writing position and objects that have already been visited.
    /// </summary>
    public class SimTraversalState
    {
        /// <summary>
        /// The current position
        /// </summary>
        public IntIndex2D CurrentPosition { get; set; } = new IntIndex2D(0,0);

        /// <summary>
        /// Number of objects that have already matched. This value is compared to <see cref="SimDataMappingRuleBase{TPropertyEnumeration, TFilter}.MaxMatches"/>
        /// </summary>
        public int MatchCount { get; set; } = 0;
        /// <summary>
        /// The depth in which the current traversal is. This value is compared to <see cref="SimDataMappingRuleBase{TPropertyEnumeration, TFilter}.MaxDepth"/>
        /// </summary>
        public int Depth { get; set; } = 0;

        /// <summary>
        /// When set to True, the root object passed to a rule is matched, otherwise the root is ignored and only the children are handled.
        /// </summary>
        public bool IncludeRoot { get; set; } = true;

        /// <summary>
        /// Stores which objects have already been visited by the rules
        /// </summary>
        public HashSet<object> VisitedObjects { get; set; } = new HashSet<object>();

        /// <summary>
        /// Stores a list of <see cref="GeometryModel"/> that were loaded during the traversal and needs to be released afterwards
        /// with a call to <see cref="ReleaseModels(ProjectData)"/>
        /// </summary>
        public List<GeometryModel> ModelsToRelease { get; set; } = new List<GeometryModel>();

        /// <summary>
        /// The range into which the results have been written
        /// </summary>
        public RowColumnRange Range { get; set; } = new RowColumnRange(-1, -1, -1, -1);
    
        /// <summary>
        /// Releases all models loaded during the traversal
        /// </summary>
        /// <param name="projectData">The project data in which the geometry models have been loaded</param>
        public void ReleaseModels(ProjectData projectData)
        {
            foreach (var gm in ModelsToRelease)
                projectData.GeometryModels.RemoveGeometryModel(gm);
        }
    }
}
