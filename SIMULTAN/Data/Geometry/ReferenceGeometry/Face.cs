using SIMULTAN;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Represents a planar surface
    /// </summary>
    [DebuggerDisplay("Face ID={Id}, Normal={Normal}, Name={Name}")]
    public class Face : BaseGeometry
    {
        /// <summary>
        /// Returns the boundary of the polygon
        /// </summary>
        public EdgeLoop Boundary { get; private set; }
        /// <summary>
        /// Returns a list of holes in the polygon
        /// </summary>
        public ObservableCollection<EdgeLoop> Holes { get; private set; }
        /// <summary>
        /// Stores the orientation of the face. Forward means CCW, Backward means CW
        /// </summary>
        public GeometricOrientation Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;
                NotifyPropertyChanged(nameof(Orientation));

                if (ModelGeometry.HandleConsistency)
                    UpdateNormal(); //GeometryChanged will be emitted by UpdateNormal -> Normal.set
                else
                    NotifyGeometryChanged();
            }
        }
        private GeometricOrientation orientation;

        /// <summary>
        /// Returns the face normal
        /// </summary>
        public SimVector3D Normal
        {
            get { return normal; }
            private set
            {
                normal = value;
                NotifyPropertyChanged(nameof(Normal));

                NotifyGeometryChanged();
            }
        }
        private SimVector3D normal;

        /// <summary>
        /// Returns all PFaces associated with this Face
        /// </summary>
        public List<PFace> PFaces { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Face class
        /// </summary>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="boundary">The boundary loop</param>
        /// <param name="orientation">Orientation of the face</param>
        /// <param name="holes">A list of hole loops</param>
        public Face(Layer layer, string nameFormat, EdgeLoop boundary, GeometricOrientation orientation = GeometricOrientation.Forward, 
            IEnumerable<EdgeLoop> holes = null)
            : this(layer != null ? layer.Model.GetFreeId() : ulong.MaxValue, layer, nameFormat, boundary, orientation, holes) { }
        /// <summary>
        /// Initializes a new instance of the Face class
        /// </summary>
        /// <param name="id">The unique identifier for this object</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="boundary">The boundary loop</param>
        /// <param name="orientation">Orientation of the face</param>
        /// <param name="holes">A list of hole loops</param>
        public Face(ulong id, Layer layer, string nameFormat, EdgeLoop boundary,
            GeometricOrientation orientation = GeometricOrientation.Forward, IEnumerable<EdgeLoop> holes = null)
            : base(id, layer)
        {
            if (boundary == null)
                throw new ArgumentNullException(nameof(boundary));
            if (nameFormat == null)
                throw new ArgumentNullException(nameof(nameFormat));

            this.Name = string.Format(nameFormat, id);
            this.Boundary = boundary;
            this.Orientation = orientation;

            if (holes == null)
                this.Holes = new ObservableCollection<EdgeLoop>();
            else
                this.Holes = new ObservableCollection<EdgeLoop>(holes);
            this.Holes.CollectionChanged += Holes_CollectionChanged;

            this.PFaces = new List<PFace>();

            MakeConsistent(false, true);

            this.ModelGeometry.Faces.Add(this);
        }

        private void Holes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ModelGeometry.HandleConsistency)
            {
                if (e.OldItems != null)
                {
                    e.OldItems.ForEach(x => ((EdgeLoop)x).Faces.Remove(this));
                    e.OldItems.ForEach(x => ((EdgeLoop)x).GeometryChanged -= Loop_GeometryChanged);
                }

                if (e.NewItems != null)
                {
                    e.NewItems.ForEach(x => ((EdgeLoop)x).Faces.Add(this));
                    e.NewItems.ForEach(x => ((EdgeLoop)x).GeometryChanged += Loop_GeometryChanged);
                }
            }

            NotifyTopologyChanged();
        }

        /// <inheritdoc />
        public override void MakeConsistent(bool notifyGeometryChanged, bool hasTopologyChanged)
        {
            if (hasTopologyChanged)
            {
                this.PFaces.Clear();

                this.Boundary.Faces.Add(this);

                this.Boundary.GeometryChanged -= Loop_GeometryChanged;
                this.Boundary.GeometryChanged += Loop_GeometryChanged;
                this.Boundary.TopologyChanged -= Loop_TopologyChanged;
                this.Boundary.TopologyChanged += Loop_TopologyChanged;

                foreach (var l in this.Holes.ToList())
                {
                    if (!ModelGeometry.EdgeLoops.Contains(l))
                        throw new Exception("Hole EdgeLoop not part of the GeometryModel. Did you delete a Loop without removing it from the face?");

                    l.GeometryChanged -= Loop_GeometryChanged;
                    l.TopologyChanged -= Loop_TopologyChanged;
                    l.Faces.Add(this);
                    l.TopologyChanged += Loop_TopologyChanged;
                    l.GeometryChanged += Loop_GeometryChanged;
                }
            }

            if (GeometryHasChanged || hasTopologyChanged)
                UpdateNormal();

            OnGeometryChanged(notifyGeometryChanged);
            OnTopologyChanged();
        }

        private void Loop_TopologyChanged(object sender)
        {
            if (ModelGeometry.HandleConsistency)
            {
                UpdateNormal();
            }

            NotifyTopologyChanged();
        }

        private void Loop_GeometryChanged(object sender)
        {
            if (ModelGeometry.HandleConsistency)
            {
                UpdateNormal();
                //UpdateNormal already calls OnGeometryChanged() do not call a second time
                //OnGeometryChanged();
            }
            else
                NotifyGeometryChanged();
        }

        /// <inheritdoc/>
        public override bool RemoveFromModel()
        {
            this.Boundary.Faces.Remove(this);
            this.Holes.ForEach(x => x.Faces.Remove(this));

            bool result = this.ModelGeometry.Faces.Remove(this);
            return result;
        }
        /// <inheritdoc/>
        public override void AddToModel()
        {
            if (!this.ModelGeometry.Faces.Contains(this))
            {
                this.Boundary.Faces.Add(this);
                this.Holes.ForEach(x => x.Faces.Add(this));

                this.ModelGeometry.Faces.Add(this);
            }
            else
                throw new Exception("Geometry is already present in the model");


            if (this.Layer != null && !this.Layer.Elements.Contains(this))
                this.Layer.Elements.Add(this);
        }


        private void UpdateNormal()
        {
            var n = EdgeLoopAlgorithms.NormalCCW(this.Boundary);
            if (Orientation == GeometricOrientation.Backward)
                n = -n;

            Normal = n;
        }
    }
}
