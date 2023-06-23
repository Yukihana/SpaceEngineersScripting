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
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            FIXEDMINIMUMINTERVAL = TimeSpan.FromMinutes(1);
        }

        public void Main()
        {
            RunCoroutine(ref _pollingTask, () => CycleOxygenFarms());
        }

        private IEnumerator<bool> _pollingTask = null;
        private const int BATCHSIZE = 16;
        private readonly TimeSpan FIXEDMINIMUMINTERVAL;
        private readonly Random _random = new Random();
        private const int RANDOMINTERVALMAX = 60;
        private const float POWERFACTORTHRESHOLD = 0.75f;
        private const float OXYGENFACTORTHRESHOLD = 0.98f;
        private const float PRODUCTIONMINIMUM = 0.5f;

        // Coroutine

        private IEnumerator<bool> CycleOxygenFarms()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            int evaluated = 0;
            int count = 0;
            int activeCount = 0;

            // Poll statistics
            var statsEnumerator = PollStatistics();
            while (statsEnumerator.MoveNext())
                yield return true;

            // Retrieve polled data: Grid power factor (Defaults to max)
            float gridPowerFactor = 1f;
            var gpfEnumerator = GetFloatStat("GridPowerFactor");
            while (gpfEnumerator.MoveNext() && gpfEnumerator.Current != float.NaN)
            {
                gridPowerFactor = gpfEnumerator.Current;
                yield return true;
            }

            // Retrieve polled data: Grid oxygen factor (Defaults to min)
            float gridOxygenFactor = 0f;
            var gofEnumerator = GetFloatStat("GridOxygenFactor");
            while (gofEnumerator.MoveNext() && gofEnumerator.Current != float.NaN)
            {
                gridOxygenFactor = gofEnumerator.Current;
                yield return true;
            }

            // Enumerate oxygen farms
            var _farms = new List<IMyOxygenFarm>();
            GridTerminalSystem.GetBlocksOfType(_farms);
            yield return true;

            // Evaluate critical mode
            bool critical = gridPowerFactor < POWERFACTORTHRESHOLD || gridOxygenFactor > OXYGENFACTORTHRESHOLD;

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
                if (evaluated % BATCHSIZE == 0)
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

                    // Control
                    if (farm.GetOutput() < PRODUCTIONMINIMUM)
                        farm.Enabled = false;
                    else
                        activeCount++;

                    // Yield by batch
                    if (evaluated % BATCHSIZE == 0)
                        yield return true;
                }
                yield return true;
            }

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Cooperative:AutoOxyFarm]");
            sb.AppendLine();
            sb.AppendLine($"[OxygenFarmsTotal:{count}]");
            sb.AppendLine($"[OxygenFarmsActive:{activeCount}]");
            sb.AppendLine($"[OxygenFarmsOutputCurrent:]");
            sb.AppendLine($"[OxygenFarmsOutputMaximum:]");
            sb.AppendLine($"[OxygenFarmsOutputFactor:]");
            string output = sb.ToString();
            yield return true;

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);
            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = new VRageMath.Color(1f, 1f, 0.5f);
            monitor.WriteText(output);
            Me.CustomData = output;
            yield return true;

            // On early finish, wait for interval
            while (DateTime.UtcNow - startTime < FIXEDMINIMUMINTERVAL)
                yield return true;

            // Followed that by an additional random interval
            startTime = DateTime.UtcNow;
            TimeSpan randomInterval = TimeSpan.FromSeconds(_random.Next(0, RANDOMINTERVALMAX));
            while (DateTime.UtcNow - startTime < randomInterval)
                yield return true;
        }
    }
}