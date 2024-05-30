using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.Transportation.HangarDoorLock
{
    /// <summary>
    /// Handles closing of hangar door contiguous groups.
    /// </summary>
    public partial class Program : SEProgramBase
    {
        public Program()
        { TagSelf("PBScript:HangarDoorLock"); }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update10)
            {
                if (!ProcessPendingRoutines())
                    Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            else if (!string.IsNullOrWhiteSpace(argument))
                ProcessInput(argument);
        }

        private void ProcessInput(string argument)
        {
            var arguments = ParseArguments(argument);
            if (!arguments.Any())
                return;

            foreach (var arg in arguments)
            {
                var routine = HangarDoorLockRoutine(
                    identifier: arg.Key,
                    value: string.IsNullOrEmpty(arg.Value) ? "" : arg.Value);

                QueueRoutine(arg.Key, routine);
            }
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        // [Segment:TagSelf]
        // [Segment:ParseArguments]
        // [Segment:RoutineRunner]

        // Actual code

        private IEnumerator<object> HangarDoorLockRoutine(string identifier, string value)
        {
            // Get all doors
            List<IMyAirtightHangarDoor> allDoors = new List<IMyAirtightHangarDoor>();
            GridTerminalSystem.GetBlocksOfType(allDoors);
            yield return null;

            // Filter
            List<IMyAirtightHangarDoor> doors = new List<IMyAirtightHangarDoor>();
            List<IMyAirtightHangarDoor> tmp = new List<IMyAirtightHangarDoor>();
            foreach (var door in allDoors)
            {
                string groupId;
                if (door.IsFunctional &&
                    SearchCookie(door.CustomData, "PressureDoorGroup", out groupId) &&
                    ValidateLocation(door, 1))
                {
                    GetContiguous(door, tmp);
                }
            }

            // Get Contiguous

            // Apply command

            // Update status in output surface (list of group keys and their states)

            // Wait for completion (Use != for state checking. That way external changes can mark it as complete too. Also check functional. wouldn't want it to wait forever if they were disabled midway.)

            // Update output surface as completed
        }

        // Dependency segments here

        // IO dependencies
        // [Segment:ParseCookies]
    }
}