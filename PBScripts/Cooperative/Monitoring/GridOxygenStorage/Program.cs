using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Cooperative.Monitoring.GridOxygenStorage
{
    public partial class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "GridOxygenStorage";

        public Program()
        {
            OutputTitle = $"Monitoring-{SCRIPT_ID}";
            TagSelf("MonitorScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => MonitorOxygenStorage()); }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // CycleCoroutine

        // ValidateBlockOnSameConstruct

        // SurfaceOutput

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
        private ulong _evaluated = 0;

        // Routine

        private IEnumerator<object> MonitorOxygenStorage()
        {
            DateTime startTime = DateTime.UtcNow;
            int count = 0, stockpiling = 0;
            double stored = 0f, capacity = 0f;
            yield return null;

            // Get tanks
            _raw.Clear();
            GridTerminalSystem.GetBlocksOfType(_raw);
            yield return null;

            // Enumerate to validate
            _tanks.Clear();
            foreach (var tank in _raw)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCH_SIZE == 0)
                    yield return null;

                if (!ValidateBlockOnSameConstruct(tank, IGNORE_MARKER) ||
                    !(string.IsNullOrWhiteSpace(tank.BlockDefinition.SubtypeId) ||
                    tank.BlockDefinition.SubtypeId.Contains("Oxygen")))
                    continue;

                if (tank.Stockpile)
                    stockpiling++;
                else
                    _tanks.Add(tank);
            }
            yield return null;

            // Calculate
            foreach (var tank in _tanks)
            {
                count++;
                capacity += tank.Capacity;
                stored += tank.Capacity * tank.FilledRatio;
            }
            float filledFactor = capacity == 0 ? 0f
                : (float)(stored / capacity);
            OutputStats["FilledFactor"] = filledFactor.ToString();
            OutputStats["OxygenStored"] = stored.ToString();
            OutputStats["OxygenCapacity"] = capacity.ToString();
            OutputStats["TanksAvailable"] = count.ToString();
            OutputStats["TanksStockpiling"] = stockpiling.ToString();
            OutputStats["UpdateGuid"] = _evaluated.ToString();

            OutputFontColor = Color.Lerp(_color0, _color1, filledFactor);
            yield return null;

            // Output
            DoManualOutput();
            yield return null;

            // On early finish, wait for interval
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
                yield return null;
        }
    }
}