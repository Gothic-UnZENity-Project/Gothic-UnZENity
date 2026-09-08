using System;
using System.Collections;
using Gothic.Core.Const;
using Gothic.Core.Models.Config;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

namespace Gothic.Tests.PlayMode
{
    public abstract class AbstractTest
    {
        protected const string ConfigResourceFolder = "DeveloperConfigs";

        protected Scene MainScene => SceneManager.GetActiveScene();

        // Both the VR and the Flat player scene are named "Player"; which one loads is driven by
        // DeveloperConfig.GameControls.
        protected Scene GeneralScene => SceneManager.GetSceneByName(Constants.ScenePlayer);
        
        private readonly InputTestFixture _inputSimulator = new ();
        protected Keyboard Keyboard { get; private set; }
        protected Mouse Mouse { get; private set; }

        /// <summary>
        /// Read a committed DeveloperConfig, e.g. to assert against the values a session was supposed to boot with.
        ///
        /// Read-only on purpose: Resources.Load hands out the shared asset instance, so writing to it dirties a
        /// tracked file on the developer's machine. Tests select their config by name instead. (ADR-0001 D5)
        /// </summary>
        protected DeveloperConfig GetConfiguration(string name)
        {
            var config = Resources.Load<DeveloperConfig>($"{ConfigResourceFolder}/{name}");

            if (config == null)
            {
                throw new ArgumentException($"DeveloperConfig >{name}< not found at " +
                                            $">Resources/{ConfigResourceFolder}/{name}<.");
            }

            return config;
        }

        protected IEnumerator PrepareTest()
        {
            _inputSimulator.Setup();
            Keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse = InputSystem.AddDevice<Mouse>();
            
            SceneManager.LoadScene($"Assets/Gothic-Core/Scenes/{Constants.SceneBootstrap}.unity");

            // Wait for 1 frame to successfully load Bootstrap scene.
            yield return null;
        }

        /// <summary>
        /// Removes the synthetic input devices again. Without it they pile up across fixtures within one Editor session.
        /// </summary>
        protected void CleanupTest()
        {
            _inputSimulator.TearDown();
            Keyboard = null;
            Mouse = null;
        }

        protected IEnumerator WaitForSceneLoaded(string sceneName, float timeoutSeconds = 30f)
        {
            var timeout = timeoutSeconds;
            while (!SceneManager.GetSceneByName(sceneName).IsValid())
            {
                timeout -= Time.deltaTime;
                if (timeout < 0)
                {
                    throw new TimeoutException(
                        $"Loading scene >{sceneName}< took longer than {timeoutSeconds}s. Are you stuck in a wrong scene?");
                }

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
