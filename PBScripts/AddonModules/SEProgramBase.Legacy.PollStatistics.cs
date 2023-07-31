using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;

namespace PBScripts.AddonModules
{
    internal partial class SEProgramBase
    {
        public readonly List<string> statistics = new List<string>();

        public IEnumerator<object> PollStatistics()
        {
            statistics.Clear();
            int evaluated = 0;
            int batchsize = 16;

            // Enumerate
            var programs = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(programs);
            programs.Remove(Me);
            yield return null;

            // Copy custom data
            foreach (var program in programs)
            {
                evaluated++;

                // Validate
                if (!program.IsSameConstructAs(Me))
                    continue;
                if (!program.IsFunctional)
                    continue;
                if (!program.Enabled)
                    continue;

                // Collect
                statistics.Add(program.GetSurface(0).GetText());

                //  Yield by batch
                if (evaluated % batchsize == 0)
                    yield return null;
            }
        }

        public IEnumerator<float> GetFloatStat(string identifier)
        {
            int evaluated = 0;
            int batchsize = 16;
            float output;
            string pattern = @"\[" + identifier + @":(?<value>[\d.]+)]";
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled);

            foreach (string stats in statistics)
            {
                evaluated++;

                // Search
                System.Text.RegularExpressions.Match match = regex.Match(stats);
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