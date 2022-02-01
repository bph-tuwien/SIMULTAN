using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Describes a 3D frustum enclosed by a list of clip planes
    /// </summary>
    public class Frustum
    {
        /// <summary>
        /// Clip planes defining the inside of the frustum as the intersection of inside half spaces 
        /// </summary>
        public List<ClipPlane> Planes { get; private set; }

        /// <summary>
        /// Initializes a new instance of this class with a set of clip planes
        /// </summary>
        /// <param name="clipPlanes"></param>
        public Frustum(List<ClipPlane> clipPlanes)
        {
            Planes = clipPlanes;
        }

        /// <summary>
        /// Checks whether a given point is contained in the frustum (i.e. the inside volume defined by the clip planes)
        /// </summary>
        /// <param name="p">Point to check</param>
        /// <returns>True, if point is contained in the frustum volume</returns>
        public bool IsPointInside(Point3D p)
        {
            return Planes.All(x => x.IsInside(p));
        }

        /// <summary>
        /// Tests if a polygon is entirely contained in the frustum.
        /// The polygon is clipped against the frustum first before testing.
        /// </summary>
        /// <param name="ngon">Polygon to test</param>
        /// <param name="allowPartially">If true, at least 1 vertex of the polygon has to remain in the frustum after clipping</param>
        /// <returns>True, if the polygon is inside the frustum</returns>
        public bool IsNgonInside(List<Point3D> ngon, bool allowPartially = false)
        {
            var clippedNgon = Clip(ngon);

            if (allowPartially)
                return clippedNgon.Count > 0;
            else
                return clippedNgon.Count == ngon.Count && clippedNgon.All(x => ngon.Contains(x));
        }

        /// <summary>
        /// Clips the given polygon against all clip planes (performs Sutherland-Hodgman clipping)
        /// </summary>
        /// <param name="ngon">List of polygon vertices</param>
        /// <returns>List of vertices resulting after clipping</returns>
        public List<Point3D> Clip(List<Point3D> ngon)
        {
            List<Point3D> outputList = new List<Point3D>(ngon);

            foreach (var clipPlane in Planes)
            {
                List<Point3D> inputList = new List<Point3D>(outputList);
                outputList.Clear();

                for (int i = 0; i < inputList.Count; i++)
                {
                    var currentPoint = inputList[i];
                    var prevPoint = inputList[(i + inputList.Count - 1) % inputList.Count];

                    (var intersectionPoint, _) = clipPlane.IntersectLine(prevPoint, currentPoint);

                    if (clipPlane.IsInside(currentPoint))
                    {
                        if (!clipPlane.IsInside(prevPoint))
                        {
                            outputList.Add(intersectionPoint);
                        }
                        outputList.Add(currentPoint);
                    }
                    else if (clipPlane.IsInside(prevPoint))
                    {
                        outputList.Add(intersectionPoint);
                    }
                }
            }

            return outputList;
        }
    }
}
