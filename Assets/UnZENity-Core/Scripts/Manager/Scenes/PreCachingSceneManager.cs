using System.Threading.Tasks;
using GUZ.Core.Creator;
using UnityEngine;
using ZenKit;

namespace GUZ.Core.Manager.Scenes
{
    public class PreCachingSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField] private GameObject _loadingArea;
        
        private static readonly string[] _gothic1Worlds = 
        {
            // FIXME reoder
            "Freemine.zen",
            "World.zen",
            "Oldmine.zen",
            "OrcGraveyard.zen",
            "OrcTempel.zen"
        };
        
        private static readonly string[] _gothic2Worlds =
        {
            "NewWorld.zen",
            "OldWorld.zen",
            "AddonWorld.zen",
            "DragonIsland.zen"
        };
        
        
        public void Init()
        {
            GameGlobals.Loading.InitLoading(_loadingArea);
            GameContext.InteractionAdapter.TeleportPlayerTo(_loadingArea.transform.position);
            
#pragma warning disable CS4014 // Do not wait. We want to update player movement (VR) and camera view (progress bar) 
            CreateCaches();
#pragma warning restore CS4014
        }

        private async Task CreateCaches()
        {
            var levelsToLoad = GameContext.GameVersionAdapter.Version == GameVersion.Gothic1 ? _gothic1Worlds : _gothic2Worlds;
            
            /*
             * Fetch all existing worlds (done)
             * Load world mesh of a level
             *  Sliced by lighting VOBs
             * Cache it
             *
             * Load static VOBs (man elements except Spot+Startpoint+Iems+[Elements with have no mesh]
             * Cache them
             */
            
            foreach (var level in levelsToLoad)
            {
                var worldData = ResourceLoader.TryGetWorld(level, GameContext.GameVersionAdapter.Version);
                var rootGo = new GameObject("World");
                
                await WorldCreator.CreateMesh(worldData, rootGo, GameGlobals.Loading);


                return;
            }
        }
    }
}
