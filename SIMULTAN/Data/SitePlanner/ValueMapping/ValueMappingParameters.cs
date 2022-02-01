using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// States whether the index stored in the parameter is interpreted as a row or column index
    /// </summary>
    public enum ComponentIndexUsage
    {
        /// <summary>
        /// The parameter in the component that parameterizes a building will be read as a row index
        /// </summary>
        Row,
        /// <summary>
        /// The parameter in the component that parameterizes a building will be read as a column index
        /// </summary>
        Column
    }

    /// <summary>
    /// Parameters for ValueMapping
    /// </summary>
    public class ValueMappingParameters : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// States whether the index stored in the parameter is interpreted as a row or column index
        /// </summary>
        public ComponentIndexUsage ComponentIndexUsage
        {
            get => componentIndexUsage;
            set
            {
                if (value != componentIndexUsage)
                {
                    componentIndexUsage = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ComponentIndexUsage)));
                }
            }
        }
        private ComponentIndexUsage componentIndexUsage = ComponentIndexUsage.Row;

        /// <summary>
        /// MultiValueBigTable used for value mapping
        /// </summary>
        public SimMultiValueBigTable ValueTable
        {
            get => valueTable;
            set
            {
                if (value != valueTable)
                {
                    valueTable = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueTable)));
                }
            }
        }
        private SimMultiValueBigTable valueTable = null;

        /// <summary>
        /// Value to color map
        /// </summary>
        public IValueToColorMap ValueToColorMap
        {
            get => RegisteredColorMaps[valueToColorMapIndex];
            set
            {
                var newIndex = RegisteredColorMaps.IndexOf(value);
                if (newIndex == -1)
                    throw new ArgumentException("ValueToColorMap must be contained in RegisteredColorMaps");
                if (valueToColorMapIndex != newIndex)
                {
                    valueToColorMapIndex = newIndex;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValueToColorMap)));
                }
            }
        }
        private int valueToColorMapIndex = 0;

        /// <summary>
        /// Contains all registered color maps. See constructor.
        /// </summary>
        public List<IValueToColorMap> RegisteredColorMaps { get; private set; }

        /// <summary>
        /// Filters values before they go to the ValueToColorMap
        /// </summary>
        public IValuePrefilter ValuePreFilter
        {
            get => RegisteredValuePrefilters[valuePreFilterIndex];
            set
            {
                var newIndex = RegisteredValuePrefilters.IndexOf(value);
                if (newIndex == -1)
                    throw new ArgumentException("ValuePreFilter must be contained in RegisteredValuePrefilters");
                if (valuePreFilterIndex != newIndex)
                {
                    valuePreFilterIndex = newIndex;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ValuePreFilter)));
                }
            }
        }
        private int valuePreFilterIndex = 0;

        /// <summary>
        /// Contains all registered prefilters. See constructor.
        /// </summary>
        public List<IValuePrefilter> RegisteredValuePrefilters { get; private set; }

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="valueTable">Associated MultiValueBigTable</param>
        public ValueMappingParameters(SimMultiValueBigTable valueTable)
        {
            this.ValueTable = valueTable;

            var markerParameters = new MarkerColorMapParameters();
            this.RegisteredColorMaps = new List<IValueToColorMap>
            {
                new MultiThresholdColorMap(markerParameters),
                new MultiLinearGradientColorMap(markerParameters)
				// add other color maps here
			};
            this.ValueToColorMap = this.RegisteredColorMaps.First();
            this.RegisteredValuePrefilters = new List<IValuePrefilter>
            {
                new TimelinePrefilter(new TimelinePrefilterParameters()),
                new MinimumPrefilter(),
                new MaximumPrefilter(),
                new AveragePrefilter()
				// add other prefilters here
			};
            this.ValuePreFilter = RegisteredValuePrefilters.First();
        }
    }
}
