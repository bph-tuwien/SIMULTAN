using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper of class SimAccessProfileEntry
    /// </summary>
    internal class DXFAccessTracker : DXFEntity
    {
        #region CLASS MEMBERS

        public SimComponentAccessPrivilege dxf_AccessTypeFlags;

        public DateTime dxf_last_access_write;
        public DateTime dxf_last_access_supervize;
        public DateTime dxf_last_access_release;

        public SimAccessProfileEntry dxf_parsed;

        #endregion

        public DXFAccessTracker()
            : base()
        {
            this.dxf_AccessTypeFlags = SimComponentAccessPrivilege.None;

            this.dxf_last_access_write = DateTime.MinValue;
            this.dxf_last_access_supervize = DateTime.MinValue;
            this.dxf_last_access_release = DateTime.MinValue;
        }

        #region OVERRIDES : Read Properties

        public override void ReadPoperty()
        {
            DateTime dt_tmp;// = DateTime.Now;
            long ticks;
            bool dt_p_success = false;

            switch (this.Decoder.FCode)
            {
                case (int)ComponentAccessTrackerSaveCode.FLAGS:
                    this.dxf_AccessTypeFlags = ComponentUtils.StringToComponentAccessType(this.Decoder.FValue);
                    break;
                case (int)ComponentAccessTrackerSaveCode.WRITE_LAST:
                    ticks = this.Decoder.LongValue();
                    if (ticks > 0)
                        this.dxf_last_access_write = new DateTime(ticks).ToLocalTime();
                    else
                    {
                        // old
                        dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                        if (dt_p_success)
                            this.dxf_last_access_write = dt_tmp;
                    }
                    break;
                case (int)ComponentAccessTrackerSaveCode.SUPERVIZE_LAST:
                    ticks = this.Decoder.LongValue();
                    if (ticks > 0)
                        this.dxf_last_access_supervize = new DateTime(ticks).ToLocalTime();
                    else
                    {
                        // old
                        dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                        if (dt_p_success)
                            this.dxf_last_access_supervize = dt_tmp;
                    }
                    break;
                case (int)ComponentAccessTrackerSaveCode.RELEASE_LAST:
                    ticks = this.Decoder.LongValue();
                    if (ticks > 0)
                        this.dxf_last_access_release = new DateTime(ticks).ToLocalTime();
                    else
                    {
                        // old
                        dt_p_success = DateTime.TryParse(this.Decoder.FValue, ParamStructTypes.DT_FORMATTER, System.Globalization.DateTimeStyles.None, out dt_tmp);
                        if (dt_p_success)
                            this.dxf_last_access_release = dt_tmp;
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            var role = ComponentUtils.StringToComponentManagerType(this.ENT_KEY);

            // create the new access tracker
            this.dxf_parsed = new SimAccessProfileEntry(role, this.dxf_AccessTypeFlags,
                                                        this.dxf_last_access_write,
                                                        this.dxf_last_access_supervize,
                                                        this.dxf_last_access_release);
        }

        #endregion
    }
}
