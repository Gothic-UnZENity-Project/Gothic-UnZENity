using UnityEngine;

namespace GUZ.Core.Manager.Scenes
{
    public class LoadingSceneManager : MonoBehaviour, ISceneManager
    {
        [SerializeField] private GameObject _loadingArea;
        
        public void Init()
        {
            GameGlobals.Loading.InitLoading(_loadingArea);
            GameContext.InteractionAdapter.TeleportPlayerTo(_loadingArea.transform.position);
            
            GlobalEventDispatcher.LoadingSceneLoaded.Invoke();

            // Start loading world!
            GameManager.I.LoadScene(SaveGameManager.CurrentWorldName);

        }
    }
}
