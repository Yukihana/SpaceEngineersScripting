using System.Diagnostics;
using System.Text;

namespace PBScriptBase
{
    // Live search cookie
    // Uses standard procedure as in Parse
    // But doesn't store anything and returns early
    public partial class SEProgramBase
    {
        public static bool SearchCookie(string input, string key, out string value)
        {
            value = null;

            // Whitespace and Null are not valid keys.
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // states
            StringBuilder buffer = new StringBuilder();
            bool isComment = true;
            bool isKey = true;
            bool isRequired = false;

            // temp
            bool escaping = false;
            string str;

            foreach (char c in input)
            {
                if (escaping)
                {
                    // Passthrough
                    escaping = false;
                }
                else if (c == '\\')
                {
                    // Mark next character as compulsory passthrough
                    escaping = true;
                    continue;
                }
                else if (c == '[')
                {
                    // Reset parameters when a cookie starts.
                    if (isComment)
                    {
                        isComment = false;
                        isKey = true;
                        isRequired = false;
                    }
                    continue;
                }
                else if (isComment)
                {
                    // optimize: enders don't work in comments.
                    continue;
                }
                else if (c == ':')
                {
                    if (isKey)
                    {
                        str = buffer.ToString();
                        buffer.Clear();

                        str = str.Trim();
                        if (string.IsNullOrWhiteSpace(str))
                            str = null;

                        isRequired = str != null && key == str;
                        isKey = false;
                    }

                    continue;
                }
                else if (c == ']')
                {
                    str = buffer.ToString();
                    buffer.Clear();

                    str = str.Trim();
                    if (string.IsNullOrWhiteSpace(str))
                        str = null;

                    if (isKey && str == key)
                    {
                        return true;
                    }
                    else if (!isKey && isRequired)
                    {
                        value = str;
                        return true;
                    }

                    isComment = true;
                    continue;
                }

                if (!isComment)
                {
                    if (isKey || isRequired)
                        buffer.Append(c);
                }
            }

            return false;
        }
    }
}