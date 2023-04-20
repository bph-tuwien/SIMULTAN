using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    public class ParameterFactory : INotifyPropertyChanged
    {
        public const string PARAMETER_RECORD_FILE_NAME = "ParameterRecord";

        #region FILEDS, PROPERTIES

        public ObservableCollection<SimBaseParameter> ParameterRecord { get; private set; }

        #endregion

        public ParameterFactory()
        {
            this.ParameterRecord = new ObservableCollection<SimBaseParameter>();
        }


        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion


        #region METHODS: Record Management

        public SimBaseParameter CopyRecord(SimBaseParameter _record)
        {
            if (_record == null) return null;
            if (!this.ParameterRecord.Contains(_record)) return _record;

            SimBaseParameter copy = _record.Clone();
            if (!copy.NameTaxonomyEntry.HasTaxonomyEntry())
            {
                copy.NameTaxonomyEntry = new Taxonomy.SimTaxonomyEntryOrString(copy.NameTaxonomyEntry.Name + " (copy)");
            }
            if (copy != null)
                this.ParameterRecord.Add(copy);

            return copy;
        }

        #endregion
    }
}
