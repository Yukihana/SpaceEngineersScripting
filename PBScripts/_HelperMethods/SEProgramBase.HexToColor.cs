using VRageMath;

namespace PBScripts._HelperMethods
{
    internal partial class SEProgramBase
    {
        protected static Color ToColor(string hex)
        {
            return new Color(
                byte.Parse(hex.Substring(1, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(3, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(5, 2), System.Globalization.NumberStyles.HexNumber));
        }
    }
}