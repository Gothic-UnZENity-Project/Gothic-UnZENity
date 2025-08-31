#if GUZ_HVR_INSTALLED
using GUZ.Core.Extensions;
using GUZ.VR.Domain.Player;
using GUZ.VR.Services;
using Reflex.Core;
using UnityEngine;

namespace GUZ.VR
{
    /// <summary>
    /// Will be automatically called by Reflex when scene is loaded and SceneScope.component is added to the scene.
    /// </summary>
    public class ReflexVRSceneInstaller : MonoBehaviour, IInstaller
    {
        public void InstallBindings(ContainerBuilder containerBuilder)
        {
            containerBuilder.OnContainerBuilt += (container) => BuiltInTypeExtension.DIContainer = container;

            containerBuilder.AddSingleton(typeof(VRPlayerService));
            containerBuilder.AddSingleton(typeof(VRWeaponService));
        }
    }
}
#endif
