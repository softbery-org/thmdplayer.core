// Version: 1.0.0.395
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThmdPlayer.Core.helpers
{
    public class HelpGenerator
    {
        private readonly string _programName;
        private readonly string _description;
        private readonly List<HelpOption> _options;

        public HelpGenerator(string programName, string description, List<HelpOption> options)
        {
            _programName = programName;
            _description = description;
            _options = options;
        }

        public void CheckHelp(string[] args)
        {
            if (args.Any(arg => arg == "--help" || arg == "--_h" || arg == "-_h"))
            {
                Console.WriteLine(GenerateHelp());
                Environment.Exit(0);
            }
        }

        private string GenerateHelp()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Użycie: {_programName} [OPCJE]");
            sb.AppendLine();
            sb.AppendLine($"Opis: {_description}");
            sb.AppendLine();
            sb.AppendLine("Opcje:");

            foreach (var option in _options)
            {
                var flags = FormatFlags(option);
                sb.AppendLine($"  {flags,-30}  {option.Description}");
            }

            return sb.ToString();
        }

        private string FormatFlags(HelpOption option)
        {
            var joinedFlags = string.Join(" lub ", option.Flags);
            return option.HasValue
                ? $"{joinedFlags} {{{option.ValueType}}}"
                : joinedFlags;
        }
    }

}
