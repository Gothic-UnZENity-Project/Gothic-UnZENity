using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Debugging;
using GUZ.Core.Globals;
using GUZ.Core.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager
{
    public class XRDeviceSimulatorManager: SingletonBehaviour<XRDeviceSimulatorManager>
    {
        private void Start()
        {
            GUZEvents.GeneralSceneLoaded.AddListener(delegate(GameObject playerGo)
            {
                WorldLoaded();
            });
            GUZEvents.MainMenuSceneLoaded.AddListener(WorldLoaded);
        }

        private void WorldLoaded()
        {
            if (!FeatureFlags.I.useXRDeviceSimulator)
                return;

            var simulator = PrefabCache.TryGetObject(PrefabCache.PrefabType.XRDeviceSimulator);
            SceneManager.GetActiveScene().GetRootGameObjects().Append(simulator);
        }
        
    }
}
