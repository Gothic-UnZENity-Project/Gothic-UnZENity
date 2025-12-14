using GUZ.Core.Adapters.Vob.Item;
using GUZ.Core.Models.Container;
using MyBox;
using Reflex.Attributes;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Services.Meshes
{
    public class ParticleService
    {
        [Inject] private readonly MeshService _meshService;
        [Inject] private readonly GameStateService _gameStateService;

        public void Init()
        {
            GlobalEventDispatcher.FightWindowInitial.AddListener((vobContainer, _) => ChangeTrail(vobContainer, false));
            GlobalEventDispatcher.FightWindowAttack.AddListener((vobContainer, _) => ChangeTrail(vobContainer, true));
            
            GlobalEventDispatcher.FightHit.AddListener(EmitBlood);
        }

        public void CreateParticleEvent(IEventParticleEffect pfx, Transform transform)
        {
            _meshService.CreateVobPfx(pfx.Name, transform.position, transform.rotation, transform.gameObject, true);
        }
        
        private void ChangeTrail(VobContainer vobContainer, bool enable)
        {
            if (enable)
                vobContainer.Go.GetComponentInChildren<WeaponAdapter>()?.StartTrail();
            else
                vobContainer.Go.GetComponentInChildren<WeaponAdapter>()?.EndTrail();
        }
        
        private void EmitBlood(NpcContainer npcContainer, VobContainer _, Vector3 position)
        {
            if (npcContainer == null || npcContainer.Instance == null)
                return;

            // Resolve guild-specific blood data
            var guild = npcContainer.Instance.Guild;

            // Emitter string used by MeshBuilder/PFX setup
            var emitter = _gameStateService?.GuildValues?.GetBloodEmitter(guild);
            if (emitter.IsNullOrEmpty())
                return;

            // Create particle effect at the hit position
            _meshService.CreateVobPfx(emitter, position, Quaternion.identity, parent: npcContainer.Go, destroyAfterPlay: true);
        }
    }
}
