// Version: 1.0.0.461
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.databases.exceptions
{
    public class VersionMismatchException : Exception
    {
        public Version Expected { get; }
        public Version Actual { get; }

        public VersionMismatchException(Version expected, Version actual)
            : base($"Niezgodność wersji. Oczekiwano: {expected}, Aktualna: {actual}")
        {
            Expected = expected;
            Actual = actual;
        }
    }
}
