// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: 3D size implementation. 
//
//             
//
//

using System;


namespace SIMULTAN.Data.SimMath
{
    /// <summary>
    /// Size3D - A value type which defined a size in terms of non-negative width, 
    /// length, and height.
    /// </summary>
    public partial struct SimSize3D
    {
        #region Constructors

        /// <summary>
        /// Constructor which sets the size's initial values.  Values must be non-negative.
        /// </summary>
        /// <param name="x">X dimension of the new size.</param>
        /// <param name="y">Y dimension of the new size.</param>
        /// <param name="z">Z dimension of the new size.</param>
        public SimSize3D(double x, double y, double z)
        {
            if (x < 0 || y < 0 || z < 0)
            {
                throw new System.ArgumentException("Size dimensions cannot be negative.");
            }


            _x = x;
            _y = y;
            _z = z;
        }

        #endregion Constructors

        #region Statics

        /// <summary>
        /// Empty - a static property which provides an Empty size.  X, Y, and Z are 
        /// negative-infinity.  This is the only situation
        /// where size can be negative.
        /// </summary>
        public static SimSize3D Empty
        {
            get
            {
                return s_empty;
            }
        }

        #endregion Statics

        #region Public Methods and Properties

        /// <summary>
        /// IsEmpty - this returns true if this size is the Empty size.
        /// Note: If size is 0 this Size3D still contains a 0, 1, or 2 dimensional set
        /// of points, so this method should not be used to check for 0 volume.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _x < 0;
            }
        }

        /// <summary>
        /// Size in X dimension. Default is 0, must be non-negative.
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException("Cannot modify empty size.");
                }

                if (value < 0)
                {
                    throw new System.ArgumentException("Size dimensions cannot be negative.");
                }

                _x = value;
            }
        }

        /// <summary>
        /// Size in Y dimension. Default is 0, must be non-negative.
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException("Cannot modify empty size.");
                }

                if (value < 0)
                {
                    throw new System.ArgumentException("Size dimensions cannot be negative.");
                }

                _y = value;
            }
        }


        /// <summary>
        /// Size in Z dimension. Default is 0, must be non-negative.
        /// </summary>
        public double Z
        {
            get
            {
                return _z;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException("Cannot modify empty size.");
                }

                if (value < 0)
                {
                    throw new System.ArgumentException("Size dimensions cannot be negative.");
                }

                _z = value;
            }
        }

        #endregion Public Methods

        #region Public Operators

        /// <summary>
        /// Explicit conversion to Vector.
        /// </summary>
        /// <param name="size">The size to convert to a vector.</param>
        /// <returns>A vector equal to this size.</returns>
        public static explicit operator SimVector3D(SimSize3D size)
        {
            return new SimVector3D(size._x, size._y, size._z);
        }

        /// <summary>
        /// Explicit conversion to point.
        /// </summary>
        /// <param name="size">The size to convert to a point.</param>
        /// <returns>A point equal to this size.</returns>
        public static explicit operator SimPoint3D(SimSize3D size)
        {
            return new SimPoint3D(size._x, size._y, size._z);
        }

        #endregion Public Operators

        #region Private Methods

        private static SimSize3D CreateEmptySize3D()
        {
            SimSize3D empty = new SimSize3D();
            // Can't use setters because they throw on negative values
            empty._x = Double.NegativeInfinity;
            empty._y = Double.NegativeInfinity;
            empty._z = Double.NegativeInfinity;
            return empty;
        }

        #endregion Private Methods

        #region Private Fields

        private readonly static SimSize3D s_empty = CreateEmptySize3D();

        #endregion Private Fields
    }
}
