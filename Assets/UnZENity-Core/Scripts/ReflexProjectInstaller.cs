using GUZ.Core.Creator;
using GUZ.Core.Domain.Animations;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Npc;
using GUZ.Core.Services;
using GUZ.Core.Services.Caches;
using GUZ.Core.Services.Config;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Vm;
using GUZ.Core.Vm;
using Reflex.Core;
using UnityEngine;

namespace GUZ.Core
{
    public class ReflexProjectInstaller : MonoBehaviour, IInstaller
    {
        public static Container DIContainer;


        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.OnContainerBuilt += (container) => DIContainer = container;

            containerBuilder.AddSingleton(typeof(UnityMonoService));
            containerBuilder.AddSingleton(typeof(ConfigService));
            containerBuilder.AddSingleton(typeof(VmService));
            containerBuilder.AddSingleton(typeof(ContextInteractionService));
            containerBuilder.AddSingleton(typeof(ContextMenuService));
            containerBuilder.AddSingleton(typeof(ContextDialogService));
            containerBuilder.AddSingleton(typeof(ContextGameVersionService));

            containerBuilder.AddSingleton(typeof(AudioService));
            containerBuilder.AddSingleton(typeof(NpcMeshCullingService));
            containerBuilder.AddSingleton(typeof(VobMeshCullingService));
            containerBuilder.AddSingleton(typeof(VobSoundCullingService));
            containerBuilder.AddSingleton(typeof(SpeechToTextService));
            containerBuilder.AddSingleton(typeof(GameTimeService));
            containerBuilder.AddSingleton(typeof(MeshService));
            containerBuilder.AddSingleton(typeof(AnimationService));
            containerBuilder.AddSingleton(typeof(DialogService));

            // Caches
            containerBuilder.AddSingleton(typeof(TextureCacheService));

            // World
            containerBuilder.AddSingleton(typeof(WayNetService));

            // FIXME - Need to be migrated to a Service!
            containerBuilder.AddSingleton(typeof(VobManager));
            containerBuilder.AddSingleton(typeof(VobInitializer));
            containerBuilder.AddSingleton(typeof(NpcManager));
            containerBuilder.AddSingleton(typeof(NpcInitializer));
            containerBuilder.AddSingleton(typeof(StationaryLightsManager));
            containerBuilder.AddSingleton(typeof(SkyService));
            containerBuilder.AddSingleton(typeof(BarrierManager));
            containerBuilder.AddSingleton(typeof(LoadingManager));
            containerBuilder.AddSingleton(typeof(BarrierManager));
        }
    }
}
