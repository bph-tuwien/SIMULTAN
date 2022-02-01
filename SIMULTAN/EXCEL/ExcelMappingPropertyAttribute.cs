using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Excel
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ExcelMappingPropertyAttribute : Attribute
    {
        public string LocalizationKey { get; }

        public Type PropertyType { get; set; } = null;

        public string FilterKey { get; set; } = null;

        public bool IsFilterable { get; set; } = true;

        public ExcelMappingPropertyAttribute(string localizationKey)
        {
            this.LocalizationKey = localizationKey;
        }
    }
}
