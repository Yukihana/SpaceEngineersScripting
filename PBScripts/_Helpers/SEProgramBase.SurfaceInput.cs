using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace PBScripts._Helpers
{
    // Surface1 : Config
    // Do not use Me.CustomData in this script.

    internal partial class SEProgramBase
    {
        public TimeSpan InputInterval = TimeSpan.FromSeconds(10);
        public readonly Color _inputFontColor = new Color(1f, 1f, 0f);
        public readonly Dictionary<string, string> _params = new Dictionary<string, string>();
        public readonly HashSet<string> _flags = new HashSet<string>();

        public IEnumerator<bool> SyncInput()
        {
            DateTime startTime = DateTime.UtcNow;
            ReadConfig();
            while (DateTime.UtcNow - startTime < InputInterval)
                yield return true;
        }

        public void ProcessArgument(string args)
        {
            ReadConfig();
            foreach (var arg in args.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = args.Split(new char[] { ':' }, 2);
                if (parts.Length > 1)
                    _params[parts[0]] = parts[1];
                else if (!_flags.Add(parts[0]))
                    _flags.Remove(parts[0]);
            }
            UpdateConfig();
        }

        public void ReadConfig()
        {
            var sb = new StringBuilder();
            Me.GetSurface(1).ReadText(sb);
            var regex = new System.Text.RegularExpressions.Regex(@"\[(.*?)\]");
            System.Text.RegularExpressions.MatchCollection matches = regex.Matches(sb.ToString());
            _flags.Clear();
            _params.Clear();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var parts = match.Groups[1].Value.Split(new char[] { ':' }, 2);
                if (parts.Length >= 2)
                    _params[parts[0]] = parts[1];
                else
                    _flags.Add(parts[0]);
            }
        }

        private void UpdateConfig()
        {
            var sb = new StringBuilder();
            foreach (var item in _params)
                sb.AppendLine($"[{item.Key}:{item.Value}]");
            foreach (var item in _flags)
                sb.AppendLine($"[{item}]");
            var output = sb.ToString();

            IMyTextSurface surface1 = Me.GetSurface(1);
            surface1.ContentType = ContentType.TEXT_AND_IMAGE;
            surface1.FontColor = _inputFontColor;
            surface1.WriteText(output);
        }
    }
}