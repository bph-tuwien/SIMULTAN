using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    public class ImageRecordManager : INotifyPropertyChanged
    {
        #region PROPERTIES: INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RegisterPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region PROPERIES / FIELDS
        private long nr_images;
        private List<ImageRecord> images_on_record;

        public IReadOnlyList<ImageRecord> ImagesForDisplay
        {
            get
            {
                List<ImageRecord> list = new List<ImageRecord> { ImageRecord.EMPTY };
                list.AddRange(this.images_on_record);
                return list.AsReadOnly();
            }
        }

        #endregion

        public ImageRecordManager()
        {
            this.nr_images = 0;
            this.images_on_record = new List<ImageRecord>();
        }

        public void AddRecord(string _file)
        {
            ImageRecord ir = new ImageRecord((++this.nr_images), _file);
            this.images_on_record.Add(ir);
        }

        public void RemoveRecord(ImageRecord _ir)
        {
            if (_ir == null) return;
            bool success = this.images_on_record.Remove(_ir);
            if (success)
            {
                if (_ir.ID == this.nr_images)
                {
                    // recalculate
                    this.nr_images = 0;
                    foreach (ImageRecord i in this.images_on_record)
                    {
                        this.nr_images = Math.Max(this.nr_images, i.ID);
                    }
                }
            }
        }

        public void ClearRecord()
        {
            this.images_on_record.Clear();
        }

        public void SaveRecordToFile(string _filename)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream stream = File.Create(_filename))
            {
                bf.Serialize(stream, this.images_on_record);
            }
        }

        public void LoadRecordsFromFile(string _filename)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream stream = File.Open(_filename, FileMode.Open))
            {
                this.images_on_record = (List<ImageRecord>)bf.Deserialize(stream);
            }

            this.nr_images = 0;
            foreach (ImageRecord ir in this.images_on_record)
            {
                this.nr_images = Math.Max(this.nr_images, ir.ID);
            }
        }

    }
}
