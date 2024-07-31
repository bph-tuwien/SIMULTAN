using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Enumeration with different algorithms for offset surface generation
    /// </summary>
    public enum OffsetAlgorithm
    {
        /// <summary>
        /// Uses the best available algorithm
        /// </summary>
        Full,
        /// <summary>
        /// Uses a algorithm that is as fast as possible
        /// </summary>
        Fast,
        /// <summary>
        /// Do not perform any offset calculations
        /// </summary>
        Disabled
    }

    /// <summary>
    /// Generates offset surfaces and handles switching between different algorithms
    /// </summary>
    public class OffsetSurfaceGenerator
    {
        /// <summary>
        /// The algorithm used for updates
        /// </summary>
        public OffsetAlgorithm Algorithm
        {
            get { return algorithm; }
            set { algorithm = value; UpdateFastFaces(); }
        }
        private OffsetAlgorithm algorithm;

        private HashSet<Face> fastFaces;
        private GeometryModelData model;
        private IDispatcherTimer fastUpdateTimer;

        /// <summary>
        /// Initializes a new instance of the OffsetSurfaceGenerator class
        /// </summary>
        /// <param name="model">The geometry model</param>
        /// <param name="dispatcherTimer">The dispatcher timer factory</param>
        public OffsetSurfaceGenerator(GeometryModelData model, IDispatcherTimer dispatcherTimer)
        {
            this.model = model;
            this.algorithm = OffsetAlgorithm.Full;
            this.fastFaces = new HashSet<Face>();
            this.fastUpdateTimer = dispatcherTimer;
            this.fastUpdateTimer.Interval = TimeSpan.FromMilliseconds(GeometrySettings.Instance.OffsetSurfaceRecalcDelay);
            this.fastUpdateTimer.AddTickEventHandler(FastUpdateTimer_Tick);
        }

        private void FastUpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateFastFaces();
            fastUpdateTimer.Stop();
        }

        /// <summary>
        /// Performs a complete offset model recalculation
        /// </summary>
        /// <param name="calculateSurfaces">True for normal recalcuation, False to prevent calculations</param>
        public void Update(bool calculateSurfaces = true)
        {
            var alg = Algorithm;
            if (!calculateSurfaces)
                alg = OffsetAlgorithm.Disabled;

            switch (alg)
            {
                case OffsetAlgorithm.Full:
                    ImprovedOffsetSurfaceGenerator.Update(model);
                    break;
                case OffsetAlgorithm.Fast:
                    fastUpdateTimer.Stop();
                    fastFaces.Clear();
                    model.Faces.ForEach(x => fastFaces.Add(x));
                    DummyOffsetSurfaceGenerator.Update(model);
                    fastUpdateTimer.Start();
                    break;
                default:
                    break;
            }

            model.OffsetModel.OnOffsetSurfaceChanged(null);
        }

        /// <summary>
        /// Performs a partial offset surface recalculation
        /// </summary>
        /// <param name="invalidatedGeometry">List of geoemtry that has to be recalculated</param>
        /// <param name="calculateSurfaces">True for normal recalcuation, False to prevent calculations</param>
        /// <returns></returns>
        public IEnumerable<Face> Update(IEnumerable<BaseGeometry> invalidatedGeometry, bool calculateSurfaces = true)
        {
            var alg = Algorithm;
            if (!calculateSurfaces)
                alg = OffsetAlgorithm.Disabled;

            return Update(invalidatedGeometry, alg);
        }

        private IEnumerable<Face> Update(IEnumerable<BaseGeometry> invalidatedGeometry, OffsetAlgorithm algorithm)
        {
            IEnumerable<Face> resultGeometry = null;

            switch (algorithm)
            {
                case OffsetAlgorithm.Full:
                    resultGeometry = ImprovedOffsetSurfaceGenerator.Update(model, invalidatedGeometry);
                    break;
                case OffsetAlgorithm.Fast:
                    fastUpdateTimer.Stop();
                    invalidatedGeometry.Where(x => x is Face).ForEach(x => fastFaces.Add((Face)x));
                    resultGeometry = DummyOffsetSurfaceGenerator.Update(model, invalidatedGeometry);
                    fastUpdateTimer.Start();
                    break;
            }

            if (resultGeometry != null)
                model.OffsetModel.OnOffsetSurfaceChanged(resultGeometry);

            return resultGeometry;
        }

        private void UpdateFastFaces()
        {
            if (fastFaces.Count > 0)
            {
                var invalidated = Update(fastFaces, OffsetAlgorithm.Full);
                model.OffsetModel.OnOffsetSurfaceChanged(invalidated);
                fastFaces.Clear();
            }
        }
    }
}
