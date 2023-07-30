using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace PBScripts.AddonModules
{
    // Surface0 : Stats
    // Do not use Me.CustomData in this script.

    internal partial class SEProgramBase
    {
        public string OutputTitle = "Untitled";
        public TimeSpan OutputInterval = TimeSpan.FromSeconds(11);
        public Color OutputFontColor = Color.White;
        public readonly Dictionary<string, string> OutputStats = new Dictionary<string, string>();

        private StringBuilder CompileStats()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{OutputTitle}]");
            sb.AppendLine();
            foreach (var item in OutputStats)
                sb.AppendLine($"[{item.Key}:{item.Value}]");
            return sb;
        }

        private void PostStats(StringBuilder data)
        {
            IMyTextSurface surface0 = Me.GetSurface(0);
            surface0.ContentType = ContentType.TEXT_AND_IMAGE;
            surface0.FontColor = OutputFontColor;
            surface0.WriteText(data);
        }

        public void DoManualOutput()
        { PostStats(CompileStats()); }

        public IEnumerator<object> SyncOutput()
        {
            DateTime startTime = DateTime.UtcNow;
            var output = CompileStats();
            yield return null;

            PostStats(output);
            yield return null;

            while (DateTime.UtcNow - startTime < OutputInterval)
                yield return null;
        }
    }
}