using GUZ.Core.Config;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Services;
using GUZ.Core.Services.Context;
using GUZ.Core.Services.Culling;
using Reflex.Core;
using UnityEngine;

namespace GUZ.Core
{
    public class ReflexProjectInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.OnContainerBuilt += (container) => BuiltInTypeExtension.DIContainer = container;

            containerBuilder.AddSingleton(typeof(UnityMonoService));
            containerBuilder.AddSingleton(typeof(ContextInteractionService));
            containerBuilder.AddSingleton(typeof(ContextMenuService));
            containerBuilder.AddSingleton(typeof(ContextDialogService));
            containerBuilder.AddSingleton(typeof(ContextGameVersionService));

            containerBuilder.AddSingleton(typeof(MusicService));
            containerBuilder.AddSingleton(typeof(NpcMeshCullingService));
            containerBuilder.AddSingleton(typeof(VobMeshCullingService));
            containerBuilder.AddSingleton(typeof(VobSoundCullingService));
            containerBuilder.AddSingleton(typeof(SpeechToTextService));
            containerBuilder.AddSingleton(typeof(GameTimeService));

            // World
            containerBuilder.AddSingleton(typeof(WayNetService));

            // FIXME - Need to be migrated to a Service!
            containerBuilder.AddSingleton(typeof(ConfigManager));
            containerBuilder.AddSingleton(typeof(VobManager));
            containerBuilder.AddSingleton(typeof(VobInitializer));
            containerBuilder.AddSingleton(typeof(StationaryLightsManager));
            containerBuilder.AddSingleton(typeof(SkyManager));
            containerBuilder.AddSingleton(typeof(BarrierManager));
            containerBuilder.AddSingleton(typeof(LoadingManager));
        }
    }
}
