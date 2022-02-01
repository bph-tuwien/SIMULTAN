using SIMULTAN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Contains informations about reserved parameter names.
    /// These names are used for special purposes by the data model or by the Geometry connection
    /// </summary>
	public static class ReservedParameters
    {
        public const string RP_COST_POSITION = "Kᴇʜ";
        public const string RP_COST_NET = "Kɴᴇ";
        public const string RP_COST_TOTAL = "Kᴃʀ";

        public const string RP_COUNT = "NRᴛᴏᴛᴀʟ";
        public const string RP_AREA = "A";
        public const string RP_WIDTH = "b";
        public const string RP_HEIGHT = "h";
        public const string RP_DIAMETER = "d";
        public const string RP_LENGTH = "L";

        public const string RP_AREA_MAX = "AᴍᴀX";
        public const string RP_WIDTH_MAX = "BᴍᴀX";
        public const string RP_HEIGHT_MAX = "HᴍᴀX";
        public const string RP_DIAMETER_MAX = "DᴍᴀX";
        public const string RP_LENGTH_MAX = "LᴍᴀX";

        public const string RP_AREA_MIN = "AᴍɪN";
        public const string RP_WIDTH_MIN = "BᴍɪN";
        public const string RP_HEIGHT_MIN = "HᴍɪN";
        public const string RP_DIAMETER_MIN = "DᴍɪN";
        public const string RP_LENGTH_MIN = "LᴍɪN";

        public const string RP_LENGTH_MIN_TOTAL = "LᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_AREA_MIN_TOTAL = "AᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_VOLUME_MIN_TOTAL = "VᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_LENGTH_MAX_TOTAL = "LᴍᴀXᴛᴏᴛᴀʟ";
        public const string RP_AREA_MAX_TOTAL = "AᴍᴀXᴛᴏᴛᴀʟ";
        public const string RP_VOLUME_MAX_TOTAL = "VᴍᴀXᴛᴏᴛᴀʟ";

        public const string RP_AREA_IN = "A1";
        public const string RP_AREA_OUT_MAIN = "A2";
        public const string RP_AREA_OUT_BRANCH = "A3";
        public const string RP_AREA_SUGGESTION = "Ax";
        public const string RP_FLOW = "V̇";
        public const string RP_SPEED = "w";
        public const string RP_SPEED_IN = "w1";
        public const string RP_SPEED_OUT_MAIN = "w2";
        public const string RP_SPEED_OUT_BRANCH = "w3";
        public const string RP_PRESS_IN = "ΔPin";
        public const string RP_PRESS_IN_MAIN = "ΔPin2";
        public const string RP_PRESS_IN_BRANCH = "ΔPin3";
        public const string RP_PRESS_OUT = "ΔPout";
        public const string RP_PRESS_OUT_MAIN = "ΔPout2";
        public const string RP_PRESS_OUT_BRANCH = "ΔPout3";
        public const string RP_RES_CORRECTION = "Zc";

        public const string RP_K_FOK = "Kꜰᴏᴋ";
        public const string RP_K_FOK_ROH = "Kꜰᴏᴋʀ";
        public const string RP_K_F_AXES = "Kꜰᴀ";
        public const string RP_K_DUK = "Kᴅᴜᴋ";
        public const string RP_K_DUK_ROH = "Kᴅᴜᴋʀ";
        public const string RP_K_D_AXES = "Kᴅᴀ";
        public const string RP_H_NET = "Hʟɪᴄʜᴛ";
        public const string RP_H_GROSS = "Hʀᴏʜ";
        public const string RP_H_AXES = "Hᴀ";
        public const string RP_L_PERIMETER = "Lᴘᴇʀ";
        public const string RP_AREA_BGF = "Aᴃɢꜰ";
        public const string RP_AREA_NGF = "Aɴɢꜰ";
        public const string RP_AREA_NF = "Aɴꜰ";
        public const string RP_AREA_AXES = "Aᴀ";
        public const string RP_VOLUME_BRI = "Vᴃʀɪ";
        public const string RP_VOLUME_NRI = "Vɴʀɪ";
        public const string RP_VOLUME_NRI_NF = "Vɴʀɪɴꜰ";
        public const string RP_VOLUME_AXES = "Vᴀ";

        public const string RP_MATERIAL_COMPOSITE_D_OUT = "Δdout";
        public const string RP_MATERIAL_COMPOSITE_D_IN = "Δdin";

        public const string RP_ORIENTATION_HRZ = "➽"; // U+27BD
        public const string RP_TABLE_POINTER = "❏"; // U+274F
        public const string RP_AGGREGATION_OPERATION = "❖"; // U+2756
        public const string RP_LABEL_SOURCE = "❝"; // U+275D
        public const string RP_INST_PROPAGATE = "▼"; // U+25BC

        public const string RP_PARAM_TO_GEOMETRY = "➤"; //U+27A4

        //Value Pointer
        public const string MVBT_OFFSET_X_FORMAT = "{0}.ValuePointer.OffsetColumn";
        public const string MVBT_OFFSET_Y_FORMAT = "{0}.ValuePointer.OffsetRow";

        public const string MVF_OFFSET_X_FORMAT = "{0}.ValuePointer.OffsetX";
        public const string MVF_OFFSET_Y_FORMAT = "{0}.ValuePointer.OffsetY";

        public const string MVT_OFFSET_X_FORMAT = "{0}.ValuePointer.OffsetX";
        public const string MVT_OFFSET_Y_FORMAT = "{0}.ValuePointer.OffsetY";
        public const string MVT_OFFSET_Z_FORMAT = "{0}.ValuePointer.OffsetZ";
    }
}
