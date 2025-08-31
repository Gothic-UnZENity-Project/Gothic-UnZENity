using System;
using System.Collections.Generic;
using System.Linq;
using MyBox;

namespace GUZ.Core.Adapters.UI.LoadingBars
{
    public class WorldLoadingBarHandler : AbstractLoadingBarHandler
    {
        public enum ProgressType
        {
            WorldMesh,
            VOB,
            Npc
        }
        
        public override List<string> GetProgressTypes()
        {
            return Enum.GetNames(typeof(ProgressType)).ToList();
        }

        protected override void SetMaterials()
        {
            base.SetMaterials();
            
            // On G1+world.zen it's either Gomez in his throne room (NewGame) or Gorn holding his Axe (LoadGame)
            var textureNameForLoadingScreen = GameGlobals.SaveGame.IsNewGame
                ? "LOADING.TGA"
                : $"LOADING_{GameGlobals.SaveGame.CurrentWorldName.ToUpper().RemoveEnd(".ZEN")}.TGA";
            GameGlobals.Textures.SetTexture(textureNameForLoadingScreen, GameGlobals.Textures.GothicLoadingMenuMaterial);
        }
    }
}
