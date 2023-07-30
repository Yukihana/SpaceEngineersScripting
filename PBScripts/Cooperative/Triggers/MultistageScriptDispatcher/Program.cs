using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.GUI.TextPanel;

namespace PBScripts.Cooperative.Triggers.CommandDispatcher
{
    internal class Program : SEProgramBase
    {
        private const string ERROR_NONAME = "Target script's name wasn't provided in the arguments. Use format `scriptname:args`.";
        private const string ERROR_NOTARGET = "Please make sure the target script's custom data contains `[[MultistageScript:ScriptName]]`.";
        // Using double square brackets only to escape echo. Actual tag should be single square brackets enclosed.

        private readonly Dictionary<string, IMyProgrammableBlock> _scripts
            = new Dictionary<string, IMyProgrammableBlock>();

        private readonly List<IMyProgrammableBlock> _raw
            = new List<IMyProgrammableBlock>();

        public void Main(string argument, UpdateType updateSource)
        {
            var result = SearchAndExecuteScript(argument, updateSource);
            if (string.IsNullOrEmpty(result))
                return;
            if (updateSource == UpdateType.Terminal)
                Echo(result);
            else
            {
                var surface = Me.GetSurface(0);
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
                surface.WriteText(result);
            }
        }

        private string SearchAndExecuteScript(string argument, UpdateType updateSource)
        {
            // Skip auto inputs just incase
            if (updateSource == UpdateType.Update1 ||
                updateSource == UpdateType.Update10 ||
                updateSource == UpdateType.Update100)
                return string.Empty;

            // Parse
            var parts = argument.Split(new char[] { ':' }, 2,
                System.StringSplitOptions.RemoveEmptyEntries);
            if (!parts.Any())
                return ERROR_NONAME;
            string scriptName = parts[0];

            // Find script
            IMyProgrammableBlock script;
            if (!TryGetScript(scriptName, out script))
                return $"A valid script with the identifier '{scriptName}' wasn't found. {ERROR_NOTARGET}";

            // Execute
            script.Enabled = true;
            if (script.TryRun(parts.Length > 1 ? parts[1] : string.Empty))
                return string.Empty;
            else
                return $"The script '{scriptName}' failed to run.";
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