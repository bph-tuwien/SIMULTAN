using SIMULTAN;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Media;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores a color that can be derived from a parent object
    /// </summary>
    public class DerivedColor : INotifyPropertyChanged
    {
        /// <summary>
        /// Returns the color (either local or derived)
        /// </summary>
        public Color Color
        {
            get
            {
                if (!fromParent || parent == null)
                    return color;
                return ((DerivedColor)this.parentColorProperty.GetValue(this.parent)).Color;
            }
            set
            {
                this.color = value;
                this.fromParent = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }
        private Color color;
        /// <summary>
        /// Returns the local color
        /// </summary>
        public Color LocalColor { get { return color; } }

        /// <summary>
        /// Returns the parent color
        /// </summary>
        public Color ParentColor
        {
            get
            {
                if (this.parent != null && this.parentColorProperty != null)
                    return ((DerivedColor)this.parentColorProperty.GetValue(this.parent)).Color;
                else
                    return LocalColor;
            }
        }

        /// <summary>
        /// Returns the parent object (or null when no parent is set)
        /// </summary>
        public object Parent { get { return parent; } }
        private object parent;
        /// <summary>
        /// Returns the name of the parent object's property (or null when no parent is set)
        /// </summary>
        public String PropertyName { get { return parentColorProperty?.Name; } }
        private PropertyInfo parentColorProperty;

        /// <summary>
        /// Returns whether the local color or the parent color should be used
        /// </summary>
        public bool IsFromParent
        {
            get { return fromParent; }
            set
            {
                fromParent = value;
                if (this.parent == null || this.parentColorProperty == null)
                    fromParent = false;

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }
        private bool fromParent;

        /// <summary>
        /// Initializes a new instance of the DerivedColor class
        /// </summary>
        /// <remarks>Using this constructor it is impossible to derive colors later</remarks>
        /// <param name="color">A color</param>
        public DerivedColor(Color color)
        {
            this.color = color;
            this.parentColorProperty = null;
            this.parent = null;
            this.fromParent = false;
        }
        /// <summary>
        /// Initializes a new instance of the DerivedColor class
        /// </summary>
        /// <param name="color">A color</param>
        /// <param name="parent">The parent object</param>
        /// <param name="property">The name of the parent color property (of type DerivedColor)</param>
        public DerivedColor(Color color, object parent, string property)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));
            if (property == null)
                throw new ArgumentNullException(nameof(property));

            this.color = color;
            this.parent = parent;
            this.parentColorProperty = parent.GetType().GetProperty(property);
            this.fromParent = true;

            if (parentColorProperty == null)
                throw new ArgumentException(String.Format("Unable to find property \"{0}\" on type {1}", property, parent.GetType().FullName));

            if (parentColorProperty.PropertyType != typeof(DerivedColor) && !parentColorProperty.PropertyType.IsSubclassOf(typeof(DerivedColor)))
                throw new ArgumentException(String.Format("Property \"{0}\" on type {1} does not derive from {2}", 
                    property, parent.GetType().FullName, typeof(DerivedColor).Name));

            if (parent is INotifyPropertyChanged)
                (parent as INotifyPropertyChanged).PropertyChanged += ParentPropertyChanged;

            ConnectParentEvents();
        }
        /// <summary>
        /// Initializes a new instance of the DerivedColor class
        /// </summary>
        /// <remarks>Using this constructor it is impossible to derive colors later</remarks>
        /// <param name="source">Another DerivedColor to copy the data from</param>
        public DerivedColor(DerivedColor source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            this.color = source.color;
            this.parent = source.parent;
            this.parentColorProperty = source.parentColorProperty;
            this.fromParent = source.fromParent;

            if (parent != null && parent is INotifyPropertyChanged)
                (parent as INotifyPropertyChanged).PropertyChanged += ParentPropertyChanged;

            if (parentColorProperty != null)
                ConnectParentEvents();
        }

        private void ParentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == parentColorProperty.Name)
            {
                ConnectParentEvents();
                if (fromParent)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }
        private void ParentPropertyPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
        }

        private void ConnectParentEvents()
        {
            DerivedColor parentColor = parentColorProperty.GetValue(parent) as DerivedColor;
            if (parentColor != null)
                parentColor.PropertyChanged += ParentPropertyPropertyChanged;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
