using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NonogramSolver
{
    class Nonogram : IDisposable
    {
        private readonly int[][] rows, columns;
        private readonly bool[,] grid;
        private readonly int cellSize;
        private readonly ConsoleBuffer ConsoleBuffer;

        public Nonogram(int[][] rows, int[][] columns)
        {
            this.rows = rows;
            this.columns = columns;
            grid = new bool[columns.Length, rows.Length];

            List<List<string>> columnTitles = columns.Select(c => c.Select(i => i.ToString()).ToList()).ToList();
            List<string> rowTitles = rows.Select(r => string.Join(",", r)).ToList();

            cellSize = columnTitles.Max(c => c.Max(i => i.Length));
            int maxRow = rowTitles.Max(r => r.Length);
            int maxColumn = columnTitles.Max(c => c.Count);
            int totalWidth = maxRow + 1 /* left border */ + columns.Length * (cellSize + 1 /* right border */);
            int totalHeight = maxColumn + 1 /* top border */ + rows.Length * (cellSize + 1 /* bottom border */);
            
            ConsoleBuffer = new ConsoleBuffer(totalWidth, totalHeight);

            // Print columns
            for (int i = 0; i < maxColumn; i++)
            {
                ConsoleBuffer[maxRow, i] = '│';
            }
            ConsoleBuffer[maxRow, maxColumn] = '┼';
            for (int i = 0; i < columnTitles.Count; i++)
            {
                int offsetY = maxColumn - columnTitles[i].Count;
                int offsetX = maxRow + 1 /* left border */ + i * (cellSize + 1 /* right border */);
                for (int j = 0; j < columnTitles[i].Count; j++)
                {
                    ConsoleBuffer.WriteAt(offsetX, j + offsetY, columnTitles[i][j]);
                }
                ConsoleBuffer[offsetX, maxColumn] = '─';

                offsetX++;
                for (int j = 0; j < maxColumn; j++)
                {
                    ConsoleBuffer[offsetX, j] = '│';
                }
                char cross = i == columnTitles.Count - 1 ? '┤' : '┼';
                for (int j = maxColumn; j < totalHeight - 1; j++)
                {
                    ConsoleBuffer[offsetX, j] = (j - maxColumn) % (cellSize + 1 /* bottom border */) == 0 ? cross : '│';
                }
                ConsoleBuffer[offsetX, totalHeight - 1] = i == columnTitles.Count - 1 ? '┘' : '┴';
            }

            // Print rows
            for (int i = 0; i < maxRow; i++)
            {
                ConsoleBuffer[i, maxColumn] = '─';
            }
            for (int i = 0; i < rowTitles.Count; i++)
            {
                int offsetY = maxColumn + 1 /* top border */ + i * (cellSize + 1 /* bottom border */);
                ConsoleBuffer.WriteAt(maxRow - rowTitles[i].Length, offsetY, rowTitles[i] + '│');

                offsetY++;
                for (int j = 0; j < maxRow; j++)
                {
                    ConsoleBuffer[j, offsetY] = '─';
                }
                ConsoleBuffer[maxRow, offsetY] = i == rowTitles.Count - 1 ? '┴' : '┼';
                for (int j = maxRow + 1; j < totalWidth; j++)
                {
                    if ((j - maxRow) % (cellSize + 1 /* bottom border */) != 0)
                    {
                        ConsoleBuffer[j, offsetY] = '─';
                    }
                }
            }
        }

        public void Dispose()
        {
            ConsoleBuffer.Dispose();
        }
    }
}
