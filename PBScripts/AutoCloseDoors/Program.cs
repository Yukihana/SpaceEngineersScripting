using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;

namespace PBScripts.AutoCloseDoors
{
    /// <summary>
    /// Sample script that closes doors every 5 seconds.
    /// If the door was opened less than 5 seconds ago,
    /// it will wait an extra 5 seconds before closing it.
    /// </summary>
    public class Program : MyGridProgram
    {
        private byte _delayCount = 0;
        private const byte _openTicks = 2;
        private readonly List<IMyDoor> _openDoors = new List<IMyDoor>();

        private readonly Dictionary<IMyDoor, byte> _doors = new Dictionary<IMyDoor, byte>();

        // replace this
        private readonly List<IMyDoor> _lastDoors = new List<IMyDoor>();

        public Program()
        {
            // Set ticks interval
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            _delayCount++;
            if (_delayCount < _openTicks)
                return;
            _delayCount = 0;

            _openDoors.Clear();
            GridTerminalSystem.GetBlocksOfType(_openDoors, x => x.Status == DoorStatus.Open || x.Status == DoorStatus.Opening);

            // Close all door that were last open and are still open. discard rest anyway.
            foreach (IMyDoor door in _lastDoors)
            {
                if (door.Status != DoorStatus.Closed && !door.CustomData.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Contains("[autoclose-ignore]"))
                    door.CloseDoor();
                _openDoors.RemoveAll(x => x == door);
            }

            _lastDoors.Clear();
            _lastDoors.AddRange(_openDoors);
        }

        public void Save()
        { }
    }
}