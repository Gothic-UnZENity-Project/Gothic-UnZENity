using System.Collections;
using GUZ.Core;
using GUZ.Core.Globals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace GUZ.Tests.PlayMode
{
    public abstract class AbstractTest
    {
        protected Scene MainScene => SceneManager.GetActiveScene();
        protected Scene GeneralScene => SceneManager.GetSceneByName(Constants.SceneGeneral);
        
        protected GameConfiguration Config { get; private set;  }

        private readonly InputTestFixture _inputSimulator = new ();
        protected Keyboard Keyboard { get; private set; }
        protected Mouse Mouse { get; private set; }
        
        protected IEnumerator PrepareTest()
        {
            // Only UnitySetup which will be called with every test can handle coroutine-waits. Therefore check if Bootstrap.scene is already loaded.
            if (SceneManager.GetSceneByName(Constants.SceneBootstrap).IsValid())
            {
                yield break;
            }

            _inputSimulator.Setup();
            Keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse = InputSystem.AddDevice<Mouse>();
            
            SceneManager.LoadScene($"Assets/UnZENity-Core/Scenes/{Constants.SceneBootstrap}.unity");

            // Wait for 1 frame to successfully load Bootstrap scene.
            yield return null;

            var gameManager = SceneManager.GetActiveScene().GetRootGameObjects()[0]
                .GetComponentInChildren<GameManager>();
            gameManager.Config = Resources.Load<GameConfiguration>("GameConfigurations/Production");
            Config = gameManager.Config;
        }

        protected IEnumerator WaitForSceneLoaded(string sceneName)
        {
            while (!SceneManager.GetSceneByName(sceneName).IsValid())
            {
                yield return null;
            }

            // It's always a good idea to wait at least one additional frame. ;-)
            yield return null;
        }
        
        protected IEnumerator PressButton(KeyControl key, float time)
        {
            _inputSimulator.Press(key, time);
            yield return new WaitForSeconds(time);
        }
    }
}
