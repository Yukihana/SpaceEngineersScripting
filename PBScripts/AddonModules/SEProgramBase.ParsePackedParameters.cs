using System;
using System.Collections.Generic;

namespace PBScripts.AddonModules
{
    internal partial class SEProgramBase
    {
        private readonly char[] PARAM_SEPARATORS = new[] { ';' };
        private readonly char[] PARAM_SPLITTERS = new[] { '=' };

        public int ParsePackedParameters(string blob, Dictionary<string, string> parameters, bool lowerCaseKeys = false)
        {
            var items = blob.Split(PARAM_SEPARATORS, StringSplitOptions.RemoveEmptyEntries);
            int count = 0;
            foreach (var item in items)
            {
                var parts = item.Split(PARAM_SPLITTERS, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                string key = parts[0];
                string value = parts.Length > 1 ? parts[1] : string.Empty;

                parameters[lowerCaseKeys ? key.ToLowerInvariant().Trim() : key] = value.Trim();
                count++;
            }
            return count;
        }
    }
}