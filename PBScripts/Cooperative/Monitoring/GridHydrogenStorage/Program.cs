using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace PBScripts.Cooperative.Monitoring.GridHydrogenStorage
{
    internal class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "GridHydrogenStorage";

        public Program()
        {
            OutputTitle = $"Monitoring-{SCRIPT_ID}";
            TagSelf("MonitorScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => MonitorOxygenStorage()); }

        private IEnumerator<object> _enumerator = null;

        // CycleCoroutine

        // ValidateBlockOnSameConstruct

        // SurfaceOutput

        // TagSelf

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(1);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(2);

        private readonly string IGNORE_MARKER = $"{SCRIPT_ID}Ignore";
        private const byte BATCH_SIZE = 32;
        private readonly Color _color0 = new Color(1f, 0.5f, 0.5f);
        private readonly Color _color1 = new Color(0f, 1f, 0.75f);

        private readonly List<IMyGasTank> _raw = new List<IMyGasTank>();
        private readonly List<IMyGasTank> _tanks = new List<IMyGasTank>();

        // Routine

        private IEnumerator<object> MonitorOxygenStorage()
        {
            DateTime startTime = DateTime.UtcNow;
            ushort evaluated = 0;
            int count = 0, stockpiling = 0;
            double stored = 0f, capacity = 0f;
            yield return true;

            // Get tanks
            _raw.Clear();
            GridTerminalSystem.GetBlocksOfType(_raw);
            yield return true;

            // Enumerate to validate
            _tanks.Clear();
            foreach (var tank in _raw)
            {
                unchecked { evaluated++; }
                if (evaluated % BATCH_SIZE == 0)
                    yield return true;

                if (!ValidateBlockOnSameConstruct(tank, IGNORE_MARKER) ||
                    !tank.BlockDefinition.SubtypeName.Contains("Hydrogen"))
                    continue;

                if (tank.Stockpile)
                    stockpiling++;
                else
                    _tanks.Add(tank);
            }
            yield return true;

            // Calculate
            foreach (var tank in _raw)
            {
                count++;
                capacity += tank.Capacity;
                stored += tank.Capacity * tank.FilledRatio;
            }
            float filledFactor = capacity == 0 ? 0f
                : (float)(stored / capacity);
            OutputStats["FilledFactor"] = filledFactor.ToString();
            OutputStats["HydrogenStored"] = stored.ToString();
            OutputStats["HydrogenCapacity"] = capacity.ToString();
            OutputStats["TanksAvailable"] = count.ToString();
            OutputStats["TanksStockpiling"] = stockpiling.ToString();

            OutputFontColor = Color.Lerp(_color0, _color1, filledFactor);
            yield return true;

            // Fart it out
            DoManualOutput();
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