using Sandbox.ModAPI;
using System;
using System.Collections.Generic;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        [Obsolete("Use ParseCookies instead. More robust rules.")]
        public Dictionary<string, string> GetDataSection(IMyTerminalBlock terminalBlock, string section)
        {
            string[] lines = terminalBlock.CustomData.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string currentSection = string.Empty;
            Dictionary<string, string> properties = new Dictionary<string, string>();

            foreach (string line in lines)
            {
                // ignore comments
                if (line.StartsWith(";"))
                    continue;

                // read section name
                if (line == "[]")
                {
                    currentSection = string.Empty;
                    continue;
                }
                else if (line.Length > 2 && line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2);
                    continue;
                }

                // ignore irrelevant sections
                if (currentSection != section)
                    continue;

                // Add to properties
                var lineSegments = line.Split(new char[] { '=' }, 2);
                string name = lineSegments[0];
                string value = string.Empty;
                if (lineSegments.Length == 2)
                    value = lineSegments[1];
                properties[name] = value;
            }

            return properties;
        }
    }
}