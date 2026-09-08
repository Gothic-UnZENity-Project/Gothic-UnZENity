using System;
using System.Collections;
using Gothic.Core.Const;
using Gothic.Core.Domain.Config;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using UnityEngine.TestTools;

namespace Gothic.Tests.PlayMode
{
    /// <summary>
    /// Base fixture for functional tests. (ADR-0001 D4)
    ///
    /// A session - not a single test - is the unit of isolation. Booting Gothic costs ~10 seconds with a warm
    /// static cache and ~45 without, so one fixture boots the game exactly once and its [UnityTest] methods are
    /// ordered steps within that single session.
    ///
    /// Two rules follow from that and both are enforced here:
    /// 1. Steps run in order. Give every step an [Order(n)]; NUnit orders alphabetically otherwise, which is not
    ///    what a playthrough means.
    /// 2. Once a step fails, the game state is no longer trustworthy. Every later step is reported inconclusive
    ///    instead of running against corrupt state and burying the actual cause under follow-up failures.
    ///
    /// The config a session boots with is selected by name via <see cref="DeveloperConfigLoader"/> - no fixture
    /// ever writes to a tracked DeveloperConfig asset. (ADR-0001 D5)
    /// </summary>
    public abstract class FunctionalTest : AbstractTest
    {
        /// <summary>
        /// DeveloperConfig below Resources/DeveloperConfigs this session boots with.
        /// </summary>
        protected virtual string ConfigName => "FunctionalTest";

        /// <summary>
        /// Scene whose existence means "the game is up". Sessions which stop at the main menu override this.
        /// </summary>
        protected virtual string BootedScene => Constants.ScenePlayer;

        /// <summary>
        /// Generous by design: a cold static cache turns a ~10s boot into ~45s, and a CI runner is slower still.
        /// This is a safety net against a hung boot, not a performance assertion.
        /// </summary>
        protected virtual float BootTimeoutSeconds => 180f;

        private bool _sessionStarted;
        private string _failedStep;
        private string _previousConfigOverride;


        [UnitySetUp]
        public IEnumerator SessionSetUp()
        {
            if (_failedStep != null)
            {
                Assert.Inconclusive($"Session aborted: step >{_failedStep}< failed. " +
                                    "Remaining steps would run against corrupt game state.");
            }

            // [UnitySetUp] is the only setup which may yield, but it is called for every single test.
            // Hence the guard: the session boots on the first step and every later step reuses it.
            if (_sessionStarted)
                yield break;
            _sessionStarted = true;

            _previousConfigOverride = Environment.GetEnvironmentVariable(DeveloperConfigLoader.OverrideEnvironmentVariable);
            Environment.SetEnvironmentVariable(DeveloperConfigLoader.OverrideEnvironmentVariable, ConfigName);

            yield return PrepareTest();
            yield return WaitForSceneLoaded(BootedScene, BootTimeoutSeconds);
        }

        [TearDown]
        public void StepTearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
                _failedStep ??= TestContext.CurrentContext.Test.Name;
        }

        [OneTimeTearDown]
        public void SessionTearDown()
        {
            if (!_sessionStarted)
                return;

            // Restore rather than clear: the value may come from the CI command line, which outlives this fixture.
            Environment.SetEnvironmentVariable(DeveloperConfigLoader.OverrideEnvironmentVariable, _previousConfigOverride);

            CleanupTest();
        }
    }
}
