using PBScripts._Helpers;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ObjectBuilders.Definitions;

namespace PBScripts.DataPolling.PollOxygenStorage
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            INTERVAL_FIXED_MINIMUM = TimeSpan.FromMinutes(1);
        }

        public void Main()
        {
            RunCoroutine(ref _enumerator, () => PollGridOxygen());
        }

        private IEnumerator<bool> _enumerator = null;
        private const int BATCH_SIZE = 20;
        private readonly TimeSpan INTERVAL_FIXED_MINIMUM;
        private readonly Random _random = new Random();
        private const int INTERVAL_APPENDED_MAXIMUM = 60;

        // Coroutine

        private const string IGNORE_MARKER = "PollIgnore";

        private IEnumerator<bool> PollGridOxygen()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            int evaluated = 0;
            int count = 0;

            double storedOxygen = 0f;
            double maxOxygen = 0f;
            float oxygenPercent = 0f;

            // Enumerate oxygen tanks
            var tanks = new List<IMyGasTank>();
            GridTerminalSystem.GetBlocksOfType(tanks);
            yield return true;

            foreach (var tank in tanks)
            {
                evaluated++;

                // Validate
                if (!tank.IsSameConstructAs(Me))
                    continue;
                if (!tank.IsFunctional)
                    continue;
                if (!tank.Enabled)
                    continue;
                if (tank.Stockpile)
                    continue;
                if (tank.CustomData.Contains($"[{IGNORE_MARKER}]"))
                    continue;
                if (tank.BlockDefinition.SubtypeName.Contains("Hydrogen")) // To rule out hydrogen tanks. OxygenTank subtype id is blank.
                    continue;

                // Accumulate
                count++;
                maxOxygen += tank.Capacity;
                storedOxygen += tank.Capacity * tank.FilledRatio;

                // Yield by batch
                if (evaluated % BATCH_SIZE == 0)
                    yield return true;
            }

            // Calculate
            oxygenPercent = (float)(storedOxygen / maxOxygen);

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[PolledStatistics:GridOxygen]");
            sb.AppendLine();
            sb.AppendLine($"[GridOxygenStored:{storedOxygen}]");
            sb.AppendLine($"[GridOxygenMaximum:{maxOxygen}]");
            sb.AppendLine($"[GridOxygenFactor:{oxygenPercent}]");
            sb.AppendLine($"[GridOxygenTankCount:{count}]");
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
            while (DateTime.UtcNow - startTime < INTERVAL_FIXED_MINIMUM)
                yield return true;

            // Followed that by an additional random interval
            startTime = DateTime.UtcNow;
            TimeSpan randomInterval = TimeSpan.FromSeconds(_random.Next(0, INTERVAL_APPENDED_MAXIMUM));
            while (DateTime.UtcNow - startTime < randomInterval)
                yield return true;
        }
    }
}