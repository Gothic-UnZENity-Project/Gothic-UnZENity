using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.GothicVR.Scripts.Manager;
using GUZ.Core.Globals;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Core.Npc
{
    public class Dialog: BasePlayerBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            DialogHelper.StartDialog(properties);
        }
    }
}
