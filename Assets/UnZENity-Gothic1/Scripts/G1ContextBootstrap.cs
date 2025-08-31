using GUZ.Core;
using GUZ.Core._Adapter;
using ZenKit;

namespace GUZ.G1
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class G1ContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterGameVersionModule(GameVersion version)
        {
            if (version != GameVersion.Gothic1)
            {
                return;
            }

            GameContext.GameVersionAdapter = new G1Adapter();
        }
    }
}
