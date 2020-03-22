using System;
using System.Linq;

namespace NonogramSolver
{
    class Program
    {
        static void Main(string[] args)
        {
            // TODO: load from args or interactive console menu
            int[][] columns = new[]
            {
                new []{3, 3},
                new []{5, 3},
                new []{1, 5, 2},
                new []{2, 4, 3},
                new []{1, 1, 2, 3},
                new []{3, 1, 2, 4},
                new []{1, 1, 1, 2, 4},
                new []{1, 3, 2, 4},
                new []{1, 1, 1, 2, 4},
                new []{1, 1, 1, 2, 4},
                new []{1, 1, 2, 4},
                new []{1, 4, 3},
                new []{1, 4, 3},
                new []{9},
                new []{3, 3}
            };
            int[][] rows = new[]
            {
                new []{3},
                new []{1, 1},
                new []{3, 1},
                new []{2, 1, 2, 2},
                new []{1, 2, 1},
                new []{2, 1, 1},
                new []{15},
                new []{4, 4},
                new []{15},
                new []{12},
                new []{2, 2},
                new []{15},
                new []{15},
                new []{10},
                new []{6}
            };

            using (var nonogram = new Nonogram(rows, columns))
            {
            }
            Console.ReadKey();
        }
    }
}
