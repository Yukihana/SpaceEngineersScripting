using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        public static bool TryClampF(ref float value, float min, float max)
        {
            var result = MathHelper.Clamp(value, min, max);
            if (result == value)
                return false;
            value = result;
            return true;
        }
    }
}