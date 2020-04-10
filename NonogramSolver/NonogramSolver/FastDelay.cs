using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace NonogramSolver
{
    /// <summary>
    /// Provides a task delay mechanism with catch-up for slow system timers
    /// </summary>
    /// <remarks>
    /// This class does not necessarily give perfectly consistent delays on every call, as it is limited by the underlying Task.Delay precision.
    /// Instead subsequent calls to the methods in this class will take into account the amount truly waited for in previous calls and will return immediately if enough elapsed time has already occured
    /// </remarks>
    public class FastDelay
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private TimeSpan extraTime = TimeSpan.Zero;

        /// <summary>
        /// Waits for <paramref name="milliseconds"/> to pass, accounting for previous calls that ran for too long
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns>Delayed task</returns>
        public async Task Delay(int milliseconds)
        {
            var delay = TimeSpan.FromMilliseconds(milliseconds);
            if(extraTime < delay)
            {
                stopwatch.Restart();
                await Task.Delay(delay - extraTime);
                stopwatch.Stop();
                extraTime += stopwatch.Elapsed;
            }
            extraTime -= delay;
        }
    }
}
