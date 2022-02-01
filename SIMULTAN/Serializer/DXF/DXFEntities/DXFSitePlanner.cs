using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// DXF Entity for <see cref="SitePlannerProject"/>
    /// </summary>
    internal class DXFSitePlanner : DXFEntity
    {
        struct BuildingEntity
        {
            public ulong id;
            public string geometryModel;
            public string color;
        }

        struct ValueParametersAssociationEntity
        {
            public string name;
            public Guid valueTableLocation;
            public long valueTableID;
            public string indexUsage;
            public string colorMapType;
            public string colorMapParams;
            public string prefilterType;
            public string prefilterParams;
        }

        private List<string> dxf_GeoMaps;
        private List<string> dxf_GeoMapsElevationProviders;
        private List<int> dxf_GeoMapsGridCellSizes;
        private (int idx, BuildingEntity be) dxf_Building;
        private BuildingEntity[] dxf_Buildings;
        private (int idx, ValueParametersAssociationEntity v) dxf_ValueParameterAssociation;
        private ValueParametersAssociationEntity[] dxf_ValueParameterAssociations;
        private int dxf_ValueParameterActiveAssociation;

        /// <inheritdoc />
        public DXFSitePlanner()
        {
            this.dxf_Building = (-1, new BuildingEntity
            {
                id = ulong.MaxValue,
                geometryModel = string.Empty,
                color = string.Empty
            });

            this.dxf_ValueParameterAssociation = (-1, new ValueParametersAssociationEntity
            {
                name = "",
                valueTableLocation = Guid.Empty,
                valueTableID = -1,
                indexUsage = ComponentIndexUsage.Row.ToString(),
                colorMapType = "",
                colorMapParams = "",
                prefilterType = "",
                prefilterParams = ""
            });
            dxf_ValueParameterActiveAssociation = -1;
        }

        /// <inheritdoc />
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)SitePlannerSaveCode.GEOMAPS:
                    int geomap_count = this.Decoder.IntValue();
                    dxf_GeoMaps = new List<string>(geomap_count);
                    dxf_GeoMapsElevationProviders = new List<string>(geomap_count);
                    dxf_GeoMapsGridCellSizes = new List<int>(geomap_count);
                    break;
                case (int)SitePlannerSaveCode.GEOMAP_PATH:
                    this.dxf_GeoMaps.Add(this.Decoder.FValue);
                    break;
                case (int)SitePlannerSaveCode.ELEVATION_PROVIDER_TYPE:
                    this.dxf_GeoMapsElevationProviders.Add(this.Decoder.FValue);
                    break;
                case (int)SitePlannerSaveCode.GRID_CELL_SIZE:
                    this.dxf_GeoMapsGridCellSizes.Add(this.Decoder.IntValue());
                    break;
                case (int)SitePlannerSaveCode.BUILDINGS:
                    dxf_Buildings = new BuildingEntity[this.Decoder.IntValue()];
                    break;
                case (int)SitePlannerSaveCode.BUILDING_INDEX:
                    this.dxf_Building.idx = this.Decoder.IntValue();
                    SaveGeometryModel();
                    break;
                case (int)SitePlannerSaveCode.BUILDING_ID:
                    this.dxf_Building.be.id = this.Decoder.UlongValue();
                    SaveGeometryModel();
                    break;
                case (int)SitePlannerSaveCode.BUILDING_GEOMETRYMODEL_PATH:
                    this.dxf_Building.be.geometryModel = this.Decoder.FValue;
                    SaveGeometryModel();
                    break;
                case (int)SitePlannerSaveCode.BUILDING_CUSTOM_COLOR:
                    this.dxf_Building.be.color = this.Decoder.FValue;
                    SaveGeometryModel();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATIONS:
                    var count = this.Decoder.IntValue();
                    if (count > 0)
                        dxf_ValueParameterAssociations = new ValueParametersAssociationEntity[count];
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_INDEX:
                    this.dxf_ValueParameterAssociation.idx = this.Decoder.IntValue();
                    SaveValueMappingAssociation();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_ASSOCIATION_NAME:
                    this.dxf_ValueParameterAssociation.v.name = this.Decoder.FValue;
                    SaveValueMappingAssociation();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_LOCATION:
                    this.dxf_ValueParameterAssociation.v.valueTableLocation = new Guid(this.Decoder.FValue);
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_VALUE_TABLE_KEY:
                    this.dxf_ValueParameterAssociation.v.valueTableID = this.Decoder.LongValue();
                    SaveValueMappingAssociation();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_INDEX_USAGE:
                    this.dxf_ValueParameterAssociation.v.indexUsage = this.Decoder.FValue;
                    SaveValueMappingAssociation();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_TYPE:
                    this.dxf_ValueParameterAssociation.v.colorMapType = this.Decoder.FValue;
                    SaveValueMappingAssociation();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_COLOR_MAP_PARAMS:
                    this.dxf_ValueParameterAssociation.v.colorMapParams = this.Decoder.FValue;
                    SaveValueMappingAssociation();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_TYPE:
                    this.dxf_ValueParameterAssociation.v.prefilterType = this.Decoder.FValue;
                    SaveValueMappingAssociation();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_PREFILTER_PARAMS:
                    this.dxf_ValueParameterAssociation.v.prefilterParams = this.Decoder.FValue;
                    SaveValueMappingAssociation();
                    break;
                case (int)SitePlannerSaveCode.VALUE_MAPPING_ACTIVE_ASSOCIATION_INDEX:
                    this.dxf_ValueParameterActiveAssociation = this.Decoder.IntValue();
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        private void SaveGeometryModel()
        {
            if (this.dxf_Building.idx == -1 || this.dxf_Building.be.geometryModel == string.Empty
                || this.dxf_Building.be.color == string.Empty || this.dxf_Building.be.id == ulong.MaxValue)
                return;

            if (dxf_Building.idx < dxf_Buildings.Count())
            {
                this.dxf_Buildings[dxf_Building.idx] = dxf_Building.be;
                this.dxf_Building = (-1, new BuildingEntity
                {
                    id = ulong.MaxValue,
                    geometryModel = string.Empty,
                    color = string.Empty
                });
            }
        }

        private void SaveValueMappingAssociation()
        {
            if (dxf_ValueParameterAssociation.idx == -1 || dxf_ValueParameterAssociation.v.name == "" ||
                dxf_ValueParameterAssociation.v.valueTableID == -1 || dxf_ValueParameterAssociation.v.colorMapType == "" ||
                dxf_ValueParameterAssociation.v.colorMapParams == "" || dxf_ValueParameterAssociation.v.prefilterType == "" ||
                dxf_ValueParameterAssociation.v.prefilterParams == "")
                return;

            if (dxf_ValueParameterAssociation.idx < dxf_ValueParameterAssociations.Count())
            {
                dxf_ValueParameterAssociations[dxf_ValueParameterAssociation.idx] = dxf_ValueParameterAssociation.v;
                dxf_ValueParameterAssociation = (-1, new ValueParametersAssociationEntity
                {
                    name = "",
                    valueTableLocation = Guid.Empty,
                    valueTableID = -1,
                    indexUsage = ComponentIndexUsage.Row.ToString(),
                    colorMapType = "",
                    colorMapParams = "",
                    prefilterType = "",
                    prefilterParams = ""
                });
            }
        }

        /// <inheritdoc />
        internal override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder is DXFDecoderSitePlanner)
            {
                var decoder = (DXFDecoderSitePlanner)this.Decoder;

                for (int i = 0; i < dxf_GeoMaps.Count; i++)
                {
                    var sp_map = new SitePlannerMap(ResourceReference.FromDXF(dxf_GeoMaps[i], decoder.AssetManager));

                    sp_map.ElevationProviderTypeName = dxf_GeoMaps.Count == dxf_GeoMapsElevationProviders.Count ? dxf_GeoMapsElevationProviders[i] : "";
                    sp_map.GridCellSize = dxf_GeoMaps.Count == dxf_GeoMapsGridCellSizes.Count ? dxf_GeoMapsGridCellSizes[i] : 100;
                    sp_map.ElevationProvider = null;

                    decoder.Project.Maps.Add(sp_map);
                }

                foreach (var gm in dxf_Buildings)
                {
                    SitePlannerBuilding spb = new SitePlannerBuilding(gm.id, ResourceReference.FromDXF(gm.geometryModel, decoder.AssetManager));
                    string[] colorComponents = gm.color.Split(' ');
                    spb.CustomColor = Color.FromRgb(byte.Parse(colorComponents[0]), byte.Parse(colorComponents[1]), byte.Parse(colorComponents[2]));
                    decoder.Project.Buildings.Add(spb);
                }

                ValueMap valueMap = new ValueMap();
                if (dxf_ValueParameterAssociations != null)
                {
                    foreach (var vm in dxf_ValueParameterAssociations)
                    {
                        var mvId = vm.valueTableID;

                        if (Decoder.CurrentFileVersion < 6)
                        {
                            mvId = DXFDecoder.IdTranslation[(typeof(SimMultiValue), vm.valueTableID)];
                        }

                        SimMultiValueBigTable table = decoder.MultiValueCollection.GetByID(vm.valueTableLocation, mvId) as SimMultiValueBigTable;
                        ValueMappingParameters vmparameters = new ValueMappingParameters(table);
                        vmparameters.ComponentIndexUsage = (ComponentIndexUsage)Enum.Parse(typeof(ComponentIndexUsage), vm.indexUsage);

                        var colorMap = vmparameters.RegisteredColorMaps.FirstOrDefault(x => x.GetType().ToString() == vm.colorMapType);
                        if (colorMap != null)
                        {
                            colorMap.Parameters.Deserialize(vm.colorMapParams);
                            vmparameters.ValueToColorMap = colorMap;
                        }

                        var prefilter = vmparameters.RegisteredValuePrefilters.FirstOrDefault(x => x.GetType().ToString() == vm.prefilterType);
                        if (prefilter != null)
                        {
                            prefilter.Parameters.Deserialize(vm.prefilterParams);
                            vmparameters.ValuePreFilter = prefilter;
                        }

                        valueMap.ParametersAssociations.Add(new ValueMappingAssociation(vm.name, vmparameters));
                    }
                    valueMap.ActiveParametersAssociationIndex = dxf_ValueParameterActiveAssociation;
                }

                valueMap.EstablishValueFieldConnection(decoder.MultiValueCollection);
                decoder.Project.ValueMap = valueMap;
            }
        }
    }
}
