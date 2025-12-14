using System.Linq;
using GUZ.Core;
using GUZ.Core.Adapters.Npc;
using GUZ.Core.Extensions;
using GUZ.Core.Models.Container;
using GUZ.Core.Services.Caches;
using HurricaneVR.Framework.Core;
using HurricaneVR.Framework.Core.Grabbers;
using Reflex.Attributes;
using UnityEngine;
using ZenKit.Daedalus;
using ZenKit.Vobs;

namespace GUZ.VR.Adapters.Marvin
{
    // FIXME - Can be used for proper Marvin mode feature: "kill"
    public class VRNpcMarvinAdapter : MonoBehaviour
    {
        [Inject] private MultiTypeCacheService _multiTypeCacheService;
        
        private NpcContainer _npcData;

        private void Awake()
        {
            _npcData = GetComponentInParent<NpcLoader>().Npc.GetUserData();
        }
        
        public void OnGrabbed(HVRGrabberBase grabber, HVRGrabbable grabbable)
        {
            // Safety return, until we have proper Marvin feature handling implemented (active = true/false)
            return;
            
            var remainingHitPoints = _npcData.Vob.GetAttribute((int)NpcAttribute.HitPoints);
            var anyWeapon = _multiTypeCacheService.VobCache.First(container => container.Vob.Type == VirtualObjectType.oCItem);
            var origDamage = anyWeapon.GetItemInstance().DamageTotal;
            
            anyWeapon.GetItemInstance().DamageTotal = remainingHitPoints;
            
            GlobalEventDispatcher.FightHit.Invoke(_npcData, anyWeapon, default);
            
            anyWeapon.GetItemInstance().DamageTotal = origDamage;
        }
    }
}
