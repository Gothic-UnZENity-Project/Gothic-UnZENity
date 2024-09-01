using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core;
using GUZ.Core.Caches;
using GUZ.Core.Creator.Meshes.V2;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Npc.Actions;
using GUZ.Core.Npc.Actions.AnimationActions;
using GUZ.Core.Properties;
using GUZ.Core.Vm;
using GUZ.Lab.AnimationActionMocks;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using ZenKit.Daedalus;

namespace GUZ.Lab.Handler
{
    public class LabNpcAnimationHandler : MonoBehaviour, ILabHandler
    {
        [FormerlySerializedAs("npcDropdown")] public TMP_Dropdown NpcDropdown;

        [FormerlySerializedAs("animationDropdown")]
        public TMP_Dropdown AnimationDropdown;

        [FormerlySerializedAs("npcSlotGo")] public GameObject NpcSlotGo;


        private Dictionary<string, (string Name, string MdhMds, string Mdm, int BodyTexNr, int BodyTexColor, string Head
            , int HeadTexNr, int TeethTexNr, string sword)> _npcs = new()
        {
            {
                "GRD_233_Bloodwyn",
                (Name: "Bloodwyn", MdhMds: "Humans_Militia.mds", Mdm: "Hum_GRDM_ARMOR", BodyTexNr: 0, BodyTexColor: 1,
                    Head: "Hum_Head_Bald", HeadTexNr: 18, TeethTexNr: 1, sword: "ItMw_1H_Sword_04")
            },
            {
                "EBR_110_Seraphia",
                (Name: "Seraphia", MdhMds: "Babe.mds", Mdm: "Bab_body_Naked0", BodyTexNr: 2, BodyTexColor: 1,
                    Head: "Bab_Head_Hair1", HeadTexNr: 2, TeethTexNr: 0, sword: null)
            },
            {
                "VLK_554_Buddler",
                (Name: "Buddler", MdhMds: "Humans_Tired.mds", Mdm: "Hum_VLKL_ARMOR", BodyTexNr: 3, BodyTexColor: 1,
                    Head: "Hum_Head_Pony", HeadTexNr: 0, TeethTexNr: 2, sword: null)
            }
        };

        private Dictionary<string, List<(Type, AnimationAction)>> _animations = new()
        {
            {
                "Human - Wash self", new List<(Type, AnimationAction)>
                {
                    (typeof(PlayAni), new AnimationAction("T_STAND_2_WASH")),
                    (typeof(Wait), new AnimationAction(float0: 5)),
                    (typeof(PlayAni), new AnimationAction("T_WASH_2_STAND"))
                }
            },
            {
                "Human - Sword training", new List<(Type, AnimationAction)>
                {
                    (typeof(DrawWeapon), new AnimationAction()),
                    (typeof(PlayAni), new AnimationAction("T_1HSFREE"))
                }
            },
            {
                "Human - Eat Apple", new List<(Type, AnimationAction)>
                {
                    (typeof(LabCreateInventoryItem), new AnimationAction("ItFoApple")),
                    (typeof(LabUseItemToState),
                        new AnimationAction("ItFoApple", int1: 0)), // int0 needs to be calculated live
                    (typeof(Wait), new AnimationAction(float0: 1)),
                    (typeof(PlayAni), new AnimationAction("T_FOOD_RANDOM_1")),
                    (typeof(Wait), new AnimationAction(float0: 1)),
                    (typeof(PlayAni), new AnimationAction("T_FOOD_RANDOM_2")),
                    (typeof(Wait), new AnimationAction(float0: 1)),
                    (typeof(LabUseItemToState), new AnimationAction("ItFoApple", int1: -1))
                }
            },
            {
                "Human - Drink Beer", new List<(Type, AnimationAction)>
                {
                    (typeof(LabCreateInventoryItem), new AnimationAction("ItFoBeer")),
                    (typeof(LabUseItemToState),
                        new AnimationAction("ItFoBeer", int1: 0)), // int0 needs to be calculated live
                    (typeof(Wait), new AnimationAction(float0: 1)),
                    (typeof(PlayAni), new AnimationAction("T_POTION_RANDOM_1")),
                    (typeof(Wait), new AnimationAction(float0: 1)),
                    (typeof(PlayAni), new AnimationAction("T_POTION_RANDOM_3")),
                    (typeof(Wait), new AnimationAction(float0: 1)),
                    (typeof(LabUseItemToState), new AnimationAction("ItFoBeer", int1: -1))
                }
            },
            {
                "Babe - Sweep", new List<(Type, AnimationAction)>
                {
                    (typeof(LabCreateInventoryItem),
                        new AnimationAction("ItMiBrush")), // int0 needs to be calculated live
                    (typeof(LabUseItemToState), new AnimationAction("ItMiBrush", int1: 1)),
                    (typeof(Wait), new AnimationAction(float0: 1)),
                    (typeof(LabUseItemToState), new AnimationAction("ItMiBrush", int1: -1))
                }
            }
        };

        public void Bootstrap()
        {
            NpcDropdown.options = _npcs.Keys.Select(item => new TMP_Dropdown.OptionData(item)).ToList();
            AnimationDropdown.options = _animations.Keys.Select(item => new TMP_Dropdown.OptionData(item)).ToList();
        }

        /// <summary>
        /// We need to prepare the NPC to load. i.e. set some NpcProperties to work properly.
        /// </summary>
        public void LoadNpcClicked()
        {
            var npcInstanceName = NpcDropdown.options[NpcDropdown.value].text;
            var npcData = _npcs[npcInstanceName];

            var newNpc = ResourceLoader.TryGetPrefabObject(PrefabType.Npc);
            newNpc.SetParent(NpcSlotGo);
            newNpc.name = npcData.Name;

            var npcSymbol = GameData.GothicVm.GetSymbolByName(npcInstanceName);
            var npcInstance = GameData.GothicVm.AllocInstance<NpcInstance>(npcSymbol!);
            var npcProps = newNpc.GetComponent<NpcProperties>();

            npcProps.NpcInstance = npcInstance;
            LookupCache.NpcCache[npcInstance.Index] = (instance: npcInstance, properties: npcProps);

            GameData.GothicVm.InitInstance(npcInstance);

            npcProps.NpcInstance = npcInstance;
            npcProps.OverlayMdsName = npcData.MdhMds;

            var body = new VmGothicExternals.ExtSetVisualBodyData
            {
                BodyTexNr = npcData.BodyTexNr,
                BodyTexColor = npcData.BodyTexColor,
                Head = npcData.Head,
                HeadTexNr = npcData.HeadTexNr,
                TeethTexNr = npcData.TeethTexNr,

                Body = "", // We set the armor via Mdm file manually
                Armor = -1 // We set the armor via Mdm file manually
            };

            MeshFactory.CreateNpc(newNpc.name, npcData.Mdm, npcData.MdhMds, body, newNpc);

            if (npcData.sword != null)
            {
                var swordIndex = GameData.GothicVm.GetSymbolByName(npcData.sword)!.Index;
                var sword = VmInstanceManager.TryGetItemData(swordIndex);

                MeshFactory.CreateNpcWeapon(newNpc, sword, (VmGothicEnums.ItemFlags)sword.MainFlag,
                    (VmGothicEnums.ItemFlags)sword.Flags);
            }
        }

        public void LoadAnimationClicked()
        {
            // Shortcut
            if (NpcSlotGo.transform.childCount == 0)
            {
                LoadNpcClicked();
            }

            var animationList = _animations[AnimationDropdown.options[AnimationDropdown.value].text];

            var npcGo = NpcSlotGo.transform.GetChild(0).gameObject;
            var props = npcGo.GetComponent<NpcProperties>();

            foreach (var anim in animationList)
            {
                var action = (AbstractAnimationAction)Activator.CreateInstance(anim.Item1, anim.Item2, npcGo);
                props.AnimationQueue.Enqueue(action);
            }
        }
    }
}
