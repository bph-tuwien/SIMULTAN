using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores information about offset faces
    /// </summary>
    public class OffsetModel
    {
        /// <summary>
        /// Stores the GeometryModel this model belongs to
        /// </summary>
        public GeometryModelData Model { get; private set; }

        /// <summary>
        /// Determines whether offset surfaces should be updated when the GeometryChanged event is invoked.
        /// Use this to disable offset generation during multi-step operations
        /// </summary>
        public bool HandleGeometryInvalidated
        {
            get { return handleGeometryInvalidated; }
            set
            {
                if (handleGeometryInvalidated != value)
                {
                    handleGeometryInvalidated = value;
                    if (handleGeometryInvalidated)
                    {
                        Generator.Update(GeometrySettings.Instance.CalculateOffsetSurfaces);
                    }
                }
            }
        }
        private bool handleGeometryInvalidated;

        /// <summary>
        /// Stores the offset faces (always inner and outer surface for each face)
        /// </summary>
        public Dictionary<(Face, GeometricOrientation), OffsetFace> Faces { get; private set; }

        /// <summary>
        /// EventHandler for the OffsetSurfaceChanged event
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="modifiedFaces">An enumerable containg all modified faces</param>
        public delegate void OffsetChangedEventHandler(object sender, IEnumerable<Face> modifiedFaces);
        /// <summary>
        /// Invoked when the offset surface has changed
        /// </summary>
        public event OffsetChangedEventHandler OffsetSurfaceChanged;

        /// <summary>
        /// Stores the generator used to generate offset surfaces
        /// </summary>
        public OffsetSurfaceGenerator Generator { get; private set; }

        /// <summary>
        /// Invokes the OffsetSurfaceChanged event
        /// </summary>
        /// <param name="faces">All modified faces</param>
        public void OnOffsetSurfaceChanged(IEnumerable<Face> faces)
        {
            OffsetSurfaceChanged?.Invoke(this, faces);
        }



        /// <summary>
        /// Initializes a new instance of the OffsetModel class
        /// </summary>
        /// <param name="model">The geometrymodel</param>
        /// <param name="dispatcherTimer">The dispatcher timer</param>
        public OffsetModel(GeometryModelData model, IDispatcherTimer dispatcherTimer)
        {
            this.Generator = new OffsetSurfaceGenerator(model, dispatcherTimer);
            this.Model = model;
            this.Faces = new Dictionary<(Face, GeometricOrientation), OffsetFace>();

            this.Model.GeometryChanged += Model_GeometryChanged;
            this.Model.TopologyChanged += Model_TopologyChanged;

            this.handleGeometryInvalidated = true;
        }




        internal void OnGeometryInvalidated(IEnumerable<BaseGeometry> affected_geometry)
        {
            if (HandleGeometryInvalidated)
                Generator.Update(GeometrySettings.Instance.CalculateOffsetSurfaces);
        }

        private void Model_TopologyChanged(object sender, IEnumerable<BaseGeometry> geometry)
        {
            var invalidated = Generator.Update(geometry, GeometrySettings.Instance.CalculateOffsetSurfaces);
            if (invalidated != null && invalidated.Any())
                OnOffsetSurfaceChanged(invalidated);
        }

        private void Model_GeometryChanged(object sender, IEnumerable<BaseGeometry> geometries)
        {
            Generator.Update(geometries, GeometrySettings.Instance.CalculateOffsetSurfaces);
        }
    }
}
