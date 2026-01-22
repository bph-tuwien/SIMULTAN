using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.TestUtils
{
    public static class FileUtils
    {
        /// <summary>
        /// Deletes a file and retries after some ms if an IOException occurred.
        /// Used to prevent FileSystemWatcher race condition errors.
        /// </summary>
        /// <param name="info">The file</param>
        /// <param name="retryCount">The retry count</param>
        /// <param name="retryDelayMs">The ms wait time</param>
        /// <exception cref="Exception">If the retries were exceeded</exception>
        public static void DeleteRetry(this FileInfo info, int retryCount = 10, int retryDelayMs = 100)
        {
            DeleteRetry(info.FullName, retryCount, retryDelayMs);
        }

        /// <summary>
        /// Deletes a file and retries after some ms if an IOException occurred.
        /// Used to prevent FileSystemWatcher race condition errors.
        /// </summary>
        /// <param name="file">The file</param>
        /// <param name="retryCount">The retry count</param>
        /// <param name="retryDelayMs">The ms wait time</param>
        /// <exception cref="Exception">If the retries were exceeded</exception>
        public static void DeleteRetry(string file, int retryCount = 10, int retryDelayMs = 100)
        {
            for (int i = 0; i < retryCount && File.Exists(file); i++)
            {
                try
                {
                    File.Delete(file);
                    return;
                }
                catch (IOException)
                {
                    Debug.WriteLine($"Failed to delete '{file}', waiting {retryDelayMs}ms. ({i + 1}/{retryCount})");
                    Thread.Sleep(retryDelayMs);
                }
            }
            throw new Exception($"Max delete retry count reached for file '{file}'");
        }
    }
}
