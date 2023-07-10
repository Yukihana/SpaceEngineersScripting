using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;

namespace PBScripts.Independent.AutoTurnOff
{
    internal class Program : SEProgramBase
    {
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _interval = TimeSpan.FromMinutes(1);
        }

        public void Main()
        {
            CycleCoroutine(ref _pollingTask, () => AutoTurnOffBlocks());
        }

        private IEnumerator<bool> _pollingTask = null;
        private readonly TimeSpan _interval;
        private const int BATCHSIZE = 20;

        // Coroutine

        private IEnumerator<bool> AutoTurnOffBlocks()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            long evaluated = 0;

            // Enumerate relevant blocks
            var thrusters = new List<IMyThrust>();
            GridTerminalSystem.GetBlocksOfType(thrusters);
            yield return true;

            // Validate and shut them down
            foreach (var thruster in thrusters)
            {
                evaluated++;

                // Validate
                if (thruster.CubeGrid != Me.CubeGrid)
                    continue;
                if (!thruster.IsFunctional)
                    continue;
                if (!thruster.Enabled)
                    continue;
                if (!thruster.BlockDefinition.SubtypeId.Contains("AtmosphericThruster"))
                    continue;

                // Shutdown
                thruster.Enabled = false;

                // Yield by batch
                if (evaluated % BATCHSIZE == 0)
                    yield return true;
            }
            yield return true;

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Independent:AutoTurnOff]");
            sb.AppendLine();
            sb.AppendLine("[AutoTurnOffType:AtmosphericThruster]");
            string output = sb.ToString();

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);
            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = new VRageMath.Color(1f, 0.5f, 0.5f);
            monitor.WriteText(output);
            Me.CustomData = output;

            // On early finish, wait for interval
            while (DateTime.UtcNow - startTime < _interval)
                yield return true;
        }
    }
}