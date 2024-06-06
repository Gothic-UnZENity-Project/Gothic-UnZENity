using GUZ.Core.Scripts.Manager;
using UnityEngine;

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
