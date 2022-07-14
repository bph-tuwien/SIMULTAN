using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        #region .CTOR
        /// <summary>
        /// Constructs a new SimNetworkBlock
        /// </summary>
        public SimNetworkBlock(string name, Point position)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (position == null)
                throw new ArgumentNullException(nameof(position));

            this.Name = name;
            this.Position = position;
            this.Id = SimId.Empty;
            this.Ports = new SimNetworkPortCollection(this);
            this.PropertyChanged += this.SimNetworkBlock_PropertyChanged;
            this.IsBeingDeleted += this.SimNetworkBlock_IsBeingDeleted;
            this.IsDeleted += this.SimNetworkBlock_IsDeleted;
        }

        /// <summary>
        /// Constructor for parsing
        /// </summary>
        /// <param name="name">Name of the SimNetworkBlock</param>
        /// <param name="position">Position of the SimNetworkBlock</param>
        /// <param name="id">ID of the SimNetworkBlock</param>
        /// <param name="ports">The ports which belong to this block</param>
        public SimNetworkBlock(string name, Point position, SimId id, IEnumerable<SimNetworkPort> ports)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            if (ports == null)
                throw new ArgumentNullException(nameof(ports));

            this.Name = name;
            this.Position = position;
            this.Id = id;

            this.Ports = new SimNetworkPortCollection(this);
            foreach (var port in ports)
                Ports.Add(port);

            this.PropertyChanged += this.SimNetworkBlock_PropertyChanged;
            this.IsBeingDeleted += this.SimNetworkBlock_IsBeingDeleted;
            this.IsDeleted += this.SimNetworkBlock_IsDeleted;
        }


        private void SimNetworkBlock_IsBeingDeleted(object sender)
        {
            this.isBeingDeleted = true;
            this.IsBeingDeleted -= this.SimNetworkBlock_IsBeingDeleted;
        }
        private void SimNetworkBlock_IsDeleted(object sender)
        {
            this.PropertyChanged -= this.SimNetworkBlock_PropertyChanged;
            this.IsDeleted -= this.SimNetworkBlock_IsDeleted;
            this.isBeingDeleted = false;
        }

        /// <summary>
        /// Removes the assigned component instance from the block as well as from the ports of the block
        /// </summary>
        public void RemoveComponentInstance()
        {
            foreach (var portComponent in this.componentInstance.Component.Components.Select(c => c.Component).Where(c => c.InstanceType == SimInstanceType.InPort || c.InstanceType == SimInstanceType.OutPort))
            {
                var correspondingPort = this.Ports.FirstOrDefault(p => p.ComponentInstance != null && p.ComponentInstance.Component == portComponent);
                portComponent.Instances.Remove(correspondingPort.ComponentInstance);
            }
            this.ComponentInstance.Component.Instances.Remove(this.ComponentInstance);
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
                        var newSlot = this.ComponentInstance.Component.Components.FindAvailableSlot(new SimSlotBase(SimDefaultSlots.Undefined));
                        var newComp = new SimComponent();
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
                    }
                }
                port.PropertyChanged += this.Port_PropertyChanged;
            }
        }

        internal void OnPortRemoved(SimNetworkPort port)
        {
            if (!this.isBeingDeleted)
            {
                if (port.ComponentInstance != null)
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

        private void SimNetworkBlock_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

            if (e.PropertyName == nameof(this.ComponentInstance))
            {
                if (this.ComponentInstance != null)
                {
                    this.isBeingAssignedToComponent = true;
                    this.ComponentInstance.IsBeingDeleted += this.ComponentInstance_IsBeingDeleted;
                    this.ComponentInstance.Component.IsBeingDeleted += this.Component_IsBeingDeleted;
                    this.ComponentInstance.Component.Components.CollectionChanged += this.Components_CollectionChanged;

                    if (this.ComponentInstance.Component.Instances.Count == 0 && this.ComponentInstance.Component.Components.Count == 0)
                    {
                        foreach (var item in this.Ports)
                        {
                            if (item.ComponentInstance != null)
                            {
                                var newSlot = this.ComponentInstance.Component.Components.FindAvailableSlot(new SimSlotBase(SimDefaultSlots.Undefined));
                                var newComp = new SimComponent();
                                if (item.PortType == PortType.Input)
                                {
                                    newComp.InstanceType = SimInstanceType.InPort;
                                }
                                else
                                {
                                    newComp.InstanceType = SimInstanceType.OutPort;
                                }
                                var compInstance = new SimComponentInstance(item);
                                newComp.Instances.Add(compInstance);
                                this.ComponentInstance.Component.Components.Add(new SimChildComponentEntry(newSlot, newComp));
                            }
                            else
                            {
                                var newSlot = this.ComponentInstance.Component.Components.FindAvailableSlot(new SimSlotBase(SimDefaultSlots.Undefined));
                                var newComp = new SimComponent();
                                if (item.PortType == PortType.Input)
                                {
                                    newComp.InstanceType = SimInstanceType.InPort;
                                }
                                else
                                {
                                    newComp.InstanceType = SimInstanceType.OutPort;
                                }
                                var compInstance = new SimComponentInstance(item);
                                newComp.Instances.Add(compInstance);
                                this.ComponentInstance.Component.Components.Add(new SimChildComponentEntry(newSlot, newComp));
                            }
                        }

                    }
                    if (this.ComponentInstance.Component.Components.Count > 0)
                    {
                        foreach (var child in this.ComponentInstance.Component.Components)
                        {
                            child.Component.PropertyChanged += this.SubComps_PropertyChanged;
                        }
                    }
                    this.InitializePorts(this.ComponentInstance.Component);
                    this.isBeingAssignedToComponent = false;
                }
            }
        }

        private void ComponentInstance_IsBeingDeleted(object sender)
        {
            if (sender is SimComponentInstance compInstance)
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
                            child.Component.PropertyChanged += this.SubComps_PropertyChanged;
                            child.Component.IsBeingDeleted += this.Component_IsBeingDeleted;

                            if (this.Ports.Any(port => port.ComponentInstance != null && port.ComponentInstance.Component == child.Component))
                            {
                                return;
                            }
                            if (child.Component.InstanceType == SimInstanceType.InPort || child.Component.InstanceType == SimInstanceType.OutPort)
                            {
                                //Look for existing empty ports which can be associated with the new component
                                SimNetworkPort newPort = this.FindAvailableEmptyPortForCompInstance(child.Component);
                                if (newPort == null)
                                {
                                    newPort = this.AddPortForComponent(child.Component);
                                    var portComponentInstance = new SimComponentInstance(newPort);
                                    child.Component.Instances.Add(portComponentInstance);

                                    this.Ports.Add(newPort);

                                }
                                else
                                {
                                    var portComponentInstance = new SimComponentInstance(newPort);
                                    child.Component.Instances.Add(portComponentInstance);
                                }
                            }

                        }
                    };
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is SimChildComponentEntry child && child.Component != null)
                        {
                            child.Component.PropertyChanged -= this.SubComps_PropertyChanged;
                            child.Component.IsBeingDeleted -= this.Component_IsBeingDeleted;
                        }
                    };
                    break;
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
            if (comp.InstanceType == SimInstanceType.InPort)
            {
                type = PortType.Input;
            }
            if (comp.InstanceType == SimInstanceType.OutPort)
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

            if (comp.InstanceType == SimInstanceType.InPort)
            {
                type = PortType.Input;
            }
            if (comp.InstanceType == SimInstanceType.OutPort)
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
                if (sender is SimComponent comp && (comp.InstanceType == SimInstanceType.InPort || comp.InstanceType == SimInstanceType.OutPort))
                {
                    //Look for existing empty ports which can be associated with the new component
                    SimNetworkPort newPort = this.FindAvailableEmptyPortForCompInstance(comp);
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
                };
            };
        }

        /// <summary>
        /// Whenever a new Componnet is assigned, new ports are created for each subcomponent of the component which has a port type (InPort or OutPort)
        /// </summary>
        private void InitializePorts(SimComponent component)
        {
            var components = component.Components.ToList();

            foreach (var child in components.Where(c => c.Component.InstanceType == SimInstanceType.OutPort || c.Component.InstanceType == SimInstanceType.InPort))
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

        #endregion

        internal override void RestoreReferences() { }
    }
}
