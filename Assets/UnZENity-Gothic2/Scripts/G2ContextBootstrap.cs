using GUZ.Core.Context;
using ZenKit;

namespace GUZ.G2
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class G2ContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterModule(GuzContext.Controls _, GameVersion version)
        {
            if (version != GameVersion.Gothic2)
            {
                return;
            }

            GuzContext.GameVersionAdapter = new G2Adapter();
        }
    }
}
