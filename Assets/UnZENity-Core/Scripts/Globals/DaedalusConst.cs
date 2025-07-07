namespace GUZ.Core.Globals
{
    public static class DaedalusConst
    {
        public static int AIVMMRealId => GameData.GothicVm.GetSymbolByName("AIV_MM_REAL_ID")!.GetInt(0);
        public static int AIVInvincibleKey => GameData.GothicVm.GetSymbolByName("AIV_INVINCIBLE")!.GetInt(0);
        public static int AIVItemStatusKey => GameData.GothicVm.GetSymbolByName("AIV_ITEMSTATUS")!.GetInt(0);
        public static int AIVItemFreqKey => GameData.GothicVm.GetSymbolByName("AIV_ITEMFREQ")!.GetInt(0);
        public static int TAITNone => GameData.GothicVm.GetSymbolByName("TA_IT_NONE")!.GetInt(0);
    }
}
