// Version: 1.0.0.676
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Xml.Linq;

/// <summary>
/// Known issues:
///     * Upewnij się, że wartości w viewBox i ścieżkach SVG używają spacji jako separatorów
///     * Unikaj przecinków w wartościach liczbowych w SVG (używaj kropek)
///     * Jeśli SVG zawiera notację naukową (np. 1e-5), dodaj obsługę w metodach parsujących
///     
/// Repaired 
///     [11.03.2025]:
///         1.Parsowanie liczb z użyciem kultury invariant:
///             * Wszystkie double.Parse używają CultureInfo.InvariantCulture
///             * Wszystkie konwersje liczb na string używają ToString(InvariantCulture)
///         2. Poprawne formatowanie współrzędnych:
///             * Separator dziesiętny zawsze jako kropka (np. 3.14 zamiast 3,14)
///             * Spójne użycie przecinków i spacjowania w geometriach
///         3. Poprawne formatowanie ClipGeometry:
///             * Użycie spacji zamiast przecinków między współrzędnymi (np. M0 0 V24 H24 V0 H0 Z)
/// </summary>

namespace ThmdPlayer.Core.images.svg;

public static class SvgToXamlConverter
{
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public static string ConvertSvgToDrawingImage(string svgContent, string brush = "#FF000000")
    {
        var fileInfo = new FileInfo(svgContent);

        var svgDoc = XDocument.Load(svgContent);
        var svg = svgDoc.Root;

        var viewBox = GetViewBox(svg);
        var clipGeometry = CreateClipGeometry(viewBox);

        var geometries = new StringBuilder();
        foreach (var path in svg.Descendants().Where(e => e.Name.LocalName == "path"))
        {
            var geometry = ConvertPathData(path.Attribute("d").Value);
            var fill = GetFillBrush(path, brush);

            geometries.AppendLine($"<GeometryDrawing Brush=\"{fill}\" Geometry=\"{geometry}\" />");
        }

        return $@"<DrawingImage 
  x:Key=""SVG_{fileInfo.Name.Replace(fileInfo.Extension,"")}""
  xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
  xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
  <DrawingImage.Drawing>
    <DrawingGroup ClipGeometry=""{clipGeometry}"">
      {geometries}
    </DrawingGroup>
  </DrawingImage.Drawing>
</DrawingImage>";
    }

    private static string GetViewBox(XElement svg)
    {
        return svg.Attribute("viewBox")?.Value ?? "0 0 24 24";
    }

    private static string CreateClipGeometry(string viewBox)
    {
        var parts = viewBox.Split().Select(p => double.Parse(p, InvariantCulture)).ToArray();
        return $"M{parts[0].ToString(InvariantCulture)},{parts[1].ToString(InvariantCulture)} " +
               $"V{parts[3].ToString(InvariantCulture)} " +
               $"H{parts[2].ToString(InvariantCulture)} " +
               $"V{parts[1].ToString(InvariantCulture)} " +
               $"H{parts[0].ToString(InvariantCulture)} Z";
    }

    private static string GetFillBrush(XElement path, string defaultBrush)
    {
        if (path.Attribute("fill")?.Value != "none")
        {
            var fill = path.Attribute("fill")?.Value;
            return fill == "currentColor" ? defaultBrush : ParseColor(fill);
        }
        else
        {
            return defaultBrush;
        }
    }

    private static string ParseColor(string color)
    {
        if (color?.StartsWith("#") == true)
        {
            if (color.Length == 4) return $"#{color[1]}{color[1]}{color[2]}{color[2]}{color[3]}{color[3]}";
            if (color.Length == 7) return $"{color}FF";
        }
        return color ?? "#FF000000";
    }

    private static string ConvertPathData(string pathData)
    {
        var result = new StringBuilder("F1 ");
        double x = 0, y = 0;

        foreach (Match m in Regex.Matches(pathData, @"([A-Za-z])([^A-Za-z]*)"))
        {
            var cmd = m.Groups[1].Value;
            var args = m.Groups[2].Value.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            switch (cmd.ToUpper())
            {
                case "M":
                    (x, y) = ParseMove(args, cmd, x, y);
                    result.Append($"M{x.ToString(InvariantCulture)},{y.ToString(InvariantCulture)} ");
                    break;

                case "V":
                    y = ParseVertical(args, cmd, y);
                    result.Append($"L{x.ToString(InvariantCulture)},{y.ToString(InvariantCulture)} ");
                    break;

                case "L":
                    (x, y) = ParseLine(args, cmd, x, y);
                    result.Append($"L{x.ToString(InvariantCulture)},{y.ToString(InvariantCulture)} ");
                    break;

                case "A":
                    (x, y, var arc) = ParseArc(args, cmd, x, y);
                    result.Append(arc);
                    break;

                case "Z":
                    result.Append("Z ");
                    break;
            }
        }
        return result.ToString().Trim();
    }

    private static (double, double) ParseMove(string[] args, string cmd, double x, double y)
    {
        var newX = double.Parse(args[0], InvariantCulture);
        var newY = double.Parse(args[1], InvariantCulture);
        return cmd == "m"
            ? (x + newX, y + newY)
            : (newX, newY);
    }

    private static double ParseVertical(string[] args, string cmd, double y)
    {
        var val = double.Parse(args[0], InvariantCulture);
        return cmd == "v" ? y + val : val;
    }

    private static (double, double) ParseLine(string[] args, string cmd, double x, double y)
    {
        var newX = double.Parse(args[0], InvariantCulture);
        var newY = double.Parse(args[1], InvariantCulture);
        return cmd == "l"
            ? (x + newX, y + newY)
            : (newX, newY);
    }

    private static (double, double, string) ParseArc(string[] args, string cmd, double x, double y)
    {
        var rx = double.Parse(args[0], InvariantCulture);
        var ry = double.Parse(args[1], InvariantCulture);
        var rot = double.Parse(args[2], InvariantCulture);
        var la = args[3] == "1";
        var sw = args[4] == "1";
        var ex = double.Parse(args[5], InvariantCulture);
        var ey = double.Parse(args[6], InvariantCulture);

        if (cmd == "a")
        {
            ex += x;
            ey += y;
        }

        var arc = $"A{rx.ToString(InvariantCulture)},{ry.ToString(InvariantCulture)} " +
                  $"{rot.ToString(InvariantCulture)} " +
                  $"{(la ? 1 : 0)} {(sw ? 1 : 0)} " +
                  $"{ex.ToString(InvariantCulture)},{ey.ToString(InvariantCulture)} ";
        return (ex, ey, arc);
    }
}
