using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Describes Row/Column headers in a BigTable
    /// </summary>
    public class SimMultiValueBigTableHeader : INotifyPropertyChanged
    {
        /// <summary>
        /// Specifies which dimensions of the BigTable was affected
        /// </summary>
        public enum AxisEnum
        {
            /// <summary>
            /// The rows are affected
            /// </summary>
            Rows = 0,
            /// <summary>
            /// The columns are affected
            /// </summary>
            Columns = 1,
            /// <summary>
            /// It's unclear which direction is affected
            /// </summary>
            Undefined = -1,
        }

        private string name, unit;


        internal SimMultiValueBigTable Table { get; set; }
        internal AxisEnum Axis { get; set; } = AxisEnum.Undefined;
        internal int Index { get; set; } = -1;

        /// <summary>
        /// Name/Descriptive text of the header
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                NotifyPropertyChanged(nameof(Name));
                if (Table != null)
                {
                    Table.NotifyHeaderValueChanged(Index, Axis);
                }
            }
        }

        /// <summary>
        /// Unit text of the header
        /// </summary>
        public string Unit
        {
            get { return unit; }
            set
            {
                unit = value;
                NotifyPropertyChanged(nameof(Unit));
                if (Table != null)
                {
                    Table.NotifyHeaderValueChanged(Index, Axis);
                }

            }
        }


        /// <summary>
        /// Initializes a new instance of the MultiValueBigTableHeader class
        /// </summary>
        /// <param name="name">Name text of the header</param>
        /// <param name="unit">Unit text of the header</param>
        public SimMultiValueBigTableHeader(string name, string unit)
        {
            if (name == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(name)));
            if (unit == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(unit)));

            this.name = name;
            this.unit = unit;
        }



        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Invokes the PropertyChanged event
        /// </summary>
        /// <param name="prop">The property name</param>
        protected void NotifyPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        /// <summary>
        /// Creates a copy of this instance
        /// </summary>
        /// <returns>A copy of this instance</returns>
        public SimMultiValueBigTableHeader Clone()
        {
            return new SimMultiValueBigTableHeader(this.name, this.unit);
        }
    }
}
