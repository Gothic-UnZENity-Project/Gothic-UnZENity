using GUZ.Core;
using GUZ.Core.Adapter;
using ZenKit;

namespace GUZ.G1
{
    public class G1Adapter : IGameVersionAdapter
    {
        public GameVersion Version => GameVersion.Gothic1;
        string IGameVersionAdapter.RootPath => GameGlobals.Config.Root.Gothic1Path;

        // FIXME - Load from GothicGame.ini
        public string InitialWorld => "world.zen";
    }
}
