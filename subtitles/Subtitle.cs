// Version: 1.0.0.1355
// Copyright (c) 2024 Softbery by Paweł Tobis
using System;

namespace ThmdPlayer.Core.Subtitles
{
    /// <summary>
    /// Represents a single subtitle entry.
    /// </summary>
    public class Subtitle
    {
        public int Id { get; }
        public TimeSpan StartTime { get; }
        public TimeSpan EndTime { get; }
        public string[] Text { get; }

        /// <summary>
        /// Initializes a new instance of the Subtitle class.
        /// </summary>
        /// <param name="id">The sequential ID of the subtitle.</param>
        /// <param name="startTime">The start time of the subtitle.</param>
        /// <param name="endTime">The end time of the subtitle.</param>
        /// <param name="text">An array of strings representing the lines of text in the subtitle.</param>
        public Subtitle(int id, TimeSpan startTime, TimeSpan endTime, string[] text)
        {
            Id = id;
            StartTime = startTime;
            EndTime = endTime;
            Text = text;
        }

        /// <summary>
        /// Returns a string representation of the subtitle.
        /// </summary>
        /// <returns>A string containing the subtitle ID, times, and concatenated text.</returns>
        public override string ToString()
        {
            // Joins the text lines with a space for a concise representation.
            return $"[{Id}] {StartTime} --> {EndTime}: {string.Join(" ", Text)}";
        }
    }
}
