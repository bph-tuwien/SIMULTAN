using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Provides methods for writing an Exception to a log file
    /// </summary>
    public static class ExceptionToFileWriter
    {
        /// <summary>
        /// Writes an exception to a log file. The path of the log is .\simultan_exception_{DATETIME}.txt
        /// </summary>
        /// <param name="e">The exception</param>
        /// <param name="message">An additional message added to the header of the log file</param>
        /// <returns>Returns the exception log file</returns>
        public static FileInfo Write(Exception e, string message = null)
        {
            string filename = String.Format(".\\simultan_exception_{0:dd_MM_yyyy-HH_mm_ss}.txt", DateTime.Now);
            return Write(e, new FileInfo(filename), message);
        }
        /// <summary>
        /// Writes an exception to a log file.
        /// </summary>
        /// <param name="e">The exception</param>
        /// <param name="exceptionFile">The file into which the exception should be written</param>
        /// <param name="message">An additional message added to the header of the log file</param>
        /// <returns>Returns the exception log file</returns>
        public static FileInfo Write(Exception e, FileInfo exceptionFile, string message = null)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(exceptionFile.FullName))
                {
                    if (message != null)
                        sw.WriteLine("{0}\n", message);

                    sw.WriteLine("{0}", DateTime.Now.ToLongDateString());
                    sw.WriteLine("{0}\n", DateTime.Now.ToLongTimeString());
                    sw.WriteLine("Application Version {0}", Assembly.GetEntryAssembly().GetName().Version);
                    sw.WriteLine("Simultan DataModel Version {0}", typeof(ExceptionToFileWriter).Assembly.GetName().Version);

                    WriteException(sw, e);

                    sw.Close();
                }

                return exceptionFile;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void WriteException(StreamWriter sw, Exception e)
        {
            sw.WriteLine("Exception Type: {0}", e.GetType().FullName);
            sw.WriteLine("Message: {0}", e.Message);
            sw.WriteLine("Stacktrace:\n{0}", e.StackTrace);

            if (e.InnerException != null)
            {
                sw.WriteLine("Inner Exception:");
                WriteException(sw, e.InnerException);
            }
        }
    }
}
