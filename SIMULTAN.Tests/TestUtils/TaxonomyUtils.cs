using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.TXDXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.TestUtils
{
    public static class TaxonomyUtils
    {
        /// <summary>
        /// Loads and returns the default taxonomies without loading a whole project.
        /// </summary>
        /// <returns>The default taxonomies</returns>
        public static SimTaxonomyCollection GetDefaultTaxonomies()
        {
            var assembly = Assembly.GetAssembly(typeof(SimTaxonomy));
            using (var default_tax_stream = assembly.GetManifestResourceStream("SIMULTAN.Data.Taxonomy.Default.default_taxonomies.txdxf"))
            {
                var dummyProjectData = new ExtendedProjectData();
                dummyProjectData.SetCallingLocation(new DummyReferenceLocation(Guid.Empty));
                var tmpParserInfo = new DXFParserInfo(Guid.Empty, dummyProjectData);
                SimTaxonomyDxfIO.Import(new DXFStreamReader(default_tax_stream), tmpParserInfo);
                return dummyProjectData.Taxonomies;
            }
        }

        public static void LoadDefaultTaxonomies(ExtendedProjectData projectData)
        {
            var assembly = Assembly.GetAssembly(typeof(SimTaxonomy));
            using (var default_tax_stream = assembly.GetManifestResourceStream("SIMULTAN.Data.Taxonomy.Default.default_taxonomies.txdxf"))
            {
                var tmpParserInfo = new DXFParserInfo(projectData.Taxonomies.CalledFromLocation.GlobalID, projectData);
                SimTaxonomyDxfIO.Import(new DXFStreamReader(default_tax_stream), tmpParserInfo);
            }
        }

        /// <summary>
        /// Quickly gets a single default slot taxonomy entry, only use when you do not need more than a single one.
        /// Otherwise get the taxonomies and then the entries manually.
        /// </summary>
        /// <param name="key">The slot taxonomy entry key</param>
        /// <returns>The taxonomy entry for the slot</returns>
        internal static SimTaxonomyEntry GetDefaultSlot(string key)
        {
            var taxonomies = GetDefaultTaxonomies();
            return taxonomies.GetDefaultSlot(key);
        }
    }
}
