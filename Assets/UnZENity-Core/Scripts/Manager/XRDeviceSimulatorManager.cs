using System;
using GUZ.Core.Context;

namespace GUZ.Core.Manager
{
    public class XRDeviceSimulatorManager
    {
        private readonly bool _featureEnable;

        public XRDeviceSimulatorManager(GameConfiguration config)
        {
            _featureEnable = config.GameControls == GuzContext.Controls.VR && config.EnableVRDeviceSimulator;
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
