using System;
using GUZ.Core.Context;

namespace GUZ.Core.Manager
{
    public class XRDeviceSimulatorManager
    {
        [Obsolete] public static XRDeviceSimulatorManager I;

        private readonly bool _featureEnable;

        public XRDeviceSimulatorManager(GameConfiguration config)
        {
            I = this;
            _featureEnable = config.EnableDeviceSimulator;
        }

        public void Init()
        {
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(delegate { AddXRDeviceSimulator(); });
            GlobalEventDispatcher.MainMenuSceneLoaded.AddListener(AddXRDeviceSimulator);
        }

        public void AddXRDeviceSimulator()
        {
            if (!_featureEnable)
            {
                return;
            }

            GuzContext.InteractionAdapter.CreateXRDeviceSimulator();
        }
    }
}
