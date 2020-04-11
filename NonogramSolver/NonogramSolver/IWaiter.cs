using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NonogramSolver
{
    /// <summary>
    /// Represents a type that can be used to wait for an event repeatedly, may be stateful
    /// </summary>
    public interface IWaiter
    {
        /// <summary>
        /// Wait for the next event
        /// </summary>
        /// <returns>Task that will be completed when the next event occurs</returns>
        Task Wait();
    }
}