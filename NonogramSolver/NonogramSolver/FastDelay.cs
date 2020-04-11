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
    public class FastDelay : IAsyncWaiter
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private TimeSpan extraTime = TimeSpan.Zero;

        private TimeSpan delay;
        public int Delay
        {
            get => (int)delay.TotalMilliseconds;
            set => delay = TimeSpan.FromMilliseconds(value);
        }

        /// <summary>
        /// Waits for <see cref="Delay"/> milliseconds to pass, accounting for previous calls that ran for too long
        /// </summary>
        /// <returns>Delayed task</returns>
        public async Task WaitAsync()
        {
            // Create a local copy of the delay so that changes to Delay do not affect the current call
            var currentDelay = delay;
            if (extraTime < currentDelay)
            {
                stopwatch.Restart();
                await Task.Delay(currentDelay - extraTime);
                stopwatch.Stop();
                extraTime += stopwatch.Elapsed;
            }
            extraTime -= currentDelay;
        }
    }
}
