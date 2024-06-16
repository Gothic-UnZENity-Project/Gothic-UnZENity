using System;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager
{
    public class XRDeviceSimulatorManager
    {
        [Obsolete] public static XRDeviceSimulatorManager I;

        private readonly bool _featureEnable;

        public XRDeviceSimulatorManager(GameConfiguration config)
        {
            I = this;
            _featureEnable = config.enableDeviceSimulator;
        }

        public void Init()
        {
            GUZEvents.GeneralSceneLoaded.AddListener(delegate(GameObject playerGo)
            {
                AddXRDeviceSimulator();
            });
            GUZEvents.MainMenuSceneLoaded.AddListener(AddXRDeviceSimulator);
        }

        public void AddXRDeviceSimulator()
        {
            if (!_featureEnable) return;

            var simulator = PrefabCache.TryGetObject(PrefabCache.PrefabType.XRDeviceSimulator);
            simulator.name = "XRDeviceSimulator - XRIT";
            SceneManager.GetActiveScene().GetRootGameObjects().Append(simulator);
        }
    }
}
