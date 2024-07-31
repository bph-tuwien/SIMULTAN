using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace SIMULTAN.Data.FlowNetworks
{
    /// <summary>
    /// Base class for all flow network elements.
    /// </summary>
    public abstract class SimFlowNetworkElement : SimObject
    {
        #region STATIC

        internal static long NR_FL_NET_ELEMENTS = 0;

        #endregion

        #region PROPERTIES: Overrides

        public override string Name
        {
            get { return base.Name; }
            set
            {
                base.Name = value;
            }
        }

        public SimFlowNetworkElement Parent { get; internal set; }

        #endregion

        #region PROPERTIES: General (Content, IsValid)

        public SimComponentInstance Content
        {
            get { return this.content; }
            internal set
            {
                if (this.content != value)
                {
                    //Remove from old content
                    if (this.content != null)
                        this.content.Component.PropertyChanged -= Content_Component_PropertyChanged;

                    this.content = value;

                    if (this.content != null)
                        this.content.Component.PropertyChanged += Content_Component_PropertyChanged;

                    this.NotifyPropertyChanged(nameof(Content));
                }
            }
        }
        private SimComponentInstance content;

        private void Content_Component_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName == nameof(SimComponent.Name)) ||
                (e.PropertyName == nameof(SimComponent.Description)) ||
                (e.PropertyName == nameof(SimComponent.Slots)))
            {
                //This one is unfortunately needed since the UI listens to it...
                this.NotifyPropertyChanged(nameof(Content));
            }
        }

        /// <summary>
        /// Moves the contents of one network element to another w/o deleting or creating objects.
        /// </summary>
        /// <param name="_source">the source network element</param>
        /// <param name="_target">the target network element</param>
        protected static void TransferContent(SimFlowNetworkElement _source, SimFlowNetworkElement _target)
        {
            if (_source.GetType() != _target.GetType())
                throw new ArgumentException("The source and target should be of the same type!");
            if (_source.Content == null || _target.Content != null)
                throw new Exception("The source should have content, the target should have none!");

            var instance = _source.Content;
            var placement = (SimInstancePlacementNetwork)instance.Placements.FirstOrDefault(p => p is SimInstancePlacementNetwork nw && nw.NetworkElement == _source);

            // transfer the correct properties to the instance
            placement.NetworkElement = _target;
        }

        protected bool is_valid;
        public bool IsValid
        {
            get { return this.is_valid; }
            protected set
            {
                this.is_valid = value;
                this.NotifyPropertyChanged(nameof(IsValid));
            }
        }

        #endregion

        #region PROPERTIES: for geometric representation

        private GeometricReference geom_representation_ref;
        /// <summary>
        /// Saves the reference to the *representing* geometry. In case 
        /// that the <see cref="Content"/> property is not Null, the contained <see cref="SimComponentInstance"/>
        /// saves the reference to the *containing* geometry.
        /// </summary>
        public GeometricReference RepresentationReference
        {
            get { return this.geom_representation_ref; }
            set
            {
                if (this.geom_representation_ref != value)
                {
                    this.geom_representation_ref = value;
                    this.NotifyPropertyChanged(nameof(RepresentationReference));
                }
            }
        }

        #endregion

        #region METHODS: General

        internal virtual void SetValidity()
        { }

        #endregion

        internal SimFlowNetworkElement(IReferenceLocation _location)
        {
            this.id = (_location == null) ? new SimObjectId(++SimFlowNetworkElement.NR_FL_NET_ELEMENTS) : new SimObjectId(_location, ++SimFlowNetworkElement.NR_FL_NET_ELEMENTS);
            this.name = "FlNetElement " + this.ID.ToString();
            this.description = string.Empty;

            this.Content = null;
            this.is_valid = false;
            this.geom_representation_ref = GeometricReference.Empty;
        }

        internal SimFlowNetworkElement(SimFlowNetworkElement _original)
        {
            this.id = (_original.ID.GlobalLocation == null) ? new SimObjectId(++SimFlowNetworkElement.NR_FL_NET_ELEMENTS) : new SimObjectId(_original.ID.GlobalLocation, ++SimFlowNetworkElement.NR_FL_NET_ELEMENTS);
            this.name = _original.Name;
            this.description = _original.Description;

            this.Content = null;
            this.is_valid = false;
            this.geom_representation_ref = GeometricReference.Empty;
        }

        internal SimFlowNetworkElement(Guid _location, long _id, string _name, string _description)
        {
            this.id = new SimObjectId(_location, _id);
            SimFlowNetworkElement.NR_FL_NET_ELEMENTS = Math.Max(SimFlowNetworkElement.NR_FL_NET_ELEMENTS, this.id.LocalId);
            this.name = _name;
            this.description = _description;

            this.Content = null;
            this.is_valid = false;
            this.geom_representation_ref = GeometricReference.Empty;
        }


        #region METHODS: Update

        /// <summary>
        /// <para>Performs all calculations on all levels within the component instance and</para>
        /// <para>transfers the results to other recipients (e.g. the size variables of the instance).</para>
        /// </summary>
        internal virtual void UpdateContentInstance()
        {
            if (this.Content != null)
            {
                ExecuteCalculationChainWoArtefacts(this.Content, this);
                this.NotifyPropertyChanged(nameof(Content));
            }
        }

        internal virtual void ResetContentInstance(SimPoint _default_offset)
        {
            if (this.Content != null)
            {
                this.Content.Reset();
                this.NotifyPropertyChanged(nameof(Content));
            }
        }

        /// <summary>
        /// For calculating with the parameter slots in the semantic instances w/o affecting the component and its parameters.
        /// Performed recursively in case the sub-components contain calculations themselves.
        /// </summary>
        /// <param name="instance">the calling instance</param>
        /// <param name="container">the network element containing the semantic inctance</param>
        private static void ExecuteCalculationChainWoArtefacts(SimComponentInstance instance, SimFlowNetworkElement container)
        {
            // populate the parameter values
            if (instance.InstanceParameterValuesTemporary == null || instance.InstanceParameterValuesTemporary.Count == 0)
                instance.Reset();

            // recursion
            ExecuteCalculationChainForInstance(instance.Component, instance);
        }

        /// <summary>
        /// Executes calculations in all subcomponents and saves the result in the given 
        /// semantic instance. Uses depth-first sub-component search.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_instance">the semantic instance of the component</param>
        private static void ExecuteCalculationChainForInstance(SimComponent _comp, SimComponentInstance _instance)
        {
            foreach (var entry in _comp.Components.Where(x => x.Component != null))
            {
                ExecuteCalculationChainForInstance(entry.Component, _instance);
            }

            if (_comp.Calculations.Count > 0)
            {
                //TEMPORARY TRANSFER OF NAME->Parameter. Should be dropped when InstanceParamValues gets refactored
                Dictionary<SimDoubleParameter, double> instanceParameterValues = _instance.InstanceParameterValuesTemporary.GetRecords<SimDoubleParameter, double>().ToDictionary(x => x.Key, y => y.Value);
                foreach (SimCalculation c in _comp.Calculations)
                {
                    c.Calculate(instanceParameterValues);

                    foreach (var param in instanceParameterValues)
                        _instance.InstanceParameterValuesTemporary[param.Key] = param.Value;
                }
            }
        }

        #endregion

        #region METHODS: Info

        internal virtual SimDoubleParameter GetFirstParamBySuffix(string _suffix, bool _in_flow_dir, CultureInfo culture)
        {
            if (this.Content == null)
                return null;
            return ComponentWalker.GetFlatParameters<SimDoubleParameter>(this.Content.Component).FirstOrDefault(x => x.NameTaxonomyEntry.GetLocalizedName(culture).EndsWith(_suffix));
        }

        public virtual bool GetBoundInstanceRealizedStatus()
        {
            SimComponentInstance gr = this.Content;
            if (gr != null)
                return gr.State.IsRealized;
            else
                return false;
        }

        /// <summary>
        /// Returns the size of this node.
        /// </summary>
        /// <returns>Returns six values. First three are for minimum sizes, next three for maximum sizes</returns>
        public virtual SimInstanceSize GetInstanceSize()
        {
            SimComponentInstance instance = this.Content;
            if (instance == null) return SimInstanceSize.Default;

            return instance.InstanceSize;
        }

        public bool InstanceHasPath()
        {
            SimComponentInstance instance = this.Content;
            if (instance == null) return false;
            if (!instance.Component.InstanceType.HasFlag(SimInstanceType.NetworkEdge)) return false;

            return true;
        }

        public bool InstanceHasValidPath()
        {
            SimComponentInstance instance = this.Content;
            if (instance == null) return false;
            if (!instance.Component.InstanceType.HasFlag(SimInstanceType.NetworkEdge)) return false;

            return instance.State.IsRealized;
        }

        #endregion

        #region METHODS: ToString


        protected string ContentToString()
        {
            string geom_rep = (this.RepresentationReference != GeometricReference.Empty) ? " [ ]" :
                                                        " [" + this.RepresentationReference.FileId + ", " + this.RepresentationReference.GeometryId + "]";
            if (this.Content == null)
                return "[ ]" + geom_rep;
            else
                return "[ " + this.Content.Component.Slots[0] + ": " +
                    this.Content.Component.Id + " " + this.Content.Component.Name + " " + this.Content.Component.Description + " ]" + geom_rep;
        }

        #endregion

        public SimFlowNetwork Network { get; internal set; }
    }
}
