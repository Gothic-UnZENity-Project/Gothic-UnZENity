using GUZ.Core.Context;
using ZenKit;

namespace GUZ.Flat
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class FlatContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterControlModule(GUZContext.Controls controls)
        {
            if (controls != GUZContext.Controls.Flat)
            {
                return;
            }

            GUZContext.InteractionAdapter = new FlatInteractionAdapter();
            GUZContext.DialogAdapter = null; // TBD
        }

        protected override void RegisterGameVersionModule(GameVersion version)
        {
            // NOP
        }
    }
}
