using GUZ.Core.Manager;
using Reflex.Core;
using UnityEngine;

namespace GUZ.Core
{
    public class ReflexProjectInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.AddSingleton(typeof(MusicService));
        }
    }
}
