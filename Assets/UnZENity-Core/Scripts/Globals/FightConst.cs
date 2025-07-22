namespace GUZ.Core.Globals
{
    public static class FightConst
    {
        public static int FightAiMoveMax = GameData.FightVm.GetSymbolByName("MAX_MOVE")!.GetInt(0);
        
        public static class AttackActions
        {
            public static string EnemyPrehit = "FA_ENEMY_PREHIT_{0}";
            public static string EnemyStormPrehit = "FA_ENEMY_STORMPREHIT_{0}";
            public static string MyWCombo = "FA_MY_W_COMBO_{0}";
            public static string MyWRunto = "FA_MY_W_RUNTO_{0}";
            public static string MyWStrafe = "FA_MY_W_STRAFE_{0}";
            public static string MyWFocus = "FA_MY_W_FOCUS_{0}";
            public static string MyWNoFocus = "FA_MY_W_NOFOCUS_{0}";
            public static string MyGCombo = "FA_MY_G_COMBO_{0}";
            public static string MyGRunto = "FA_MY_G_RUNTO_{0}";
            public static string MyGStrafe = "FA_MY_G_STRAFE_{0}";
            public static string MyGFocus = "FA_MY_G_FOCUS_{0}";
            public static string MyFkFocus = "FA_MY_FK_FOCUS_{0}";
            public static string MyGFkNoFocus = "FA_MY_G_FK_NOFOCUS_{0}";
            public static string MyFkFocusFar = "FA_MY_FK_FOCUS_FAR_{0}";
            public static string MyFkNoFocusFar = "FA_MY_FK_NOFOCUS_FAR_{0}";
            public static string MyFkNofocusMag = "FA_MY_FK_FOCUS_MAG_{0}";
            public static string MyFkNoFocusMag = "FA_MY_FK_NOFOCUS_MAG_{0}";
        }
    }
}
