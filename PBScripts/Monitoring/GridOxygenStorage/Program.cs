using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace PBScripts.Monitoring.GridOxygenStorage
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            ModuleDisplayName = "GridOxygenStorageMonitor";
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => MonitorOxygenStorage()); }

        private IEnumerator<bool> _enumerator = null;

        // Coroutine

        // Monitor Grid Oxygen

        private readonly TimeSpan INTERVAL_FIXED_MINIMUM = TimeSpan.FromMinutes(1);
        private const int BATCH_SIZE = 20;
        private readonly Random _random = new Random();
        private const int INTERVAL_APPENDED_MAXIMUM = 60;
        private const string IGNORE_MARKER = "PollIgnore";
        private readonly Color _color0 = new Color(1f, 0f, 0f);
        private readonly Color _color1 = new Color(0f, 1f, 0.5f);

        private IEnumerator<bool> MonitorOxygenStorage()
        {
            DateTime startTime = DateTime.UtcNow;
            double storedOxygen = 0f;
            double maxOxygen = 0f;
            float oxygenPercent = 0f;
            int count = 0;
            var tanks = new List<IMyGasTank>();
            yield return true;

            // Enumerate oxygen tanks
            GridTerminalSystem.GetBlocksOfType(tanks,
                x => ValidateBlockOnSameConstruct(x, $"[{IGNORE_MARKER}]")
                && !x.BlockDefinition.SubtypeName.Contains("Hydrogen"));
            yield return true;

            // Calculate
            foreach (var tank in tanks.Where(x => !x.Stockpile))
            {
                count++;
                maxOxygen += tank.Capacity;
                storedOxygen += tank.Capacity * tank.FilledRatio;
            }
            oxygenPercent = maxOxygen == 0 ? 0f
                : (float)(storedOxygen / maxOxygen);
            _stats["StoredVolume"] = storedOxygen.ToString();
            _stats["Capacity"] = maxOxygen.ToString();
            _stats["FilledFactor"] = oxygenPercent.ToString();
            _stats["TanksCount"] = count.ToString();
            _stats["TanksStockpiling"] = tanks.Count(x => x.Stockpile).ToString();
            _outputFontColor = Color.Lerp(_color0, _color1, oxygenPercent);
            yield return true;

            DoOutput();
            yield return true;

            // On early finish, wait for interval
            DateTime waitTill = startTime + INTERVAL_FIXED_MINIMUM
                + TimeSpan.FromSeconds(_random.Next(0, INTERVAL_APPENDED_MAXIMUM));
            while (DateTime.UtcNow < waitTill)
                yield return true;
        }
    }
}