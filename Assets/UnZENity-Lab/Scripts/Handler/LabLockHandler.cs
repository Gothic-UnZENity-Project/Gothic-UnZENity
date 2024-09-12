using GUZ.Core;
using UnityEngine;

namespace GUZ.Lab.Handler
{
    public class LabLockHandler : AbstractLabHandler
    {
        [SerializeField] private GameObject _doorSlot;
        [SerializeField] private GameObject _lockPickSlot;


        public override void Bootstrap()
        {
            SpawnInteractable("DOOR_WOODEN", PrefabType.VobDoor, _doorSlot);
            SpawnItem("ItKeLockpick", _lockPickSlot, new(0, -0.5f, 0), PrefabType.VobItemLockPick);
        }
    }
}
