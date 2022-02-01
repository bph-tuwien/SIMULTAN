using SIMULTAN.Data;
using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper of class FlNetNode
    /// </summary>
    internal class DXF_FlNetNode : DXF_FlNetElement
    {
        #region CLASS MEMBERS

        protected Point dxf_Position;
        protected List<SimFlowNetworkCalcRule> dxf_CalculationRules;
        protected int dxf_nr_CalculationRules;
        protected int dxf_nr_CalculationRules_read;
        protected SimFlowNetworkCalcRule dxf_current_rule;
        // create the node (and save it internally)
        internal SimFlowNetworkNode dxf_parsed;

        #endregion

        public DXF_FlNetNode()
        {
            this.dxf_Position = new Point(0, 0);

            this.dxf_CalculationRules = new List<SimFlowNetworkCalcRule>();
            this.dxf_nr_CalculationRules = 0;
            this.dxf_nr_CalculationRules_read = 0;
            this.dxf_current_rule = new SimFlowNetworkCalcRule();
        }

        #region OVERRIDES: Read Property
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)FlowNetworkSaveCode.POSITION_X:
                    this.dxf_Position.X = this.Decoder.DoubleValue();
                    break;
                case (int)FlowNetworkSaveCode.POSITION_Y:
                    this.dxf_Position.Y = this.Decoder.DoubleValue();
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULES:
                    this.dxf_nr_CalculationRules = this.Decoder.IntValue();
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_OPERANDS:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Suffix_Operands = this.Decoder.FValue;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_SUFFIX_RESULT:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Suffix_Result = this.Decoder.FValue;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_DIRECTION:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Direction = (this.Decoder.FValue == "1") ? SimFlowNetworkCalcDirection.Forward : SimFlowNetworkCalcDirection.Backward;
                    }
                    break;
                case (int)FlowNetworkSaveCode.CALC_RULE_OPERATOR:
                    if (this.dxf_nr_CalculationRules_read < this.dxf_nr_CalculationRules)
                    {
                        this.dxf_current_rule.Operator = SimFlowNetworkCalcRule.StringToOperator(this.Decoder.FValue);
                        this.dxf_CalculationRules.Add(this.dxf_current_rule);
                        this.dxf_current_rule = new SimFlowNetworkCalcRule();
                        this.dxf_nr_CalculationRules_read++;
                    }
                    break;
                default:
                    // DXF_FlNetNode: ENTITY_ID, NAME, DESCRIPTION, CONTENT_ID, IS_VALID
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

            //Bugfix: global ids were set to Empty for network elements in some older versions
            if (this.ENT_LOCATION == Guid.Empty)
                this.ENT_LOCATION = this.Decoder.ProjectData.Owner.GlobalID;

            // create and save the node ...
            this.dxf_parsed = new SimFlowNetworkNode(this.ENT_LOCATION, this.dxf_ID, this.dxf_Name, this.dxf_Description, this.dxf_IsValid, this.dxf_Position, this.dxf_CalculationRules);
            this.dxf_parsed.RepresentationReference = new GeometricReference(
                this.dxf_RepresentationReference_FileId,
                this.dxf_RepresentationReference_GeometryId);

        }

        #endregion

    }
}
