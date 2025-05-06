namespace GUZ.VR
{
    public static class VRConstants
    {
        public static class IniNames
        {
            public const string SmoothSpectator = "smoothSpectator";
        }
        
        public const string IniSectionAccessibility = "UNZENITY_VR_ACCESSIBILITY"; // [UNZENITY_VR_ACCESSIBILITY]
        public const string IniSectionImmersion = "UNZENITY_VR_IMMERSION"; // [UNZENITY_VR_IMMERSION]

        
        public static float SpectatorSmoothingNone = 0.0f;
        public static float SpectatorSmoothingLow = 0.02f;
        public static float SpectatorSmoothingMedium = 0.08f;
        public static float SpectatorSmoothingHigh = 0.15f;
    }
}
