// Version: 1.0.0.1355
// Copyright (c) 2024 Softbery by Paweł Tobis
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows; // Still needed if you intend to keep MessageBox.Show at a higher UI level.
                      // For a pure library, this dependency would be removed.

namespace ThmdPlayer.Core.Subtitles
{
    /// <summary>
    /// Custom exception for errors occurring during subtitle file loading.
    /// </summary>
    public class SubtitleLoadException : Exception
    {
        public SubtitleLoadException() { }
        public SubtitleLoadException(string message) : base(message) { }
        public SubtitleLoadException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Custom exception for errors occurring during subtitle content parsing.
    /// </summary>
    public class SubtitleParseException : Exception
    {
        public SubtitleParseException() { }
        public SubtitleParseException(string message) : base(message) { }
        public SubtitleParseException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Subtitles manager class for parsing and managing SRT files.
    /// </summary>
    /// <example>
    /// SRT file structure:
    ///
    /// 1
    /// 00:00:03,400 --> 00:00:06,177
    /// In this lesson, we're going to
    /// be talking about finance. And
    ///
    /// 2
    /// 00:00:06,177 --> 00:00:10,009
    /// one of the most important aspects
    /// of finance is interest.
    /// </example>
    public class SubtitleManager
    {
        // Changed from static to instance field. Each instance of SubtitleManager
        // will now manage its own file path and subtitle collection.
        private string _subtitlesFile = "";
        private List<Subtitle> _subtitles = new List<Subtitle>();

        /// <summary>
        /// Gets the list of parsed subtitles.
        /// Returns a copy to prevent external modification of the internal list.
        /// </summary>
        public List<Subtitle> Subtitles
        {
            get => new List<Subtitle>(_subtitles); // Return a new list containing the elements
        }

        /// <summary>
        /// Gets the count of subtitle lines.
        /// </summary>
        public int Count
        {
            get => _subtitles.Count;
        }

        /// <summary>
        /// Gets or sets the subtitles file path. Setting this property will
        /// attempt to read and parse the new SRT file.
        /// </summary>
        public string Path
        {
            get => _subtitlesFile;
            set
            {
                // Only attempt to load if the path has actually changed.
                if (_subtitlesFile != value)
                {
                    try
                    {
                        // Call the refactored loading method.
                        LoadSubtitlesFromFile(value);
                        _subtitlesFile = value; // Update the internal path only upon successful load.
                    }
                    catch (SubtitleLoadException ex)
                    {
                        // Handle exceptions from file loading/parsing.
                        // For a library, it's better to re-throw or log.
                        // Keeping MessageBox.Show for now as per original code's intent for UI feedback.
                        MessageBox.Show($"Error loading subtitles: {ex.Message}");
                    }
                    catch (SubtitleParseException ex)
                    {
                        MessageBox.Show($"Error parsing subtitle content: {ex.Message}");
                    }
                    catch (Exception ex) // Catch any other unexpected exceptions
                    {
                        MessageBox.Show($"An unexpected error occurred: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the SubtitleManager class with a specified subtitle file path.
        /// </summary>
        /// <param name="path">The path to the SRT subtitle file.</param>
        public SubtitleManager(string path)
        {
            try
            {
                // Call the refactored loading method.
                LoadSubtitlesFromFile(path);
                _subtitlesFile = path; // Update the internal path only upon successful load.
            }
            catch (SubtitleLoadException ex)
            {
                MessageBox.Show($"Error loading subtitles on initialization: {ex.Message}");
                // Optionally, re-throw if the constructor must guarantee loaded subtitles.
            }
            catch (SubtitleParseException ex)
            {
                MessageBox.Show($"Error parsing subtitle content on initialization: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred on initialization: {ex.Message}");
            }
        }

        /// <summary>
        /// Get Subtitles dictionary. This method is now redundant since the Subtitles property
        /// provides the same functionality. However, keeping it for backward compatibility.
        /// </summary>
        /// <returns>Full subtitles dictionary</returns>
        public List<Subtitle> GetSubtitles()
        {
            return new List<Subtitle>(_subtitles); // Return a copy
        }

        /// <summary>
        /// Gets subtitles that fall within a specified time range.
        /// </summary>
        /// <param name="start">The start TimeSpan for the desired range.</param>
        /// <param name="end">The end TimeSpan for the desired range.</param>
        /// <returns>A list of subtitles that start after or at 'start' and end before or at 'end'.</returns>
        public List<Subtitle> GetStartToEndTimeSpan(TimeSpan start, TimeSpan end)
        {
            // Using LINQ for a more concise and readable filter operation.
            return _subtitles
                .Where(item => item.StartTime >= start && item.EndTime <= end)
                .ToList();
        }

        /// <summary>
        /// Reads the content of a file.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <returns>The content of the file as a string, or throws an exception if the file does not exist or cannot be read.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified file does not exist.</exception>
        /// <exception cref="IOException">Thrown if an I/O error occurs during file reading.</exception>
        private string ReadFileContent(string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists)
            {
                throw new FileNotFoundException("Subtitle file not found.", path);
            }

            try
            {
                // File.ReadAllText can automatically detect encoding for some files,
                // but explicitly specifying UTF8 is good for SRTs.
                return File.ReadAllText(file.FullName, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // Wrap the exception to provide more context,
                // or simply re-throw if a higher layer handles it.
                throw new IOException($"Could not read subtitle file '{path}'.", ex);
            }
        }

        /// <summary>
        /// Reads and parses an SRT file using regular expressions.
        /// This method now handles clearing existing subtitles and robust parsing.
        /// </summary>
        /// <param name="path">The path to the SRT file.</param>
        private void LoadSubtitlesFromFile(string path)
        {
            // Clear any previously loaded subtitles before parsing a new file.
            _subtitles.Clear();

            string fileContent;
            try
            {
                fileContent = ReadFileContent(path);
            }
            catch (Exception ex)
            {
                // Re-throw as a specific subtitle load exception for better handling.
                throw new SubtitleLoadException($"Failed to load file '{path}'.", ex);
            }

            // Regex to match SRT blocks:
            // Group 1: Subtitle ID (e.g., "1")
            // Group 2: Start time (e.g., "00:00:03,400")
            // Group 3: End time (e.g., "00:00:06,177")
            // Group 4: Subtitle text (non-greedy, captures all lines until next block or end of file)
            // \r?\n handles both Windows (\r\n) and Unix (\n) line endings.
            // (.*?) non-greedy match for text.
            // (?=\r?\n\r?\n\d+|$): Positive lookahead for two newlines followed by a digit (start of next block) OR end of string.
            Regex regex = new Regex(
                @"(\d+)\r?\n" + // Subtitle ID and newline
                @"(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})\r?\n" + // Timestamps and arrow
                @"(.*?)" + // Non-greedy match for subtitle text (can be multiple lines)
                @"(?=\r?\n\r?\n\d+|$)", // Lookahead for next subtitle block or end of file
                RegexOptions.Multiline // Allows '.' to match newlines for the text group
            );

            var matches = regex.Matches(fileContent);

            if (matches.Count == 0 && !string.IsNullOrWhiteSpace(fileContent))
            {
                // If there's content but no matches, it implies a parsing issue or malformed file.
                throw new SubtitleParseException("No subtitle blocks found or file is malformed.");
            }

            foreach (Match match in matches)
            {
                try
                {
                    // Convert ID
                    var id = int.Parse(match.Groups[1].Value);

                    // TimeSpan.Parse expects '.' for milliseconds, but SRT uses ','. Replace it.
                    var startTime = TimeSpan.Parse(match.Groups[2].Value.Replace(',', '.'));
                    var endTime = TimeSpan.Parse(match.Groups[3].Value.Replace(',', '.'));

                    // Split text by newlines and remove any empty entries (e.g., from trailing newlines in a block)
                    var text = match.Groups[4].Value.Trim()
                                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    _subtitles.Add(new Subtitle(id, startTime, endTime, text));
                }
                catch (FormatException ex)
                {
                    // Catch specific parsing errors for individual blocks and provide context.
                    throw new SubtitleParseException($"Error parsing subtitle block (ID: {match.Groups[1].Value}). " +
                                                    $"Check format of times or text. Raw block: \n{match.Value}", ex);
                }
                catch (Exception ex)
                {
                    // Catch any other unexpected errors during block processing.
                    throw new SubtitleParseException($"An unexpected error occurred while processing subtitle block (ID: {match.Groups[1].Value}).", ex);
                }
            }
        }
    }
}
