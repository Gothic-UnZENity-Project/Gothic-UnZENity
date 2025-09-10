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
        [SerializeField] private GameObject _chestSlot;
        [SerializeField] private GameObject _doorSlot;
        [SerializeField] private GameObject _lockPickSlot;


        public override void Bootstrap()
        {
            // Chest
            {
                var chest = new Container
                {
                    IsLocked = true,
                    PickString = "RRLLRL",
                    Visual = new VisualMesh
                    {
                        Name = "CHESTSMALL_OCCHESTSMALLLOCKED"
                    }
                };

                var go = new GameObject("Chest");
                go.SetParent(_chestSlot);
                var loader = go.AddComponent<VobLoader>();
                loader.Container = new VobContainer(chest);

                VobService.InitVob(go);
            }

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
        }
    }
}
