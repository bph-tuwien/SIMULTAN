using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides methods to triangulate a polygon (with holes)
    /// </summary>
    /// <remarks>There is a updated version present in the old geometry viewer project which has to be ported</remarks>
    public static class Triangulation
    {
        private const double LINEDISTCALC_TOLERANCE = 0.0001;
        private const double GENERAL_CALC_TOLERANCE = 0.0001;
        private enum TriangulationOrientation { CW, CCW, Invalid }

        /// <summary>
        /// Segments a polygon in the XZ plane into triangles
        /// </summary>
        /// <param name="polygon">A list of points describing the boundary of the polygon</param>
        /// <param name="holes">A list containing holes</param>
        /// <param name="positions">Returns the positions of the triangulated polygon</param>
        /// <param name="indices">Returns the indices of the triangulation</param>
        /// <param name="reverse">When set to true all SimPoint3D list are read back-to-front</param>
        public static void PolygonComplexFill(List<SimPoint3D> polygon, List<List<SimPoint3D>> holes,
            out List<SimPoint3D> positions, out List<int> indices,
            bool reverse = false)
        {
            positions = null;
            indices = null;

            if (polygon == null || polygon.Count < 3)
                return;

            if (PolygonIsConvexXZ(polygon) && (holes == null || holes.Count == 0))
            {
                // use the simpler algorithm
                (positions, indices) = PolygonFillSimpleOptimized(polygon, reverse);
                return;
            }

            // reverse the winding direction of the polygon and holes
            List<SimPoint3D> polyIN = new List<SimPoint3D>(polygon);
            List<List<SimPoint3D>> holesIN = null;
            if (holes != null)
                holesIN = new List<List<SimPoint3D>>(holes);

            if (reverse)
            {
                polyIN = ReversePolygon(polygon);
                holesIN = new List<List<SimPoint3D>>();
                if (holes != null)
                {
                    foreach (List<SimPoint3D> hole in holes)
                    {
                        holesIN.Add(ReversePolygon(hole));
                    }
                }
            }

            // perform actual algorithm
            List<List<SimPoint3D>> simplePolys = DecomposeInSimplePolygons(polyIN, holesIN); // ........................................................
            List<List<SimPoint3D>> monotonePolys = new List<List<SimPoint3D>>();
            //// debug
            //List<List<SimPoint3D>> mpolys = DecomposeInMonotonePolygons(simplePolys[9]);
            //monotonePolys.AddRange(mpolys);
            //// debug
            foreach (List<SimPoint3D> spoly in simplePolys)
            {
                List<List<SimPoint3D>> mpolys = DecomposeInMonotonePolygons(spoly);
                monotonePolys.AddRange(mpolys);
            }

            List<(List<SimPoint3D> positions, List<int> indices)> triangulation = new List<(List<SimPoint3D>, List<int>)>();
            int counter = 0;
            foreach (List<SimPoint3D> mpoly in monotonePolys)
            {
                counter++;
                var geometry = PolygonFillMonotone(mpoly, reverse);
                if (geometry.positions != null)
                    triangulation.Add(geometry);
            }

            var finalTriangulation = CombineMeshes(triangulation);

            positions = finalTriangulation.positions;
            indices = finalTriangulation.indices;
        }


        private static bool PolygonIsConvexXZ(List<SimPoint3D> _polygon)
        {
            if (_polygon == null)
                return false;

            int n = _polygon.Count;
            if (n < 3)
                return false;

            SimVector3D v1 = _polygon[0] - _polygon[n - 1];
            SimVector3D v2 = _polygon[1] - _polygon[0];
            double crossY_prev = v1.X * v2.Z - v1.Z * v2.X;

            for (int i = 2; i <= n; i++)
            {
                v1 = v2;
                v2 = _polygon[i % n] - _polygon[i - 1];
                double crossY = v1.X * v2.Z - v1.Z * v2.X;

                if ((crossY < 0) != (crossY_prev < 0))
                    return false;

                crossY_prev = crossY;
            }

            return true;
        }

        private static (List<SimPoint3D> positions, List<int> indices) PolygonFillSimpleOptimized(List<SimPoint3D> polygon, bool reverse = false)
        {
            // save positions
            List<SimPoint3D> positions = ExtractWellDefined(polygon);

            if (positions.Count < 3)
                return (null, null);

            // save indices
            List<int> indices = new List<int>(positions.Count * 3);
            for (int i = 1; i < positions.Count - 1; i++)
            {
                if (!reverse)
                {
                    indices.Add(0);
                    indices.Add(i);
                    indices.Add((i + 1) % positions.Count);
                }
                else
                {
                    indices.Add(0);
                    indices.Add((i + 1) % positions.Count);
                    indices.Add(i);
                }
            }

            return (positions, indices);
        }

        private static List<SimPoint3D> ExtractWellDefined(List<SimPoint3D> polygon, double tolerance = GENERAL_CALC_TOLERANCE * 0.01)
        {
            var t2 = tolerance * tolerance;

            /*bool point_removed = true;
			List<SimPoint3D> poly_well_defined = new List<SimPoint3D>(polygon);
			List<SimPoint3D> poly_well_defined_1 = new List<SimPoint3D>();
			while (point_removed)
			{
				// check polygon
				point_removed = false;
				int n = poly_well_defined.Count;
				int index_to_remove = -1;
				for (int i = 0; i < n; i++)
				{
					bool tri_well_defined = TriangleIsWellDefined(poly_well_defined[i], poly_well_defined[(i + 1) % n], poly_well_defined[(i + 2) % n], tolerance);
					if (!tri_well_defined)
					{
						point_removed = true;
						index_to_remove = (i + 1) % n;
						break;
					}
				}
				// rebuild polygon
				if (index_to_remove > - 1)
				{
					poly_well_defined_1.Clear();
					for (int i = 0; i < n; i++)
					{
						if (i == index_to_remove) continue;
						poly_well_defined_1.Add(poly_well_defined[i]);
					}
					poly_well_defined = new List<SimPoint3D>(poly_well_defined_1);
				}				
			}*/

            List<SimPoint3D> poly_well_defined = new List<SimPoint3D>(polygon.Count);
            var n = polygon.Count;
            for (int i = 0; i < n; ++i)
            {
                //var p0 = polygon[i];
                //var p1 = polygon[(i + 1) % n];
                //var p2 = polygon[(i + 2) % n];

                // this does not change the vertex order of the polygon
                var p0 = polygon[(n + i - 1) % n];
                var p1 = polygon[i];
                var p2 = polygon[(i + 1) % n];

                if (SimVector3D.CrossProduct(p1 - p0, p2 - p0).LengthSquared > t2)
                    poly_well_defined.Add(p1);
            }

            return poly_well_defined;
        }

        private static List<SimPoint3D> ReversePolygon(List<SimPoint3D> _original)
        {
            if (_original == null)
                return null;

            int n = _original.Count;
            List<SimPoint3D> reversed = new List<SimPoint3D>(n) { _original[0] };
            for (int i = n - 1; i > 0; i--)
                reversed.Add(_original[i]);

            return reversed;
        }

        /// <summary>
        /// Returns a list of polygons that cover the polygon but have no holes.
        /// </summary>
        /// <param name="polygon">The original polygon</param>
        /// <param name="holes">A list of hole polygons</param>
        /// <returns></returns>
        public static List<List<SimPoint3D>> DecomposeInSimplePolygons(List<SimPoint3D> polygon, List<List<SimPoint3D>> holes)
        {
            if (polygon == null)
                throw new ArgumentNullException(nameof(polygon));

            if (holes == null || holes.Count < 1)
                return new List<List<SimPoint3D>> { polygon };

            // make sure the winding direction of the polygon and all contained holes is the same!
            var polygon_orient = CalculateIfPolygonClockWise(polygon, GENERAL_CALC_TOLERANCE);
            int nrH = holes.Count;
            for (int i = 0; i < nrH; i++)
            {
                var hole_orient = CalculateIfPolygonClockWise(holes[i], GENERAL_CALC_TOLERANCE);
                if (polygon_orient != hole_orient)
                {
                    holes[i] = ReversePolygon(holes[i]);
                }
            }

            var connectingLines = ConnectPolygonWContainedHolesTwice_Improved3(polygon, holes);
            int nrCL = connectingLines.Count;
            if (nrCL < 2)
                return new List<List<SimPoint3D>> { polygon };

            // perform decomposition (no duplicates in the connecting lines)
            var splitting_paths = ExtractSplittingPathsFrom(connectingLines, nrH);

            int d = splitting_paths.Count;

            // perform splitting
            List<List<SimPoint3D>> list_before_Split_polys = new List<List<SimPoint3D>>();
            List<List<ValueTuple<int, int>>> list_before_Split_inds = new List<List<ValueTuple<int, int>>>();

            List<List<SimPoint3D>> list_after_Split_polys = new List<List<SimPoint3D>>();
            List<List<ValueTuple<int, int>>> list_after_Split_inds = new List<List<ValueTuple<int, int>>>();

            list_before_Split_polys.Add(polygon);
            list_before_Split_inds.Add(GenerateDoubleIndices(-1, 0, polygon.Count));

            for (int j = 0; j < d; j++)
            {
                int nrToSplit = list_before_Split_polys.Count;
                //int nrSuccessfulSplis = 0;
                for (int k = 0; k < nrToSplit; k++)
                {
                    List<SimPoint3D> polyA, polyB;
                    List<ValueTuple<int, int>> originalIndsA, originalIndsB;
                    bool inputValid;
                    SplitPolygonWHolesAlongPath(list_before_Split_polys[k], list_before_Split_inds[k],
                                                splitting_paths[j], true, polygon, holes,
                                                out polyA, out polyB, out originalIndsA, out originalIndsB, out inputValid);

                    if (inputValid && polyA.Count > 2 && polyB.Count > 2)
                    {
                        // successful split
                        list_after_Split_polys.Add(polyA);
                        list_after_Split_inds.Add(originalIndsA);
                        list_after_Split_polys.Add(polyB);
                        list_after_Split_inds.Add(originalIndsB);
                    }
                    else
                    {
                        // no split
                        list_after_Split_polys.Add(list_before_Split_polys[k]);
                        list_after_Split_inds.Add(list_before_Split_inds[k]);
                    }
                }
                // swap lists
                list_before_Split_polys = new List<List<SimPoint3D>>(list_after_Split_polys);
                list_before_Split_inds = new List<List<ValueTuple<int, int>>>(list_after_Split_inds);
                list_after_Split_polys = new List<List<SimPoint3D>>();
                list_after_Split_inds = new List<List<ValueTuple<int, int>>>();
            }


            return list_before_Split_polys;
        }

        private static TriangulationOrientation CalculateIfPolygonClockWise(List<SimPoint3D> _polygon, double _tolerance)
        {
            if (_polygon == null || _polygon.Count < 3)
                return TriangulationOrientation.Invalid;

            double area = CalculatePolygonSignedArea(_polygon);
            if (Math.Abs(area) > _tolerance)
            {
                return (area > 0) ? TriangulationOrientation.CW : TriangulationOrientation.CCW;
            }

            return TriangulationOrientation.Invalid;
        }

        private static double CalculatePolygonSignedArea(List<SimPoint3D> _polygon)
        {
            if (_polygon == null || _polygon.Count < 3)
                return 0.0;

            int n = _polygon.Count;
            double area = 0;

            for (int i = 0; i < n; i++)
            {
                area += (_polygon[(i + 1) % n].Z + _polygon[i].Z) * (_polygon[(i + 1) % n].X - _polygon[i].X);
            }

            return (area * 0.5);
        }


        private static List<ValueTuple<int, int, int, int>> ConnectPolygonWContainedHolesTwice_Improved3(List<SimPoint3D> polygon, List<List<SimPoint3D>> holes)
        {
            var connectingLines = new List<ValueTuple<int, int, int, int>>();

            // 1. use polygons as they are
            var connectingLines_original = ConnectPolygonWContainedHoles(polygon, holes);
            RemoveNearlyAlignedConnectingLines(connectingLines_original, polygon, holes);

            // 2. switch X and Z and ...
            List<SimPoint3D> poly_swapped = polygon.Select(x => new SimPoint3D(x.Z, x.Y, x.X)).ToList();
            List<List<SimPoint3D>> holes_swapped = new List<List<SimPoint3D>>();
            foreach (List<SimPoint3D> h in holes)
            {
                holes_swapped.Add(h.Select(x => new SimPoint3D(x.Z, x.Y, x.X)).ToList());
            }
            var connectingLines_swapped = ConnectPolygonWContainedHoles(poly_swapped, holes_swapped);
            RemoveNearlyAlignedConnectingLines(connectingLines_swapped, polygon, holes);

            // 3. rotate by 30 deg
            var (polygon_r30, holes_r30) = Rotate(polygon, holes, 30);
            var connectingLines_r30 = ConnectPolygonWContainedHoles(polygon_r30, holes_r30);
            RemoveNearlyAlignedConnectingLines(connectingLines_r30, polygon, holes);

            // 4. rotate by 45 deg
            var (polygon_r45, holes_r45) = Rotate(polygon, holes, 45);
            var connectingLines_r45 = ConnectPolygonWContainedHoles(polygon_r45, holes_r45);
            RemoveNearlyAlignedConnectingLines(connectingLines_r45, polygon, holes);

            // 5. rotate by 60 deg
            var (polygon_r60, holes_r60) = Rotate(polygon, holes, 60);
            var connectingLines_r60 = ConnectPolygonWContainedHoles(polygon_r60, holes_r60);
            RemoveNearlyAlignedConnectingLines(connectingLines_r60, polygon, holes);

            //// debug
            //Debug.WriteLine("---------------------------- original");
            //foreach (var cL in connectingLines_original)
            //{
            //    Print(cL);
            //}
            //Debug.WriteLine("---------------------------- swapped X and Z");
            //foreach (var cL in connectingLines_swapped)
            //{
            //    Print(cL);
            //}
            //Debug.WriteLine("---------------------------- rotated 30 deg");
            //foreach (var cL in connectingLines_r30)
            //{
            //    Print(cL);
            //}
            //Debug.WriteLine("---------------------------- rotated 45 deg");
            //foreach (var cL in connectingLines_r45)
            //{
            //    Print(cL);
            //}
            //Debug.WriteLine("---------------------------- rotated 60 deg");
            //foreach (var cL in connectingLines_r60)
            //{
            //    Print(cL);
            //}

            // 6. add all connections one by one           
            var all_clines = new List<(int, int, int, int)>();

            List<int> sizes = new List<int> { connectingLines_original.Count, connectingLines_swapped.Count, connectingLines_r30.Count, connectingLines_r45.Count, connectingLines_r60.Count };
            int max_nr_cL = sizes.Max();
            for (int i = 0; i < max_nr_cL; i++)
            {
                if (connectingLines_original.Count > i)
                    all_clines.Add(connectingLines_original[i]);
                if (connectingLines_swapped.Count > i)
                    all_clines.Add(connectingLines_swapped[i]);
                if (connectingLines_r30.Count > i)
                    all_clines.Add(connectingLines_r30[i]);
                if (connectingLines_r45.Count > i)
                    all_clines.Add(connectingLines_r45[i]);
                if (connectingLines_r60.Count > i)
                    all_clines.Add(connectingLines_r60[i]);

            }

            //// debug
            //Debug.WriteLine("---------------------------- ALL");
            //foreach (var cL in all_clines)
            //{
            //    Print(cL);
            //}

            var cycle_detector = new HoleCycleDetector(holes.Count, true);

            List<(int, int, int, int)> to_add = new List<(int, int, int, int)>();
            List<(int, int, int, int)> to_remove = new List<(int, int, int, int)>();
            for (int s = 0; s < all_clines.Count; s++)
            {
                connectingLines = GetAdaptedConnections(connectingLines, to_add, to_remove);
                to_add.Clear();
                to_remove.Clear();
                var (admissible, obsolete_connection) = CanInsertConnectingLineInConnections(connectingLines, all_clines[s], polygon, holes);
                if (admissible)
                {
                    // fill the cycle detector
                    if (obsolete_connection.Item2 > -1)
                        cycle_detector.RemoveConnectingLine(obsolete_connection);
                    cycle_detector.AddConnectingLine(all_clines[s]);

                    // fill the connecting lines
                    //Debug.WriteLine(" ----------------------------- ADDING CONNECTION {0}", PrintString(all_clines[s]));
                    to_add.Add(all_clines[s]);
                    if (obsolete_connection.Item2 > -1)
                    {
                        //Debug.WriteLine(" >>>>>>>>>>>>>>>>>>>>>>>>>>> sacrificed connection {0}", PrintString(obsolete_connection));
                        to_remove.Add(obsolete_connection);
                    }
                }
            }

            // 7. check if there are holes involved only in 1 connecting line
            var usage = GetIndexUsage_Improved(connectingLines, polygon.Count, holes.Count);

            // 7a. find additional connecting lines to compensate for 7!
            foreach (int index in usage.not_used)
            {
                // this should not really happen!
                int count = connectingLines.Count;
                InsertAdditionalConnectingLines(connectingLines, polygon, holes, index, new List<int>());
                if (count < connectingLines.Count)
                {
                    cycle_detector.AddConnectingLine(connectingLines[connectingLines.Count - 1]);
                    usage.single_use.Add(index);
                }
                else
                {
                    var neightbor_holes = FindClosestHoles(holes, index);
                    if (neightbor_holes.Item1 != -1)
                        InsertAdditionalConnectingLines(connectingLines, polygon, holes, index, neightbor_holes.Item1, new List<int>(), new List<int>());
                    if (neightbor_holes.Item2 != -1)
                        InsertAdditionalConnectingLines(connectingLines, polygon, holes, index, neightbor_holes.Item2, new List<int>(), new List<int>());
                    if (count == connectingLines.Count - 1)
                        usage.single_use.Add(index);
                }
            }
            foreach (int index in usage.single_use)
            {
                if (index != -1)
                {
                    var cL = connectingLines.FirstOrDefault(x => x.Item1 == index || x.Item3 == index);
                    int single_index_in_hole_in_use = (cL.Item1 == index) ? cL.Item2 : cL.Item4;

                    // extract potential candidates for connection and test them
                    int neighbor = (cL.Item1 == index) ? cL.Item3 : cL.Item1;
                    List<int> candidates = cycle_detector.AllNodes[neighbor].Nodes.Select(x => x.Key.Index).ToList();
                    candidates.Remove(index);
                    if (!candidates.Contains(-1))
                        candidates.Add(-1);

                    int count = connectingLines.Count;
                    foreach (int c in candidates)
                    {
                        if (c == -1)
                            InsertAdditionalConnectingLines(connectingLines, polygon, holes, index, new List<int> { single_index_in_hole_in_use });
                        else
                            InsertAdditionalConnectingLines(connectingLines, polygon, holes, index, c, new List<int> { single_index_in_hole_in_use }, new List<int>());
                        // stop after finding one valid connection
                        if (count < connectingLines.Count)
                        {
                            cycle_detector.AddConnectingLine(connectingLines[connectingLines.Count - 1]);
                            break;
                        }
                    }
                }
            }

            // 8. check for holes connected to the outer polygon via only one other hole
            var bottleneck_check = cycle_detector.GetAllHolesConnectedToOnlyOneOtherHoleInclIndirection();

            // 8a. find additional connecting lines to compensate for 8!
            if (bottleneck_check.found)
            {
                foreach (var entry in bottleneck_check.holes_bottleneck)
                {
                    // avoid redundent work
                    if (usage.single_use.Contains(entry.Key))
                        continue;

                    // extract potential candidates for connection and test them
                    List<int> candidates = cycle_detector.AllNodes[entry.Value].Nodes.Select(x => x.Key.Index).ToList();
                    candidates.Remove(entry.Key);
                    if (!candidates.Contains(-1))
                        candidates.Add(-1);

                    var cL = connectingLines.FirstOrDefault(x => x.Item1 == entry.Key || x.Item3 == entry.Key);
                    int single_index_in_hole_in_use = (cL.Item1 == entry.Key) ? cL.Item2 : cL.Item4;

                    int count = connectingLines.Count;
                    foreach (int c in candidates)
                    {
                        if (c == -1)
                            InsertAdditionalConnectingLines(connectingLines, polygon, holes, entry.Key, new List<int> { single_index_in_hole_in_use });
                        else
                            InsertAdditionalConnectingLines(connectingLines, polygon, holes, entry.Key, c, new List<int> { single_index_in_hole_in_use }, new List<int>());
                        // stop after finding one valid connection
                        if (count < connectingLines.Count)
                            break;
                    }

                }

            }

            return connectingLines;
        }

        private static void InsertAdditionalConnectingLines(List<ValueTuple<int, int, int, int>> connecting_lines, List<SimPoint3D> polygon, List<List<SimPoint3D>> holes, int hole_index, List<int> indices_in_hole_to_avoid)
        {
            List<ValueTuple<int, int, int, int>> candidates = new List<(int, int, int, int)>();
            for (int h = 0; h < holes[hole_index].Count; h++)
            {
                if (indices_in_hole_to_avoid.Contains(h))
                    continue;

                for (int p = 0; p < polygon.Count; p++)
                {
                    var candidate = ValueTuple.Create<int, int, int, int>(hole_index, h, -1, p);
                    var loop1 = connecting_lines.FirstOrDefault(cL => cL.Item1 == -1 && cL.Item2 == p && cL.Item3 == hole_index);
                    if (!(loop1.Item1 == default && loop1.Item2 == default && loop1.Item3 == default && loop1.Item4 == default) && loop1.Item3 == hole_index)
                        continue;
                    var loop2 = connecting_lines.FirstOrDefault(cL => cL.Item1 == hole_index && cL.Item3 == -1 && cL.Item4 == p);
                    if (!(loop2.Item1 == default && loop2.Item2 == default && loop2.Item3 == default && loop2.Item4 == default) && loop2.Item1 == hole_index)
                        continue;
                    candidates.Add(candidate);
                }
            }
            foreach (var c in candidates)
            {
                bool admissible_in_poly = LineIsValidInPolygonWHoles(polygon, holes, c.Item4, c.Item1, c.Item2);
                if (admissible_in_poly)
                {
                    var (admissible, obsolete_connection) = CanInsertConnectingLineInConnections(connecting_lines, c, polygon, holes);
                    if (admissible && (obsolete_connection.Item2 == -1 ||
                        (obsolete_connection.Item1 == hole_index && indices_in_hole_to_avoid.Contains(obsolete_connection.Item2)) ||
                        (obsolete_connection.Item3 == hole_index && indices_in_hole_to_avoid.Contains(obsolete_connection.Item4))))
                    // the obsolete connection is actually an invalid one or the one causing 'single usage' of the hole
                    {
                        List<ValueTuple<int, int, int, int>> found = new List<(int, int, int, int)> { c };
                        RemoveNearlyAlignedConnectingLines(found, polygon, holes, GENERAL_CALC_TOLERANCE * 50);
                        if (found.Count == 1)
                        {
                            // admissible w/o side-effects
                            connecting_lines.Add(c);
                            break;
                        }
                    }
                }
            }
        }

        private static void InsertAdditionalConnectingLines(List<ValueTuple<int, int, int, int>> connecting_lines, List<SimPoint3D> polygon, List<List<SimPoint3D>> holes,
                                                        int hole_index1, int hole_index2, List<int> indices_in_hole_to_avoid_in1, List<int> indices_in_hole_to_avoid_in2)
        {
            List<ValueTuple<int, int, int, int>> candidates = new List<(int, int, int, int)>();
            for (int h1 = 0; h1 < holes[hole_index1].Count; h1++)
            {
                if (indices_in_hole_to_avoid_in1.Contains(h1))
                    continue;

                for (int h2 = 0; h2 < holes[hole_index2].Count; h2++)
                {
                    if (indices_in_hole_to_avoid_in2.Contains(h2))
                        continue;

                    var candidate = ValueTuple.Create<int, int, int, int>(hole_index1, h1, hole_index2, h2);
                    var loop1 = connecting_lines.FirstOrDefault(cL => cL.Item1 == hole_index2 && cL.Item2 == h2 && cL.Item3 == hole_index1);
                    if (!(loop1.Item1 == default && loop1.Item2 == default && loop1.Item3 == default && loop1.Item4 == default) && loop1.Item3 == hole_index1)
                        continue;
                    var loop2 = connecting_lines.FirstOrDefault(cL => cL.Item1 == hole_index1 && cL.Item3 == hole_index2 && cL.Item4 == h2);
                    if (!(loop2.Item1 == default && loop2.Item2 == default && loop2.Item3 == default && loop2.Item4 == default) && loop2.Item1 == hole_index1)
                        continue;
                    candidates.Add(candidate);
                }
            }
            foreach (var c in candidates)
            {
                bool admissible_in_poly = LineIsValidInPolygonWHoles(polygon, holes, c.Item1, c.Item2, c.Item3, c.Item4);
                if (admissible_in_poly)
                {
                    var (admissible, obsolete_connection) = CanInsertConnectingLineInConnections(connecting_lines, c, polygon, holes);
                    if (admissible && obsolete_connection.Item2 == -1) // the obsolete connection is actually an invalid one
                    {
                        List<ValueTuple<int, int, int, int>> found = new List<(int, int, int, int)> { c };
                        RemoveNearlyAlignedConnectingLines(found, polygon, holes, GENERAL_CALC_TOLERANCE * 50);
                        if (found.Count == 1)
                        {
                            // admissible w/o side-effects
                            connecting_lines.Add(c);
                            break;
                        }
                    }
                }
            }
        }

        private static void RemoveNearlyAlignedConnectingLines(List<ValueTuple<int, int, int, int>> connecting_lines, List<SimPoint3D> polygon, List<List<SimPoint3D>> holes,
                                                                double tolerance_factor = GENERAL_CALC_TOLERANCE * 200)
        {
            List<ValueTuple<int, int, int, int>> to_remove = new List<(int, int, int, int)>();
            foreach (var cL in connecting_lines)
            {
                SimPoint3D p1 = (cL.Item1 == -1) ? polygon[cL.Item2] : holes[cL.Item1][cL.Item2];
                SimPoint3D p2 = (cL.Item3 == -1) ? polygon[cL.Item4] : holes[cL.Item3][cL.Item4];

                bool polygon_overlap_detected = LineNearlyOverlapsWithPolygon(p1, p2, polygon, tolerance_factor);
                if (polygon_overlap_detected)
                {
                    to_remove.Add(cL);
                    continue;
                }
                foreach (var hole in holes)
                {
                    bool hole_overlap_detected = LineNearlyOverlapsWithPolygon(p1, p2, hole, tolerance_factor);
                    if (hole_overlap_detected)
                    {
                        to_remove.Add(cL);
                        continue;
                    }
                }
            }

            foreach (var cL in to_remove)
            {
                connecting_lines.Remove(cL);
            }
        }

        private static (List<SimPoint3D> polygon_rotated, List<List<SimPoint3D>> holes_rotated) Rotate(List<SimPoint3D> polygon, List<List<SimPoint3D>> holes, double angle_in_deg)
        {
            SimVector3D pcenter = new SimVector3D(0, 0, 0);
            for (int i = 0; i < polygon.Count; i++)
            {
                pcenter += (SimVector3D)polygon[i];
            }
            pcenter /= polygon.Count;

            SimMatrix3D matrixR = new SimMatrix3D();
            matrixR.Translate(-pcenter);
            matrixR.Rotate(new SimQuaternion(new SimVector3D(0, 1, 0), angle_in_deg));
            matrixR.Translate(pcenter);

            SimPoint3D[] polyR = polygon.ToArray();
            matrixR.Transform(polyR);
            List<SimPoint3D> polygon_rotated = polyR.ToList();
            List<List<SimPoint3D>> holes_rotated = new List<List<SimPoint3D>>();
            foreach (List<SimPoint3D> h in holes)
            {
                SimPoint3D[] hR = h.ToArray();
                matrixR.Transform(hR);
                holes_rotated.Add(hR.ToList());
            }
            return (polygon_rotated, holes_rotated);
        }


        //TODO: Check for simplifications
        private static List<ValueTuple<int, int, int, int>> ConnectPolygonWContainedHoles(List<SimPoint3D> polygon, List<List<SimPoint3D>> holes)
        {
            // Return value contains
            // [X]:-1 for polygon / otherwise hole index [Y]: index in polygon / hole, [Z]:hole index, [W]:index in hole

            if (holes == null || holes.Count == 0) //TODO Remove
                throw new Exception("SHOULD HAVE BEEN CHECKED BEFORE");

            // left to right
            List<ValueTuple<int, int, int, int>> connectingLines_LR = new List<ValueTuple<int, int, int, int>>();
            List<int> ind_connected_holes_LR = new List<int>();
            // right to left
            List<ValueTuple<int, int, int, int>> connectingLines_RL = new List<ValueTuple<int, int, int, int>>();
            List<int> ind_connected_holes_RL = new List<int>();

            // order the vertices according to the X component
            SimPoint3DComparer SimPoint3Dcomp = new SimPoint3DComparer();
            SortedList<SimPoint3D, int> vertices_ordered = new SortedList<SimPoint3D, int>(SimPoint3Dcomp);

            var n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                //TODO: Check if this improves performance
                if (vertices_ordered.ContainsKey(polygon[i]))
                    continue;

                try
                {
                    vertices_ordered.Add(polygon[i], i + 1);
                }
                catch (ArgumentException)
                {
                    // if the same vertex occurs more than once, just skip it
                    continue;
                }
            }

            var nrH = holes.Count;
            for (int j = 0; j < nrH; j++)
            {
                List<SimPoint3D> hole = holes[j];

                int h = hole.Count;
                for (int i = 0; i < h; i++)
                {
                    //TODO Check if this improves performance
                    if (vertices_ordered.ContainsKey(hole[i]))
                        continue;

                    try
                    {
                        vertices_ordered.Add(hole[i], (j + 1) * 1000 + i + 1);
                    }
                    catch (ArgumentException)
                    {
                        // if the same vertex occurs more than once, just skip it
                        continue;
                    }
                }
            }

            int m = vertices_ordered.Count;

            // -------------------------------- TRAVERSAL LEFT -> RIGHT ------------------------------------- //

            // traverse the polygon in X-direction to determine the admissible diagonals 
            // connecting the FIRST points of each hole with the polygon vertices to the LEFT of them
            // (if such do not exist -> try connecting to previous holes to the LEFT)
            for (int j = 1; j < m - 1; j++)
            {
                var current_alongX = vertices_ordered.ElementAt(j).Key;
                int ind_current_alongX = vertices_ordered.ElementAt(j).Value - 1;
                int ind_hole = ind_current_alongX / 1000 - 1;
                if (ind_hole < 0 || ind_hole > nrH - 1 || ind_connected_holes_LR.Contains(ind_hole))
                    continue;

                // get information of the neighbor vertices in the hole
                List<SimPoint3D> hole = holes[ind_hole];
                int nHole = hole.Count;
                int ind_in_hole = ind_current_alongX % 1000;
                var prev = hole[(nHole + ind_in_hole - 1) % nHole];
                var next = hole[(ind_in_hole + 1) % nHole];

                if (prev.X >= current_alongX.X && next.X >= current_alongX.X)
                {
                    // START VERTEX -> Connect to a polygon vertex that is before this one along the X axis
                    for (int c = 1; c < j + 1; c++)
                    {
                        var prev_poly_alongX = vertices_ordered.ElementAt(j - c).Key;
                        int ind_prev_poly_alongX = vertices_ordered.ElementAt(j - c).Value - 1;
                        int ind_prev_hole = ind_prev_poly_alongX / 1000 - 1;
                        if (ind_prev_hole == ind_hole)
                            continue;

                        int ind_prev_in_hole = ind_prev_poly_alongX % 1000;


                        if (prev_poly_alongX.X < current_alongX.X)
                        {
                            // check if the diagonal is valid
                            bool isAdmissible = false;
                            if (ind_prev_hole == -1)
                            {
                                // check admissibility in the polygon
                                //Debug.WriteLine("testing diagonal -1:{0} - {1}:{2}", ind_prev_poly_alongX, ind_hole, ind_in_hole);
                                isAdmissible = LineIsValidInPolygonWHoles(polygon, holes, ind_prev_poly_alongX, ind_hole, ind_in_hole);
                            }
                            else
                            {
                                // check admissiblity w regard to two holes contained in the polygon
                                //Debug.WriteLine("testing diagonal {0}:{1} - {2}:{3}", ind_prev_hole, ind_prev_in_hole, ind_hole, ind_in_hole);
                                isAdmissible = LineIsValidInPolygonWHoles(polygon, holes,
                                                                          ind_prev_hole, ind_prev_in_hole, ind_hole, ind_in_hole);
                            }
                            if (isAdmissible)
                            {
                                //Debug.WriteLine(">>>>>>>>>>>> YES");
                                connectingLines_LR.Add(ValueTuple.Create(ind_prev_hole, ind_prev_in_hole, ind_hole, ind_in_hole));
                                ind_connected_holes_LR.Add(ind_hole);
                                break;
                            }
                            else
                            {
                                //Debug.WriteLine("NO");
                            }
                        }
                    }
                }

            }

            List<ValueTuple<int, int, int, int>> connectingLines = new List<(int, int, int, int)>(connectingLines_LR);

            // -------------------------------- TRAVERSAL RIGHT -> LEFT ------------------------------------- //
            // traverse the polygon in X-direction to determine the admissible diagonals 
            // connecting the LAST points of each hole with the polygon vertices to the RIGHT of them
            // (if such do not exist -> try connecting to previous holes to the RIGHT)
            for (int j = m - 2; j > 0; j--)
            {
                var current_alongX = vertices_ordered.ElementAt(j).Key;
                int ind_current_alongX = vertices_ordered.ElementAt(j).Value - 1;
                int ind_hole = ind_current_alongX / 1000 - 1;
                if (ind_hole < 0 || ind_hole > nrH - 1 || ind_connected_holes_RL.Contains(ind_hole))
                    continue;

                // get information of the neighbor vertices in the hole
                List<SimPoint3D> hole = holes[ind_hole];
                int nHole = hole.Count;
                int ind_in_hole = ind_current_alongX % 1000;
                var prev = hole[(nHole + ind_in_hole - 1) % nHole];
                var next = hole[(ind_in_hole + 1) % nHole];

                if (prev.X <= current_alongX.X && next.X <= current_alongX.X)
                {
                    // END VERTEX -> Connect to a polygon vertex that is after this one along the X axis
                    for (int c = 1; c < m - j; c++)
                    {
                        var next_poly_alongX = vertices_ordered.ElementAt(j + c).Key;
                        var ind_next_poly_alongX = vertices_ordered.ElementAt(j + c).Value - 1;
                        int ind_next_hole = ind_next_poly_alongX / 1000 - 1;
                        if (ind_next_hole == ind_hole)
                            continue;

                        int ind_next_in_hole = ind_next_poly_alongX % 1000;

                        if (next_poly_alongX.X > current_alongX.X)
                        {
                            // check if the diagonal is valid
                            bool isAdmissible = false;
                            if (ind_next_hole == -1)
                            {
                                // check admissibility in the polygon
                                isAdmissible = LineIsValidInPolygonWHoles(polygon, holes,
                                                                                    ind_next_poly_alongX, ind_hole, ind_in_hole);
                            }
                            else
                            {
                                // check admissiblity w regard to two holes contained in the polygon
                                isAdmissible = LineIsValidInPolygonWHoles(polygon, holes,
                                                                          ind_next_hole, ind_next_in_hole, ind_hole, ind_in_hole);
                            }
                            // check if the diagonal intersects any diagonals from the previous traversal
                            if (isAdmissible)
                            {
                                var p1 = holes[ind_hole][ind_in_hole];
                                SimPoint3D p2;
                                if (ind_next_hole == -1)
                                    p2 = polygon[ind_next_poly_alongX];
                                else
                                    p2 = holes[ind_next_hole][ind_next_in_hole];

                                foreach (var entry in connectingLines_LR)
                                {
                                    var q1 = holes[(int)entry.Item3][(int)entry.Item4];
                                    SimPoint3D q2;
                                    if (entry.Item1 == -1)
                                        q2 = polygon[(int)entry.Item2];
                                    else
                                        q2 = holes[(int)entry.Item1][(int)entry.Item2];

                                    bool intersection = LineWLineCollision3D(p1, p2, q1, q2,
                                                                    GENERAL_CALC_TOLERANCE);
                                    if (intersection)
                                    {
                                        isAdmissible = false;
                                        break;
                                    }
                                }
                            }
                            if (isAdmissible)
                            {
                                connectingLines_RL.Add(ValueTuple.Create(ind_next_hole, ind_next_in_hole, ind_hole, ind_in_hole));
                                ind_connected_holes_RL.Add(ind_hole);
                                break;
                            }
                        }
                    }
                }

            }
            connectingLines.AddRange(connectingLines_RL);

            // remove duplicates
            List<ValueTuple<int, int, int, int>> connectingLines_optimized = new List<(int, int, int, int)>();
            int nrC = connectingLines.Count;
            for (int i = 0; i < nrC; i++)
            {
                bool hasReversedDuplicate = false;
                for (int j = i + 1; j < nrC; j++)
                {
                    if (connectingLines[i].Item1 == connectingLines[j].Item3 && connectingLines[i].Item2 == connectingLines[j].Item4 &&
                        connectingLines[i].Item3 == connectingLines[j].Item1 && connectingLines[i].Item4 == connectingLines[j].Item2)
                    {
                        hasReversedDuplicate = true;
                        break;
                    }
                }
                if (!hasReversedDuplicate)
                    connectingLines_optimized.Add(connectingLines[i]);
            }

            // remove intersections
            //// NOTE: not necessary for ConnectPolygonWContainedHolesTwice_Improved3, but for all others!!!
            //List<ValueTuple<int, int, int, int>> to_remove = new List<(int, int, int, int)>();
            //for(int i = 0; i < connectingLines_optimized.Count; i++)
            //{
            //    SimPoint3D p1, p2;
            //    if (connectingLines_optimized[i].Item1 == -1)
            //        p1 = polygon[connectingLines_optimized[i].Item2];
            //    else
            //        p1 = holes[connectingLines_optimized[i].Item1][connectingLines_optimized[i].Item2];

            //    if (connectingLines_optimized[i].Item3 == -1)
            //        p2 = polygon[connectingLines_optimized[i].Item4];
            //    else
            //        p2 = holes[connectingLines_optimized[i].Item3][connectingLines_optimized[i].Item4];

            //    for (int j = i + 1; j < connectingLines_optimized.Count; j++)
            //    {
            //        SimPoint3D q1, q2;
            //        if (connectingLines_optimized[j].Item1 == -1)
            //            q1 = polygon[connectingLines_optimized[j].Item2];
            //        else
            //            q1 = holes[connectingLines_optimized[j].Item1][connectingLines_optimized[j].Item2];

            //        if (connectingLines_optimized[j].Item3 == -1)
            //            q2 = polygon[connectingLines_optimized[j].Item4];
            //        else
            //            q2 = holes[connectingLines_optimized[j].Item3][connectingLines_optimized[j].Item4];

            //        bool inters = LineWLineCollision3D(p1, p2, q1, q2, GENERAL_CALC_TOLERANCE);
            //        if (inters)
            //            to_remove.Add(connectingLines_optimized[j]);
            //    }
            //}
            //foreach(var entry in to_remove)
            //{
            //    connectingLines_optimized.Remove(entry);
            //}

            return new List<ValueTuple<int, int, int, int>>(connectingLines_optimized);
        }


        //- holes != null
        //- polygon != null
        //- polygon.Count >= 3
        //- holes does not contain null
        //- all indices are within limits
        private static bool LineIsValidInPolygonWHoles(List<SimPoint3D> polygon, List<List<SimPoint3D>> holes,
                                                    int indPolygon, int indHole, int indInHole, bool when_aligned_check_for_overlap = false)
        {
            if (holes == null) //TODO Remove
                throw new Exception("DON'T, DON'T, DON'T EVER DO THIS");

            var hole = holes[indHole];

            // define the line
            var p1 = polygon[indPolygon];
            var p2 = hole[indInHole];
            bool intersectsSomething = false;

            // check for intersections w the polygon
            intersectsSomething = LineIntersectsPolygon(p1, p2, polygon, indPolygon, when_aligned_check_for_overlap);
            if (intersectsSomething)
                return false;

            // if no intersection, check if the line is inside or outside of the polygon
            bool p1IsInside = PointIsInsidePolygonXZ(polygon, p1);
            if (!p1IsInside)
                return false;

            // check for intersections w the hole
            intersectsSomething = LineIntersectsPolygon(p1, p2, hole, indInHole, when_aligned_check_for_overlap);
            if (intersectsSomething)
                return false;

            // check for intersections w all other holes
            for (int c = 0; c < holes.Count; c++)
            {
                if (c == indHole)
                    continue;

                intersectsSomething = LineIntersectsPolygon(p1, p2, holes[c], -1, when_aligned_check_for_overlap);
                if (intersectsSomething)
                    break;
            }

            return !intersectsSomething;
        }

        private static bool LineIsValidInPolygonWHoles(List<SimPoint3D> polygon, List<List<SimPoint3D>> holes,
                                                    int indHole1, int indInHole1, int indHole2, int indInHole2, bool when_aligned_check_for_overlap = false)
        {
            int n = polygon.Count;
            int nrH = holes.Count;

            var hole1 = holes[indHole1];
            if (hole1 == null)
                return false;
            var hole2 = holes[indHole2];
            if (hole2 == null)
                return false;

            int h1 = hole1.Count;
            if (indInHole1 < 0 || indInHole1 > (h1 - 1))
                return false;
            int h2 = hole2.Count;
            if (indInHole2 < 0 || indInHole2 > (h2 - 1))
                return false;

            // define the line
            var p1 = hole1[indInHole1];
            var p2 = hole2[indInHole2];
            bool intersectsSomething = false;

            // check for intersections w the polygon
            intersectsSomething = LineIntersectsPolygon(p1, p2, polygon, -1, when_aligned_check_for_overlap);
            if (intersectsSomething)
                return false;

            // if no intersection, check if the line is inside or outside of the polygon
            bool p1IsInside = PointIsInsidePolygonXZ(polygon, p1);
            bool p2IsInside = PointIsInsidePolygonXZ(polygon, p2);
            if (!p1IsInside || !p2IsInside)
                return false;

            // check for intersections w  hole 1
            intersectsSomething = LineIntersectsPolygon(p1, p2, hole1, indInHole1, when_aligned_check_for_overlap);
            if (intersectsSomething)
                return false;

            // check for intersections w  hole 2
            intersectsSomething = LineIntersectsPolygon(p1, p2, hole2, indInHole2, when_aligned_check_for_overlap);
            if (intersectsSomething)
                return false;

            // check for intersections w all other holes
            for (int c = 0; c < nrH; c++)
            {
                if (c != indHole1 && c != indHole2)
                {
                    intersectsSomething = LineIntersectsPolygon(p1, p2, holes[c], -1);
                    if (intersectsSomething)
                        break;
                }
            }

            return !intersectsSomething;

        }

        private static bool LineIntersectsPolygon(SimPoint3D p1, SimPoint3D p2, List<SimPoint3D> polygon, int exclIndices = -1, bool when_aligned_check_for_overlap = false)
        {
            int n = polygon.Count;

            // check added 17.05.2019
            var aligned_w_polygon = LineAlignedWithPolygon(p1, p2, polygon);
            if (aligned_w_polygon)
            {
                if (when_aligned_check_for_overlap)
                {
                    var overlaps_polygon = LineOverlapsWithPolygonAtEnds(p1, p2, polygon, LINEDISTCALC_TOLERANCE * 100);
                    if (overlaps_polygon)
                        return true;
                }
                else
                    return true;
            }


            for (int i = 0; i < n; i++)
            {
                if (exclIndices == i || exclIndices == (i + 1) % n)
                    continue;

                var intersects_polygon = LineWLineCollision3D(p1, p2, polygon[i], polygon[(i + 1) % n],
                                                GENERAL_CALC_TOLERANCE * 0.01);
                if (intersects_polygon)
                    return true;
            }

            return false;
        }


        private static bool LineWLineCollision3D(SimPoint3D p1, SimPoint3D p2, SimPoint3D p3, SimPoint3D p4, double _tolerance)
        {
            var intersectionResult = LineToLineShortestLine3D(p1, p2, p3, p4);
            if (intersectionResult.isValid)
            {
                double dAB = (intersectionResult.prB - intersectionResult.prA).LengthSquared;
                if (dAB < _tolerance)
                {
                    var d12 = (p2 - p1).LengthSquared;
                    var d1A = (intersectionResult.prA - p1).LengthSquared;
                    var d2A = (intersectionResult.prA - p2).LengthSquared;
                    if (_tolerance < d1A && d1A < d12 && _tolerance < d2A && d2A < d12)
                    {
                        var d34 = (p4 - p3).LengthSquared;
                        var d3B = (intersectionResult.prB - p3).LengthSquared;
                        var d4B = (intersectionResult.prB - p4).LengthSquared;
                        if (_tolerance < d3B && d3B < d34 && _tolerance < d4B && d4B < d34)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static (bool isValid, SimPoint3D prA, SimPoint3D prB) LineToLineShortestLine3D(SimPoint3D p1, SimPoint3D p2, SimPoint3D p3, SimPoint3D p4)
        {
            // on the shortest line connecting line 1 (defined by _p1 and _p2)
            // and line 2 (defined by _p3 and _p4): prA lies on line 1, prB lies on line 2

            SimVector3D v21 = p2 - p1;
            SimVector3D v13 = p1 - p3;
            SimVector3D v43 = p4 - p3;

            // stop if the lines are not well defined (i.e. definingpoints too close to each other)
            if (v21.LengthSquared < LINEDISTCALC_TOLERANCE || v43.LengthSquared < LINEDISTCALC_TOLERANCE)
                return (false, new SimPoint3D(), new SimPoint3D());

            //double d1343 = v13.X * (double)v43.X + v13.Y * (double)v43.Y + v13.Z * (double)v43.Z;
            //double d4321 = v43.X * (double)v21.X + v43.Y * (double)v21.Y + v43.Z * (double)v21.Z;
            //double d1321 = v13.X * (double)v21.X + v13.Y * (double)v21.Y + v13.Z * (double)v21.Z;
            //double d4343 = v43.X * (double)v43.X + v43.Y * (double)v43.Y + v43.Z * (double)v43.Z;
            //double d2121 = v21.X * (double)v21.X + v21.Y * (double)v21.Y + v21.Z * (double)v21.Z;

            // ignoring the y-coordinate
            double d1343 = v13.X * (double)v43.X + v13.Z * (double)v43.Z;
            double d4321 = v43.X * (double)v21.X + v43.Z * (double)v21.Z;
            double d1321 = v13.X * (double)v21.X + v13.Z * (double)v21.Z;
            double d4343 = v43.X * (double)v43.X + v43.Z * (double)v43.Z;
            double d2121 = v21.X * (double)v21.X + v21.Z * (double)v21.Z;

            double denom = d2121 * d4343 - d4321 * d4321;
            if (Math.Abs(denom) < LINEDISTCALC_TOLERANCE)
                return (false, new SimPoint3D(), new SimPoint3D());

            double numer = d1343 * d4321 - d1321 * d4343;

            double mua = numer / denom;
            double mub = (d1343 + d4321 * (mua)) / d4343;

            return (true, p1 + mua * v21, p3 + mub * v43);
        }

        private static bool PointIsInsidePolygonXZ(List<SimPoint3D> polygon, SimPoint3D p)
        {
            // source: http://conceptual-misfire.awardspace.com/point_in_polygon.htm
            // for polygons in the XZ- Plane (works with areas)
            SimPoint3D p1, p2;
            bool isInside = false;

            int n = polygon.Count;
            var oldPoint = polygon[n - 1];

            for (int i = 0; i < n; i++)
            {
                // my code: START
                // check for coinciding points first
                if (Math.Abs(oldPoint.X - p.X) <= GENERAL_CALC_TOLERANCE &&
                    Math.Abs(oldPoint.Z - p.Z) <= GENERAL_CALC_TOLERANCE)
                {
                    return true;
                }
                // my code: END

                var newPoint = polygon[i];
                if (newPoint.X > oldPoint.X)
                {
                    p1 = oldPoint;
                    p2 = newPoint;
                }
                else
                {
                    p1 = newPoint;
                    p2 = oldPoint;
                }

                if ((newPoint.X < p.X) == (p.X <= oldPoint.X) &&
                    (p.Z - p1.Z) * (p2.X - p1.X) < (p2.Z - p1.Z) * (p.X - p1.X))
                {
                    isInside = !isInside;
                }

                oldPoint = newPoint;
            }

            return isInside;
        }

        private static List<ValueTuple<int, int, int, int>> GetAdaptedConnections(List<ValueTuple<int, int, int, int>> original,
            List<ValueTuple<int, int, int, int>> to_add, List<ValueTuple<int, int, int, int>> to_remove)
        {
            List<ValueTuple<int, int, int, int>> result = new List<ValueTuple<int, int, int, int>>();
            result.AddRange(original);

            if (to_remove != null)
            {
                foreach (var v in to_remove)
                {
                    result.Remove(v);
                }
            }
            if (to_add != null)
                result.AddRange(to_add);

            return result;
        }

        private static (bool canInsert, ValueTuple<int, int, int, int> connection_to_remove) CanInsertConnectingLineInConnections(
            List<ValueTuple<int, int, int, int>> connecting_lines,
            ValueTuple<int, int, int, int> line, List<SimPoint3D> polygon, List<List<SimPoint3D>> holes)
        {
            if (connecting_lines.Count == 0)
                return (true, ValueTuple.Create(-1, -1, -1, -1));

            SimPoint3D q1, q2;
            if (line.Item1 == -1)
                q1 = polygon[line.Item2];
            else
                q1 = holes[line.Item1][line.Item2];

            if (line.Item3 == -1)
                q2 = polygon[line.Item4];
            else
                q2 = holes[line.Item3][line.Item4];

            bool admissible = false;
            bool exit = false;
            List<ValueTuple<int, int, int, int>> intersecting_connections = new List<ValueTuple<int, int, int, int>>();
            for (int o = 0; o < connecting_lines.Count; o++)
            {
                //TODO: The duplicate check is done at several locations. Find out if this can be only done once!!

                // 1. check for duplicates
                if ((connecting_lines[o].Item1 == line.Item1 && connecting_lines[o].Item2 == line.Item2 && connecting_lines[o].Item3 == line.Item3 && connecting_lines[o].Item4 == line.Item4) ||
                    (connecting_lines[o].Item1 == line.Item3 && connecting_lines[o].Item2 == line.Item4 && connecting_lines[o].Item3 == line.Item1 && connecting_lines[o].Item4 == line.Item2))
                {
                    admissible = false;
                    exit = true;
                    break;
                }

                //// 2. check for loops
                //if ((connecting_lines[o].Item1 == line.Item1 && connecting_lines[o].Item2 == line.Item2 && connecting_lines[o].Item3 == line.Item3) ||
                //    (connecting_lines[o].Item1 == line.Item3 && connecting_lines[o].Item2 == line.Item4 && connecting_lines[o].Item3 == line.Item1) ||
                //    (connecting_lines[o].Item3 == line.Item3 && connecting_lines[o].Item4 == line.Item4 && connecting_lines[o].Item1 == line.Item1) ||
                //    (connecting_lines[o].Item3 == line.Item1 && connecting_lines[o].Item4 == line.Item2 && connecting_lines[o].Item1 == line.Item3))
                //{
                //    admissible = false;
                //    intersecting_connections.Add(connecting_lines[o]);
                //}

                // 3. check for intersection
                SimPoint3D p1, p2;
                if (connecting_lines[o].Item1 == -1)
                    p1 = polygon[connecting_lines[o].Item2];
                else
                    p1 = holes[connecting_lines[o].Item1][connecting_lines[o].Item2];

                if (connecting_lines[o].Item3 == -1)
                    p2 = polygon[connecting_lines[o].Item4];
                else
                    p2 = holes[connecting_lines[o].Item3][connecting_lines[o].Item4];

                bool inters = LineWLineCollision3D(p1, p2, q1, q2, GENERAL_CALC_TOLERANCE * 0.01);

                if (inters)
                    intersecting_connections.Add(connecting_lines[o]);

                if (intersecting_connections.Count > 1)
                {
                    // too many intersections
                    admissible = false;
                    break;
                }

                admissible = true;
            }

            // check if some of the intersecting connections can be replaced by the tested line:
            // A. if it connects to an index in the polygon or an index in a hole that has not been used before
            // B. if the intersecting connecting line connects on BOTH sides to indices that have been used at least TWICE
            if (!exit && intersecting_connections.Count == 1)
            {
                // A
                Dictionary<int, List<int>> all_unused = GetAllUnusedIndices(connecting_lines, polygon, holes);
                if ((all_unused.ContainsKey(line.Item1) && all_unused[line.Item1].Contains(line.Item2)) ||
                    (all_unused.ContainsKey(line.Item3) && all_unused[line.Item3].Contains(line.Item4)))
                {
                    // B1 - the old line includes hole indices used twice or more
                    var old_line = intersecting_connections[0];
                    Dictionary<int, List<int>> all_used_twice_or_more = GetAllIndicesUsedMoreThanOnce(connecting_lines, polygon, holes);
                    if (all_used_twice_or_more.ContainsKey(old_line.Item1) && all_used_twice_or_more[old_line.Item1].Contains(old_line.Item2) &&
                        all_used_twice_or_more.ContainsKey(old_line.Item3) && all_used_twice_or_more[old_line.Item3].Contains(old_line.Item4))
                    {
                        return (true, old_line);
                    }
                    // B2 - the old line coincides with the new one at one end and the other invloves an index used twice or more
                    if ((((line.Item1 == old_line.Item1 && line.Item2 == old_line.Item2) || (line.Item3 == old_line.Item1 && line.Item4 == old_line.Item2)) &&
                        all_used_twice_or_more.ContainsKey(old_line.Item3) && all_used_twice_or_more[old_line.Item3].Contains(old_line.Item4)) ||
                        (((line.Item1 == old_line.Item3 && line.Item2 == old_line.Item4) || (line.Item3 == old_line.Item3 && line.Item4 == old_line.Item4)) &&
                        all_used_twice_or_more.ContainsKey(old_line.Item1) && all_used_twice_or_more[old_line.Item1].Contains(old_line.Item2)))
                    {
                        return (true, old_line);
                    }
                }
                return (false, ValueTuple.Create(-1, -1, -1, -1));
            }

            return (admissible, ValueTuple.Create(-1, -1, -1, -1));
        }

        private static Dictionary<int, List<int>> GetAllUnusedIndices(List<ValueTuple<int, int, int, int>> connecting_lines, List<SimPoint3D> polygon, List<List<SimPoint3D>> holes)
        {
            //List<int> all_polygon_and_hole_indices = connecting_lines.SelectMany(v => new List<int> { (int)v.Item1, (int)v.Item3 }).GroupBy(a => a).Select(gr => gr.First()).ToList();
            List<int> all_polygon_and_hole_indices = Enumerable.Range(-1, holes.Count + 1).ToList();
            Dictionary<int, List<int>> all_unused = new Dictionary<int, List<int>>();
            foreach (int index in all_polygon_and_hole_indices)
            {
                if (index == -1)
                {
                    all_unused.Add(-1, GetUnusedIndicesIn(connecting_lines, -1, polygon.Count));
                }
                else
                {
                    all_unused.Add(index, GetUnusedIndicesIn(connecting_lines, index, holes[index].Count));
                }
            }

            return all_unused;
        }

        private static (List<int> not_used, List<int> single_use, List<int> multiple_use) GetIndexUsage_Improved(List<ValueTuple<int, int, int, int>> connecting_lines, int nr_points_in_polygon, int nr_holes)
        {
            List<int> not_used = new List<int>();
            List<int> single_use = new List<int>();
            List<int> multiple_use = new List<int>();

            Dictionary<int, List<int>> usage = new Dictionary<int, List<int>>();
            for (int i = -1; i < nr_holes; i++)
            {
                usage.Add(i, new List<int>());
            }
            foreach (var cL in connecting_lines)
            {
                usage[cL.Item1].Add(cL.Item2);
                usage[cL.Item3].Add(cL.Item4);
            }
            for (int i = -1; i < nr_holes; i++)
            {
                int nr_used = usage[i].Distinct().Count();
                if (nr_used == 0)
                    not_used.Add(i);
                else if (nr_used == 1)
                    single_use.Add(i);
                else
                    multiple_use.Add(i);
            }

            return (not_used, single_use, multiple_use);
        }

        private static List<int> GetUnusedIndicesIn(List<ValueTuple<int, int, int, int>> _connecting_lines, int _polygon_or_hole, int _nr_vertices)
        {
            var used_w_repeats = _connecting_lines.Where(x => (x.Item1 == _polygon_or_hole || x.Item3 == _polygon_or_hole)).Select(x => (x.Item1 == _polygon_or_hole) ? x.Item2 : x.Item4);
            var used_wo_repeats = used_w_repeats.GroupBy(x => x).Select(gr => gr.First());

            List<int> all_indices = Enumerable.Range(0, _nr_vertices).ToList();
            List<int> unused_indices = all_indices.Where(x => !used_wo_repeats.Contains(x)).ToList();

            return unused_indices;
        }

        private static Dictionary<int, List<int>> GetAllIndicesUsedMoreThanOnce(List<ValueTuple<int, int, int, int>> connecting_lines, List<SimPoint3D> polygon, List<List<SimPoint3D>> holes)
        {
            List<int> all_polygon_and_hole_indices = connecting_lines.SelectMany(v => new List<int> { v.Item1, v.Item3 }).GroupBy(a => a).Select(gr => gr.First()).ToList();
            Dictionary<int, List<int>> all_used_twice_or_more = new Dictionary<int, List<int>>();
            foreach (int index in all_polygon_and_hole_indices)
            {
                if (index == -1)
                {
                    all_used_twice_or_more.Add(-1, GetIndicesUsedTwiceOrMoreIn(connecting_lines, -1, polygon.Count));
                }
                else
                {
                    all_used_twice_or_more.Add(index, GetIndicesUsedTwiceOrMoreIn(connecting_lines, index, holes[index].Count));
                }
            }

            return all_used_twice_or_more;
        }

        private static List<int> GetIndicesUsedTwiceOrMoreIn(List<ValueTuple<int, int, int, int>> connecting_lines, int polygon_or_hole, int nr_vertices)
        {
            var used_w_repeats = connecting_lines.Where(x => (x.Item1 == polygon_or_hole || x.Item3 == polygon_or_hole)).Select(x => (x.Item1 == polygon_or_hole) ? (int)x.Item2 : (int)x.Item4);
            var used_more_than_once = used_w_repeats.GroupBy(x => x).Where(gr => gr.Count() > 1).Select(gr => gr.First());

            return used_more_than_once.ToList();
        }

        private static List<List<ValueTuple<int, int, int, int>>> ExtractSplittingPathsFrom(List<ValueTuple<int, int, int, int>> connecting_lines, int nr_holes_to_split)
        {
            int nrCL = connecting_lines.Count;
            int nrH = nr_holes_to_split;
            List<int> holes_toSplit = Enumerable.Range(0, nrH).ToList();
            List<bool> connectingLines_used = Enumerable.Repeat(false, nrCL).ToList();

            List<List<ValueTuple<int, int, int, int>>> splitting_paths = new List<List<ValueTuple<int, int, int, int>>>();
            List<List<ValueTuple<int, int, int, int>>> discarded_paths = new List<List<ValueTuple<int, int, int, int>>>();
            List<int> discarded_connectingLines_used_entries = new List<int>();
            ////int debug = 0;
            while (holes_toSplit.Count > 0)
            {
                ////debug++;
                ////Debug.WriteLine("{0} holes to split: {1}", debug, holes_toSplit.Count);
                // look for a connected path of connecting lines 
                // that STARTS at the polygon, goes THROUGH a not yet split hole, and ENDS at the polygon
                // or a hole that has been split already
                List<ValueTuple<int, int, int, int>> splitting_path = new List<ValueTuple<int, int, int, int>>();
                // START
                bool reached_other_end = false;
                List<int> holes_to_remove_from_toSplit = new List<int>();
                for (int i = 0; i < nrCL; i++)
                {
                    if (connectingLines_used[i])
                        continue;

                    var candidate = connecting_lines[i];
                    bool candidate_viable = false;
                    if (candidate.Item1 == -1 || !holes_toSplit.Contains(candidate.Item1))
                    {
                        candidate_viable = true;
                    }
                    else if (candidate.Item3 == -1 || !holes_toSplit.Contains(candidate.Item3))
                    {
                        candidate = ValueTuple.Create(connecting_lines[i].Item3, connecting_lines[i].Item4, connecting_lines[i].Item1, connecting_lines[i].Item2);
                        candidate_viable = true;
                    }
                    if (candidate_viable)
                    {
                        splitting_path.Add(candidate);
                        connectingLines_used[i] = true;
                        if (!discarded_connectingLines_used_entries.Contains(i))
                            discarded_connectingLines_used_entries.Add(i);

                        int split_hole_ind = splitting_path[0].Item3;
                        if (holes_toSplit.Contains(split_hole_ind))
                        {
                            if (!holes_to_remove_from_toSplit.Contains(split_hole_ind))
                                holes_to_remove_from_toSplit.Add(split_hole_ind);
                            reached_other_end = false;
                        }
                        else
                            reached_other_end = true;

                        break;
                    }

                }

                // HOLES and END
                int nrSP = splitting_path.Count;
                if (nrSP == 0)
                    break;

                int maxNrIter = connecting_lines.Count;
                int counter_iterations = 0;

                List<int> holes_used_in_this_path = new List<int>();
                while (!reached_other_end && counter_iterations <= maxNrIter)
                {
                    counter_iterations++;
                    ////Debug.WriteLine("for");
                    ////foreach (var sP in splitting_path)
                    ////{
                    ////    Print(sP);
                    ////}

                    for (int i = 0; i < nrCL; i++)
                    {
                        if (connectingLines_used[i])
                            continue;

                        ////Debug.WriteLine("testing {0}", PrintString(connecting_lines[i]));

                        if (connecting_lines[i].Item1 == splitting_path[nrSP - 1].Item3 && connecting_lines[i].Item2 != splitting_path[nrSP - 1].Item4 &&
                            !holes_used_in_this_path.Contains(connecting_lines[i].Item3))
                        {
                            //if (splitting_path.Count > 0 && connecting_lines[i].Item3 == -1 && connecting_lines[i].Item4 == splitting_path[0].Item2)
                            if (splitting_path.Count > 0 && connecting_lines[i].Item3 == splitting_path[0].Item1 && connecting_lines[i].Item4 == splitting_path[0].Item2)
                            {
                                // same point as start -> do not take
                                continue;
                            }
                            splitting_path.Add(connecting_lines[i]);
                            holes_used_in_this_path.Add(connecting_lines[i].Item1);
                        }
                        else if (connecting_lines[i].Item3 == splitting_path[nrSP - 1].Item3 && connecting_lines[i].Item4 != splitting_path[nrSP - 1].Item4 &&
                                !holes_used_in_this_path.Contains(connecting_lines[i].Item1))
                        {
                            var conn_new = ValueTuple.Create(connecting_lines[i].Item3, connecting_lines[i].Item4,
                                                           connecting_lines[i].Item1, connecting_lines[i].Item2);
                            //if (splitting_path.Count > 0 && conn_new.Item3 == -1 && conn_new.Item4 == splitting_path[0].Item2)
                            if (splitting_path.Count > 0 && conn_new.Item3 == splitting_path[0].Item1 && conn_new.Item4 == splitting_path[0].Item2)
                            {
                                // same point as start -> do not take
                                continue;
                            }
                            splitting_path.Add(conn_new);
                            holes_used_in_this_path.Add(connecting_lines[i].Item3);
                        }
                        else
                            continue;

                        if (SplittingPathContainedIn(splitting_path, discarded_paths))
                        {
                            splitting_path.RemoveAt(splitting_path.Count - 1);
                            holes_used_in_this_path.RemoveAt(holes_used_in_this_path.Count - 1);
                            if (splitting_path.Count > 0)
                                continue;
                            else
                                break;
                        }

                        nrSP = splitting_path.Count;
                        connectingLines_used[i] = true;
                        if (!discarded_connectingLines_used_entries.Contains(i))
                            discarded_connectingLines_used_entries.Add(i);

                        int split_hole_ind = (int)splitting_path[nrSP - 1].Item3;
                        if (holes_toSplit.Contains(split_hole_ind))
                        {
                            if (!holes_to_remove_from_toSplit.Contains(split_hole_ind))
                                holes_to_remove_from_toSplit.Add(split_hole_ind);
                            reached_other_end = false;
                        }
                        else
                            reached_other_end = true;

                        break;
                    }

                }
                if (reached_other_end)
                {
                    foreach (int id in holes_to_remove_from_toSplit)
                    {
                        holes_toSplit.Remove(id);
                    }
                    if (!SplittingPathContainedIn(splitting_path, splitting_paths))
                    {
                        ////Debug.WriteLine("- - - - adding splitting path:");
                        ////foreach (var sp in splitting_path)
                        ////{
                        ////    Print(sp);
                        ////}
                        ////Debug.WriteLine("- - - - used connecting lines:");
                        ////for (int i = 0; i < connecting_lines.Count; i++)
                        ////{
                        ////    if (connectingLines_used[i])
                        ////        Print(connecting_lines[i]);
                        ////}

                        splitting_paths.Add(splitting_path);
                    }
                }
                else
                {
                    // roll-back
                    discarded_paths.Add(splitting_path);
                    ////Debug.WriteLine("DISCARDED PATH:");
                    ////foreach (var pE in splitting_path)
                    ////{
                    ////    Print(pE);
                    ////}
                    if (splitting_path.Count == 1)
                    {
                        discarded_connectingLines_used_entries.RemoveAt(discarded_connectingLines_used_entries.Count - 1);
                    }
                    foreach (int entry in discarded_connectingLines_used_entries)
                    {
                        ////Debug.WriteLine("rollback on {0}", entry);
                        connectingLines_used[entry] = false;
                    }
                }
            }

            // remove single-line paths contained in multi-line paths
            var singles = splitting_paths.Where(p => p.Count == 1).ToList();
            var multiples = splitting_paths.Where(p => p.Count > 1).ToList();
            foreach (var sp in singles)
            {
                foreach (var mp in multiples)
                {
                    if (EntryContainedInPath(sp[0], mp))
                        splitting_paths.Remove(sp);
                }
            }

            return splitting_paths;
        }

        private static bool SplittingPathEqual(List<ValueTuple<int, int, int, int>> _path_1, List<ValueTuple<int, int, int, int>> _path_2)
        {
            if (_path_1.Count != _path_2.Count)
                return false;

            for (int i = 0; i < _path_1.Count; i++)
            {
                if (_path_1[i].Item1 != _path_2[i].Item1 ||
                    _path_1[i].Item2 != _path_2[i].Item2 ||
                    _path_1[i].Item3 != _path_2[i].Item3 ||
                    _path_1[i].Item4 != _path_2[i].Item4)
                    return false;
            }

            return true;
        }

        private static bool SplittingPathContainedIn(List<ValueTuple<int, int, int, int>> _path, List<List<ValueTuple<int, int, int, int>>> _paths)
        {
            foreach (var test_path in _paths)
            {
                if (SplittingPathEqual(_path, test_path))
                    return true;
            }
            return false;
        }

        private static bool EntryContainedInPath(ValueTuple<int, int, int, int> _entry, List<ValueTuple<int, int, int, int>> _path_2)
        {
            for (int i = 0; i < _path_2.Count; i++)
            {
                if ((_entry.Item1 == _path_2[i].Item1 && _entry.Item2 == _path_2[i].Item2 &&
                    _entry.Item3 == _path_2[i].Item3 && _entry.Item4 == _path_2[i].Item4) ||
                    (_entry.Item3 == _path_2[i].Item1 && _entry.Item4 == _path_2[i].Item2 &&
                    _entry.Item1 == _path_2[i].Item3 && _entry.Item2 == _path_2[i].Item4))

                    return true;
            }
            return false;
        }

        private static List<ValueTuple<int, int>> GenerateDoubleIndices(int firstIndex, int secondIndexStart, int nrIndices)
        {
            List<ValueTuple<int, int>> indices = new List<ValueTuple<int, int>>();
            for (int i = 0; i < nrIndices; i++)
            {
                indices.Add(ValueTuple.Create(firstIndex, secondIndexStart + i));
            }

            return indices;
        }

        private static void SplitPolygonWHolesAlongPath(List<SimPoint3D> polygon, List<ValueTuple<int, int>> polyIndices,
                                                        List<ValueTuple<int, int, int, int>> splitting_path_ind, bool checkAdmissibility,
                                                        List<SimPoint3D> outer_polygon, List<List<SimPoint3D>> holes,
                                                    out List<SimPoint3D> polyA, out List<SimPoint3D> polyB,
                                                    out List<ValueTuple<int, int>> originalIndsA, out List<ValueTuple<int, int>> originalIndsB,
                                                    out bool inputValid)
        {
            polyA = new List<SimPoint3D>();
            polyB = new List<SimPoint3D>();
            originalIndsA = new List<ValueTuple<int, int>>();
            originalIndsB = new List<ValueTuple<int, int>>();
            inputValid = false;

            if (polygon == null || polyIndices == null || splitting_path_ind == null || splitting_path_ind.Count < 1)
                return;

            int n = polygon.Count;
            if (n != polyIndices.Count)
                return;
            List<int> outer_poly_indices = polyIndices.Where(x => x.Item1 == -1).Select(x => x.Item2).ToList();
            List<int> involved_hole_indices = polyIndices.Where(x => x.Item1 != -1).Select(x => x.Item1).ToList();


            int nrH = holes.Count;
            int nrSP = splitting_path_ind.Count;

            // check validity of the given path
            for (int p = 0; p < nrSP; p++)
            {
                ValueTuple<int, int> ind1 = ValueTuple.Create(splitting_path_ind[p].Item1, splitting_path_ind[p].Item2);
                ValueTuple<int, int> ind2 = ValueTuple.Create(splitting_path_ind[p].Item3, splitting_path_ind[p].Item4);

                // check if those are valid indices
                if (ind1.Item1 != -1)
                {
                    if (ind1.Item1 < 0 || ind1.Item1 > nrH - 1)
                        return;
                    if (ind1.Item2 < 0 || ind1.Item2 > holes[ind1.Item1].Count - 1)
                        return;
                }
                else
                {
                    if (!(outer_poly_indices.Contains(ind1.Item2)))
                        return;
                }
                if (ind2.Item1 != -1)
                {
                    if (ind2.Item1 < 0 || ind2.Item1 > nrH - 1)
                        return;
                    if (ind2.Item2 < 0 || ind2.Item2 > holes[ind2.Item1].Count - 1)
                        return;
                }
                else
                {
                    if (!(outer_poly_indices.Contains(ind2.Item2)))
                        return;
                }

                // check if the path segment is valid within the polygon and holes
                if (checkAdmissibility)
                {
                    bool isAdmissible = false;
                    if (ind1.Item1 == -1 && ind2.Item1 == -1)
                        isAdmissible = DiagonalIsAdmissible(polygon, polyIndices.IndexOf(ind1), polyIndices.IndexOf(ind2));
                    else if (ind1.Item1 == -1 && ind2.Item1 != -1)
                        isAdmissible = LineIsValidInPolygonWHoles(polygon, holes, polyIndices.IndexOf(ind1), ind2.Item1, ind2.Item2, true);
                    else if (ind1.Item1 != -1 && ind2.Item1 == -1)
                        isAdmissible = LineIsValidInPolygonWHoles(polygon, holes, polyIndices.IndexOf(ind2), ind1.Item1, ind1.Item2, true);
                    else
                        isAdmissible = LineIsValidInPolygonWHoles(polygon, holes, ind1.Item1, ind1.Item2, ind2.Item1, ind2.Item2, true);

                    if (!isAdmissible)
                    {
                        polyA = new List<SimPoint3D>(polygon);
                        originalIndsA = new List<ValueTuple<int, int>>(polyIndices);
                        return;
                    }
                }

                // check if the path lies within the polygon to be split
                SimPoint3D start = (ind1.Item1 == -1) ? outer_polygon[ind1.Item2] : holes[ind1.Item1][ind1.Item2];
                SimPoint3D end = (ind2.Item1 == -1) ? outer_polygon[ind2.Item2] : holes[ind2.Item1][ind2.Item2];
                SimPoint3D middle = new SimPoint3D(start.X * 0.5 + end.X * 0.5, start.Y * 0.5 + end.Y * 0.5, start.Z * 0.5 + end.Z * 0.5);
                bool start_inside = PointIsInsidePolygonXZ(polygon, start);
                bool end_inside = PointIsInsidePolygonXZ(polygon, end);
                bool middle_inside = PointIsInsidePolygonXZ(polygon, middle);
                if (!start_inside || !end_inside || !middle_inside)
                {
                    polyA = new List<SimPoint3D>(polygon);
                    originalIndsA = new List<ValueTuple<int, int>>(polyIndices);
                    return;
                }
            }

            //perform actual split
            for (int p = 0; p < nrSP; p++)
            {
                // add subpolygons
                var split_ind1 = ValueTuple.Create(splitting_path_ind[p].Item3, splitting_path_ind[p].Item4);
                var split_ind2 = ValueTuple.Create(splitting_path_ind[(p + 1) % nrSP].Item1, splitting_path_ind[(p + 1) % nrSP].Item2);
                bool splittingOuterMost = polyIndices.Contains(split_ind1);
                if (splittingOuterMost)
                {
                    if (!polyIndices.Contains(split_ind2))
                        return;
                }
                else
                {
                    if (split_ind1.Item1 != split_ind2.Item1)
                        return;
                }

                List<SimPoint3D> toSplit_chain;
                List<ValueTuple<int, int>> toSplit_chain_ind;
                if (splittingOuterMost)
                {
                    toSplit_chain = new List<SimPoint3D>(polygon);
                    toSplit_chain_ind = new List<ValueTuple<int, int>>(polyIndices);
                }
                else
                {
                    toSplit_chain = new List<SimPoint3D>(holes[split_ind1.Item1]);
                    toSplit_chain_ind = GenerateDoubleIndices(split_ind1.Item1, 0, toSplit_chain.Count);
                }

                int h = toSplit_chain.Count;
                int split_start_ind = toSplit_chain_ind.IndexOf(split_ind1);
                int split_end_ind = toSplit_chain_ind.IndexOf(split_ind2);

                List<SimPoint3D> for_polyA = new List<SimPoint3D>();
                List<ValueTuple<int, int>> for_originalIndsA = new List<ValueTuple<int, int>>();
                List<SimPoint3D> for_polyB = new List<SimPoint3D>();
                List<ValueTuple<int, int>> for_originalIndsB = new List<ValueTuple<int, int>>();

                int split_current_ind = split_start_ind;
                while (split_current_ind != split_end_ind)
                {
                    for_polyA.Add(toSplit_chain[split_current_ind]);
                    for_originalIndsA.Add(toSplit_chain_ind[split_current_ind]);
                    split_current_ind = (split_current_ind + 1) % h;
                }
                for_polyA.Add(toSplit_chain[split_end_ind]);
                for_originalIndsA.Add(toSplit_chain_ind[split_end_ind]);

                // subpolygon B
                split_current_ind = split_end_ind;
                while (split_current_ind != split_start_ind)
                {
                    for_polyB.Add(toSplit_chain[split_current_ind]);
                    for_originalIndsB.Add(toSplit_chain_ind[split_current_ind]);
                    split_current_ind = (split_current_ind + 1) % h;
                }
                for_polyB.Add(toSplit_chain[split_start_ind]);
                for_originalIndsB.Add(toSplit_chain_ind[split_start_ind]);

                if (!splittingOuterMost)
                {
                    polyA.AddRange(for_polyA);
                    originalIndsA.AddRange(for_originalIndsA);
                    polyB.InsertRange(0, for_polyB);
                    originalIndsB.InsertRange(0, for_originalIndsB);
                }
                else
                {
                    for_polyA.Reverse();
                    for_polyB.Reverse();
                    for_originalIndsA.Reverse();
                    for_originalIndsB.Reverse();

                    polyA.AddRange(for_polyB);
                    originalIndsA.AddRange(for_originalIndsB);
                    polyB.InsertRange(0, for_polyA);
                    originalIndsB.InsertRange(0, for_originalIndsA);
                }

            }

            polyA.Reverse();
            originalIndsA.Reverse();
            polyB.Reverse();
            originalIndsB.Reverse();

            inputValid = true;
        }

        private static bool DiagonalIsAdmissible(List<SimPoint3D> polygon, int startInd, int endInd)
        {
            // index out of bounds
            int n = polygon.Count;
            if (startInd < 0 || startInd > n - 1 || endInd < 0 || startInd > n - 1) //TODO: Is that even correct?
                throw new Exception("CHECK THAT BEFORE!!"); //TODO: remove

            int minInd = Math.Min(startInd, endInd);
            int maxInd = Math.Max(startInd, endInd);

            // consecutive indices -> not a diagonal
            if (minInd == (maxInd - 1) % n)
                return false;

            // test for intersections
            var d1 = polygon[minInd];
            var d2 = polygon[maxInd];

            for (int i = 0; i < n; i++)
            {
                if (i == minInd || i == maxInd)
                    continue;
                if ((i + 1) % n == minInd || (i + 1) % n == maxInd)
                    continue;

                // exclude points at another index that coincide with the start or end points (double points)
                double dist1 = DistV3Simple(polygon[i], d1);
                double dist2 = DistV3Simple(polygon[i], d2);
                double dist3 = DistV3Simple(polygon[(i + 1) % n], d1);
                double dist4 = DistV3Simple(polygon[(i + 1) % n], d2);
                if (dist1 < GENERAL_CALC_TOLERANCE || dist2 < GENERAL_CALC_TOLERANCE ||
                    dist3 < GENERAL_CALC_TOLERANCE || dist4 < GENERAL_CALC_TOLERANCE)
                {
                    continue;
                }

                var (intersects_polygon, colPos) = LineWLineCollision3D_InclAtEnds(d1, d2, polygon[i], polygon[(i + 1) % n],
                                                GENERAL_CALC_TOLERANCE * 100);

                if (intersects_polygon)
                    return false;
            }

            // if no intersection, check if the diagonal is inside or outside of the polygon
            // if inside  -> winding direction of both subpolygons the same as that of the big polygon
            // if outside -> winding directions of the subpolygons differ from each other

            List<SimPoint3D> subpoly1 = new List<SimPoint3D>();
            List<SimPoint3D> subpoly2 = new List<SimPoint3D>();
            for (int i = 0; i < n; i++)
            {
                if (i <= minInd || i >= maxInd)
                    subpoly1.Add(polygon[i]);
                if (minInd <= i && i <= maxInd)
                    subpoly2.Add(polygon[i]);
            }

            var subpoly1_cw = CalculateIfPolygonClockWise(subpoly1, GENERAL_CALC_TOLERANCE);
            var subpoly2_cw = CalculateIfPolygonClockWise(subpoly2, GENERAL_CALC_TOLERANCE);

            if (subpoly1_cw == TriangulationOrientation.Invalid || subpoly2_cw == TriangulationOrientation.Invalid)
                return false;

            return (subpoly1_cw == subpoly2_cw);
        }

        private static double DistV3Simple(SimPoint3D v1, SimPoint3D v2)
        {
            double dX = Math.Abs(v1.X - v2.X);
            double dY = 0; // Math.Abs(v1.Y - v2.Y); // ignore the y-coordinate
            double dZ = Math.Abs(v1.Z - v2.Z);

            double dMax = Math.Max(dX, Math.Max(dY, dZ));
            return dMax;
        }

        private static (bool hasCollision, SimPoint3D collisionPoint) LineWLineCollision3D_InclAtEnds(SimPoint3D p1, SimPoint3D p2, SimPoint3D p3, SimPoint3D p4,
                                                double tolerance)
        {
            var (success, prA, prB) = LineToLineShortestLine3D(p1, p2, p3, p4);
            if (success)
            {
                // var dAB = (prB - prA).LengthSquared; // OLD: takes the y-coordinate into account                
                var dAB = (new SimPoint3D(prB.X, 0, prB.Z) - new SimPoint3D(prA.X, 0, prA.Z)).LengthSquared; // NEW: ignores the y-coordinate
                if (dAB < tolerance)
                {
                    var d12 = (p2 - p1).LengthSquared;
                    var d1A = (prA - p1).LengthSquared;
                    var d2A = (prA - p2).LengthSquared;
                    if (0 <= d1A && d1A <= d12 && 0 <= d2A && d2A <= d12)
                    //if (d1A <= d12 + LINEDISTCALC_TOLERANCE && d2A <= d12 + LINEDISTCALC_TOLERANCE)
                    {
                        var d34 = (p3 - p4).LengthSquared;
                        var d3B = (prB - p3).LengthSquared;
                        var d4B = (prB - p4).LengthSquared;
                        if (0 <= d3B && d3B <= d34 && 0 <= d4B && d4B <= d34)
                        //if (d3B <= d34 + LINEDISTCALC_TOLERANCE && d4B <= d34 + LINEDISTCALC_TOLERANCE)
                        {
                            return (true, prA);
                        }
                    }
                }

            }
            return (false, new SimPoint3D());
        }

        private static List<List<SimPoint3D>> DecomposeInMonotonePolygons(List<SimPoint3D> polygon)
        {
            // order the vertices according to the X component
            int n = polygon.Count;
            SimPoint3DComparer pointComparer = new SimPoint3DComparer();
            SortedList<SimPoint3D, int> vertices_ordered = new SortedList<SimPoint3D, int>(pointComparer);
            for (int i = 0; i < n; i++)
            {
                if (vertices_ordered.ContainsKey(polygon[i]))
                    continue;

                try
                {
                    vertices_ordered.Add(polygon[i], i + 1);
                }
                catch (ArgumentException)
                {
                    // if the same vertex occurs more than once, just skip it
                    continue;
                }
            }

            // traverse the polygon in X-direction to determine the split diagonals
            // leave out the start and end points
            int m = vertices_ordered.Count;
            List<ValueTuple<int, int>> splitIndices = new List<ValueTuple<int, int>>();
            for (int j = 1; j < m - 1; j++)
            {
                var current_alongX = vertices_ordered.ElementAt(j).Key;
                int ind_current_alongX = vertices_ordered.ElementAt(j).Value - 1;

                var prev = polygon[(n + ind_current_alongX - 1) % n];
                var next = polygon[(ind_current_alongX + 1) % n];


                if (prev.X <= current_alongX.X && next.X <= current_alongX.X)
                {
                    // MERGE VERTEX -> split polygon along the current vertex and the NEXT one along the X-axis
                    for (int c = 1; c < m - j; c++)
                    {
                        var next_alongX = vertices_ordered.ElementAt(j + c).Key;
                        var ind_next_alongX = vertices_ordered.ElementAt(j + c).Value - 1;
                        if (next_alongX.X > current_alongX.X)
                        {
                            // check if the diagonal is valid
                            bool isAdmissible = DiagonalIsAdmissible(polygon, ind_current_alongX, ind_next_alongX);
                            bool isAligned = (AreConsecutiveIndices(ind_current_alongX, ind_next_alongX, m)) ? false : LineOverlapsWithPolygonAtEnds(polygon[ind_current_alongX], polygon[ind_next_alongX], polygon, LINEDISTCALC_TOLERANCE * 100);
                            bool isAligned2 = LineNearlyOverlapsWithPolygon(polygon[ind_current_alongX], polygon[ind_next_alongX], polygon);
                            if (isAdmissible && !isAligned && !isAligned2)
                            {
                                splitIndices.Add(ValueTuple.Create(ind_current_alongX, ind_next_alongX));
                                break;
                            }
                        }
                    }
                }
                else if (prev.X >= current_alongX.X && next.X >= current_alongX.X)
                {
                    // SPLIT VERTEX -> split polygon along the current vertex and the PERVIOUS one along the X-axis
                    for (int c = 1; c < j + 1; c++)
                    {
                        var prev_alongX = vertices_ordered.ElementAt(j - c).Key;
                        var ind_prev_alongX = vertices_ordered.ElementAt(j - c).Value - 1;
                        if (prev_alongX.X < current_alongX.X)
                        {
                            // check if the diagonal is valid
                            bool isAdmissible = DiagonalIsAdmissible(polygon, ind_current_alongX, ind_prev_alongX);
                            bool isAligned = (AreConsecutiveIndices(ind_current_alongX, ind_prev_alongX, m)) ? false : LineOverlapsWithPolygonAtEnds(polygon[ind_current_alongX], polygon[ind_prev_alongX], polygon, LINEDISTCALC_TOLERANCE * 100);
                            bool isAligned2 = LineNearlyOverlapsWithPolygon(polygon[ind_current_alongX], polygon[ind_prev_alongX], polygon);
                            if (isAdmissible && !isAligned && !isAligned2)
                            {
                                splitIndices.Add(ValueTuple.Create(ind_current_alongX, ind_prev_alongX));
                                break;
                            }
                        }
                    }
                }

            }

            // split the polygon along the saved diagonals
            int d = splitIndices.Count;
            if (d == 0)
            {
                return new List<List<SimPoint3D>> { polygon };
            }

            // remove double split diagonal entries
            List<ValueTuple<int, int>> splitInideces_optimized = new List<ValueTuple<int, int>>();
            for (int a = 0; a < d; a++)
            {
                bool hasReversedDuplicate = false;
                for (int b = a + 1; b < d; b++)
                {
                    if (splitIndices[a].Item1 == splitIndices[b].Item2 && splitIndices[a].Item2 == splitIndices[b].Item1)
                    {
                        hasReversedDuplicate = true;
                        break;
                    }
                }
                if (!hasReversedDuplicate)
                    splitInideces_optimized.Add(splitIndices[a]);
            }
            splitIndices = new List<ValueTuple<int, int>>(splitInideces_optimized);
            d = splitIndices.Count;

            // perform the actual splitting of the polygon
            List<SimPoint3D> poly = new List<SimPoint3D>(polygon);
            List<int> polyIndices = Enumerable.Range(0, n).ToList();

            List<List<SimPoint3D>> list_before_Split_polys = new List<List<SimPoint3D>>();
            list_before_Split_polys.Add(poly);
            List<List<int>> list_brefore_Split_inds = new List<List<int>>();
            list_brefore_Split_inds.Add(polyIndices);

            List<List<SimPoint3D>> list_after_Split_polys = new List<List<SimPoint3D>>();
            List<List<int>> list_after_Split_inds = new List<List<int>>();

            for (int j = 0; j < d; j++)
            {
                int nrToSplit = list_before_Split_polys.Count;
                for (int k = 0; k < nrToSplit; k++)
                {
                    List<SimPoint3D> polyA, polyB;
                    List<int> originalIndsA, originalIndsB;
                    SplitPolygonAlongDiagonal(list_before_Split_polys[k], list_brefore_Split_inds[k],
                                              splitIndices[j].Item1, splitIndices[j].Item2, true,
                                              out polyA, out polyB, out originalIndsA, out originalIndsB);

                    if (polyA.Count > 2 && polyB.Count > 2)
                    {
                        // successful split
                        list_after_Split_polys.Add(polyA);
                        list_after_Split_inds.Add(originalIndsA);
                        list_after_Split_polys.Add(polyB);
                        list_after_Split_inds.Add(originalIndsB);
                    }
                    else
                    {
                        // no split
                        list_after_Split_polys.Add(list_before_Split_polys[k]);
                        list_after_Split_inds.Add(list_brefore_Split_inds[k]);
                    }
                }
                // swap lists
                list_before_Split_polys = new List<List<SimPoint3D>>(list_after_Split_polys);
                list_brefore_Split_inds = new List<List<int>>(list_after_Split_inds);
                list_after_Split_polys = new List<List<SimPoint3D>>();
                list_after_Split_inds = new List<List<int>>();
            }


            return list_before_Split_polys;

        }

        private static void SplitPolygonAlongDiagonal(List<SimPoint3D> polygon, List<int> polyIndices,
                                                     int ind1, int ind2, bool checkAdmissibility,
                                                 out List<SimPoint3D> polyA, out List<SimPoint3D> polyB,
                                                 out List<int> originalIndsA, out List<int> originalIndsB)
        {
            polyA = new List<SimPoint3D>();
            polyB = new List<SimPoint3D>();
            originalIndsA = new List<int>();
            originalIndsB = new List<int>();

            int n = polygon.Count;
            int minInd = Math.Min(ind1, ind2);
            int maxInd = Math.Max(ind1, ind2);
            int test1 = polyIndices.IndexOf(minInd);
            int test2 = polyIndices.IndexOf(maxInd);

            if (n < 4 || n != polyIndices.Count || test1 == -1 || test2 == -1 ||
                minInd < 0 || maxInd < 0 || minInd == maxInd || minInd + 1 == maxInd)
            {
                polyA = new List<SimPoint3D>(polygon);
                originalIndsA = new List<int>(polyIndices);
                return;
            }
            if (checkAdmissibility)
            {
                bool isAdmissible = DiagonalIsAdmissible(polygon, test1, test2);
                if (!isAdmissible)
                {
                    polyA = new List<SimPoint3D>(polygon);
                    originalIndsA = new List<int>(polyIndices);
                    return;
                }
            }

            // perform actual split
            for (int i = 0; i < n; i++)
            {
                int index = polyIndices[i];
                if (index <= minInd || index >= maxInd)
                {
                    polyA.Add(polygon[i]);
                    originalIndsA.Add(index);
                }
                if (index >= minInd && index <= maxInd)
                {
                    polyB.Add(polygon[i]);
                    originalIndsB.Add(index);
                }
            }
        }

        private static (List<SimPoint3D> positions, List<int> indices) PolygonFillMonotone(List<SimPoint3D> polygon, bool reverse)
        {
            // extract info about the polygon
            var orientation = CalculateIfPolygonClockWise(polygon, GENERAL_CALC_TOLERANCE);
            if (orientation == TriangulationOrientation.Invalid)
                return (null, null);

            // order the vertices according to the X component            
            int n = polygon.Count;
            SimPoint3DComparer pointComparer = new SimPoint3DComparer(); //TODO: The same sorted list is created in multiple places
            SortedList<SimPoint3D, int> vertices_ordered = new SortedList<SimPoint3D, int>(pointComparer);
            for (int i = 0; i < n; i++)
            {
                if (vertices_ordered.ContainsKey(polygon[i]))
                    continue;

                try
                {
                    vertices_ordered.Add(polygon[i], i + 1);
                }
                catch (ArgumentException)
                {
                    // if the same vertex occurs more than once, just skip it
                    continue;
                }
            }

            n = vertices_ordered.Count;

            if (n == 3)
            {
                List<SimPoint3D> tri = new List<SimPoint3D>(vertices_ordered.Keys);

                var orientation3 = CalculateIfPolygonClockWise(tri, GENERAL_CALC_TOLERANCE);
                if (orientation3 == TriangulationOrientation.Invalid)
                    return (null, null);

                if (orientation != orientation3)
                    tri = ReversePolygon(tri);

                return PolygonFillSimpleOptimized(tri, false); // reverse
            }

            // and determine to which chain (upper = 1, lower = -1, both = 0) they belong            
            List<int> vertices_in_chain = Enumerable.Repeat(0, polygon.Count).ToList();

            int chain_startInd = Math.Min(vertices_ordered.ElementAt(0).Value, vertices_ordered.ElementAt(n - 1).Value) - 1;
            int chain_endInd = Math.Max(vertices_ordered.ElementAt(0).Value, vertices_ordered.ElementAt(n - 1).Value) - 1;
            for (int i = 0; i < polygon.Count; i++)
            {
                if (i < chain_startInd || i > chain_endInd)
                    vertices_in_chain[i] = 1;
                else if (chain_startInd < i && i < chain_endInd)
                    vertices_in_chain[i] = -1;
            }

            // ALGORITHM
            Stack<SimPoint3D> to_process = new Stack<SimPoint3D>();
            // 1. push first 2 vertices onto stack
            to_process.Push(vertices_ordered.ElementAt(0).Key);
            to_process.Push(vertices_ordered.ElementAt(1).Key);

            List<SimPoint3D> positions = new List<SimPoint3D>();
            List<int> indices = new List<int>();

            // 2. check the vertices moving along the X axis
            for (int i = 2; i < n; i++)
            {
                if (to_process.Count < 1)
                    break;

                var current = vertices_ordered.ElementAt(i).Key;
                int current_Ind = vertices_ordered.ElementAt(i).Value - 1;
                var topOfStack = to_process.Peek();
                int topOfStack_Ind = vertices_ordered[topOfStack] - 1;

                if (vertices_in_chain[current_Ind] == vertices_in_chain[topOfStack_Ind])
                {
                    // 3A. if on the SAME chain: add diagonals as long as they are admissible
                    while (to_process.Count > 1)
                    {
                        var last_on_stack = to_process.Pop();
                        int last_on_stack_Ind = vertices_ordered[last_on_stack] - 1;
                        var before_last_on_stack = to_process.Peek();
                        int before_last_on_stack_Ind = vertices_ordered[before_last_on_stack] - 1;
                        if (DiagonalIsAdmissible(polygon, current_Ind, before_last_on_stack_Ind))
                        {
                            // 4AA. add triangle
                            var tr_isCW = CalculateIfPolygonClockWise(new List<SimPoint3D> { current, last_on_stack, before_last_on_stack },
                                GENERAL_CALC_TOLERANCE);

                            if (tr_isCW == orientation)
                            {
                                positions.Add(current);
                                positions.Add(last_on_stack);
                                positions.Add(before_last_on_stack);
                            }
                            else
                            {
                                positions.Add(current);
                                positions.Add(before_last_on_stack);
                                positions.Add(last_on_stack);
                            }

                            int nrInd = indices.Count;
                            indices.Add(nrInd);
                            //if (reverse)
                            //{
                            //    indices.Add(nrInd + 2);
                            //    indices.Add(nrInd + 1);
                            //}
                            //else
                            {
                                indices.Add(nrInd + 1);
                                indices.Add(nrInd + 2);
                            }
                        }
                        else
                        {
                            // 4AB. push the top of the stack back on
                            to_process.Push(last_on_stack);
                            break;
                        }
                    }
                    // 5. put current on the stack:
                    to_process.Push(current);
                }
                else
                {
                    // 3B. if on DIFFERENT chains:
                    // pop all vertices from the stack and add the corresponding diagonals
                    var top_of_stack = to_process.Peek();
                    while (to_process.Count > 1)
                    {
                        var last_on_stack = to_process.Pop();
                        var before_last_on_stack = to_process.Peek();

                        // 4. add triangle   
                        var tr_isCW = CalculateIfPolygonClockWise(new List<SimPoint3D> { current, last_on_stack, before_last_on_stack },
                            GENERAL_CALC_TOLERANCE);

                        if (tr_isCW == orientation)
                        {
                            positions.Add(current);
                            positions.Add(last_on_stack);
                            positions.Add(before_last_on_stack);
                        }
                        else
                        {
                            positions.Add(current);
                            positions.Add(before_last_on_stack);
                            positions.Add(last_on_stack);
                        }

                        int nrInd = indices.Count;
                        indices.Add(nrInd);
                        //if (reverse)
                        //{
                        //    indices.Add(nrInd + 2);
                        //    indices.Add(nrInd + 1);
                        //}
                        //else
                        {
                            indices.Add(nrInd + 1);
                            indices.Add(nrInd + 2);
                        }
                    }
                    // 4B. save to stack: the previous top of the stack and the current point
                    to_process.Clear();
                    to_process.Push(top_of_stack);
                    to_process.Push(current);
                }
            }

            return (positions, indices);
        }

        internal static (List<SimPoint3D> positions, List<int> indices) CombineMeshes(
            List<(List<SimPoint3D> positions, List<int> indices)> meshGs
            )
        {
            int nrMeshes = meshGs.Count;
            if (nrMeshes == 1)
                return meshGs[0];

            List<SimPoint3D> allPositions = new List<SimPoint3D>();
            List<int> allIndices = new List<int>();
            int posOffset = 0;

            for (int i = 0; i < nrMeshes; i++)
            {
                var mesh = meshGs[i];

                // copy positions and normals and texture coordinates
                allPositions.AddRange(mesh.positions);

                // copy and offset indices into the position and normal arrays               
                for (int j = 0; j < mesh.indices.Count(); j++)
                {
                    allIndices.Add(mesh.indices[j] + posOffset);
                }
                posOffset += mesh.positions.Count();
            }

            return (allPositions, allIndices);
        }

        #region Comparer

        private class SimPoint3DComparer : IComparer<SimPoint3D>
        {
            private double tolerance; // raster step
            public SimPoint3DComparer(double tolerance = 0.0)
            {
                this.tolerance = tolerance;
            }

            public int Compare(SimPoint3D v1, SimPoint3D v2)
            {
                // ADAPTED VERSION
                bool sameX = Math.Abs(v1.X - v2.X) <= this.tolerance;

                if (sameX)
                {
                    bool sameZ = Math.Abs(v1.Z - v2.Z) <= this.tolerance;
                    if (sameZ)
                    {
                        //bool sameY = Math.Abs(v1.Y - v2.Y) <= this.tolerance;
                        //if (sameY)
                        //	return 0;
                        //else if (v1.Y > v2.Y)
                        //	return 1;
                        //else
                        //	return -1;
                        return 0;
                    }
                    else if (v1.Z > v2.Z)
                        return 1;
                    else
                        return -1;
                }
                else if (v1.X > v2.X)
                    return 1;
                else
                    return -1;
            }
        }

        #endregion

        #region Alignment, Projection

        private static bool LineAlignedWithPolygon(SimPoint3D p1, SimPoint3D p2, List<SimPoint3D> polygon)
        {
            SimVector3D vL = p2 - p1;
            vL.Normalize();

            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                //Debug.WriteLine("testing " + ToXZString(p1) + " and " + ToXZString(p2) + " on " + ToXZString(polygon[i]) + " and " + ToXZString(polygon[(i + 1) % n]));
                SimVector3D vP = polygon[(i + 1) % n] - polygon[i];
                vP.Normalize();

                //var debug = SimVector3D.DotProduct(vL, vP);
                var aligned = Math.Abs(SimVector3D.DotProduct(vL, vP)) > (1 - GENERAL_CALC_TOLERANCE);
                if (aligned)
                {
                    var p1_pr = NormalProject(p1, polygon[i], polygon[(i + 1) % n]);
                    var p2_pr = NormalProject(p2, polygon[i], polygon[(i + 1) % n]);
                    if (p1_pr.isInside || p2_pr.isInside)
                    {
                        if (p1_pr.distance < GENERAL_CALC_TOLERANCE * 10 &&
                            p2_pr.distance < GENERAL_CALC_TOLERANCE * 10)
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool LineOverlapsWithPolygonAtEnds(SimPoint3D p1, SimPoint3D p2, List<SimPoint3D> polygon, double overlap)
        {
            SimVector3D vL = p2 - p1;
            vL.Normalize();

            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                SimVector3D vP = polygon[(i + 1) % n] - polygon[i];
                vP.Normalize();

                var aligned = Math.Abs(SimVector3D.DotProduct(vL, vP)) > (1 - GENERAL_CALC_TOLERANCE);
                if (aligned)
                {
                    var p1_pr = NormalProject(p1, polygon[i], polygon[(i + 1) % n]);
                    var p2_pr = NormalProject(p2, polygon[i], polygon[(i + 1) % n]);
                    if (p1_pr.isInside || p2_pr.isInside)
                    {
                        if (p1_pr.distance < GENERAL_CALC_TOLERANCE * 10 &&
                            p2_pr.distance < GENERAL_CALC_TOLERANCE * 10)
                        {
                            // check for overlap
                            SimPoint3D p1o = p1 + overlap * vL;
                            SimPoint3D p2o = p2 - overlap * vL;
                            var p1o_pr = NormalProject(p1o, polygon[i], polygon[(i + 1) % n]);
                            var p2o_pr = NormalProject(p2o, polygon[i], polygon[(i + 1) % n]);
                            if (p1o_pr.isInside || p2o_pr.isInside)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private static bool LineNearlyOverlapsWithPolygon(SimPoint3D p1, SimPoint3D p2, List<SimPoint3D> polygon, double tolerance_factor = GENERAL_CALC_TOLERANCE * 10)
        {
            SimVector3D vL = p2 - p1;
            double tolerance = vL.Length * tolerance_factor;
            vL.Normalize();

            int n = polygon.Count;
            for (int i = 0; i < n; i++)
            {
                SimVector3D vP = polygon[(i + 1) % n] - polygon[i];
                vP.Normalize();

                var aligned = Math.Abs(SimVector3D.DotProduct(vL, vP)) > (1 - GENERAL_CALC_TOLERANCE);
                if (aligned)
                {
                    var p1_pr = NormalProject(p1, polygon[i], polygon[(i + 1) % n]);
                    var p2_pr = NormalProject(p2, polygon[i], polygon[(i + 1) % n]);
                    if (p1_pr.distance < tolerance && p2_pr.distance < tolerance)
                    {
                        double pr_lowerX = Math.Min(p1_pr.projection.X, p2_pr.projection.X);
                        double pr_upperX = Math.Max(p1_pr.projection.X, p2_pr.projection.X);

                        double poly_lowerX = Math.Min(polygon[i].X, polygon[(i + 1) % n].X);
                        double poly_upperX = Math.Max(polygon[i].X, polygon[(i + 1) % n].X);

                        if (!(poly_upperX < pr_lowerX + GENERAL_CALC_TOLERANCE || pr_upperX < poly_lowerX + GENERAL_CALC_TOLERANCE))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        private static (SimPoint3D projection, double distance, bool isInside) NormalProject(SimPoint3D p, SimPoint3D q0, SimPoint3D q1)
        {
            SimVector3D v0 = q1 - q0;
            SimVector3D vP = p - q0;
            if (v0.Length < GENERAL_CALC_TOLERANCE)
                return (q0, Distance(p, q0), true);

            if (vP.Length < GENERAL_CALC_TOLERANCE)
            {
                v0 = q0 - q1;
                vP = p - q1;
                if (vP.Length < GENERAL_CALC_TOLERANCE)
                    return (q1, 0.0, true);

                SimVector3D e0 = q0 - q1;
                SimVector3D eP = p - q1;
                e0.Normalize();
                eP.Normalize();

                if (Math.Abs(SimVector3D.DotProduct(e0, eP)) > (1 - GENERAL_CALC_TOLERANCE * 0.01))
                {
                    //    return (p, 0.0, CollinearIsPointBetweenPoints(p, q0, q1));
                }

                // project vP onto v0
                SimPoint3D pPr = q1 + SimVector3D.DotProduct(e0, eP) * vP.Length * e0;
                return (pPr, Distance(p, pPr), CollinearIsPointBetweenPoints(pPr, q0, q1));
            }
            else
            {
                SimVector3D e0 = q1 - q0;
                SimVector3D eP = p - q0;
                e0.Normalize();
                eP.Normalize();

                if (Math.Abs(SimVector3D.DotProduct(e0, eP)) > (1 - GENERAL_CALC_TOLERANCE * 0.01))
                {
                    //    return (p, 0.0, CollinearIsPointBetweenPoints(p, q0, q1));
                }

                // project vP onto v0
                SimPoint3D pPr = q0 + SimVector3D.DotProduct(e0, eP) * vP.Length * e0;
                return (pPr, Distance(p, pPr), CollinearIsPointBetweenPoints(pPr, q0, q1));
            }
        }

        private static bool CollinearIsPointBetweenPoints(SimPoint3D p, SimPoint3D q0, SimPoint3D q1)
        {
            bool inside = (q0.X < q1.X) ? (q0.X <= p.X && p.X <= q1.X) : (q1.X <= p.X && p.X <= q0.X);
            inside &= (q0.Y < q1.Y) ? (q0.Y <= p.Y && p.Y <= q1.Y) : (q1.Y <= p.Y && p.Y <= q0.Y);
            inside &= (q0.Z < q1.Z) ? (q0.Z <= p.Z && p.Z <= q1.Z) : (q1.Z <= p.Z && p.Z <= q0.Z);
            return inside;
        }

        private static double Distance(SimPoint3D p1, SimPoint3D p2)
        {
            SimVector3D v = p2 - p1;
            return v.Length;
        }

        private static (int, int) FindClosestHoles(List<List<SimPoint3D>> holes, int index_of_hole)
        {
            Dictionary<int, SimPoint3D> pivots = new Dictionary<int, SimPoint3D>();
            int counter = 0;
            foreach (List<SimPoint3D> hole in holes)
            {
                SimVector3D center = new SimVector3D(0, 0, 0);
                for (int i = 0; i < hole.Count; i++)
                {
                    center += (SimVector3D)hole[i];
                }
                center /= hole.Count;

                pivots.Add(counter, (SimPoint3D)center);
                counter++;
            }

            // order the holes according to the distance from the hole of the given index
            int n = pivots.Count;
            SortedList<double, int> holes_ordered = new SortedList<double, int>();
            for (int i = 0; i < n; i++)
            {
                double sd = (pivots[i] - pivots[index_of_hole]).LengthSquared;
                if (holes_ordered.ContainsKey(sd))
                    continue;

                try
                {
                    holes_ordered.Add(sd, i);
                }
                catch (ArgumentException)
                {
                    // if the same distance occurs more than once, just skip it, unless it is the the hole
                    if (i == index_of_hole)
                        holes_ordered[sd] = index_of_hole;
                    continue;
                }
            }

            // find neighbors (the given hole itself should be at the start of the list with distance 0
            if (n >= 3)
            {
                return (holes_ordered.ElementAt(1).Value, holes_ordered.ElementAt(2).Value);
            }
            else if (n >= 2)
            {
                return (holes_ordered.ElementAt(1).Value, -1);
            }
            else
            {
                return (-1, -1);
            }
        }

        #endregion

        #region Info

        private static bool AreConsecutiveIndices(int index1, int index2, int nrIndices)
        {
            if (Math.Abs(index1 - index2) == 1)
                return true;
            if (index1 == nrIndices - 1 && index2 == 0)
                return true;
            if (index2 == nrIndices - 1 && index1 == 0)
                return true;

            return false;
        }

        #endregion
    }

    #region HELPER CLASSES: Cycle detection
    /// <summary>
    /// For detecting cycles in the connecting lines between a polygon and the holes contained in it.
    /// </summary>
    public class HoleCycleDetector
    {
        /// <summary>
        /// A reserved integer to indicate invalid indices.
        /// </summary>
        public static readonly int INVALID_INDEX = -2;
        /// <summary>
        /// A look-up table for all nodes - i.e. the ones representing holes and the one representing the polygon.
        /// </summary>
        public Dictionary<int, HoleCycleNode> AllNodes { get; }

        private bool verbose;

        /// <summary>
        /// Initializes the hole cycle detector and its root node.
        /// </summary>
        /// <param name="nr_of_holes">the number of holes in the polygon</param>
        /// <param name="verbose">if true, generate debug console output</param>
        public HoleCycleDetector(int nr_of_holes, bool verbose)
        {
            this.AllNodes = new Dictionary<int, HoleCycleNode>();
            for (int i = -1; i < nr_of_holes; i++)
            {
                this.AllNodes.Add(i, null);
            }
            this.AllNodes[-1] = new HoleCycleNode(-1, verbose);
            this.verbose = verbose;
        }

        /// <summary>
        /// Adds a connecting line w/o performing any tests.
        /// </summary>
        /// <param name="connecting_line">the connecting line</param>
        public void AddConnectingLine((int, int, int, int) connecting_line)
        {
            if (this.AllNodes[connecting_line.Item1] == null)
                this.AllNodes[connecting_line.Item1] = new HoleCycleNode(connecting_line.Item1, verbose);

            if (this.AllNodes[connecting_line.Item3] == null)
                this.AllNodes[connecting_line.Item3] = new HoleCycleNode(connecting_line.Item3, verbose);

            this.AllNodes[connecting_line.Item1].Connect(connecting_line, this.AllNodes[connecting_line.Item3]);
            this.AllNodes[connecting_line.Item3].Connect(ValueTuple.Create(connecting_line.Item3, connecting_line.Item4, connecting_line.Item1, connecting_line.Item2), this.AllNodes[connecting_line.Item1]);
        }

        /// <summary>
        /// Removes a connecting line.
        /// </summary>
        /// <param name="connecting_line">the connecting line</param>
        public void RemoveConnectingLine((int, int, int, int) connecting_line)
        {
            if (this.AllNodes[connecting_line.Item1] == null || this.AllNodes[connecting_line.Item3] == null)
                throw new NullReferenceException("This should not happen!");

            this.AllNodes[connecting_line.Item1].Disconnect(connecting_line, this.AllNodes[connecting_line.Item3]);
            this.AllNodes[connecting_line.Item3].Disconnect(ValueTuple.Create(connecting_line.Item3, connecting_line.Item4, connecting_line.Item1, connecting_line.Item2), this.AllNodes[connecting_line.Item1]);
        }

        /// <summary>
        /// Gets all holes that have to pass through a bottleneck of only one other hole in order to reach the outer polygon.
        /// </summary>
        /// <returns>true if such holes were found; in addition - a list of holes needing additional connections</returns>
        public (bool found, Dictionary<int, int> holes_bottleneck) GetAllHolesConnectedToOnlyOneOtherHoleInclIndirection()
        {
            Dictionary<int, int> holes = new Dictionary<int, int>();
            bool found_single = false;

            for (int i = -1; i < this.AllNodes.Count - 1; i++)
            {
                if (this.AllNodes[i] == null)
                    this.AllNodes[i] = new HoleCycleNode(i, this.verbose);
            }

            foreach (var entry in this.AllNodes)
            {
                if (entry.Key == -1)
                    continue;

                var paths = entry.Value.BFSwCutoff(3);
                //Debug.WriteLine("paths of {0}", entry.Key);
                //foreach (var p in paths)
                //{
                //    foreach (int step in p)
                //    {
                //        Debug.Write(">{0}", step.ToString());
                //    }
                //    Debug.WriteLine();
                //}
                int common_element = FindFirstCommonElement(paths, entry.Key, INVALID_INDEX);
                if (common_element != INVALID_INDEX)
                {
                    //Debug.WriteLine("found common element {0}", common_element);
                    holes.Add(entry.Key, common_element);
                    found_single = true;
                }
                // see if the bottleneck is the outer polygon
                if (paths.Count() == 1 && common_element == INVALID_INDEX)
                {
                    List<(int, int)> outgoing_indices = new List<(int, int)>();
                    foreach (var connection in entry.Value.Connected2Nnodes)
                    {
                        var oi = ValueTuple.Create(connection.Key.Item3, connection.Key.Item4);
                        if (!outgoing_indices.Contains(oi))
                            outgoing_indices.Add(oi);
                    }
                    if (outgoing_indices.Count == 1 && outgoing_indices[0].Item1 == -1)
                    {
                        holes.Add(entry.Key, -1);
                        found_single = true;
                    }
                }
            }

            return (found_single, holes);
        }

        /// <summary>
        /// Gets the common element in the given paths. Excluded from those are the index -1, indicating the outer poygon, and the given excluded index.
        /// </summary>
        /// <param name="paths">all paths to check</param>
        /// <param name="excluded">the index to exclude, generally the index of the hole itself that occupies the start of each path</param>
        /// <param name="invalid_element">the value to return in case no common element was found</param>
        /// <returns>the furst common element, or the ibvalid element if there was no common element</returns>
        private static int FindFirstCommonElement(IEnumerable<List<int>> paths, int excluded, int invalid_element)
        {
            int nr_paths = paths.Count();
            Dictionary<int, int> element_count = new Dictionary<int, int>();
            foreach (var path in paths)
            {
                // assumes path contains unique entries by construction
                foreach (int p in path)
                {
                    if (p == -1 || p == excluded)
                        continue;

                    if (element_count.ContainsKey(p))
                        element_count[p]++;
                    else
                        element_count.Add(p, 1);
                }
            }
            foreach (var entry in element_count)
            {
                if (entry.Value == nr_paths)
                    return entry.Key;
            }

            return invalid_element;
        }

    }

    /// <summary>
    /// Represents a connected hole in the context of the search for connecting lines between 
    /// an outer polygon and the holes contained in it.
    /// </summary>
    public class HoleCycleNode
    {
        /// <summary>
        /// Representing the hole
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Representing the nodes reachable via the respective connecting lines.
        /// </summary>
        public Dictionary<(int, int, int, int), HoleCycleNode> Connected2Nnodes { get; }
        /// <summary>
        /// Represent the unique nodes this one is conected to, and the number of times this node is connected to them.
        /// </summary>
        public Dictionary<HoleCycleNode, int> Nodes { get; }

        private bool verbose;

        /// <summary>
        /// Initializes a node with a specific index.
        /// </summary>
        /// <param name="index">the hole index, -1 indicates the outer polygon</param>
        /// <param name="verbose">if true, print the debug output to the console</param>
        public HoleCycleNode(int index, bool verbose)
        {
            this.Index = index;
            this.Connected2Nnodes = new Dictionary<(int, int, int, int), HoleCycleNode>();
            this.Nodes = new Dictionary<HoleCycleNode, int>();
            this.verbose = verbose;
        }

        /// <summary>
        /// Adds a connnection to another node.
        /// </summary>
        /// <param name="connection">the connecting line</param>
        /// <param name="node">the node</param>
        public void Connect((int, int, int, int) connection, HoleCycleNode node)
        {
            if (connection.Item1 != this.Index)
                throw new ArgumentException("The connection has to start at this node!");
            if (connection.Item3 != node.Index)
                throw new ArgumentException("The connection has to end at the given input node!");

            if (!this.Connected2Nnodes.ContainsKey(connection))
                this.Connected2Nnodes.Add(connection, node);
            if (this.Nodes.ContainsKey(node))
                this.Nodes[node]++;
            else
                this.Nodes.Add(node, 1);
        }

        /// <summary>
        /// Removes an existing connection to a node.
        /// </summary>
        /// <param name="connection">the connecting line</param>
        /// <param name="node">the node</param>
        public void Disconnect((int, int, int, int) connection, HoleCycleNode node)
        {
            if (connection.Item1 != this.Index)
                throw new ArgumentException("The connection has to start at this node!");
            if (connection.Item3 != node.Index)
                throw new ArgumentException("The connection has to end at the given input node!");

            this.Connected2Nnodes.Remove(connection);
            if (this.Nodes.ContainsKey(node))
            {
                this.Nodes[node]--;
                if (this.Nodes[node] <= 0)
                    this.Nodes.Remove(node);
            }
        }

        /// <summary>
        /// Searches for a cycle containing only holes and docking only at one point in any of the holes.
        /// NOT IN USE !!!
        /// </summary>
        /// <param name="used_connections">the connecting lines used in the evaluation</param>
        /// <param name="used_stops">the used points on the holes</param>
        /// <param name="debug">the indent for the debug output</param>
        /// <returns>the found cycles</returns>
        private (bool reached_end, bool found_cycle, (int, int, int, int) cause) TraverseAndTest(List<(int, int, int, int)> used_connections, List<(int, int, int)> used_stops, string debug)
        {
            debug += "\t";
            if (verbose)
            {
                Debug.WriteLine(debug + "traversing node {0}", this.ToString());
                Debug.WriteLine(debug + "used stops:");
                foreach (var s in used_stops)
                {
                    Debug.WriteLine(debug + "{0}:{1}>{2}", s.Item1, s.Item2, s.Item3);
                }
            }

            bool reached_end = false;
            bool found_cycle = false;
            var cause = ValueTuple.Create(-1, -1, -1, -1);

            int counter = 0;
            foreach (var next in this.Connected2Nnodes)
            {
                List<(int, int, int)> used_stops_before_iteration = new List<(int, int, int)>(used_stops);

                if (used_connections.Contains(next.Key))
                    continue;
                var reversed_connection = ValueTuple.Create(next.Key.Item3, next.Key.Item4, next.Key.Item1, next.Key.Item2);
                if (used_connections.Contains(reversed_connection))
                    continue;

                counter++;
                used_connections.Add(next.Key);
                if (verbose)
                    Debug.WriteLine(debug + "traversing connection {0}:{1} - {2}:{3}", next.Key.Item1, next.Key.Item2, next.Key.Item3, next.Key.Item4);

                var stopA = ValueTuple.Create(next.Key.Item1, next.Key.Item2, 1); // 1 = out
                if (verbose)
                    Debug.WriteLine(debug + "TESTING STOP {0}:{1}>{2}", stopA.Item1, stopA.Item2, stopA.Item3);
                if (stopA.Item1 == -1)
                    used_stops.Clear(); // no cycles via the outer polygon

                var testA = ValueTuple.Create(stopA.Item1, stopA.Item2, 2);
                if (stopA.Item1 != -1 && used_stops.Contains(testA) && used_stops.IndexOf(testA) < used_stops.Count - 1)
                {
                    if (verbose)
                        Debug.WriteLine(debug + "!!!");
                    found_cycle = true;
                    cause = next.Key;
                    break;
                }
                if (stopA.Item1 != -1)
                    used_stops.Add(stopA);

                var stopB = ValueTuple.Create(next.Key.Item3, next.Key.Item4, 2); // 2 = in
                if (verbose)
                    Debug.WriteLine(debug + "TESTING STOP {0}:{1}>{2}", stopB.Item1, stopB.Item2, stopB.Item3);
                if (stopB.Item1 == -1)
                    used_stops.Clear(); // no cycles via the outer polygon

                var testB = ValueTuple.Create(stopB.Item1, stopB.Item2, 1);
                if (stopB.Item1 != -1 && used_stops.Contains(testB) && used_stops.IndexOf(testB) < used_stops.Count - 1)
                {
                    if (verbose)
                        Debug.WriteLine(debug + "!!!");
                    found_cycle = true;
                    cause = next.Key;
                    break;
                }
                if (stopB.Item1 != -1)
                    used_stops.Add(stopB);

                HoleCycleNode n = next.Value;
                if (n.Index > -1)
                {
                    var n_result = n.TraverseAndTest(used_connections, used_stops, debug);
                    if (n_result.reached_end)
                        reached_end = n_result.reached_end;
                    if (n_result.found_cycle)
                    {
                        found_cycle = n_result.found_cycle;
                        cause = n_result.cause;
                    }

                    if (found_cycle)
                        break;
                }

                used_stops = used_stops_before_iteration;
            }
            if (counter == 0)
                reached_end = true;

            if (verbose)
                Debug.WriteLine(debug + "result node {0}: reached_end = {1}, found_cycle = {2}", this.ToString(), reached_end, found_cycle);
            return (reached_end, found_cycle, cause);
        }

        /// <summary>
        /// Breadth-First search for all paths starting at this node and ending at the outer polygon or at this node again
        /// with a cut-off after reaching the outer polygon a given number of times.
        /// </summary>
        /// <param name="after_reaching_the_outside_n_times">the number of times to reach the outer polygon</param>
        /// <returns>each found path separately</returns>
        internal IEnumerable<List<int>> BFSwCutoff(int after_reaching_the_outside_n_times)
        {
            var queue = new Queue<Tuple<List<int>, HoleCycleNode>>();
            queue.Enqueue(new Tuple<List<int>, HoleCycleNode>(new List<int> { this.Index }, this));

            int reached_the_ouside = 0;
            while (queue.Any() && reached_the_ouside < after_reaching_the_outside_n_times)
            {
                var node = queue.Dequeue();
                if (node.Item2.Nodes.Any())
                {
                    int counter = 0;
                    foreach (var successor in node.Item2.Nodes)
                    {
                        if (successor.Key.Index == -1)
                        {
                            var path_end = new List<int>(node.Item1);
                            path_end.Add(-1);
                            reached_the_ouside++;
                            counter++;
                            yield return path_end;
                            continue;
                        }
                        if (node.Item1.Contains(successor.Key.Index))
                            continue;

                        counter++;
                        var path = new List<int>(node.Item1);
                        path.Add(successor.Key.Index);
                        queue.Enqueue(new Tuple<List<int>, HoleCycleNode>(path, successor.Key));
                    }
                    if (counter == 0)
                        yield return node.Item1;
                }
                else
                {
                    yield return node.Item1;
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "HoleCycleNode " + this.Index + " -> [" + this.Connected2Nnodes.Count + " nodes]";
        }
    }
    #endregion
}
