using System.Collections.Generic;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        private const string PROPERTY_PATTERN = @"\[(?<identifier>[^:]+):(?<value>[^\]]+)\]";

        protected Dictionary<string, string> ParseProperties(string blob)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            System.Text.RegularExpressions.MatchCollection matches = System.Text.RegularExpressions.Regex.Matches(blob, PROPERTY_PATTERN, System.Text.RegularExpressions.RegexOptions.Compiled);

            foreach (System.Text.RegularExpressions.Match match in matches)
                properties[match.Groups["identifier"].Value] = match.Groups["value"].Value;

            return properties;
        }
    }
}