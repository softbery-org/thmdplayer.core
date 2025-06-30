// Version: 1.0.0.676
using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SharpVectors.Converters;
using SharpVectors.Dom.Svg;
using SharpVectors.Renderers.Wpf;

namespace ThmdPlayer.Core.images.svg
{

    public static class SvgImageConverter
    {
        public static ImageSource ConvertSvgToImageSource(string svgPath, Brush strokeBrush = null, Brush fillBrush = null)
        {
            var settings = new WpfDrawingSettings
            {
                IncludeRuntime = true,
                TextAsGeometry = false,
                OptimizePath = true
            };

            /*if (strokeBrush != null)
                settings.SetOverrideColor(SvgPaintType.Stroke, strokeBrush.Color);

            if (fillBrush != null)
                settings.SetOverrideColor(SvgPaintType.Fill, fillBrush.Color);
            */
            var converter = new WpfDrawingRenderer(settings);
            var reader = new FileSvgReader(settings);
            
            var result = reader.Read(svgPath);
            
            DrawingImage image = new DrawingImage(result);
            return image;
        }
    }
}
