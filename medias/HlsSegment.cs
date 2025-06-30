// Version: 1.0.0.676
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.medias
{
    public class HlsSegment
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public TimeSpan Duration { get; set; }
        public string Uri { get; set; }
        public bool IsDiscontinuity { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }
}
