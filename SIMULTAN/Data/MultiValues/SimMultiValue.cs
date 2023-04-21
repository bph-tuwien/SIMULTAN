using System;

namespace SIMULTAN.Data.MultiValues
{
    #region ENUMS

    /// <summary>
    /// Describes the type of a value field
    /// </summary>
    public enum SimMultiValueType : int
    {
        /// <summary>
        /// Holds a function filed with multiple function graphs.
        /// </summary>
        Function = 0,
        /// <summary>
        /// A 3D value field that allows for interpolation
        /// </summary>
        Field3D = 1,
        /// <summary>
        /// A large 2D value field without interpolation support (but can perform matrix operations)
        /// </summary>
        BigTable = 2,
    }

    #endregion

    /// <summary>
    /// Base class for all value fields. 
    /// Have a look at SimMultiValueField3D, SimMultiValueFunction and SimMultiValueBigTable for implementations
    /// </summary>
    public abstract class SimMultiValue : SimNamedObject<SimMultiValueCollection>
    {
        #region STATIC
        internal static readonly string NEWLINE_PLACEHOLDER = "[NewLine]";
        #endregion

        #region Properties
        /// <summary>
        /// Gets or sets the type of the value field.
        /// This value is redundant since types have a 1-1 mapping to C# types
        /// </summary>
        public abstract SimMultiValueType MVType { get; }

        /// <summary>
        /// Returns true when the value field supports interpolation between values.
        /// Interpolation happens (when supported) when fractal values are passed as coordinates.
        /// </summary>
        public abstract bool CanInterpolate { get; set; }

        /// <summary>
        /// Gets or sets the unit description for the X-axis. Just a text, does not influence any calculations.
        /// </summary>
        public string UnitX
        {
            get { return this.unitX; }
            set
            {
                if (this.unitX != value)
                {
                    this.unitX = value;
                    this.NotifyPropertyChanged(nameof(UnitX));
                    this.NotifyChanged();
                }
            }
        }
        private string unitX;

        /// <summary>
        /// Gets or sets the unit description for the Y-axis. Just a text, does not influence any calculations.
        /// </summary>
        public string UnitY
        {
            get { return this.unitY; }
            set
            {
                if (this.unitY != value)
                {
                    this.unitY = value;
                    this.NotifyPropertyChanged(nameof(UnitY));
                    this.NotifyChanged();
                }
            }
        }
        private string unitY;

        /// <summary>
        /// Gets or sets the unit description for the Z-axis. Just a text, does not influence any calculations.
        /// </summary>
        public string UnitZ
        {
            get { return this.unitZ; }
            set
            {
                if (this.unitZ != value)
                {
                    this.unitZ = value;
                    this.NotifyPropertyChanged(nameof(UnitZ));
                    this.NotifyChanged();
                }
            }
        }
        private string unitZ;

        #endregion

        #region Events

        /// <summary>
        /// Invoked when the MultiValue gets removed from the containing collection
        /// </summary>
        public event EventHandler Deleting;
        /// <summary>
        /// Invokes the <see cref="Deleting"/> event
        /// </summary>
        internal void NotifyDeleting()
        {
            this.Deleting?.Invoke(this, EventArgs.Empty);
        }

        #endregion



        /// <summary>
        /// Initializes a new instance of the SimMultiValue class
        /// </summary>
        /// <param name="name">Name of the SimMultiValue</param>
        /// <param name="unitX">Unit description of the X-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitY">Unit description of the Y-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitZ">Unit description of the Z-axis. Just a text, does not influence any calculations.</param>
        protected SimMultiValue(string name, string unitX, string unitY, string unitZ)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.Name = name;

            this.unitX = (string.IsNullOrEmpty(unitX)) ? "-" : unitX;
            this.unitY = (string.IsNullOrEmpty(unitY)) ? "-" : unitY;
            this.unitZ = (string.IsNullOrEmpty(unitZ)) ? "-" : unitZ;
        }

        /// <summary>
        /// Initializes a new instance of the SimMultiValue class
        /// </summary>
        /// <param name="localId">the local id of the SimMultiValue (used when loading fields from a, e.g., a file)</param>
        /// <param name="name">Name of the SimMultiValue</param>
        /// <param name="unitX">Unit description of the X-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitY">Unit description of the Y-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitZ">Unit description of the Z-axis. Just a text, does not influence any calculations.</param>
        protected SimMultiValue(long localId, string name, string unitX, string unitY, string unitZ)
            : base(new SimId(localId))
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.Name = name;

            this.unitX = (string.IsNullOrEmpty(unitX)) ? "-" : unitX;
            this.unitY = (string.IsNullOrEmpty(unitY)) ? "-" : unitY;
            this.unitZ = (string.IsNullOrEmpty(unitZ)) ? "-" : unitZ;
        }

        /// <summary>
        /// Creates a deep copy of a SimMultiValue
        /// </summary>
        /// <param name="original">The original SimMultiValue</param>
        protected SimMultiValue(SimMultiValue original)
        {
            if (original == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(original)));

            this.Name = original.Name;

            this.unitX = original.UnitX;
            this.unitY = original.UnitY;
            this.unitZ = original.UnitZ;
        }



        /// <summary>
        /// Creates a deep copy of this instance
        /// </summary>
        public abstract SimMultiValue Clone();


        #region METHODS: External Pointer

        /// <summary>
        /// Creates a new (default) pointer to this SimMultiValue
        /// </summary>
        /// <returns>A pointer to this field with a default pointer address</returns>
        public abstract SimMultiValueParameterSource CreateNewPointer();


        /// <summary>
        /// Creates a new pointer which copies data from an existing pointer.
        /// In general, this is only possible when the source pointer points to the same type of ValueField.
        /// </summary>
        /// <returns>A pointer to this field with parameters taken from the source pointer</returns>
        public abstract SimMultiValueParameterSource CreateNewPointer(SimMultiValueParameterSource source);

        #endregion

    }
}
