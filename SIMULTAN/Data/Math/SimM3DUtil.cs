// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
// Description: Collection of utility classes for the Media3D namespace.
//

using System;

namespace SIMULTAN.Data.SimMath
{
    internal static class SimM3DUtil
    {

        internal static double RadiansToDegrees(double radians)
        {
            return radians * (180.0 / Math.PI);
        }

        internal static double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180.0);
        }
    }

}
