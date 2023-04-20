using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.FlowNetworks
{
    public class SimFlowNetworkNode : SimFlowNetworkElement
    {
        #region PROPERTIES: Specific (Position, derived: Edges_In, derived: Edges_Out)

        protected Point position;
        public Point Position
        {
            get { return this.position; }
            set
            {
                if (this.position.X != value.X || this.position.Y != value.Y)
                {
                    var old_value = new Point(this.position.X, this.position.Y);
                    this.position = value;
                    var new_value = new Point(this.position.X, this.position.Y);
                    this.SetValidity();
                    this.NotifyPropertyChanged(nameof(Position));
                }
            }
        }

        // derived from association with FlNetEdge objects (no observation needed here)
        public ObservableCollection<SimFlowNetworkEdge> Edges_In { get; internal set; }
        public ObservableCollection<SimFlowNetworkEdge> Edges_Out { get; internal set; }

        // derived, resulting from sorting all nodes in flow direction in nested flow networks (no observation needed here)
        public ObservableCollection<SimFlowNetworkEdge> Edges_In_Nested { get; protected set; }
        public ObservableCollection<SimFlowNetworkEdge> Edges_Out_Nested { get; protected set; }

        internal void TransferConnectionsToNested()
        {
            if (this.Edges_In_Nested == null)
                this.Edges_In_Nested = new ObservableCollection<SimFlowNetworkEdge>(this.Edges_In);
            else
            {
                foreach (var e in this.Edges_In)
                {
                    if (!this.Edges_In_Nested.Contains(e))
                        this.Edges_In_Nested.Add(e);
                }
            }

            if (this.Edges_Out_Nested == null)
                this.Edges_Out_Nested = new ObservableCollection<SimFlowNetworkEdge>(this.Edges_Out);
            else
            {
                foreach (var e in this.Edges_Out)
                {
                    if (!this.Edges_Out_Nested.Contains(e))
                        this.Edges_Out_Nested.Add(e);
                }
            }
        }

        internal virtual void ResetNested()
        {
            this.Edges_In_Nested?.Clear();
            this.Edges_Out_Nested?.Clear();
        }

        #endregion

        #region PROPERTIES: Specific (Operations)

        protected ObservableCollection<SimFlowNetworkCalcRule> calculation_rules;
        public ObservableCollection<SimFlowNetworkCalcRule> CalculationRules
        {
            get { return this.calculation_rules; }
            set
            {
                this.calculation_rules = value;
            }
        }

        #endregion

        #region METHODS: General overrides

        internal override void SetValidity()
        {
            if (double.IsNaN(this.position.X) || double.IsNaN(this.position.Y) ||
                double.IsInfinity(this.position.X) || double.IsInfinity(this.position.Y) ||
                (this.Edges_In.Count == 0 && this.Edges_Out.Count == 0))
                this.IsValid = false;
            else
                this.IsValid = true;
        }

        #endregion

        #region .CTOR
        internal SimFlowNetworkNode(IReferenceLocation _location, Point _pos)
            : base(_location)
        {
            this.name = "Node " + this.ID.LocalId.ToString();
            this.Edges_In = new ObservableCollection<SimFlowNetworkEdge>();
            this.Edges_Out = new ObservableCollection<SimFlowNetworkEdge>();
            this.position = _pos;
            this.calculation_rules = new ObservableCollection<SimFlowNetworkCalcRule>();
        }

        #endregion

        #region COPY .CTOR

        internal SimFlowNetworkNode(SimFlowNetworkNode _original)
            : base(_original)
        {
            this.position = _original.position;
            // we do NOT copy content
            // we do NOT copy derived properties (Edges_In, Edges_Out)
            // we do NOT copy calculation rules
            this.Edges_In = new ObservableCollection<SimFlowNetworkEdge>();
            this.Edges_Out = new ObservableCollection<SimFlowNetworkEdge>();
            this.calculation_rules = new ObservableCollection<SimFlowNetworkCalcRule>();
        }

        #endregion

        #region PARSING .CTOR

        // for parsing
        // the content component has to be parsed FIRST
        internal SimFlowNetworkNode(Guid _location, long _id, string _name, string _description, bool _is_valid, Point _position,
            IEnumerable<SimFlowNetworkCalcRule> _calc_rules)
            : base(_location, _id, _name, _description)
        {
            this.is_valid = _is_valid;

            this.position = _position;

            this.calculation_rules = (_calc_rules == null) ? new ObservableCollection<SimFlowNetworkCalcRule>() : new ObservableCollection<SimFlowNetworkCalcRule>(_calc_rules);

            // derived properties: i.e. Edges_In, Edges_Out are filled in later, when the edges are parsed
            this.Edges_In = new ObservableCollection<SimFlowNetworkEdge>();
            this.Edges_Out = new ObservableCollection<SimFlowNetworkEdge>();
        }

        #endregion

        #region METHODS: ToString overrides

        public override string ToString()
        {
            return "Node " + this.ID.ToString() + " " + this.Name + " " + this.ContentToString();
        }

        #endregion

        #region METHODS: General Flow Calculation

        /*
         *  I. The calculations in the flow network are performed as follows (30.06.2017):
         *  ----------------------------------------------------------------------------- 
         *  1. Create a component with Ralation2GeomType CONTAINES_IN or CONNECTING
         *  2. Add the desired parameters (NOT for size), sub- and referenced components.
         *  3. Place an instance of the Component in a NW Element.
         *      a. Ralation2GeomType CONTAINED_IN only in a node
         *      b. Ralation2GeomType CONNECTING only in an edge
         *  4. The first placement causes the automatic creation of sub-components containing instance and type size parameters.
         *     NOTE: if the Ralation2GeomType of the component is neither CONTAINED_IN nor CONNECTING no sub-components are created
         *  5. Formulate calculations (those can also include the size parameters).
         *     NOTE: Use the window for component comparison for fast copying of parameters and calculations
         *           from one component to another
         *  6. Individual instance sizes are assigned in the NW Editor (see InstanceSize).
         *  7. Each instance also carries an individual copy of the component's parameter values (see InstanceParameterValues*).
         *     NOTE: Excluded from this are parameters with propagation CALC_IN (they are used as type, not instance parameters).
         *  8. Continue placing instances.
         *  9. For nested Flow Networks do not forget to mark the sink and the source nodes.
         *  10. Set the calculation rules in all relevant nodes (e.g. fork, transfer, etc.). The sequence of the rules should correspond to the
         *      sequence in which the calculations are to be performed.
         *      NOTE: To remove a rule choose the operator NoNe.
         *  11. Calculate: calculations are performed on the instance values and NOT on the parameters contained in the Component.
         *      For this reason set the component parameter to their desired initial values.
         *      
         *  NOTE: Parameters with propagation CALC_IN are global and contain cumulative values over all instances.
         *        They are automatically generated and not to be altered by the user.
         *        This differs from the flow propagation method used in the previous versions of the flow calculation (e.g. SynchFlows)
         *  NOTE: When performing calculations repeatedly with different input values (contained in parameters with propagation INPUT)
         *        perform the reset AFTER entering the new values.
         *  
         *  II. Perform placement in a geometric space in the GeometryViewer program module
         */

        // the nodes need to be SORTED in flow direction before calling this method!
        // Note: Method GetFirstParamBySuffix is called according to the DYNAMIC Type of the caller

        internal void CalculateFlow(bool _in_flow_dir)
        {
            if (this.Content == null) return;

            // get the appropriate instance and update its parameter slots w/o resetting
            SimComponentInstance instance = this.Content;
            if (instance == null) return;
            this.UpdateContentInstance();
            // Debug.WriteLine("START Instance:\t\t\t" + instance.ToString());

            foreach (SimFlowNetworkCalcRule rule in this.calculation_rules)
            {
                // check if the calculation direction matches the rule
                if ((rule.Direction == SimFlowNetworkCalcDirection.Backward && _in_flow_dir) ||
                    (rule.Direction == SimFlowNetworkCalcDirection.Forward && !_in_flow_dir))
                    continue;

                // check if the rule applies to the type component of the instance in this node
                SimDoubleParameter p_result = this.GetFirstParamBySuffix(rule.Suffix_Result, _in_flow_dir);
                if (p_result == null) continue;

                if (!(instance.InstanceParameterValuesTemporary.Contains(p_result)))
                    continue;

                if (_in_flow_dir)
                {
                    if (this.Edges_In_Nested.Count > 0)
                    {
                        foreach (SimFlowNetworkEdge e in this.Edges_In_Nested)
                        {
                            if (e.Content == null) continue;
                            if (!e.CanCalculateFlow(_in_flow_dir)) continue;

                            SimComponentInstance instance_in_e = e.Content;
                            SimDoubleParameter p_e_Operand = e.GetFirstParamBySuffix(rule.Suffix_Operands, _in_flow_dir);
                            SimDoubleParameter p_e_Result = e.GetFirstParamBySuffix(rule.Suffix_Result, _in_flow_dir);
                            SimComponentInstance instance_in_eStart = e.Start.Content;
                            SimDoubleParameter p_eStart_Operand = e.Start.GetFirstParamBySuffix(rule.Suffix_Operands, _in_flow_dir);

                            if (instance_in_e == null || p_e_Operand == null || p_e_Result == null) continue;
                            if (!(instance_in_e.InstanceParameterValuesTemporary.Contains(p_e_Operand)) || !(instance_in_e.InstanceParameterValuesTemporary.Contains(p_e_Result)))
                                continue;

                            // propagate incoming value along edge
                            if (instance_in_eStart != null && p_eStart_Operand != null && instance_in_eStart.InstanceParameterValuesTemporary.Contains(p_eStart_Operand))
                            {
                                e.Start.UpdateContentInstance();
                                //e.UpdateContentInstance(); // added 26.07.2018
                                instance_in_e.InstanceParameterValuesTemporary[p_e_Result] = instance_in_eStart.InstanceParameterValuesTemporary[p_eStart_Operand];
                                e.UpdateContentInstance();
                            }

                            // perform operation in this node
                            double operation_result = rule.Calculate((double)instance.InstanceParameterValuesTemporary[p_result], (double)instance_in_e.InstanceParameterValuesTemporary[p_e_Operand]);
                            instance.InstanceParameterValuesTemporary[p_result] = operation_result;
                        }
                    }
                }
                else
                {
                    if (this.Edges_Out_Nested.Count > 0)
                    {
                        foreach (SimFlowNetworkEdge e in this.Edges_Out_Nested)
                        {
                            if (e.Content == null) continue;
                            if (!e.CanCalculateFlow(_in_flow_dir)) continue;

                            SimComponentInstance instance_in_e = e.Content;
                            SimDoubleParameter p_e_Operand = e.GetFirstParamBySuffix(rule.Suffix_Operands, _in_flow_dir);
                            SimDoubleParameter p_e_Result = e.GetFirstParamBySuffix(rule.Suffix_Result, _in_flow_dir);
                            SimComponentInstance instance_in_eEnd = e.End.Content;
                            SimDoubleParameter p_eEnd_Operand = e.End.GetFirstParamBySuffix(rule.Suffix_Operands, _in_flow_dir);

                            if (instance_in_e == null || p_e_Operand == null || p_e_Result == null) continue;
                            if (!(instance_in_e.InstanceParameterValuesTemporary.Contains(p_e_Operand)) || !(instance_in_e.InstanceParameterValuesTemporary.Contains(p_e_Result)))
                                continue;

                            // propagate outgoing value along edge
                            if (instance_in_eEnd != null && p_eEnd_Operand != null && instance_in_eEnd.InstanceParameterValuesTemporary.Contains(p_eEnd_Operand))
                            {
                                e.End.UpdateContentInstance();
                                //e.UpdateContentInstance(); // added 26.07.2018
                                instance_in_e.InstanceParameterValuesTemporary[p_e_Result] = instance_in_eEnd.InstanceParameterValuesTemporary[p_eEnd_Operand];
                                e.UpdateContentInstance();
                            }


                            // perform operation in this node
                            double operation_result = rule.Calculate((double)instance.InstanceParameterValuesTemporary[p_result], (double)instance_in_e.InstanceParameterValuesTemporary[p_e_Operand]);
                            instance.InstanceParameterValuesTemporary[p_result] = operation_result;
                        }
                    }
                }
            }

            // Debug.WriteLine("Instance before calc:\t" + instance.ToString());
            this.UpdateContentInstance();
            // Debug.WriteLine("END Instance:\t\t\t" + instance.ToString());
        }


        #endregion
    }
}
