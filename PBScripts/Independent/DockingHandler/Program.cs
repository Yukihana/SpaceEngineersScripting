using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.Independent.DockingHandler
{
    internal class Program : SEProgramBase
    {
        public Program()
        { Runtime.UpdateFrequency = UpdateFrequency.Update100; }

        public void Main()
        {
            bool changed = IsDockedStateChanged();

            if (changed)
                _sequenceOngoing = true;

            // Handle enumerator if sequence ongoing. Reset if state changed.
            if (_sequenceOngoing)
                CycleCoroutine(ref _enumerator_docking, () => HandleDocking(_connectedLast.Any()), changed);

            // Only run connector polling if docking sequence is not active
            if (!_sequenceOngoing || !_allConnectors.Any())
                CycleCoroutine(ref _enumerator_caching, () => PollConnectors());
        }

        private IEnumerator<bool> _enumerator_caching = null;
        private IEnumerator<bool> _enumerator_docking = null;

        // Shared parameters

        private const string SCRIPT_ID = "DockingHandler";
        private readonly Random _random = new Random();

        private static readonly Dictionary<string, string> _panelColors = new Dictionary<string, string>()
        {
            { "Undocking", "#FF0000" }, // Red
            { "Docking", "#0088FF" }, // Cyan
            { "Undocked", "#FF8800" }, // Orange
            { "Docked", "#00FF00" }, // Green
        };

        private static readonly Dictionary<string, string> _indicatorColors = new Dictionary<string, string>()
        {
            { "Undocking", "#FF0000" }, // Red
            { "SafeUndocking", "#00FF00" }, // Green
            { "Docking", "#8800FF" }, // Purple
            { "SafeDocking", "#0044FF" }, // Blue
        };

        // Cycle Coroutine (code goes here)

        // Validate

        // To Color

        // Compile Stats

        private void RunStatsCompiler()
        {
            string state;
            if (!_params.TryGetValue($"{SCRIPT_ID}State", out state))
                state = "#FFFFFF";
            CompileOutput(SCRIPT_ID, ToColor(_panelColors[state]));
        }

        // Enumerator : Poll connectors

        private const int BATCH_SIZE_POLLING = 32;
        private readonly TimeSpan INTERVAL_POLLING = TimeSpan.FromMinutes(5);
        private readonly TimeSpan INTERVAL_POLLING_EXTRA_CAP = TimeSpan.FromMinutes(5);

        private readonly HashSet<IMyShipConnector> _allConnectors = new HashSet<IMyShipConnector>();

        private IEnumerator<bool> PollConnectors()
        {
            uint evaluated = 0;
            DateTime startTime = DateTime.UtcNow;

            _allConnectors.RemoveWhere(x => !ValidateBlockOnSameConstruct(x, $"{SCRIPT_ID}Ignore"));
            var raw = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType(raw);
            yield return true;

            foreach (var connector in raw)
            {
                unchecked { evaluated++; }

                if (ValidateBlockOnSameConstruct(connector, $"{SCRIPT_ID}Ignore") &&
                    connector.IsParkingEnabled)
                    _allConnectors.Add(connector);

                if (evaluated % BATCH_SIZE_POLLING == 0)
                    yield return true;
            }
            _params[$"{SCRIPT_ID}ConnectorsTotal"] = _allConnectors.Count.ToString();
            yield return true;

            RunStatsCompiler();
            yield return true;

            var extraSeconds = _random.Next(0, (int)INTERVAL_POLLING_EXTRA_CAP.TotalSeconds);
            var total = INTERVAL_POLLING + TimeSpan.FromSeconds(extraSeconds);
            while (DateTime.UtcNow - startTime < total)
                yield return true;
        }

        // Docking detection

        private HashSet<IMyShipConnector> _connectedLast = new HashSet<IMyShipConnector>();

        private bool IsDockedStateChanged()
        {
            var current = _allConnectors.Where(x => x.IsConnected).ToHashSet();
            _params[$"{SCRIPT_ID}ConnectorsDocked"] = current.Count.ToString();
            if (current.SetEquals(_connectedLast))
                return false;
            _connectedLast = current;
            return true;
        }

        // Enumerator : Handle grid changes

        private bool _sequenceOngoing = false;

        private IEnumerator<bool> HandleDocking(bool isDocked = false)
        {
            _params[$"{SCRIPT_ID}State"] = isDocked ? "Docking" : "Undocking";

            // Startup Indicators
            var indicators = FindBlocksOnSameConstruct<IMyInteriorLight>($"{SCRIPT_ID}Required", true);
            _params[$"{SCRIPT_ID}Indicators"] = indicators.Count.ToString();
            foreach (var light in indicators)
            {
                light.Color = ToColor(_indicatorColors[isDocked ? "Docking" : "Undocking"]);
                light.BlinkIntervalSeconds = 0.5f;
                light.BlinkLength = 50f;
                light.Enabled = true;
            }
            yield return true;

            // Write output
            RunStatsCompiler();
            yield return true;

            // Detect backup batteries
            var backupBatteries = FindBlocksOnSameConstruct<IMyBatteryBlock>($"{SCRIPT_ID}BackupBattery", true);
            _params[$"{SCRIPT_ID}BackupBatteries"] = backupBatteries.Count.ToString();

            if (backupBatteries.Any())
            {
                foreach (var light in indicators)
                    light.Color = ToColor(_indicatorColors[isDocked ? "SafeDocking" : "SafeUndocking"]);
            }
            yield return true;

            // Handle thrusters
            var thrusters = FindBlocksOnSameConstruct<IMyThrust>($"{SCRIPT_ID}Ignore");
            foreach (var thruster in thrusters)
                thruster.Enabled = !isDocked;
            _params[$"{SCRIPT_ID}Thrusters"] = $"{thrusters.Count}_" + (isDocked ? "Off" : "On");
            yield return true;

            // Handle tanks
            var tanks = FindBlocksOnSameConstruct<IMyGasTank>($"{SCRIPT_ID}Ignore");
            foreach (var tank in tanks)
                tank.Stockpile = isDocked;
            _params[$"{SCRIPT_ID}Tanks"] = $"{tanks.Count}_" + (isDocked ? "Stockpiling" : "Auto");
            yield return true;

            // Handle primary batteries
            var batteries = FindBlocksOnSameConstruct<IMyBatteryBlock>($"{SCRIPT_ID}Ignore");
            foreach (var battery in batteries)
            {
                if (!backupBatteries.Contains(battery))
                    battery.ChargeMode = isDocked ? ChargeMode.Recharge : ChargeMode.Auto;
            }
            _params[$"{SCRIPT_ID}BatteryCount"] = $"{batteries.Count}_" + (isDocked ? "Recharging" : "Auto");
            yield return true;

            // Handle backup batteries
            foreach (var battery in backupBatteries)
                battery.ChargeMode = isDocked ? ChargeMode.Auto : ChargeMode.Recharge;
            yield return true;

            // Final update
            _params[$"{SCRIPT_ID}State"] = isDocked ? "Docked" : "Undocked";
            RunStatsCompiler();
            yield return true;

            // Handle lights
            foreach (var light in indicators)
                light.Enabled = false;

            // Finish
            _sequenceOngoing = false;
        }
    }
}