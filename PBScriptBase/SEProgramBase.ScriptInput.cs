using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace PBScriptBase
{
    // Surface1 : Config
    // Do not use Me.CustomData in this script.

    public partial class SEProgramBase
    {
        public readonly Color InputFontColor = new Color(1f, 1f, 0f);
        public bool IsConfigUnsaved = false;
        public readonly Dictionary<string, string> InputParameters = new Dictionary<string, string>();
        public readonly HashSet<string> InputFlags = new HashSet<string>();

        public void ProcessArgument(string args)
        {
            ReadConfig();
            foreach (var arg in args.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = args.Split(new char[] { ':' }, 2);
                if (parts.Length > 1)
                    InputParameters[parts[0]] = parts[1];
                else if (!InputFlags.Add(parts[0]))
                    InputFlags.Remove(parts[0]);
            }
            UpdateConfig();
        }

        public void ReadConfig()
        {
            var sb = new StringBuilder();
            Me.GetSurface(1).ReadText(sb);
            var regex = new System.Text.RegularExpressions.Regex(@"\[(.*?)\]");
            System.Text.RegularExpressions.MatchCollection matches = regex.Matches(sb.ToString());
            InputFlags.Clear();
            InputParameters.Clear();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var parts = match.Groups[1].Value.Split(new char[] { ':' }, 2);
                if (parts.Length >= 2)
                    InputParameters[parts[0]] = parts[1];
                else
                    InputFlags.Add(parts[0]);
            }
        }

        public void UpdateConfig()
        {
            var sb = new StringBuilder();
            foreach (var item in InputParameters)
                sb.AppendLine($"[{item.Key}:{item.Value}]");
            foreach (var item in InputFlags)
                sb.AppendLine($"[{item}]");

            IMyTextSurface surface1 = Me.GetSurface(1);
            surface1.ContentType = ContentType.TEXT_AND_IMAGE;
            surface1.FontColor = InputFontColor;
            surface1.WriteText(sb);
        }

        public bool TryGetOrAddParameter(string key, out string value, string defaultValue)
        {
            if (InputParameters.TryGetValue(key, out value))
                return true;
            InputParameters[key] = defaultValue;
            value = defaultValue;
            return false;
        }

        public bool TryGetOrAddParameter(string key, out float value, float defaultValue)
        {
            string valueString;
            if (InputParameters.TryGetValue(key, out valueString) &&
                float.TryParse(valueString, out value))
                return true;
            InputParameters[key] = defaultValue.ToString();
            value = defaultValue;
            return false;
        }

        public bool TryGetOrAddParameter(string key, out bool value, bool defaultValue)
        {
            string valueString;
            if (InputParameters.TryGetValue(key, out valueString) &&
                bool.TryParse(valueString, out value))
                return true;
            InputParameters[key] = defaultValue.ToString();
            value = defaultValue;
            return false;
        }
    }
}