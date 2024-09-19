using GUZ.Core.Context;
using ZenKit;

namespace GUZ.G1
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class G1ContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterControlModule(GUZContext.Controls controls)
        {
            // NOP
        }

        protected override void RegisterGameVersionModule(GameVersion version)
        {
            if (version != GameVersion.Gothic1)
            {
                return;
            }

            GUZContext.GameVersionAdapter = new G1Adapter();
        }
    }
}
