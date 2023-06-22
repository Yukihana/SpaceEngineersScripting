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
            RunCoroutine(ref _pollingTask, () => PollGridPower());
        }

        private IEnumerator<bool> _pollingTask = null;
        private const int BATCHSIZE = 20;
        private readonly TimeSpan FIXEDMINIMUMINTERVAL;
        private readonly Random _random = new Random();
        private const int RANDOMINTERVALMAX = 60;
        private const float POWERFACTORTHRESHOLD = 0.75f;

        // Coroutine

        private IEnumerator<bool> PollGridPower()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            int evaluated = 0;
            int count = 0;

            // Retrieve polled data: Grid power factor (Defaults to max)
            float gridPowerFactor = 1f;
            var pollEnumerator = GetPolledDataAsFloat("GridPowerFactor");
            while (pollEnumerator.MoveNext())
            {
                gridPowerFactor = pollEnumerator.Current;
                yield return true;
            }

            // Enumerate oxygen farms
            var _farms = new List<IMyOxygenFarm>();
            GridTerminalSystem.GetBlocksOfType(_farms);
            yield return true;

            foreach (IMyOxygenFarm farm in _farms)
            {
                evaluated++;

                // Validate
                if (!farm.IsFunctional)
                    continue;

                // Accumulate
                count++;
                storedPower += battery.CurrentStoredPower;
                maxPower += battery.MaxStoredPower;
                powerPercent = storedPower / maxPower;

                // Yield by batch
                if (evaluated % BATCHSIZE == 0)
                    yield return true;
            }
            yield return true;

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[DataPolling:StoredPower]");
            sb.AppendLine($"[GridBatteryCount:{count}]");
            sb.AppendLine($"[GridPowerStored:{storedPower}]");
            sb.AppendLine($"[GridPowerMaximum:{maxPower}]");
            sb.AppendLine($"[GridPowerFactor:{powerPercent}]");
            string output = sb.ToString();
            yield return true;

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);
            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = new VRageMath.Color(1f, 1f, 0.5f);
            monitor.WriteText(output);
            Me.CustomData = output;
            yield return true;

            // Wait for interval to finish followed by an additional random
            while (DateTime.UtcNow - startTime < FIXEDMINIMUMINTERVAL)
                yield return true;
            startTime = DateTime.UtcNow;
            TimeSpan randomInterval = TimeSpan.FromSeconds(_random.Next(0, RANDOMINTERVALMAX));
            while (DateTime.UtcNow - startTime < randomInterval)
                yield return true;
        }
    }
}