using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;

namespace NonogramSolver
{
    /// <summary>
    /// Wraps the console to allow writing at arbitrary positions in the console, with a fallback when this is not possible
    /// </summary>
    /// <remarks>
    /// This class does not handle surrogate pairs correctly
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

            // Attempt to fit buffer to console
            IsScreen = false;
            if (!Console.IsOutputRedirected)
            {
                // Check if console already big enough to fit buffer
                if (Console.BufferWidth >= width && Console.BufferHeight >= height)
                {
                    IsScreen = true;
                }
                // Otherwise attempt to increase console size
                else if(TrySetBufferSize(width, height))
                {
                    Console.SetWindowSize(width, height);
                    IsScreen = true;
                }

                if(IsScreen)
                {
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
        /// Gets or sets the character in the buffer at the position specified by <paramref name="x"/> and <paramref name="y"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">The buffer has already been closed when attempting to set, not checked when getting</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="x"/> or <paramref name="y"/> does not fit inside the buffer size</exception>
        /// <param name="x">Horizontal position of the character</param>
        /// <param name="y">Vertical position of the character</param>
        /// <returns>The character at the specified position within the buffer</returns>
        /// <seealso cref="WriteAt(int, int, char)"/>
        public char this[int x, int y]
        {
            get => buffer[y * Width + x];
            set => WriteAt(x, y, value);
        }

        /// <summary>
        /// Write the character <paramref name="c"/> at the position specified by <paramref name="x"/> and <paramref name="y"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">The buffer has already been closed</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="x"/> or <paramref name="y"/> does not fit inside the buffer size</exception>
        /// <param name="x">Horizontal position the character should be displayed</param>
        /// <param name="y">Vertical position the character should be displayed</param>
        /// <param name="c">Character to display</param>
        public void WriteAt(int x, int y, char c)
        {
            if (disposed) throw new InvalidOperationException("Console buffer has been closed");

            if (x < 0 || x >= Width) throw new ArgumentOutOfRangeException(nameof(x));
            if (y < 0 || y >= Height) throw new ArgumentOutOfRangeException(nameof(y));

            buffer[y * Width + x] = c;
            if (IsScreen)
            {
                Console.SetCursorPosition(x, y);
                Console.Write(c);
            }
        }
        /// <summary>
        /// Write the string <paramref name="s"/> starting from the position specified by <paramref name="x"/> and <paramref name="y"/>
        /// </summary>
        /// <exception cref="InvalidOperationException">The buffer has already been closed</exception>
        /// <remarks>
        /// The string will be printed horizontally, replacing any existing characters.
        /// The string will automatically wrap onto a new line if it does not fit into the buffer. If the string reaches the end of the buffer the remainder will be silently ignored and not printed
        /// The wrapping of the string ignores the initial x position, it continues from x=0 on the next line.
        /// </remarks>
        /// <param name="x">Horizontal position the string should start at</param>
        /// <param name="y">Vertical position the string should start at</param>
        /// <param name="s">String to display</param>
        public void WriteAt(int x, int y, string s)
        {
            foreach (char c in s)
            {
                WriteAt(x, y, c);
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
                        Console.SetWindowSize(Width, Height + 1);
                        Console.SetCursorPosition(0, Height);
                    }
                    else
                    {
                        Console.SetCursorPosition(Width - 1, Height - 1);
                    }
                }
                else
                {
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
