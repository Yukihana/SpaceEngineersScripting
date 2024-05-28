using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PBScriptBase
{
    public partial class SEProgramBase
    {
        public bool ValidateLocation(IMyTerminalBlock block, int location)
        {
            if (location == 1 || location == 4)
                return block.IsSameConstructAs(Me) == (location == 1);
            if (location == 2 || location == 3)
                return (block.CubeGrid == Me.CubeGrid) == (location == 2);
            return true;
        }

        public bool ValidateTag(IMyTerminalBlock block, string tag, out string value)
        {
            var customData = ParseCookies(block.CustomData);
            if (customData.TryGetValue(tag, out value))
                return true;
            return false;
        }

        // Legacy

        // Same construct

        [Obsolete("Use a section for validation in the target script itself.")]
        public bool ValidateBlockOnSameConstruct(
            IMyTerminalBlock block,
            string selectionMarker = null,
            bool isWhitelistMarker = false)
        {
            if (isWhitelistMarker &&
                string.IsNullOrWhiteSpace(block.CustomData))
                return false;

            if (!block.IsFunctional ||
                !block.IsSameConstructAs(Me))
                return false;

            if (!string.IsNullOrWhiteSpace(selectionMarker) &&
                block.CustomData.Contains(selectionMarker) != isWhitelistMarker)
                return false;

            return true;
        }

        [Obsolete("Use a section for validation in the target script itself.")]
        public List<T> FindBlocksOnSameConstruct<T>(
            string selectionMarker = null,
            bool isWhitelistMarker = false)
            where T : class, IMyTerminalBlock
        {
            List<T> blocks = new List<T>();
            GridTerminalSystem.GetBlocksOfType(blocks);
            return blocks
                .Where(x => ValidateBlockOnSameConstruct(x, selectionMarker, isWhitelistMarker))
                .ToList();
        }
    }
}