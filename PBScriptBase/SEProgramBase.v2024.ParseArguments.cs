using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PBScriptBase
{
    // Since we're not parsing based on doublequotes, we dont need to handle or escape them.
    // Delimit by separators and splitters. Escape them using backslash.
    public partial class SEProgramBase
    {
        public static int ParseArguments(
            string input,
            char[] splitters,
            char[] separators,
            Dictionary<string, string> existing,
            bool forceLowerCaseKeys = false)
        {
            StringBuilder currentKey = new StringBuilder();
            StringBuilder currentValue = new StringBuilder();
            bool isKey = true;
            int count = 0;

            bool escaping = false;
            foreach (char c in input)
            {
                if (escaping)
                {
                    escaping = false;
                }
                else if (c == '\\')
                {
                    escaping = true;
                    continue;
                }
                else if (splitters.Contains(c))
                {
                    string key = currentKey.ToString().Trim();
                    currentKey.Clear();

                    if (!string.IsNullOrEmpty(key))
                    {
                        string value = currentValue.ToString().Trim();
                        currentValue.Clear();
                        existing[key] = string.IsNullOrEmpty(value) ? null : value;
                        count++;
                    }

                    isKey = true;
                    currentKey.Clear();
                    currentValue.Clear();
                    continue;
                }
                else if (separators.Contains(c))
                {
                    isKey = false;
                    continue;
                }

                if (isKey)
                    currentKey.Append(forceLowerCaseKeys ? char.ToLowerInvariant(c) : c);
                else
                    currentValue.Append(c);
            }

            return count;
        }

        public static Dictionary<string, string> ParseArguments(string input, bool forceLowerCaseKeys = false)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            int count = ParseArguments(
                input: input,
                splitters: new char[] { ';' },
                separators: new char[] { '=' },
                forceLowerCaseKeys: forceLowerCaseKeys,
                existing: result);
            return result;
        }

        public static int ParseArguments(string input, Dictionary<string, string> existing, bool forceLowerCaseKeys = false) => ParseArguments(
            input: input,
            splitters: new char[] { ';' },
            separators: new char[] { '=' },
            forceLowerCaseKeys: forceLowerCaseKeys,
            existing: existing);
    }
}