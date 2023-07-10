using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;

namespace PBScripts.PollStoredPower
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            FIXEDINTERVALMINIMUM = TimeSpan.FromMinutes(1);
        }

        public void Main()
        {
            CycleCoroutine(ref _pollingTask, () => PollGridPower());
        }

        private IEnumerator<bool> _pollingTask = null;
        private readonly TimeSpan FIXEDINTERVALMINIMUM;
        private const int BATCHSIZE = 20;

        // Coroutine

        private IEnumerator<bool> PollGridPower()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            int evaluated = 0;
            int count = 0;

            float storedPower = 0f;
            float maxPower = 0f;
            float powerPercent = 0f;

            // Enumerate batteries
            var _batteries = new List<IMyBatteryBlock>();
            GridTerminalSystem.GetBlocksOfType(_batteries);
            yield return true;

            foreach (IMyBatteryBlock battery in _batteries)
            {
                evaluated++;

                // Validate
                if (battery.CubeGrid != Me.CubeGrid)
                    continue;
                if (!battery.IsFunctional)
                    continue;
                if (battery.ChargeMode == ChargeMode.Recharge)
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

            // Wait for interval to finish
            while (DateTime.UtcNow - startTime < FIXEDINTERVALMINIMUM)
                yield return true;
        }
    }
}