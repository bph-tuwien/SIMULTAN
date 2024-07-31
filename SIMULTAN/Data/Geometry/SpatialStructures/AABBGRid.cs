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
    /// Stores AABBs in a grid structure
    /// </summary>
	public class AABBGrid
    {
        /// <summary>
        /// The cells of the grid. May be null when no AABBs are present
        /// </summary>
		public List<AABB>[,,] Cells { get; private set; }

        /// <summary>
        /// The size of each cell in the grid. Might defer from the desired size due to equalization reasons
        /// </summary>
		public SimVector3D ActualCellSize { get; private set; }
        /// <summary>
        /// Minimum position of the grid
        /// </summary>
		public SimPoint3D Min { get; private set; }
        /// <summary>
        /// Maximum position in the grid
        /// </summary>
		public SimPoint3D Max { get; private set; }

        private double Eps { get { return 0.01; } }

        /// <summary>
        /// Initializes a new instance of the AABBGrid class
        /// </summary>
        /// <param name="min">Minimum position of the grid</param>
        /// <param name="max">Maximum position of the grid</param>
        /// <param name="desiredCellSize">The desired cell size. Used to calculate the number of cells.
        /// To prevent memory issues in large models, the number of cells is fixed between [0, maxCellSize], which may lead to larger 
        /// cell sizes than expected
        /// </param>
        /// <param name="maxCellSize">The maximum number of cells along each axis. Prevents memory issues with very large models.</param>
		public AABBGrid(SimPoint3D min, SimPoint3D max, SimVector3D desiredCellSize, int maxCellSize = 1000)
        {
            //Calculate size of grid
            Min = min;
            Max = max;

            int numCellsX = Math.Min(maxCellSize, Math.Max(1, (int)Math.Ceiling((Max.X - Min.X) / desiredCellSize.X)));
            int numCellsY = Math.Min(maxCellSize, Math.Max(1, (int)Math.Ceiling((Max.Y - Min.Y) / desiredCellSize.Y)));
            int numCellsZ = Math.Min(maxCellSize, Math.Max(1, (int)Math.Ceiling((Max.Z - Min.Z) / desiredCellSize.Z)));

            this.ActualCellSize = new SimVector3D(
                Math.Max((Max.X - Min.X) / numCellsX, 1),
                Math.Max((Max.Y - Min.Y) / numCellsY, 1),
                Math.Max((Max.Z - Min.Z) / numCellsZ, 1)
                );

            this.Cells = new List<AABB>[numCellsX, numCellsY, numCellsZ];
        }

        private (IntIndex3D from, IntIndex3D to) CellFromValues(SimPoint3D min, SimPoint3D max)
        {
            var x = CellFromValues(min.X, max.X, this.Min.X, Cells.GetLength(0), this.ActualCellSize.X);
            var y = CellFromValues(min.Y, max.Y, this.Min.Y, Cells.GetLength(1), this.ActualCellSize.Y);
            var z = CellFromValues(min.Z, max.Z, this.Min.Z, Cells.GetLength(2), this.ActualCellSize.Z);

            return (new IntIndex3D(x.from, y.from, z.from), new IntIndex3D(x.to, y.to, z.to));
        }

        private (int from, int to) CellFromValues(double itemMin, double itemMax, double gridMin, int cellCount, double cellSize)
        {
            var fromDouble = (itemMin - gridMin) / cellSize;
            var toDouble = (itemMax - gridMin) / cellSize;

            int from = (int)fromDouble;
            int to = (int)toDouble;

            //Check for eps region around cell boundaries
            if (fromDouble - Math.Floor(fromDouble) <= this.Eps)
            {
                from -= 1;
            }
            if (toDouble - Math.Ceiling(toDouble) <= -this.Eps)
            {
                to += 1;
            }

            var fromClamped = Math.Max(Math.Min(from, cellCount - 1), 0);
            var toClamped = Math.Min(Math.Max(fromClamped, to), cellCount - 1); //Guarantees at least one cell

            return (fromClamped, toClamped);
        }

        /// <summary>
        /// Iterates over each cell a AABB touches and executes an action with this index
        /// </summary>
        /// <param name="item">The aabb</param>
        /// <param name="action">The action</param>
		public void ForEachCell(AABB item, Action<IntIndex3D> action)
        {
            if (item.Min.X < this.Min.X || item.Min.Y < this.Min.Y || item.Min.Z < this.Min.Z ||
                item.Max.X > this.Max.X || item.Max.Y > this.Max.Y || item.Max.Y > this.Max.Y)
                throw new IndexOutOfRangeException("item outside of grid");

            (var from, var to) = CellFromValues(item.Min, item.Max);

            for (int x = from.X; x <= to.X; ++x)
            {
                for (int y = from.Y; y <= to.Y; ++y)
                {
                    for (int z = from.Z; z <= to.Z; ++z)
                    {
                        action(new IntIndex3D(x, y, z));
                    }
                }
            }
        }
        /// <summary>
        /// Executes an action for the cell the position is in
        /// </summary>
        /// <param name="position">The position</param>
        /// <param name="action">The action</param>
		public void ForCell(SimPoint3D position, Action<IntIndex3D> action)
        {
            (var from, var to) = CellFromValues(position, position);
            action(new IntIndex3D(from.X, from.Y, from.Z));
        }

        /// <summary>
        /// Adds an AABB to the grid
        /// </summary>
        /// <param name="item"></param>
		public void Add(AABB item)
        {
            //if (item.Content is Vertex v && v.Name == "subline_v1")
            //{ }
            //if (item.Content is Edge e && e.Name == "Edge 8")
            //{ }


            ForEachCell(item, x =>
            {
                if (this.Cells[x.X, x.Y, x.Z] == null)
                    this.Cells[x.X, x.Y, x.Z] = new List<AABB>();
                this.Cells[x.X, x.Y, x.Z].Add(item);
            });
        }
        /// <summary>
        /// Adds a number of AABBs to the grid
        /// </summary>
        /// <param name="items"></param>
		public void AddRange(IEnumerable<AABB> items)
        {
            items.ForEach(x => Add(x));
        }
        /// <summary>
        /// Removes an AABB from the grid
        /// </summary>
        /// <param name="item"></param>
		public void Remove(AABB item)
        {
            ForEachCell(item, x =>
            {
                if (this.Cells[x.X, x.Y, x.Z] != null)
                {
                    this.Cells[x.X, x.Y, x.Z].Remove(item);
                    if (this.Cells[x.X, x.Y, x.Z].Count == 0)
                        this.Cells[x.X, x.Y, x.Z] = null;
                }
            });
        }

        /// <summary>
        /// Returns a list of all AABB which could contain a given point.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IEnumerable<AABB> this[IntIndex3D index]
        {
            get
            {
                return this.Cells[index.X, index.Y, index.Z];
            }
        }
    }
}
