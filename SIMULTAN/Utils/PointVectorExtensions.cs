using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Usefull extensions for the Point3D class
    /// </summary>
    public static class PointVectorExtensions
    {
        /// <summary>
        /// Multiplies the point with a scalar value
        /// </summary>
        /// <param name="p">The point</param>
        /// <param name="d">The scalar</param>
        /// <returns>The multiplied point</returns>
        public static Point3D Multiply(this Point3D p, double d)
        {
            return new Point3D(p.X * d, p.Y * d, p.Z * d);
        }
        /// <summary>
        /// Divides the point by a scalar value
        /// </summary>
        /// <param name="p">The point</param>
        /// <param name="d">The scalar</param>
        /// <returns>The divided point</returns>
        public static Point3D Divide(this Point3D p, double d)
        {
            return new Point3D(p.X / d, p.Y / d, p.Z / d);
        }

        /// <summary>
        /// Returns a point containing only the X and Y coordinate
        /// </summary>
        /// <param name="p">Original point</param>
        /// <returns>A point containing only X and Y coordinate</returns>
        public static Point XY(this Point3D p)
        {
            return new Point(p.X, p.Y);
        }

        /// <summary>
        /// Returns a vector containing only the X and Y coordinate
        /// </summary>
        /// <param name="v">Original vector</param>
        /// <returns>A vector containing only X and Y coordinate</returns>
        public static Vector XY(this Vector3D v)
        {
            return new Vector(v.X, v.Y);
        }

        /// <summary>
        /// Returns a specific dimension of a Vector3D
        /// </summary>
        /// <param name="v">The Vector3D</param>
        /// <param name="idx">Index of the dimension: X: 0, Y: 1, Z: 2</param>
        /// <returns></returns>
		public static double Get(this Vector3D v, int idx)
        {
            switch (idx)
            {
                case 0:
                    return v.X;
                case 1:
                    return v.Y;
                case 2:
                    return v.Z;
                default:
                    throw new IndexOutOfRangeException("idx has to be between 0 and 2");
            }
        }
        /// <summary>
        /// Returns a specific dimension of a Point3D
        /// </summary>
        /// <param name="v">The Point3D</param>
        /// <param name="idx">Index of the dimension: X: 0, Y: 1, Z: 2</param>
        /// <returns></returns>
		public static double Get(this Point3D v, int idx)
        {
            switch (idx)
            {
                case 0:
                    return v.X;
                case 1:
                    return v.Y;
                case 2:
                    return v.Z;
                default:
                    throw new IndexOutOfRangeException("idx has to be between 0 and 2");
            }
        }

        /// <summary>
        /// Allows to get XYZ with an index
        /// </summary>
        /// <param name="p">The point</param>
        /// <param name="idx">The index (0=X, 1=Y, 2=Z, 3.. = Exception)</param>
        /// <returns>The desired axis</returns>
        public static double At(this Point3D p, int idx)
        {
            switch (idx)
            {
                case 0:
                    return p.X;
                case 1:
                    return p.Y;
                case 2:
                    return p.Z;
                default:
                    throw new IndexOutOfRangeException();
            }
        }
    }
}
