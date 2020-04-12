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
        private readonly (int start, int end)[] rowEndpoints, columnEndpoints;
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

        private static IEnumerable<int> FilterZeros(int[] segments)
        {
            bool yielded = false;

            foreach (int segment in segments.Where(s => s > 0))
            {
                yield return segment;
                yielded = true;
            }

            if (!yielded)
            {
                yield return 0;
            }
        }

        public Nonogram(int[][] rawRows, int[][] rawColumns)
        {
            rows = rawRows.Select(FilterZeros).Select(Enumerable.ToList).ToArray();
            rowFilled = rows.Select(Enumerable.Sum).ToArray();

            columns = rawColumns.Select(FilterZeros).Select(Enumerable.ToList).ToArray();
            columnFilled = columns.Select(Enumerable.Sum).ToArray();

            rowEndpoints = Enumerable.Repeat((0, columns.Length), rows.Length).ToArray();
            columnEndpoints = Enumerable.Repeat((0, rows.Length), columns.Length).ToArray();

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

        private void AdjustRowEndpoints(int x, int y)
        {
            (int start, int end) = rowEndpoints[y];
            if(x == start)
            {
                while(x < grid.GetLength(1) - 1 && grid[y, x] == false/*.HasValue*/)
                {
                    x++;
                    if (x == end) break;
                }
                start = x;
            }
            else if(x == end - 1)
            {
                while(x > 0 && grid[y, x] == false/*.HasValue*/)
                {
                    x--;
                    if (x == start) break;
                }
                end = x + 1;
            }

            rowEndpoints[y] = (start, end);
            if(start == end && rowFilled[y] != 0)
            {
                throw new UnsolvablePuzzleException($"Row {y} unable to be satisfied");
            }
        }

        private void AdjustColumnEndpoints(int x, int y)
        {
            (int start, int end) = columnEndpoints[x];
            if (y == start)
            {
                while (y < grid.GetLength(0) - 1 && grid[y, x] == false/*.HasValue*/)
                {
                    y++;
                    if (y == end) break;
                }
                start = y;
            }
            else if (y == end - 1)
            {
                while (y > 0 && grid[y, x] == false/*.HasValue*/)
                {
                    y--;
                    if (y == start) break;
                }
                end = y + 1;
            }

            columnEndpoints[x] = (start, end);
            if (start == end && columnFilled[y] != 0)
            {
                throw new UnsolvablePuzzleException($"Column {x} unable to be satisfied");
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
                AdjustRowEndpoints(x, y);
                AdjustColumnEndpoints(x, y);
            }
            else if (grid[y, x] != valid)
            {
                throw new UnsolvablePuzzleException($"Tried to set cell ({x}, {y}) to conflicting values");
            }
        }

        private static int CalculateGap(List<int> segments, (int start, int end) endpoints)
        {
            int gap = endpoints.end - endpoints.start - segments[0];
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
                if(row.Count == 1 && row[0] == 0)
                {
                    await FillCompletedRow(i, waiter);
                    row.Clear();
                }
                else if(row.Count > 0)
                {
                    (int start, int end) endpoints = rowEndpoints[i];
                    int gap = CalculateGap(row, endpoints);

                    int j = endpoints.start;
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
                                if(columnFilled[j] == 0)
                                {
                                    columns[j].Clear();
                                }
                            }
                            j++;
                        }
                    }
                    if(gap == 0)
                    {
                        row.Clear();
                    }
                }
            }

            for (int j = 0; j < gridWidth; j++)
            {
                List<int> col = columns[j];
                if(col.Count == 1 && col[0] == 0)
                {
                    await FillCompletedColumn(j, waiter);
                    col.Clear();
                }
                else if (col.Count > 0)
                {
                    (int start, int end) endpoints = columnEndpoints[j];
                    int gap = CalculateGap(col, endpoints);

                    int i = endpoints.start;
                    foreach (int segmentLength in col)
                    {
                        if (segmentLength < gap)
                        {
                            i += segmentLength + 1;
                        }
                        else
                        {
                            i += gap;
                            for (int segI = gap; segI < segmentLength; segI++, i++)
                            {
                                await FillCell(j, i, true, waiter);
                                if(rowFilled[i] == 0)
                                {
                                    rows[i].Clear();
                                }
                            }
                            i++;
                        }
                    }
                    if(gap == 0)
                    {
                        col.Clear();
                    }
                }
            }

            await waiter.WaitAsync();
        }

        public void Dispose()
        {
            ConsoleBuffer.Dispose();
        }

        public class UnsolvablePuzzleException : Exception
        {
            public UnsolvablePuzzleException(string message) : base(message) { }
        }
    }
}
