// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// This file contains the implementation of DoubleUtil, which
// provides "fuzzy" comparison functionality for doubles.
// The file is based on the similar util class from the Avalon tree.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SimMath
{
    internal static class SimDoubleUtil
    {

        // Const values come from sdk\inc\crt\float.h
        private const double DBL_EPSILON = 2.2204460492503131e-016; /* smallest such that 1.0+DBL_EPSILON != 1.0 */

        /// <summary>
        /// Verifies if the given value is close to 0.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static bool IsZero(double d)
        {
            // IsZero(d) check can be used to make sure that dividing by 'd' will never produce Infinity.
            // use DBL_EPSILON instead of double.Epsilon because double.Epsilon is too small and doesn't guarantee that.
            return Math.Abs(d) <= DBL_EPSILON;
        }

        /// <summary>
        /// IsOne - Returns whether or not the double is "close" to 1.  Same as AreClose(double, 1),
        /// but this is faster.
        /// </summary>
        /// <returns>
        /// bool - the result of the AreClose comparision.
        /// </returns>
        /// <param name="value"> The double to compare to 1. </param>
        public static bool IsOne(double value)
        {
            return Math.Abs(value - 1.0) < 10.0 * DBL_EPSILON;
        }

    }
}
