using System;
using System.Collections.Generic;
using System.Linq;
using GUZ.Core.Caches;
using GUZ.Core.Data.Container;
using GUZ.Core.Extensions;
using GUZ.Core.Globals;
using GUZ.Core.Properties;
using GUZ.Core.Util;
using GUZ.Core.Vm;
using JetBrains.Annotations;
using UnityEngine;
using ZenKit.Daedalus;
using Logger = GUZ.Core.Util.Logger;

namespace GUZ.Core.Manager
{
    public static class NpcHelper
    {
        /// <summary>
        /// Ranges are in meter.
        ///
        /// FIXME - We should use PERC_ASSESSTALK range to leverage HVR's Grabbable hover and remote grab distance!
        /// </summary>
        public static readonly Dictionary<VmGothicEnums.PerceptionType, int> PerceptionRanges = new ();


        private const float _fpLookupDistance = 7f; // meter


        public static void Init()
        {
            // Perceptions
            var percInitSymbol = GameData.GothicVm.GetSymbolByName("InitPerceptions");
            if (percInitSymbol == null)
            {
                Logger.LogError("InitPerceptions symbol not found.", LogCat.Npc);
            }
            else
            {
                GameData.GothicVm.Call(percInitSymbol.Index);
            }
        }

        public static void ExtPErcSetRange(int perceptionId, int rangeInCm)
        {
            PerceptionRanges[(VmGothicEnums.PerceptionType)perceptionId] = rangeInCm / 100;
        }

        public static bool ExtIsMobAvailable(NpcInstance npcInstance, string vobName)
        {
            var npc = GetNpc(npcInstance);
            var vob = VobHelper.GetFreeInteractableWithin10M(npc.transform.position, vobName);

            return vob != null;
        }

        public static int ExtWldGetMobState(NpcInstance npcInstance, string scheme)
        {
            var npcGo = GetNpc(npcInstance);

            var prefabProps = npcInstance.GetUserData().PrefabProps;

            VobProperties vob;

            if (prefabProps.CurrentInteractable != null)
            {
                vob = prefabProps.CurrentInteractable.GetComponent<VobProperties>();
            }
            else
            {
                vob = VobHelper.GetFreeInteractableWithin10M(npcGo.transform.position, scheme);
            }

            if (vob == null || vob.VisualScheme != scheme)
            {
                return -1;
            }

            if (vob is VobInteractiveProperties interactiveVob)
            {
                return Math.Max(0, interactiveVob.InteractiveProperties.State);
            }

            return -1;
        }

        public static ItemInstance ExtNpcGetEquippedMeleeWeapon(NpcInstance npc)
        {
            var meleeWeapon = GetProperties(npc).EquippedItems
                .FirstOrDefault(i => i.MainFlag == (int)VmGothicEnums.ItemFlags.ItemKatNf);

            return meleeWeapon;
        }

        public static bool ExtNpcHasEquippedMeleeWeapon(NpcInstance npc)
        {
            return ExtNpcGetEquippedMeleeWeapon(npc) != null;
        }

        public static ItemInstance ExtNpcGetEquippedRangedWeapon(NpcInstance npc)
        {
            var rangedWeapon = GetProperties(npc).EquippedItems
                .FirstOrDefault(i => i.MainFlag == (int)VmGothicEnums.ItemFlags.ItemKatFf);

            return rangedWeapon;
        }

        public static bool ExtNpcHasEquippedRangedWeapon(NpcInstance npc)
        {
            return ExtNpcGetEquippedRangedWeapon(npc) != null;
        }

        public static bool ExtIsNpcOnFp(NpcInstance npc, string vobNamePart)
        {
            var freePoint = GetProperties(npc).CurrentFreePoint;

            if (freePoint == null)
            {
                return false;
            }

            return freePoint.Name.ContainsIgnoreCase(vobNamePart);
        }

        /// <summary>
        /// Returns true and sets VM.other, if NPC was found.
        ///
        /// Hint:
        /// As WldDetectNpc and WldDetectNpc seem to be the same logic except one parameter, we implement both in this function.
        /// </summary>
        public static bool ExtWldDetectNpcEx(NpcInstance npcInstance, int specificNpcIndex, int aiState, int guild,
            bool detectPlayer)
        {
            var npc = GetProperties(npcInstance);
            var npcPos = npcInstance.GetUserData().Go.transform.position;
            var npcVob = npcInstance.GetUserData().Vob;

            // FIXME - add range check based on perceiveAll's range (npc.sense_range)
            var foundNpc = MultiTypeCache.NpcCache
                .Where(i => i.Props != null) // ignore empty (safe check)
                .Where(i => i.Go != null) // ignore empty (safe check)
                .Where(i => i.Instance.Index != npcInstance.Index) // ignore self
                .Where(i => detectPlayer ||
                            i.Instance.Index !=
                            GameData.GothicVm.GlobalHero!.Index) // if we don't detect player, then skip it
                .Where(i => specificNpcIndex < 0 ||
                            specificNpcIndex == i.Instance.Index) // Specific NPC is found right now?
                .Where(i => aiState < 0 || npcVob.CurrentStateIndex == i.Vob.CurrentStateIndex)
                .Where(i => i.Instance.Guild == guild) // check guild
                .OrderBy(i => Vector3.Distance(i.Go.transform.position, npcPos)) // get nearest
                .FirstOrDefault();

            // without this Dialog box stops and breaks the entire NPC logic
            if (foundNpc == null)
            {
                return false;
            }

            // We need to set it, as there are calls where we immediately need _other_. e.g.:
            // if (Wld_DetectNpc(self, ...) && (Npc_GetDistToNpc(self, other)<HAI_DIST_SMALLTALK)
            if (foundNpc.Instance != null)
            {
                GameData.GothicVm.GlobalOther = foundNpc.Instance;
            }

            return foundNpc.Instance != null;
        }

        public static int ExtNpcHasItems(NpcInstance npc, uint itemId)
        {
            if (GetProperties(npc).Items.TryGetValue(itemId, out var amount))
            {
                return amount;
            }

            return 0;
        }

        public static int ExtNpcGetDistToWp(NpcInstance npc, string waypointName)
        {
            var npcGo = GetNpc(npc);
            var npcPos = npcGo.transform.position;

            var waypoint = WayNetHelper.GetWayNetPoint(waypointName);

            if (waypoint == null || !npcGo)
            {
                return int.MaxValue;
            }

            // *100 as Gothic metrics are in cm, not m.
            return (int)(Vector3.Distance(npcPos, waypoint.Position) * 100);
        }

        public static void ExtNpcClearInventory(NpcInstance npc)
        {
            var props = GetProperties(npc);
            props.Items.Clear();
        }

        public static int ExtNpcGetTalentSkill(NpcInstance npc, int skillId)
        {
            var props = GetProperties(npc);

            // FIXME - this is related to overlays for the npc's
            return 0;
        }

        public static int ExtNpcGetTalentValue(NpcInstance npc, int skillId)
        {
            return GetProperties(npc).Talents[(VmGothicEnums.Talent)skillId];
        }

        public static VmGothicEnums.Attitude GetPersonAttitude(NpcContainer self, NpcContainer other)
        {
            if (!self.PrefabProps.IsHero() && !other.PrefabProps.IsHero())
            {
                return GetGuildAttitude(self.Vob.GuildTrue, other.Vob.Guild);
            }

            NpcContainer npc = self.PrefabProps.IsHero() ? other : self;
            
            if(npc.Props.TempAttitude != npc.Props.Attitude)
            {
                return npc.Props.TempAttitude;
            }

            return npc.Props.Attitude;
        }

        public static VmGothicEnums.Attitude GetGuildAttitude(int selfGuild, int otherGuild)
        {
            return (VmGothicEnums.Attitude)GameData.GuildAttitudes[selfGuild * GameData.GuildCount + otherGuild];
        }

        [CanBeNull]
        private static GameObject GetNpc([CanBeNull] NpcInstance npc)
        {
            return npc.GetUserData().Go;
        }

        private static NpcContainer GetContainer(NpcInstance npc)
        {
            return npc.GetUserData();
        }

        private static NpcProperties GetProperties([CanBeNull] NpcInstance npc)
        {
            return npc?.GetUserData().Props;
        }

        // FIXME - CanSense is not separating between smell, hear, and see as of now. Please add functionality.
        public static bool CanSenseNpc(NpcInstance self, NpcInstance other, bool freeLOS)
        {
            var senseRange = (self.SensesRange / 100); // daedalus values are in cm, we need them in m
            var range = Vector3.Distance(other.GetUserData().Go.transform.position,
                self.GetUserData().Go.transform.position);
            if (range > senseRange)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
