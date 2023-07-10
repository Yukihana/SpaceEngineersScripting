using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;

namespace PBScripts.Cooperative.LifeSupport.AutoOxyFarm
{
    internal class Program : SEProgramBase
    {
        public Program()
        { Runtime.UpdateFrequency = UpdateFrequency.Update100; }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => CycleOxygenFarms()); }

        private IEnumerator<bool> _enumerator = null;
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(1);
        private const int BATCH_SIZE = 16;
        private readonly Random _random = new Random();
        private const int INTERVAL_RANDOM_MAX = 60;

        // Polling

        // Coroutine

        private const float POWER_MINIMUM = 0.75f;
        private const float OXYGEN_MAXIMUM = 0.98f;
        private const float EFFICIENCY_MINIMUM = 0.5f;
        private const float OXYFARM_MAX = 180f;

        private IEnumerator<bool> CycleOxygenFarms()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            uint evaluated = 0, count = 0, activeCount = 0;
            float farmCurrent = 0f, farmMaximum = 0f, farmEfficiency = 0f;

            // Poll statistics
            var statsEnumerator = PollStatistics();
            while (statsEnumerator.MoveNext())
                yield return true;

            // Retrieve polled data: Grid power factor (Defaults to max)
            float gridPowerFactor = 1f;
            var gpfEnumerator = GetFloatStat("GridPowerFactor");
            while (gpfEnumerator.MoveNext())
            {
                if (!float.IsNaN(gpfEnumerator.Current))
                    gridPowerFactor = gpfEnumerator.Current;
                yield return true;
            }

            // Retrieve polled data: Grid oxygen factor (Defaults to min)
            float gridOxygenFactor = 0f;
            var gofEnumerator = GetFloatStat("GridOxygenFactor");
            while (gofEnumerator.MoveNext())
            {
                if (!float.IsNaN(gofEnumerator.Current))
                    gridOxygenFactor = gofEnumerator.Current;
                yield return true;
            }

            // Enumerate oxygen farms
            var _farms = new List<IMyOxygenFarm>();
            GridTerminalSystem.GetBlocksOfType(_farms);
            yield return true;

            // Evaluate critical mode
            bool critical = gridPowerFactor < POWER_MINIMUM || gridOxygenFactor > OXYGEN_MAXIMUM;

            // Switch all on (or off if critical)
            foreach (IMyOxygenFarm farm in _farms)
            {
                evaluated++;

                // Validate
                if (!farm.IsSameConstructAs(Me))
                    continue;
                if (!farm.IsFunctional)
                    continue;

                // Control
                farm.Enabled = !critical;
                count++;

                // Yield by batch
                if (evaluated % BATCH_SIZE == 0)
                    yield return true;
            }
            yield return true;
            evaluated = 0;

            // Switch inefficient ones off (skip on critical)
            if (!critical)
            {
                foreach (IMyOxygenFarm farm in _farms)
                {
                    evaluated++;

                    // Validate
                    if (!farm.IsSameConstructAs(Me))
                        continue;
                    if (!farm.IsFunctional)
                        continue;
                    if (!farm.Enabled)
                        continue;

                    // Control
                    var efficiency = farm.GetOutput();
                    if (efficiency < EFFICIENCY_MINIMUM || critical)
                        farm.Enabled = false;
                    else
                    {
                        activeCount++;
                        farmMaximum += OXYFARM_MAX;
                        farmCurrent += OXYFARM_MAX * efficiency;
                    }

                    // Yield by batch
                    if (evaluated % BATCH_SIZE == 0)
                        yield return true;
                }
                yield return true;
            }

            // Calculate
            farmEfficiency = farmCurrent / farmMaximum;
            long pollsize = 0;
            foreach (var item in statistics)
                pollsize += item.Length;

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Cooperative:AutoOxyFarm]");
            sb.AppendLine();
            sb.AppendLine($"[OxygenFarmsTotal:{count}]");
            sb.AppendLine($"[OxygenFarmsActive:{activeCount}]");
            sb.AppendLine();
            sb.AppendLine($"[OxygenFarmsOutputCurrent:{farmCurrent}]");
            sb.AppendLine($"[OxygenFarmsOutputMaximum:{farmMaximum}]");
            sb.AppendLine($"[OxygenFarmsOutputFactor:{farmEfficiency}]");
            sb.AppendLine();
            sb.AppendLine($"[OxygenFarmsPollCount:{statistics.Count}]");
            sb.AppendLine($"[OxygenFarmsPollSize:{pollsize}]");
            sb.AppendLine($"[OxygenFarmsGPF:{gridPowerFactor}]");
            sb.AppendLine($"[OxygenFarmsGOF:{gridOxygenFactor}]");
            string output = sb.ToString();
            yield return true;

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);
            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = new VRageMath.Color(0f, 1f, 0.5f);
            monitor.WriteText(output);
            Me.CustomData = output;
            yield return true;

            // On early finish, wait for interval
            while (DateTime.UtcNow - startTime < INTERVAL_MINIMUM)
                yield return true;

            // Followed that by an additional random interval
            startTime = DateTime.UtcNow;
            TimeSpan randomInterval = TimeSpan.FromSeconds(_random.Next(0, INTERVAL_RANDOM_MAX));
            while (DateTime.UtcNow - startTime < randomInterval)
                yield return true;
        }
    }
}