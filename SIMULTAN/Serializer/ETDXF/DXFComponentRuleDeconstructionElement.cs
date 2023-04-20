using SIMULTAN.Data.Components;
using SIMULTAN.DataMapping;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.ETDXF
{
    internal class DXFComponentRuleDeconstructionElement : DXFEntityParserElementBase<SimDataMappingRuleComponent>
    {
        private DXFEntityParserElementBase<(SimDataMappingRuleComponent, SimComponent[])> sourceElement;

        public DXFComponentRuleDeconstructionElement(DXFEntityParserElementBase<(SimDataMappingRuleComponent, SimComponent[])> sourceElement)
            : base(sourceElement.EntityName, sourceElement.Entries)
        {
            this.sourceElement = sourceElement;
        }

        internal override SimDataMappingRuleComponent Parse(DXFStreamReader reader, DXFParserInfo info)
        {
            return sourceElement.Parse(reader, info).Item1;
        }
    }
}
