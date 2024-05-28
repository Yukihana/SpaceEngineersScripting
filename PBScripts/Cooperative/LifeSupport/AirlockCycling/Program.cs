using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace PBScripts.Cooperative.LifeSupport.AirlockCycling
{
    public partial class Program : SEProgramBase
    {
        public Program()
        { TagSelf("MultistageScript:AirlockCycling"); }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update10)
            {
                if (_enumerators.Any())
                    CycleAllPendingAirlocks();
                else
                    Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            else if (!string.IsNullOrWhiteSpace(argument))
                AddRoutine(argument);
        }

        private readonly Dictionary<string, IEnumerator<object>> _enumerators
            = new Dictionary<string, IEnumerator<object>>();

        // TagSelf

        // RunCoroutineOnce

        //

        // Entry handlers

        private void AddRoutine(string argument)
        {
            // Extract identifier from argument
            var parts = argument.Split(new char[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (!parts.Any())
                return;
            var identifier = parts[0];

            // If an enumerator with the current identifier is active, bail
            if (_enumerators.ContainsKey(identifier))
                return;

            // Evaluate the direction, if any
            sbyte ingress = 0;
            if (parts.Length > 1)
            {
                var dirText = parts[1].ToLower();
                if (dirText == "ingress")
                    ingress = 1;
                else if (dirText == "egress")
                    ingress = -1;
            }

            // Initiate the routine
            _enumerators.Add(identifier, CycleAirlock(identifier, ingress));
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        private void CycleAllPendingAirlocks()
        {
            var keysToRemove = new List<string>();
            foreach (var enumerator in _enumerators)
            {
                if (!RunCoroutine(enumerator.Value))
                    keysToRemove.Add(enumerator.Key);
            }

            foreach (var key in keysToRemove)
                _enumerators.Remove(key);
        }

        // Routine

        private readonly Dictionary<int, Color> _colors = new Dictionary<int, Color>()
        {
            { 0, new Color(1f,1f,0f) }, // Lighting Default : Yellow
            { 1, new Color(0f,0f,1f) }, // Ingress lighting : Blue
            { 2, new Color(1f,0.5f,0f) }, // Egress lighting : Orange
            { 3, new Color(0f,1f,0f) }, // Pressurized indicator : Green
            { 4, new Color(1f,0f,0f) }, // Depressurized indicator : Red
        };

        private enum ComponentType : byte
        {
            Unknown,
            Vent,
            OuterHatch,
            InnerHatch,
            Indicator,
            IndicatorPressurized,
            IndicatorDepressurized,
            Lighting,
        }

        private IEnumerator<object> CycleAirlock(string identifier, sbyte ingress)
        {
            // Phase 0A : Get
            string requiredTag = $"[AirlockComponent:{identifier}]";
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, x => Validate(x, requiredTag));
            yield return null;

            // Phase 0B : Sort and check if critical components are available
            var components = new Dictionary<IMyTerminalBlock, ComponentType>();
            if (!SortAndQualify(blocks, components))
                yield break;
            yield return null;

            // Phase 0C : Prepare runtime
            bool depressurize = false;
            if (ingress < 0)
                depressurize = true;
            else if (ingress == 0)
                depressurize = !(components.First(x => x.Value == ComponentType.Vent).Key as IMyAirVent).Depressurize;
            var lightingColor = _colors[depressurize ? 2 : 1];
            var startingColor = _colors[depressurize ? 3 : 4];
            var endingColor = _colors[depressurize ? 4 : 3];
            var exitDoorType = depressurize ? ComponentType.OuterHatch : ComponentType.InnerHatch;
            var startingIndicator = depressurize ? ComponentType.IndicatorDepressurized : ComponentType.IndicatorPressurized;
            var endingIndicator = depressurize ? ComponentType.IndicatorPressurized : ComponentType.IndicatorDepressurized;
            yield return null;

            // Phase 1A : Set starter lights
            foreach (var component in components.Where(x => x.Key is IMyLightingBlock))
            {
                var light = component.Key as IMyLightingBlock;
                if (component.Value == startingIndicator || component.Value == ComponentType.Indicator)
                    SetLight(light, startingColor, blink: true);
                else if (component.Value == endingIndicator)
                    light.Enabled = false;
                else if (component.Value == ComponentType.Lighting)
                    SetLight(light, _colors[0]);
            }
            yield return null;

            // Phase 1B : Wait for all doors to close
            while (components
                .Keys.OfType<IMyDoor>()
                .Count(door => TryToggleDoor(door)) != 0)
                yield return null;

            // Phase 2A : Set operation lights
            foreach (var component in components.Where(x => x.Key is IMyLightingBlock))
            {
                var light = component.Key as IMyLightingBlock;
                if (component.Value == ComponentType.Lighting)
                    SetLight(light, lightingColor);
                else
                    light.Enabled = false;
            }
            yield return null;

            // Phase 2B : Vent
            IMyProgrammableBlock oxygenMonitor = null;
            bool monitorAvailable = GetMonitorScript("GridOxygenStorage", out oxygenMonitor);
            DateTime startTime = DateTime.UtcNow;
            DateTime waitMin = startTime + TimeSpan.FromSeconds(3);
            DateTime waitMax = startTime + TimeSpan.FromSeconds(10);
            while (true)
            {
                var now = DateTime.UtcNow;

                // Past max, abort anyway
                if (now > waitMax)
                    break;

                // Process vents
                float o2Level;
                if (!monitorAvailable ||
                    !GetOutput(oxygenMonitor, "FilledFactor", out o2Level))
                    o2Level = 0.5f;
                var cleared = components
                    .Keys.OfType<IMyAirVent>()
                    .Count(vent => TryVenting(vent, depressurize, o2Level)) == 0;

                // Pre min, wait anyway
                if (now > waitMin && cleared)
                    break;
                yield return null;
            }
            foreach (var vent in components.Keys.OfType<IMyAirVent>())
                vent.Enabled = false;

            // Phase 3A : Set finish lights
            foreach (var component in components.Where(x => x.Key is IMyLightingBlock))
            {
                var light = component.Key as IMyLightingBlock;
                if (component.Value == endingIndicator || component.Value == ComponentType.Indicator)
                    SetLight(light, endingColor, blink: true);
                else if (component.Value == startingIndicator)
                    light.Enabled = false;
                else if (component.Value == ComponentType.Lighting)
                    SetLight(light, _colors[0]);
            }
            yield return null;

            // Phase 4 : Wait for exit doors to open
            while (components
                .Where(x => x.Value == exitDoorType)
                .Count(x => TryToggleDoor(x.Key as IMyDoor, true)) != 0)
                yield return null;

            // Phase 5 : Finish
            foreach (var component in components.Where(x => x.Key is IMyLightingBlock))
            {
                var light = component.Key as IMyLightingBlock;
                if (component.Value == endingIndicator || component.Value == ComponentType.Indicator)
                    SetLight(light, endingColor);
                else
                    light.Enabled = false;
            }
            yield return null;
        }

        // Setters

        private void SetLight(IMyLightingBlock light, Color color, bool blink = false)
        {
            light.BlinkIntervalSeconds = blink ? 0.5f : 0f;
            light.BlinkLength = 50f;
            light.Color = color;
            light.Enabled = true;
        }

        private bool TryToggleDoor(IMyDoor door, bool open = false)
        {
            var ready =
                open && door.Status == DoorStatus.Open ||
                !open && door.Status == DoorStatus.Closed;
            door.Enabled = !ready;
            if (ready)
                return false;

            if (open && door.Status != DoorStatus.Opening)
                door.OpenDoor();
            else if (!open && door.Status != DoorStatus.Closing)
                door.CloseDoor();
            return true;
        }

        private bool TryVenting(IMyAirVent vent, bool depressurize, float gridOxygenFactor)
        {
            vent.Enabled = true;
            vent.Depressurize = depressurize;

            if (depressurize && (vent.GetOxygenLevel() == 0f || gridOxygenFactor == 1f))
                return false;
            if (!depressurize && (vent.GetOxygenLevel() > 0.95f || gridOxygenFactor == 0f))
                return false;

            return true;
        }

        // Validate and Sort

        private bool Validate(IMyTerminalBlock block, string requiredTag)
        {
            if (string.IsNullOrWhiteSpace(block.CustomData) ||
                !block.IsFunctional ||
                !block.IsSameConstructAs(Me) ||
                !(block is IMyDoor || block is IMyAirVent || block is IMyInteriorLight))
                return false;
            return block.CustomData.Contains(requiredTag);
        }

        private static bool SortAndQualify(
            IEnumerable<IMyTerminalBlock> blocks,
            Dictionary<IMyTerminalBlock, ComponentType> result)
        {
            bool hasVent = false, hasOuterHatch = false, hasInnerHatch = false;

            foreach (var block in blocks)
            {
                ComponentType type = ComponentType.Unknown;
                if (block is IMyAirVent)
                {
                    type = ComponentType.Vent;
                    hasVent = true;
                }
                else if (block is IMyDoor)
                {
                    if (block.CustomData.Contains("[AirlockOuterHatch]"))
                    {
                        type = ComponentType.OuterHatch;
                        hasOuterHatch = true;
                    }
                    else if (block.CustomData.Contains("[AirlockInnerHatch]"))
                    {
                        type = ComponentType.InnerHatch;
                        hasInnerHatch = true;
                    }
                }
                else if (block is IMyLightingBlock)
                {
                    if (block.CustomData.Contains("[AirlockIndicator]"))
                        type = ComponentType.Indicator;
                    else if (block.CustomData.Contains("[AirlockIndicator0]"))
                        type = ComponentType.IndicatorPressurized;
                    else if (block.CustomData.Contains("[AirlockIndicator1]"))
                        type = ComponentType.IndicatorDepressurized;
                    else
                        type = ComponentType.Lighting;
                }

                if (type != ComponentType.Unknown)
                    result.Add(block, type);
            }
            return hasVent && hasOuterHatch && hasInnerHatch;
        }
    }
}