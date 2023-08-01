using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRageMath;

namespace PBScripts.Independent.AutoCloseDoors
{
    internal class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "AutoCloseDoors";

        public Program()
        {
            OutputTitle = $"Independent-{SCRIPT_ID}";
            TagSelf("IndependentScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => CloseDoors()); }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // CycleCoroutine

        // Validate

        // ScriptOutput

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromSeconds(5);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromSeconds(10);

        private readonly string IGNORE_MARKER = $"{SCRIPT_ID}Ignore";
        private const int BATCH_SIZE = 32;

        private readonly TimeSpan DOOR_DELAY = TimeSpan.FromSeconds(20);
        private readonly Color Color0 = Color.Gray;
        private readonly Color Color1 = new Color(0f, 1f, 0.25f);

        private readonly List<IMyDoor> _doors = new List<IMyDoor>();
        private readonly Dictionary<IMyDoor, DateTime> _pendingDoors = new Dictionary<IMyDoor, DateTime>();
        private readonly Dictionary<IMyDoor, DateTime> _carryOver = new Dictionary<IMyDoor, DateTime>();
        private uint _evaluated = 0;

        // Routine

        private IEnumerator<object> CloseDoors()
        {
            DateTime startTime = DateTime.UtcNow;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            int count = 0, closing = 0;
            DateTime closeTime;
            yield return null;

            // Get doors
            _doors.Clear();
            GridTerminalSystem.GetBlocksOfType(_doors);
            yield return null;

            // Enumerate
            foreach (var door in _doors)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCH_SIZE == 0)
                    yield return null;

                if (!ValidateBlockOnSameConstruct(door, IGNORE_MARKER))
                    continue;

                if (door.BlockDefinition.SubtypeId.Contains("Hangar") ||
                    door.BlockDefinition.SubtypeId.Contains("Gate") ||
                    door.BlockDefinition.SubtypeId.Contains("Hatch"))
                    continue;

                count++;
                if (door.Status == DoorStatus.Closed)
                    continue;

                if (_pendingDoors.TryGetValue(door, out closeTime))
                {
                    _carryOver[door] = closeTime;

                    if (closeTime < DateTime.UtcNow)
                    {
                        door.CloseDoor();
                        closing++;
                    }
                }
                else
                    _carryOver[door] = DateTime.UtcNow + DOOR_DELAY;
            }
            yield return null;

            // Clean pending list
            _pendingDoors.Clear();
            foreach (var item in _carryOver)
                _pendingDoors[item.Key] = item.Value;
            _carryOver.Clear();
            yield return null;

            // Calculate
            OutputStats["DoorsTotal"] = count.ToString();
            OutputStats["DoorsPending"] = _pendingDoors.Count.ToString();
            OutputStats["DoorsClosing"] = closing.ToString();
            OutputStats["UpdateGuid"] = _evaluated.ToString();
            OutputFontColor = _pendingDoors.Count > 0 ? Color1 : Color0;
            yield return null;

            // Output
            DoManualOutput();
            yield return null;

            // On early finish, wait for interval
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
                yield return null;
        }
    }
}