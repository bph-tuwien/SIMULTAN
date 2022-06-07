using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Excel
{
    [Obsolete("Will be replaced in future with SimObjectNew")]
    public abstract class HierarchicalContainer
    {

        public long ID_primary { get; private set; }
        public int ID_secondary { get; private set; }

        protected object content;
        public virtual object Content { get { return this.content; } }

        public virtual string ContentValue { get { return content.ToString(); } }

        public string Label { get; protected set; }

        public virtual string TypeName { get { return this.content.GetType().ToString(); } }

        public virtual List<HierarchicalContainer> Children
        {
            get
            {
                return null;
            }
        }

        public bool IsLeaf { get { return this.Children == null; } }

        public HierarchicalContainer(long _id_primary, int _id_secondary, object _content, string _label)
        {
            this.ID_primary = _id_primary;
            this.ID_secondary = _id_secondary;
            this.content = _content;
            this.Label = _label;
        }

        public override bool Equals(object obj)
        {
            HierarchicalContainer hc = obj as HierarchicalContainer;
            if (hc == null)
                return false;
            else
                return (this.ID_primary.Equals(hc.ID_primary) && this.ID_secondary.Equals(hc.ID_secondary));
        }

        public override int GetHashCode()
        {
            return this.ID_primary.GetHashCode() ^ this.ID_secondary.GetHashCode();
        }

        public static bool operator ==(HierarchicalContainer _hc1, HierarchicalContainer _hc2)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(_hc1, _hc2))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)_hc1 == null) || ((object)_hc2 == null))
            {
                return false;
            }

            // Return true if the fields match:
            return _hc1.ID_primary == _hc2.ID_primary && _hc1.ID_secondary == _hc2.ID_secondary;
        }

        public static bool operator !=(HierarchicalContainer _hc1, HierarchicalContainer _hc2)
        {
            return !(_hc1 == _hc2);
        }
    }

    /// <summary>
    /// Wrapper for the System.Double type
    /// </summary>
    public class DoubleContainer : HierarchicalContainer
    {
        public override string ContentValue
        {
            get
            {
                System.IFormatProvider nr_formatter = new System.Globalization.NumberFormatInfo();
                return ((double)this.content).ToString("F4", nr_formatter);
            }
        }
        public DoubleContainer(long _id_primary, int _id_secondary, double _content, string _label)
            : base(_id_primary, _id_secondary, _content, _label)
        { }
    }

    /// <summary>
    /// Wrapper for the System.Windows.Media.Media3D.Point3D type
    /// </summary>
    public class Point3DContainer : HierarchicalContainer
    {
        public override string ContentValue { get { return null; } }

        public override List<HierarchicalContainer> Children
        {
            get
            {
                return new List<HierarchicalContainer>
                {
                    new DoubleContainer(this.ID_primary, this.ID_secondary * 100 + 1, ((Point3D)this.content).X, "x"),
                    new DoubleContainer(this.ID_primary, this.ID_secondary * 100 + 2, ((Point3D)this.content).Y, "y"),
                    new DoubleContainer(this.ID_primary, this.ID_secondary * 100 + 3, ((Point3D)this.content).Z, "z")
                };
            }
        }

        public Point3DContainer(long _id_primary, int _id_secondary, Point3D _content)
            : base(_id_primary, _id_secondary, _content, "Point3D")
        { }

    }

}
