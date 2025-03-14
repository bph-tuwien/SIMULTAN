using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON.Serializables
{
    public class SimTaxonomyLocalizationSerializable
    {
        public string CultureCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public SimTaxonomyLocalizationSerializable(string cultureCode, string name, string description)
        {
            this.CultureCode = cultureCode;
            this.Name = name;
            this.Description = description;
        }
    }
}
