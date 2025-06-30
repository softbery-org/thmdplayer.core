// Version: 1.0.0.384
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.helpers
{
    public class HelpOption
    {
        public List<string> Flags { get; }
        public string Description { get; }
        public bool HasValue { get; }
        public string ValueType { get; }

        public HelpOption(List<string> flags, string description, bool hasValue = false, string valueType = "bool")
        {
            Flags = flags;
            Description = description;
            HasValue = hasValue;
            ValueType = valueType;
        }
    }
}
