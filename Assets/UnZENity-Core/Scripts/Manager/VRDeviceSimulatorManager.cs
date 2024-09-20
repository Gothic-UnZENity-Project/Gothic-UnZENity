namespace GUZ.Core.Manager
{
    public class VRDeviceSimulatorManager
    {
        private readonly bool _enabled;

        public VRDeviceSimulatorManager(GameConfiguration config)
        {
            _enabled = config.GameControls == GameContext.Controls.VR && config.EnableVRDeviceSimulator;
        }

        public void Init()
        {
            GlobalEventDispatcher.GeneralSceneLoaded.AddListener(delegate { AddVRDeviceSimulator(); });
            GlobalEventDispatcher.MainMenuSceneLoaded.AddListener(AddVRDeviceSimulator);
        }

        public void AddVRDeviceSimulator()
        {
            if (!_enabled)
            {
                return;
            }

            GameContext.InteractionAdapter.CreateVRDeviceSimulator();
        }
    }
}
