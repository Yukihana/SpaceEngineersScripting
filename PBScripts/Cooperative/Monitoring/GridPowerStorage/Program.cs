using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Cooperative.Monitoring.GridPowerStorage
{
    public partial class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "GridPowerStorage";

        public Program()
        {
            OutputTitle = $"Monitoring-{SCRIPT_ID}";
            TagSelf("MonitorScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => MonitorPowerStorage()); }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // CycleCoroutine

        // ValidateBlockOnSameConstruct

        // SurfaceOutput

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromSeconds(30);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromSeconds(60);

        private readonly string IGNORE_MARKER = $"{SCRIPT_ID}Ignore";
        private const int BATCHSIZE = 32;
        private readonly Color Color0 = new Color(1f, 0.5f, 0.25f);
        private readonly Color Color1 = new Color(0.25f, 0.5f, 1f);

        private ulong _evaluated = 0;
        private readonly List<IMyBatteryBlock> _raw = new List<IMyBatteryBlock>();
        private readonly List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();

        // Routine

        private IEnumerator<object> MonitorPowerStorage()
        {
            DateTime startTime = DateTime.UtcNow;
            int count = 0, charging = 0;
            float stored = 0f, capacity = 0f;
            yield return null;

            // Get batteries
            _raw.Clear();
            GridTerminalSystem.GetBlocksOfType(_raw);
            yield return null;

            // Enumerate to validate
            _batteries.Clear();
            foreach (IMyBatteryBlock battery in _raw)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCHSIZE == 0)
                    yield return null;

                if (!ValidateBlockOnSameConstruct(battery) ||
                    battery.CustomData.Contains(IGNORE_MARKER))
                    continue;

                if (battery.ChargeMode == ChargeMode.Recharge)
                    charging++;
                else
                    _batteries.Add(battery);
            }
            yield return null;

            // Calculate
            foreach (var battery in _batteries)
            {
                count++;
                stored += battery.CurrentStoredPower;
                capacity += battery.MaxStoredPower;
            }
            float filledFactor = count > 0 ? stored / capacity : 0;

            OutputStats["FilledFactor"] = filledFactor.ToString();
            OutputStats["PowerStored"] = $"{stored} MWh";
            OutputStats["PowerCapacity"] = $"{capacity} MWh";
            OutputStats["BatteriesAvailable"] = count.ToString();
            OutputStats["BatteriesCharging"] = charging.ToString();
            yield return null;

            // Output
            OutputFontColor = Color.Lerp(Color0, Color1, filledFactor);
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