using System;
using VRageMath;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
        [Obsolete("Use VRageMath.Color.ToColor")]
        public static Color ToColor(string hex)
        {
            return new Color(
                byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber));
        }
    }
}