﻿using SIMULTAN.Serializer.DXF;
using System;
using System.Text;

namespace SIMULTAN.Data.MultiValues
{
    #region ENUMS

    /// <summary>
    /// Describes the type of a value field
    /// </summary>
    public enum MultiValueType
    {
        /// <summary>
        /// Holds a function filed with multiple function graphs.
        /// </summary>
        FUNCTION_ND,
        /// <summary>
        /// A 3D value field that allows for interpolation
        /// </summary>
        FIELD_3D,
        /// <summary>
        /// A large 2D value field without interpolation support (but can perform matrix operations)
        /// </summary>
        TABLE
    }

    #endregion

    /// <summary>
    /// Base class for all value fields. 
    /// Have a look at SimMultiValueField3D, SimMultiValueFunction and SimMultiValueBigTable for implementations
    /// </summary>
    public abstract class SimMultiValue : SimObjectNew<SimMultiValueCollection>
    {
        #region STATIC

        internal static readonly string NEWLINE_PLACEHOLDER = "[NewLine]";

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the type of the value field.
        /// This value is redundant since types have a 1-1 mapping to C# types
        /// </summary>
        public abstract MultiValueType MVType { get; }

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


        #region .CTOR

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

        #endregion

        #region .CTOR for PARSING

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

        #endregion

        #region .CTOR for COPYING

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

        #endregion

        #region METHODS: To and From String

        /// <summary>
        /// Adds the data of this class ot the DXF exporter
        /// </summary>
        /// <param name="sb">The string builder that contains the DXF file content</param>
        public virtual void AddToExport(ref StringBuilder sb)

        {
            if (sb == null) return;
            string tmp = null;

            // common
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_LOCATION).ToString());
            sb.AppendLine(this.Id.GlobalId.ToString());

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            sb.AppendLine(this.Id.LocalId.ToString());

            sb.AppendLine(((int)MultiValueSaveCode.MVType).ToString());
            sb.AppendLine(((int)this.MVType).ToString());

            sb.AppendLine(((int)MultiValueSaveCode.MVName).ToString());
            sb.AppendLine(this.Name);

            sb.AppendLine(((int)MultiValueSaveCode.MVCanInterpolate).ToString());
            tmp = (this.CanInterpolate) ? "1" : "0";
            sb.AppendLine(tmp);

            // common info
            sb.AppendLine(((int)MultiValueSaveCode.MVUnitX).ToString());
            sb.AppendLine(this.UnitX);

            sb.AppendLine(((int)MultiValueSaveCode.MVUnitY).ToString());
            sb.AppendLine(this.UnitY);

            sb.AppendLine(((int)MultiValueSaveCode.MVUnitZ).ToString());
            sb.AppendLine(this.UnitZ);

        }

        #endregion

        #region METHODS: External Pointer

        /// <summary>
        /// Creates a new (default) pointer to this SimMultiValue
        /// </summary>
        /// <returns>A pointer to this field with a default pointer address</returns>
        public abstract SimMultiValuePointer CreateNewPointer();
        /// <summary>
        /// Creates a new pointer which copies data from an existing pointer.
        /// In general, this is only possible when the source pointer points to the same type of ValueField.
        /// </summary>
        /// <returns>A pointer to this field with parameters taken from the source pointer</returns>
        public abstract SimMultiValuePointer CreateNewPointer(SimMultiValuePointer source);

        #endregion
    }
}