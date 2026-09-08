using System;
using System.Collections;
using Gothic.Core.Const;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Gothic.Tests.PlayMode
{
    /// <summary>
    /// Smallest possible functional session: boot the game with the VR device simulator and prove the player
    /// reacts to keyboard input. Session semantics (one boot, ordered steps) come from <see cref="FunctionalTest"/>.
    /// </summary>
    public class VRGameTest : FunctionalTest
    {
        [UnityTest]
        [Order(1)]
        public IEnumerator GameLoadedTest()
        {
            Assert.That(Camera.main, Is.Not.Null, "There is no main camera. Game might not be loaded correctly.");
            yield break;
        }

        [UnityTest]
        [Order(2)]
        public IEnumerator TestGameplay()
        {
            // Arrange
            var playerGo = GameObject.FindWithTag(Constants.PlayerTag);
            Assert.That(playerGo, Is.Not.Null, $"No GameObject tagged >{Constants.PlayerTag}< found.");
            var initialPos = playerGo.transform.position;

            // Act
            yield return PressButton(Keyboard.wKey, 3f);
            
            // Assert
            var newPos = playerGo.transform.position;
            
            Assert.That(Math.Abs(initialPos.x - newPos.x) > 0.5f, "Player didn't move.");
        }
    }
}
