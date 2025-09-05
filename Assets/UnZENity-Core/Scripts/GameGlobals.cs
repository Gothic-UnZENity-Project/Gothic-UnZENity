using GUZ.Core.Manager;
using GUZ.Core.Services;
using GUZ.Core.Services.Culling;
using GUZ.Core.Services.Vobs;
using GUZ.Core.Services.World;

namespace GUZ.Core
{
    public static class GameGlobals
    {
        public static IGlobalDataProvider Instance;

        public static VobManager Vobs => Instance.Vobs;
    }
}
