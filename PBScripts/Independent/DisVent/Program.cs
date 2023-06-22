using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.GUI.TextPanel;

namespace PBScripts.Independent.DisVent
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
            RunCoroutine(ref _pollingTask, () => DisableVents());
        }

        private IEnumerator<bool> _pollingTask = null;
        private readonly TimeSpan _interval;
        private const int BATCHSIZE = 20;

        // Coroutine

        private const string IGNOREMARKER = "DisVentIgnore";
        private List<IMyAirVent> _marked = new List<IMyAirVent>();

        private IEnumerator<bool> DisableVents()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            long evaluated = 0;

            // Enumerate relevant blocks
            var vents = new List<IMyAirVent>();
            GridTerminalSystem.GetBlocksOfType(vents);
            yield return true;

            // Validate and shut them down
            foreach (var vent in vents)
            {
                evaluated++;

                // Validate
                if (vent.CubeGrid != Me.CubeGrid)
                    continue;
                if (vent.CustomData.Contains($"[{IGNOREMARKER}]"))
                    continue;
                if (!vent.Enabled)
                    continue;
                if (!vent.IsFunctional)
                    continue;

                // Depress Or Shutdown
                if (vent.GetOxygenLevel() > 0)
                {
                    vent.Depressurize = true;
                    _marked.Add(vent);
                }
                else
                {
                    vent.Enabled = false;
                    if (_marked.Contains(vent))
                        vent.Depressurize = false;
                }

                // Yield by batch
                if (evaluated % BATCHSIZE == 0)
                    yield return true;
            }
            yield return true;

            // Clean up
            _marked = _marked.Where(x => x.IsFunctional && x.Depressurize).ToList();

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[Independent:DisVent]");
            sb.AppendLine();
            sb.AppendLine($"[DisVentEvaluated:{evaluated}]");
            sb.AppendLine($"[DisVentPending:{_marked.Count}]");
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