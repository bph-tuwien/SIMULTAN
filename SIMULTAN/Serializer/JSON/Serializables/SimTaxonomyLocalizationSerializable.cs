using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// JSON import/export model of the <see cref="SimTaxonomyLocalizationEntry"/>
    /// </summary>
    public class SimTaxonomyLocalizationSerializable
    {
        /// <summary>
        /// The culture code of the entry. <see cref="CultureInfo.Name"/>
        /// </summary>
        public string CultureCode { get; set; }
        /// <summary>
        /// The name of the entry
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The description of the entry
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// Creates a new <see cref="SimTaxonomyLocalizationSerializable"/>
        /// </summary>
        public SimTaxonomyLocalizationSerializable()
        { }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyLocalizationSerializable"/>
        /// </summary>
        /// <param name="cultureCode">The culture code</param>
        /// <param name="name">The name</param>
        /// <param name="description">The description</param>
        public SimTaxonomyLocalizationSerializable(string cultureCode, string name, string description)
        {
            this.CultureCode = cultureCode;
            this.Name = name;
            this.Description = description;
        }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyLocalizationSerializable"/> from an <see cref="SimTaxonomyLocalizationEntry"/>
        /// </summary>
        /// <param name="entry">The entry</param>
        public SimTaxonomyLocalizationSerializable(SimTaxonomyLocalizationEntry entry) :
            this(entry.Culture.Name, entry.Name, entry.Description)
        {
        }
    }
}
