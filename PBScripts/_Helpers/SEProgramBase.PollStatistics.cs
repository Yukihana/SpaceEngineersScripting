using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
        private readonly List<string> statistics = new List<string>();

        // Poll statistics
        protected IEnumerator<bool> PollStatistics()
        {
            statistics.Clear();
            int evaluated = 0;
            int batchsize = 16;

            // Enumerate
            var programs = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(programs);
            programs.Remove(Me);
            yield return true;

            // Copy custom data
            foreach (var program in programs)
            {
                evaluated++;

                // Validate
                if (!program.IsSameConstructAs(Me))
                    continue;
                if (!program.IsFunctional)
                    continue;
                if (!program.IsWorking)
                    continue;

                // Collect
                statistics.Add(program.CustomData);

                //  Yield by batch
                if (evaluated % batchsize == 0)
                    yield return true;
            }
        }

        protected IEnumerator<float> GetFloatStat(string identifier)
        {
            int evaluated = 0;
            int batchsize = 16;
            float output;
            string pattern = @"\[" + identifier + @":(?<value>\w+)]";
            Regex regex = new Regex(pattern, RegexOptions.Compiled);

            foreach (string stats in statistics)
            {
                evaluated++;

                // Search
                Match match = regex.Match(stats);
                if (match.Success && float.TryParse(match.Groups["value"].Value, out output))
                {
                    yield return output;
                    yield break;
                }

                //  Yield by batch
                if (evaluated % batchsize == 0)
                    yield return float.NaN;
            }
        }
    }
}