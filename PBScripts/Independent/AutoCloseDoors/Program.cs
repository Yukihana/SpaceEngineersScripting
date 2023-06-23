using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.GUI.TextPanel;

namespace PBScripts.Independent.AutoCloseDoors
{
    /// <summary>
    /// Sample script that closes doors every 5 seconds.
    /// If the door was opened less than 5 seconds ago,
    /// it will wait an extra 5 seconds before closing it.
    /// </summary>
    internal class Program : SEProgramBase
    {
        public Program()
        { Runtime.UpdateFrequency = UpdateFrequency.Update100; }

        public void Main()
        { RunCoroutine(ref _enumerator, () => CloseDoors()); }

        private IEnumerator<bool> _enumerator = null;
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromSeconds(10);
        private const int BATCH_SIZE = 16;

        // Coroutine

        private readonly Dictionary<IMyDoor, DateTime> _pendingDoors = new Dictionary<IMyDoor, DateTime>();
        private readonly TimeSpan DOOR_DELAY = TimeSpan.FromSeconds(20);
        private const string IGNORE_MARKER = "AutoCloseIgnore";

        private IEnumerator<bool> CloseDoors()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            uint evaluated = 0;
            int count = 0, closedCount = 0;

            // Enumerate
            var doors = new List<IMyDoor>();
            GridTerminalSystem.GetBlocksOfType(doors);
            yield return true;

            var carryOver = new List<IMyDoor>();
            foreach (var door in doors)
            {
                unchecked { evaluated++; }

                // Validate
                if (!door.IsSameConstructAs(Me))
                    continue;
                if (!door.IsFunctional)
                    continue;
                if (!door.Enabled)
                    continue;
                if (door.CustomData.Contains($"[{IGNORE_MARKER}]"))
                    continue;
                if (door.BlockDefinition.SubtypeId.Contains("Hangar"))
                    continue;
                if (door.BlockDefinition.SubtypeId.Contains("Gate"))
                    continue;

                // Handle door
                count++;
                DateTime openedAt;

                if (_pendingDoors.TryGetValue(door, out openedAt))
                {
                    // If already registered:
                    // Carry over if time hasn't elapsed
                    // and door hasn't been closed
                    if (DateTime.UtcNow - openedAt < DOOR_DELAY &&
                        door.Status != DoorStatus.Closed &&
                        door.Status != DoorStatus.Closing)
                        carryOver.Add(door);
                    else
                    {
                        door.CloseDoor();
                        closedCount++;
                    }
                }
                else if (door.Status == DoorStatus.Open || door.Status == DoorStatus.Opening)
                {
                    // If unregistered and open:
                    // register it, and mark for carry over
                    // Everything else, including invalidated, gets purged along the way
                    _pendingDoors.Add(door, DateTime.UtcNow);
                    carryOver.Add(door);
                }

                // Yield by batch
                if (evaluated % BATCH_SIZE == 0)
                    yield return true;
            }

            // Clean pending list
            var toClear = _pendingDoors.Keys.Except(carryOver).ToList();
            foreach (var key in toClear)
                _pendingDoors.Remove(key);
            yield return true;

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Independent:AutoCloseDoors]");
            sb.AppendLine();
            sb.AppendLine($"[GridPedestrianDoorsTotal:{count}]");
            sb.AppendLine($"[GridPedestrianDoorsPending:{_pendingDoors.Count}]");
            sb.AppendLine($"[GridPedestrianDoorsClosing:{closedCount}]");
            string output = sb.ToString();
            yield return true;

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);
            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = new VRageMath.Color(1f, 0f, 0.8f);
            monitor.WriteText(output);
            Me.CustomData = output;
            yield return true;

            // On early finish, wait for interval (No append randomization for doors)
            while (DateTime.UtcNow - startTime < INTERVAL_MINIMUM)
                yield return true;
        }
    }
}