using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Independent.AutoVent
{
    // AutoVent
    // This script should not have a fixed minimum interval
    public partial class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "AutoVent";

        public Program()
        {
            OutputTitle = $"LifeSupport-{SCRIPT_ID}";
            TagSelf($"LifeSupportScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Trigger ||
                updateSource == UpdateType.Terminal)
                HandleTriggerInput(argument);
            else
                CycleCoroutine(ref _enumerator, () => UpdateVents());
        }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // ScriptInput

        // ScriptOuput

        // CycleCoroutine

        // Validate

        // TryClampF

        // Run by trigger

        private bool _depressurize = false;

        private void HandleTriggerInput(string argument)
        {
            ProcessArgument(argument);

            // In case of state change, dispose the current routine.
            string valueString; bool depressurize;
            if (InputParameters.TryGetValue(KEY_DEPRESSURIZE, out valueString) &&
                bool.TryParse(valueString, out depressurize) &&
                depressurize != _depressurize)
            {
                _enumerator.Dispose();
                _enumerator = null;
            }
        }

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromSeconds(20);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromSeconds(40);

        private readonly string IGNORE_MARKER = $"[{SCRIPT_ID}Ignore]";
        private const uint BATCH_SIZE = 16;
        private ushort _evaluated = 0;

        private const string KEY_DEPRESSURIZE = "Depressurize";
        private const string KEY_LOWERTHRESHOLD = "LowerThreshold";
        private const string KEY_UPPERTHRESHOLD = "UpperThreshold";
        private const float OXYGEN_LOWER_DEFAULT = 0.75f;
        private const float OXYGEN_UPPER_DEFAULT = 0.98f;
        private readonly Color Color0 = new Color(0.25f, 0.5f, 1f);
        private readonly Color Color1 = new Color(0f, 1f, 0.5f);

        private readonly List<IMyAirVent> _vents = new List<IMyAirVent>();

        // Routine

        private IEnumerator<object> UpdateVents()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            DateTime startTime = DateTime.UtcNow;

            string valueString;
            bool depressurize;
            float lowerThreshold, upperThreshold;
            bool requiresUpdate = false;
            yield return null;

            // Config in a frame
            ReadConfig();

            if (!(InputParameters.TryGetValue(KEY_DEPRESSURIZE, out valueString) &&
                bool.TryParse(valueString, out depressurize)))
            {
                depressurize = false;
                _depressurize = depressurize;
                requiresUpdate = true;
            }

            if (!(InputParameters.TryGetValue(KEY_LOWERTHRESHOLD, out valueString) &&
                float.TryParse(valueString, out lowerThreshold)))
            {
                lowerThreshold = OXYGEN_LOWER_DEFAULT;
                requiresUpdate = true;
            }
            else if (TryClampF(ref lowerThreshold, 0f, 1f))
                requiresUpdate = true;

            if (!(InputParameters.TryGetValue(KEY_UPPERTHRESHOLD, out valueString) &&
                float.TryParse(valueString, out upperThreshold)))
            {
                upperThreshold = OXYGEN_UPPER_DEFAULT;
                requiresUpdate = true;
            }
            else if (TryClampF(ref upperThreshold, lowerThreshold, 1f))
                requiresUpdate = true;

            if (requiresUpdate)
            {
                InputParameters[KEY_DEPRESSURIZE] = depressurize.ToString();
                InputParameters[KEY_LOWERTHRESHOLD] = lowerThreshold.ToString();
                InputParameters[KEY_UPPERTHRESHOLD] = upperThreshold.ToString();
                UpdateConfig();
            }
            yield return null;

            // Get Vents
            _vents.Clear();
            GridTerminalSystem.GetBlocksOfType(_vents);
            yield return null;

            // Enumerate to validate
            int count = 0,
                enabledCount = 0,
                depressurizingCount = 0;
            foreach (var vent in _vents)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCH_SIZE == 0)
                    yield return null;

                if (!ValidateBlockOnSameConstruct(vent, IGNORE_MARKER) ||
                    vent.CustomData.Contains("[AirlockComponent:"))
                    continue;

                count++;
                vent.Enabled = true; // Enable to scan info (fans report zero if not)
                vent.Depressurize = depressurize;
                var level = vent.GetOxygenLevel();

                if (depressurize)
                    vent.Enabled = level > 0f;
                else
                {
                    if (level >= upperThreshold)
                        vent.Enabled = false;
                    else if (level <= lowerThreshold)
                        vent.Enabled = true;
                }

                if (vent.Enabled)
                    enabledCount++;
                if (vent.Depressurize)
                    depressurizingCount++;
            }
            yield return null;

            // Calculate
            OutputStats["IsDepressurizing"] = depressurize.ToString();
            OutputStats["VentsTotal"] = count.ToString();
            OutputStats["VentsEnabled"] = enabledCount.ToString();
            OutputStats["VentsDepressurizing"] = depressurizingCount.ToString();
            OutputStats["UpdateIndex"] = _evaluated.ToString();
            OutputFontColor = depressurize ? Color0 : Color1;
            yield return null;

            // Output
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