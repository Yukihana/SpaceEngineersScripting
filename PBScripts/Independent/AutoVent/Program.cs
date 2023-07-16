using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Independent.AutoVent
{
    // AutoVent
    // This script should not have a fixed minimum interval
    internal class Program : SEProgramBase
    {
        public Program()
        {
            ModuleID = "AutoVent";
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            bool reset = false;
            if (updateSource == UpdateType.Trigger ||
                updateSource == UpdateType.Terminal)
            {
                if (string.IsNullOrEmpty(argument))
                    reset = true;
                else
                    ProcessArgument(argument);
                return;
            }

            CycleCoroutine(ref _enumerator_input, () => SyncInput(), reset);
            _depressurize = _flags.Contains(DEPRESSURIZE_FLAG);
            _stats[$"{ModuleID}IsPressurizing"] = (!_depressurize).ToString();
            _outputFontColor = _depressurize ? _colorDepressurizing : _colorPressurizing;
            CycleCoroutine(ref _enumerator_updater, () => UpdateVents(), reset);
            CycleCoroutine(ref _enumerator_output, () => SyncOutput(), reset);
        }

        private IEnumerator<bool> _enumerator_updater = null;
        private IEnumerator<bool> _enumerator_output = null;
        private IEnumerator<bool> _enumerator_input = null;

        // SurfaceInput

        // SurfaceOuput

        // CycleCoroutine

        // Validate

        // Shared parameters

        private const uint BATCH_SIZE = 32;
        private const string DEPRESSURIZE_FLAG = "Depressurize";
        private string IgnoreMarker => $"{ModuleID}Ignore";
        private readonly Color _colorPressurizing = new Color(0f, 1f, 0.5f);
        private readonly Color _colorDepressurizing = new Color(1f, 0.5f, 0.5f);

        private uint _evaluated = 0;
        private bool _depressurize = false;
        private const float OXYGEN_MAX = 0.98f;
        private const float OXYGEN_MIN = 0.75f;

        private IEnumerator<bool> UpdateVents()
        {
            var vents = new List<IMyAirVent>();
            uint relevents = 0, currentIndex = 0;
            GridTerminalSystem.GetBlocksOfType(vents);
            _stats[$"{ModuleID}TotalVents"] = vents.Count.ToString();

            yield return true;
            var ignoreMarker = IgnoreMarker;

            foreach (var vent in vents)
            {
                unchecked { _evaluated++; }
                currentIndex++;

                if (ValidateBlockOnSameConstruct(vent, ignoreMarker))
                {
                    relevents++;

                    if (_depressurize)
                    {
                        vent.Depressurize = true;
                        vent.Enabled = vent.GetOxygenLevel() > 0f;
                    }
                    else
                    {
                        vent.Depressurize = false;
                        var level = vent.GetOxygenLevel();
                        if (level >= OXYGEN_MAX)
                            vent.Enabled = false;
                        else if (level <= OXYGEN_MIN)
                            vent.Enabled = true;
                    }
                }

                if (_evaluated % BATCH_SIZE == 0)
                {
                    _stats[$"{ModuleID}CurrentIndex"] = currentIndex.ToString();
                    yield return true;
                }
            }

            _stats[$"{ModuleID}RelevantVents"] = relevents.ToString();
        }
    }
}