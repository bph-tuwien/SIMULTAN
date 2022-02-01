using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    public abstract class Asset : INotifyPropertyChanged
    {
        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region PROPERTIES: General

        protected int resourceKey;
        public int ResourceKey
        {
            get { return this.resourceKey; }
            protected set
            {
                this.resourceKey = value;
                this.RegisterPropertyChanged(nameof(ResourceKey));
            }
        }

        protected string contained_object_id;
        public string ContainedObjectId
        {
            get { return this.contained_object_id; }
            set
            {
                if (this.contained_object_id != value)
                {
                    var old_value = this.contained_object_id;
                    this.contained_object_id = value;
                    this.RegisterPropertyChanged(nameof(ContainedObjectId));
                }
            }
        }

        public ResourceEntry Resource
        {
            get
            {
                return manager.GetResource(this.resourceKey);
            }
        }

        #endregion

        #region PROPERTIES: referencing components

        public ObservableCollection<long> ReferencingComponentIds { get; }

        public int AddReferencing(long _id)
        {
            if (!this.ReferencingComponentIds.Contains(_id))
                this.ReferencingComponentIds.Add(_id);

            return this.ReferencingComponentIds.Count;
        }

        public int RemoveReferencing(long _id)
        {
            if (this.ReferencingComponentIds.Contains(_id))
                this.ReferencingComponentIds.Remove(_id);

            return this.ReferencingComponentIds.Count;
        }

        public bool IsBeingReferencedBy(long _id)
        {
            return this.ReferencingComponentIds.Contains(_id);
        }

        #endregion

        #region CLASS MEMBERS

        protected AssetManager manager;

        #endregion

        #region .CTOR

        internal Asset(AssetManager _manger, long _caller_id, int _path_code_to_asset, string _id)
        {
            this.manager = _manger;
            this.resourceKey = _path_code_to_asset;
            this.contained_object_id = _id;
            this.ReferencingComponentIds = new ObservableCollection<long> { _caller_id };
        }

        #endregion

        #region PARSING .CTOR

        internal Asset(AssetManager _manger, List<long> _caller_ids, int _path_code_to_asset, string _id)
        {
            this.manager = _manger;
            this.resourceKey = _path_code_to_asset;
            this.contained_object_id = _id;
            this.ReferencingComponentIds = new ObservableCollection<long>();
            if (_caller_ids != null)
            {
                foreach (var id in _caller_ids)
                {
                    this.ReferencingComponentIds.Add(id);
                }
            }
        }

        #endregion

        #region METHODS: Access to Content

        protected object cached_content;

        [Obsolete]
        public object GetAssetContent(bool _look_in_cache_first)
        {
            if (_look_in_cache_first && this.cached_content != null)
                return this.cached_content;

            this.cached_content = this.OpenAssetContent();
            return this.cached_content;
        }

        [Obsolete]
        public virtual object OpenAssetContent()
        {
            return null;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            string file = "-";
            if (this.manager != null)
                file = this.manager.GetPath(this.resourceKey);


            string info = "Asset in \"" + file + "\": " + this.contained_object_id;
            return info;
        }

        public virtual void AddToExport(ref StringBuilder _sb)
        {
            // the ENTITY declaration in the sub-types!!!

            _sb.AppendLine(((int)AssetSaveCode.PATH_CODE).ToString());
            _sb.AppendLine(this.resourceKey.ToString());

            _sb.AppendLine(((int)AssetSaveCode.CONTENT_ID).ToString());
            _sb.AppendLine(this.contained_object_id);

            _sb.AppendLine(((int)AssetSaveCode.REFERENCE_COL).ToString());
            _sb.AppendLine(this.ReferencingComponentIds.Count.ToString());
            if (this.ReferencingComponentIds.Count > 0)
            {
                foreach (long id in this.ReferencingComponentIds)
                {
                    _sb.AppendLine(((int)AssetSaveCode.REFERENCE).ToString());
                    _sb.AppendLine(id.ToString());
                }
            }
        }

        #endregion
    }
}
