using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFUserComponentList : DXFEntity
    {
        private string dxf_Name;
        private int dxf_Count;

        internal List<SimId> dxf_comp_ids;

        internal SimUserComponentList dxf_parsed;

        internal DXFUserComponentList() : base()
        {
            dxf_Name = "";
            dxf_Count = -1;
            dxf_comp_ids = new List<SimId>();
        }

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)UserComponentListSaveCode.NAME:
                    this.dxf_Name = Decoder.FValue;
                    break;
                case (int)UserComponentListSaveCode.NR_OF_ROOT_COMPONENTS:
                    this.dxf_Count = Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.ENTITY_ID:
                    if (dxf_comp_ids.Count() < dxf_Count)
                    {
                        var ids = Decoder.GlobalAndLocalValue();
                        dxf_comp_ids.Add(new SimId(ids.global, ids.local));
                    }
                    break;

            }
        }

        internal override void OnLoaded()
        {
            this.dxf_parsed = new SimUserComponentList(dxf_Name);
            var fact = Decoder.ProjectData.UserComponentLists;

            foreach (var id in dxf_comp_ids)
            {
                var comp = Decoder.ProjectData.IdGenerator.GetById<SimComponent>(id);
                if (comp != null)
                {
                    dxf_parsed.RootComponents.Add(comp);
                }
            }

            fact.Add(dxf_parsed);
        }
    }
}
