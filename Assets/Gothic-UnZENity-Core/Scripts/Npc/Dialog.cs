using GUZ.Core.Manager;
using UnityEngine;

namespace GUZ.Core.Npc
{
    public class Dialog : BasePlayerBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            DialogManager.StartDialog(Properties);
        }
    }
}
