using ZenKit;

namespace GUZ.Core.Context
{
    public interface IGameVersionAdapter
    {
        GameVersion Version { get; }
        string RootPath { get; }
    }
}
