using System.Collections;
using GUZ.Core.Globals;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace GUZ.Tests.PlayMode
{
    public class VRGameTest : AbstractTest
    {
        [UnitySetUp]
        private IEnumerator SetUp()
        {
            yield return PrepareTest();
            Config.EnableMainMenu = false;

            EditorApplication.EnterPlaymode();
            yield return WaitForSceneLoaded(Constants.SceneGeneral);
        }
        
        
        [UnityTest]
        public IEnumerator GameLoadedTest()
        {
            // Assert
            Assert.That(Camera.main != null, "There is no main camera. Game might not be loaded correctly.");
            yield break;
        }

        [UnityTest]
        public IEnumerator TestGameplay()
        {
            // Arrange
            var playerGo = GameObject.FindWithTag(Constants.PlayerTag);
            var initialPos = playerGo.transform.position;

            // Act
            yield return PressButton(Keyboard.wKey, 3f);
            
            // Assert
            var newPos = playerGo.transform.position;
            
            Assert.AreNotEqual(initialPos, newPos, "Player didn't move.");
        }
    }
}
