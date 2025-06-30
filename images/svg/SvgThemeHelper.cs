// Version: 1.0.0.676
using System.Windows.Media;
using System.Windows;

namespace ThmdPlayer.Core.images.svg
{
    public class SvgThemeHelper
    {
        public static readonly DependencyProperty ThemeBrushProperty =
            DependencyProperty.RegisterAttached("ThemeBrush",
                typeof(Brush),
                typeof(SvgThemeHelper),
                new PropertyMetadata(null, OnThemeBrushChanged));

        public static void SetThemeBrush(DependencyObject obj, Brush value) => obj.SetValue(ThemeBrushProperty, value);

        public static Brush GetThemeBrush(DependencyObject obj) => (Brush)obj.GetValue(ThemeBrushProperty);

        private static void OnThemeBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is System.Windows.Controls.Image img && img.Source is DrawingImage drawingImage)
            {
                var newBrush = e.NewValue as Brush;
                ApplyThemeToDrawing(drawingImage.Drawing, newBrush);
            }
        }

        private static void ApplyThemeToDrawing(Drawing drawing, Brush brush)
        {
            if (drawing is DrawingGroup group)
            {
                foreach (var child in group.Children)
                {
                    ApplyThemeToDrawing(child, brush);
                }
            }
            else if (drawing is GeometryDrawing geometryDrawing)
            {
                geometryDrawing.Brush = brush;
            }
        }
    }
}
