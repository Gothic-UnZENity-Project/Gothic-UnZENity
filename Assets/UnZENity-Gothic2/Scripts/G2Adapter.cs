using GUZ.Core;
using GUZ.Core.Adapter;
using ZenKit;

namespace GUZ.G2
{
    public class G2Adapter : IGameVersionAdapter
    {
        public GameVersion Version => GameVersion.Gothic2;
        string IGameVersionAdapter.RootPath => GameGlobals.Config.Root.Gothic2Path;
        
        // FIXME - Load from GothicGame.ini
        public string InitialWorld => "newworld.zen";
    }
}
