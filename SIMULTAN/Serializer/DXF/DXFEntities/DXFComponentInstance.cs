using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper class for Geometric Relationships
    /// </summary>
    internal class DXFComponentInstance : DXFEntity
    {
        #region CLASS MEMBERS

        // name, type
        public long dxf_ID { get; private set; }
        public string dxf_Name { get; private set; }
        public SimInstanceType dxf_Type { get; private set; }
        public bool dxf_rel2geom_IsRealized { get; private set; }
        public SimInstanceConnectionState dxf_rel2geom_ConnectionState { get; private set; }

        // referenced geometry
        private int dxf_GeomFileId;
        private ulong dxf_GeomGeometryId;

        // instance information
        private List<double> dxf_inst_size;
        private Quaternion dxf_inst_rotation;
        private int dxf_nr_inst_size;
        private long dxf_inst_nwe_id;
        private Guid dxf_inst_nwe_location;
        private string dxf_inst_nwe_name;

        private List<Point3D> dxf_inst_path;
        private int dxf_nr_inst_path;
        private Point3D dxf_inst_path_current_vertex;

        private List<(SimId id, string name, double value)> dxf_instance_param_values;
        private int dxf_nr_instance_param_values;
        private string dxf_param_key_current;
        private SimId dxf_param_id_current;
        private double dxf_param_value_current;


        private SimInstanceSizeTransferDefinition dxf_size_transfer_def;
        private string dxf_sizedef_current_parameter_name;
        private SimId dxf_sizedef_current_parameter;
        private double dxf_sizedef_current_add;
        private SimInstanceSizeTransferSource dxf_sizedef_current_source;
        private int dfx_sizedef_current_index;


        // for being included in components
        internal SimComponentInstance dxf_parsed;

        #endregion

        public DXFComponentInstance()
        {
            this.dxf_ID = -1;
            this.dxf_Name = string.Empty;
            this.dxf_Type = SimInstanceType.None;
            this.dxf_rel2geom_IsRealized = false;
            this.dxf_rel2geom_ConnectionState = SimInstanceConnectionState.Ok;

            this.dxf_GeomFileId = -1;
            this.dxf_GeomGeometryId = ulong.MaxValue;

            this.dxf_inst_rotation = Quaternion.Identity;
            this.dxf_inst_size = new List<double>();
            this.dxf_nr_inst_size = 0;

            this.dxf_inst_nwe_id = -1L;
            this.dxf_inst_nwe_location = Guid.Empty;
            this.dxf_inst_nwe_name = "NW_Element";

            this.dxf_inst_path = new List<Point3D>();
            this.dxf_nr_inst_path = 0;
            this.dxf_inst_path_current_vertex = new Point3D(0, 0, 0);

            this.dxf_instance_param_values = new List<(SimId id, string name, double value)>();
            this.dxf_nr_instance_param_values = 0;
            this.dxf_param_key_current = string.Empty;
            this.dxf_param_value_current = 0;

            this.dxf_size_transfer_def = new SimInstanceSizeTransferDefinition();
            this.dxf_sizedef_current_add = 0.0;
            this.dxf_sizedef_current_parameter = SimId.Empty;
            this.dxf_sizedef_current_parameter_name = null;
            this.dxf_sizedef_current_source = SimInstanceSizeTransferSource.User;
            this.dfx_sizedef_current_index = 0;
        }

        #region OVERRIDES : Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.ENTITY_NAME:
                    this.ENT_Name = this.Decoder.FValue;
                    break;
                case (int)ComponentInstanceSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)ComponentInstanceSaveCode.STATE_TYPE:
                    string type_as_str = this.Decoder.FValue;
                    this.dxf_Type = DXFComponentInstance.StringToInstanceType(type_as_str);
                    break;
                case (int)ComponentInstanceSaveCode.STATE_ISREALIZED:
                    this.dxf_rel2geom_IsRealized = (this.Decoder.IntValue() == 1);
                    break;
                case (int)ComponentInstanceSaveCode.STATE_CONNECTION_STATE:
                    SimInstanceConnectionState tmp_constate = SimInstanceConnectionState.Ok;
                    bool success = Enum.TryParse<SimInstanceConnectionState>(this.Decoder.FValue, out tmp_constate);
                    if (success)
                        this.dxf_rel2geom_ConnectionState = tmp_constate;
                    break;
                case (int)ComponentInstanceSaveCode.GEOM_REF_FILE:
                    this.dxf_GeomFileId = this.Decoder.IntValue();
                    break;
                case (int)ComponentInstanceSaveCode.GEOM_REF_ID:
                    this.dxf_GeomGeometryId = this.Decoder.UlongValue();
                    break;
                // instance information
                case (int)ComponentInstanceSaveCode.INST_ROTATION:
                    this.dxf_inst_rotation = Quaternion.Parse(this.Decoder.FValue);
                    break;
                case (int)ComponentInstanceSaveCode.INST_SIZE:
                    this.dxf_nr_inst_size = this.Decoder.IntValue();
                    break;
                case (int)ComponentInstanceSaveCode.INST_NWE_ID:
                    this.dxf_inst_nwe_id = this.Decoder.LongValue();
                    break;
                case (int)ComponentInstanceSaveCode.INST_NWE_LOCATION:
                    this.dxf_inst_nwe_location = new Guid(this.Decoder.FValue);
                    break;
                case (int)ComponentInstanceSaveCode.INST_NWE_NAME:
                    this.dxf_inst_nwe_name = this.Decoder.FValue;
                    break;
                case (int)ComponentInstanceSaveCode.INST_PATH:
                    this.dxf_nr_inst_path = this.Decoder.IntValue();
                    break;
                case (int)ComponentInstanceSaveCode.INST_PARAMS:
                    this.dxf_nr_instance_param_values = this.Decoder.IntValue();
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_inst_size > this.dxf_inst_size.Count)
                    {
                        this.dxf_inst_size.Add(this.Decoder.DoubleValue());
                    }
                    else if (this.dxf_nr_inst_path > this.dxf_inst_path.Count)
                    {
                        this.dxf_inst_path_current_vertex.X = this.Decoder.DoubleValue();
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_inst_path > this.dxf_inst_path.Count)
                    {
                        this.dxf_inst_path_current_vertex.Y = this.Decoder.DoubleValue();
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    if (this.dxf_nr_inst_path > this.dxf_inst_path.Count)
                    {
                        this.dxf_inst_path_current_vertex.Z = this.Decoder.DoubleValue();
                        this.dxf_inst_path.Add(this.dxf_inst_path_current_vertex);
                        this.dxf_inst_path_current_vertex = new Point3D(0, 0, 0);
                    }
                    break;
                case (int)ComponentInstanceSaveCode.INST_SIZE_TS_SOURCE:
                    this.dxf_sizedef_current_source = SimInstanceSizeTransferDefinition.StringToSource(this.Decoder.FValue);
                    break;
                case (int)ComponentInstanceSaveCode.INST_SIZE_TS_PARAMETER:
                    if (Decoder.CurrentFileVersion < 8)
                        this.dxf_sizedef_current_parameter_name = this.Decoder.FValue;
                    else
                        this.dxf_sizedef_current_parameter = new SimId(Decoder.ProjectData.Owner, Decoder.LongValue());
                    break;
                case (int)ComponentInstanceSaveCode.INST_SIZE_TS_CORRECT:
                    this.dxf_sizedef_current_add = this.Decoder.DoubleValue();

                    if (dfx_sizedef_current_index < 6)
                    {
                        this.dxf_size_transfer_def[(SimInstanceSizeIndex)dfx_sizedef_current_index] =
                            new SimInstanceSizeTransferDefinitionItem(dxf_sizedef_current_source, dxf_sizedef_current_parameter,
                                                                    dxf_sizedef_current_parameter_name, dxf_sizedef_current_add);

                        //Reset
                        this.dxf_sizedef_current_add = 0.0;
                        this.dxf_sizedef_current_parameter = SimId.Empty;
                        this.dxf_sizedef_current_parameter_name = null;
                        this.dxf_sizedef_current_source = SimInstanceSizeTransferSource.User;
                        this.dfx_sizedef_current_index++;
                    }
                    break;
                case (int)ComponentInstanceSaveCode.INST_PARAM_KEY:
                    if (this.dxf_nr_instance_param_values > this.dxf_instance_param_values.Count)
                    {
                        if (Decoder.CurrentFileVersion <= 7)
                        {
                            this.dxf_param_key_current = this.Decoder.FValue;
                            this.dxf_param_id_current = SimId.Empty;
                        }
                        else
                        {
                            this.dxf_param_key_current = null;
                            this.dxf_param_id_current = new SimId(this.Decoder.ProjectData.Owner, this.Decoder.LongValue());
                        }
                    }
                    break;
                case (int)ComponentInstanceSaveCode.INST_PARAM_VAL:
                    if (this.dxf_nr_instance_param_values > this.dxf_instance_param_values.Count)
                    {
                        if (!string.IsNullOrEmpty(this.dxf_param_key_current) || this.dxf_param_id_current != SimId.Empty)
                        {
                            this.dxf_param_value_current = this.Decoder.DoubleValue();
                            this.dxf_instance_param_values.Add((this.dxf_param_id_current, this.dxf_param_key_current, this.dxf_param_value_current));
                            this.dxf_param_key_current = string.Empty;
                            this.dxf_param_value_current = 0;
                        }
                    }
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            // create the geometric relationship and save it internally
            SimInstanceState state = new SimInstanceState(this.dxf_rel2geom_IsRealized, this.dxf_rel2geom_ConnectionState);

            if (Decoder.CurrentFileVersion < 7)
            {
                //Remove first entry from path which contained IDs
                if (dxf_inst_path.Count > 0)
                    dxf_inst_path.RemoveAt(0);
            }

            if (Decoder.CurrentFileVersion < 7) //Id translation
            {
                if (DXFDecoder.InstanceCount > 1073741824)
                    throw new Exception("Too many Instances");

                var newId = DXFDecoder.InstanceIdOffset + DXFDecoder.InstanceCount;
                DXFDecoder.InstanceCount++;

                if (!DXFDecoder.IdTranslation.ContainsKey((typeof(SimComponentInstance), this.dxf_ID)))
                {
                    DXFDecoder.IdTranslation.Add((typeof(SimComponentInstance), this.dxf_ID), newId);
                }
                else
                {
                    Decoder.Log(string.Format("Multiple Instances with Id {0} found.", this.dxf_ID));
                }

                this.dxf_ID = newId;
            }

            //Bugfix for wrong global Ids in network elements
            //In some version, it seems that global Ids were set to empty although they should have been the id of the current project
            if (this.dxf_inst_nwe_id != -1 && this.dxf_inst_nwe_location == Guid.Empty)
                this.dxf_inst_nwe_location = this.Decoder.ProjectData.Owner.GlobalID;

            // NEW
            var size = SimInstanceSize.Default;
            if (dxf_inst_size.Count == 6)
                size = SimInstanceSize.FromList(dxf_inst_size);

            (int, ulong, List<ulong>)? geoRef = null;
            if (dxf_GeomFileId != -1 && dxf_GeomGeometryId != ulong.MaxValue)
                geoRef = (dxf_GeomFileId, dxf_GeomGeometryId, null);

            this.dxf_parsed = new SimComponentInstance(dxf_ID, dxf_Name, this.dxf_Type, state, geoRef,
                                                        dxf_inst_rotation, size, dxf_size_transfer_def, new SimObjectId(dxf_inst_nwe_location, dxf_inst_nwe_id),
                                                        dxf_inst_path, dxf_instance_param_values);
        }

        #endregion


        #region Utils

        public static string InstanceTypeToString(SimInstanceType _rel)
        {
            switch (_rel)
            {
                case SimInstanceType.Entity3D:
                    return "DESCRIBES";
                case SimInstanceType.GeometricVolume:
                    return "DESCRIBES_3D";
                case SimInstanceType.GeometricSurface:
                    return "DESCRIBES_2DorLESS";
                case SimInstanceType.Attributes2D:
                    return "ALIGNED_WITH";
                case SimInstanceType.NetworkNode:
                    return "CONTAINED_IN";
                case SimInstanceType.NetworkEdge:
                    return "CONNECTS";
                case SimInstanceType.Group:
                    return "GROUPS";
                case SimInstanceType.BuiltStructure:
                    return "PARAMETERIZES";
                default:
                    return "NONE";
            }
        }

        public static SimInstanceType StringToInstanceType(string _input)
        {
            if (string.IsNullOrEmpty(_input)) return SimInstanceType.None;

            switch (_input)
            {
                case "DESCRIBES":
                    return SimInstanceType.Entity3D;
                case "DESCRIBES_3D":
                    return SimInstanceType.GeometricVolume;
                case "DESCRIBES_2DorLESS":
                    return SimInstanceType.GeometricSurface;
                case "ALIGNED_WITH":
                    return SimInstanceType.Attributes2D;
                case "CONTAINED_IN":
                    return SimInstanceType.NetworkNode;
                case "CONNECTS":
                    return SimInstanceType.NetworkEdge;
                case "GROUPS":
                    return SimInstanceType.Group;
                case "PARAMETERIZES":
                    return SimInstanceType.BuiltStructure;
                default:
                    return SimInstanceType.None;
            }
        }

        #endregion
    }
}
