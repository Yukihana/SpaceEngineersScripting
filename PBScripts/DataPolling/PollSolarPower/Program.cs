using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.GUI.TextPanel;

namespace PBScripts.DataPolling.PollSolarPower
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _interval = TimeSpan.FromMinutes(1);
        }

        public void Main()
        {
            CycleCoroutine(ref _pollingTask, () => PollSolarPower());
        }

        private IEnumerator<bool> _pollingTask = null;
        private readonly TimeSpan _interval;
        private const int BATCHSIZE = 20;

        // Coroutine

        private IEnumerator<bool> PollSolarPower()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            int evaluated = 0;

            int count = 0;
            int activeCount = 0;

            var powerFactors = new List<float>();
            float currentSolarPower = 0f;
            float availableSolarPower = 0f;
            float maximumSolarPower = 0f;

            // Enumerate solar panels
            var panels = new List<IMySolarPanel>();
            GridTerminalSystem.GetBlocksOfType(panels);
            yield return true;

            foreach (var panel in panels)
            {
                evaluated++;

                // Validate
                if (!panel.Enabled)
                    continue;
                if (!panel.IsFunctional)
                    continue;

                // Accumulate
                count++;
                if (panel.CurrentOutput > 0)
                    activeCount++;
                float panelMax = panel.CubeGrid.GridSizeEnum == MyCubeSize.Large ? 0.16f : 0.04f;
                currentSolarPower += panel.CurrentOutput;
                availableSolarPower += panel.MaxOutput;
                maximumSolarPower += panelMax;
                powerFactors.Add(panel.MaxOutput / panelMax);

                // Yield by batch
                if (evaluated % BATCHSIZE == 0)
                    yield return true;
            }
            yield return true;

            // Calculate
            var efficiency = availableSolarPower / maximumSolarPower;
            var actualEfficiency = currentSolarPower / maximumSolarPower;
            float mean = powerFactors.Average();
            var sqdiff = powerFactors.Select(x => (x - mean) * (x - mean)).ToList();
            var cumsqdiff = sqdiff.Sum();
            var variance = cumsqdiff / count;
            var deviance = Math.Sqrt(variance);
            yield return true;

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[DataPolling:SolarInput]");
            sb.AppendLine();
            sb.AppendLine($"[TotalSolarPanelCount:{count}]");
            sb.AppendLine($"[ActiveSolarPanelCount:{activeCount}]");
            sb.AppendLine();
            sb.AppendLine($"[CurrentSolarInput:{currentSolarPower}]");
            sb.AppendLine($"[AvailableSolarInput:{availableSolarPower}]");
            sb.AppendLine($"[MaximumSolarInput:{maximumSolarPower}]");
            sb.AppendLine();
            sb.AppendLine($"[SolarInputEfficiency:{efficiency}]");
            sb.AppendLine($"[SolarInputFactor:{actualEfficiency}]");
            sb.AppendLine($"[SolarInputVariance:{variance}]");
            sb.AppendLine($"[SolarInputDeviance:{deviance}]");
            string output = sb.ToString();
            yield return true;

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);
            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = new VRageMath.Color(1f, 1f, 0.5f);
            monitor.WriteText(output);
            Me.CustomData = output;
            yield return true;

            // Wait for interval
            while (DateTime.UtcNow - startTime < _interval)
                yield return true;
        }
    }
}