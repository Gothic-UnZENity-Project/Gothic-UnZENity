using GUZ.Core;
using GUZ.Core.Context;
using ZenKit;

namespace GUZ.G2
{
    public class G2Adapter : IGameVersionAdapter
    {
        public GameVersion Version => GameVersion.Gothic2;
        string IGameVersionAdapter.RootPath => GameGlobals.Settings.Gothic2Path;
    }
}
