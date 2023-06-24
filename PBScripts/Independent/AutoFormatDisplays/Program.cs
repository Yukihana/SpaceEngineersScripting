using PBScripts._Helpers;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game.GUI.TextPanel;

namespace PBScripts.Independent.AutoFormatDisplays
{
    internal class Program : SEProgramBase
    {
        public Program()
        { Runtime.UpdateFrequency = UpdateFrequency.Update100; }

        public void Main()
        { RunCoroutine(ref _enumerator, () => FormatDisplays()); }

        private IEnumerator<bool> _enumerator = null;
        private readonly TimeSpan INTERVAL_FIXED_MINIMUM = TimeSpan.FromMinutes(5);
        private const int BATCH_SIZE = 1;

        // Coroutine

        private const string IGNORE_MARKER = "AutoFormatIgnore";
        private readonly Dictionary<IMyTextPanel, string> _panelCustomDataCache = new Dictionary<IMyTextPanel, string>();

        private IEnumerator<bool> FormatDisplays()
        {
            // Prepare
            DateTime startTime = DateTime.UtcNow;
            ulong evaluated = 0;
            uint count = 0, updated = 0;

            string lastData;
            var carryOver = new List<IMyTextPanel>();

            // Enumerate panels
            var panels = new List<IMyTextPanel>();
            GridTerminalSystem.GetBlocksOfType(panels);
            yield return true;

            foreach (var panel in panels)
            {
                // Yield by batch
                if (++evaluated % BATCH_SIZE == 0)
                    yield return true;

                // Validate
                if (!panel.IsSameConstructAs(Me) ||
                    !panel.IsFunctional || !panel.Enabled ||
                    panel.CustomData.Contains($"[{IGNORE_MARKER}]") ||
                    string.IsNullOrWhiteSpace(panel.CustomData))
                    continue;

                // Preprocess
                count++;
                carryOver.Add(panel);
                if (_panelCustomDataCache.TryGetValue(panel, out lastData) &&
                    panel.CustomData.Equals(lastData))
                    continue;
                else
                    _panelCustomDataCache[panel] = panel.CustomData;

                // Parse and apply
                var properties = ParseProperties(panel.CustomData);
                yield return true;
                if (ApplyProperties(panel, properties))
                    updated++;
            }
            yield return true;

            // Cleanup
            var toClear = _panelCustomDataCache.Keys.Except(carryOver).ToList();
            foreach (var key in toClear)
                _panelCustomDataCache.Remove(key);
            yield return true;

            // Prepare stats
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[Independent:AutoFormatDisplays]");
            sb.AppendLine();
            sb.AppendLine($"[AutoFormatDisplaysCount:{count}]");
            sb.AppendLine($"[AutoFormatDisplaysUpdated:{updated}]");
            string output = sb.ToString();
            yield return true;

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);
            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = new VRageMath.Color(1f, 0f, 0.8f);
            monitor.WriteText(output);
            Me.CustomData = output;
            yield return true;

            // On early finish, wait for interval
            while (DateTime.UtcNow - startTime < INTERVAL_FIXED_MINIMUM)
                yield return true;
        }

        private bool ApplyProperties(IMyTextPanel panel, Dictionary<string, string> properties)
        {
            int updated = 0;
            try
            {
                string stringValue;
                if (properties.TryGetValue("ThisTextPanelMode", out stringValue) &&
                    stringValue.ToUpperInvariant().Equals("TEXT_AND_IMAGE"))
                { panel.ContentType = ContentType.TEXT_AND_IMAGE; updated++; }
                if (properties.TryGetValue("ThisTextPanelFont", out stringValue))
                { panel.Font = stringValue; updated++; }
                if (properties.TryGetValue("ThisTextPanelFontColor", out stringValue))
                { panel.FontColor = ToColor(stringValue); updated++; }
                if (properties.TryGetValue("ThisTextPanelBackgroundColor", out stringValue))
                { panel.BackgroundColor = ToColor(stringValue); updated++; }

                float floatValue;
                if (properties.TryGetValue("ThisTextPanelBackgroundColor", out stringValue) &&
                    float.TryParse(stringValue, out floatValue))
                { panel.FontSize = floatValue; updated++; }

                return updated > 0;
            }
            catch { return false; }
        }
    }
}