using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Manages a number of MultiValues which belong to the same source location.
    /// Ids inside the factory are unique.
    /// MultiValues that are added to the factory have to have an empty Id unless loading mode is enabled first.
    /// </summary>
    public class SimMultiValueCollection : SimManagedCollection<SimMultiValue>
    {
        #region ObservableCollection Overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, SimMultiValue item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            SetValues(item);
            base.InsertItem(index, item);
            NotifyChanged();
        }
        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];
            UnsetValues(oldItem);
            base.RemoveItem(index);
            NotifyChanged();
        }
        /// <inheritdoc />
        protected override void ClearItems()
        {
            foreach (var item in this)
                UnsetValues(item);
            base.ClearItems();
            NotifyChanged();
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SimMultiValue item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var oldItem = this[index];
            UnsetValues(oldItem);
            SetValues(item);
            base.SetItem(index, item);
            NotifyChanged();
        }


        private void SetValues(SimMultiValue item)
        {
            if (item.Factory != null)
                throw new ArgumentException("item already belongs to a factory");

            if (item.Id != SimId.Empty) //Used pre-stored id (only possible during loading)
            {
                if (isLoading)
                {
                    item.Id = new SimId(CalledFromLocation, item.Id.LocalId);
                    ProjectData.IdGenerator.Reserve(item, item.Id);
                }
                else
                    throw new NotSupportedException("Existing Ids may only be used during a loading operation");
            }
            else
                item.Id = ProjectData.IdGenerator.NextId(item, CalledFromLocation);

            item.Factory = this;
        }

        private void UnsetValues(SimMultiValue item)
        {
            ProjectData.IdGenerator.Remove(item);
            item.Id = SimId.Empty;
            item.Factory = null;
        }

        #endregion

        #region Loading

        private bool isLoading = false;

        /// <summary>
        /// Sets the factory in loading mode which allows to add MultiValues with a pre-defined Id
        /// </summary>
        public void StartLoading()
        {
            isLoading = true;
        }
        /// <summary>
        /// Ends the loading operation and reenables Id checking
        /// </summary>
        public void EndLoading()
        {
            isLoading = false;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the SimMultiValueCollection class
        /// </summary>
        public SimMultiValueCollection(ProjectData owner) : base(owner) { }


        #region METHODS: DXF Export

        public StringBuilder ExportRecord(bool _finalize = false)
        {
            return ExportRecord(this, _finalize);
        }

        private static StringBuilder ExportRecord(IEnumerable<SimMultiValue> multiValues, bool _finalize = false)
        {
            if (multiValues == null)
                throw new ArgumentNullException(nameof(multiValues));

            StringBuilder sb = new StringBuilder();

            //Version
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.VERSION_SECTION);

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            sb.AppendLine(ParamStructTypes.FILE_VERSION);                            // FILE_VERSION

            sb.AppendLine(((int)ParamStructCommonSaveCode.COORDS_X).ToString());       // 10
            sb.AppendLine(DXFDecoder.CurrentFileFormatVersion.ToString());

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            //Content
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            foreach (var record in multiValues)
            {
                record.AddToExport(ref sb);
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

        /// <summary>
        /// Serializes only the given values.
        /// </summary>
        /// <param name="_values">the values to serialize</param>
        /// <param name="_finalize">if true, finish off with EOF</param>
        /// <returns>the serialization</returns>
        public static StringBuilder ExportSome(IEnumerable<SimMultiValue> _values, bool _finalize)
        {
            return ExportRecord(_values, _finalize);
        }

        /// <summary>
        /// Serializes all values used by public components.
        /// </summary>
        /// <param name="components">he component factory using this value factory as value source</param>
        /// <param name="_finalize">true = set the EOF marker, false = do not set the EOF marker</param>
        /// <returns>the serialized content</returns>
        [Obsolete("Should be merged with ExportRecord (only difference is the choice of MVs)")]
        public StringBuilder ExportPublicRecord(SimComponentCollection components, bool _finalize = true)
        {
            StringBuilder sb = new StringBuilder();

            //Version
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.VERSION_SECTION);

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            sb.AppendLine(ParamStructTypes.FILE_VERSION);                            // FILE_VERSION

            sb.AppendLine(((int)ParamStructCommonSaveCode.COORDS_X).ToString());       // 10
            sb.AppendLine(DXFDecoder.CurrentFileFormatVersion.ToString());

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_END);

            //Content

            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString());
            sb.AppendLine(ParamStructTypes.SECTION_START);
            sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_NAME).ToString());
            sb.AppendLine(ParamStructTypes.ENTITY_SECTION);

            if (components != null)
            {
                HashSet<SimMultiValue> exportedMVs = new HashSet<SimMultiValue>();

                ComponentWalker.ForeachComponent(components, x =>
                {
                    if (x.Visibility == SimComponentVisibility.AlwaysVisible)
                    {
                        foreach (var param in x.Parameters)
                        {
                            if (param.MultiValuePointer != null && !exportedMVs.Contains(param.MultiValuePointer.ValueField))
                            {
                                param.MultiValuePointer.ValueField.AddToExport(ref sb);
                                exportedMVs.Add(param.MultiValuePointer.ValueField);
                            }
                        }
                    }
                });
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

        /// <summary>
        /// Checks which values are used in the given component factory and removes all unused ones.
        /// If the factory is null this results in removing all values.
        /// </summary>
        /// <param name="components">the component factory using the value fields</param>
        /// <param name="_excluded_from_removal">multi values that should not be removed</param>
        public void RemoveUnused(SimComponentCollection components, IEnumerable<SimMultiValue> _excluded_from_removal)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            HashSet<SimMultiValue> usedMultiValues = new HashSet<SimMultiValue>();
            ComponentWalker.ForeachComponent(components, x =>
            {
                foreach (var param in x.Parameters)
                    if (param.MultiValuePointer != null && !usedMultiValues.Contains(param.MultiValuePointer.ValueField))
                        usedMultiValues.Add(param.MultiValuePointer.ValueField);
            });

            if (_excluded_from_removal != null)
                foreach (var excl in _excluded_from_removal)
                    usedMultiValues.Add(excl);

            for (int i = this.Count - 1; i >= 0; i--)
            {
                var mv = this.ElementAt(i);
                if (!usedMultiValues.Contains(mv))
                {
                    this.RemoveAt(i);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnCalledFromLocationChanged()
        {
            base.OnCalledFromLocationChanged();

            foreach (var mv in this)
                mv.Id = new SimId(this.CalledFromLocation != null ? this.CalledFromLocation.GlobalID : Guid.Empty, mv.Id.LocalId);
        }

        #endregion

        #region METHODS: Merge Records

        /// <summary>
        /// Merges another factory into this factory. 
        /// The source factory gets emptied and all items are transfered into this factory while assigning new Ids.
        /// </summary>
        /// <returns>
        /// Returns a dictionary which maps old SimMultiValue Ids (from source) to new Ids (in this factory).
        /// Key = old Id, Value = new Id.
        /// </returns>
        public Dictionary<long, long> Merge(SimMultiValueCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            List<(SimMultiValue value, long id)> oldIds = source.Select(x => (x, x.LocalID)).ToList();
            source.Clear();

            Dictionary<long, long> id_change_record = new Dictionary<long, long>();

            foreach ((var mv, var oldId) in oldIds)
            {
                this.Add(mv);
                id_change_record.Add(oldId, mv.LocalID);
            }

            return id_change_record;
        }

        #endregion

        #region METHODS: Getter

        /// <summary>
        /// Returns the SimMultiValue with a given Id
        /// </summary>
        /// <param name="_location">The global Id</param>
        /// <param name="_id">The local Id</param>
        /// <returns>
        /// When _location equals the current CalledFromLocation or equals Guid.Empty, the SimMultiValue with the given local Id is returned.
        /// Returns null when either the global Id doesn't match or when no SimMultiValue with the local Id exists.
        /// </returns>
        public SimMultiValue GetByID(Guid _location, long _id)
        {
            if (this.CalledFromLocation != default && _location != Guid.Empty && this.CalledFromLocation.GlobalID != _location)
                return null;
            return this.FirstOrDefault(x => x.Id.LocalId == _id);
        }

        #endregion
    }
}
