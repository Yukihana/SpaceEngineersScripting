using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBScripts.AddonModules
{
    internal partial class SEProgramBase
    {
        private readonly Dictionary<string, System.Text.RegularExpressions.Regex> _parameterRegex
            = new Dictionary<string, System.Text.RegularExpressions.Regex>();

        private System.Text.RegularExpressions.Regex GetPollRegex(string parameterName)
        {
            System.Text.RegularExpressions.Regex regex;
            if (!_parameterRegex.TryGetValue(parameterName, out regex))
            {
                string pattern = @"\[" + parameterName + @":(?<value>[\d.]+)]";
                regex = new System.Text.RegularExpressions.Regex(pattern,
                    System.Text.RegularExpressions.RegexOptions.Compiled);
                _parameterRegex.Add(parameterName, regex);
            }
            return regex;
        }

        public bool GetMonitorScript(string scriptId, out IMyProgrammableBlock script)
        {
            var scriptTag = $"[MonitorScript:{scriptId}]";
            var scripts = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(scripts, x
                => x.IsFunctional
                && x.IsSameConstructAs(Me)
                && x.CustomData.Contains(scriptTag));
            var result = scripts.Any();
            script = result ? scripts.First() : null;
            return result;
        }

        public bool GetScriptData(IMyProgrammableBlock script, string parameterName, out string result)
        {
            var regex = GetPollRegex(parameterName);
            var sb = new StringBuilder();
            script.GetSurface(1).ReadText(sb);
            var data = sb.ToString();

            System.Text.RegularExpressions.Match match = regex.Match(data);
            result = match.Success ? match.Groups["value"].Value : string.Empty;
            return match.Success;
        }

        public bool GetScriptData(IMyProgrammableBlock script, string parameterName, out float result)
        {
            var regex = GetPollRegex(parameterName);
            var sb = new StringBuilder();
            script.GetSurface(1).ReadText(sb);
            var data = sb.ToString();

            System.Text.RegularExpressions.Match match = regex.Match(data);
            string resultString = match.Success ? match.Groups["value"].Value : string.Empty;
            result = 0f;
            if (match.Success && float.TryParse(resultString, out result))
                return true;
            return false;
        }
    }
}