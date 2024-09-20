﻿using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GUZ.Core.Manager.Scenes
{
    public class PlayerSceneManager : MonoBehaviour, ISceneManager
    {
        public void Init()
        {
            // TODO - Needed?
            // SceneManager.MoveGameObjectToScene(InteractionManager, _generalScene);

            GameContext.InteractionAdapter.CreatePlayerController(SceneManager.GetSceneByName(Constants.ScenePlayer));
            GameContext.InteractionAdapter.CreateVRDeviceSimulator();

            GameManager.I.LoadScene(Constants.SceneGameVersion);
        }
    }
}