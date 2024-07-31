using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Contains information about how to map a <see cref="SimMultiValueBigTable"/> onto other data.
    /// Used, for example, by the SitePlanner or by the GeometryViewer.
    /// 
    /// Value Mappings take a row or column (depending on the <see cref="componentIndexUsage"/>) from the table.
    /// The vector of values is than reduced by applying a prefilter which returns a subset or statistical aggregate.
    /// The final color is computed by sending the result of the prefilter to a <see cref="ColorMap"/> which converts the data value
    /// to a color.
    /// </summary>
    public class SimValueMapping : SimNamedObject<SimValueMappingCollection>
    {
        #region Properties

        /// <summary>
        /// States whether the index stored in the parameter is interpreted as a row or column index
        /// </summary>
        public SimComponentIndexUsage ComponentIndexUsage
        {
            get => componentIndexUsage;
            set
            {
                if (value != componentIndexUsage)
                {
                    componentIndexUsage = value;
                    NotifyPropertyChanged(nameof(ComponentIndexUsage));
                    this.NotifyValueMappingChanged();
                }
            }
        }
        private SimComponentIndexUsage componentIndexUsage = SimComponentIndexUsage.Row;

        /// <summary>
        /// MultiValueBigTable used for value mapping
        /// </summary>
        public SimMultiValueBigTable Table
        {
            get => table;
            set
            {
                if (value != table)
                {
                    if (table != null)
                    {
                        //Detach Events
                        table.Resized -= this.Table_Resized;
                        table.ValueChanged -= this.Table_ValueChanged;
                    }

                    table = value;
                    NotifyPropertyChanged(nameof(Table));

                    if (table != null)
                    {
                        //Attach events
                        table.Resized += this.Table_Resized;
                        table.ValueChanged += this.Table_ValueChanged;
                    }
                }
            }
        }
        private SimMultiValueBigTable table = null;

        /// <summary>
        /// The Colormap which maps data values to colors
        /// </summary>
        public SimColorMap ColorMap
        {
            get { return colorMap; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                if (colorMap != value)
                {
                    if (this.colorMap != null)
                        this.colorMap.Owner = null;

                    this.colorMap = value;

                    if (this.colorMap != null)
                        this.colorMap.Owner = this;

                    NotifyPropertyChanged(nameof(ColorMap));
                    NotifyValueMappingChanged();
                }
            }
        }
        private SimColorMap colorMap;
        /// <summary>
        /// The prefilter which reduces the selected column/row vector to a single value
        /// </summary>
        public SimPrefilter Prefilter
        {
            get { return this.prefilter; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                if (value != this.prefilter)
                {
                    if (this.prefilter != null)
                        this.prefilter.Owner = null;

                    this.prefilter = value;

                    if (this.prefilter != null)
                        this.prefilter.Owner = this;

                    NotifyPropertyChanged(nameof(Prefilter));
                    NotifyValueMappingChanged();
                }
            }
        }
        private SimPrefilter prefilter;

        #endregion

        #region Events

        /// <summary>
        /// EventHandler for the ValueMappingChanged event
        /// </summary>
        /// <param name="sender">The sender</param>
        public delegate void ValueMappingChangedEventHandler(object sender);

        /// <summary>
        /// Invoked when a parameter related to value mapping has changed
        /// </summary>
        public event ValueMappingChangedEventHandler ValueMappingChanged;

        /// <summary>
        /// Invokes the <see cref="ValueMappingChanged"/> event
        /// </summary>
        internal void NotifyValueMappingChanged()
        {
            ValueMappingChanged?.Invoke(this);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SimValueMapping"/> class
        /// </summary>
        /// <param name="name">The name of the mapping</param>
        /// <param name="table">The table from which values should be taken</param>
        /// <param name="prefilter">The prefilter</param>
        /// <param name="colorMap">The colormap</param>
        public SimValueMapping(string name, SimMultiValueBigTable table, SimPrefilter prefilter, SimColorMap colorMap)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            if (prefilter == null)
                throw new ArgumentNullException(nameof(prefilter));
            if (colorMap == null)
                throw new ArgumentNullException(nameof(colorMap));

            this.Name = name;
            this.Table = table;
            this.Prefilter = prefilter;
            this.ColorMap = colorMap;
        }

        /// <summary>
        /// Calculates the final color for a given row/column
        /// </summary>
        /// <param name="objectIndex">The row/column index of the object to map. 
        /// The index is either a row index when <see cref="ComponentIndexUsage"/> equals <see cref="SimComponentIndexUsage.Row"/> or a 
        /// column index (in case of <see cref="SimComponentIndexUsage.Column" />.
        /// </param>
        /// <param name="timelineIndex">The currently selected timeline index. May be used by the <see cref="Prefilter"/></param>
        /// <returns>The final color of the mapping</returns>
        public SimColor ApplyMapping(int objectIndex, int timelineIndex)
        {
            IEnumerable<double> values = this.componentIndexUsage == SimComponentIndexUsage.Row ?
                table.GetColumn(objectIndex).Select(x => x.ConvertToDoubleIfNumeric()) :
                table.GetRow(objectIndex).Select(x => x.ConvertToDoubleIfNumeric());

            double filteredValue = Prefilter.Filter(values, timelineIndex).First();

            var color = ColorMap.Map(filteredValue);

            return color;
        }

        #region Event Handler

        private void Table_Resized(object sender, SimMultiValueBigTable.ResizeEventArgs e)
        {
            this.NotifyValueMappingChanged();
        }

        private void Table_ValueChanged(object sender, SimMultiValueBigTable.ValueChangedEventArgs args)
        {
            this.NotifyValueMappingChanged();
        }

        #endregion
    }
}
