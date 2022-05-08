namespace Microsoft.Extensions.Configuration
{
    using System.IO;
    using System;
    using System.Collections.Generic;
    using System.Linq;


    /// <summary>
    /// Class Configuration extensions.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds the kubernetes secrets config.  Reads from a directory where it takes the file name as the key (config property) and the
        /// value is the content within the file.
        /// </summary>
        /// <param name="builder">The configuration builder to bind to.</param>
        /// <param name="path">The path to the directory containing the secrets (defaults to "secrets").</param>
        /// <param name="optional">if set to <c>true</c>, ignore if does not exist [optional].</param>
        /// <returns>The configuration builder after config has been added.</returns>
        public static IConfigurationBuilder AddKubernetesSecrets(this IConfigurationBuilder builder, string path = null, bool optional = true)
        {
            if (path == null)
            {
                path = "/etc/secrets";
            }

            // Default return if we cant find this folder to avoid runtime errors.  Worst case scenario
            // is that the settings are not loaded from here.
            if (!Directory.Exists(path))
            {
                return builder;
            }

            return builder.AddKeyPerFile(path, optional);
        }

        /// <summary>
        /// Uses the desired default configurations.
        /// Builds configuration sources in the following order:
        /// - Kubernetes Secrets (looks in the "secrets" folder)
        /// - Environment variables
        /// - Command line arguments
        /// - Json file (appsettings.json, followed by appsettings.dev.json)
        /// Note: appsettings.dev.json WILL override appsettings.json file settings, as it is only to be used for dev scenarios.
        /// </summary>
        /// <param name="builder">The configuration builder to bind to.</param>
        /// <param name="appSettingsPath">The application settings path.</param>
        /// <param name="kubernetesSecretsPath">The K8S secrets path.</param>
        /// <returns>
        /// The configuration builder after config has been added.
        /// </returns>
        public static IConfigurationBuilder UseDefaultConfigs(this IConfigurationBuilder builder,
            string appSettingsPath = "appsettings.json", string kubernetesSecretsPath = null)
        {
            builder.AddKubernetesSecrets(kubernetesSecretsPath)
                .AddEnvironmentVariables()
                .AddCommandLine(Environment.GetCommandLineArgs());

            if (!string.IsNullOrEmpty(appSettingsPath))
            {
                builder.AddJsonFile(appSettingsPath, true);
            }

            var env = Environment.GetEnvironmentVariable("ENVIRONMENT");
            if (!string.IsNullOrEmpty(env))
            {
                builder.AddJsonFile($"appsettings.{env}.json", true, true);
            }

            return builder;
        }

        /// <summary>
        /// Gets the base config as a "section".
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>IConfigurationSection.</returns>
        public static IConfigurationSection GetBaseSection(this IConfiguration config)
        {
            var configBase = new ConfigurationBuilder();
            configBase.AddInMemoryCollection(config.GetChildren().Where(c => c.Value != null).Select(c => new KeyValuePair<string, string>("base:" + c.Key, c.Value)));
            return configBase.Build().GetSection("base");
        }

        /// <summary>
        /// Add key/value to config builder.
        /// </summary>
        /// <param name="builder">Builder to extend.</param>
        /// <param name="key">Key for value being added.</param>
        /// <param name="value">Value to add.</param>
        /// <returns>Builder with key/value added.</returns>
        public static IConfigurationBuilder AddValue(this IConfigurationBuilder builder, string key, string value)
        {
            return builder.AddInMemoryCollection(new List<KeyValuePair<string, string>> {
                new KeyValuePair<string, string>(key, value)
            });
        }

        /// <summary>
        /// Add enumerable list of config values.
        /// </summary>
        /// <param name="builder">Builder to extend.</param>
        /// <param name="values">List of values to add.</param>
        /// <returns>Builder with values added.</returns>
        public static IConfigurationBuilder AddValues(this IConfigurationBuilder builder, IDictionary<string, string> values)
        {
            return builder.AddInMemoryCollection(values);
        }

        /// <summary>
        /// Get all settings as a string representation.
        /// </summary>
        /// <param name="builder">Builder to extend.</param>
        /// <returns>String representation of settings.</returns>
        public static string GetAllSettingsAsString(this IConfigurationBuilder builder)
        {
            return builder.Build().GetAllSettingsAsString();
        }

        /// <summary>
        /// Binds the base section of the config to an actual class of type T.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns>IConfigurationSection.</returns>
        public static T BindBaseSection<T>(this IConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config), "Configuration must be set");

            var configBase = new ConfigurationBuilder();
            var items = new Dictionary<string, string>();

            foreach (var c in config.GetChildren().Where(c => c.Value != null))
            {
                // We replace "--" with ":" so as that is how we denote our sub items in key vault, it is therefore converted
                // into the colon so it can be split appropriately in the standard way.
                var key = c.Key.Replace("--", ":", StringComparison.InvariantCulture);

                var parts = key.Split('-');
                if (parts.Length > 1)
                {
                    var safeKey = $"base:{string.Join(string.Empty, parts)}";
                    items.Add(safeKey, c.Value);
                }
                items.Add($"base:{key}", c.Value);
            }

            configBase.AddInMemoryCollection(items);
            return configBase.Build().GetSection("base").Get<T>();
        }

        /// <summary>
        /// Extension to grab values from existing configs during the build process.
        /// </summary>
        /// <typeparam name="T">Type of config object being pulled.</typeparam>
        /// <param name="builder">The builder being extended.</param>
        /// <param name="key">The key for the config value to search for.</param>
        /// <returns>T config value.</returns>
        public static T GetValue<T>(this IConfigurationBuilder builder, string key)
        {
            return builder.Build().GetValue<T>(key);
        }

        /// <summary>
        /// Gets all key values as a list of KeyValuePairs.
        /// </summary>
        /// <param name="rootConfig">The root configuration to get flattened configuration for.</param>
        /// <param name="providersToSkip">Types pf IConfigProviders to skip.</param>
        /// <returns>KeyValuePair&lt;System.String, System.String&gt;[].</returns>
        public static KeyValuePair<string, string>[] GetAllSettings(this IConfiguration rootConfig, Type[] providersToSkip = null)
        {
            return rootConfig.InternalGetAllKeyValues(providersToSkip);
        }

        /// <summary>
        /// Gets value from config based on key.
        /// </summary>
        /// <param name="config">The configuration to get value from.</param>
        /// <param name="key">The unique key which holds the wanted value.</param>
        /// <param name="value">Out variable which returns found value if present.</param>
        /// <returns>bool, true or false depending on if the value associated with the key could be found.</returns>
        public static bool TryGetValue<T>(this IConfiguration config, string key, out T value)
        {
            if (key == null)
                key = "";

            value = config.GetValue<T>(key);
            if (value == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the configuration keys and values represented as a string.
        /// Provider name is also shown and if no settings exist within a provider, it is not appended to the string.
        /// </summary>
        /// <param name="rootConfig">The root configuration to generate a string for.</param>
        /// <param name="providersToSkip">Types pf IConfigProviders to skip.</param>
        /// <returns>System.String representation of the configuration.</returns>
        public static string GetAllSettingsAsString(this IConfiguration rootConfig, Type[] providersToSkip = null)
        {
            var keys = rootConfig.InternalGetAllKeyValues(providersToSkip, true).Select(s =>
            {
                // If this is the provider node, format appropriately.
                if (s.Key == "PROV")
                {
                    return s.Value;
                }

                // Otherwise, tab the KeyValue format and return.
                return $"   [{s.Key}]: {s.Value}";
            });

            // All keys are returned with a newline between each.
            return string.Join(Environment.NewLine, keys);
        }

        /// <summary>
        /// Override a configuration setting based on an environment variable's value. For example, if `envVarName` was 
        /// `ASPNETCORE_ENVIRONMENT`, and the `envVariableMatch` was `Development`, override the configuration key with 
        /// the name `configKey` with the value of `overrideValue`.
        /// Useful if you want to override a param in a development environment.
        /// </summary>
        /// <param name="config">ConfigurationBuilder</param>
        /// <param name="envVariableName">Environment variable to lookup.</param>
        /// <param name="envVariableMatch">The environment variable value to match on.</param>
        /// <param name="configKey">The config key you wish to override.</param>
        /// <param name="overrideValue">The config value you wish to set.</param>
        /// <returns>ConfigurationBuilder that has been updated.</returns>
        public static IConfigurationBuilder AddEnvironmentOverride(this IConfigurationBuilder config, string envVariableName, string envVariableMatch, string configKey, string overrideValue)
        {
            var env = Environment.GetEnvironmentVariable(envVariableName);
            if (env == envVariableMatch)
            {
                config.AddValue("JwtSecret", config.GetValue<string>("Jwt:Secret"));
            }
            return config;
        }

        /// <summary>
        /// Internal method for generating all key values.
        /// </summary>
        /// <param name="rootConfig">The root configuration to get flattened configuration for.</param>
        /// <param name="providersToSkip">Types pf IConfigProviders to skip.</param>
        /// <param name="includeProviders">if set to <c>true</c> [include providers] the provider nodes are added to the output (used for string geneartion).</param>
        /// <returns>KeyValuePair&lt;System.String, System.String&gt;[].</returns>
        private static KeyValuePair<string, string>[] InternalGetAllKeyValues(this IConfiguration rootConfig,
            Type[] providersToSkip = null, bool includeProviders = false)
        {
            var prov = new List<KeyValuePair<string, string>>();

            // If providers have been configured, then build the flattened list of settings.
            if (rootConfig is ConfigurationRoot configRoot && configRoot.Providers != null)
            {
                foreach (var provider in configRoot.Providers.Where(p => providersToSkip == null || !providersToSkip.Contains(p.GetType())))
                {
                    var settingKeys = GetKeyNames(new List<string>(), provider, null);

                    // If providers are to be included AND there are settings, add the provider node.
                    if (includeProviders && settingKeys.Count > 0)
                    {
                        prov.Add(new KeyValuePair<string, string>("PROV", $"{Environment.NewLine}{provider.GetType().Name} [{settingKeys.Count} setting(s)]"));
                    }

                    // Append each config value, using the keys to identity each.
                    foreach (var settingKey in settingKeys)
                    {
                        provider.TryGet(settingKey, out var val);
                        prov.Add(new KeyValuePair<string, string>(settingKey, val));
                    }
                }
            }
            return prov.ToArray();
        }

        /// <summary>
        /// Pulls the config from the provider, using the path passed in.
        /// </summary>
        /// <param name="keyList">The key list built with the unique keys.</param>
        /// <param name="provider">The provider to build config from.</param>
        /// <param name="path">The path to find the config for.</param>
        /// <returns>List&lt;System.String&gt; of all config keys.</returns>
        private static List<string> GetKeyNames(List<string> keyList, IConfigurationProvider provider, string path)
        {
            // Grab distinct keys to parse for children.
            var distinctKeys = provider.GetChildKeys(new List<string>(), path).Distinct();

            foreach (var key in distinctKeys)
            {
                // Full path of key.
                var newPath = (string.IsNullOrEmpty(path) ? null : path) +
                           (!string.IsNullOrEmpty(path) && !string.IsNullOrEmpty(key) ? ":" : null) +
                           (string.IsNullOrEmpty(key) ? null : key);

                // If there are children of this config node, then recursively call this method again, otherwise add to key path.
                var hasChildren = provider.GetChildKeys(new List<string>(), newPath).Any();
                if (hasChildren)
                {
                    AddKeys(keyList, newPath, provider);
                }
                else
                {
                    keyList.Add(newPath);
                }
            }

            return keyList;
        }

        /// <summary>
        /// Add a path to the list if it doesn't yet exist in the list
        /// </summary>
        /// <param name="keyList">The key list built with the unique keys.</param>
        /// <param name="newPath">The path to the build config</param>
        /// <param name="provider">The provider to build config from.</param>
        private static void AddKeys(List<string> keyList, string newPath, IConfigurationProvider provider)
        {
            if (!keyList.Contains(newPath)) // Ensure keys are unique before adding new key.
            {
                var kvConfigs = GetKeyNames(keyList, provider, newPath);
                foreach (var kv in kvConfigs)
                {
                    if (!keyList.Contains(kv))
                    {
                        keyList.Add(kv);
                    }
                }
            }
        }
    }
}
