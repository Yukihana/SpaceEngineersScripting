using PBScripts._Helpers;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.Triggers.CommandDispatcher
{
    internal class Program : SEProgramBase
    {
        private readonly Dictionary<string, IMyProgrammableBlock> _scripts
            = new Dictionary<string, IMyProgrammableBlock>();

        private readonly List<IMyProgrammableBlock> _raw
            = new List<IMyProgrammableBlock>();

        public void Main(string argument, UpdateType updateSource)
        {
            // Skip auto inputs just incase
            if (updateSource == UpdateType.Update1 ||
                updateSource == UpdateType.Update10 ||
                updateSource == UpdateType.Update100)
                return;

            // Parse
            var parts = argument.Split(new char[] { ':' }, 2,
                System.StringSplitOptions.RemoveEmptyEntries);
            if (!parts.Any())
                return;
            string scriptName = parts[0];

            // Execute
            IMyProgrammableBlock script;
            if (TryGetScript(scriptName, out script))
            {
                script.Enabled = true;
                script.TryRun(parts.Length > 1 ? parts[1] : string.Empty);
            }
        }

        private bool TryGetScript(string scriptName, out IMyProgrammableBlock script)
        {
            if (_scripts.TryGetValue(scriptName, out script) && script.IsFunctional)
                return true;
            var tag = $"[MultistageScript:{scriptName}]";
            GridTerminalSystem.GetBlocksOfType(_raw, x => x.CustomData.Contains(tag));
            if (!_raw.Any())
                return false;
            script = _raw.First();
            _scripts[scriptName] = script;
            return true;
        }
    }
}