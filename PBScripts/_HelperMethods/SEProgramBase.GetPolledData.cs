using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PBScripts._HelperMethods
{
    internal partial class SEProgramBase
    {
        // Method to retrieve the Grid Power Factor from programmable blocks
        private float GetFloatData(string paramName, float defaultValue = 0.0f)
        {
            List<IMyProgrammableBlock> programmableBlocks = new List<IMyProgrammableBlock>();
            GridTerminalSystem.GetBlocksOfType(programmableBlocks);
            string prefix = $"[{paramName}:";

            foreach (IMyProgrammableBlock programmableBlock in programmableBlocks)
            {
                string customData = programmableBlock.CustomData;
                int startIndex = customData.IndexOf(prefix);

                if (startIndex != -1)
                {
                    int endIndex = customData.IndexOf("]", startIndex);
                    if (endIndex != -1)
                    {
                        int valueIndex = startIndex + prefix.Length;
                        int valueLength = endIndex - valueIndex;
                        string valueString = customData.Substring(valueIndex, valueLength);
                        if (float.TryParse(valueString, out float powerFactor))
                        {
                            return powerFactor;
                        }
                    }
                }
            }

            // Return a default value if no valid power factor is found
            return defaultValue;
        }
    }
}