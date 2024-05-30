using Sandbox.ModAPI.Ingame;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        public int GetContiguous<T>(T sourceBlock, HashSet<T> existing) where T : class, IMyTerminalBlock
        {
            if (sourceBlock == null)
                return 0;

            int oldCount = existing.Count;

            // A set to keep track of visited blocks
            Queue<T> toProcess = new Queue<T>();
            HashSet<T> processed = new HashSet<T>();
            HashSet<T> tmp = new HashSet<T>();

            // Start the search with the source block
            toProcess.Enqueue(sourceBlock);
            while (toProcess.Count > 0)
            {
                T current = toProcess.Dequeue();
                tmp.Clear();
                GetNeighbors(current, tmp);

                foreach (T neighbor in tmp)
                {
                    if (!processed.Contains(neighbor))
                        toProcess.Enqueue(neighbor);
                }
                processed.Add(current);
            }

            foreach (T block in processed)
                existing.Add(block);

            return existing.Count - oldCount;
        }

        public HashSet<T> GetContiguous<T>(T sourceBlock) where T : class, IMyTerminalBlock
        {
            HashSet<T> contiguous = new HashSet<T>();
            GetContiguous(sourceBlock, contiguous);
            return contiguous;
        }
    }
}