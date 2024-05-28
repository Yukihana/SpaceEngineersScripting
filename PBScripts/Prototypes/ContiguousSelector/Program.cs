using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace PBScripts.Prototypes.ContiguousSelector
{
    public partial class Program : SEProgramBase
    {
        public Program()
        { }

        public void Main(string argument, UpdateType updateSource)
        {
            string searchText = "[PressureDoorGroup:HangarBay0]";

            HashSet<IMyAirtightHangarDoor> unprocessed = new HashSet<IMyAirtightHangarDoor>();
            HashSet<IMyAirtightHangarDoor> processed = new HashSet<IMyAirtightHangarDoor>();

            // Add doors to unprocessed using custom data tags
            List<IMyAirtightHangarDoor> raw = new List<IMyAirtightHangarDoor>();
            GridTerminalSystem.GetBlocksOfType(raw, door => door.CubeGrid == Me.CubeGrid);
            var thisGrid = Me.CubeGrid;
            foreach (var door in raw)
            {
                if (Validate(door, thisGrid))
                    unprocessed.Add(door);
            }

            // Process doors
            while (unprocessed.Count > 0)
            {
                var sourceDoor = unprocessed.FirstElement();

                var adjacent = GetAdjacentBlocks(sourceDoor);
                foreach (var block in adjacent)
                {
                    var door = block as IMyAirtightHangarDoor;
                    if (door == null)
                        continue;

                    unprocessed.Add(door);
                }

                unprocessed.Remove(sourceDoor);
            }

            // Once all doors are available
            // Read state from the first door
            // Later, add specfication to intake argument.
            DoorStatus target = DoorStatus.Closed;
            if (processed.Any())
            {
                var door = processed.FirstElement();
                switch (door.Status)
                {
                    case DoorStatus.Closed:
                    case DoorStatus.Closing:
                        target = DoorStatus.Open;
                        break;

                    case DoorStatus.Open:
                    case DoorStatus.Opening:
                        target = DoorStatus.Closed;
                        break;
                }
            }

            // Revert
            foreach (var door in processed)
            {
                if (target == DoorStatus.Closed)
                    door.CloseDoor();
                else if (target == DoorStatus.Open)
                    door.OpenDoor();
            }
        }

        private bool Validate(IMyAirtightHangarDoor door, IMyCubeGrid cubeGrid)
        {
            /*
            string[] lines = door.CustomData.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = lines[i].Replace('\t', ' ').Trim();
            }

            if (lines.Contains(searchText))

                SelectContiguousHangarDoors(door, selectedHangarDoors);

            // Perform an action with the selected hangar doors, e.g., open/close them
            foreach (var door in selectedHangarDoors)
            {
                door.OpenDoor();
            }
            */
            throw new NotImplementedException();
        }

        private void SelectContiguousHangarDoors(IMyAirtightHangarDoor startDoor, HashSet<IMyAirtightHangarDoor> selectedDoors)
        {
            Queue<IMyAirtightHangarDoor> toCheck = new Queue<IMyAirtightHangarDoor>();
            toCheck.Enqueue(startDoor);

            while (toCheck.Count > 0)
            {
                var currentDoor = toCheck.Dequeue();
                if (selectedDoors.Contains(currentDoor))
                    continue;

                selectedDoors.Add(currentDoor);

                var neighbors = GetAdjacentBlocks(currentDoor);

                foreach (var neighbor in neighbors)
                {
                    var door = neighbor as IMyAirtightHangarDoor;
                    if (door == null)
                        continue;

                    if (!selectedDoors.Contains(door))
                        toCheck.Enqueue(door);
                }
            }
        }

        private List<IMyTerminalBlock> GetAdjacentBlocks(IMyTerminalBlock block)
        {
            var grid = block.CubeGrid;
            var position = block.Position;

            var directions = new Vector3I[]
            {
                new Vector3I(1, 0, 0),
                new Vector3I(-1, 0, 0),
                new Vector3I(0, 1, 0),
                new Vector3I(0, -1, 0),
                new Vector3I(0, 0, 1),
                new Vector3I(0, 0, -1)
            };

            var neighbors = new List<IMyTerminalBlock>();

            foreach (var direction in directions)
            {
                var neighborPosition = position + direction;
                var cubeBlock = grid.GetCubeBlock(neighborPosition);
                if (cubeBlock == null)
                    continue;
                var fatBlock = cubeBlock.FatBlock as IMyTerminalBlock;
                if (fatBlock == null)
                    continue;
                neighbors.Add(fatBlock);
            }

            return neighbors;
        }

        public void Save()
        {
            // Save state if needed (not required for this simple script)
        }
    }
}