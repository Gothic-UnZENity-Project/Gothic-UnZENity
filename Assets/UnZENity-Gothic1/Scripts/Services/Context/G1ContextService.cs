using GUZ.Core;
using GUZ.Core.Services.Context;
using ZenKit;

namespace GUZ.G1
{
    public class G1ContextService : IContextGameVersionService
    {
        public GameVersion Version => GameVersion.Gothic1;
        string IContextGameVersionService.RootPath => GameGlobals.Config.Root.Gothic1Path;
        public string CutsceneFileSuffix => "CSL";

        // FIXME - Load from GothicGame.ini
        public string InitialWorld => "world.zen";
    }
}
