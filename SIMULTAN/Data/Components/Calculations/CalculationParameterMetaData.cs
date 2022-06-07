using SIMULTAN.Utils;
using SIMULTAN.Utils.Randomize;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Additional information for a variable in a Calculation
    /// </summary>
    public class CalculationParameterMetaData : INotifyPropertyChanged
    {
        /// <summary>
        /// Modes for standard deviation in the randomization
        /// </summary>
        public enum DeviationModeType
        {
            /// <summary>
            /// The deviation is treated as an absolute value
            /// </summary>
            Absolute = 0,
            /// <summary>
            /// The deviation is treated relative to the start value
            /// </summary>
            Relative = 1
        }

        /// <summary>
        /// The randomizer used for this variable. By default, a normal distribution is used
        /// </summary>
        public static IRandomizer Randomizer { get; internal set; } = new NormalDistributedRandomizer();

        #region Properties

        /// <summary>
        /// The sub-range of the MultiValue field which should be used.
        /// Values are clamped during calculation to the maximum the valuefield supports
        /// </summary>
        public RowColumnRange Range { get { return range; } set { if (range != value) { range = value; NotifyPropertyChanged(nameof(Range)); } } }
        private RowColumnRange range = new RowColumnRange(0, 0, int.MaxValue, int.MaxValue);

        /// <summary>
        /// When set to True, the parameter values are randomized
        /// </summary>
        public bool IsRandomized
        {
            get { return isRandomized; }
            set
            {
                if (isRandomized != value)
                {
                    isRandomized = value;
                    NotifyPropertyChanged(nameof(IsRandomized));
                }
            }
        }
        private bool isRandomized = false;

        /// <summary>
        /// Mean value for the normal distribution. The mean is given relative to the actual value. 
        /// A value of 1 means that the mean equals the original value
        /// </summary>
        public double RandomizeRelativeMean
        {
            get { return randomizeRelativeMean; }
            set
            {
                if (randomizeRelativeMean != value)
                {
                    randomizeRelativeMean = value;
                    NotifyPropertyChanged(nameof(RandomizeRelativeMean));
                }
            }
        }
        private double randomizeRelativeMean = 1.0;

        /// <summary>
        /// Standard deviation for the randomization.
        /// Depending on RandomizeDeviationMode, the deviation is either treated as an absolute value or relative to the original value
        /// </summary>
        public double RandomizeDeviation
        {
            get { return randomizeDeviation; }
            set
            {
                if (randomizeDeviation != value)
                {
                    randomizeDeviation = value;
                    NotifyPropertyChanged(nameof(RandomizeDeviation));
                }
            }
        }
        private double randomizeDeviation = 1.0;

        /// <summary>
        /// Mode of the RandomizeDeviation. See RandomizeDeviation for details
        /// </summary>
        public DeviationModeType RandomizeDeviationMode
        {
            get { return randomizedeviationMode; }
            set
            {
                if (randomizedeviationMode != value)
                {
                    randomizedeviationMode = value;
                    NotifyPropertyChanged(nameof(RandomizeDeviationMode));
                }
            }
        }
        private DeviationModeType randomizedeviationMode = DeviationModeType.Absolute;

        /// <summary>
        /// Defines whether randomization should be clamped.
        /// Clamping range is defined by RandomizeClampDeviation relative to the RandomizeDeviation 
        /// in range: original value +/- clamp range
        /// </summary>
        public bool RandomizeIsClamping
        {
            get { return randomizeIsClamping; }
            set
            {
                if (randomizeIsClamping != value)
                {
                    randomizeIsClamping = value;
                    NotifyPropertyChanged(nameof(RandomizeIsClamping));
                }
            }
        }
        private bool randomizeIsClamping;

        /// <summary>
        /// Specifies the clamping range when RandomizeIsClamping is set to True.
        /// The value is given as multiple of the randomization deviation.
        /// </summary>
        public double RandomizeClampDeviation
        {
            get { return randomizeClampDeviation; }
            set
            {
                if (randomizeClampDeviation != value)
                {
                    randomizeClampDeviation = value;
                    NotifyPropertyChanged(nameof(RandomizeClampDeviation));
                }
            }
        }
        private double randomizeClampDeviation = 1.0;

        #endregion

        /// <summary>
        /// Copies all properties from another meta data to this meta data.
        /// </summary>
        /// <param name="other">The source meta data</param>
        public void AssignFrom(CalculationParameterMetaData other)
        {
            this.IsRandomized = other.IsRandomized;
            this.RandomizeClampDeviation = other.RandomizeClampDeviation;
            this.RandomizeDeviation = other.RandomizeDeviation;
            this.RandomizeDeviationMode = other.RandomizeDeviationMode;
            this.RandomizeIsClamping = other.RandomizeIsClamping;
            this.RandomizeRelativeMean = other.RandomizeRelativeMean;
            this.Range = other.Range;
        }

        #region Events

        /// <inheritdoc />

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion
    }
}
