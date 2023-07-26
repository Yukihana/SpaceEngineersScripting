using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace PBScripts._Helpers
{
    // Surface0 : Stats
    // Do not use Me.CustomData in this script.

    internal partial class SEProgramBase
    {
        public string ModuleDisplayName = "Untitled";
        public TimeSpan OutputInterval = TimeSpan.FromSeconds(11);
        public Color _outputFontColor = Color.White;
        public readonly Dictionary<string, string> _stats = new Dictionary<string, string>();

        public void DoManualOutput()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{ModuleDisplayName}]");
            sb.AppendLine();
            foreach (var item in _stats)
                sb.AppendLine($"[{item.Key}:{item.Value}]");
            string output = sb.ToString();

            IMyTextSurface surface0 = Me.GetSurface(0);
            surface0.ContentType = ContentType.TEXT_AND_IMAGE;
            surface0.FontColor = _outputFontColor;
            surface0.WriteText(output);
        }

        public IEnumerator<bool> SyncOutput()
        {
            DateTime startTime = DateTime.UtcNow;

            var sb = new StringBuilder();
            sb.AppendLine($"[{ModuleDisplayName}]");
            sb.AppendLine();
            foreach (var item in _stats)
                sb.AppendLine($"[{item.Key}:{item.Value}]");
            string output = sb.ToString();
            yield return true;

            // Post stats
            IMyTextSurface surface0 = Me.GetSurface(0);
            surface0.ContentType = ContentType.TEXT_AND_IMAGE;
            surface0.FontColor = _outputFontColor;
            surface0.WriteText(output);

            while (DateTime.UtcNow - startTime < InputInterval)
                yield return true;
        }
    }
}