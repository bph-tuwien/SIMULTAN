using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.BackgroundWork
{
    /// <summary>
    /// Provides information about the ongoing background operation
    /// </summary>
    public interface IBackgroundAlgorithmInfo
    {
        /// <summary>
        /// True when the user wants to cancel the operation. In this case the algorithm should stop as soon as possible
        /// </summary>
        bool CancellationPending { get; }
        /// <summary>
        /// Stores whether this operation was canceled. Set this to true while responding to CancellationPending
        /// </summary>
        bool Cancel { set; }

        /// <summary>
        /// Notifies the main thread about progress
        /// </summary>
        /// <param name="percent">Progress in Percent (0-100)</param>
        void ReportProgress(int percent);
    }

    /// <summary>
    /// Info for a background worker called from inside the DoWork event
    /// Implementation of the IBackgroundAlgorithmInfo interface
    /// </summary>
    public class BackgroundAlgorithmInfo : IBackgroundAlgorithmInfo
    {
        private double percentStart, percentLength;
        private string userText;
        private BackgroundWorker worker;
        private DoWorkEventArgs args;
        private int lastReportedProgress = int.MinValue;

        /// <inheritdoc/>
        public bool CancellationPending { get { return worker.CancellationPending; } }

        /// <inheritdoc/>
        public bool Cancel { set { args.Cancel = value; } }


        /// <summary>
        /// Initializes a new instance of the BackgroundAlgorithmInfo class
        /// </summary>
        /// <param name="worker">The worker who's executing the operation</param>
        /// <param name="args">The EventArgs from the background workers DoWork event</param>
        /// <param name="percentStart">Start progress for this part of the algorithm (0-100)</param>
        /// <param name="percentEnd">Final progress for this part of the algorithm</param>
        /// <param name="userText">The text displayed while this algorithm is working</param>
        public BackgroundAlgorithmInfo(BackgroundWorker worker, DoWorkEventArgs args, double percentStart, double percentEnd, string userText)
        {
            this.percentStart = percentStart;
            this.percentLength = percentEnd - percentStart;
            this.worker = worker;
            this.userText = userText;
            this.args = args;
        }

        /// <inheritdoc/>
        public void ReportProgress(int percent)
        {
            if (percent != lastReportedProgress)
            {
                worker.ReportProgress((int)(percentStart + (double)percent / 100.0 * percentLength), new BackgroundUserState(userText));
                lastReportedProgress = percent;
            }
        }
    }

    /// <summary>
    /// Dummy implementation that does nothing
    /// </summary>
    public class EmptyBackgroundAlgorithmInfo : IBackgroundAlgorithmInfo
    {
        /// <inheritdoc/>
        public bool CancellationPending => false;

        /// <inheritdoc/>
        public bool Cancel { set { } }

        /// <inheritdoc/>
        public void ReportProgress(int percent)
        { }
    }
}
