using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFAccessProfile : DXFEntityContainer
    {
        #region CLASS MEMBERS

        public SimComponentValidity dxf_profile_state;
        private int nr_entries;
        public SimAccessProfile dxf_parsed;

        #endregion

        #region OVERRIDES

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ComponentAccessProfileSaveCode.STATE:
                    this.dxf_profile_state = ComponentUtils.StringToComponentValidity(this.Decoder.FValue);
                    break;
                case (int)ComponentAccessProfileSaveCode.PROFILE:
                    this.nr_entries = this.Decoder.IntValue();
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        internal override bool AddEntity(DXFEntity _e)
        {
            if (_e == null) return false;

            DXFAccessTracker at = _e as DXFAccessTracker;
            if (at == null) return false;

            if (this.nr_entries <= this.EC_Entities.Count) return false;

            this.EC_Entities.Add(_e);
            return true;
        }

        internal override void OnLoaded()
        {
            base.OnLoaded();

            Dictionary<SimUserRole, SimAccessProfileEntry> profile = new Dictionary<SimUserRole, SimAccessProfileEntry>();
            foreach (DXFEntity e in this.EC_Entities)
            {
                DXFAccessTracker at = e as DXFAccessTracker;
                if (at == null) continue;
                if (at.dxf_parsed == null) continue;

                profile.Add(at.dxf_parsed.Role, at.dxf_parsed);
            }

            if (this.nr_entries != profile.Count) return;
            this.dxf_parsed = new SimAccessProfile(profile);
        }

        #endregion
    }
}
