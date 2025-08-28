using GUZ.Core.Config;
using GUZ.Core.Manager;
using GUZ.Core.Manager.Vobs;
using GUZ.Core.Services.Culling;
using Reflex.Core;
using UnityEngine;

namespace GUZ.Core
{
    public class ReflexProjectInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.AddSingleton(typeof(MusicService));
            containerBuilder.AddSingleton(typeof(VobSoundCullingService));
            
            // FIXME - Need to be migrated to a Service!
            containerBuilder.AddSingleton(typeof(ConfigManager));
            containerBuilder.AddSingleton(typeof(VobManager));
            containerBuilder.AddSingleton(typeof(VobInitializer));
        }
    }
}
