// Version: 1.0.0.665
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ThmdPlayer.Core.Subtitles
{
    public class SubtitleFontArgs : EventArgs
    {
        public FontFamily FontFamily { get; set; } = new FontFamily("Calibri");
        public double FontSize { get; set; } = 24;
        public FontWeight FontWeight { get; set; } = FontWeights.Normal;
        public TextDecorationCollection FontDecoration { get; set; } = TextDecorations.Baseline;
    }
}
