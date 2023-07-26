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
            TagSelf("IndependentScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            ModuleDisplayName = SCRIPT_ID;
        }

        public void Main()
        {
            bool changed = IsDockedStateChanged();

            if (changed)
                _sequenceOngoing = true;

            // Handle enumerator if sequence ongoing. Reset if state changed. @10 ticks
            // Change this to null-check with RunOnce on docking handler
            // On docked state changed, set a new enumerator instead, dispose the old one if applicable
            // doesn't need to run on every cycle
            if (_sequenceOngoing)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
                CycleCoroutine(ref _enumerator_docking, () => HandleDocking(_connectedLast.Any()), changed);
            }

            // Only run connector polling if docking sequence is not active. @100 ticks
            if (!_sequenceOngoing || !_allConnectors.Any())
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                CycleCoroutine(ref _enumerator_caching, () => PollConnectors());
            }
        }

        private IEnumerator<bool> _enumerator_caching = null;
        private IEnumerator<bool> _enumerator_docking = null;

        // Shared constants

        private readonly Random _random = new Random();

        private enum DockState
        {
            Unknown,
            Docking,
            Docked,
            Undocking,
            Undocked,
            Error,
        }

        private enum DockIndicatorMode
        {
            Disabled,
            UnsafeDock,
            SafeDock,
            UnsafeUndock,
            SafeUndock,
        }

        private static readonly Dictionary<DockState, Color> _outputColors = new Dictionary<DockState, Color>()
        {
            {DockState.Unknown, new Color(0.5f, 0.5f, 0.5f)},   // Grey
            {DockState.Docking, new Color(0f, 1f, 0f)},         // Teal
            {DockState.Docked, new Color(0f, 0.5f, 1f)},        // Blue
            {DockState.Undocking, new Color(1f, 0.5f, 0f)},     // Yellow
            {DockState.Undocked, new Color(1f, 0.5f, 0f)},      // Orange
            {DockState.Error, new Color(1f, 0f, 0f)},           // Red
        };

        // Shared variables

        private DockState _dockState = DockState.Unknown;
        private DockIndicatorMode _dockMode = DockIndicatorMode.SafeDock;

        // Cycle Coroutine (code goes here)

        // To Color

        // SurfaceOutput

        private void UpdateOutput()
        {
            _outputFontColor = _outputColors[_dockState];
            _params["DockedState"] = _dockState.ToString();
            DoManualOutput();
        }

        // Validate

        // Enumerator : Poll connectors

        private const int BATCH_SIZE = 128;
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(5);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(10);

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
                if (evaluated % BATCH_SIZE == 0)
                    yield return true;

                if (ValidateBlockOnSameConstruct(connector, $"{SCRIPT_ID}Ignore") &&
                    connector.IsParkingEnabled)
                    _allConnectors.Add(connector);
            }
            _params[$"ConnectorsTotal"] = _allConnectors.Count.ToString();
            yield return true;

            UpdateOutput();
            yield return true;

            // Random wait
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
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

        private bool _sequenceOngoing = false;

        // Routine

        private enum ComponentType
        {
            Unspecified,
            BackupBattery,
            Indicator,
        }

        private IEnumerator<bool> HandleDocking(bool isDocked = false)
        {
            // 0A : Prepare output
            _dockState = isDocked ? DockState.Docking : DockState.Undocking;
            UpdateOutput();
            yield return true;

            // 0B : Get blocks on the ship
            string ignoreTag = $"{SCRIPT_ID}Ignore";
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType(blocks, x => ValidateBlockOnSameConstruct(x, ignoreTag));
            yield return true;

            // 0C : Sort for relevant components
            var components = FilterAndSort(blocks);
            yield return true;

            // 0D : Misc
            int count;

            // 1A : Startup Indicators
            count = SetIndicators(components, isDocked ? DockIndicatorMode.UnsafeDock : DockIndicatorMode.UnsafeUndock);
            _params["Indicators"] = count.ToString();
            yield return true;

            // 1B : Detect backup batteries
            count = components.Count(x => x.Value == ComponentType.BackupBattery);
            if (count > 0)
                SetIndicators(components, isDocked ? DockIndicatorMode.SafeDock : DockIndicatorMode.SafeUndock);
            _params["BackupBatteries"] = count.ToString();

            // 2A : Handle thrusters
            count = 0;
            foreach (var thruster in components.Keys.OfType<IMyThrust>())
            {
                thruster.Enabled = !isDocked;
                count++;
            }
            _params["Thrusters"] = $"{count}_" + (isDocked ? "Off" : "On");
            yield return true;

            // 2B : Handle gyroscopes
            count = 0;
            foreach (var gyro in components.Keys.OfType<IMyGyro>())
            {
                gyro.Enabled = !isDocked;
                count++;
            }
            _params["Gyroscopes"] = $"{count}_" + (isDocked ? "Off" : "On");
            yield return true;

            // 2C : Handle tanks
            count = 0;
            foreach (var tank in components.Keys.OfType<IMyGasTank>())
            {
                tank.Stockpile = isDocked;
                count++;
            }
            _params["GasTanks"] = $"{count}_" + (isDocked ? "Stockpiling" : "Auto");
            yield return true;

            // 2D : Handle reflectors
            count = 0;
            foreach (var reflector in components.Where(x => x.Key is IMyReflectorLight && x.Value != ComponentType.Indicator).Select(x => x.Key as IMyReflectorLight))
            {
                if (isDocked)
                    reflector.Enabled = false;
                count++;
            }
            _params["Reflectors"] = $"{count}_" + (isDocked ? "Off" : "Free");
            yield return true;

            // 3A : PrimaryBatteries
            count = 0;
            foreach (var battery in components.Where(x => x.Key is IMyBatteryBlock && x.Value != ComponentType.BackupBattery).Select(x => x.Key as IMyBatteryBlock))
            {
                battery.ChargeMode = isDocked ? ChargeMode.Recharge : ChargeMode.Auto;
                count++;
            }
            _params["BatteryCount"] = $"{count}_" + (isDocked ? "Recharging" : "Auto");
            yield return true;

            // 3B : BackupBatteries
            foreach (var battery in components.Where(x => x.Value == ComponentType.BackupBattery).Select(x => x.Key as IMyBatteryBlock))
                battery.ChargeMode = isDocked ? ChargeMode.Auto : ChargeMode.Recharge;
            yield return true;

            // 4A : Final update
            _dockState = isDocked ? DockState.Docked : DockState.Undocked;
            UpdateOutput();
            yield return true;

            // 4B : Handle lights
            SetIndicators(components, DockIndicatorMode.Disabled);

            // Finish
            _sequenceOngoing = false;
        }

        // Subroutines

        private Dictionary<IMyTerminalBlock, ComponentType> FilterAndSort(IEnumerable<IMyTerminalBlock> blocks)
        {
            var indicatorMarker = $"[{SCRIPT_ID}Indicators]";
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