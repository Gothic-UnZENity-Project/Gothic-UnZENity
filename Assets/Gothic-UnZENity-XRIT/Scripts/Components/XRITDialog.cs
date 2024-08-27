using System;
using GUZ.Core.Manager;
using GUZ.Core.Npc;
using UnityEngine;

namespace GUZ.XRIT.Components
{
    [Obsolete("Not yet migrated to new HVR/XRIT logic. Can be removed once the menus are migrated to HVR.")]
    public class XRITDialog : BasePlayerBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            DialogManager.StartDialog(gameObject, Properties, true);
        }
    }
}
