﻿using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    public class ParameterFactory : INotifyPropertyChanged
    {
        public const string PARAMETER_RECORD_FILE_NAME = "ParameterRecord";

        #region FILEDS, PROPERTIES

        public ObservableCollection<SimParameter> ParameterRecord { get; private set; }

        #endregion

        public ParameterFactory()
        {
            this.ParameterRecord = new ObservableCollection<SimParameter>();
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


        #region METHODS: Factory Methods

        // only call when parsing
        [Obsolete]
        internal SimParameter ReconstructParameter(long _id, string _name, string _unit, SimCategory _category, SimInfoFlow _propagation,
                                                double _value_min, double _value_max, double _value_current,
                                                SimMultiValuePointer valuePointer,
                                                DateTime _time_stamp, string _text_value, SimParameterOperations _ops,
                                                SimParameterInstancePropagation instancePropagationMode,
                                                bool isAutoGenerated)
        {

            SimParameter created = new SimParameter(_id, _name, _unit, _category, _propagation,
                                               _value_current, _value_min, _value_max,
                                               _text_value, valuePointer, _ops, instancePropagationMode, isAutoGenerated);

            // check if a valid value table was created
            if (created == null) return null;

            // create
            this.ParameterRecord.Add(created);

            // done
            return created;
        }

        #endregion

        #region METHODS: DXF Export

        /// <summary>
        /// Exports the Parameters in DFX format. If it gets "parametersToExport" s null, it exports the parameters in the ParameterFactory.ParameterRecord
        /// </summary>
        public StringBuilder ExportRecord(List<SimParameter> parametersToExport = null, bool _finalize = false)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            if (parametersToExport != null)
            {
                if (parametersToExport.Count > 0)
                {
                    foreach (SimParameter record in parametersToExport)
                    {
                        record.AddToExport(ref sb);
                    }
                }
            }
            else
            {
                if (this.ParameterRecord.Count > 0)
                {
                    foreach (SimParameter record in this.ParameterRecord)
                    {
                        record.AddToExport(ref sb);
                    }
                }
            }


            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            if (_finalize)
            {
                sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
                sb.AppendLine(ParamStructTypes.EOF);
            }

            return sb;
        }


        #endregion

        #region METHODS: Record Management

        [Obsolete("... and inefficient")]
        public bool DeleteRecord(long _record_id)
        {
            SimParameter found = this.ParameterRecord.FirstOrDefault(x => x.Id.LocalId == _record_id);
            if (found == null) return false;

            bool success = this.ParameterRecord.Remove(found);
            return success;
        }

        public void ClearRecord()
        {
            this.ParameterRecord.Clear();
        }

        public SimParameter CopyRecord(SimParameter _record)
        {
            if (_record == null) return null;
            if (!this.ParameterRecord.Contains(_record)) return _record;

            SimParameter copy = _record.Clone();
            copy.Name = copy.Name + " (copy)";
            if (copy != null)
                this.ParameterRecord.Add(copy);

            return copy;
        }

        public SimParameter CopyWithoutRecord(SimParameter _original)
        {
            if (_original == null) return null;
            SimParameter copy = _original.Clone();
            return copy;
        }

        #endregion
    }
}