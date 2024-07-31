using SIMULTAN;
using SIMULTAN.Data.SimMath;
using System;
using System.ComponentModel;
using System.Reflection;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores a color that can be derived from a parent object
    /// </summary>
    public class DerivedColor : INotifyPropertyChanged
    {
        /// <summary>
        /// Returns the local color
        /// </summary>
        public SimColor LocalColor { get { return localColor; } }
        private SimColor localColor;

        /// <summary>
        /// Returns the parent object (or null when no parent is set)
        /// </summary>
        public object Parent
        {
            get { return parent; }
            internal set
            {
                if (parent != value)
                {
                    parent = value;
                    NotifyParentColorChanged();
                }
            }
        }
        private object parent;

        /// <summary>
        /// Returns whether the local color or the parent color should be used
        /// </summary>
        public bool IsFromParent
        {
            get { return fromParent; }
            set
            {
                fromParent = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }
        private bool fromParent;

        /// <summary>
        /// Returns the parent color
        /// </summary>
        public SimColor ParentColor
        {
            get
            {
                switch (this.parent)
                {
                    case Layer l:
                        return l.Color.Color;
                    case BaseGeometry bg:
                        return bg.Color.Color;
                    default:
                        return LocalColor;
                }
            }
        }

        /// <summary>
        /// Returns the color (either local or derived)
        /// </summary>
        public SimColor Color
        {
            get
            {
                if (!fromParent || parent == null)
                    return localColor;
                return ParentColor;
            }
            set
            {
                this.localColor = value;
                this.fromParent = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }

        /// <summary>
        /// Initializes a new instance of the DerivedColor class
        /// </summary>
        /// <param name="isFromParent">Specifies whether the local color or the parent color should be used</param>
        /// <param name="color">A color</param>
        public DerivedColor(SimColor color, bool isFromParent = false)
        {
            this.localColor = color;
            this.parent = null;
            this.fromParent = isFromParent;
        }
        /// <summary>
        /// Initializes a new instance of the DerivedColor class. Does not copy the parent!
        /// </summary>
        /// <param name="source">Another DerivedColor to copy the data from</param>
        public DerivedColor(DerivedColor source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            this.localColor = source.localColor;
            this.fromParent = source.fromParent;
            this.parent = null;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyParentColorChanged()
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
        }
    }
}
