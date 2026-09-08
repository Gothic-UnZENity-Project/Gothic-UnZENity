using System;
using Gothic.Core.Logging;
using Gothic.Core.Models.Config;
using MyBox;
using UnityEngine;
using Logger = Gothic.Core.Logging.Logger;

namespace Gothic.Core.Domain.Config
{
    /// <summary>
    /// Resolves which <see cref="DeveloperConfig"/> a session actually boots with.
    ///
    /// By default this is the asset wired into the Bootstrap scene. Automated runs override it by name so they
    /// never have to mutate the tracked Production.asset on a developer machine. (ADR-0001 D5)
    ///
    /// Two equivalent ways to set the override:
    /// - Command line: >unity run ... -- -gothicTestConfig FunctionalTest< (or -batchmode -gothicTestConfig ...)
    /// - Environment variable: >GOTHIC_TEST_CONFIG=FunctionalTest< - the only option for a PlayMode test running
    ///   inside an already started Editor, which can no longer influence its own command line.
    ///
    /// The name is a path below Resources/DeveloperConfigs, so nested assets work too (e.g. >Local/Diego<).
    /// </summary>
    public static class DeveloperConfigLoader
    {
        public const string OverrideArgument = "-gothicTestConfig";
        public const string OverrideEnvironmentVariable = "GOTHIC_TEST_CONFIG";

        private const string _resourceFolder = "DeveloperConfigs";


        /// <summary>
        /// Returns the overridden config if one is requested, otherwise the fallback provided by the caller
        /// (i.e. GameManager's or LabManager's inspector slot).
        /// </summary>
        public static DeveloperConfig Load(DeveloperConfig fallback)
        {
            var overrideName = GetOverrideName();

            if (overrideName.IsNullOrEmpty())
                return fallback;

            var config = Resources.Load<DeveloperConfig>($"{_resourceFolder}/{overrideName}");

            // Failing loudly is the whole point: a run which silently continues with the fallback config tests
            // something other than what was asked for.
            if (config == null)
            {
                throw new ArgumentException(
                    $"DeveloperConfig >{overrideName}< requested via {OverrideArgument}/{OverrideEnvironmentVariable} " +
                    $"not found at >Resources/{_resourceFolder}/{overrideName}<.");
            }

            Logger.Log($"DeveloperConfig overridden with >{overrideName}<.", LogCat.Loading);

            return config;
        }

        /// <summary>
        /// Command line wins over the environment variable, so a CI invocation can't be shadowed by a stale
        /// variable on the runner.
        /// </summary>
        public static string GetOverrideName()
        {
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == OverrideArgument)
                    return args[i + 1];
            }

            return Environment.GetEnvironmentVariable(OverrideEnvironmentVariable);
        }
    }
}
