using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
        // Method to retrieve a value from programmable blocks based on [identifier:value]
        protected IEnumerator<string> GetData(string identifier)
        {
            int evaluated = 0;
            int batchsize = 10;
            string pattern = @"\[" + identifier + @":(?<value>\w+)]";
            Regex regex = new Regex(pattern, RegexOptions.Compiled);
            yield return string.Empty;

            // Enumerate
            var programs = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(programs);
            yield return string.Empty;

            foreach (var program in programs)
            {
                evaluated++;

                // Validate
                if (program.CubeGrid != Me.CubeGrid)
                    continue;
                if (!program.IsFunctional)
                    continue;
                if (!program.IsWorking)
                    continue;

                // Accumulate
                Match match = regex.Match(program.CustomData);
                if (match.Success)
                {
                    yield return match.Groups["value"].Value;
                    yield break;
                }

                //  Yield by batch
                if (evaluated % batchsize == 0)
                    yield return string.Empty;
            }
        }

        // Float
        protected IEnumerator<float> GetPolledDataAsFloat(string identifier)
        {
            float parsed;
            IEnumerator<string> enumerator = GetData(identifier);
            while (enumerator.MoveNext() && float.TryParse(enumerator.Current, out parsed))
                yield return parsed;
        }
    }
}