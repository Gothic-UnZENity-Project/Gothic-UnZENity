using System;

namespace GUZ.Core.Vm
{
    public static class VmGothicEnums
    {
        public enum FightMode
        {
            None  = 0,
            Fists = 1,
            Melee = 2,
            Far   = 5,
            Magic = 7,
        }

        public enum PerceptionType
        {
            AssessPlayer = 1,
            AssessEnemy = 2,
            AssessFighter = 3,
            AssessBody = 4,
            AssessItem = 5,
            AssessMurder = 6,
            AssessDefeat = 7,
            AssessDamage = 8,
            AssessOthersDamage = 9,
            AssessThreat = 10,
            AssessRemoveWeapon = 11,
            ObserveIntruder = 12,
            AssessFightSound = 13,
            AssessQuietSound = 14,
            AssessWarn = 15,
            CatchThief = 16,
            AssessTheft = 17,
            AssessCall = 18,
            AssessTalk = 19,
            AssessGivenItem = 20,
            AssessFakeGuild = 21,
            MoveMob = 22,
            MoveNpc = 23,
            DrawWeapon = 24,
            ObserveSuspect = 25,
            NpcCommand = 26,
            AssessMagic = 27,
            AssesssTopMagic = 28,
            AssessCaster = 29,
            AssessSurprise = 30,
            AssessEnterRoom = 31,
            AssessUseMob = 32
        }

        public enum Attitude
        {
            Hostile  = 0,
            Angry    = 1,
            Neutral  = 2,
            Friendly = 3
        }

        public enum WalkMode
        {
            Run = 0,
            Walk = 1,
            Sneak = 2,
            Water = 4,
            Swim = 8,
            Dive = 16
        }

        public enum Talent
        {
            Unknown = 0,
            _1H = 1,
            _2H = 2,
            Bow = 3,
            Crossbow = 4,
            Picklock = 5,
            Mage = 7,
            Sneak = 8,
            Regenerate = 9,
            Firemaster = 10,
            Acrobat = 11,
            Pickpocket = 12,
            Smith = 13,
            Runes = 14,
            Alchemy = 15,
            Takeanimaltrophy = 16,
            Foreignlanguage = 17,
            Wispdetector = 18,
            C = 19,
            D = 20,
            E = 21,

            MaxG1 = 12,
            MaxG2 = 22
        }

        [Flags]
        public enum BodyState
        {
            // Interruptable Flags
            BsFlagInterruptable = 32768,
            BsFlagFreehands = 65536,

            // ******************************************
            // BodyStates / Overlays and Flags
            // ******************************************
            BsStand = 0 | BsFlagInterruptable | BsFlagFreehands,
            BsWalk = 1 | BsFlagInterruptable, // PointAt not possible
            BsSneak = 2 | BsFlagInterruptable,
            BsRun = 3, // PointAt not possible
            BsSprint = 4, // PointAt not possible
            BsSwim = 5,
            BsCrawl = 6,
            BsDive = 7,
            BsJump = 8,
            BsClimb = 9 | BsFlagInterruptable, // GE�NDERT!
            BsFall = 10,
            BsSit = 11 | BsFlagFreehands,
            BsLie = 12,
            BsInventory = 13,
            BsIteminteract = 14 | BsFlagInterruptable,
            BsMobinteract = 15,
            BsMobinteractInterrupt = 16 | BsFlagInterruptable,

            BsTakeitem = 17,
            BsDropitem = 18,
            BsThrowitem = 19,
            BsPickpocket = 20 | BsFlagInterruptable,

            BsStumble = 21,
            BsUnconscious = 22,
            BsDead = 23,

            BsAimnear = 24, // wird z.Zt nicht benutzt
            BsAimfar = 25, // d.h. Bogenschütze kann weiterschießen, auch wenn er geschlagen wird
            BsHit = 26 | BsFlagInterruptable,
            BsParade = 27,

            // Magic
            BsCasting = 28 | BsFlagInterruptable,
            BsPetrified = 29,
            BsControlling = 30 | BsFlagInterruptable,

            BsMax = 31,

            // Modifier / Overlays
            BsModHidden = 128,
            BsModDrunk = 256,
            BsModNuts = 512,
            BsModBurning = 1024,
            BsModControlled = 2048,
            BsModTransformed = 4096
        }

        [Flags]
        public enum ItemFlags
        {
            // Item categories
            ItemKatNone = 1 << 0, // misc
            ItemKatNf = 1 << 1, // melee weapons
            ItemKatFf = 1 << 2, // distant-combat weapons
            ItemKatMun = 1 << 3, // munition (->MultiSlot)
            ItemKatArmor = 1 << 4, // armor and helmets
            ItemKatFood = 1 << 5, // food (->MultiSlot)
            ItemKatDocs = 1 << 6, // documents
            ItemKatPotions = 1 << 7, // potions
            ItemKatLight = 1 << 8, // light sources
            ItemKatRune = 1 << 9, // runes and scrolls
            ItemKatMagic = 1 << 31, // rings and amulets
            ItemKatKeys = ItemKatNone,

            // Item flags
            ItemBurn = 1 << 10, // can be burnt (i.e. torch)
            ItemMission = 1 << 12, // mission item
            ItemMulti = 1 << 21, // is multi
            ItemTorch = 1 << 28, // use like a torch
            ItemThrow = 1 << 29, // item can be thrown

            // Item weapon flags
            ItemSwd = 1 << 14, // use like sword
            ItemAxe = 1 << 15, // use like axe
            Item2HdSwd = 1 << 16, // use like two handed weapon
            Item2HdAxe = 1 << 17, // use like two handed axe
            ItemBow = 1 << 19, // use like bow
            ItemCrossbow = 1 << 20, // use like crossbow
            ItemAmulet = 1 << 22, // use like amulet
            ItemRing = 1 << 11 // use like ring
        }

        public enum InvCats
        {
            InvWeapon = 1,
            InvArmor  = 2,
            InvRune   = 3,
            InvMagic  = 4,
            InvFood	  = 5,
            InvPotion = 6,
            InvDoc    = 7,
            InvMisc   = 8,
            InvCatMax = 9
        }

        public static InvCats ToInventoryCategory(this ItemFlags mainFlag)
        {
            switch (mainFlag) {
                case ItemFlags.ItemKatNf:
                case ItemFlags.ItemKatFf:
                case ItemFlags.ItemKatMun:
                    return InvCats.InvWeapon;
                case ItemFlags.ItemKatArmor:
                    return InvCats.InvArmor;
                case ItemFlags.ItemKatFood:
                    return InvCats.InvFood;
                case ItemFlags.ItemKatDocs:
                    return InvCats.InvDoc;
                case ItemFlags.ItemKatPotions:
                    return InvCats.InvPotion;
                case ItemFlags.ItemKatRune:
                    return InvCats.InvRune;
                case ItemFlags.ItemKatMagic:
                    return InvCats.InvMagic;
                // None and others
                default:
                    return InvCats.InvMisc;
            }
        }

        /// <summary>
        /// Used for INpc.FightMode
        /// </summary>
        public enum WeaponState
        {
            NoWeapon,
            Fist,
            W1H,
            W2H,
            Bow,
            CBow,
            Mage
        }

        public enum MoverState
        {
            Open,
            Opening,
            Closed,
            Closing
        }

        /// <summary>
        /// Seems like it's used inside Gothic to define if an object is "active" (Awake) or rendered only (DoAiOnly)
        /// </summary>
        public enum VobSleepMode
        {
            Sleeping = 0,
            Awake = 1,
            AwakeDoAiOnly = 2
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
            GIL_MAX = 66,

            // Gothic 1 guilds
            GIL_G1_NONE = GIL_NONE,
            GIL_G1_HUMAN = GIL_HUMAN,
            GIL_G1_EBR = 1, // Erzbaron
            GIL_G1_GRD = 2, // Gardist
            GIL_G1_STT = 3, // Schatten
            GIL_G1_KDF = GIL_KDF,
            GIL_G1_VLK = 5, // Buddler
            GIL_G1_KDW = 6, // Wassermagier
            GIL_G1_SLD = GIL_SLD,
            GIL_G1_ORG = 8, // Bandit
            GIL_G1_BAU = 9, // Bauer
            GIL_G1_SFB = 10, // Schuerfer
            GIL_G1_GUR = 11, // Guru
            GIL_G1_NOV = 12, // Novize
            GIL_G1_TPL = 13, // Templer
            GIL_G1_DMB = 14, // Darkmagic Xardas
            GIL_G1_BAB = 15, // Female
            GIL_G1_SEPERATOR_HUM = GIL_SEPERATOR_HUM,
            GIL_G1_WARAN = 17,
            GIL_G1_SLF = 18, // Sleeper
            GIL_G1_GOBBO = GIL_GOBBO,
            GIL_G1_TROLL = 20,
            GIL_G1_SNAPPER = 21,
            GIL_G1_MINECRAWLER = 22,
            GIL_G1_MEATBUG = 23,
            GIL_G1_SCAVENGER = 24,
            GIL_G1_DEMON = 25,
            GIL_G1_WOLF = 26,
            GIL_G1_SHADOWBEAST = 27,
            GIL_G1_BLOODFLY = 28,
            GIL_G1_SWAMPSHARK = 29,
            GIL_G1_ZOMBIE = 30,
            GIL_G1_UNDEADORC = 31,
            GIL_G1_SKELETON = 32,
            GIL_G1_ORCDOG = 33,
            GIL_G1_MOLERAT = 34,
            GIL_G1_GOLEM = 35,
            GIL_G1_LURKER = 36,
            GIL_G1_SEPERATOR_ORC = 37,
            GIL_G1_ORCSHAMAN = 38,
            GIL_G1_ORCWARROIR = 39,
            GIL_G1_ORCSCOUT = 40,
            GIL_G1_ORCSLAVE = 41,
            GIL_G1_MAX = 42,
        }
    }
}
