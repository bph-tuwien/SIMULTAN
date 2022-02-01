using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Excel
{

    [DebuggerDisplay("[ExcelMappingExtents] {StartX}; {StartY} -  {EndX}; {EndY}")]
    internal struct ExcelMappingExtents
    {
        internal int StartX { get; private set; }
        internal int StartY { get; private set; }
        internal int SizeX { get; private set; }
        internal int SizeY { get; private set; }

        internal int EndX { get { return this.StartX + this.SizeX - 1; } }
        internal int EndY { get { return this.StartY + this.SizeY - 1; } }

        internal int Data { get; private set; }

        internal static ExcelMappingExtents GetDefault()
        {
            return new ExcelMappingExtents(1, 1, 1, 1, default);
        }

        internal ExcelMappingExtents(int startX, int startY, int sizeX, int sizeY, int data)
        {
            this.StartX = startX;
            this.StartY = startY;
            this.SizeX = Math.Max(1, sizeX);
            this.SizeY = Math.Max(1, sizeY);
            this.Data = data;
        }

        internal ExcelMappingExtents(Point4D p, int data)
        {
            this.StartX = (int)p.X;
            this.StartY = (int)p.Y;
            this.SizeX = Math.Max(1, (int)p.Z);
            this.SizeY = Math.Max(1, (int)p.W);
            this.Data = data;
        }

        internal bool Contains(Point p)
        {
            return (this.StartX <= p.X && this.StartY <= p.Y && this.EndX >= p.X && this.EndY >= p.Y);
        }

        internal string GetContent()
        {
            return "(" + this.StartX + "; " + this.StartY + " - " + this.EndX + "; " + this.EndY + ")";
        }

        internal static bool OverlapBetween(ExcelMappingExtents e1, ExcelMappingExtents e2)
        {
            return (e1.StartX >= e2.StartX && e1.StartX <= e2.EndX && e1.StartY >= e2.StartY && e1.StartY <= e2.EndY) ||
                   (e2.StartX >= e1.StartX && e2.StartX <= e1.EndX && e2.StartY >= e1.StartY && e2.StartY <= e1.EndY);
        }

        internal static (ExcelMappingExtents result, bool success) Concat(ExcelMappingExtents e1, ExcelMappingExtents e2)
        {
            if (e1.EndX + 1 == e2.StartX && e1.StartY == e2.StartY && e1.SizeY == e2.SizeY)
            {
                return (new ExcelMappingExtents(e1.StartX, e1.StartY, e1.SizeX + e2.SizeX, e1.SizeY, e1.Data), true);
            }
            else if (e1.EndY + 1 == e2.StartY && e1.StartX == e2.StartX && e1.SizeX == e2.SizeX)
            {
                return (new ExcelMappingExtents(e1.StartX, e1.StartY, e1.SizeX, e1.SizeY + e2.SizeY, e1.Data), true);
            }
            else if (e2.EndX + 1 == e1.StartX && e2.StartY == e1.StartY && e2.SizeY == e1.SizeY)
            {
                return (new ExcelMappingExtents(e2.StartX, e2.StartY, e2.SizeX + e1.SizeX, e2.SizeY, e2.Data), true);
            }
            else if (e2.EndY + 1 == e1.StartY && e2.StartX == e1.StartX && e2.SizeX == e1.SizeX)
            {
                return (new ExcelMappingExtents(e2.StartX, e2.StartY, e2.SizeX, e2.SizeY + e1.SizeY, e2.Data), true);
            }
            else
                return (e1, false);
        }


    }

    public class ExcelMappingTracker
    {
        internal const string DATA = "_DATA";

        internal ExcelMappingExtents ExtentsOfLast { get; private set; }

        private Dictionary<ExcelMappingExtents, ExcelMappingNode> grid;
        private Dictionary<ExcelMappingNode, List<ExcelMappingExtents>> nodeLookup;

        internal ExcelMappingTracker()
        {
            this.ExtentsOfLast = ExcelMappingExtents.GetDefault();
            // initialize the grid
            this.grid = new Dictionary<ExcelMappingExtents, ExcelMappingNode>();
            this.nodeLookup = new Dictionary<ExcelMappingNode, List<ExcelMappingExtents>>();
        }

        internal bool AddMappingRecord(ExcelMappingNode node, int nr_applications, bool overlapAllowed, IList<ExcelMappedData> mapping)
        {
            if (mapping == null)
                throw new ArgumentNullException(nameof(mapping));

            bool overlap = false;

            // try to concatenate the mappings
            if (mapping.Count == 1)
            {
                // Range X=start col, Y=start row, Z=size in cols, W=size in rows
                overlap |= this.AddMappingRecord(node, nr_applications, overlapAllowed, (int)mapping[0].Range.Y, (int)mapping[0].Range.X, (int)mapping[0].Range.W, (int)mapping[0].Range.Z);
            }
            else if (mapping.Count > 1)
            {
                List<ExcelMappingExtents> mapping_ext = mapping.Select(x => new ExcelMappingExtents(x.Range, nr_applications)).ToList();
                List<bool> mapping_taken = Enumerable.Repeat(false, mapping_ext.Count).ToList();
                mapping_taken[0] = true;
                for (int i = 0; i < mapping_taken.Count - 1; i++)
                {
                    for (int j = i + 1; j < mapping_taken.Count; j++)
                    {
                        if (mapping_taken[i] && !mapping_taken[j])
                        {
                            var cc = ExcelMappingExtents.Concat(mapping_ext[i], mapping_ext[j]);
                            if (!cc.success)
                                continue;
                            else
                            {
                                mapping_ext[i] = cc.result;
                                mapping_taken[j] = true;
                            }
                        }
                    }
                }

                List<ExcelMappingExtents> concatenated = new List<ExcelMappingExtents>();
                concatenated.Add(mapping_ext[0]);
                for (int i = 1; i < mapping_taken.Count; i++)
                {
                    if (!mapping_taken[i])
                        concatenated.Add(mapping_ext[i]);
                    else if (mapping_taken[i] && !mapping_taken[i - 1])
                        concatenated.Add(mapping_ext[i]);
                }

                foreach (var m in concatenated)
                {
                    // Range X=start col, Y=start row, Z=size in cols, W=size in rows
                    overlap |= this.AddMappingRecord(node, nr_applications, overlapAllowed, m.StartY, m.StartX, m.SizeY, m.SizeX);
                }

            }

            return overlap;
        }

        private bool AddMappingRecord(ExcelMappingNode node, int nr_applications, bool overlapAllowed, int row, int column, int sizeInRow = 1, int sizeInColumn = 1)
        {
            var actualSizeInRow = Math.Max(1, sizeInRow);
            var actualSizeInColumn = Math.Max(1, sizeInColumn);

            // this.ExtentsOfLast = new ExcelMappingExtents(column, row, actualSizeInColumn, actualSizeInRow, int.MinValue);

            ExcelMappingExtents ext = new ExcelMappingExtents(column, row, actualSizeInColumn, actualSizeInRow, nr_applications + 1);
            this.ExtentsOfLast = ext;

            bool overlap = this.grid.Any(x => ExcelMappingExtents.OverlapBetween(x.Key, ext));

            // drop the record
            if (overlapAllowed || !overlap)
            {
                if (overlap)
                {
                    // we assume it was intentional
                    if (this.grid.ContainsKey(ext))
                    {
                        var node_mapping_to_replace = this.grid[ext];
                        this.grid.Remove(ext);
                        if (this.nodeLookup.ContainsKey(node_mapping_to_replace))
                        {
                            this.nodeLookup[node_mapping_to_replace].Remove(ext);
                            if (this.nodeLookup[node_mapping_to_replace].Count == 0)
                                this.nodeLookup.Remove(node_mapping_to_replace);
                        }
                    }
                }

                this.grid.Add(ext, node);
                if (this.nodeLookup.ContainsKey(node))
                    this.nodeLookup[node].Add(ext);
                else
                    this.nodeLookup.Add(node, new List<ExcelMappingExtents> { ext });
            }

            return overlap;
        }


        #region FOOTPRINTS and BOUNDING BOXES
        internal List<ExcelMappingExtents> GetFootprintOf(ExcelMappingNode node, bool includeChildren)
        {
            List<ExcelMappingExtents> footprint = new List<ExcelMappingExtents>();
            if (node == null)
                return footprint;
            if (this.nodeLookup.ContainsKey(node))
            {
                footprint.AddRange(this.nodeLookup[node]);
                if (includeChildren)
                {
                    foreach (var child in node.Children)
                    {
                        footprint.AddRange(this.GetFootprintOf(child, includeChildren));
                    }
                }
            }
            return footprint;
        }

        private List<ExcelMappingExtents> GetLastFootprintOf(ExcelMappingNode node, bool includeChildren)
        {
            List<ExcelMappingExtents> footprint = new List<ExcelMappingExtents>();
            if (node == null)
                return footprint;
            if (this.nodeLookup.ContainsKey(node))
            {
                var last = this.grid.Where(x => x.Value == node).Select(x => x.Key).OrderBy(x => x.Data).Last();
                footprint.Add(last);
                if (includeChildren)
                {
                    foreach (var child in node.Children)
                    {
                        footprint.AddRange(this.GetLastFootprintOf(child, includeChildren));
                    }
                }
            }
            return footprint;
        }

        private List<ExcelMappingExtents> GetFootprintOfParent(ExcelMappingNode node, bool includeChildren)
        {
            if (node == null)
                return new List<ExcelMappingExtents>();
            if (node.Parent == null)
                return new List<ExcelMappingExtents>();
            return this.GetFootprintOf(node.Parent, includeChildren);
        }

        private static ExcelMappingExtents GetBoundingBoxOf(IEnumerable<ExcelMappingExtents> footprints)
        {
            ExcelMappingExtents bb_default = ExcelMappingExtents.GetDefault();
            if (footprints == null)
                return bb_default;
            if (footprints.Count() == 0)
                return bb_default;
            if (footprints.Count() == 1)
                return footprints.First();

            int startX = int.MaxValue;
            int startY = int.MaxValue;
            int endX = 1;
            int endY = 1;
            foreach (var f in footprints)
            {
                if (startX > f.StartX)
                    startX = f.StartX;
                if (startY > f.StartY)
                    startY = f.StartY;

                if (endX < f.EndX)
                    endX = f.EndX;
                if (endY < f.EndY)
                    endY = f.EndY;
            }

            return new ExcelMappingExtents(startX, startY, endX - startX + 1, endY - startY + 1, default);
        }

        internal ExcelMappingExtents GetBoundingBoxOfParent(ExcelMappingNode node)
        {
            List<ExcelMappingExtents> footprint = this.GetFootprintOfParent(node, true);
            return GetBoundingBoxOf(footprint);
        }

        internal ExcelMappingExtents GetFullBoundingBox()
        {
            var footprint = this.nodeLookup.SelectMany(x => x.Value);
            return GetBoundingBoxOf(footprint);
        }

        internal ExcelMappingExtents GetBoundingBoxOfParentWoChildren(ExcelMappingNode node)
        {
            List<ExcelMappingExtents> footprint = this.GetFootprintOfParent(node, false);
            return GetBoundingBoxOf(footprint);
        }

        internal ExcelMappingExtents GetBoundingBoxOfLastApplicationOf(ExcelMappingNode node)
        {
            List<ExcelMappingExtents> footprint = this.GetLastFootprintOf(node, true);
            return GetBoundingBoxOf(footprint);
        }
        #endregion

        #region INFO
        internal bool CausesOverlap(Point p)
        {
            return this.grid.Any(x => x.Key.Contains(p));
        }

        internal string GetContent(bool size)
        {
            string content = string.Empty;
            foreach (var entry in this.grid)
            {
                if (size)
                    content += "(" + entry.Key.StartX + "; " + entry.Key.StartY + "; " + entry.Key.SizeX + "; " + entry.Key.SizeY + "): ";
                else
                    content += "(" + entry.Key.StartX + "; " + entry.Key.StartY + " - " + entry.Key.EndX + "; " + entry.Key.EndY + "): ";
                content += (entry.Value == null) ? " . . ." : entry.Value.NodeName;
                content += "[" + entry.Key.Data + "]";
                content += Environment.NewLine;
            }
            return content;
        }

        #endregion
    }
}
