using GUZ.Core;
using GUZ.Core._Adapter;
using ZenKit;

namespace GUZ.Flat
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class FlatContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterGameVersionModule(GameVersion version)
        {
            // NOP
        }
    }
}
