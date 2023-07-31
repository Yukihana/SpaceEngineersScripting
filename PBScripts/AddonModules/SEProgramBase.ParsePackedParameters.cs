using System;
using System.Collections.Generic;

namespace PBScripts.AddonModules
{
    internal partial class SEProgramBase
    {
        public int ParsePackedParameters(string blob, Dictionary<string, string> parameters, bool toLower = false)
        {
            var items = blob.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            int count = 0;
            foreach (var item in items)
            {
                var parts = item.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                    continue;

                string key = parts[0];
                string value = parts.Length > 1 ? parts[1] : string.Empty;

                parameters[toLower ? key.ToLowerInvariant() : key] = value;
                count++;
            }
            return count;
        }
    }
}