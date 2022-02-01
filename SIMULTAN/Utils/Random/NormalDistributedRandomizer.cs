using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Randomize
{
    /// <summary>
    /// Provides normal distributed random numbers in the [0, 1] range
    /// </summary>
    public class NormalDistributedRandomizer : IRandomizer
    {
        private Random rand = new Random();

        /// <inheritdoc />
        public double Next()
        {
            double u1 = 1.0 - rand.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0 - rand.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                            Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return randStdNormal;
        }
    }
}
