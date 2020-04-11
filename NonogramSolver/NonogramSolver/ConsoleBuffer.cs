using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NonogramSolver
{
    /// <summary>
    /// Wraps the console to allow writing at arbitrary positions in the console, with a fallback when this is not possible
    /// </summary>
    /// <remarks>
    /// Surrogate pairs are not supported.
    /// 
    /// Thread safety is not guaranteed.
    /// </remarks>
    class ConsoleBuffer : IDisposable
    {
        /// <summary>
        /// Gets whether this buffer is capable of showing in the console continuously
        /// </summary>
        /// <value>
        /// When true the buffer is outputting directly to the console. When false the buffer will output as one block when disposed or when manually calling <see cref="Snapshot"/>
        /// </value>
        public bool IsScreen { get; }

        /// <summary>
        /// Width of the buffer
        /// </summary>
        /// <value>
        /// The width of the buffer, in number of characters
        /// </value>
        public int Width { get; }

        /// <summary>
        /// Height of the buffer
        /// </summary>
        /// <value>
        /// The Height of the buffer, in number of characters
        /// </value>
        public int Height { get; }

        /// <summary>
        /// Constructs a new console buffer with the given <paramref name="width"/> and <paramref name="height"/>
        /// </summary>
        /// <param name="width">The width the buffer should take up in the console, in characters</param>
        /// <param name="height">The height the buffer should take up in the console, in characters</param>
        public ConsoleBuffer(int width, int height)
        {
            // Create internal buffer
            Width = width;
            Height = height;
            buffer = new char[height * width];
            Array.Fill(buffer, ' ');

            Console.OutputEncoding = Encoding.UTF8;

            // Attempt to fit buffer to console
            IsScreen = false;
            if (!Console.IsOutputRedirected)
            {
                // Check if console already big enough to fit buffer or if the console buffer can be set to the appropriate size
                if((Console.BufferWidth >= width && Console.BufferHeight >= height) || TrySetBufferSize(width, height))
                {
                    IsScreen = true;
                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        Console.SetWindowSize(Math.Max(width, Console.WindowWidth), Math.Max(height, Console.WindowHeight));
                    }

                    Console.Clear();
                }
                else // Has console screen but buffer does not fit
                {
                    // Show spinner animation to show that the program is running
                    timer = new Timer()
                    {
                        AutoReset = true,
                        Interval = 500
                    };
                    timer.Elapsed += UpdateSpinner;
                }
            }
        }

        /// <summary>
        /// Gets the character in the buffer at the position specified by <paramref name="x"/> and <paramref name="y"/>
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="x"/> or <paramref name="y"/> does not fit inside the buffer size</exception>
        /// <param name="x">Horizontal position of the character</param>
        /// <param name="y">Vertical position of the character</param>
        /// <returns>The character at the specified position within the buffer</returns>
        public char this[int x, int y]
        {
            get => buffer[y * Width + x];
        }

        /// <summary>
        /// Write the character <paramref name="c"/> at the position specified by <paramref name="x"/> and <paramref name="y"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">The buffer has already been closed</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="x"/> or <paramref name="y"/> does not fit inside the buffer size</exception>
        /// <param name="x">Horizontal position the character should be displayed</param>
        /// <param name="y">Vertical position the character should be displayed</param>
        /// <param name="c">Character to display</param>
        /// <returns>Task to wait on before writing more characters</returns>
        public Task WriteAt(int x, int y, char c, IAsyncWaiter waiter = null)
        {
            CheckValid(x, y);
            return UncheckedWriteAt(x, y, c, waiter);
        }

        private void CheckValid(int x, int y)
        {
            if (disposed) throw new InvalidOperationException("Console buffer has been closed");

            if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(y));
        }

        private Task UncheckedWriteAt(int x, int y, char c, IAsyncWaiter waiter)
        {
            buffer[y * Width + x] = c;
            if (IsScreen)
            {
                Console.SetCursorPosition(x, y);
                Console.Write(c);
            }
            return !IsScreen || waiter == null ? Task.CompletedTask : waiter.WaitAsync();
        }

        /// <summary>
        /// Write the string <paramref name="s"/> starting from the position specified by <paramref name="x"/> and <paramref name="y"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">The buffer has already been closed</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="x"/> or <paramref name="y"/> does not fit inside the buffer size</exception>
        /// <remarks>
        /// The string will be printed horizontally, replacing any existing characters.
        /// The string will automatically wrap onto a new line if it does not fit into the buffer. If the string reaches the end of the buffer the remainder will be silently ignored and not printed
        /// The wrapping of the string ignores the initial x position, it continues from x=0 on the next line.
        /// </remarks>
        /// <param name="x">Horizontal position the string should start at</param>
        /// <param name="y">Vertical position the string should start at</param>
        /// <param name="s">String to display</param>
        /// <returns>Task to wait on before writing more characters</returns>
        public async Task WriteAt(int x, int y, string s, IAsyncWaiter waiter = null)
        {
            CheckValid(x, y);
            foreach (char c in s)
            {
                await UncheckedWriteAt(x, y, c, waiter);
                x++;
                if(x >= Width)
                {
                    y++;
                    if (y >= Height) return;

                    x = 0;
                }
            }
        }

        /// <summary>
        /// Draws a horizontal line going from (<paramref name="startX"/>, <paramref name="y"/>) to (<paramref name="endX"/>, <paramref name="y"/>)
        /// </summary>
        /// <remarks>
        /// Draws a horizontal line between the start and end coordinates, combining with any existing lines in the buffer.
        /// If <paramref name="endX"/> is less than <paramref name="startX"/> they will be swapped
        /// If <paramref name="startX"/> equals <paramref name="endX"/> and both <paramref name="shortStart"/> and <paramref name="shortEnd"/> are true then no line will be drawn and bounds checking will not be performed
        /// </remarks>
        /// <exception cref="InvalidOperationException">The buffer has already been closed</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startX"/>, <paramref name="endX"/>, or <paramref name="y"/> does not fit inside the buffer size</exception>
        /// <param name="startX">Horizontal position of the start of the line</param>
        /// <param name="y">Vertical position of the line</param>
        /// <param name="endX">Horizontal position of the end of the line</param>
        /// <param name="shortStart">Whether the start of the line should take only the right half of the character width or the entire width</param>
        /// <param name="shortEnd">Whether the end of the line should take only the left half of the character width or the entire width</param>
        /// <returns>Task to wait on before writing more characters</returns>
        public async Task DrawHorizontalLine(int startX, int y, int endX, bool shortStart = false, bool shortEnd = false, IAsyncWaiter waiter = null)
        {
            if (startX == endX && shortStart && shortEnd) return; // Empty line

            if (disposed) throw new InvalidOperationException("Console buffer has been closed");

            if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(y));

            if (startX < 0 || startX >= Width) throw new ArgumentOutOfRangeException(nameof(startX));

            if (startX != endX)
            {
                if (endX < 0 || endX >= Width) throw new ArgumentOutOfRangeException(nameof(endX));

                if (endX < startX)
                {
                    (startX, endX) = (endX, startX);
                }
            }

            if (shortStart)
            {
                switch (this[startX, y])
                {
                    case '─':
                    case '┌':
                    case '└':
                    case '├':
                    case '┬':
                    case '┴':
                    case '┼':
                    case '╶':
                        break;
                    case '│':
                        await UncheckedWriteAt(startX, y, '├', waiter);
                        break;
                    case '┐':
                        await UncheckedWriteAt(startX, y, '┬', waiter);
                        break;
                    case '┘':
                        await UncheckedWriteAt(startX, y, '┴', waiter);
                        break;
                    case '┤':
                        await UncheckedWriteAt(startX, y, '┼', waiter);
                        break;
                    case '╴':
                        await UncheckedWriteAt(startX, y, '─', waiter);
                        break;
                    case '╵':
                        await UncheckedWriteAt(startX, y, '└', waiter);
                        break;
                    case '╷':
                        await UncheckedWriteAt(startX, y, '┌', waiter);
                        break;
                    default:
                        await UncheckedWriteAt(startX, y, '╶', waiter);
                        break;
                }
                startX++;
            }

            if(shortEnd)
            {
                endX--;
            }

            for (int x = startX; x <= endX; x++)
            {
                switch (this[x, y])
                {
                    case '─':
                    case '┬':
                    case '┴':
                    case '┼':
                        break;
                    case '│':
                    case '├':
                    case '┤':
                        await UncheckedWriteAt(x, y, '┼', waiter);
                        break;
                    case '┌':
                    case '┐':
                        await UncheckedWriteAt(x, y, '┬', waiter);
                        break;
                    case '└':
                    case '┘':
                        await UncheckedWriteAt(x, y, '┴', waiter);
                        break;
                    case '╵':
                        await UncheckedWriteAt(x, y, '┴', waiter);
                        break;
                    case '╷':
                        await UncheckedWriteAt(x, y, '┬', waiter);
                        break;
                    case '╴':
                    case '╶':
                    default:
                        await UncheckedWriteAt(x, y, '─', waiter);
                        break;
                }
            }

            if(shortEnd)
            {
                int x = endX + 1;
                switch (this[x, y])
                {
                    case '─':
                    case '┐':
                    case '┘':
                    case '┤':
                    case '┬':
                    case '┴':
                    case '┼':
                    case '╴':
                        break;
                    case '│':
                        await UncheckedWriteAt(x, y, '┤', waiter);
                        break;
                    case '┌':
                        await UncheckedWriteAt(x, y, '┬', waiter);
                        break;
                    case '└':
                        await UncheckedWriteAt(x, y, '┴', waiter);
                        break;
                    case '├':
                        await UncheckedWriteAt(x, y, '┼', waiter);
                        break;
                    case '╵':
                        await UncheckedWriteAt(x, y, '┘', waiter);
                        break;
                    case '╶':
                        await UncheckedWriteAt(x, y, '─', waiter);
                        break;
                    case '╷':
                        await UncheckedWriteAt(x, y, '┐', waiter);
                        break;
                    default:
                        await UncheckedWriteAt(x, y, '╴', waiter);
                        break;
                }
            }
        }

        /// <summary>
        /// Draws a vertical line going from (<paramref name="x"/>, <paramref name="startY"/>) to (<paramref name="x"/>, <paramref name="endY"/>)
        /// </summary>
        /// <remarks>
        /// Draws a vertical line between the start and end coordinates, combining with any existing lines in the buffer.
        /// If <paramref name="endY"/> is less than <paramref name="startY"/> they will be swapped
        /// If <paramref name="startY"/> equals <paramref name="endY"/> and both <paramref name="shortStart"/> and <paramref name="shortEnd"/> are true then no line will be drawn and bounds checking will not be performed
        /// </remarks>
        /// <exception cref="InvalidOperationException">The buffer has already been closed</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startY"/>, <paramref name="endY"/>, or <paramref name="x"/> does not fit inside the buffer size</exception>
        /// <param name="startY">Vertical position of the start of the line</param>
        /// <param name="x">Horizontal position of the line</param>
        /// <param name="endY">Vertical position of the end of the line</param>
        /// <param name="shortStart">Whether the start of the line should take only the bottom half of the character height or the entire height</param>
        /// <param name="shortEnd">Whether the end of the line should take only the top half of the character height or the entire height</param>
        /// <returns>Task to wait on before writing more characters</returns>
        public async Task DrawVerticalLine(int x, int startY, int endY, bool shortStart = false, bool shortEnd = false, IAsyncWaiter waiter = null)
        {
            if (startY == endY && shortStart && shortEnd) return; // Empty line

            if (disposed) throw new InvalidOperationException("Console buffer has been closed");

            if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException(nameof(x));

            if (startY < 0 || startY >= Height) throw new ArgumentOutOfRangeException(nameof(startY));

            if (startY != endY)
            {
                if (endY < 0 || endY >= Height) throw new ArgumentOutOfRangeException(nameof(endY));

                if (endY < startY)
                {
                    (startY, endY) = (endY, startY);
                }
            }

            if (shortStart)
            {
                switch (this[x, startY])
                {
                    case '─':
                        await UncheckedWriteAt(x, startY, '┬', waiter);
                        break;
                    case '│':
                    case '┌':
                    case '┐':
                    case '├':
                    case '┤':
                    case '┬':
                    case '┼':
                    case '╷':
                        break;
                    case '└':
                        await UncheckedWriteAt(x, startY, '├', waiter);
                        break;
                    case '┘':
                        await UncheckedWriteAt(x, startY, '┤', waiter);
                        break;
                    case '┴':
                        await UncheckedWriteAt(x, startY, '┼', waiter);
                        break;
                    case '╴':
                        await UncheckedWriteAt(x, startY, '┐', waiter);
                        break;
                    case '╵':
                        await UncheckedWriteAt(x, startY, '│', waiter);
                        break;
                    case '╶':
                        await UncheckedWriteAt(x, startY, '┌', waiter);
                        break;
                }
                startY++;
            }

            if (shortEnd)
            {
                endY--;
            }

            for (int y = startY; y <= endY; y++)
            {
                switch (this[x, y])
                {
                    case '─':
                    case '┬':
                    case '┴':
                        await UncheckedWriteAt(x, y, '┼', waiter);
                        break;
                    case '│':
                    case '├':
                    case '┤':
                    case '┼':
                        break;
                    case '┌':
                    case '└':
                    case '╶':
                        await UncheckedWriteAt(x, y, '├', waiter);
                        break;
                    case '┐':
                    case '┘':
                    case '╴':
                        await UncheckedWriteAt(x, y, '┤', waiter);
                        break;
                    case '╵':
                    case '╷':
                    default:
                        await UncheckedWriteAt(x, y, '│', waiter);
                        break;
                }
            }

            if (shortEnd)
            {
                int y = endY + 1;
                switch (this[x, y])
                {
                    case '─':
                        await UncheckedWriteAt(x, y, '┴', waiter);
                        break;
                    case '│':
                    case '└':
                    case '┘':
                    case '├':
                    case '┤':
                    case '┴':
                    case '┼':
                    case '╵':
                        break;
                    case '┌':
                        await UncheckedWriteAt(x, y, '├', waiter);
                        break;
                    case '┐':
                        await UncheckedWriteAt(x, y, '┤', waiter);
                        break;
                    case '┬':
                        await UncheckedWriteAt(x, y, '┼', waiter);
                        break;
                    case '╴':
                        await UncheckedWriteAt(x, y, '┘', waiter);
                        break;
                    case '╶':
                        await UncheckedWriteAt(x, y, '└', waiter);
                        break;
                    case '╷':
                        await UncheckedWriteAt(x, y, '│', waiter);
                        break;
                    default:
                        await UncheckedWriteAt(x, y, '╵', waiter);
                        break;
                }
            }
        }

        /// <summary>
        /// Force the buffer to produce an output
        /// </summary>
        /// <exception cref="InvalidOperationException">The buffer has already been closed</exception>
        /// <remarks>
        /// If <see cref="IsScreen"/> is true the cursor is attempted to be moved to after the buffer to allow for printing text. This may involve expanding the size of the console.
        /// If there is not enough space for the cursor to be moved to after the buffer it will instead be moved to the bottom right corner of the buffer
        /// 
        /// If <see cref="IsScreen"/> is false the current contents of the buffer will be immediately printed without disposing the buffer
        /// </remarks>
        public void Snapshot()
        {
            if (disposed) throw new InvalidOperationException("Console buffer has been closed");

            if (IsScreen)
            {
                // Attempt to position cursor after buffer
                if (Height >= Console.BufferHeight)
                {
                    if (TrySetBufferSize(Width, Height + 1))
                    {
                        Console.WindowHeight = Height + 1;
                        Console.SetCursorPosition(0, Height);
                    }
                    else
                    {
                        Console.SetCursorPosition(Width - 1, Height - 1);
                    }
                }
                else
                {
                    if(Height >= Console.WindowHeight && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        try
                        {
                            Console.WindowHeight = Height + 1;
                        }
                        catch (Exception) {}
                    }
                    Console.SetCursorPosition(0, Height);
                }
            }
            else
            {
                // Print out buffer
                Console.WriteLine();
                for (int i = 0, x = 0; i < buffer.Length; i++)
                {
                    Console.Write(buffer[i]);
                    x++;
                    if (x >= Width)
                    {
                        Console.WriteLine();
                        x = 0;
                    }
                }
            }
        }

        public void Close() => Dispose();

        #region IDisposable Support
        private bool disposed = false; // To detect redundant calls

        public void Dispose()
        {
            if(!disposed)
            {
                timer?.Dispose();

                try
                {
                    // Attempt to ensure that the final value of the buffer is printed
                    Snapshot();
                }
                catch (Exception) { }

                disposed = true;
            }
        }
        #endregion

        // Row-major buffer (y * width + x) for efficient printing
        // Assuming one char per character on screen
        private readonly char[] buffer;

        private static bool TrySetBufferSize(int width, int height)
        {
            // Buffer size can only be set on windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Console.SetBufferSize(width, height);
                    return true;
                }
                catch (Exception) { }
            }

            return false;
        }


        // Spinner logic
        private readonly Timer timer = null;
        private int spinnerIndex = 0;
        private static readonly char[] spinnerChars = new[] { '/', '-', '\\', '|' };

        private void UpdateSpinner(object sender, ElapsedEventArgs e)
        {
            if (disposed) return;

            spinnerIndex = (spinnerIndex + 1) % 4;
            Console.Write(spinnerChars[spinnerIndex]);
            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
        }
    }
}
