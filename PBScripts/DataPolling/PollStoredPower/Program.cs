using PBScripts._HelperMethods;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;

namespace PBScripts.PollStoredPower
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        {
            RunCoroutine(ref _pollingTask, () => PollGridPower());
        }

        private IEnumerator<bool> _pollingTask = null;

        // Specifics

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
        private DateTime _startTime = DateTime.UtcNow;

        private float _maxPower = 0f;
        private float _storedPower = 0f;
        private float _powerPercent = 0f;
        private StringBuilder _builder = null;

        private readonly List<IMyBatteryBlock> _batteries = new List<IMyBatteryBlock>();
        private int _count = 0;

        private const int _batchSize = 10;
        private int _batchIndex = 0;

        // Coroutine

        private IEnumerator<bool> PollGridPower()
        {
            // Prepare
            _startTime = DateTime.UtcNow;
            _storedPower = 0f;
            _maxPower = 0f;
            _powerPercent = 0f;
            _count = 0;
            _batchIndex = 0;

            // Enumerate batteries
            _batteries.Clear();
            GridTerminalSystem.GetBlocksOfType(_batteries);
            yield return true;

            foreach (IMyBatteryBlock battery in _batteries)
            {
                // Validate
                if (!battery.CubeGrid.Equals(Me.CubeGrid))
                    continue;
                if (!battery.IsFunctional)
                    continue;
                if (battery.ChargeMode == ChargeMode.Recharge)
                    continue;

                // Accumulate
                _count++;
                _storedPower += battery.CurrentStoredPower;
                _maxPower += battery.MaxStoredPower;
                _powerPercent = _storedPower / _maxPower;

                // Yield by batch
                if (++_batchIndex % _batchSize == 0)
                    yield return true;
            }
            yield return true;

            // Output to Custom Data
            _builder = new StringBuilder();
            _builder.AppendLine("[DataPolling]");
            _builder.AppendLine($"[GridBatteryCount:{_count}]");
            _builder.AppendLine($"[GridPowerStored:{_storedPower}]");
            _builder.AppendLine($"[GridPowerMaximum:{_maxPower}]");
            _builder.AppendLine($"[GridPowerFactor:{_powerPercent}]");
            Me.CustomData = _builder.ToString();
            yield return true;

            // Wait for interval to finish
            while (DateTime.UtcNow - _startTime < _interval)
                yield return true;
        }
    }
}