using SIMULTAN;
using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.Components
{

    public static class ReservedParameterKeys
    {
        public const string RP_TAXONOMY_KEY = "resparam";

        public const string RP_COST_POSITION = "keh";
        public const string RP_COST_NET = "kne";
        public const string RP_COST_TOTAL = "kbr";

        public const string RP_COUNT = "nrtotal";
        public const string RP_AREA = "a";
        public const string RP_WIDTH = "b";
        public const string RP_HEIGHT = "h";
        public const string RP_DIAMETER = "d";
        public const string RP_LENGTH = "l";

        public const string RP_AREA_MAX = "amax";
        public const string RP_WIDTH_MAX = "bmax";
        public const string RP_HEIGHT_MAX = "hmax";
        public const string RP_DIAMETER_MAX = "dmax";
        public const string RP_LENGTH_MAX = "lmax";

        public const string RP_AREA_MIN = "amin";
        public const string RP_WIDTH_MIN = "bmin";
        public const string RP_HEIGHT_MIN = "hmin";
        public const string RP_DIAMETER_MIN = "dmin";
        public const string RP_LENGTH_MIN = "lmin";

        public const string RP_LENGTH_MIN_TOTAL = "lmintotal";
        public const string RP_AREA_MIN_TOTAL = "amintotal";
        public const string RP_VOLUME_MIN_TOTAL = "vmintotal";
        public const string RP_LENGTH_MAX_TOTAL = "lmaxtotal";
        public const string RP_AREA_MAX_TOTAL = "amaxtotal";
        public const string RP_VOLUME_MAX_TOTAL = "vmaxtotal";

        public const string RP_AREA_IN = "a1";
        public const string RP_AREA_OUT_MAIN = "a2";
        public const string RP_AREA_OUT_BRANCH = "a3";
        public const string RP_AREA_SUGGESTION = "ax";
        public const string RP_FLOW = "v";
        public const string RP_SPEED = "w";
        public const string RP_SPEED_IN = "w1";
        public const string RP_SPEED_OUT_MAIN = "w2";
        public const string RP_SPEED_OUT_BRANCH = "w3";
        public const string RP_PRESS_IN = "pin";
        public const string RP_PRESS_IN_MAIN = "pin2";
        public const string RP_PRESS_IN_BRANCH = "pin3";
        public const string RP_PRESS_OUT = "pout";
        public const string RP_PRESS_OUT_MAIN = "pout2";
        public const string RP_PRESS_OUT_BRANCH = "pout3";
        public const string RP_RES_CORRECTION = "zc";

        public const string RP_K_FOK = "kfok";
        public const string RP_K_FOK_ROH = "kfokr";
        public const string RP_K_F_AXES = "kfa";
        public const string RP_K_DUK = "kduk";
        public const string RP_K_DUK_ROH = "kdukr";
        public const string RP_K_D_AXES = "kda";
        public const string RP_H_NET = "hlicht";
        public const string RP_H_GROSS = "hroh";
        public const string RP_H_AXES = "ha";
        public const string RP_L_PERIMETER = "lper";
        public const string RP_AREA_BGF = "abgf";
        public const string RP_AREA_NGF = "angf";
        public const string RP_AREA_NF = "anf";
        public const string RP_AREA_AXES = "aa";
        public const string RP_VOLUME_BRI = "vbri";
        public const string RP_VOLUME_NRI = "vnri";
        public const string RP_VOLUME_NRI_NF = "vnrinf";
        public const string RP_VOLUME_AXES = "va";

        public const string RP_MATERIAL_COMPOSITE_D_OUT = "dout";
        public const string RP_MATERIAL_COMPOSITE_D_IN = "din";

        public const string RP_ORIENTATION_HRZ = "ohrz"; // U+27BD
        public const string RP_TABLE_POINTER = "tp"; // U+274F
        public const string RP_AGGREGATION_OPERATION = "ao"; // U+2756
        public const string RP_LABEL_SOURCE = "ls"; // U+275D

        public const string RP_PARAM_TO_GEOMETRY = "ptg"; //U+27A4

        public static Dictionary<String, String> NameToKeyLookup = new Dictionary<string, string> {
            { ReservedParameters.RP_COST_POSITION , RP_COST_POSITION }, 
            { ReservedParameters.RP_COST_NET , RP_COST_NET }, 
            { ReservedParameters.RP_COST_TOTAL , RP_COST_TOTAL }, 

            { ReservedParameters.RP_COUNT , RP_COUNT }, 
            { ReservedParameters.RP_AREA , RP_AREA }, 
            { ReservedParameters.RP_WIDTH , RP_WIDTH }, 
            { ReservedParameters.RP_HEIGHT , RP_HEIGHT }, 
            { ReservedParameters.RP_DIAMETER , RP_DIAMETER }, 
            { ReservedParameters.RP_LENGTH , RP_LENGTH }, 

            { ReservedParameters.RP_AREA_MAX , RP_AREA_MAX }, 
            { ReservedParameters.RP_WIDTH_MAX , RP_WIDTH_MAX }, 
            { ReservedParameters.RP_HEIGHT_MAX , RP_HEIGHT_MAX }, 
            { ReservedParameters.RP_DIAMETER_MAX , RP_DIAMETER_MAX }, 
            { ReservedParameters.RP_LENGTH_MAX , RP_LENGTH_MAX }, 

            { ReservedParameters.RP_AREA_MIN , RP_AREA_MIN }, 
            { ReservedParameters.RP_WIDTH_MIN , RP_WIDTH_MIN }, 
            { ReservedParameters.RP_HEIGHT_MIN , RP_HEIGHT_MIN }, 
            { ReservedParameters.RP_DIAMETER_MIN , RP_DIAMETER_MIN }, 
            { ReservedParameters.RP_LENGTH_MIN , RP_LENGTH_MIN }, 

            { ReservedParameters.RP_LENGTH_MIN_TOTAL , RP_LENGTH_MIN_TOTAL }, 
            { ReservedParameters.RP_AREA_MIN_TOTAL , RP_AREA_MIN_TOTAL }, 
            { ReservedParameters.RP_VOLUME_MIN_TOTAL , RP_VOLUME_MIN_TOTAL }, 
            { ReservedParameters.RP_LENGTH_MAX_TOTAL , RP_LENGTH_MAX_TOTAL }, 
            { ReservedParameters.RP_AREA_MAX_TOTAL , RP_AREA_MAX_TOTAL }, 
            { ReservedParameters.RP_VOLUME_MAX_TOTAL , RP_VOLUME_MAX_TOTAL }, 

            { ReservedParameters.RP_AREA_IN , RP_AREA_IN }, 
            { ReservedParameters.RP_AREA_OUT_MAIN , RP_AREA_OUT_MAIN }, 
            { ReservedParameters.RP_AREA_OUT_BRANCH , RP_AREA_OUT_BRANCH }, 
            { ReservedParameters.RP_AREA_SUGGESTION , RP_AREA_SUGGESTION }, 
            { ReservedParameters.RP_FLOW , RP_FLOW }, 
            { ReservedParameters.RP_SPEED , RP_SPEED }, 
            { ReservedParameters.RP_SPEED_IN , RP_SPEED_IN }, 
            { ReservedParameters.RP_SPEED_OUT_MAIN , RP_SPEED_OUT_MAIN }, 
            { ReservedParameters.RP_SPEED_OUT_BRANCH , RP_SPEED_OUT_BRANCH }, 
            { ReservedParameters.RP_PRESS_IN , RP_PRESS_IN }, 
            { ReservedParameters.RP_PRESS_IN_MAIN , RP_PRESS_IN_MAIN }, 
            { ReservedParameters.RP_PRESS_IN_BRANCH , RP_PRESS_IN_BRANCH }, 
            { ReservedParameters.RP_PRESS_OUT , RP_PRESS_OUT }, 
            { ReservedParameters.RP_PRESS_OUT_MAIN , RP_PRESS_OUT_MAIN }, 
            { ReservedParameters.RP_PRESS_OUT_BRANCH , RP_PRESS_OUT_BRANCH }, 
            { ReservedParameters.RP_RES_CORRECTION , RP_RES_CORRECTION }, 

            { ReservedParameters.RP_K_FOK , RP_K_FOK }, 
            { ReservedParameters.RP_K_FOK_ROH , RP_K_FOK_ROH }, 
            { ReservedParameters.RP_K_F_AXES , RP_K_F_AXES }, 
            { ReservedParameters.RP_K_DUK , RP_K_DUK }, 
            { ReservedParameters.RP_K_DUK_ROH , RP_K_DUK_ROH }, 
            { ReservedParameters.RP_K_D_AXES , RP_K_D_AXES }, 
            { ReservedParameters.RP_H_NET , RP_H_NET }, 
            { ReservedParameters.RP_H_GROSS , RP_H_GROSS }, 
            { ReservedParameters.RP_H_AXES , RP_H_AXES }, 
            { ReservedParameters.RP_L_PERIMETER , RP_L_PERIMETER }, 
            { ReservedParameters.RP_AREA_BGF , RP_AREA_BGF }, 
            { ReservedParameters.RP_AREA_NGF , RP_AREA_NGF }, 
            { ReservedParameters.RP_AREA_NF , RP_AREA_NF }, 
            { ReservedParameters.RP_AREA_AXES , RP_AREA_AXES }, 
            { ReservedParameters.RP_VOLUME_BRI , RP_VOLUME_BRI }, 
            { ReservedParameters.RP_VOLUME_NRI , RP_VOLUME_NRI }, 
            { ReservedParameters.RP_VOLUME_NRI_NF , RP_VOLUME_NRI_NF }, 
            { ReservedParameters.RP_VOLUME_AXES , RP_VOLUME_AXES }, 
            
            { ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT , RP_MATERIAL_COMPOSITE_D_OUT }, 
            { ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN , RP_MATERIAL_COMPOSITE_D_IN }, 

            { ReservedParameters.RP_ORIENTATION_HRZ , RP_ORIENTATION_HRZ }, 
            { ReservedParameters.RP_TABLE_POINTER , RP_TABLE_POINTER }, 
            { ReservedParameters.RP_AGGREGATION_OPERATION , RP_AGGREGATION_OPERATION }, 
            { ReservedParameters.RP_LABEL_SOURCE , RP_LABEL_SOURCE }, 

            { ReservedParameters.RP_PARAM_TO_GEOMETRY , RP_PARAM_TO_GEOMETRY }, 
        };

        /// <summary>
        /// Tries to find a reserved taxonomy entry in the provided taxonomy collection. Throws and exception if not found.
        /// </summary>
        /// <param name="taxonomies">The taxnomies</param>
        /// <param name="key">The reserved parameter to look for</param>
        /// <returns>The taxonomy entry with the provided key</returns>
        /// <exception cref="Exception">If the taxonomy entry with the key could not be found</exception>
        public static SimTaxonomyEntry GetReservedTaxonomyEntry(SimTaxonomyCollection taxonomies, String key)
        {
            var taxEntry = taxonomies.FindEntry(ReservedParameterKeys.RP_TAXONOMY_KEY, key, true);
            if (taxEntry == null)
                throw new Exception(String.Format("Could not find default reserved parameter taxonomy entry with key \"{0}\"", key));
            return taxEntry;
        }
    }

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
