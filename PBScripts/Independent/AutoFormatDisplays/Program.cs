using PBScriptBase;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace PBScripts.Independent.AutoFormatDisplays
{
    public partial class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "AutoFormatDisplays";

        public Program()
        {
            OutputTitle = $"Independent-{SCRIPT_ID}";
            TagSelf("IndependentScript", SCRIPT_ID);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => FormatDisplays()); }

        private IEnumerator<object> _enumerator = null;

        // TagSelf

        // CycleCoroutine

        // Validate

        // TryGetMetadata

        // ParsePackedParameters

        // ScriptOutput

        // Required

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(5);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(10);

        private readonly string IGNORE_MARKER = $"[{SCRIPT_ID}Ignore]";
        private const int BATCH_SIZE = 4;

        private const string ParameterGroupIdentifier = "AutoFormatParameters";
        private readonly Color Color0 = Color.Gray;
        private readonly Color Color1 = Color.Cyan;

        private readonly List<IMyTextPanel> _panels = new List<IMyTextPanel>();
        private readonly Dictionary<IMyTextPanel, string> _customDatas = new Dictionary<IMyTextPanel, string>();
        private readonly Dictionary<string, string> _panelParameters = new Dictionary<string, string>();
        private ulong _evaluated = 0, _errors = 0, _updatesTotal = 0;

        // Routine

        private IEnumerator<object> FormatDisplays()
        {
            DateTime startTime = DateTime.UtcNow;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            uint count = 0, recent = 0, updates = 0;
            _panels.Clear();

            // Enumerate panels
            GridTerminalSystem.GetBlocksOfType(_panels);
            yield return null;

            foreach (var panel in _panels)
            {
                unchecked { _evaluated++; }
                if (_evaluated % BATCH_SIZE == 0)
                    yield return null;

                // Skip customdata validation here since it's required anyway in the step after.
                if (!ValidateBlockOnSameConstruct(panel))
                    continue;

                // Do all custom data related validations here onwards
                string customData = panel.CustomData;
                if (string.IsNullOrWhiteSpace(customData) ||
                    customData.Contains(IGNORE_MARKER))
                    continue;

                count++;
                string lastCustomData;
                if (_customDatas.TryGetValue(panel, out lastCustomData) &&
                    lastCustomData.Equals(customData))
                    continue;

                _customDatas[panel] = customData;
                _panelParameters.Clear();
                if (TryGetMetadata(panel, ParameterGroupIdentifier, out customData) &&
                    ParseArguments(customData, _panelParameters, true) > 0)
                {
                    updates += ApplyProperties(panel, _panelParameters);
                    recent++;
                }
            }
            yield return null;

            // Calculate
            _updatesTotal += updates;
            OutputStats["DisplaysTotal"] = count.ToString();
            OutputStats["DisplaysRecent"] = recent.ToString();
            OutputStats["UpdatesRecent"] = updates.ToString();
            OutputStats["UpdatesTotal"] = _updatesTotal.ToString();
            OutputStats["Errors"] = _errors.ToString();
            OutputFontColor = updates > 0 ? Color1 : Color0;
            yield return null;

            // Output
            DoManualOutput();
            yield return null;

            // On early finish, wait for interval
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
                yield return null;
        }

        private uint ApplyProperties(IMyTextPanel panel, Dictionary<string, string> properties)
        {
            uint updates = 0;
            try
            {
                // Strings
                string stringValue;
                if (properties.TryGetValue("mode", out stringValue) &&
                    stringValue.ToUpperInvariant().Equals("TEXT_AND_IMAGE"))
                { panel.ContentType = ContentType.TEXT_AND_IMAGE; updates++; }
                if (properties.TryGetValue("font", out stringValue))
                { panel.Font = stringValue; updates++; }
                // Colors
                if (properties.TryGetValue("fontcolor", out stringValue))
                { panel.FontColor = ColorExtensions.HexToColor(stringValue); updates++; }
                if (properties.TryGetValue("backcolor", out stringValue))
                { panel.BackgroundColor = ColorExtensions.HexToColor(stringValue); updates++; }
                // Floats
                float floatValue;
                if (properties.TryGetValue("fontsize", out stringValue) &&
                    float.TryParse(stringValue, out floatValue))
                { panel.FontSize = floatValue; updates++; }
            }
            catch { unchecked { _errors++; } }
            return updates;
        }
    }
}