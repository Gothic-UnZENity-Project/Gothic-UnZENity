using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HurricaneVR;
using HurricaneVR.Framework.Core.Sockets;
using HurricaneVR.Framework.Core;
using GUZ.VR.Components;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using GUZ.Core;
using ZenKit.Daedalus;
using GUZ.Core.Extensions;
using System;
using GUZ.Core.Util;

namespace GUZ
{
    public class BackPackManager : MonoBehaviour
    {

        public List<string> items = new List<string>();

        public Dictionary<int, int> itemsId = new Dictionary<int, int>();
        public List<HVRGrabbable> grabbables = new List<HVRGrabbable>();

        public GameObject MeshChild;
        public HVRSocketContainer socketContainer;

        // Start is called before the first frame update
        void Start()
        {
            MeshChild = transform.Find("Mesh").gameObject;
            socketContainer = MeshChild.GetComponent<HVRSocketContainer>();
        }

        public bool HasItem(int itemID, int amount = 1)
        {
            if (itemsId.ContainsKey(itemID))
            {
                return itemsId[itemID] >= amount;
            }
            return false;
        }

        void PopulateItemIds()
        {
            itemsId.Clear();
            foreach (var item in grabbables)
            {
                var itemProperties = item.GetComponent<VobItemProperties>();
                var instanceName = itemProperties.ItemProperties.Instance;
                var itemData = VmInstanceManager.TryGetItemData(instanceName);
                var itemID = itemData.Index;
                var itemAmount = itemProperties.ItemProperties.Amount == 0 ? 1 : itemProperties.ItemProperties.Amount ;
                if (itemsId.ContainsKey(itemID))
                {
                    itemsId[itemID] += itemAmount;
                }

                // populate items inside of Hero Instance

                var npc = GameManager.I.Npcs.GetHeroContainer().Instance;
                if (npc.GetUserData() == null)
                {
                    Core.Util.Logger.LogError($"NPC is not set for {nameof(ExtCreateInvItems)}. Is it an error on Daedalus or our end?", LogCat.Npc);
                    return;
                }

                var props = npc.GetUserData().Props;
                if (props == null)
                {
                    Core.Util.Logger.LogError($"NPC not found with index {npc.Index}", LogCat.Npc);
                    return;
                }
                props.Items.TryAdd((uint)itemID, itemAmount);
            }
        }

        private object ExtCreateInvItems()
        {
            throw new NotImplementedException();
        }

        // Update is called once per frame
        void Update()
        {

            if (Time.frameCount % 150 == 0)
            {
                if (MeshChild == null)
                {
                    MeshChild = transform.Find("BackPack.fbx").Find("Mesh").gameObject;

                }
                if (socketContainer == null)
                {
                    socketContainer = MeshChild.GetComponent<HVRSocketContainer>();
                }
                List<HVRGrabbable> Listgrabbables = new List<HVRGrabbable>();
                // go through the sockets and see if there is an item in it
                foreach (var socket in socketContainer.Sockets)
                {
                    if (socket.HeldObject != null)
                    {
                        Listgrabbables.Add(socket.HeldObject);
                        var item = socket.HeldObject;
                        var itemID = item.name;
                        if (!items.Contains(itemID))
                        {
                            items.Add(itemID);
                        }
                    }
                }
                grabbables = Listgrabbables;
                PopulateItemIds();
            }


        }
    }
}
