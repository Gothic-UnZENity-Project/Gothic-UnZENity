using System.Collections;
using GUZ.Core;
using GUZ.Core._Npc2;
using GUZ.Core.Caches;
using GUZ.Core.Creator;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Npc;
using GUZ.Core.Vm;
using GUZ.Lab.Mocks;
using UnityEngine;
using ZenKit.Daedalus;

namespace GUZ.Lab.Handler
{
    public class LabNpcDialogHandler : AbstractLabHandler
    {
        public GameObject NpcSlotGo;

        private string _bloodwynInstanceId = "GRD_233_Bloodwyn";
        private NpcInstance _bloodwynInstance;


        public override void Bootstrap()
        {
            BootstrapBloodwyn();

            StartCoroutine(IdleAnimations());
        }

        /// <summary>
        /// Some random animations to test blending etc.
        /// </summary>
        private IEnumerator IdleAnimations()
        {
            var mdsNames = new [] { "humans" };
            var npcRoot = NpcSlotGo.transform.GetChild(0).GetChild(0).gameObject;
            var animHandler = npcRoot.GetComponent<NpcAnimationHandler>();
            var animHeadHandler = npcRoot.GetComponent<NpcHeadAnimationHandler>();
            yield return new WaitForSeconds(1f);

            while (true)
            {
                animHandler.PlayAnimation("S_WALK", "");
                Debug.Log("idle");
                yield return new WaitForSeconds(1f);

                animHeadHandler.StartLookAt(Camera.main!.transform);
                break;

                animHandler.PlayAnimation("T_DIALOGGESTURE_08", null);
                Debug.Log("8");
                yield return new WaitForSeconds(8f);
            }
        }

        private void BootstrapBloodwyn()
        {
            var newNpc = new GameObject("NPC");
            var loaderComp = newNpc.AddComponent<NpcLoader2>();
            newNpc.SetParent(NpcSlotGo);

            var npcSymbol = GameData.GothicVm.GetSymbolByName(_bloodwynInstanceId)!;
            _bloodwynInstance = GameData.GothicVm.AllocInstance<NpcInstance>(npcSymbol);

            var npcData = new NpcContainer2
            {
                Instance = _bloodwynInstance,
                Props = new()
                {
                    WalkMode = VmGothicEnums.WalkMode.Walk
                },
                Vob = new()
            };

            _bloodwynInstance.UserData = npcData;
            loaderComp.Npc = _bloodwynInstance;
            MultiTypeCache.NpcCache2.Add(npcData);

            newNpc.name = _bloodwynInstance.GetName(NpcNameSlot.Slot0);
            GameData.GothicVm.GlobalSelf = _bloodwynInstance;

            GameData.GothicVm.InitInstance(_bloodwynInstance);

            // Hero
            {
                // Need to be set for later usage (e.g. Bloodwyn checks your inventory if enough nuggets are carried)
                var heroInstance = GameData.GothicVm.InitInstance<NpcInstance>("hero");
                GameData.GothicVm.GlobalHero = heroInstance;
            }

            // We need to initialize the NPC at this frame to set the positions of child GOs now.
            GameGlobals.Npcs.InitNpc(newNpc, true);
            newNpc.transform.SetLocalPositionAndRotation(default, default);
            newNpc.transform.GetChild(0).SetLocalPositionAndRotation(default, default);

            // Otherwise NPC will start its daily routine.
            Destroy(newNpc.GetComponentInChildren<AiHandler>());
            newNpc.transform.GetChild(0).gameObject.AddComponent<LabAiHandler>();
        }
    }
}
