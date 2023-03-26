using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace PBScripts.LcdApplyProperty
{
    internal class Program : MyGridProgram
    {
        // Run

        private bool _firstRun = true;
        private const bool _autorun = true;
        private const byte _delayMultiplier = 60;
        private byte _delayCount = 0;

        public Program()
        {
            if (_autorun)
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        {
            _delayCount++;
            if (_delayCount >= _delayMultiplier)
            {
                DoRoutine();
                _delayCount = 0;
            }
            else if (_firstRun)
            {
                DoRoutine();
                _firstRun = false;
            }
        }

        // Routine

        private readonly List<IMyTextPanel> _panels = new List<IMyTextPanel>();

        public void DoRoutine()
        {
            // Enumerate:
            // We don't want to overload per-frame processing
            // So we'll process one block per permitted cycle
            if (_panels.Any())
            {
                IMyTextPanel panel = _panels[0];
                _panels.RemoveAt(0);
                SetOne(panel);
                return;
            }

            // else get all for next enumeration (this condition shouldn't be heavy)
            GridTerminalSystem.GetBlocksOfType(_panels, x => !string.IsNullOrEmpty(x.CustomData));
        }

        // Process one block

        private string[] _customData;
        private readonly List<string> _lcdproperties = new List<string>();

        private void SetOne(IMyTextPanel panel)
        {
            _customData = panel.CustomData
                    .Split(new string[] { "\r\n", "\r", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries);

            ExtractProperties(_customData);

            if (_lcdproperties.Count > 0)
                SetFontColor(panel, _lcdproperties[0]);
            if (_lcdproperties.Count > 1)
                SetBackgroundColor(panel, _lcdproperties[1]);
        }

        private void ExtractProperties(string[] elements)
        {
            bool read = false;
            foreach (string element in elements)
            {
                if (element.ToLowerInvariant().Equals("lcdautoapply{"))
                    read = true;
                else if (element.ToLowerInvariant().Equals("}"))
                    read = false;
                else if (read)
                    _lcdproperties.Add(element);
            }
        }

        private void SetFontColor(IMyTextPanel panel, string colorHex)
        {
            try { panel.FontColor = ToColor(colorHex); }
            catch { Echo($"Failed: FontColor-{colorHex}"); }
        }

        private void SetBackgroundColor(IMyTextPanel panel, string colorHex)
        {
            try { panel.BackgroundColor = ToColor(colorHex); }
            catch { Echo($"Failed: BackgroundColor-{colorHex}"); }
        }

        private static Color ToColor(string hex)
        {
            return new Color(
                byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber));
        }

        // Storage

        public void Save()
        { }
    }
}