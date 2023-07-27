using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace PBScripts.Cooperative.Monitoring.GridOxygenStorage
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            OutputTitle = "GridOxygenStorageMonitor";
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => MonitorOxygenStorage()); }

        private IEnumerator<bool> _enumerator = null;

        // CycleCoroutine

        // ValidateBlockOnSameConstruct

        // SurfaceOutput

        // TagSelf

        // Custom Code : Monitor Grid Oxygen

        private readonly List<IMyGasTank> _raw = new List<IMyGasTank>();
        private readonly List<IMyGasTank> _tanks = new List<IMyGasTank>();
        private const string IGNORE_MARKER = "PollIgnore";
        private const byte BATCH_SIZE = 16;

        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(1);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(2);
        private readonly Random _random = new Random();

        private readonly Color _color0 = new Color(1f, 0.25f, 0.25f);
        private readonly Color _color1 = new Color(0f, 1f, 0.5f);

        private IEnumerator<bool> MonitorOxygenStorage()
        {
            DateTime startTime = DateTime.UtcNow;
            double storedOxygen = 0f;
            double maxOxygen = 0f;
            float filledFactor = 0f;
            ushort evaluated = 0;
            int count = 0, stockpile = 0;
            yield return true;

            // Enumerate oxygen tanks
            _raw.Clear();
            GridTerminalSystem.GetBlocksOfType(_raw);
            yield return true;

            // Filter
            _tanks.Clear();
            foreach (var tank in _raw)
            {
                unchecked { evaluated++; }
                if (evaluated % BATCH_SIZE == 0)
                    yield return true;

                if (!ValidateBlockOnSameConstruct(tank, IGNORE_MARKER) &&
                    tank.BlockDefinition.SubtypeName.Contains("Hydrogen"))
                    continue;

                if (tank.Stockpile)
                    stockpile++;
                else
                    _tanks.Add(tank);
            }
            yield return true;

            // Calculate
            foreach (var tank in _tanks.Where(x => !x.Stockpile))
            {
                count++;
                maxOxygen += tank.Capacity;
                storedOxygen += tank.Capacity * tank.FilledRatio;
            }
            filledFactor = maxOxygen == 0 ? 0f
                : (float)(storedOxygen / maxOxygen);
            OutputStats["StorageFilled"] = storedOxygen.ToString();
            OutputStats["StorageCapacity"] = maxOxygen.ToString();
            OutputStats["FilledFactor"] = filledFactor.ToString();
            OutputStats["TanksAvailable"] = count.ToString();
            OutputStats["TanksStockpiling"] = stockpile.ToString();
            OutputFontColor = Color.Lerp(_color0, _color1, filledFactor);
            yield return true;

            // Fart it out
            DoManualOutput();
            yield return true;
            TagSelf("MonitorScript:GridOxygenStorage");
            yield return true;

            // On early finish, wait for interval
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
                yield return true;
        }
    }
}