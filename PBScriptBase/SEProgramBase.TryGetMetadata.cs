﻿using Sandbox.ModAPI.Ingame;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        public bool TryGetMetadata(IMyTerminalBlock block, string key, out string value)
        {
            string pattern = @"\[" + System.Text.RegularExpressions.Regex.Escape(key) + @":(?<value>.*?)\]";
            System.Text.RegularExpressions.Match match
                = System.Text.RegularExpressions.Regex.Match(block.CustomData, pattern);

            if (match.Success)
            {
                value = match.Groups["value"].Value;
                return true;
            }
            value = string.Empty;
            return false;
        }
    }
}