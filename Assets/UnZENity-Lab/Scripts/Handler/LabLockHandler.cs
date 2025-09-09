using System.Collections;
using GUZ.Core.Adapters.Vob;
using GUZ.Core.Extensions;
using GUZ.Core.Models.Container;
using UnityEngine;
using ZenKit.Vobs;

namespace GUZ.Lab.Handler
{
    public class LabLockHandler : AbstractLabHandler
    {
        [SerializeField] private GameObject _doorSlot;
        [SerializeField] private GameObject _lockPickSlot;


        public override void Bootstrap()
        {
            // Door
            {
                var door = new Door
                {
                    IsLocked = true,
                    PickString = "LLRRLR",
                    Visual = new VisualMesh
                    {
                        Name = "DOOR_WOODEN"
                    }
                };

                var go = new GameObject("Door");
                go.SetParent(_doorSlot);
                var loader = go.AddComponent<VobLoader>();
                loader.Container = new VobContainer(door);

                VobService.InitVob(go);
            }

            // LockPick
            {
                var vobContainer = VobService.CreateItem(new Item
                {
                    Name = "ItKeLockpick",
                    Visual = new VisualMesh(),
                    Instance = "ItKeLockpick"
                });

                vobContainer.Go.SetParent(_lockPickSlot);
            }

            // SpawnInteractable("DOOR_WOODEN", PrefabType.VobDoor, _doorSlot);
            // SpawnItem("ItKeLockpick", _lockPickSlot, new(0, -0.5f, 0), PrefabType.VobItemLockPick);
            //
            // StartCoroutine(ExecAfter1Frame());
        }

        private IEnumerator ExecAfter1Frame()
        {
            // We need to wait 1 frame for HVR to create additional Components
            yield return null;

            // Lock the door (no rotation)
            _doorSlot.GetComponentInChildren<ConfigurableJoint>().axis = Vector3.zero;
        }
    }
}
