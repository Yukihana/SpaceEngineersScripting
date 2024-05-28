using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SuccubusSEMods.Addons;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace SuccubusSEMods
{
    public class RadiationDamage : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation100()
        { Coroutines.CycleCoroutine(ref _enumerator, DoRadiationDamage); }

        private IEnumerator<object> _enumerator = null;

        private MyDefinitionId itemDefinitionId = new MyDefinitionId(typeof(MyObjectBuilder_Component), "YourItemSubtypeId");
        private readonly List<IMyPlayer> _players = new List<IMyPlayer>();

        public IEnumerator<object> DoRadiationDamage()
        {
            base.UpdateBeforeSimulation();

            if (!MyAPIGateway.Multiplayer.IsServer)
                yield break;

            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players);

            foreach (var player in _players)
            {
                var inventory = player.Character.GetInventory();
                if (inventory.GetItems())
                    if (
                    IMyPlayer player = .GetPlayerById(playerId);
                if (player != null)
                {
                    CheckAndApplyEffects(player);
                }
            }
        }

        public void ApplyRadiationEffects()
        {
        }
    }
}