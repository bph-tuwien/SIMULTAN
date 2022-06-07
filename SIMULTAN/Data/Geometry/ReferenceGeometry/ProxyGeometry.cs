using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Displays a arbitrary triangle mesh that is attached to a vertex.
    /// The proxy acts similar to the vertex during selection and all other operations
    /// 
    /// Note, that after changing the geometry (<see cref="Positions"/>, <see cref="Normals"/> or <see cref="Indices"/>)
    /// the <see cref="BaseGeometry.NotifyGeometryChanged"/> has to be called manually
    /// </summary>
    public class ProxyGeometry : BaseGeometry
    {
        /// <summary>
        /// The vertex to which the proxy geometry is attached
        /// </summary>
        public Vertex Vertex { get { return vertex; } }
        private Vertex vertex;

        /// <summary>
        /// A list of triangles vertices
        /// </summary>
        public List<Point3D> Positions { get; set; }
        /// <summary>
        /// A list of vertex normals
        /// </summary>
        public List<Vector3D> Normals { get; set; }
        /// <summary>
        /// Index list for the triangle mesh (always three indices form a face)
        /// </summary>
        public List<int> Indices { get; set; }

        /// <summary>
        /// Gets or sets the size (scaling) of the proxy geometry
        /// </summary>
        public Vector3D Size
        {
            get { return size; }
            set
            {
                size = value;
                OnPropertyChanged(nameof(Size));
                OnTransformationChanged();
            }
        }
        private Vector3D size;
        /// <summary>
        /// Gets or sets the local rotation of the proxy geometry
        /// </summary>
        public Quaternion Rotation
        {
            get { return rotation; }
            set
            {
                rotation = value;
                OnPropertyChanged(nameof(Rotation));
                OnTransformationChanged();
            }
        }
        private Quaternion rotation;

        /// <summary>
        /// Euler angles of rotation
        /// </summary>
        public Vector3D EulerAngles
        {
            get { return Rotation.ToEulerAngles(); }
            set
            {
                Rotation = QuaternionExtensions.CreateFromYawPitchRoll(value);
            }
        }
        /// <summary>
        /// Returns the full model matrix. Includes Size and the vertex position.
        /// </summary>
        public Matrix3D Transformation
        {
            get
            {
                var mat = Matrix3D.Identity;
                mat.Scale(size);
                mat.Rotate(rotation);
                mat.Translate((Vector3D)vertex.Position);
                return mat;
            }
        }

        /// <summary>
        /// Initializes a new instance of the ProxyGeometry class
        /// </summary>
        /// <param name="layer">The layer on which the proxy geometry should exist</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="vertex">The vertex to which the proxy is attached</param>
        public ProxyGeometry(Layer layer, string nameFormat, Vertex vertex)
            : this(layer != null ? layer.Model.GetFreeId() : ulong.MaxValue, layer, nameFormat, vertex) { }

        /// <summary>
        /// Initializes a new instance of the ProxyGeometry class
        /// </summary>
        /// <param name="id">The unique id of this geometry</param>
        /// <param name="layer">The layer on which the proxy geometry should exist</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="vertex">The vertex to which the proxy is attached</param>
        /// <param name="positions">List of vertex positions, default null</param>
        /// <param name="normals">List of vertex normals, default null</param>
        /// <param name="indices">List of indices, default null</param>
        public ProxyGeometry(ulong id, Layer layer, string nameFormat, Vertex vertex, List<Point3D> positions = null, List<Vector3D> normals = null, List<int> indices = null)
            : base(id, layer)
        {
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));
            if (nameFormat == null)
                throw new ArgumentNullException(nameof(nameFormat));

            this.Name = string.Format(nameFormat, id);

            this.vertex = vertex;

            this.Positions = positions;
            this.Normals = normals;
            this.Indices = indices;

            this.vertex.GeometryChanged += Vertex_GeometryChanged;
            this.size = new Vector3D(1, 1, 1);
            this.rotation = Quaternion.Identity;

            ModelGeometry.ProxyGeometries.Add(this);

            MakeConsistent(false, true);

            this.Color = new DerivedColor(Colors.White, vertex, nameof(Vertex.Color));
        }

        private void Vertex_GeometryChanged(object sender)
        {
            NotifyGeometryChanged();
        }

        /// <inheritdoc/>
        public override void AddToModel()
        {
            if (!ModelGeometry.ProxyGeometries.Contains(this))
                ModelGeometry.ProxyGeometries.Add(this);
            else
                throw new Exception("Geometry is already present in the model");

            if (ModelGeometry.HandleConsistency)
            {
                this.vertex.ProxyGeometries.Add(this);
            }

            if (this.Layer != null && !this.Layer.Elements.Contains(this))
                this.Layer.Elements.Add(this);
        }
        /// <inheritdoc/>
        public override void MakeConsistent(bool notifyGeometryChanged, bool hasTopologyChanged)
        {
            if (hasTopologyChanged)
                vertex.ProxyGeometries.Add(this);

            OnGeometryChanged(notifyGeometryChanged);
        }
        /// <inheritdoc/>
        public override bool RemoveFromModel()
        {
            if (ModelGeometry.HandleConsistency)
            {
                this.vertex.ProxyGeometries.Remove(this);
                this.Layer.Elements.Remove(this);
            }

            return ModelGeometry.ProxyGeometries.Remove(this);
        }

        /// <inheritdoc />
        protected override void AssignToLayerColor(Layer layer)
        {
            //Do nothing. Proxies use the vertex color as parent
        }

        private void OnTransformationChanged()
        {
            OnPropertyChanged(nameof(Transformation));
            NotifyGeometryChanged();
            vertex.NotifyGeometryChanged();
        }
    }
}
