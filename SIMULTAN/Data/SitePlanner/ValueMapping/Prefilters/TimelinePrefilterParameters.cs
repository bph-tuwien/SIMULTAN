using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Prefilter parameters for the timeline prefilter
    /// </summary>
    public class TimelinePrefilterParameters : ValuePrefilterParameters
    {
        /// <summary>
        /// Current value
        /// </summary>
        public int Current
        {
            get => current;
            set
            {
                if (current != value)
                {
                    this.current = value;
                    NotifyPropertyChanged(nameof(Current));
                    NotifyValuePrefilterParametersChanged();
                }
            }
        }
        private int current = 0;

        /// <summary>
        /// Displayed headers
        /// </summary>
        public ObservableCollection<string> Headers
        {
            get => headers;
            set
            {
                if (headers != value)
                {
                    headers = value;
                    NotifyPropertyChanged(nameof(Headers));
                }
            }
        }
        private ObservableCollection<string> headers = new ObservableCollection<string>() { string.Empty, string.Empty };

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public TimelinePrefilterParameters()
        {
        }

        /// <inheritdoc />
        public override string Serialize()
        {
            object[] parameters = { Current };
            return string.Join(";", parameters);
        }

        /// <inheritdoc />
        public override void Deserialize(string obj)
        {
            string[] parameters = obj.Split(';');
            this.current = int.Parse(parameters[0]);
        }
    }
}
