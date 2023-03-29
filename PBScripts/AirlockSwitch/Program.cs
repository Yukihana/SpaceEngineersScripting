using PBScripts._HelperMethods;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;

namespace PBScripts.AirlockSwitch
{
    internal class Program : GridProgramHelper
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        // Coordinator

        private readonly Dictionary<string, IEnumerator<byte>> _tasks = new Dictionary<string, IEnumerator<byte>>();
        private readonly HashSet<string> _overrides = new HashSet<string>();
        private readonly HashSet<string> _egressed = new HashSet<string>();

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Trigger)
            {
                string[] args = argument.Split(':', StringSplitOptions.RemoveEmptyEntries);
                if (args.Length < 2)
                    return;

                switch (args[0].ToLowerInvariant())
                {
                    case "override":
                        if (!_overrides.Add(args[1]))
                            _overrides.Remove(args[1]);
                        else
                            DiscardTask(args[1]);
                        break;

                    case "ingress":
                        _tasks.TryAdd(args[1], StartRoutine(args[1], false));
                        break;

                    case "egress":
                        _tasks.TryAdd(args[1], StartRoutine(args[1], true));
                        break;

                    case "cycle":
                        _tasks.TryAdd(args[1], StartRoutine(args[1], !_egressed.Contains(args[1])));
                        break;

                    default:
                        break;
                }

                return;
            }

            foreach (var task in _tasks)
            {
                if (_overrides.Contains(task.Key))
                    continue;
                if (!task.Value.MoveNext())
                    DiscardTask(task.Key);
                Echo($"Active tasks: {_tasks.Count}");
            }
        }

        private void DiscardTask(string key)
        {
            IEnumerator<byte> task;
            if (_tasks.TryGetValue(key, out task))
            {
                _tasks.Remove(key);
                task.Dispose();
            }
        }

        // Phase 0 : Targets

        private IEnumerator<byte> StartRoutine(string scopeId, bool egress)
        {
            yield return 1;

            // Disable inputs: sensors and lights

            var inputsensors = new List<IMySensorBlock>();
            GridTerminalSystem.GetBlocksOfType(inputsensors, x => x.CustomData.ToLower().Contains($"{scopeId}-inputsensor;"));
            inputsensors.ForEach(x => x.ApplyAction("OnOff_Off"));
            yield return 1;

            var inputbuttons = new List<IMyButtonPanel>();
            GridTerminalSystem.GetBlocksOfType(inputbuttons, x => x.CustomData.ToLower().Contains($"{scopeId}-inputbutton;"));
            inputbuttons.ForEach(x => x.ApplyAction("OnOff_Off"));
            yield return 1;

            // acquire feedback lights, append(-default:value, -opening:value, -closing:value, -pressurizing:value, -depressurizing:value)
            var lightsRaw = new List<IMyLightingBlock>();
            string lightDataId = scopeId + "-light";
            GridTerminalSystem.GetBlocksOfType(lightsRaw, x => x.CustomData.ToLower().Contains(lightDataId + "{"));
            var lights = new Dictionary<IMyLightingBlock, Dictionary<string, string>>();
            foreach (var light in lightsRaw)
            {
                yield return 1;
                var lightDetails = GrabDetails(light, lightDataId);
                lights.Add(light, lightDetails);

                string value;
                if (lightDetails.TryGetValue("closing-enabled", out value))
                    light.Color = ToColor(value);
                if (lightDetails.TryGetValue("closing-color", out value))
                    light.Color = ToColor(value);
                continue;
            }
            yield return 1;

            // acquire
            _lcds.Clear();
            GridTerminalSystem.GetBlocksOfType(_lights, x => x.CustomData.ToLower().Contains($"{_scopeId}-lcd"));
            yield return 1;

            // acquire doors
            var entryDoors = goingOut ? _doorsInside : _doorsOutside;
            string entryDoorId = $"{_scopeId}-door-{(goingOut ? "inside" : "outside")}";
            entryDoors.Clear();
            GridTerminalSystem.GetBlocksOfType(entryDoors, x => x.CustomData.ToLower().Contains(entryDoorId));
            yield return 1;

            // enable entry doors
            entryDoors.ForEach(x => x.ApplyAction("OnOff_On"));
            yield return 1;

            // close entry doors
            entryDoors.ForEach(x => x.CloseDoor());
            yield return 1;

            // wait for doors to finish closing
            foreach (var door in entryDoors)
            {
                if (door.Status != DoorStatus.Closed)
                    yield return 1;
            }

            // Feedback : Door closing
            yield return 1;
            // Phase 2 enable door(A or B) and start close
            yield return 1;
            // Phase 3 wait for door to close, then disable it
            yield return 1;
            // Feedback : Pressurize/Depressurize start
            yield return 1;
            // Red light for depress, blue light for repress
            yield return 1;
            // LCD feedback by properties
            yield return 1;

            // Action : Set vents mode, and enable
            yield return 1;
            // Action : Wait for vents to finish pressurizing
            yield return 1;

            // Feedback : Restore lights, Set 'Welcome Home' or 'Take Care' on the LCD
            yield return 1;

            // enable door B and open
            // enable buttons, sensor

            // Ensure player has moved out of sensor area
            if (inputsensors[0].)

                // Update state
                if (egress)
                    _egressed.Add(scopeId);
                else
                    _egressed.Remove(scopeId);
            yield return 1;

            // Restore inputs functionality
            foreach (var button in inputbuttons)
            {
            }
        }

        private Dictionary<string, string> GrabDetails(IMyLightingBlock light, string lightDataId)
        {
            throw new NotImplementedException();
        }
    }
}