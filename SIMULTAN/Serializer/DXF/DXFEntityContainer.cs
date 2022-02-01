using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    internal class DXFEntityContainer : DXFEntity
    {
        #region CLASS MEMBERS

        internal List<DXFEntity> EC_Entities;

        internal List<long> dxf_ids_of_children_for_deferred_adding;

        #endregion

        #region .CTOR

        public DXFEntityContainer()
            : base()
        {
            this.ENT_HasEntites = true;
            this.EC_Entities = new List<DXFEntity>();

            this.dxf_ids_of_children_for_deferred_adding = new List<long>();
        }

        #endregion

        #region OVERRIDES

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        internal override bool AddEntity(DXFEntity _e)
        {
            if (_e != null)
                this.EC_Entities.Add(_e);
            return (_e != null);
        }

        public override string ToString()
        {
            string dxfS = "DXF ENTITY CONTAINER:";
            if (this.ENT_Name != null && this.ENT_Name.Count() > 0)
                dxfS += ": " + this.ENT_Name;
            int n = this.EC_Entities.Count;
            dxfS += " has " + n.ToString() + " entities:\n";
            for (int i = 0; i < n; i++)
            {
                dxfS += "_[ " + i + "]_" + this.EC_Entities[i].ToString() + "\n";
            }
            dxfS += "\n";
            return dxfS;
        }

        #endregion
    }
}
