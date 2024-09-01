using System.Collections;
using GUZ.Core.Globals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GUZ.Tests.PlayMode
{
    public class ExampleTests
    {
        [OneTimeSetUp]
        protected void LoadScene()
        {
            SceneManager.LoadScene($"Assets/UnZENity-Core/Scenes/{Constants.SceneBootstrap}.unity");
        }
        
        [UnityTest]
        public IEnumerator TestCall()
        {
            // Load game
            EditorApplication.EnterPlaymode();
            
            // Wait for finished loading
            yield return WaitForSceneLoaded("General");
            
            // Check if main.Camera exists

            yield return null;
        }

        private IEnumerator WaitForSceneLoaded(string sceneName)
        {
            while (!SceneManager.GetSceneByName(sceneName).IsValid())
            {
                yield return null;
            }
        }
    }
}
