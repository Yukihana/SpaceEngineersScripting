using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.Transportation.SmartRetractor
{
    /// <summary>
    /// Handles moving the retractor arms in the docking bays.
    /// </summary>
    public partial class Program : SEProgramBase
    {
        public Program()
        { TagSelf("Cooperative:SmartRetractor"); }

        public void Main(string argument, UpdateType updateSource)
        {
            if (updateSource == UpdateType.Update10)
            {
                if (_enumerators.Any())
                    ProcessPendingHangars();
                else
                    Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            else if (!string.IsNullOrWhiteSpace(argument))
                ProcessInput(argument);
        }

        // [Segment:TagSelf]

        // [Segment:ParseArguments]

        // Input

        private void ProcessInput(string argument)
        {
            var arguments = ParseArguments(argument);
            if (!arguments.Any())
                return;
            foreach (var arg in arguments)
            {
                QueueHangar(
                    identifier: arg.Key,
                    mode: string.IsNullOrEmpty(arg.Value) ? "" : arg.Value);
            }
        }

        // Queueing

        private void QueueHangar(string identifier, string mode = "")
        {
        }

        private bool Validate(IMyTerminalBlock block, string requiredTag)
        {
            if (string.IsNullOrWhiteSpace(block.CustomData) ||
                !block.IsFunctional ||
                !block.IsSameConstructAs(Me) ||
                !(block is IMyDoor || block is IMyAirVent || block is IMyInteriorLight))
                return false;
            return block.CustomData.Contains(requiredTag);
        }

        private void ProcessPendingHangars()
        {
            throw new NotImplementedException();
        }

        private void GetHangarDoors(List<IMyAirtightHangarDoor> list, string identifier, byte location)
        {
        }

        private void GetHatchDoors(List<IMyAirtightHangarDoor> list, string identifier)
        {
        }

        private void GetLights(List<IMyLightingBlock> list, string identifier)
        {
        }

        private void GetVents(List<IMyAirVent> list, string identifier)
        {
        }
    }
}