using System;
using System.Linq;
using VRageMath;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        [Obsolete("Use VRageMath.ColorExtensions.HexToColor as it already exists in the game.")]
        public static Color ToColor(string hex)
        {
            hex = hex.TrimStart('#');

            if (hex.Length == 3)
                hex = string.Concat(hex.Select(c => new string(c, 2)));

            if (hex.Length == 6)
            {
                return new Color(
                    byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                    byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
            }

            throw new ArgumentException("Invalid hex code", "hex");
        }
    }
}