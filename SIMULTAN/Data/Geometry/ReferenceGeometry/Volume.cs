using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Represents a volume
    /// </summary>
    [DebuggerDisplay("Volume ID={Id}")]
    public class Volume : BaseGeometry
    {
        /// <summary>
        /// Returns a list of all boundary PFaces
        /// </summary>
        public ObservableCollection<PFace> Faces { get; private set; }

        /// <summary>
        /// Stores whether the faces describe a closed and consistently oriented volume. If False, several methods might
        /// return wrong values (e.g. Volume calculation returns NaN).
        /// </summary>
        public bool IsConsistentOriented
        {
            get { return isConsistentOriented; }
            private set
            {
                if (isConsistentOriented != value)
                {
                    isConsistentOriented = value;
                    NotifyPropertyChanged(nameof(IsConsistentOriented));
                }
            }
        }
        private bool isConsistentOriented;


        /// <summary>
        /// Initializes a new instance of the Volume class
        /// </summary>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="faces">A list of boundary faces</param>
        public Volume(Layer layer, string nameFormat, IEnumerable<Face> faces)
            : this(layer != null ? layer.Model.GetFreeId() : ulong.MaxValue, layer, nameFormat, faces) { }
        /// <summary>
        /// Initializes a new instance of the Volume class
        /// </summary>
        /// <param name="id">The unique identifier for this object</param>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="faces">A list of boundary faces</param>
        public Volume(ulong id, Layer layer, string nameFormat, IEnumerable<Face> faces)
            : base(id, layer)
        {
            if (nameFormat == null)
                throw new ArgumentNullException(nameof(nameFormat));
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));

            this.Name = string.Format(nameFormat, id);

            this.isConsistentOriented = false;

            Faces = new ObservableCollection<PFace>();
            foreach (var f in faces)
                Faces.Add(new PFace(f, this, GeometricOrientation.Forward));
            Faces.CollectionChanged += Faces_CollectionChanged;
            MakeConsistent(false, true);

            ModelGeometry.Volumes.Add(this);
        }

        private void Faces_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                e.OldItems.ForEach(x => ((PFace)x).Face.GeometryChanged -= Face_GeometryChanged);
                e.OldItems.ForEach(x => ((PFace)x).Face.TopologyChanged -= Face_TopologyChanged);
            }

            if (ModelGeometry.HandleConsistency)
            {
                if (e.OldItems != null)
                {
                    e.OldItems.ForEach(x => ((PFace)x).Face.PFaces.Remove((PFace)x));
                }

                if (e.NewItems != null)
                {
                    e.NewItems.ForEach(x => ((PFace)x).Face.PFaces.Add((PFace)x));
                }

                //Make orientation consistent
                IsConsistentOriented = VolumeAlgorithms.FindConsistentOrientation(this);
            }

            NotifyTopologyChanged();
        }

        /// <inheritdoc />
        public override void MakeConsistent(bool notifyGeometryChanged, bool hasTopologyChanged)
        {
            //Check if faces are holes in other faces of the volume
            if (hasTopologyChanged)
            {
                //Make faces consistent
                foreach (var f in this.Faces)
                    f.MakeConsistent();

                //Attach events
                foreach (var f in this.Faces)
                {
                    f.Face.GeometryChanged -= Face_GeometryChanged;
                    f.Face.GeometryChanged += Face_GeometryChanged;
                    f.Face.TopologyChanged -= Face_TopologyChanged;
                    f.Face.TopologyChanged += Face_TopologyChanged;
                }
            }

            if (this.GeometryHasChanged || hasTopologyChanged)
            {
                //Make orientation consistent
                IsConsistentOriented = VolumeAlgorithms.FindConsistentOrientation(this);
            }

            OnGeometryChanged(notifyGeometryChanged);
            OnTopologyChanged();
        }

        private void Face_GeometryChanged(object sender)
        {
            NotifyGeometryChanged();
        }

        private void Face_TopologyChanged(object sender)
        {
            NotifyTopologyChanged();
        }

        /// <inheritdoc/>
        public override bool RemoveFromModel()
        {
            bool result = this.ModelGeometry.Volumes.Remove(this);

            this.Faces.ForEach(x => x.Face.PFaces.Remove(x));

            return result;
        }
        /// <inheritdoc/>

        public override void AddToModel()
        {
            if (!this.ModelGeometry.Volumes.Contains(this))
            {
                this.Faces.ForEach(x => x.Face.PFaces.Add(x));

                this.ModelGeometry.Volumes.Add(this);
            }
            else
                throw new Exception("Geometry is already present in the model");

            if (this.Layer != null && !this.Layer.Elements.Contains(this))
                this.Layer.Elements.Add(this);
        }

        /// <summary>
        /// Adds a face to the volume
        /// </summary>
        /// <param name="face">The face to add</param>
        public PFace AddFace(Face face)
        {
            var existingPF = Faces.FirstOrDefault(x => x.Face == face);
            if (existingPF != null)
                return existingPF;

            var pf = new PFace(face, this, GeometricOrientation.Forward);
            Faces.Add(pf);

            if (ModelGeometry.HandleConsistency)
            {
                MakeConsistent(true, true);
            }

            return pf;
        }

        /// <summary>
        /// Removes a face from the volume if found.
        /// </summary>
        /// <param name="face">The face to remove.</param>
        /// <returns>True if the face was removed, false if it could not be found.</returns>
        public bool RemoveFace(Face face)
        {
            var existingPF = Faces.FirstOrDefault(x => x.Face == face);
            if (existingPF == null) // face could not be found
                return false;

            Faces.Remove(existingPF);

            if (ModelGeometry.HandleConsistency)
            {
                MakeConsistent(true, true);
            }

            return true;
        }
    }
}
