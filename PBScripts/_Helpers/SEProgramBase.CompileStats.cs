using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
        public readonly Dictionary<string, string> _stats = new Dictionary<string, string>();

        public void CompileStats(string moduleName, Color color)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{moduleName}]");
            sb.AppendLine();
            foreach (var item in _stats)
                sb.AppendLine($"[{item.Key}:{item.Value}]");
            string output = sb.ToString();

            // Post stats
            IMyTextSurface monitor = Me.GetSurface(0);
            monitor.ContentType = ContentType.TEXT_AND_IMAGE;
            monitor.FontColor = color;
            monitor.WriteText(output);
            Me.CustomData = output;
        }
    }
}