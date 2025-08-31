#if GUZ_HVR_INSTALLED
using GUZ.Core;
using GUZ.Core.Npc;
using TMPro;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.VR.Adapters.UI
{
    /// <summary>
    /// Multiple subtitles can be shown at once.
    /// e.g. NPCs do ambient talks and Hero is talking to another NPC in parallel.
    /// We therefore attach this component to each NPC prefab separately.
    /// </summary>
    public class VRSubtitles : BasePlayerBehaviour, INpcSubtitles
    {
        // Hero has different behaviour for NpcInstance handling within Awake() function.
        [SerializeField]
        private bool _isHero;

        [SerializeField] private TMP_Text _dialogNpcNameText;
        [SerializeField] private TMP_Text _dialogText;


        protected override void Awake()
        {
            gameObject.SetActive(false); // The whole subtitle topic will be enabled later during gameplay.
            _dialogNpcNameText.spriteAsset = GameGlobals.Font.HighlightSpriteAsset;

            if (_isHero)
            {
                // If it's our hero, then we have no LazyLoading component and also no NpcInstance when game boots.
                // We will set these values later when calling CacheHero().
            }
            // NPC
            else
            {
                base.Awake(); // Load NpcInstance from NpcLoader2.
                _dialogNpcNameText.text = NpcInstance.GetName(NpcNameSlot.Slot0);
                NpcData.PrefabProps.NpcSubtitles = this;
            }
        }

        public void ShowSubtitles(string text)
        {
            if (!GameGlobals.Config.Gothic.IniSubtitles)
                return;

            gameObject.SetActive(true);
            _dialogText.text = text;
        }

        public void HideSubtitles()
        {
            gameObject.SetActive(false);
        }
    }
}
#endif
