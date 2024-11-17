using GUZ.Core.UnZENity_Core.Scripts.UI;
using UnityEngine;

namespace GUZ.Core.UI
{
    public class StatusMenu : AbstractMenu
    {
        [SerializeField] private GameObject _canvas;
        [SerializeField] private GameObject _background;


        private void Awake()
        {
            GlobalEventDispatcher.ZenKitBootstrapped.AddListener(Setup);
        }

        private void Setup()
        {

        }

        public override void ToggleVisibility()
        {
            // FIXME - If opened, then fetch latest hero stats.
        }
    }
}
