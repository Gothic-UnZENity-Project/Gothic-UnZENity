using GUZ.Core;
using GUZ.Core.Models.Container;
using GUZ.Core.Globals;
using GUZ.Core.Domain.Npc.Actions;
using GUZ.Core.Services.Npc;
using Reflex.Attributes;

namespace GUZ.Lab.AnimationActionMocks
{
    public class LabCreateInventoryItem : AbstractLabAnimationAction
    {
        [Inject] private readonly NpcService _npcService;

        public LabCreateInventoryItem(AnimationAction action, NpcContainer npcContainer) : base(action, npcContainer)
        {
        }

        public override void Start()
        {
            var itemSymbol = GameData.GothicVm.GetSymbolByName(Action.String0);

            _npcService.ExtCreateInvItems(NpcInstance, itemSymbol!.Index, 1);

            base.Start();
        }
    }
}
