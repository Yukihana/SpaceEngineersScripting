using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts.AddonModules
{
    internal partial class SEProgramBase
    {
        // Same construct

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
                block.CustomData.Contains($"{selectionMarker}") != isWhitelistMarker)
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