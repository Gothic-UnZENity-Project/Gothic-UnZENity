using GUZ.Core.Models.Container;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Npc;
using Reflex.Attributes;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Core.Services.Player
{
    public class PlayerService
    {
        public Vector3 HeroSpawnPosition;
        public Quaternion HeroSpawnRotation;
        
        public string LastLevelChangeTriggerVobName;
        public NpcContainer HeroContainer { get; private set; }
        
        
        [Inject] private readonly VmCacheService _vmCacheService;
        [Inject] private readonly NpcInventoryService _npcInventoryService;

        
        public PlayerService()
        {
            GlobalEventDispatcher.LockPickComboBroken.AddListener(
                (_, lockPick, _) => RemoveItem(lockPick.VobAs<IItem>().Instance, 1));
        }
        
        public void SetHero(NpcContainer heroContainer)
        {
            HeroContainer = heroContainer;
        }
        
        public void ResetSpawn()
        {
            HeroSpawnPosition = default;
            HeroSpawnRotation = default;
        }
        
        public void RemoveItem(string itemInstanceName, int amount)
        {
            var item = _vmCacheService.TryGetItemData(itemInstanceName)!;
            _npcInventoryService.ExtRemoveInvItems(HeroContainer.Instance, item.Index, amount);
        }

    }
}
