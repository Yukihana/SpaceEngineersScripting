using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Independent.Maintenance.AssemblerCoop
{
    internal class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "AssemblerCoop";

        public Program()
        {
            TagSelf("IndependentScript", SCRIPT_ID);
            OutputTitle = $"Independent-{SCRIPT_ID}";
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            bool enable = true;
            bool isAuto =
                updateSource == UpdateType.Once ||
                updateSource == UpdateType.Update1 ||
                updateSource == UpdateType.Update10 ||
                updateSource == UpdateType.Update100;

            if (!isAuto && !string.IsNullOrWhiteSpace(argument))
            {
                ParseArguments(argument);
                string enabledString;
                if (Arguments.TryGetValue("mode", out enabledString) &&
                    enabledString.ToLowerInvariant() == "independent")
                    enable = false;
            }

            bool canContinue = RunCoroutine(ref _enumerator, () => MarkAssemblersToCooperate(enable), !isAuto);
            Runtime.UpdateFrequency = canContinue ? UpdateFrequency.Update10 : UpdateFrequency.None;
        }

        private IEnumerator<object> _enumerator;

        // TagSelf

        // ParseArguments

        // ParsePackedParameters

        // RunCoroutine

        // Validate

        // ScriptOutput

        // Required

        private readonly string IGNORE_MARKER = $"[{SCRIPT_ID}Ignore]";
        private const string ASSEMBLER_TYPE_ID = "MyObjectBuilder_Assembler";
        private const uint BATCH_SIZE = 32;

        private readonly Color Color0 = new Color(1f, 0f, 0.5f);
        private readonly Color Color1 = new Color(0.5f, 0f, 1f);

        private ulong _evaluated = 0;
        private readonly List<IMyAssembler> _assemblers = new List<IMyAssembler>();

        // Main Routine

        private IEnumerator<object> MarkAssemblersToCooperate(bool enable = true)
        {
            uint count = 0,
                changed = 0;
            yield return null;

            // Get assemblers
            _assemblers.Clear();
            GridTerminalSystem.GetBlocksOfType(_assemblers);
            yield return null;

            // Enumerate to validate
            foreach (IMyAssembler asm in _assemblers)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCH_SIZE == 0)
                    yield return null;

                if (!ValidateBlockOnSameConstruct(asm, IGNORE_MARKER))
                    continue;
                if (asm.BlockDefinition.TypeIdString != ASSEMBLER_TYPE_ID)
                    continue;

                count++;
                if (asm.CooperativeMode != enable)
                {
                    asm.CooperativeMode = enable;
                    changed++;
                }
            }
            yield return null;

            // Calculate
            OutputStats["Mode"] = enable ? "Cooperative" : "Independent";
            OutputStats["AssemblersTotal"] = count.ToString();
            OutputStats["AssemblersChanged"] = changed.ToString();
            OutputStats["AssemblersUnchanged"] = (count - changed).ToString();
            OutputStats["UpdateGuid"] = _evaluated.ToString();
            OutputFontColor = changed > 0 ? Color1 : Color0;
            yield return null;

            // Output and finish (no wait cycle)
            DoManualOutput();
        }
    }
}