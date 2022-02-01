using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Model class for a user component list.
    /// Stores a user defined list of top-level components which are managed by some other factory. 
    /// </summary>
    public class SimUserComponentList : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// The name of the list
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }
        private string name;

        /// <summary>
        /// The <see cref="SimUserComponentListCollection"/> the user component list belongs to.
        /// </summary>
        public SimUserComponentListCollection Factory
        {
            get
            {
                return factory;
            }
            internal set
            {
                if (factory != value)
                {
                    var oldfactory = factory;
                    factory = value;
                    if (factory != null && oldfactory == null)
                    {
                        AttachEvents();
                    }
                    else if (factory == null && oldfactory != null)
                    {
                        DetachEvents();
                    }
                }
            }
        }
        private SimUserComponentListCollection factory;

        private void DetachEvents()
        {
            foreach (var compref in RootComponents.RootComponents)
            {
                compref.DetachEvents();
            }
        }

        private void AttachEvents()
        {
            for (int i = 0; i < RootComponents.RootComponents.Count; i++)
            {
                if (RootComponents[i].Factory != null)
                {
                    RootComponents.RootComponents[i].AttachEvents();
                }
                else
                {
                    RootComponents.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// A collection of root components that are a part of the list
        /// </summary>
        public SimUserRootComponentCollection RootComponents
        {
            get;
            private set;
        }

        #endregion

        #region .CTOR

        /// <summary>
        /// Constructs a UserComponentList
        /// </summary>
        /// <param name="name">The name of the list</param>
        /// <param name="rootComponents">The root component of the list</param>
        public SimUserComponentList(string name, ObservableCollection<SimComponent> rootComponents)
        {
            if (rootComponents == null)
            {
                throw new ArgumentNullException(nameof(rootComponents));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            RootComponents = new SimUserRootComponentCollection(this, rootComponents);
        }

        /// <summary>
        /// Constructs a UserComponentList
        /// </summary>
        /// <param name="name">The name of the list</param>
        public SimUserComponentList(string name) : this(name, new ObservableCollection<SimComponent>())
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds (serializes) this UserComponentList to the export StringBuilder.
        /// </summary>
        /// <param name="sb">The StringBuilder to export to.</param>
        public void AddToExport(StringBuilder sb)
        {
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            sb.AppendLine(ParamStructTypes.USER_LIST);

            sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            sb.AppendLine(this.GetType().ToString());

            sb.AppendLine(((int)UserComponentListSaveCode.NAME).ToString());
            sb.AppendLine(this.Name);

            sb.AppendLine(((int)UserComponentListSaveCode.NR_OF_ROOT_COMPONENTS).ToString());
            sb.AppendLine(this.RootComponents.Count.ToString());


            foreach (var comp in RootComponents)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
                sb.AppendLine(comp.Id.ToString());
            }

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_CONTINUE);

        }

        /// <summary>
        /// Returns the content id of the UserComponentList
        /// </summary>
        /// <param name="ucl">The UserComponentList</param>
        /// <returns>The content id of the given UserComponentList</returns>
        public static string GetContentId(SimUserComponentList ucl)
        {
            return "UserComponentList_" + ucl.Name;
        }
        #endregion

        #region Events

        /// <summary>
        /// The PropertyChanged event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

    }
}
