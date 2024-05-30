using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        public int GetNeighbors<T>(T sourceBlock, HashSet<T> existing) where T : class, IMyTerminalBlock
        {
            int count = 0;
            IMyCubeGrid grid = sourceBlock.CubeGrid;

            // Compensating for variable block sizes
            Vector3I min = sourceBlock.Min;
            Vector3I max = sourceBlock.Max;

            // Loop through all potential neighboring positions
            for (int x = min.X - 1; x <= max.X + 1; x++)
            {
                for (int y = min.Y - 1; y <= max.Y + 1; y++)
                {
                    for (int z = min.Z - 1; z <= max.Z + 1; z++)
                    {
                        Vector3I position = new Vector3I(x, y, z);
                        if (grid.CubeExists(position))
                        {
                            IMySlimBlock slimBlock = grid.GetCubeBlock(position);
                            if (slimBlock != null)
                            {
                                T neighborBlock = slimBlock.FatBlock as T;
                                if (neighborBlock != null && neighborBlock != sourceBlock)
                                {
                                    existing.Add(neighborBlock);
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            return count;
        }

        public HashSet<T> GetNeighbors<T>(T sourceBlock) where T : class, IMyTerminalBlock
        {
            HashSet<T> result = new HashSet<T>();
            GetNeighbors(sourceBlock, result);
            return result;
        }
    }
}