// Version: 0.1.0.82
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ThmdPlayer.Core.controls
{
    /// <summary>
    /// Logika interakcji dla klasy ColorPicker.xaml
    /// </summary>
    public partial class ColorPicker : UserControl
    {
        public RGB Selected = new RGB();
        private double _h = 360;

        public RGB SelectedSpectrumColor
        {
            get
            {
                return Selected;
            }
            set
            {
                Selected = value;
                _hexCodeTextBlock.Background = new SolidColorBrush(Selected.Color());
                _hexCodeTextBlock.Text = "#" + Selected.Hex();
                Console.WriteLine($"Selected color changed to: {Selected}");
            }
        }

        public Color SpectrumColor
        {
            get
            {
                return _spectrumMainColorGradientStop.Color;
            }
        }

        public ColorPicker()
        {
            InitializeComponent();
                        
            _spectrumGrid.Opacity = 1;
            _spectrumGrid.VerticalAlignment = VerticalAlignment.Stretch;
            _spectrumGrid.Background = GradientBrushGenerator();
            _spectrumGrid.MouseMove += _spectrumGrid_MouseMove;
            _spectrumGrid.MouseLeftButtonDown += _spectrumGrid_MouseLeftButtonDown;

            _rgbGradientGrid.MouseMove += _rgbGradient_MouseMove;
            _rgbGradientGrid.MouseLeftButtonDown += _rgbGradientGrid_MouseLeftButtonDown;

            _spectrumMainColorGradientStop.Color = HSV.RGBFromHSV(0, 1f, 1f).Color();

            Console.WriteLine($"ColorPicker initialized with gradient spectrum.{_spectrumMainColorGradientStop.Color}");
        }

        private void _rgbGradientGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RgbGradientColor(_rgbGradientGrid, e);
        }

        private Color RgbGradientColor(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                var pos = e.GetPosition(_rgbGradientGrid);
                var x = pos.X;
                var y = pos.Y;
                RGB c;
                if (y < Height / 2)
                {
                    c = HSV.RGBFromHSV(_h, 1f, y / (Height / 2));
                }
                else
                {
                    c = HSV.RGBFromHSV(_h, ((Height / 2 )- (y - Height / 2))/Height, 1f);
                }
                _hexCodeTextBlock.Background = new SolidColorBrush(c.Color());
                _hexCodeTextBlock.Text = "#" + c.Hex();
                Selected = c;

                return c.Color();
            }

            return _spectrumMainColorGradientStop.Color;
        }

        private LinearGradientBrush GradientBrushGenerator()
        {
            var g6 = HSV.GradientSpectrum();

            LinearGradientBrush gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new Point(0, 0);
            gradientBrush.EndPoint = new Point(1, 0);
            for (int i = 0; i < g6.Length; i++)
            {
                GradientStop stop = new GradientStop(g6[i].Color(), (i) * 0.16);
                gradientBrush.GradientStops.Add(stop);
            }

            return gradientBrush;
        }

        private void _rgbGradient_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            RgbGradientColor(_rgbGradientGrid, e);
        }

        private void _spectrumGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SpectrumColorGradient(sender, e);
        }

        private void _spectrumGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SpectrumColorGradient(sender, e);
        }

        private void SpectrumColorGradient(object sender, MouseEventArgs e)
        {
            var x = e.GetPosition(_spectrumGrid).X;
            var y = e.GetPosition(_spectrumGrid).Y;
            _spectrumGrid.Margin = new Thickness(0, 0, 0, 0);

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _h = 360 * (x / this.Width);
                _spectrumMainColorGradientStop.Color = HSV.RGBFromHSV(_h, 1f, 1f).Color();
            }

            // Update the hex code text block with the selected color
            _hexCodeTextBlock.Text = $"#{_spectrumMainColorGradientStop.Color.R:X2}{_spectrumMainColorGradientStop.Color.G:X2}{_spectrumMainColorGradientStop.Color.B:X2}";
        }
    }

    public class RGB
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public RGB()
        {
            R = 0xff;
            G = 0xff;
            B = 0xff;
        }
        public RGB(double r, double g, double b)
        {
            if (r > 255 || g > 255 || b > 255) throw new Exception("RGB must be under 255 (1byte)");
            R = (byte)r;
            G = (byte)g;
            B = (byte)b;
        }
        public string Hex()
        {
            return BitConverter.ToString(new byte[] { R, G, B }).Replace("-", string.Empty);
        }
        public Color Color()
        {
            var color = new Color();
            color.R = R;
            color.G = G;
            color.B = B;
            color.A = 255;
            return color;
        }
    }
    public static class HSV
    {
        public static RGB[] GetSpectrum()
        {
            RGB[] rgbs = new RGB[360];

            for (int h = 0; h < 360; h++)
            {
                rgbs[h] = RGBFromHSV(h, 1f, 1f);
            }
            return rgbs;
        }
        public static RGB[] GradientSpectrum()
        {
            RGB[] rgbs = new RGB[7];

            for (int h = 0; h < 7; h++)
            {
                rgbs[h] = RGBFromHSV(h * 60, 1f, 1f);
            }
            return rgbs;
        }
        public static RGB RGBFromHSV(double h, double s, double v)
        {
            if (h > 360 || h < 0 || s > 1 || s < 0 || v > 1 || v < 0)
                return null;

            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60 % 2) - 1));
            double m = v - c;

            double r = 0, g = 0, b = 0;

            if (h < 60)
            {
                r = c;
                g = x;
            }
            else if (h < 120)
            {
                r = x;
                g = c;
            }
            else if (h < 180)
            {
                g = c;
                b = x;
            }
            else if (h < 240)
            {
                g = x;
                b = c;
            }
            else if (h < 300)
            {
                r = x;
                b = c;
            }
            else if (h <= 360)
            {
                r = c;
                b = x;
            }

            return new RGB((r + m) * 255, (g + m) * 255, (b + m) * 255);
        }
    }
}
