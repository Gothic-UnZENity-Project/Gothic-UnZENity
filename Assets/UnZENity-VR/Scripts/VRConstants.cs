namespace GUZ.VR
{
    public static class VRConstants
    {
        public static class IniNames
        {
            public const string MoveDirection = "moveDirection";
            public const string SmoothSpectator = "smoothSpectator";
            public static string RotationType = "rotationType";
            public static string SmoothRotationAmount = "smoothRotationAmount";
            public static string SnapRotationAmount = "snapRotationAmount";
        }
        
        public const string IniSectionAccessibility = "UNZENITY_VR_ACCESSIBILITY"; // [UNZENITY_VR_ACCESSIBILITY]
        public const string IniSectionImmersion = "UNZENITY_VR_IMMERSION"; // [UNZENITY_VR_IMMERSION]

        // 0...10 Ini values
        public const int SmoothTurnSettingAmount = 10;
        // e.g. Ini value of 3 == SmoothTurnSpeedPerSetting * 3
        public const int SmoothTurnSpeedPerSetting = 5;
        
        public static float SpectatorSmoothingNone = 0.0f;
        public static float SpectatorSmoothingLow = 0.02f;
        public static float SpectatorSmoothingMedium = 0.08f;
        public static float SpectatorSmoothingHigh = 0.15f;
    }
}
