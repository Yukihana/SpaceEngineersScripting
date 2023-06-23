using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.Legacy.LegacyAutoCloseDoors
{
    internal class Program : MyGridProgram
    {
        /// <summary>
        /// The constructor, called only once every session and
        /// always before any other method is called. Use it to
        /// initialize your script.
        ///
        /// The constructor is optional and can be removed if not
        /// needed.
        ///
        /// It's recommended to set RuntimeInfo.UpdateFrequency
        /// here, which will allow your script to run itself without a
        /// timer block.
        /// </summary>
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        private byte _delayCount = 0;
        private const byte _delayMultiplier = 2;

        private readonly List<IMyDoor> _openDoors = new List<IMyDoor>();
        private readonly List<IMyDoor> _lastDoors = new List<IMyDoor>();

        /// <summary>
        /// The main entry point of the script, invoked every time
        /// one of the programmable block's Run actions are invoked,
        /// or the script updates itself. The updateSource argument
        /// describes where the update came from.
        ///
        /// The method itself is required, but the arguments above
        /// can be removed if not needed.
        /// </summary>
        /// <param name="argument"></param>
        /// <param name="updateSource"></param>
        public void Main(string argument, UpdateType updateSource)
        {
            _delayCount++;
            if (_delayCount < _delayMultiplier)
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

        /// <summary>
        /// Called when the program needs to save its state. Use
        /// this method to save your state to the Storage field
        /// or some other means.
        ///
        /// This method is optional and can be removed if not
        /// needed.
        /// </summary>
        public void Save()
        { }
    }
}