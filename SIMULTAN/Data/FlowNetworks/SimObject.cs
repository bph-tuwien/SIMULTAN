using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.FlowNetworks
{
    /// <summary>
    /// The base class for all SIMULTAN entities (eventually).
    /// </summary>
    [Obsolete]
    public abstract class SimObject : INotifyPropertyChanged, IReference
    {
        #region PROPERTIES: INotifyPropertyChanged

        /// <summary>
        /// Emitted when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Emits the PropertyChanged event with the given property name. Use nameof().
        /// </summary>
        /// <param name="_propName">the name of the property</param>
        protected void NotifyPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion  

        #region Properties: ID

        /// <summary>
        /// The id of the SIMULTAN object. The local part of the id is unique 
        /// within an open file or project. When projects are merged, duplicate local ids are changed!
        /// </summary>
        public SimObjectId ID
        {
            get { return this.id; }
            internal set
            {
                if (this.id != value)
                {
                    this.id = value;
                    this.NotifyPropertyChanged(nameof(ID));
                }
            }
        }
        /// <summary>
        /// The field corresponding to property ID.
        /// </summary>
        protected SimObjectId id;

        /// <summary>
        /// Returns only the local id of the instance.
        /// </summary>
        public long LocalID { get { return this.id.LocalId; } }
        /// <summary>
        /// Returns only the global location of the instance.
        /// </summary>
        public Guid GlobalID { get { return this.id.GlobalId; } }


        #endregion

        #region Properties: Name, Description

        /// <summary>
        /// The name of the displayable product definition. It can be any character string.
        /// </summary>
        public virtual string Name
        {
            get { return this.name; }
            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    this.NotifyPropertyChanged(nameof(Name));
                }
            }
        }
        /// <summary>
        /// The field corresponding to property Name.
        /// </summary>
        protected string name;

        /// <summary>
        /// The description of the displayable product definition. It can be any character string.
        /// </summary>
        public string Description
        {
            get { return this.description; }
            set
            {
                if (this.description != value)
                {
                    this.description = value;
                    this.NotifyPropertyChanged(nameof(Description));
                }
            }
        }
        /// <summary>
        /// The field corresponding to property Description.
        /// </summary>
        protected string description;

        #endregion
    }
}
