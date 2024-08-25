using System;

namespace GUZ.Core.Vm
{
    public static class VmGothicEnums
    {
        public enum PerceptionType
        {
            Assessplayer = 1,
            Assessenemy = 2,
            Assessfighter = 3,
            Assessbody = 4,
            Assessitem = 5,
            Assessmurder = 6,
            Assessdefeat = 7,
            Assessdamage = 8,
            Assessothersdamage = 9,
            Assessthreat = 10,
            Assessremoveweapon = 11,
            Observeintruder = 12,
            Assessfightsound = 13,
            Assessquietsound = 14,
            Assesswarn = 15,
            Catchthief = 16,
            Assesstheft = 17,
            Assesscall = 18,
            Assesstalk = 19,
            Assessgivenitem = 20,
            Assessfakeguild = 21,
            Movemob = 22,
            Movenpc = 23,
            Drawweapon = 24,
            Observesuspect = 25,
            Npccommand = 26,
            Assessmagic = 27,
            Assessstopmagic = 28,
            Assesscaster = 29,
            Assesssurprise = 30,
            Assessenterroom = 31,
            Assessusemob = 32,
            Count
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
    }
}
