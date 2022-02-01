using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.BackgroundWork
{
    /// <summary>
    /// Stores additional data for the progress of background worker
    /// </summary>
    public class BackgroundUserState
    {
        /// <summary>
        /// Usertext displayed next to the title in the ui. Use this to describe which part of your algorithm is currently running.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// Initializes a new instance of the BackgroundUserState class
        /// </summary>
        /// <param name="text">The text</param>
        public BackgroundUserState(string text)
        {
            this.Text = text;
        }
    }
}
