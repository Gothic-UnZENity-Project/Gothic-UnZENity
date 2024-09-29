using ZenKit;

namespace GUZ.Core.Adapter
{
    public interface IGameVersionAdapter
    {
        GameVersion Version { get; }
        string RootPath { get; }
        
        /// <summary>
        /// Start world from GothicGame.ini
        /// </summary>
        string InitialWorld { get;  }
    }
}
