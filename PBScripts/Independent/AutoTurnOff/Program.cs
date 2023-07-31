using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Independent.AutoTurnOff
{
    internal class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "AutoTurnOff";

        public Program()
        {
            OutputTitle = "AutoTurnOff";
            OutputFontColor = new Color(1f, 0.5f, 0.5f);
            TagSelf("IndependentScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => AutoTurnOffBlocks()); }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // CycleCoroutine

        // ScriptOutput

        // Validate

        // TypeConfig

        private bool IsTargetType(IMyTerminalBlock block)
        {
            if (block is IMyThrust &&
                block.BlockDefinition.SubtypeId.Contains("AtmosphericThruster"))
                return true;

            // System.Type.IsAssignableFrom is prohibited
            return false;
        }

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(2);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(5);

        private uint _evaluated = 0;
        private const int BATCHSIZE = 32;

        private ulong _total = 0;

        // Routine

        private IEnumerator<object> AutoTurnOffBlocks()
        {
            DateTime startTime = DateTime.UtcNow;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            uint count = 0;

            // Get all blocks
            var blocks = new List<IMyFunctionalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks);
            yield return null;

            // Validate and shut them down
            foreach (var block in blocks)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCHSIZE == 0)
                    yield return null;

                if (!ValidateBlockOnSameConstruct(block))
                    continue;

                if (!IsTargetType(block))
                    continue;

                block.Enabled = false;
                count++;
            }
            yield return null;

            // Calculate
            unchecked { _total += count; }
            OutputStats["BlocksDisabledThisCycle"] = count.ToString();
            OutputStats["BlocksDisabledTotal"] = _total.ToString();
            OutputStats["UpdateIndex"] = _evaluated.ToString();
            DoManualOutput();
            yield return null;

            // On early finish, wait for interval
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
                yield return null;
        }
    }
}