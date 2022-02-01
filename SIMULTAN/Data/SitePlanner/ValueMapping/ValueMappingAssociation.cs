using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Represents a single value mapping, i.e. one color map, one prefilter a well as one MultiValueBigTable (+ additional properties)
    /// </summary>
    public class ValueMappingAssociation : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Name of this value mapping association
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }
        private string name;

        /// <summary>
        /// Parameters for this value mapping association (color map, prefilter, table, ...)
        /// </summary>
        public ValueMappingParameters Parameters { get; private set; }

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="name">Name of the value mapping association</param>
        /// <param name="parameters">Value mapping parameters</param>
        public ValueMappingAssociation(string name, ValueMappingParameters parameters)
        {
            this.Name = name;
            this.Parameters = parameters;
        }
    }
}
