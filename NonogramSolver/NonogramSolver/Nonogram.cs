using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonogramSolver
{
    class Nonogram : IDisposable
    {
        private readonly int[][] rows, columns;
        private readonly bool[,] grid;
        private readonly int cellSize;
        private readonly ConsoleBuffer ConsoleBuffer;
        private readonly List<List<string>> columnTitles;
        private readonly List<string> rowTitles;
        private readonly int maxRow;
        private readonly int maxColumn;
        private readonly int totalWidth;
        private readonly int totalHeight;
        private const char FilledChar = '█';
        private const char InvalidChar = '╳';

        public Nonogram(int[][] rows, int[][] columns)
        {
            this.rows = rows;
            this.columns = columns;
            grid = new bool[columns.Length, rows.Length];

            columnTitles = columns.Select(c => c.Select(i => i.ToString()).ToList()).ToList();
            rowTitles = rows.Select(r => string.Join(" ", r)).ToList();

            cellSize = columnTitles.Max(c => c.Max(i => i.Length));
            maxRow = rowTitles.Max(r => r.Length);
            maxColumn = columnTitles.Max(c => c.Count);
            totalWidth = maxRow + 1 /* left border */ + columns.Length * (cellSize + 1 /* right border */);
            totalHeight = maxColumn + 1 /* top border */ + rows.Length * (cellSize + 1 /* bottom border */);

            ConsoleBuffer = new ConsoleBuffer(totalWidth, totalHeight);
        }

        public async Task Draw(int characterDelay)
        {
            var waiter = new FastDelay()
            {
                Delay = characterDelay
            };

            // Print columns
            await ConsoleBuffer.DrawVerticalLine(maxRow, 0, totalHeight - 1, shortEnd: true, waiter: waiter);
            for (int i = 0; i < columnTitles.Count; i++)
            {
                int offsetY = maxColumn - columnTitles[i].Count;
                int offsetX = maxRow + 1 /* left border */ + i * (cellSize + 1 /* right border */);
                for (int j = 0; j < columnTitles[i].Count; j++)
                {
                    await ConsoleBuffer.WriteAt(offsetX, j + offsetY, columnTitles[i][j], waiter: waiter);
                }
                offsetX += cellSize;
                await ConsoleBuffer.DrawVerticalLine(offsetX, 0, totalHeight - 1, shortEnd: true, waiter: waiter);
            }

            // Print rows
            await ConsoleBuffer.DrawHorizontalLine(0, maxColumn, totalWidth - 1, shortEnd: true, waiter: waiter);
            for (int i = 0; i < rowTitles.Count; i++)
            {
                int offsetY = maxColumn + 1 /* top border */ + i * (cellSize + 1 /* bottom border */);
                await ConsoleBuffer.WriteAt(maxRow - rowTitles[i].Length, offsetY, rowTitles[i], waiter: waiter);
                offsetY += cellSize;
                await ConsoleBuffer.DrawHorizontalLine(0, offsetY, totalWidth - 1, shortEnd: true, waiter: waiter);
            }
        }

        private Task FillCell(int x, int y, char c, IAsyncWaiter waiter)
        {
            int cellDrawSise = cellSize - 1;
            int cellDiff = cellSize + 1 /* right border */;
            x = x * cellDiff + maxRow + 1 /* left border */;
            y = y * cellDiff + maxColumn + 1 /* top border */;
            return ConsoleBuffer.Fill(x, y, x + cellDrawSise, y + cellDrawSise, c, waiter);
        }

        private static int CalculateGap(int[] segments, int size)
        {
            int gap = size - segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                gap -= 1 + segments[i];
            }
            return gap;
        }

        public async Task Solve(IAsyncWaiter waiter)
        {
            int gridHeight = grid.GetLength(0);
            int gridWidth = grid.GetLength(1);
            for (int i = 0; i < gridHeight; i++)
            {
                int[] row = rows[i];
                int gap = CalculateGap(row, gridWidth);

                int j = 0;
                foreach (int segmentLength in row)
                {
                    if (segmentLength < gap)
                    {
                        j += segmentLength + 1;
                    }
                    else
                    {
                        j += gap;
                        for (int segI = gap; segI < segmentLength; segI++, j++)
                        {
                            await FillCell(j, i, FilledChar, waiter);
                        }
                        j++;
                    }
                }
            }

            for (int j = 0; j < gridWidth; j++)
            {
                int[] col = columns[j];
                int gap = CalculateGap(col, gridHeight);

                int i = 0;
                foreach (int segmentLength in col)
                {
                    if(segmentLength < gap)
                    {
                        i += segmentLength + 1;
                    }
                    else
                    {
                        i += gap;
                        for (int segI = gap; segI < segmentLength; segI++, i++)
                        {
                            await FillCell(j, i, FilledChar, waiter);
                        }
                        i++;
                    }
                }
            }

            await waiter.WaitAsync();
        }

        public void Dispose()
        {
            ConsoleBuffer.Dispose();
        }
    }
}
