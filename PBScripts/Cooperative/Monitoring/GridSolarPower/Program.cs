using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game;
using VRageMath;

namespace PBScripts.Cooperative.Monitoring.GridSolarPower
{
    public partial class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "GridSolarPowerIntake";

        public Program()
        {
            OutputTitle = $"Monitoring-{SCRIPT_ID}";
            TagSelf("MonitorScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => GridSolarPower()); }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // CycleCoroutine

        // Validate

        // ScriptOutput

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(1);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(2);

        private readonly string IGNORE_MARKER = $"[{SCRIPT_ID}Ignore]";
        private const int BATCHSIZE = 16;
        private Color Color0 = new Color(1f, 0.5f, 0f);
        private Color Color1 = new Color(0f, 1f, 0.5f);

        private readonly List<float> _powerFactors = new List<float>();
        private ulong _evaluated = 0;

        // Routine

        private IEnumerator<object> GridSolarPower()
        {
            DateTime startTime = DateTime.UtcNow;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;

            int count = 0;
            int activeCount = 0;

            float currentSolarPower = 0f;
            float availableSolarPower = 0f;
            float maximumSolarPower = 0f;

            _powerFactors.Clear();
            yield return null;

            // Get solar panels
            var panels = new List<IMySolarPanel>();
            GridTerminalSystem.GetBlocksOfType(panels);
            yield return null;

            // Enumerate to analyze
            foreach (var panel in panels)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCHSIZE == 0)
                    yield return null;

                if (!panel.Enabled ||
                    !panel.IsFunctional ||
                    panel.CustomData.Contains(IGNORE_MARKER))
                    continue;

                // Accumulate
                count++;
                if (panel.CurrentOutput > 0)
                    activeCount++;
                float panelMax = panel.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 0.16f : 0.04f;
                currentSolarPower += panel.CurrentOutput;
                availableSolarPower += panel.MaxOutput;
                maximumSolarPower += panelMax;
                _powerFactors.Add(panel.MaxOutput / panelMax);
            }
            yield return null;

            // Calculate
            float efficiency = count > 0 ? availableSolarPower / maximumSolarPower : 0;
            float actualEfficiency = count > 0 ? currentSolarPower / maximumSolarPower : 0;
            float mean = count > 0 ? _powerFactors.Average() : 0;
            float cumsqdiff = 0;
            foreach (var factor in _powerFactors)
                cumsqdiff += (factor - mean) * (factor - mean);
            float variance = count > 0 ? cumsqdiff / count : 0;
            double deviance = count > 0 ? Math.Sqrt(variance) : 0;

            OutputStats["PanelTotal"] = count.ToString();
            OutputStats["PanelActive"] = activeCount.ToString();
            OutputStats["InputCurrent"] = currentSolarPower.ToString();
            OutputStats["InputAvailable"] = availableSolarPower.ToString();
            OutputStats["InputMaximum"] = maximumSolarPower.ToString();
            OutputStats["Efficiency"] = efficiency.ToString();
            OutputStats["EfficiencyActual"] = actualEfficiency.ToString();
            OutputStats["EfficiencyVariance"] = variance.ToString();
            OutputStats["EfficiencyDeviance"] = deviance.ToString();
            OutputStats["UpdateIndex"] = _evaluated.ToString();
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