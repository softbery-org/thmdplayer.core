// Version: 1.0.0.676
using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Markup;
    using System.Windows.Media;
    using System.Xml;
using ThmdPlayer.Core.logs;

namespace ThmdPlayer.Core.images.svg
{
    public static class SvgImageHelper
    {
        /// <summary>
        /// Displays an SVG image in a WPF Image control.
        /// </summary>
        /// <param name="element">Image control</param>
        /// <param name="svgContent">Svg content</param>
        /// <param name="brush">Brush color</param>
        public static void DisplaySvgInImage(Image element, string svgContent, string brush = "#FF000000")
        {
            try
            {
                // Konwersja SVG na XAML
                var xaml = SvgToXamlConverter.ConvertSvgToDrawingImage(svgContent, brush);

                // Ładowanie XAML do obiektu
                var drawingImage = LoadDrawingImageFromXaml(xaml);

                // Przypisanie do kontrolki Image
                element.Source = drawingImage;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error on loading SVG image: {ex.Message}");
                Logger.Log.Log(LogLevel.Error, new string[] { "Console", "File" }, $"Error on loading SVG image: {ex.Message}", ex);
            }
        }

        private static DrawingImage LoadDrawingImageFromXaml(string xaml)
        {
            using (var stringReader = new StringReader(xaml))
            using (var xmlReader = XmlReader.Create(stringReader))
            {
                return (DrawingImage)XamlReader.Load(xmlReader);
            }
        }
    }
}
