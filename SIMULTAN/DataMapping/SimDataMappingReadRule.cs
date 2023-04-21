using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Mapping rule for copying data from Excel to a <see cref="SimBaseParameter"/>
    /// 
    /// When a 1x1 range is copied, the value is stored in the parameters value. When larger ranges are read,
    /// a <see cref="SimMultiValueBigTable"/> is constructed and the <see cref="SimBaseParameter.ValueSource"/> is attached to the table.
    /// </summary>
    public class SimDataMappingReadRule : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// The target parameter into which the results are stored
        /// </summary>
        public SimBaseParameter Parameter 
        {
            get { return parameter; }
            set
            {
                if (parameter != value)
                {
                    parameter = value;
                    NotifyPropertyChange();
                }
            }
        }
        private SimBaseParameter parameter;

        /// <summary>
        /// The name of the worksheet in the Excel file
        /// </summary>
        public string SheetName
        {
            get { return sheetName; }
            set
            {
                if (sheetName != value)
                {
                    sheetName = value;
                    NotifyPropertyChange();
                }
            }
        }
        private string sheetName;

        /// <summary>
        /// The range of rows/columns that should be copied
        /// </summary>
        public RowColumnRange Range 
        {
            get { return range; }
            set
            {
                if (range != value)
                {
                    range = value;
                    NotifyPropertyChange();
                }
            }
        }
        private RowColumnRange range;

        /// <summary>
        /// The tool to which this rule belongs
        /// </summary>
        public SimDataMappingTool Tool
        {
            get { return tool; }
            internal set
            {
                if (tool != value)
                {
                    tool = value;
                    NotifyPropertyChange();
                }
            }
        }
        private SimDataMappingTool tool;

        #endregion

        #region INotifyPropertyChanged

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="property">The name of the property</param>
        private void NotifyPropertyChange([CallerMemberName] string property = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion
    
        /// <summary>
        /// Creates a deep copy of the rule
        /// </summary>
        /// <returns>A copy of the rule</returns>
        public SimDataMappingReadRule Clone()
        {
            var rule = new SimDataMappingReadRule()
            {
                Parameter = this.Parameter,
                SheetName = this.SheetName,
                Range = this.Range,
            };

            return rule;
        }
    }
}
