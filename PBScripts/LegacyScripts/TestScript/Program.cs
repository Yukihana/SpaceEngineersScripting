using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;

namespace PBScripts.LegacyScripts.TestScript
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        {
            var panels = new List<IMySolarPanel>();
            GridTerminalSystem.GetBlocksOfType(panels);

            Echo(panels[0].CurrentOutput.ToString());
            Echo(panels[0].MaxOutput.ToString());
        }
    }
}