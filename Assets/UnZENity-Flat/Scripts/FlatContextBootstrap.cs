﻿using GUZ.Core;
using GUZ.Core.Adapter;
using ZenKit;

namespace GUZ.Flat
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class FlatContextBootstrap : AbstractContextBootstrap
    {
        protected override void RegisterControlModule(GameContext.Controls controls)
        {
            if (controls != GameContext.Controls.Flat)
            {
                return;
            }

            GameContext.InteractionAdapter = new FlatInteractionAdapter();
            GameContext.DialogAdapter = null; // TBD
            GameContext.SubtitlesAdapter = null; // TBD
        }

        protected override void RegisterGameVersionModule(GameVersion version)
        {
            // NOP
        }
    }
}
