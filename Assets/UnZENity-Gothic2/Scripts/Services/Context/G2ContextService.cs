using GUZ.Core;
using GUZ.Core.Services.Context;
using ZenKit;

namespace GUZ.G2.Services.Context
{
    public class G2ContextService : IContextGameVersionService
    {
        public GameVersion Version => GameVersion.Gothic2;
        string IContextGameVersionService.RootPath => GameGlobals.Config.Root.Gothic2Path;
        public string CutsceneFileSuffix => "LSC";

        // FIXME - Load from GothicGame.ini
        public string InitialWorld => "newworld.zen";
    }
}
