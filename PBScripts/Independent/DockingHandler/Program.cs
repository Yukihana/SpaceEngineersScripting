using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.VisualScripting.Utils;
using VRageMath;

namespace PBScripts.Independent.DockingHandler
{
    internal class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "DockingHandler";

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            TagSelf("IndependentScript", SCRIPT_ID);
            OutputTitle = SCRIPT_ID;
            OutputInterval = TimeSpan.FromSeconds(5);
        }

        public void Main()
        {
            bool changed = IsDockedStateChanged();

            if (_task != null || changed)
            {
                RunCoroutine(ref _task, () => HandleDocking(_connectedLast.Any()), changed);
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            }

            if (_task == null || !_allConnectors.Any())
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                CycleCoroutine(ref _enumerator_caching, () => PollConnectors());
            }

            OutputStats["UpdateSpeed"] = Runtime.UpdateFrequency.ToString();
            CycleCoroutine(ref _outputEnumerator, () => SyncOutput());
        }

        private IEnumerator<object> _enumerator_caching = null;
        private IEnumerator<object> _task = null;
        private IEnumerator<object> _outputEnumerator = null;

        // Coroutines

        // TagSelf

        // SurfaceOutput

        // Validate

        // Cycle Coroutine

        // Output Scaffold

        private static readonly Dictionary<DockState, Color> _outputColors = new Dictionary<DockState, Color>()
        {
            {DockState.Unknown, new Color(0.5f, 0.5f, 0.5f)},   // Grey
            {DockState.Docking, new Color(0f, 1f, 0f)},         // Teal
            {DockState.Docked, new Color(0f, 0.5f, 1f)},        // Blue
            {DockState.Undocking, new Color(1f, 0.5f, 0f)},     // Yellow
            {DockState.Undocked, new Color(1f, 0.5f, 0f)},      // Orange
            {DockState.Error, new Color(1f, 0f, 0f)},           // Red
        };

        private void UpdateOutput()
        {
            OutputFontColor = _outputColors[_dockState];
            OutputStats["DockedState"] = _dockState.ToString();
            DoManualOutput();
        }

        // Enumerator : Poll connectors

        private readonly Random _random = new Random();
        private const int BATCH_SIZE = 128;
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(5);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(10);
        private readonly HashSet<IMyShipConnector> _allConnectors = new HashSet<IMyShipConnector>();

        private IEnumerator<object> PollConnectors()
        {
            uint evaluated = 0;
            DateTime startTime = DateTime.UtcNow;
            string ignoreMarker = $"[{SCRIPT_ID}Ignore]";

            if (_allConnectors.Any())
            {
                _allConnectors.RemoveWhere(x => !ValidateBlockOnSameConstruct(x, ignoreMarker));
                yield return null;
            }

            var raw = new List<IMyShipConnector>();
            GridTerminalSystem.GetBlocksOfType(raw);
            yield return null;

            foreach (var connector in raw)
            {
                unchecked { evaluated++; }
                if (evaluated % BATCH_SIZE == 0)
                    yield return null;

                if (ValidateBlockOnSameConstruct(connector, ignoreMarker) &&
                    connector.IsParkingEnabled)
                    _allConnectors.Add(connector);
            }
            OutputStats[$"ConnectorsTotal"] = _allConnectors.Count.ToString();
            yield return null;

            UpdateOutput();
            yield return null;

            // Random wait
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
                yield return null;
        }

        // Docking detection

        private HashSet<IMyShipConnector> _connectedLast = new HashSet<IMyShipConnector>();

        private bool IsDockedStateChanged()
        {
            var current = _allConnectors.Where(x => x.IsConnected).ToHashSet();
            OutputStats[$"{SCRIPT_ID}ConnectorsDocked"] = current.Count.ToString();
            if (current.SetEquals(_connectedLast))
                return false;
            _connectedLast = current;
            return true;
        }

        // Routine

        private enum DockState
        { Unknown, Docking, Docked, Undocking, Undocked, Error, }

        private enum DockIndicatorMode
        { Disabled, UnsafeDock, SafeDock, UnsafeUndock, SafeUndock, }

        private DockState _dockState = DockState.Unknown;

        private IEnumerator<object> HandleDocking(bool isDocked = false)
        {
            // 0A : Prepare output
            _dockState = isDocked ? DockState.Docking : DockState.Undocking;
            UpdateOutput();
            yield return null;

            // 0B : Get blocks on the ship
            string ignoreTag = $"[{SCRIPT_ID}Ignore]";
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, x => ValidateBlockOnSameConstruct(x, ignoreTag));
            yield return null;

            // 0C : Sort for relevant components
            var components = FilterAndSort(blocks);
            yield return null;

            // 0D : Misc
            int count;

            // 1A : Startup Indicators
            count = SetIndicators(components, isDocked ? DockIndicatorMode.UnsafeDock : DockIndicatorMode.UnsafeUndock);
            OutputStats["Indicators"] = count.ToString();
            yield return null;

            // 1B : Detect backup batteries
            count = components.Count(x => x.Value == ComponentType.BackupBattery);
            if (count > 0)
                SetIndicators(components, isDocked ? DockIndicatorMode.SafeDock : DockIndicatorMode.SafeUndock);
            OutputStats["BackupBatteries"] = count.ToString();

            // 2A : Handle thrusters
            count = 0;
            foreach (var thruster in components.Keys.OfType<IMyThrust>())
            {
                thruster.Enabled = !isDocked;
                count++;
            }
            OutputStats["Thrusters"] = $"{count}_" + (isDocked ? "Off" : "On");
            yield return null;

            // 2B : Handle gyroscopes
            count = 0;
            foreach (var gyro in components.Keys.OfType<IMyGyro>())
            {
                gyro.Enabled = !isDocked;
                count++;
            }
            OutputStats["Gyroscopes"] = $"{count}_" + (isDocked ? "Off" : "On");
            yield return null;

            // 2C : Handle tanks
            count = 0;
            foreach (var tank in components.Keys.OfType<IMyGasTank>())
            {
                tank.Stockpile = isDocked;
                count++;
            }
            OutputStats["GasTanks"] = $"{count}_" + (isDocked ? "Stockpiling" : "Auto");
            yield return null;

            // 2D : Handle reflectors
            count = 0;
            foreach (var reflector in components.Where(x => x.Key is IMyReflectorLight && x.Value != ComponentType.Indicator).Select(x => x.Key as IMyReflectorLight))
            {
                if (isDocked)
                    reflector.Enabled = false;
                count++;
            }
            OutputStats["Reflectors"] = $"{count}_" + (isDocked ? "Off" : "Free");
            yield return null;

            // 3A : PrimaryBatteries
            count = 0;
            foreach (var battery in components.Where(x => x.Key is IMyBatteryBlock && x.Value != ComponentType.BackupBattery).Select(x => x.Key as IMyBatteryBlock))
            {
                battery.ChargeMode = isDocked ? ChargeMode.Recharge : ChargeMode.Auto;
                count++;
            }
            OutputStats["BatteryCount"] = $"{count}_" + (isDocked ? "Recharging" : "Auto");
            yield return null;

            // 3B : BackupBatteries
            foreach (var battery in components.Where(x => x.Value == ComponentType.BackupBattery).Select(x => x.Key as IMyBatteryBlock))
                battery.ChargeMode = isDocked ? ChargeMode.Auto : ChargeMode.Recharge;
            yield return null;

            // 4A : Final update
            _dockState = isDocked ? DockState.Docked : DockState.Undocked;
            UpdateOutput();
            yield return null;

            // 4B : Handle lights
            SetIndicators(components, DockIndicatorMode.Disabled);
        }

        // Subroutines

        private enum ComponentType
        {
            Unspecified,
            BackupBattery,
            Indicator,
        }

        private Dictionary<IMyTerminalBlock, ComponentType> FilterAndSort(IEnumerable<IMyTerminalBlock> blocks)
        {
            var indicatorMarker = $"[{SCRIPT_ID}Indicator]";
            var backupBatteryMarker = $"[{SCRIPT_ID}BackupBattery]";
            var result = new Dictionary<IMyTerminalBlock, ComponentType>();
            foreach (var block in blocks)
            {
                var blockType = ComponentType.Unspecified;
                if (block is IMyLightingBlock)
                {
                    if (block.CustomData.Contains(indicatorMarker))
                        blockType = ComponentType.Indicator;
                    else if (block is IMyReflectorLight)
                        blockType = ComponentType.Unspecified;
                }
                else if (block is IMyBatteryBlock)
                {
                    blockType = block.CustomData.Contains(backupBatteryMarker)
                        ? ComponentType.BackupBattery
                        : ComponentType.Unspecified;
                }
                else if (!(block is IMyThrust
                    || block is IMyGyro
                    || block is IMyGasTank))
                    continue;

                result.Add(block, blockType);
            }
            return result;
        }

        // Indicators

        private static readonly Dictionary<DockIndicatorMode, Color> _indicatorColors = new Dictionary<DockIndicatorMode, Color>()
        {
            {DockIndicatorMode.Disabled, new Color(1f,1f,1f)},          // Off*
            {DockIndicatorMode.UnsafeDock, new Color(1f, 0f, 0f)},      // Red
            {DockIndicatorMode.SafeDock, new Color(0f, 1f, 0f)},        // Green
            {DockIndicatorMode.UnsafeUndock, new Color(0.5f, 0f, 1f) }, // Purple
            {DockIndicatorMode.SafeUndock, new Color(0f, 0.5f, 1f)},    // Indigo
        };

        private int SetIndicators(Dictionary<IMyTerminalBlock, ComponentType> components, DockIndicatorMode mode)
        {
            int count = 0;
            bool enabled = mode != DockIndicatorMode.Disabled;
            Color color = _indicatorColors[mode];
            foreach (var light in components.Where(x => x.Value == ComponentType.Indicator).Select(x => x.Key as IMyLightingBlock))
            {
                count++;
                if (enabled)
                {
                    light.Color = color;
                    light.BlinkIntervalSeconds = 0.5f;
                    light.BlinkLength = 50f;
                }
                light.Enabled = enabled;
            }
            return count;
        }
    }
}