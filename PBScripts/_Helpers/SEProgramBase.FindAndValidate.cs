using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PBScripts._Helpers
{
    internal partial class SEProgramBase
    {
        // Validation postfix marker convention.
        // (Preferred, but not enforced in this code)
        // Ignore = Blacklist
        // Required = Whitelist
        // E.g.: [MyGridScriptIgnore], [MyGridScriptRequired]

        public bool ValidateBlock(
            IMyTerminalBlock block,
            string selectionMarker = null,
            bool isWhitelistMarker = false)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            if (!block.IsFunctional)
                return false;

            if (!string.IsNullOrWhiteSpace(selectionMarker) &&
                block.CustomData.Contains($"[{selectionMarker}]") != isWhitelistMarker)
                return false;

            return true;
        }

        // Same construct

        public bool ValidateBlockOnSameConstruct(
            IMyTerminalBlock block,
            string selectionMarker = null,
            bool isWhitelistMarker = false)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            if (isWhitelistMarker && string.IsNullOrWhiteSpace(block.CustomData))
                return false;
            if (!block.IsFunctional)
                return false;
            if (!block.IsSameConstructAs(Me))
                return false;

            if (!string.IsNullOrWhiteSpace(selectionMarker) &&
                block.CustomData.Contains($"[{selectionMarker}]") != isWhitelistMarker)
                return false;

            return true;
        }

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

        // Same grid

        public bool ValidateBlockOnSameGrid(
            IMyTerminalBlock block,
            IMyTerminalBlock me,
            string ignoreMarker = null)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));
            if (me == null)
                throw new ArgumentNullException(nameof(me));

            if (!block.IsFunctional ||
                block.CubeGrid != me.CubeGrid)
                return false;

            if (!string.IsNullOrWhiteSpace(ignoreMarker) &&
                block.CustomData.Contains($"[{ignoreMarker}]"))
                return false;

            return true;
        }
    }
}