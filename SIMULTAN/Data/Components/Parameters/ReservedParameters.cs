using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;

namespace SIMULTAN.Data.Components
{

    public static class ReservedParameterKeys
    {
        public const string RP_TAXONOMY_KEY = "resparam";

        [Obsolete("Still used by the old networks")]
        public const string RP_COUNT = "nrtotal";

        [Obsolete("Still used by the old networks")]
        public const string RP_LENGTH_MIN_TOTAL = "lmintotal";
        [Obsolete("Still used by the old networks")]
        public const string RP_AREA_MIN_TOTAL = "amintotal";
        [Obsolete("Still used by the old networks")]
        public const string RP_VOLUME_MIN_TOTAL = "vmintotal";
        [Obsolete("Still used by the old networks")]
        public const string RP_LENGTH_MAX_TOTAL = "lmaxtotal";
        [Obsolete("Still used by the old networks")]
        public const string RP_AREA_MAX_TOTAL = "amaxtotal";
        [Obsolete("Still used by the old networks")]
        public const string RP_VOLUME_MAX_TOTAL = "vmaxtotal";

        public const string RP_MATERIAL_COMPOSITE_D_OUT = "dout";
        public const string RP_MATERIAL_COMPOSITE_D_IN = "din";

        public const string RP_ORIENTATION_HRZ = "ohrz"; // U+27BD
        public const string RP_TABLE_POINTER = "tp"; // U+274F
        public const string RP_AGGREGATION_OPERATION = "ao"; // U+2756
        public const string RP_LABEL_SOURCE = "ls"; // U+275D

        public const string RP_PARAM_TO_GEOMETRY = "ptg"; //U+27A4


        public const string SIMENUMPARAM_DEFAULT = "simenumparam_default";


        public const string SIMNW_STATIC_PORT_POSITION_X = "simnw_postn_x";
        public const string SIMNW_STATIC_PORT_POSITION_Y = "simnw_postn_y";
        public const string SIMNW_STATIC_PORT_POSITION_Z = "simnw_postn_z";


        public static Dictionary<String, String> NameToKeyLookup = new Dictionary<string, string> {

            { ReservedParameters.RP_COUNT , RP_COUNT },

            { ReservedParameters.RP_LENGTH_MIN_TOTAL , RP_LENGTH_MIN_TOTAL },
            { ReservedParameters.RP_AREA_MIN_TOTAL , RP_AREA_MIN_TOTAL },
            { ReservedParameters.RP_VOLUME_MIN_TOTAL , RP_VOLUME_MIN_TOTAL },
            { ReservedParameters.RP_LENGTH_MAX_TOTAL , RP_LENGTH_MAX_TOTAL },
            { ReservedParameters.RP_AREA_MAX_TOTAL , RP_AREA_MAX_TOTAL },
            { ReservedParameters.RP_VOLUME_MAX_TOTAL , RP_VOLUME_MAX_TOTAL },

            { ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT , RP_MATERIAL_COMPOSITE_D_OUT },
            { ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN , RP_MATERIAL_COMPOSITE_D_IN },

            { ReservedParameters.RP_ORIENTATION_HRZ , RP_ORIENTATION_HRZ },
            { ReservedParameters.RP_TABLE_POINTER , RP_TABLE_POINTER },
            { ReservedParameters.RP_AGGREGATION_OPERATION , RP_AGGREGATION_OPERATION },
            { ReservedParameters.RP_LABEL_SOURCE , RP_LABEL_SOURCE },

            { ReservedParameters.RP_PARAM_TO_GEOMETRY , RP_PARAM_TO_GEOMETRY },

            { ReservedParameters.SIMENUMPARAM_DEFAULT , SIMENUMPARAM_DEFAULT },

            { ReservedParameters.SIMNW_STATIC_PORT_POSITION_X , SIMNW_STATIC_PORT_POSITION_X },
            { ReservedParameters.SIMNW_STATIC_PORT_POSITION_Y , SIMNW_STATIC_PORT_POSITION_Y },
            { ReservedParameters.SIMNW_STATIC_PORT_POSITION_Z , SIMNW_STATIC_PORT_POSITION_Z },
        };

        /// <summary>
        /// Tries to find a reserved taxonomy entry in the provided taxonomy collection. Throws and exception if not found.
        /// </summary>
        /// <param name="taxonomies">The taxonomies</param>
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

        /// <summary>
        /// Extension function to the reserved parameters directly from the taxonomy collection.
        /// Uses <see cref="ReservedParameterKeys.GetReservedTaxonomyEntry(SimTaxonomyCollection, string)"/> to find the taxonomy entry.
        /// </summary>
        /// <param name="taxonomies">The taxonomies</param>
        /// <param name="key">The taxonomy entry key</param>
        /// <returns>The taxonomy entry</returns>
        /// <exception cref="Exception">If the taxonomy entry with the key could not be found</exception>
        public static SimTaxonomyEntry GetReservedParameter(this SimTaxonomyCollection taxonomies, string key)
        {
            return GetReservedTaxonomyEntry(taxonomies, key);
        }
    }

    /// <summary>
    /// Contains informations about reserved parameter names.
    /// These names are used for special purposes by the data model or by the Geometry connection
    /// </summary>
	public static class ReservedParameters
    {
        public const string RP_COUNT = "NRᴛᴏᴛᴀʟ";

        public const string RP_LENGTH_MIN_TOTAL = "LᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_AREA_MIN_TOTAL = "AᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_VOLUME_MIN_TOTAL = "VᴍɪNᴛᴏᴛᴀʟ";
        public const string RP_LENGTH_MAX_TOTAL = "LᴍᴀXᴛᴏᴛᴀʟ";
        public const string RP_AREA_MAX_TOTAL = "AᴍᴀXᴛᴏᴛᴀʟ";
        public const string RP_VOLUME_MAX_TOTAL = "VᴍᴀXᴛᴏᴛᴀʟ";

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

        public const string SIMENUMPARAM_DEFAULT = "Default";

        public const string SIMNW_STATIC_PORT_POSITION_X = "X";
        public const string SIMNW_STATIC_PORT_POSITION_Y = "Y";
        public const string SIMNW_STATIC_PORT_POSITION_Z = "Z";
    }
}
