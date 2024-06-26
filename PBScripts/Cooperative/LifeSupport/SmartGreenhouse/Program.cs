﻿using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Cooperative.LifeSupport.SmartGreenhouse
{
    public partial class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "SmartGreenhouse";

        public Program()
        {
            OutputTitle = $"LifeSupport-{SCRIPT_ID}";
            TagSelf("LifeSupportScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => CycleOxygenFarms()); }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // CycleCoroutine

        // TryGetOutput

        // Validate

        // SurfaceOutput

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(10);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(20);

        private readonly string IGNORE_MARKER = $"[{SCRIPT_ID}Ignore]";
        private const int BATCH_SIZE = 32;

        private const float GRID_POWER_MINIMUM = 0.5f;
        private const float GRID_OXYGEN_MAXIMUM = 0.98f;
        private const float EFFICIENCY_MINIMUM = 0.5f;
        private const float OXYFARM_MAX = 180f;
        private readonly Color Color0 = new Color(1f, 0.5f, 0f);
        private readonly Color Color1 = new Color(0f, 1f, 0.5f);

        private readonly List<IMyOxygenFarm> _farms = new List<IMyOxygenFarm>();
        private readonly List<IMyOxygenFarm> _carryOver = new List<IMyOxygenFarm>();
        private readonly List<float> _factors = new List<float>();
        private IMyProgrammableBlock _script;
        private ulong _evaluated = 0;

        // Routine

        private IEnumerator<object> CycleOxygenFarms()
        {
            DateTime startTime = DateTime.UtcNow;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            uint count = 0, activeCount = 0;
            float farmCurrent = 0f, farmMaximum = 0f;
            string valueString;
            yield return null;

            // Get current power storage factor (Defaults to max)
            float gridPowerFactor;
            if (!TryGetScript("GridPowerStorage", "MonitorScript", out _script) ||
                !TryGetOutput(_script, "FilledFactor", out valueString) ||
                !float.TryParse(valueString, out gridPowerFactor))
                gridPowerFactor = 1f; // Set default: Max
            yield return null;

            // Get current oxygen storage factor (Defaults to min)
            float gridOxygenFactor;
            if (!TryGetScript("GridOxygenStorage", "MonitorScript", out _script) ||
                !TryGetOutput(_script, "FilledFactor", out valueString) ||
                !float.TryParse(valueString, out gridOxygenFactor))
                gridOxygenFactor = 0f;
            yield return null;

            // Check if farming should be allowed
            bool canFarm = gridPowerFactor > GRID_POWER_MINIMUM
                && gridOxygenFactor < GRID_OXYGEN_MAXIMUM;

            // Get farms
            _farms.Clear();
            GridTerminalSystem.GetBlocksOfType(_farms);
            yield return null;

            // Phase 1: Turn them on
            _carryOver.Clear();
            foreach (IMyOxygenFarm farm in _farms)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCH_SIZE == 0)
                    yield return null;

                if (!ValidateBlockOnSameConstruct(farm, IGNORE_MARKER))
                    continue;

                farm.Enabled = canFarm;
                _carryOver.Add(farm);
            }

            // Wait 5 seconds for the farms to get upto speed
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            DateTime midPause = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            while (DateTime.UtcNow < midPause)
                yield return null;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            yield return null;

            // Phase 2: Evaluate by output
            _factors.Clear();
            if (canFarm)
            {
                foreach (IMyOxygenFarm farm in _carryOver)
                {
                    unchecked { _evaluated++; }
                    if (_evaluated % BATCH_SIZE == 0)
                        yield return null;

                    if (!ValidateBlockOnSameConstruct(farm, IGNORE_MARKER))
                        continue;

                    count++;
                    var factor = farm.GetOutput();
                    if (factor < EFFICIENCY_MINIMUM)
                    {
                        farm.Enabled = false;
                        continue;
                    }

                    // Accumulate if farm is left enabled
                    activeCount++;
                    _factors.Add(factor);
                    farmMaximum += OXYFARM_MAX;
                    farmCurrent += OXYFARM_MAX * factor;
                }
                yield return null;
            }

            // Calculate
            var efficiency = farmMaximum > 0 ? farmCurrent / farmMaximum : 0;

            OutputStats["FarmingAllowed"] = canFarm.ToString();
            OutputStats["Efficiency"] = efficiency.ToString("0.###");
            OutputStats["EnabledFactor"] = (count > 0 ? activeCount / (float)count : 0).ToString("0.###");

            OutputStats["OutputCurrent"] = farmCurrent.ToString("0.###") + " L/min";
            OutputStats["OutputMaximum"] = farmMaximum.ToString("0.###") + " L/min";
            OutputStats["FarmsTotal"] = count.ToString();
            OutputStats["FarmsActive"] = activeCount.ToString();

            OutputStats["ActivationThreshold"] = EFFICIENCY_MINIMUM.ToString();
            OutputStats["InputPowerFactor"] = (gridPowerFactor >= 0 ? gridPowerFactor : 0).ToString("0.###");
            OutputStats["InputOxygenFactor"] = gridOxygenFactor.ToString("0.###");
            OutputStats["UpdateGuid"] = _evaluated.ToString();

            OutputFontColor = Color.Lerp(Color0, Color1, efficiency);
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