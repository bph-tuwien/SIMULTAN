using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// DXF Entity for <see cref="GeoMap"/>
    /// </summary>
    internal class DXFGeoReference : DXFEntity
    {
        private string dxf_MapImage_FullName;
        private int dxf_nr_GeoReferences;
        private (double, double, double, double, double) dxf_tmp_gr;
        private List<(double, double, double, double, double)> dxf_GeoReferences;

        /// <inheritdoc />
        public DXFGeoReference()
        {
            this.dxf_MapImage_FullName = string.Empty;
            this.dxf_nr_GeoReferences = 0;
            this.dxf_tmp_gr = ValueTuple.Create(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            this.dxf_GeoReferences = new List<(double, double, double, double, double)>();
        }

        /// <inheritdoc />
        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)GeoMapSaveCode.MAP_PATH:
                    this.dxf_MapImage_FullName = this.Decoder.FValue;
                    break;
                case (int)GeoMapSaveCode.GEOREFS:
                    this.dxf_nr_GeoReferences = this.Decoder.IntValue();
                    this.SaveGeoReference(this.dxf_tmp_gr);
                    break;
                case (int)GeoMapSaveCode.IMAGEPOS_X:
                    this.dxf_tmp_gr.Item1 = this.Decoder.DoubleValue();
                    this.SaveGeoReference(this.dxf_tmp_gr);
                    break;
                case (int)GeoMapSaveCode.IMAGEPOS_Y:
                    this.dxf_tmp_gr.Item2 = this.Decoder.DoubleValue();
                    this.SaveGeoReference(this.dxf_tmp_gr);
                    break;
                case (int)GeoMapSaveCode.LONGITUDE:
                    this.dxf_tmp_gr.Item3 = this.Decoder.DoubleValue();
                    this.SaveGeoReference(this.dxf_tmp_gr);
                    break;
                case (int)GeoMapSaveCode.LATITUDE:
                    this.dxf_tmp_gr.Item4 = this.Decoder.DoubleValue();
                    this.SaveGeoReference(this.dxf_tmp_gr);
                    break;
                case (int)GeoMapSaveCode.HEIGHT:
                    this.dxf_tmp_gr.Item5 = this.Decoder.DoubleValue();
                    this.SaveGeoReference(this.dxf_tmp_gr);
                    break;
                default:
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        private void SaveGeoReference((double, double, double, double, double) refdata)
        {
            if (double.IsNaN(refdata.Item1) ||
                double.IsNaN(refdata.Item2) ||
                double.IsNaN(refdata.Item3) ||
                double.IsNaN(refdata.Item4) ||
                double.IsNaN(refdata.Item5))
                return;
            if (this.dxf_nr_GeoReferences > this.dxf_GeoReferences.Count)
            {
                this.dxf_GeoReferences.Add(refdata);
                this.dxf_tmp_gr = (double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
            }
        }

        /// <inheritdoc />
        internal override void OnLoaded()
        {
            base.OnLoaded();

            if (this.Decoder is DXFDecoderGeoReferences)
            {
                List<ImageGeoReference> refs = this.dxf_GeoReferences.Select(
                    x => new ImageGeoReference(
                            new System.Windows.Point(x.Item1, x.Item2),
                            new System.Windows.Media.Media3D.Point3D(x.Item3, x.Item4, x.Item5))).ToList();

                var decoder = (DXFDecoderGeoReferences)this.Decoder;
                decoder.Map.MapImageRes = ResourceReference.FromDXF(this.dxf_MapImage_FullName, decoder.AssetManager);
                foreach (var igr in refs)
                    decoder.Map.GeoReferences.Add(igr);
            }
        }
    }
}
