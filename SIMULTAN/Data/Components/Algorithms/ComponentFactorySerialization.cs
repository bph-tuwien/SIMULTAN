using SIMULTAN.Data.Assets;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Provides methods for serializing and deserializing component collections
    /// </summary>
    public static class ComponentFactorySerialization
    {
        #region Serialization to a SINGLE FILE

        /// <summary>
        /// Serializes the entire component factory record regardless of writing access to a single string builder, together with networks and assets
        /// The chat is included.
        /// </summary>
        /// <param name="networkFactory">the calling component factory</param>
        /// <param name="components">The components to export</param>
        /// <param name="assetManager">The asset manager to export</param>
        /// <param name="userComponentLists">The user component lists to export</param>
        /// <param name="finalize">true = set the EOF marker, false = do not set the EOF marker</param>
        /// <returns>A string builder containing the result of the serialization</returns>
        public static StringBuilder ExportRecord(SimNetworkFactory networkFactory, SimComponentCollection components, AssetManager assetManager,
            SimUserComponentListCollection userComponentLists,
            bool finalize)
        {
            StringBuilder sb = new StringBuilder();

            // FILE VERSION
            SaveFileVersion(sb, networkFactory.ProjectData.IdGenerator);

            // ASSETS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ASSET_SECTION);

            assetManager.AddToExport(ref sb);

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // COMPONENTS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            if (components.Count > 0)
            {
                foreach (SimComponent record in components)
                {
                    record.AddToExport(ref sb);
                }
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // USER LISTS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            foreach (var userList in userComponentLists)
            {
                userList.AddToExport(sb);
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // NETWORKS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.NETWORK_SECTION);

            foreach (SimFlowNetwork nw in networkFactory.NetworkRecord)
            {
                nw.AddToExport(ref sb);
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // FINALIZE FILE
            if (finalize)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.EOF);
            }

            return sb;
        }

        private static void SaveFileVersion(StringBuilder _sb, SimIdGenerator generator)
        {
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            _sb.AppendLine(ParamStructTypes.SECTION_START);
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            _sb.AppendLine(ParamStructTypes.VERSION_SECTION);

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.FILE_VERSION);                            // FILE_VERSION

            _sb.AppendLine(((int)ParamStructCommonSaveCode.COORDS_X).ToString());       // 10
            _sb.AppendLine(DXFDecoder.CurrentFileFormatVersion.ToString());

            _sb.AppendLine(((int)ComponentFileMetaInfoSaveCode.MAX_CALCULATION_ID).ToString());       // 10
            _sb.AppendLine(generator.MaxId.ToString());

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            _sb.AppendLine(ParamStructTypes.SECTION_END);
        }

        #endregion

        #region Serialization to a single PUBLIC FILE

        /// <summary>
        /// Serializes the PUBLIC component factory content. This does not include components referenced by
        /// the public ones. Networks and important parameters are not serialized either.
        /// Resources and assets are serialized and the paths leading to them are extracted for unpacking
        /// during project loading.
        /// </summary>
        /// <param name="networks">the calling component factory</param>
        /// <param name="components">The components in which public components are searched for export</param>
        /// <param name="assetManager">The asset manager for exporting</param>
        /// <param name="_finalize">true = set the EOF marker, false = do not set the EOF marker</param>
        /// <returns>the filled string builder and a list of paths</returns>
        public static (StringBuilder content, List<string> publicPaths) ExportPublic(SimNetworkFactory networks,
            SimComponentCollection components, AssetManager assetManager,
            bool _finalize)
        {
            StringBuilder sb = new StringBuilder();

            // FILE VERSION
            SaveFileVersion(sb, networks.ProjectData.IdGenerator);

            // ASSETS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ASSET_SECTION);

            var public_paths = assetManager.AddToPublicExport(ref sb);

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // COMPONENTS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            foreach (SimComponent record in components)
            {
                if (record.Visibility == SimComponentVisibility.AlwaysVisible)
                {
                    record.AddToExport(ref sb);
                }
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // FINALIZE FILE
            if (_finalize)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.EOF);
            }

            return (sb, public_paths);
        }

        #endregion

        #region Serialization of SELECTED to a SINGLE FILE

        /// <summary>
        /// Serializes the given components *not* including the components referenced by them.
        /// All input components have to belong to the calling factory.
        /// </summary>
        /// <param name="_factory">the calling factory</param>
        /// <param name="_cs_to_export">the components to export</param>
        /// <param name="_nws_to_export">the networks to export</param>
        /// <param name="_finalize">if true, finalize the serialization (EOF)</param>
        /// <returns>the serialization</returns>
        public static StringBuilder ExportSome(this SimComponentCollection _factory, IEnumerable<SimComponent> _cs_to_export,
            IEnumerable<SimFlowNetwork> _nws_to_export, bool _finalize)
        {
            if (_cs_to_export == null)
                throw new ArgumentNullException(nameof(_cs_to_export));
            if (_nws_to_export == null)
                throw new ArgumentNullException(nameof(_nws_to_export));

            StringBuilder sb = new StringBuilder();

            // FILE VERSION
            SaveFileVersion(sb, _factory.ProjectData.IdGenerator);

            // COMPONENTS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            // export
            foreach (SimComponent ce in _cs_to_export)
            {
                if (ce.Factory == _factory)
                    ce.AddToExport(ref sb);
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // NETWORKS
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.NETWORK_SECTION);

            foreach (SimFlowNetwork nw in _nws_to_export)
            {
                nw.AddToExport(ref sb);
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            // FINALIZE FILE
            if (_finalize)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.EOF);
            }

            return sb;
        }

        #endregion
    }
}
