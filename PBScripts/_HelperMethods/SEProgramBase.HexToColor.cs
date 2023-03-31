using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace PBScripts._HelperMethods
{
    // Base class for adding shared helper methods.
    // Copy respective helper methods to PBs as required.
    internal partial class SEProgramBase : MyGridProgram
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