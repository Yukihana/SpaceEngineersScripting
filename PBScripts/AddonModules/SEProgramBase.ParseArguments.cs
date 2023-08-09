using System.Collections.Generic;

namespace PBScripts.AddonModules
{
    internal partial class SEProgramBase
    {
        public readonly Dictionary<string, string> Arguments = new Dictionary<string, string>();

        public bool ParseArguments(string argument)
        {
            Arguments.Clear();
            return ParsePackedParameters(argument, Arguments, true) > 0;
        }
    }
}