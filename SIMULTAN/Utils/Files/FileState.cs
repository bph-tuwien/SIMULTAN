using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Files
{
    /// <summary>
    /// Contains methods for querying information about files in the file system.
    /// </summary>
    public static class FileState
    {
        /// <summary>
        /// Checks if the file is in use by applications that lock files. Attention: the state of 
        /// the file can change immediately after calling this method. So an attempts at deleting it
        /// might still throw an exception!
        /// </summary>
        /// <param name="file">the file to check</param>
        /// <returns>true if the given file is in use, false otherwise</returns>
        public static bool IsInUse(FileInfo file)
        {
            try
            {
                using (Stream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    // no need to do anything here
                }
                return false;
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// Waits on the current thread till the lock on the file is removed.
        /// Throws a <see cref="TimeoutException"/> if the timeout is reached.
        /// </summary>
        /// <param name="file">File to wait for.</param>
        /// <param name="sleepTime">Time in ms to wait between checks. Default 1 second.</param>
        /// <param name="timeout">Timeout in ms. Default 1 minute.</param>
        public static void WaitFile(FileInfo file, int sleepTime = 1000, int timeout = 60000)
        {
            DateTime start = DateTime.Now;
            var span = TimeSpan.FromMilliseconds(timeout);

            while (FileState.IsInUse(file))
            {
                if (DateTime.Now - start > span)
                {
                    throw new TimeoutException();
                }
                System.Threading.Thread.Sleep(sleepTime);
            }
        }
    }
}
