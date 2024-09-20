using UnityEngine;
using ZenKit;

namespace GUZ.Core.Adapter
{
    /// <summary>
    /// We need to find a way to properly instantiate every module which wants to listen to GUZContext.Register() event.
    /// Therefore, each of them will inherit this class and be put inside Bootstrap.unity scene.
    /// </summary>
    public abstract class AbstractContextBootstrap : MonoBehaviour
    {
        protected abstract void RegisterControlModule(GameContext.Controls controls);
        protected abstract void RegisterGameVersionModule(GameVersion version);
        
        private void Awake()
        {
            GlobalEventDispatcher.RegisterControlAdapters.AddListener(RegisterControlModule);
            GlobalEventDispatcher.RegisterGameVersionAdapters.AddListener(RegisterGameVersionModule);
        }
    }
}
