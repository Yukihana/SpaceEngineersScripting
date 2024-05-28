using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Input;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        // ParseCookies Rules
        // Anything outside [] are comments.
        // Separator is : (Only the first one)
        // Escape char is \
        // Special characters are parsed as is.
        // Can be added to Keys and Values. (eg space and newline)
        // Example: [Apple:Book]Comment[Cat:Dog]
        // Subsequent `[]` will just be omitted if not escaped.
        // No need to escape quotes since they're not parse critical.
        // TODO force omit critical characters if they don't belong.
        // Critical characters need to be escaped for consistency

        public static int ParseCookies(string input, Dictionary<string, string> existing)
        {
            StringBuilder currentKey = new StringBuilder();
            StringBuilder currentValue = new StringBuilder();
            bool isComment = true;
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
                else if (c == '[')
                {
                    if (isComment)
                    {
                        isComment = false;
                        isKey = true;
                    }
                    continue;
                }
                else if (c == ']')
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

                    isComment = true;
                    currentKey.Clear();
                    currentValue.Clear();
                    continue;
                }
                else if (c == ':')
                {
                    isKey = false;
                    continue;
                }

                if (!isComment)
                {
                    if (isKey)
                        currentKey.Append(c);
                    else
                        currentValue.Append(c);
                }
            }

            return count;
        }

        public static Dictionary<string, string> ParseCookies(string input)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            int count = ParseCookies(input, result);
            return result;
        }
    }
}