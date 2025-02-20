using System;
using GUZ.Core.Extensions;
using GUZ.Core.Npc;
using GUZ.Core.Properties;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core._Npc2
{
    public class NpcComponentProperties2 : VobProperties
    {
        // Hero has different behaviour. e.g. no AIHandler attached and we therefore set the data from above (CacheHero())
        [SerializeField]
        private bool _isHero;

        // FIXME - Set these via Prefab UI
        public AudioSource NpcSound;
        public Transform Bip01;
        public Transform ColliderRootMotion;
        public AiHandler AiHandler;

        [NonSerialized]
        public Transform Head;
        [NonSerialized]
        public HeadMorph HeadMorph;

        [NonSerialized]
        public GameObject CurrentInteractable; // e.g. PSI_CAULDRON
        [NonSerialized]
        public GameObject CurrentInteractableSlot; // e.g. ZS_0


        private string _focusName;

        private void Awake()
        {
            if (_isHero)
            {
                return;
            }

            // else
            var npcContainer = GetComponentInParent<NpcLoader2>().Npc.GetUserData2();
            npcContainer.Properties.NpcPrefabProperties = this;
            _focusName = npcContainer.Instance.GetName(NpcNameSlot.Slot0);
        }

        public override string GetFocusName()
        {
            return _focusName;
        }
    }
}
