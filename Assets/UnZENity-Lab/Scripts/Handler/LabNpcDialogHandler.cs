using System.Collections;
using GUZ.Core;
using GUZ.Core._Npc2;
using GUZ.Core.Animations;
using GUZ.Core.Caches;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Npc;
using GUZ.Core.Properties;
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
            var npcRoot = NpcSlotGo.transform.GetChild(0).GetChild(0).gameObject;
            var animSystem = npcRoot.GetComponent<AnimationSystem>();
            var animHeadHandler = npcRoot.GetComponent<NpcHeadAnimationHandler>();

            // For UseItemToState animations
            var props = npcRoot.GetComponentInParent<NpcLoader2>().Npc.GetUserData2().Props;
            var beerSymbol = GameData.GothicVm.GetSymbolByName("ItFoBeer");
            props.CurrentItem = beerSymbol!.Index;

            yield return new WaitForSeconds(1f);

            while (true)
            {
                // Leg shake with FPS:10
                {
                    animSystem.PlayAnimation("t_BoringKick");
                    yield return new WaitForSeconds(1f);
                }
                continue;

                // Rotate, then idle
                {
                    animSystem.PlayAnimation("T_RUNTURNL");
                    yield return new WaitForSeconds(2f);
                    animSystem.StopAnimation("T_RUNTURNL");
                    animSystem.PlayAnimation("S_WALK");
                    yield return new WaitForSeconds(1f);
                    animSystem.StopAnimation("S_WALK");
                    continue;
                }

                animSystem.PlayAnimation("t_Potion_Stand_2_S0");
                yield return new WaitForSeconds(5f);
                animSystem.PlayAnimation("t_Potion_Random_3");
                yield return new WaitForSeconds(5f);
                animSystem.PlayAnimation("t_Potion_S0_2_Stand");
                yield return new WaitForSeconds(5f);
                continue;


                animSystem.PlayAnimation("T_DIALOGGESTURE_01");
                yield return new WaitForSeconds(2f);
                animSystem.StopAnimation("T_DIALOGGESTURE_01");
                animSystem.PlayAnimation("T_DIALOGGESTURE_02");
                yield return new WaitForSeconds(2f);

                continue;

                animSystem.PlayAnimation("S_WALK");
                Debug.Log("idle");
                yield return new WaitForSeconds(2f);

                // Test Layer2-blendout automation
                animSystem.PlayAnimation("T_DIALOGGESTURE_01");
                yield return new WaitForSeconds(3f);

                // Test start-stop
                animSystem.PlayAnimation("T_DIALOGGESTURE_08");
                yield return new WaitForSeconds(3f);
                animSystem.StopAnimation("T_DIALOGGESTURE_08");
                yield return new WaitForSeconds(1f);

                // Change idle state (layer 1)
                animSystem.PlayAnimation("S_RUN");
                yield return new WaitForSeconds(2f);
                // animHeadHandler.StartLookAt(Camera.main!.transform);
                continue;

                animSystem.PlayAnimation("T_DIALOGGESTURE_08");
                Debug.Log("8");
                yield return new WaitForSeconds(8f);
            }
        }

        private void BootstrapBloodwyn()
        {
            var newNpc = new GameObject();
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

            newNpc.name = "Dialog NPC";
        }
    }
}
