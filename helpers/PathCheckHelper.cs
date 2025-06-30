// Version: 1.0.0.676
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.helpers
{
    public class PathCheckHelper
    {
        public static bool IsUrl(string input)
        {
            // Sprawdź, czy string zawiera znany schemat URI (http, https, ftp, file)
            if (Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult))
            {
                return uriResult.Scheme == Uri.UriSchemeHttp
                    || uriResult.Scheme == Uri.UriSchemeHttps
                    || uriResult.Scheme == Uri.UriSchemeFtp
                    || uriResult.Scheme == Uri.UriSchemeFile;
            }
            return false;
        }

        public static bool IsFilePath(string input)
        {
            // Sprawdź typowe formaty ścieżek
            bool isWindowsPath = Regex.IsMatch(input, @"^[a-zA-Z]:\\"); // np. C:\
            bool isUnixPath = input.StartsWith("/");                    // np. /home
            bool isUncPath = input.StartsWith(@"\\");                   // np. \\server\share

            // Sprawdź, czy string zawiera niedozwolone znaki w URL (np. spacje bez kodowania)
            bool hasInvalidUrlChars = Regex.IsMatch(input, @"\s|\[|\]|\{|\}");

            return isWindowsPath || isUnixPath || isUncPath || hasInvalidUrlChars;
        }

        public static PathEnum Check(string input)
        {
            if (IsUrl(input))
            {
                Console.WriteLine($"'{input}' to URL");
                return PathEnum.isUrl;
            }
            else if (IsFilePath(input))
            {
                Console.WriteLine($"'{input}' to ścieżka pliku");
                return PathEnum.isFile;
            }
            else
            {
                Console.WriteLine($"Nie można określić typu: '{input}'");
                return PathEnum.isNone;
            }
        }

        public enum PathEnum
        {
            isNone,
            isFile,
            isUrl
        }
    }
}
