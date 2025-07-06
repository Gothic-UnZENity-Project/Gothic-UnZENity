namespace GUZ.Core.Globals
{
    public static class DaedalusConst
    {
        public static int AIVMMRealId => GameData.GothicVm.GetSymbolByName("AIV_MM_REAL_ID")!.GetInt(0);
        public static int AIVInvincibleKey => GameData.GothicVm.GetSymbolByName("AIV_INVINCIBLE")!.GetInt(0);
        public static int AIVItemStatusKey => GameData.GothicVm.GetSymbolByName("AIV_ITEMSTATUS")!.GetInt(0);
        public static int AIVItemFreqKey => GameData.GothicVm.GetSymbolByName("AIV_ITEMFREQ")!.GetInt(0);
        public static int TAITNone => GameData.GothicVm.GetSymbolByName("TA_IT_NONE")!.GetInt(0);
            
        public static int InvCatWeapon = GameData.GothicVm.GetSymbolByName("INV_WEAPON")!.GetInt(0);
        public static int InvCatArmor = GameData.GothicVm.GetSymbolByName("INV_ARMOR")!.GetInt(0);
        public static int InvCatRune = GameData.GothicVm.GetSymbolByName("INV_RUNE")!.GetInt(0);
        public static int InvCatMagic = GameData.GothicVm.GetSymbolByName("INV_MAGIC")!.GetInt(0);
        public static int InvCatFood = GameData.GothicVm.GetSymbolByName("INV_FOOD")!.GetInt(0);
        public static int InvCatPotion = GameData.GothicVm.GetSymbolByName("INV_POTION")!.GetInt(0);
        public static int InvCatDoc = GameData.GothicVm.GetSymbolByName("INV_DOC")!.GetInt(0);
        public static int InvCatMisc = GameData.GothicVm.GetSymbolByName("INV_MISC")!.GetInt(0);
        public static int InvCatMax = GameData.GothicVm.GetSymbolByName("INV_CAT_MAX")!.GetInt(0);
    }
}
