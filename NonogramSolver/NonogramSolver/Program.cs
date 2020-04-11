using System;
using System.Linq;
using System.Threading.Tasks;

namespace NonogramSolver
{
    class Program
    {
        private static (int[][] columns, int[][] rows) test1 = (
            new[]
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
            },
            new[]
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
            }
        );

        private static (int[][] columns, int[][] rows) test2 = (
            new []
            {
                new []{1, 4},
                new []{10},
                new []{12},
                new []{2, 1, 8},
                new []{2, 8},
                new []{3, 8},
                new []{2, 8},
                new []{8},
                new []{5},
                new []{4},
                new []{4},
                new []{4},
                new []{3},
                new []{3},
                new []{2}
            },
            new []
            {
                new []{4},
                new []{6},
                new []{2, 2},
                new []{4},
                new []{2},
                new []{2},
                new []{2},
                new []{15},
                new []{15},
                new []{14},
                new []{12},
                new []{7},
                new []{5},
                new []{5},
                new []{5}
            }
        );

        static async Task Main(string[] args)
        {
            // TODO: load from args or interactive console menu
            (int[][] columns, int[][] rows) = test2;
            int gridCharacterDelay = 1;

            using (var nonogram = new Nonogram(rows, columns))
            {
                await nonogram.Draw(gridCharacterDelay);
            }
            Console.ReadKey();
        }
    }
}
