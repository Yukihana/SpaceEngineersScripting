using PBScripts._HelperMethods;
using Sandbox.Game.GameSystems.Electricity;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;

namespace PBScripts.DataPolling.PollSolarPower
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        {
            RunCoroutine(ref _pollingTask, () => PollSolarPower());
        }

        private IEnumerator<bool> _pollingTask = null;

        // Specifics

        private class PowerInfo
        {
            public float Current { get; set; } = 0.0f;
            public float Maximum { get; set; } = 0.0f;
        }

        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);
        private DateTime _startTime = DateTime.UtcNow;

        private const int _batchSize = 10;
        private int _batchIndex = 0;

        private readonly List<IMySolarPanel> _panels = new List<IMySolarPanel>();
        private int _count = 0;

        private readonly List<PowerInfo> _powerValues = new List<PowerInfo>();
        private float _currentSolarPower = 0f;
        private float _maximumSolarPower = 0f;

        // Coroutine

        private IEnumerator<bool> PollSolarPower()
        {
            // Prepare
            _startTime = DateTime.UtcNow;
            _count = 0;
            _powerValues.Clear();
            _currentSolarPower = 0f;
            _maximumSolarPower = 0f;

            // Wait
            while (DateTime.UtcNow - _startTime < _interval)
                yield return true;

            // Enumerate solar panels
            var panels = new List<IMySolarPanel>();
            GridTerminalSystem.GetBlocksOfType(panels);
            yield return true;

            // Accumulate power data
            _powerValues.Clear();
            int batch = 0;
            foreach (var panel in panels)
            {
                // Validate
                if (!panel.IsFunctional)
                    continue;

                // Accumulate
                _powerValues.Add(new PowerInfo
                {
                    Current = panel.CurrentOutput,
                    Maximum = panel.MaxOutput,
                });

                // Yield by batch
                if (++batch >= _batchSize)
                {
                    yield return true;
                    batch = 0;
                }
            }
            yield return true;

            // Calculate
            foreach (var powerInfo in _powerValues)
            {
            }
            yield return true;

            // Wait for interval to finish
            while (DateTime.UtcNow - _startTime < _interval)
                yield return true;
        }
    }