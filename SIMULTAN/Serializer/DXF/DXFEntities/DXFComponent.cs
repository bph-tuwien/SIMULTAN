using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    internal class DXFComponent : DXFEntityContainer
    {
        #region CLASS MEMBERS

        // general
        public long dxf_ID;
        public string dxf_Name;
        public string dxf_Description;
        public bool dxf_IsAutomaticallyGenerated;

        // management
        public SimAccessProfile dxf_AccessLocal;
        private int dxf_nr_FitsInSlots;
        private int dxf_nr_FitsInSlots_read;
        public SimSlot dxf_CurrentSlot;

        // contained components
        public List<(SimSlot slot, SimComponent component)> dxf_ContainedComponents;
        private int dxf_nr_ContainedComponents;
        private int dxf_nr_ContainedComponent_Slots;
        private int dxf_nr_ContainedComponent_Slots_read;

        // referenced components
        public List<(string slot, Guid globalId, long localId)> dxf_ReferencedComponents;
        private int dxf_nr_ReferencedComponents;
        private int dxf_nr_ReferencedComponents_read;
        private (bool, bool, bool) dxf_RefComp_read;
        private string dxf_RefComp_slot;
        private long dxf_RefComp_id;
        private Guid dxf_RefComp_location_Guid;

        // contained parameters
        public List<SimParameter> dxf_ContainedParameters;
        private int dxf_nr_ContainedParameters;

        // contained calculations
        internal List<CalculationInitializationData> dxf_ContainedCalculations_Ref;
        private int dxf_nr_ContainedCalculations;


        // Used to store pre v6 per-instance relation types. May only be used when loading legacy projects
        protected List<SimInstanceType> dxf_R2GInstancesTypes;
        // relationships to geometry
        protected List<SimComponentInstance> dxf_R2GInstances;
        private int dxf_nr_R2GInstances;

        // mappings to other components (for shared usage of calculations)
        protected List<CalculatorMapping> dxf_Mapping2Comps;
        private int dxf_nr_Mapping2Comps;

        // mapping to EXCEL tools
        protected Dictionary<string, ExcelComponentMapping> dxf_MappingsPerExcelTool;
        private int dxf_nr_MappingsPerExcelTool;

        // conversation
        protected List<SimChatItem> dxf_ChatItems;
        private int dxf_nr_ChatItems;

        // symbol
        public long dxf_SymbolId;

        // project-relevant
        protected SimComponentVisibility dxf_Visibility;
        protected System.Windows.Media.Color dxf_ComponentColor;
        protected int dxf_nr_color_components;

        // view-relevant
        protected SimComponentContentSorting dxf_SortingType;

        // parsed encapsulated class
        internal SimComponent dxf_parsed;

        protected SimInstanceType dxf_instanceType;

        #endregion

        #region .CTOR
        public DXFComponent()
            : base()
        {
            this.dxf_IsAutomaticallyGenerated = false;

            this.dxf_nr_FitsInSlots = 0;
            this.dxf_nr_FitsInSlots_read = 0;

            this.dxf_ContainedComponents = new List<(SimSlot slot, SimComponent component)>();
            this.dxf_nr_ContainedComponents = 0;
            this.dxf_nr_ContainedComponent_Slots = 0;
            this.dxf_nr_ContainedComponent_Slots_read = 0;

            this.dxf_ReferencedComponents = new List<(string slot, Guid globalId, long localId)>();
            this.dxf_nr_ReferencedComponents = 0;
            this.dxf_nr_ReferencedComponents_read = 0;
            this.dxf_RefComp_read = (false, false, false);

            this.dxf_ContainedParameters = new List<SimParameter>();
            this.dxf_nr_ContainedParameters = 0;

            this.dxf_R2GInstancesTypes = new List<SimInstanceType>();
            this.dxf_ContainedCalculations_Ref = new List<CalculationInitializationData>();
            this.dxf_nr_ContainedCalculations = 0;

            this.dxf_R2GInstances = new List<SimComponentInstance>();
            this.dxf_nr_R2GInstances = 0;

            this.dxf_Mapping2Comps = new List<CalculatorMapping>();
            this.dxf_nr_Mapping2Comps = 0;

            this.dxf_MappingsPerExcelTool = new Dictionary<string, ExcelComponentMapping>();
            this.dxf_nr_MappingsPerExcelTool = 0;

            this.dxf_ChatItems = new List<SimChatItem>();
            this.dxf_nr_ChatItems = 0;

            this.dxf_SymbolId = -1;

            this.dxf_Visibility = SimComponentVisibility.Hidden;
            this.dxf_ComponentColor = System.Windows.Media.Color.FromArgb(255, 0, 0, 0);
            this.dxf_nr_color_components = 0;
            this.dxf_SortingType = SimComponentContentSorting.ByName;

            this.dxf_instanceType = SimInstanceType.None;
        }
        #endregion

        #region OVERRIDES : Processing

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ComponentSaveCode.NAME:
                    this.dxf_Name = this.Decoder.FValue;
                    break;
                case (int)ComponentSaveCode.DESCRIPTION:
                    string tmp = this.Decoder.FValue;
                    string[] comps = tmp.Split(new string[] { ParamStructTypes.DELIMITER_WITHIN_ENTRY }, StringSplitOptions.RemoveEmptyEntries);
                    if (comps.Length == 0)
                        this.dxf_Description = this.Decoder.FValue;
                    else if (comps.Length == 1)
                        this.dxf_Description = comps[0];
                    else
                        this.dxf_Description = comps.Aggregate((x, y) => x + Environment.NewLine + y);
                    break;
                case (int)ComponentSaveCode.GENERATED_AUTOMATICALLY:
                    this.dxf_IsAutomaticallyGenerated = (this.Decoder.IntValue() == 1);
                    break;
                case (int)ComponentSaveCode.FUNCTION_SLOTS_ALL:
                    this.dxf_nr_FitsInSlots = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.FUNCTION_SLOT_CURRENT:
                    this.dxf_CurrentSlot = SimSlot.FromSerializerString(this.Decoder.FValue);
                    break;
                case (int)ComponentSaveCode.CONTAINED_COMPONENTS:
                    this.dxf_nr_ContainedComponents = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.CONTAINED_COMPONENT_SLOTS:
                    this.dxf_nr_ContainedComponent_Slots = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.REFERENCED_COMPONENTS:
                    this.dxf_nr_ReferencedComponents = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.CONTAINED_PARAMETERS:
                    this.dxf_nr_ContainedParameters = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.CONTAINED_CALCULATIONS:
                    this.dxf_nr_ContainedCalculations = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.RELATIONSHIPS_TO_GEOMETRY:
                    this.dxf_nr_R2GInstances = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.MAPPINGS_TO_COMPONENTS:
                    this.dxf_nr_Mapping2Comps = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.MAPPINGS_TO_EXCEL_TOOLS:
                    this.dxf_nr_MappingsPerExcelTool = this.Decoder.IntValue();
                    break;
                case (int)ChatItemSaveCode.CONVERSATION:
                    this.dxf_nr_ChatItems = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.SYMBOL_ID:
                    this.dxf_SymbolId = this.Decoder.LongValue();
                    break;
                case (int)ComponentSaveCode.VISIBILTY:
                    bool success = Enum.TryParse(this.Decoder.FValue, out SimComponentVisibility vis);
                    if (success)
                        this.dxf_Visibility = vis;
                    break;
                case (int)ComponentSaveCode.COLOR:
                    this.dxf_nr_color_components = this.Decoder.IntValue();
                    break;
                case (int)ComponentSaveCode.SORTING_TYPE:
                    bool success1 = Enum.TryParse(this.Decoder.FValue, out SimComponentContentSorting st);
                    if (success1)
                        this.dxf_SortingType = st;
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_FitsInSlots > this.dxf_nr_FitsInSlots_read)
                    {
                        this.dxf_nr_FitsInSlots_read++;
                    }
                    else if (this.dxf_nr_ContainedComponent_Slots > this.dxf_nr_ContainedComponent_Slots_read)
                    {
                        this.dxf_ContainedComponents.Add((SimSlot.FromSerializerString(this.Decoder.FValue), null));
                        this.dxf_nr_ContainedComponent_Slots_read++;
                    }
                    else if (this.dxf_nr_ReferencedComponents > this.dxf_nr_ReferencedComponents_read)
                    {
                        if (!this.dxf_RefComp_read.Item1)
                        {
                            this.dxf_RefComp_slot = this.Decoder.FValue;
                            this.dxf_RefComp_read.Item1 = true;
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_ReferencedComponents > this.dxf_nr_ReferencedComponents_read)
                    {
                        if (!this.dxf_RefComp_read.Item2)
                        {
                            this.dxf_RefComp_id = this.Decoder.LongValue();
                            this.dxf_RefComp_read.Item2 = true;
                        }
                        if (this.Decoder.CurrentFileVersion <= 1 && this.dxf_RefComp_read.Item1 && this.dxf_RefComp_read.Item2)
                        {
                            this.dxf_ReferencedComponents.Add((this.dxf_RefComp_slot, Guid.Empty, this.dxf_RefComp_id));
                            this.dxf_nr_ReferencedComponents_read++;
                            this.dxf_RefComp_read = (false, false, false);
                        }
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_ReferencedComponents > this.dxf_nr_ReferencedComponents_read)
                    {
                        if (!this.dxf_RefComp_read.Item3)
                        {
                            this.dxf_RefComp_location_Guid = new Guid(this.Decoder.FValue);
                            this.dxf_RefComp_read.Item3 = true;
                        }
                        if (this.Decoder.CurrentFileVersion > 1 && this.dxf_RefComp_read.Item1 && this.dxf_RefComp_read.Item2 && this.dxf_RefComp_read.Item3)
                        {
                            this.dxf_ReferencedComponents.Add((this.dxf_RefComp_slot, this.dxf_RefComp_location_Guid, this.dxf_RefComp_id));
                            this.dxf_nr_ReferencedComponents_read++;
                            this.dxf_RefComp_read = (false, false, false);
                        }
                    }
                    break;
                case (int)ComponentSaveCode.INSTANCE_TYPE:
                    {
                        this.dxf_instanceType = DXFComponentInstance.StringToInstanceType(this.Decoder.FValue);
                    }
                    break;
                default:
                    // DXFEntityContainer: ENTITY_NAME
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    this.dxf_ID = this.ENT_ID;
                    break;
            }
        }
        #endregion

        #region OVERRIDES: Adding Entities

        internal override bool AddEntity(DXFEntity _e)
        {
            // handle depending on type
            if (_e == null) return false;
            bool add_successful = false;

            DXFAccessProfile ap = _e as DXFAccessProfile;
            if (ap != null)
            {
                this.dxf_AccessLocal = ap.dxf_parsed;
                return true;
            }

            DXFComponentSubContainer container = _e as DXFComponentSubContainer;
            if (container != null)
            {
                add_successful = true;
                foreach (DXFEntity sE in container.EC_Entities)
                {
                    if (sE is DXFParameter param && this.dxf_nr_ContainedParameters > this.dxf_ContainedParameters.Count)
                    {
                        // take the parsed parameter
                        this.dxf_ContainedParameters.Add(param.dxf_parsed);
                        // delete it from the parameter factory
                        this.Decoder.ProjectData.ParameterLibraryManager.DeleteRecord(param.dxf_parsed.Id.LocalId);
                        add_successful &= true;
                    }

                    if (sE is DXFComponent sComp)
                    {
                        // take the parsed component
                        var key = SimSlot.FromSerializerString(sComp.ENT_KEY);
                        if (this.dxf_ContainedComponents.Any(x => x.slot == key))
                        {
                            add_successful &= false;
                            continue;
                        }

                        // take the parsed component
                        this.dxf_ContainedComponents.Add((key, sComp.dxf_parsed));
                        add_successful &= true;
                    }

                    DXFCalculation sCalc = sE as DXFCalculation;
                    if (sCalc != null && sCalc.dxf_parsed != null)
                    {
                        if (this.dxf_nr_ContainedCalculations > this.dxf_ContainedCalculations_Ref.Count)
                            this.dxf_ContainedCalculations_Ref.Add(sCalc.dxf_parsed);
                    }

                    DXFComponentInstance gr = sE as DXFComponentInstance;
                    if (gr != null && gr.dxf_parsed != null &&
                        this.dxf_nr_R2GInstances > this.dxf_R2GInstances.Count)
                    {
                        this.dxf_R2GInstancesTypes.Add(gr.dxf_Type);
                        this.dxf_R2GInstances.Add(gr.dxf_parsed);
                    }

                    DXFMapping2Component map = sE as DXFMapping2Component;
                    if (map != null && map.dxf_parsed != null &&
                        this.dxf_nr_Mapping2Comps > this.dxf_Mapping2Comps.Count)
                    {
                        this.dxf_Mapping2Comps.Add(map.dxf_parsed);
                    }

                    DXFComponent2ExcelMappingRule excel_map = sE as DXFComponent2ExcelMappingRule;
                    if (excel_map != null && excel_map.dxf_parsed != null &&
                        this.dxf_nr_MappingsPerExcelTool > this.dxf_MappingsPerExcelTool.Count)
                    {
                        // take the parsed key
                        string key = excel_map.ENT_KEY;
                        if (string.IsNullOrEmpty(key) || this.dxf_MappingsPerExcelTool.ContainsKey(key))
                        {
                            add_successful &= false;
                            continue;
                        }

                        // take the parsed mapping
                        this.dxf_MappingsPerExcelTool.Add(key, excel_map.dxf_parsed);
                        add_successful &= true;
                    }

                    DXFChatItem chat_item = sE as DXFChatItem;
                    if (chat_item != null && chat_item.dxf_parsed != null &&
                        this.dxf_nr_ChatItems > this.dxf_ChatItems.Count)
                    {
                        // take the parsed chat item
                        this.dxf_ChatItems.Add(chat_item.dxf_parsed);
                        add_successful &= true;
                    }

                    DXFByteColor color = sE as DXFByteColor;
                    if (color != null && color.dxf_parsed != null)
                    {
                        // take the parsed color
                        this.dxf_ComponentColor = color.dxf_parsed;
                        add_successful &= true;
                    }
                }
            }

            return add_successful;
        }

        #endregion

        #region OVERRIDES: Post-Processing
        internal override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder.CurrentFileVersion < 7)
            {
                //Remove instance 0
                if (this.dxf_R2GInstances.Count > 0)
                {
                    if (this.dxf_instanceType == SimInstanceType.None)
                        this.dxf_instanceType = this.dxf_R2GInstancesTypes[0];

                    //Check if first instance is used, if not -> remove
                    if ((!this.dxf_R2GInstances[0].State.IsRealized && this.dxf_R2GInstances[0].Placements.Count == 1
                        && this.dxf_R2GInstances[0].Placements[0] is SimInstancePlacementGeometry gp &&
                        gp.GeometryId == ulong.MaxValue
                        && this.dxf_R2GInstances[0].LoadingNetworkElementId.LocalId == -1)
                        ||
                        (this.dxf_R2GInstances[0].Placements.Count == 0 && this.dxf_R2GInstances[0].LoadingNetworkElementId.LocalId == -1))
                    {
                        this.dxf_R2GInstances.RemoveAt(0);
                    }
                }
            }

            //Translate ids (needed because id=0 now means empty, previously -1 has been used)
            this.dxf_ID = this.Decoder.TranslateComponentIdV8(this.dxf_ID);

            //Create references
            List<SimComponentReference> referencedComponents = new List<SimComponentReference>();

            var projectId = this.Decoder.ProjectData.Owner != null ? this.Decoder.ProjectData.Owner.GlobalID : Guid.Empty;

            for (int i = 0; i < dxf_ReferencedComponents.Count; ++i)
            {
                var currentRef = dxf_ReferencedComponents[i];

                var globalRefId = currentRef.globalId;
                //Reset reference global ids in case the project Id doesn't match the current component id. This is needed
                //when the collection is imported into another project
                if (this.ENT_LOCATION != projectId && this.ENT_LOCATION == currentRef.globalId)
                {
                    globalRefId = projectId;
                }

                referencedComponents.Add(new SimComponentReference(
                SimSlot.FromSerializerString(currentRef.slot),
                    new SimId(globalRefId, Decoder.TranslateComponentIdV8(currentRef.localId))
                ));
            }

            //Create and add to collection (if toplevel)
            bool isRootComponent = string.IsNullOrEmpty(this.ENT_KEY);

            this.dxf_parsed = new SimComponent(this.ENT_LOCATION, this.dxf_ID,
                this.dxf_Name, this.dxf_Description, this.dxf_IsAutomaticallyGenerated,
                this.dxf_AccessLocal, dxf_CurrentSlot.SlotBase,
                this.dxf_ContainedComponents, referencedComponents,
                this.dxf_ContainedParameters, this.dxf_ContainedCalculations_Ref,
                this.dxf_instanceType, this.dxf_R2GInstances,
                this.dxf_Mapping2Comps, this.dxf_MappingsPerExcelTool, this.dxf_ChatItems,
                this.dxf_Visibility, this.dxf_ComponentColor, this.dxf_SortingType, this.dxf_SymbolId
                );

            //Set parameters to auto propagate for old composite components
            if (Decoder.CurrentFileVersion <= 9)
            {
                if (this.dxf_parsed.InstanceType == SimInstanceType.AttributesFace)
                {
                    var propagationParameter = this.dxf_parsed.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_INST_PROPAGATE);
                    if (propagationParameter != null)
                    {
                        propagationParameter.ValueCurrent = 1.0;
                    }

                    //Reset instance parameters for propagating params
                    foreach (var param in dxf_parsed.Parameters.
                        Where(x => x.InstancePropagationMode != SimParameterInstancePropagation.PropagateNever))
                    {
                        foreach (var instance in dxf_parsed.Instances)
                        {
                            // as of version 11 the propagation parameter might not exist anymore, make sure this is set correctly
                            instance.PropagateParameterChanges = true; 
                            for (int i = 0; i < instance.LoadingParameterValuesPersistent.Count; ++i)
                            {
                                if (instance.LoadingParameterValuesPersistent[i].id.LocalId == param.Id.LocalId ||
                                    (instance.LoadingParameterValuesPersistent[i].id == SimId.Empty && 
                                     instance.LoadingParameterValuesPersistent[i].parameterName == param.Name))
                                {
                                    instance.LoadingParameterValuesPersistent.RemoveAt(i);
                                    i--;
                                }
                            }
                        }
                    }
                }
            }

            // migrate instance propagation parameter
            if(Decoder.CurrentFileVersion <= 10)
            {
                var propagationParameter = this.dxf_parsed.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_INST_PROPAGATE);
                if(propagationParameter != null)
                {
                    foreach(var instance in dxf_parsed.Instances)
                    {
                        instance.PropagateParameterChanges = propagationParameter.ValueCurrent != 0;
                    }
                    dxf_parsed.Parameters.Remove(propagationParameter);
                }
            }

            if (isRootComponent)
                this.Decoder.ProjectData.Components.Add(dxf_parsed);
        }

        #endregion

        #region OVERRIDES: To String
        public override string ToString()
        {
            string dxfS = "DXFComponent ";
            if (!(string.IsNullOrEmpty(this.dxf_Name)))
                dxfS += this.dxf_Name + ": ";

            int n = this.dxf_ContainedParameters.Count();
            dxfS += " has " + n.ToString() + " parameters:\n";
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
