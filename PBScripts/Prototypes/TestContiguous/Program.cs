using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.Prototypes.TestContiguous
{
    public partial class Program : SEProgramBase
    {
        public void Main(string argument, UpdateType updateSource)
        {
            // Define the identifier for the hangar door
            string hangarDoorIdentifier = "HangarDoor";

            // Create a list to store all doors
            List<IMyAirtightHangarDoor> allDoors = new List<IMyAirtightHangarDoor>();

            // Get all hangar doors in the grid
            GridTerminalSystem.GetBlocksOfType(allDoors);

            // Find the source door with the specific identifier in its custom data
            IMyAirtightHangarDoor sourceDoor = allDoors.FirstOrDefault(door => door.CustomData.Contains(hangarDoorIdentifier));

            if (sourceDoor != null)
            {
                // Create a list to store contiguous doors
                List<IMyAirtightHangarDoor> contiguousDoors = new List<IMyAirtightHangarDoor>();

                // Call the GetContiguous method
                int numberOfContiguousDoors = GetContiguous(sourceDoor, contiguousDoors);

                // Print the number of contiguous doors found
                Echo($"Number of contiguous hangar doors found: {numberOfContiguousDoors}");
            }
            else
            {
                Echo("Source hangar door with the specified identifier not found.");
            }
        }
    }
}