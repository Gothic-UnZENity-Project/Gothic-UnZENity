﻿using GUZ.Core;
using GUZ.Core.Adapter;
using ZenKit;

namespace GUZ.G2
{
    /// <summary>
    /// Bootstrap class which will register listener to set this module as Active if GameSettings.Controls match.
    /// </summary>
    public class G2ContextBootstrap : AbstractContextBootstrap

    {
        protected override void RegisterControlModule(GameContext.Controls controls)
        {
            // NOP
        }

        protected override void RegisterGameVersionModule(GameVersion version)
        {
            if (version != GameVersion.Gothic2)
            {
                return;
            }

            GameContext.GameVersionAdapter = new G2Adapter();
        }
    }
}
