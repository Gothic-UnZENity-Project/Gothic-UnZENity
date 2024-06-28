using System;
using GUZ.Core.Context;
using UnityEngine;

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
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(delegate(GameObject playerGo)
            {
                AddXRDeviceSimulator();
            });
            GlobalEventDispatcher.MainMenuSceneLoaded.AddListener(AddXRDeviceSimulator);
        }

        public void AddXRDeviceSimulator()
        {
            if (!_featureEnable)
            {
                return;
            }

            GUZContext.InteractionAdapter.CreateXRDeviceSimulator();
        }
    }
}
