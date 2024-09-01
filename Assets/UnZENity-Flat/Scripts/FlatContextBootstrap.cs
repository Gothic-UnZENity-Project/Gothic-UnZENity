using GUZ.Core.Context;

namespace GUZ.Flat
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class FlatContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterModule(GuzContext.Controls controls)
        {
            if (controls != GuzContext.Controls.Flat)
            {
                return;
            }

            GuzContext.InteractionAdapter = new FlatInteractionAdapter();
            GuzContext.DialogAdapter = null; // TBD
        }
    }
}
