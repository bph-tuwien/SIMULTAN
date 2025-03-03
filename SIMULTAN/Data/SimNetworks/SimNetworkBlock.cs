using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Exchange;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static SIMULTAN.Data.SimNetworks.SimNetworkPort;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Element representing a block in the network. Can contain component
    /// </summary>
    public class SimNetworkBlock : BaseSimNetworkElement, IElementWithComponent
    {
        private bool componentIsDeleted = false;
        private bool isBeingDeleted = false;
        private bool isBeingAssignedToComponent = false;

        /// <summary>
        /// Representing an attached component to the network block element
        /// </summary>
        public SimComponentInstance ComponentInstance
        {
            get { return this.componentInstance; }
            set
            {
                this.componentInstance = value;
                this.NotifyPropertyChanged(nameof(this.ComponentInstance));
            }
        }
        private SimComponentInstance componentInstance;


        /// <summary>
        /// Tells whether the block is static. If it is static, then the ports have a relative position (or at least the attached components)
        /// </summary>
        public bool IsStatic
        {
            get
            {
                return this.Ports.Any(p => p.ComponentInstance != null
                            && p.ComponentInstance.Component.Parameters.Any(n => n.NameTaxonomyEntry.TaxonomyEntryReference != null && n.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X)
                            && p.ComponentInstance.Component.Parameters.Any(t => t.NameTaxonomyEntry.TaxonomyEntryReference != null && t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y)
                            && p.ComponentInstance.Component.Parameters.Any(k => k.NameTaxonomyEntry.TaxonomyEntryReference != null && k.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z));
            }
        }


        #region .CTOR
        /// <summary>
        /// Constructs a new instance of the SimNetworkBlock
        /// </summary>
        /// <param name="name">Name of the block</param>
        /// <param name="position">The optional position</param>
        /// <exception cref="ArgumentNullException">Exception whenever no name is given</exception>
        public SimNetworkBlock(string name, SimPoint position)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.RepresentationReference = GeometricReference.Empty;
            this.Name = name;
            this.Position = position;
            this.Id = SimId.Empty;
            this.Ports = new SimNetworkPortCollection(this);
            this.IsBeingDeleted += this.SimNetworkBlock_IsBeingDeleted;
            this.IsDeleted += this.SimNetworkBlock_IsDeleted;
            this.Color = SimColors.DarkGray;
        }

        /// <summary>
        /// Constructor for parsing
        /// </summary>
        /// <param name="name">Name of the SimNetworkBlock</param>
        /// <param name="position">Position of the SimNetworkBlock</param>
        /// <param name="id">ID of the SimNetworkBlock</param>
        /// <param name="ports">The ports which belong to this block</param>
        /// <param name="color">Color of the block</param>
        public SimNetworkBlock(string name, SimPoint position, SimId id, IEnumerable<SimNetworkPort> ports, SimColor color)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (ports == null)
                throw new ArgumentNullException(nameof(ports));

            this.Name = name;
            this.Position = position;
            this.Id = id;
            this.Color = color;

            this.Ports = new SimNetworkPortCollection(this);
            foreach (var port in ports)
                Ports.Add(port);

            //Do not forget to attach events with 
        }

        /// <summary>
        /// attaches to Block events
        /// </summary>
        public void AttachEvents()
        {
            SubscribeToComponentEvents();
            this.IsBeingDeleted += this.SimNetworkBlock_IsBeingDeleted;
            this.IsDeleted += this.SimNetworkBlock_IsDeleted;
        }

        /// <summary>
        /// Constructor for cloning
        /// </summary>
        /// <param name="sourceBlock">The SimNetworkBlock we base our clone on</param>
        private SimNetworkBlock(SimNetworkBlock sourceBlock)
        {
            this.Name = sourceBlock.Name;
            this.Position = sourceBlock.Position;
            this.Color = sourceBlock.Color;
            this.Id = this.Id = SimId.Empty;
            this.RepresentationReference = GeometricReference.Empty;
            this.Ports = new SimNetworkPortCollection(this);
            this.Width = sourceBlock.Width;
            this.Height = sourceBlock.Height;
            this.IsBeingDeleted += this.SimNetworkBlock_IsBeingDeleted;
            this.IsDeleted += this.SimNetworkBlock_IsDeleted;
        }

        #endregion


        #region Functions
        /// <summary>
        /// Clones this SimNetworkBlock
        /// </summary>
        /// <param name="parent">The parent network</param>
        /// <returns>Returns the cloned SimNetworkBLock, and a Dictionary with the original and cloned port LocalId pairs</returns>
        public (SimNetworkBlock Cloned, Dictionary<SimId, SimId> PortPairs) Clone(SimNetwork parent)
        {
            var cloned = new SimNetworkBlock(this);
            parent.ContainedElements.Add(cloned);
            var portIdPairs = new Dictionary<SimId, SimId>();
            foreach (var port in this.Ports)
            {
                var newPort = new SimNetworkPort(port);
                cloned.Ports.Add(newPort);
                portIdPairs.Add(port.Id, newPort.Id);
            }
            if (this.componentInstance != null)
            {
                cloned.AssignComponent(this.componentInstance.Component, true);
            }


            return (cloned, portIdPairs);
        }

        private void SimNetworkBlock_IsBeingDeleted(object sender)
        {
            this.isBeingDeleted = true;
            this.IsBeingDeleted -= this.SimNetworkBlock_IsBeingDeleted;
        }
        private void SimNetworkBlock_IsDeleted(object sender)
        {
            this.IsDeleted -= this.SimNetworkBlock_IsDeleted;
            this.isBeingDeleted = false;
        }

        /// <summary>
        /// Removes the assigned component instance from the block as well as from the ports of the block
        /// </summary>
        /// <param name="sendAssociatedEvent">If the AssociationChanged event on the network should be called</param>
        public void RemoveComponentInstance(bool sendAssociatedEvent = true)
        {
            var portComponents = this.componentInstance.Component.Components.Select(c => c.Component).Where(c => c.InstanceType.HasFlag(SimInstanceType.InPort) || c.InstanceType.HasFlag(SimInstanceType.OutPort));
            foreach (var portComponent in portComponents)
            {
                var correspondingPort = this.Ports.FirstOrDefault(p => p.ComponentInstance != null && p.ComponentInstance.Component == portComponent);
                if (correspondingPort != null)
                {
                    portComponent.Instances.Remove(correspondingPort.ComponentInstance);
                }

            }
            this.ComponentInstance.Component.Instances.Remove(this.ComponentInstance);
            if (sendAssociatedEvent)
                this.ParentNetwork?.OnAssociationChanged(new[] { this });
        }


        internal void OnPortAdded(SimNetworkPort port)
        {
            if (!this.isBeingDeleted)
            {
                if (this.ComponentInstance != null && this.ComponentInstance.Component != null)
                {
                    //For a new port, create a subcomponent in the associated component, and only one
                    if (port.ComponentInstance == null)
                    {
                        var newSlot = this.ComponentInstance.Component.Components
                            .FindAvailableSlot(this.ComponentInstance.Component.ParentContainer != null
                            ? this.ComponentInstance.Component.ParentContainer.Slot.SlotBase.Target
                            : this.ComponentInstance.Component.Slots[0].Target);

                        var newComp = new SimComponent();
                        newComp.AccessLocal = this.ComponentInstance.Component.AccessLocal;
                        newComp.Slots.Add(new SimTaxonomyEntryReference(newSlot.SlotBase));
                        if (port.PortType == PortType.Input)
                        {
                            newComp.InstanceType = SimInstanceType.InPort;
                        }
                        else
                        {
                            newComp.InstanceType = SimInstanceType.OutPort;
                        }
                        var compInstance = new SimComponentInstance(port);
                        newComp.Instances.Add(compInstance);
                        this.ComponentInstance.Component.Components.Add(new SimChildComponentEntry(newSlot, newComp));


                        if (this.IsStatic)
                        {
                            AddRelativPositionToPortComp(newComp, port);
                        }
                    }
                }
                port.PropertyChanged += this.Port_PropertyChanged;
            }
        }




        internal void OnPortRemoved(SimNetworkPort port)
        {
            if (!this.isBeingDeleted)
            {
                if (port.ComponentInstance != null && this.ComponentInstance != null)
                {
                    var childComp = this.ComponentInstance.Component.Components.FirstOrDefault(c => c.Component == port.ComponentInstance.Component);
                    if (!this.componentIsDeleted)
                    {
                        this.ComponentInstance.Component.Components.Remove(childComp);
                    }
                    else
                    {
                        this.componentIsDeleted = false;
                    }
                }
                port.PropertyChanged -= this.Port_PropertyChanged;
            }
        }


        private void Port_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimNetworkPort.ComponentInstance))
            {
                if (sender is SimNetworkPort port && port.ComponentInstance != null)
                {
                    port.ComponentInstance.Component.IsBeingDeleted += this.Component_IsBeingDeleted;
                }
            };
        }

        /// <summary>
        /// Method to assign a component to the block. Updates the ports as well.
        /// </summary>
        /// <param name="component">The component which is being assigned</param>
        /// <param name="syncBlockToComp">Shows whether the assignment should synchronize from the block to the component as well (meaning, that SubComponents are created during assignment for each port)</param>
        /// <exception cref="ArgumentNullException">Thrown whenever the component is null</exception>
        /// <exception cref="NotSupportedException">Thrown whenever the component does not have the proper InstanceType</exception>
        public void AssignComponent(SimComponent component, bool syncBlockToComp)
        {
            if (component == null)
                throw new ArgumentNullException("Component which should be assigned can not be null");
            if (!component.InstanceType.HasFlag(SimInstanceType.SimNetworkBlock))
                throw new NotSupportedException("Only components with InstanceType SimInstanceType.SimNetworkBlock are allowed");
            this.isBeingAssignedToComponent = true;

            //Handle older components assigned to the block
            if (this.ComponentInstance != null)
            {
                this.RemoveComponentInstance(false);
            }

            //Assign the component (an instance of the component) to the network Element
            var componentInstance = new SimComponentInstance(this);
            component.Instances.Add(componentInstance);

            InitializePorts(component, syncBlockToComp);
            SubscribeToComponentEvents();
            this.isBeingAssignedToComponent = false;

            ParentNetwork?.OnAssociationChanged(new[] { this });
        }

        private void SubscribeToComponentEvents()
        {
            if (this.ComponentInstance != null)
            {
                this.ComponentInstance.IsBeingDeleted += this.ComponentInstance_IsBeingDeleted;
                this.ComponentInstance.Component.IsBeingDeleted += this.Component_IsBeingDeleted;
                this.ComponentInstance.Component.Components.CollectionChanged += this.Components_CollectionChanged;

                if (this.ComponentInstance.Component.Components.Count > 0)
                {
                    foreach (var child in this.ComponentInstance.Component.Components)
                    {
                        child.Component.PropertyChanged += this.SubComps_PropertyChanged;
                    }
                }
            }
        }
        /// <summary>
        /// Creates the according Taxonomy records <see cref="Taxonomy"/> to the block´s ports´ components(X,Y,Z)
        /// </summary>
        /// <param name="portComponent">The port component</param>
        /// <param name="port">The SimNetworkPort</param>
        /// <exception cref="ArgumentException">If the SimComponent is not <see cref="SimInstanceType.InPort"/> or  <see cref="SimInstanceType.OutPort"/></exception>
        public void AddRelativPositionToPortComp(SimComponent portComponent, SimNetworkPort port)
        {
            if (!(portComponent.InstanceType.HasFlag(SimInstanceType.OutPort) || portComponent.InstanceType.HasFlag(SimInstanceType.InPort)))
                throw new ArgumentException(nameof(portComponent));

            ExchangeHelpers.CreateParameterIfNotExists(portComponent, ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X, "X",
                    SimParameterInstancePropagation.PropagateAlways, port.PortType == PortType.Input ? -2 : 2);
            ExchangeHelpers.CreateParameterIfNotExists(portComponent, ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y, "Y",
                     SimParameterInstancePropagation.PropagateAlways, (this.Ports.Where(p => p.PortType == port.PortType).ToList().IndexOf(port)) * 2);
            ExchangeHelpers.CreateParameterIfNotExists(portComponent, ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z, "Z",
                    SimParameterInstancePropagation.PropagateAlways, 0.0);

        }

        /// <summary>
        /// Whenever a new Component is assigned, new ports are created for each subcomponent of the component which has a port type (InPort or OutPort)
        /// </summary>
        private void InitializePorts(SimComponent component, bool syncBlockToComp)
        {
            var components = component.Components.ToList();

            foreach (var child in components.Where(c => c.Component.InstanceType.HasFlag(SimInstanceType.OutPort) ||
                c.Component.InstanceType.HasFlag(SimInstanceType.InPort)))
            {
                if (!this.Ports.Any(p => p.ComponentInstance != null && p.ComponentInstance.Component == child.Component))
                {
                    //Look for existing empty ports which can be associated with the new component
                    SimNetworkPort newPort = this.FindAvailableEmptyPortForCompInstance(child.Component);
                    if (newPort == null)
                    {
                        newPort = this.AddPortForComponent(child.Component);
                        var portComponentInstance = new SimComponentInstance(newPort);
                        child.Component.Instances.Add(portComponentInstance);

                        child.Component.PropertyChanged += this.SubComps_PropertyChanged;
                        child.Component.IsBeingDeleted += this.Component_IsBeingDeleted;
                        this.Ports.Add(newPort);
                    }
                    else
                    {
                        var portComponentInstance = new SimComponentInstance(newPort);
                        child.Component.Instances.Add(portComponentInstance);
                    }
                }
            }
            // when it is the "first assignment" the ports are synchronized both ways (form block --> component and vice-versa)
            if (syncBlockToComp)
            {
                foreach (var prt in this.Ports)
                {
                    if (prt.ComponentInstance == null)
                    {
                        if (prt.ComponentInstance == null)
                        {
                            OnPortAdded(prt);
                        }
                    }
                }
            }
            //Remove all the emtpy ports -> empty port means it does not have any correpsonding subcomponent in the assigned component, hence it is unnecessary
            for (var i = this.Ports.Count - 1; i >= 0; i--)
            {
                var port = this.Ports[i];
                if (port.ComponentInstance == null)
                {
                    this.Ports.Remove(this.Ports[i]);
                }

            }
        }
        private void ComponentInstance_IsBeingDeleted(object sender)
        {
            if (sender is SimComponentInstance compInstance && this.ComponentInstance != null)
            {
                //Remove Ports 
                if (compInstance.Component != this.ComponentInstance.Component)
                {
                    var portsToDelete = this.Ports.Where(p => compInstance.Component.Instances.Any(i => i == p.ComponentInstance)).ToList();

                    for (int i = portsToDelete.Count() - 1; i >= 0; i--)
                    {
                        this.Ports.Remove(portsToDelete[i]);
                    }
                }

                compInstance.IsBeingDeleted -= this.ComponentInstance_IsBeingDeleted;
                compInstance.Component.IsBeingDeleted -= this.Component_IsBeingDeleted;
                compInstance.Component.Components.CollectionChanged -= this.Components_CollectionChanged;
            }
        }


        /// <summary>
        /// Whenever a new sub component is added to the associated component, the block starts to listen to the chnges of that new subComponent, 
        /// and if it has a type which represents a port connection (port in or port out) adds a new Port to the block, and associates them. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Components_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.isBeingAssignedToComponent)
            {
                return;
            }
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is SimChildComponentEntry child)
                        {
                            if (child.Component != null)
                            {
                                UpdatePortComponents(child.Component);
                            }
                            if (child.Component.InstanceType.HasFlag(SimInstanceType.InPort) || child.Component.InstanceType.HasFlag(SimInstanceType.OutPort))
                            {
                                child.PropertyChanged += this.Child_PropertyChanged;
                            }

                        }
                    };
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is SimChildComponentEntry child)
                        {
                            if (child.Component != null)
                            {
                                child.Component.PropertyChanged -= this.SubComps_PropertyChanged;
                                child.Component.IsBeingDeleted -= this.Component_IsBeingDeleted;
                            }
                            else
                            {
                                child.PropertyChanged -= this.Child_PropertyChanged;
                            }
                        }
                    };
                    break;
            }
        }

        private void Child_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimChildComponentEntry.Component))
            {
                if (((SimChildComponentEntry)sender).Component != null)
                {
                    UpdatePortComponents(((SimChildComponentEntry)sender).Component);
                    ((SimChildComponentEntry)sender).PropertyChanged -= this.Child_PropertyChanged;
                }
            }
        }

        private void UpdatePortComponents(SimComponent component)
        {
            component.PropertyChanged += this.SubComps_PropertyChanged;
            component.IsBeingDeleted += this.Component_IsBeingDeleted;
            if (this.Ports.Any(port => port.ComponentInstance != null && port.ComponentInstance.Component == component))
            {
                return;
            }
            if (component.InstanceType == SimInstanceType.InPort || component.InstanceType == SimInstanceType.OutPort)
            {
                //Look for existing empty ports which can be associated with the new component
                SimNetworkPort newPort = this.FindAvailableEmptyPortForCompInstance(component);
                if (newPort == null)
                {
                    newPort = this.AddPortForComponent(component);
                    var portComponentInstance = new SimComponentInstance(newPort);
                    component.Instances.Add(portComponentInstance);

                    this.Ports.Add(newPort);
                }
                else
                {
                    var portComponentInstance = new SimComponentInstance(newPort);
                    component.Instances.Add(portComponentInstance);
                }
            }
        }

        private void Component_IsBeingDeleted(object sender)
        {
            if (sender is SimComponent deletedComp)
            {
                if (this.ComponentInstance == null)
                {
                    return;
                }
                this.componentIsDeleted = true;
                if (deletedComp != this.ComponentInstance.Component && this.Ports != null)
                {
                    var portsToDelete = this.Ports.Where(p => deletedComp.Instances.Any(i => i == p.ComponentInstance)).ToList();

                    for (int i = portsToDelete.Count() - 1; i >= 0; i--)
                    {
                        this.Ports.Remove(portsToDelete[i]);
                    }
                }

                deletedComp.PropertyChanged -= this.SubComps_PropertyChanged;
                deletedComp.IsBeingDeleted -= this.Component_IsBeingDeleted;
                deletedComp.Components.CollectionChanged -= this.Components_CollectionChanged;
                this.componentIsDeleted = false;
            }
        }

        private SimNetworkPort FindAvailableEmptyPortForCompInstance(SimComponent comp)
        {
            PortType type = PortType.Input;
            if (comp.InstanceType.HasFlag(SimInstanceType.InPort))
            {
                type = PortType.Input;
            }
            if (comp.InstanceType.HasFlag(SimInstanceType.OutPort))
            {
                type = PortType.Output;
            }
            var avaialblePort = this.Ports.FirstOrDefault(t => t.PortType == type && t.ComponentInstance == null);
            if (avaialblePort != null)
            {
                return avaialblePort;
            }
            return null;

        }

        private SimNetworkPort AddPortForComponent(SimComponent comp)
        {
            PortType type = PortType.Input;

            if (comp.InstanceType.HasFlag(SimInstanceType.InPort))
            {
                type = PortType.Input;
            }
            if (comp.InstanceType.HasFlag(SimInstanceType.OutPort))
            {
                type = PortType.Output;
            }
            var inPort = new SimNetworkPort(type);
            return inPort;
        }


        private void SubComps_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this.isBeingAssignedToComponent)
            {
                return;
            }
            if (e.PropertyName == nameof(SimComponent.InstanceType))
            {
                if (sender is SimComponent comp && (comp.InstanceType.HasFlag(SimInstanceType.InPort) || comp.InstanceType.HasFlag(SimInstanceType.OutPort)))
                {
                    //Look for existing empty ports which can be associated with the new component
                    SimNetworkPort newPort = this.FindAvailableEmptyPortForCompInstance(comp);
                    comp.Slots.Add(new SimTaxonomyEntryReference(this.ComponentInstance.Component.Slots[0]));
                    if (newPort == null)
                    {
                        newPort = this.AddPortForComponent(comp);
                        var portComponentInstance = new SimComponentInstance(newPort);
                        comp.Instances.Add(portComponentInstance);
                        this.Ports.Add(newPort);
                    }
                    else
                    {
                        var portComponentInstance = new SimComponentInstance(newPort);
                        comp.Instances.Add(portComponentInstance);
                    }
                    //We check if thre are components with "X", "Y", "Z" parameres and if yes, we also give these parameters to the newly created componnet, but
                    //we do it only once, hence we also check the new componentInstnace´s Component of it already contains the "X", "Y", "Z" parameters
                    if (this.Ports.Any(p => p.ComponentInstance != null
                        && p.ComponentInstance.Component.Parameters.Any(n => n.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X))
                        && p.ComponentInstance.Component.Parameters.Any(t => t.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y))
                        && p.ComponentInstance.Component.Parameters.Any(k => k.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z)))
                        && newPort.ComponentInstance.Component != null
                        && !newPort.ComponentInstance.Component.Parameters.Any(n => n.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X))
                        && !newPort.ComponentInstance.Component.Parameters.Any(t => t.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y))
                        && !newPort.ComponentInstance.Component.Parameters.Any(k => k.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z)))
                    {
                        AddRelativPositionToPortComp(newPort.ComponentInstance.Component, newPort);
                    }
                };
            };
        }



        /// <summary>
        /// Converts to SimNetworkBlock to be a static, which means that all the attached SimnetowrkPortComponents get X,Y,Z coordinates
        /// </summary>
        public void ConvertToStatic()
        {
            if (this.ComponentInstance != null)
            {
                foreach (var port in this.Ports)
                {
                    if (port.ComponentInstance != null)
                    {
                        AddRelativPositionToPortComp(port.ComponentInstance.Component, port);
                    }
                }
            }
        }
        #endregion

        internal override void RestoreReferences() { }
    }
}
