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

        public Nonogram(int[][] rows, int[][] columns)
        {
            this.rows = rows;
            this.columns = columns;
            grid = new bool[columns.Length, rows.Length];

            columnTitles = columns.Select(c => c.Select(i => i.ToString()).ToList()).ToList();
            rowTitles = rows.Select(r => string.Join(",", r)).ToList();

            cellSize = columnTitles.Max(c => c.Max(i => i.Length));
            maxRow = rowTitles.Max(r => r.Length);
            maxColumn = columnTitles.Max(c => c.Count);
            totalWidth = maxRow + 1 /* left border */ + columns.Length * (cellSize + 1 /* right border */);
            totalHeight = maxColumn + 1 /* top border */ + rows.Length * (cellSize + 1 /* bottom border */);

            ConsoleBuffer = new ConsoleBuffer(totalWidth, totalHeight);
        }

        public async Task Draw(int characterDelay)
        {
            ConsoleBuffer.CharacterDelay = characterDelay;

            // Print columns
            await ConsoleBuffer.DrawVerticalLine(maxRow, 0, totalHeight - 1, shortEnd: true);
            for (int i = 0; i < columnTitles.Count; i++)
            {
                int offsetY = maxColumn - columnTitles[i].Count;
                int offsetX = maxRow + 1 /* left border */ + i * (cellSize + 1 /* right border */);
                for (int j = 0; j < columnTitles[i].Count; j++)
                {
                    await ConsoleBuffer.WriteAt(offsetX, j + offsetY, columnTitles[i][j]);
                }
                offsetX++;
                await ConsoleBuffer.DrawVerticalLine(offsetX, 0, totalHeight - 1, shortEnd: true);
            }

            // Print rows
            await ConsoleBuffer.DrawHorizontalLine(0, maxColumn, totalWidth - 1, shortEnd: true);
            for (int i = 0; i < rowTitles.Count; i++)
            {
                int offsetY = maxColumn + 1 /* top border */ + i * (cellSize + 1 /* bottom border */);
                await ConsoleBuffer.WriteAt(maxRow - rowTitles[i].Length, offsetY, rowTitles[i]);
                offsetY++;
                await ConsoleBuffer.DrawHorizontalLine(0, offsetY, totalWidth - 1, shortEnd: true);
            }
        }

        public void Dispose()
        {
            ConsoleBuffer.Dispose();
        }
    }
}
