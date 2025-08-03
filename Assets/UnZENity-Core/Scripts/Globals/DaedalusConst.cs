using System.Collections.Generic;
using System.Linq;

namespace GUZ.Core.Globals
{
    public static class DaedalusConst
    {
        public static int AIVMMRealId => GameData.GothicVm.GetSymbolByName("AIV_MM_REAL_ID")!.GetInt(0);
        public static int AIVInvincibleKey => GameData.GothicVm.GetSymbolByName("AIV_INVINCIBLE")!.GetInt(0);
        public static int AIVItemStatusKey => GameData.GothicVm.GetSymbolByName("AIV_ITEMSTATUS")!.GetInt(0);
        public static int AIVItemFreqKey => GameData.GothicVm.GetSymbolByName("AIV_ITEMFREQ")!.GetInt(0);
        public static int TAITNone => GameData.GothicVm.GetSymbolByName("TA_IT_NONE")!.GetInt(0);
        
        
        public static string DoorLockSoundName => "DOOR_LOCK.WAV";
        public static string PickLockFailureSoundName => GameData.GothicVm.GetSymbolByName("_STR_SOUND_PICKLOCK_FAILURE").GetString(0);
        public static string PickLockBrokenSoundName => GameData.GothicVm.GetSymbolByName("_STR_SOUND_PICKLOCK_BROKEN").GetString(0);
        public static string PickLockSuccessSoundName => GameData.GothicVm.GetSymbolByName("_STR_SOUND_PICKLOCK_SUCCESS").GetString(0);
        public static string PickLockUnlockSoundName => GameData.GothicVm.GetSymbolByName("_STR_SOUND_PICKLOCK_UNLOCK").GetString(0);
        public static string DoorUnlockSoundName => "DOOR_UNLOCK.WAV"; // _STR_*_UNLOCK value above couldn't be found/isn't used in G1, therefore we use this as fallback.


        public static string[] MobSit => GameData.GothicVm.GetSymbolByName("MOB_SIT").GetString(0).Split(',');
        public static string[] MobLie => GameData.GothicVm.GetSymbolByName("MOB_LIE").GetString(0).Split(',');
        public static string[] MobClimb => GameData.GothicVm.GetSymbolByName("MOB_CLIMB").GetString(0).Split(',');
        public static string[] MobNotInterruptable => GameData.GothicVm.GetSymbolByName("MOB_NOTINTERRUPTABLE").GetString(0).Split(',');

        // TODO - Gothic2 has a different amount of talents
        public static int TalentsMax = GameData.GothicVm.GetSymbolByName("NPC_TALENT_MAX").GetInt(0);
        public static List<string> TalentTitles
        {
            get
            {
                var talents = GameData.GothicVm.GetSymbolByName("TXT_TALENTS");;
                return Enumerable.Range(0, TalentsMax).Select(i => talents.GetString((ushort)i)).ToList();
            }
        }

        public static List<string> TalentSkills
        {
            get
            {
                var talentSkills = GameData.GothicVm.GetSymbolByName("TXT_TALENTS_SKILLS");;
                var talentCount = TalentsMax;
                return Enumerable.Range(0, talentCount).Select(i => talentSkills.GetString((ushort)i)).ToList();
            }
        }

        public enum Guild
        {
            GIL_NONE = 0, // (keine)
            GIL_HUMAN = 1, // Special Guild -> To set Constants for ALL Human Guilds --> wird verwendet in Species.d
            GIL_PAL = 1, // Paladin
            GIL_MIL = 2, // Miliz
            GIL_VLK = 3, // Bürger
            GIL_KDF = 4, // Magier
            GIL_NOV = 5, // Magier Novize
            GIL_DJG = 6, // Drachenjäger
            GIL_SLD = 7, // Söldner
            GIL_BAU = 8, // Bauer
            GIL_BDT = 9, // Bandit
            GIL_STRF = 10, // Prisoner, Sträfling
            GIL_DMT = 11, // Dementoren
            GIL_OUT = 12, // Outlander (z.B. kleine Bauernhöfe)
            GIL_PIR = 13, // Pirat
            GIL_KDW = 14, // KDW
            GIL_EMPTY_D = 15, // NICHT VERWENDEN!

            //-----------------------------------------------
            GIL_PUBLIC = 15, // für öffentliche Portalräume

            //-----------------------------------------------
            GIL_SEPERATOR_HUM = 16,
            GIL_MEATBUG = 17,
            GIL_SHEEP = 18,
            GIL_GOBBO = 19, // Green Goblin / Black Goblin
            GIL_GOBBO_SKELETON = 20,
            GIL_SUMMONED_GOBBO_SKELETON = 21,
            GIL_SCAVENGER = 22, // (bei Bedarf) Scavenger / Evil Scavenger /OrcBiter
            GIL_GIANT_RAT = 23,
            GIL_GIANT_BUG = 24,
            GIL_BLOODFLY = 25,
            GIL_WARAN = 26, // Waren / Feuerwaran
            GIL_WOLF = 27, // Wolf / Warg
            GIL_SUMMONED_WOLF = 28,
            GIL_MINECRAWLER = 29, // Minecrawler / Minecrawler Warrior
            GIL_LURKER = 30,
            GIL_SKELETON = 31,
            GIL_SUMMONED_SKELETON = 32,
            GIL_SKELETON_MAGE = 33,
            GIL_ZOMBIE = 34,
            GIL_SNAPPER = 35, // Snapper / Dragon Snapper /Razor
            GIL_SHADOWBEAST = 36, //Shadowbeast / Bloodhound
            GIL_SHADOWBEAST_SKELETON = 37,
            GIL_HARPY = 38,
            GIL_STONEGOLEM = 39,
            GIL_FIREGOLEM = 40,
            GIL_ICEGOLEM = 41,
            GIL_SUMMONED_GOLEM = 42,
            GIL_DEMON = 43,
            GIL_SUMMONED_DEMON = 44,
            GIL_TROLL = 45, // Troll / Schwarzer Troll
            GIL_SWAMPSHARK = 46, // (bei Bedarf)
            GIL_DRAGON = 47, // Feuerdrache / Eisdrache / Felsdrache / Sumpfdrache / Untoter Drache
            GIL_MOLERAT = 48, // Molerat
            GIL_ALLIGATOR = 49,
            GIL_SWAMPGOLEM = 50,
            GIL_Stoneguardian = 51,
            GIL_Gargoyle = 52,
            GIL_Empty_A = 53,
            GIL_SummonedGuardian = 54,
            GIL_SummonedZombie = 55,
            GIL_EMPTY_B = 56,
            GIL_EMPTY_C = 57,
            GIL_SEPERATOR_ORC = 58, // (ehem. 37)
            GIL_ORC = 59, // Ork-Krieger / Ork-Shamane / Ork-Elite
            GIL_FRIENDLY_ORC = 60, // Ork-Sklave / Ur-Shak
            GIL_UNDEADORC = 61,
            GIL_DRACONIAN = 62,
            GIL_EMPTY_X = 63,
            GIL_EMPTY_Y = 64,
            GIL_EMPTY_Z = 65,
            GIL_MAX = 66
        }
    }
}
