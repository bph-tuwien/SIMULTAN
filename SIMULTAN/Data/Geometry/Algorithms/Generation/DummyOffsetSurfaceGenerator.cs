using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Very fast offset surface generation. Just offsets the face into the required direction.
    /// Does not close corners/edges to adjacent faces
    /// </summary>
    public static class DummyOffsetSurfaceGenerator
    {
        /// <summary>
        /// Updates the offset mesh (on the full mesh)
        /// </summary>
        /// <param name="model">The geometry model to update</param>
        public static void Update(GeometryModelData model)
        {
            model.OffsetModel.Faces.Clear();

            if (model.OffsetQuery != null)
            {
                foreach (var face in model.Faces)
                {
                    model.OffsetModel.Faces.Add((face, GeometricOrientation.Forward), DummyFace(face, GeometricOrientation.Forward, model.OffsetQuery));
                    model.OffsetModel.Faces.Add((face, GeometricOrientation.Backward), DummyFace(face, GeometricOrientation.Backward, model.OffsetQuery));
                }
            }
        }

        /// <summary>
        /// Performs an partial update to the offset mesh
        /// </summary>
        /// <param name="model">The geometry model</param>
        /// <param name="invalidatedGeometry">The geometry that should be updated (only Faces in this list are considered)</param>
        /// <returns>A list of faces that have been modified</returns>
        public static IEnumerable<Face> Update(GeometryModelData model, IEnumerable<BaseGeometry> invalidatedGeometry)
        {
            HashSet<Face> modified = new HashSet<Face>();

            if (model.OffsetQuery != null)
            {
                foreach (var f in invalidatedGeometry.Where(x => x is Face))
                {
                    var face = (Face)f;

                    if (!model.ContainsGeometry(face))
                    {
                        model.OffsetModel.Faces.Remove((face, GeometricOrientation.Forward));
                        model.OffsetModel.Faces.Remove((face, GeometricOrientation.Backward));
                    }
                    else
                    {
                        modified.Add(face);

                        model.OffsetModel.Faces[(face, GeometricOrientation.Forward)] = DummyFace(face, GeometricOrientation.Forward, model.OffsetQuery);
                        model.OffsetModel.Faces[(face, GeometricOrientation.Backward)] = DummyFace(face, GeometricOrientation.Backward, model.OffsetQuery);
                    }
                }
            }

            return modified;
        }

        private static OffsetFace DummyFace(Face face, GeometricOrientation orientation, IOffsetQueryable offsetQuery)
        {
            var offset = 0.0;
            if (orientation == GeometricOrientation.Backward)
                offset = offsetQuery.GetFaceOffset(face).outer;
            else
                offset = offsetQuery.GetFaceOffset(face).inner;

            var dir = face.Normal * (int)orientation * offset;

            var off = new OffsetFace(face);
            off.Boundary.AddRange(face.Boundary.Edges.Select(x => x.StartVertex.Position + dir));
            return off;
        }
    }
}
