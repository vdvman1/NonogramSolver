using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NonogramSolver
{
    class Nonogram : IDisposable
    {
        private readonly List<int>[] rows, columns;
        private readonly int[] rowFilled, columnFilled;
        private readonly bool?[,] grid;
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
            this.rows = rows.Select(Enumerable.ToList).ToArray();
            rowFilled = rows.Select(Enumerable.Sum).ToArray();
            this.columns = columns.Select(Enumerable.ToList).ToArray();
            columnFilled = columns.Select(Enumerable.Sum).ToArray();

            grid = new bool?[rows.Length, columns.Length];

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

        private async Task FillCompletedRow(int y, IAsyncWaiter waiter)
        {
            if (rowFilled[y] > 0)
            {
                rowFilled[y]--;
                if (rowFilled[y] == 0)
                {
                    for (int i = 0; i < grid.GetLength(1); i++)
                    {
                        if (grid[y, i] == null)
                        {
                            await FillCell(i, y, false, waiter);
                        }
                    }
                }
            }
        }

        private async Task FillCompletedColumn(int x, IAsyncWaiter waiter)
        {
            if (columnFilled[x] > 0)
            {
                columnFilled[x]--;
                if (columnFilled[x] == 0)
                {
                    for (int i = 0; i < grid.GetLength(0); i++)
                    {
                        if (grid[i, x] == null)
                        {
                            await FillCell(x, i, false, waiter);
                        }
                    }
                }
            }
        }

        private async Task FillCell(int x, int y, bool valid, IAsyncWaiter waiter)
        {
            if (grid[y, x] == null)
            {
                grid[y, x] = valid;
                int cellDrawSise = cellSize - 1;
                int cellDiff = cellSize + 1 /* right border */;
                int drawX = x * cellDiff + maxRow + 1 /* left border */;
                int drawY = y * cellDiff + maxColumn + 1 /* top border */;
                await ConsoleBuffer.Fill(drawX, drawY, drawX + cellDrawSise, drawY + cellDrawSise, valid ? FilledChar : InvalidChar, waiter);

                if (valid)
                {
                    await FillCompletedRow(y, waiter);
                    await FillCompletedColumn(x, waiter);
                }
            }
            else if(grid[y, x] != valid)
            {
                throw new InvalidOperationException($"Tried to set cell ({x}, {y}) to conflicting values");
            }
        }

        private static int CalculateGap(List<int> segments, int size)
        {
            int gap = size - segments[0];
            for (int i = 1; i < segments.Count; i++)
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
                List<int> row = rows[i];
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
                            await FillCell(j, i, true, waiter);
                        }
                        j++;
                    }
                }
            }

            for (int j = 0; j < gridWidth; j++)
            {
                List<int> col = columns[j];
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
                            await FillCell(j, i, true, waiter);
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
