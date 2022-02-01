using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace SIMULTAN.Data.Components
{
    [Serializable()]
    public class ImageRecord
    {
        #region STATIC

        internal static ImageRecord EMPTY;
        static ImageRecord()
        {
            EMPTY = new ImageRecord();
        }

        #endregion

        #region PROPERTIES : Logic

        private SerializableBitmapImageWrapper symbol_behind;
        public BitmapImage Symbol
        {
            get { return this.symbol_behind; }
            private set
            {
                this.symbol_behind = value;
            }
        }
        public long ID { get; private set; }

        public string Name { get; private set; }

        #endregion

        private ImageRecord()
        {
            this.ID = -1;
            this.Name = "no symbol";
            this.Symbol = null;
        }

        internal ImageRecord(long _id, string _file)
        {
            this.ID = _id;

            int ind_lastBS = _file.LastIndexOfAny(new char[] { '\\', '/' });
            if (ind_lastBS > -1 && ind_lastBS < _file.Length - 1)
                this.Name = _file.Substring(ind_lastBS + 1);
            else
                this.Name = "image" + this.ID.ToString();

            try
            {
                // load the symbol from the file
                this.Symbol = new BitmapImage(new Uri(_file, UriKind.RelativeOrAbsolute));
            }
            catch //(Exception ex)
            {
                this.Symbol = null;
                // MessageBox.Show(ex.Message, "Image Building Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // MessageBox.Show(ex.StackTrace, "Image Building Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
