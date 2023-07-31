using PBScripts.AddonModules;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace PBScripts.Independent.AutoBasicIngot
{
    internal class Program : SEProgramBase
    {
        private const string SCRIPT_ID = "AutoBasicIngot";

        private Program()
        {
            OutputTitle = $"{SCRIPT_ID} (Survival Kits)";
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Main()
        { CycleCoroutine(ref _enumerator, () => RefreshBasicIngotQueue()); }

        private IEnumerator<object> _enumerator = null;

        // Cycle Coroutine

        // SurfaceOutput

        // Routine

        private readonly Random _random = new Random();
        private readonly TimeSpan INTERVAL_MINIMUM = TimeSpan.FromMinutes(5);
        private readonly TimeSpan INTERVAL_MAXIMUM = TimeSpan.FromMinutes(12);
        private const uint BATCH_SIZE = 4;
        private readonly string IGNORE_MARKER = $"[{SCRIPT_ID}Ignore]";

        private const string SurvivalKitTypeIdString = "MyObjectBuilder_SurvivalKit";
        private const string BasicIngotDefinitionIdString = "MyObjectBuilder_BlueprintDefinition/StoneOreToIngotBasic";
        private readonly MyDefinitionId BasicIngotDefinitionId = MyDefinitionId.Parse(BasicIngotDefinitionIdString);
        private const uint QueueAmountMax = 9000;

        private readonly List<IMyProductionBlock> _productionBlocks = new List<IMyProductionBlock>();
        private readonly List<MyProductionItem> _queueItems = new List<MyProductionItem>();

        private IEnumerator<object> RefreshBasicIngotQueue()
        {
            DateTime startTime = DateTime.UtcNow;
            uint evaluated = 0;
            double queued, pending, total = 0;
            var survivalKits = new List<IMyProductionBlock>();
            _productionBlocks.Clear();
            yield return null;

            // Filter
            GridTerminalSystem.GetBlocksOfType(survivalKits, x => x.IsSameConstructAs(Me));
            yield return null;

            survivalKits.RemoveAll(x
                => x.BlockDefinition.TypeIdString != SurvivalKitTypeIdString
                || x.CustomData.Contains(IGNORE_MARKER));
            OutputStats["SurvivalKitCount"] = survivalKits.Count.ToString();
            yield return null;

            // Requeue
            foreach (var kit in survivalKits)
            {
                unchecked { evaluated++; }
                if (evaluated % BATCH_SIZE == 0)
                    yield return null;

                _queueItems.Clear();
                kit.GetQueue(_queueItems);

                queued = 0;
                foreach (MyProductionItem item in _queueItems)
                {
                    if (item.BlueprintId == BasicIngotDefinitionId)
                        queued += (double)item.Amount;
                }

                pending = QueueAmountMax - queued;
                if (pending > 0)
                {
                    kit.AddQueueItem(BasicIngotDefinitionId, pending);
                    total += pending;
                }
            }
            yield return null;

            // Output
            OutputStats["RequeuedInLastCycle"] = total.ToString();
            OutputFontColor = total > 0 ? Color.Lime : Color.Gray;
            DoManualOutput();

            // On early finish, wait for interval
            DateTime waitTill = startTime + TimeSpan.FromSeconds(_random.Next(
                (int)INTERVAL_MINIMUM.TotalSeconds,
                (int)INTERVAL_MAXIMUM.TotalSeconds));
            while (DateTime.UtcNow < waitTill)
                yield return null;
        }
    }
}