using System;
using Cloud.Core.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.IO;
using System.Linq;
using Cloud.Core.Configuration.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Cloud.Core.Configuration.Tests
{
    public class ConfigurationExtensionsTest : IDisposable
    {
        private class TestSettings
        {
            public string TestKey1 { get; set; }
            public string TestKey2 { get; set; }
        }

        public ConfigurationExtensionsTest()
        {
            // Do "global" initialization here; Only called once.
            var currentDir = Directory.GetCurrentDirectory();

            // Method 1 - app settings.
            File.WriteAllText(Path.Combine(currentDir, "appsettings.json"), "{ \"TestKey1\":\"testVal1\", \"TestKey2\": { \"TestKey3\":\"testVal3\" } }");

            // Method 2 - Kubernetes Secrets simulation.
            File.WriteAllText(Path.Combine(currentDir, "TestKey2"), "testVal2");
        }

        [Fact, IsUnit]
        public void Test_UseKubernetesContainerConfig()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseKubernetesContainerConfig("appsettings.json", Directory.GetCurrentDirectory())
                .Build();

            var settings = configBuilder.Get<TestSettings>();

            Assert.True(configBuilder.GetAllSettings().Count(s => s.Key == "TestKey1")  == 1);
            Assert.NotNull(settings);
            Assert.NotNull(settings.TestKey1);
            Assert.NotNull(settings.TestKey2);
        }

        [Fact, IsUnit]
        public void Test_UseKubernetesContainerConfig_WrongPaths()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseKubernetesContainerConfig("madeUpSettings.json", "madeUpDir")
                .Build();

            var settings = configBuilder.Get<TestSettings>();

            Assert.NotNull(settings);
            Assert.Null(settings.TestKey1);
            Assert.Null(settings.TestKey2);
        }

        [Fact, IsUnit]
        public void Test_UseKubernetesContainerConfig_NoPaths()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseKubernetesContainerConfig(null)
                .Build();

            var configString = configBuilder.GetAllSettingsAsString();
            configString.Length.Should().BeGreaterThan(0);

            var configSkipped = configBuilder.GetAllSettingsAsString(new[] {typeof(EnvironmentVariablesConfigurationProvider)});
            Assert.True(configSkipped.Length != configString.Length);

            var settings = configBuilder.Get<TestSettings>();

            Assert.NotNull(settings);
            Assert.Null(settings.TestKey1);
            Assert.Null(settings.TestKey2);
        }

        public void Dispose()
        {
            // Do "global" teardown here; Only called once.
            var currentDir = Directory.GetCurrentDirectory();

            // Unset method 1 - app settings.
            File.Delete(Path.Combine(currentDir, "appsettings.json"));
            
            // Unset method 2 - Kubernetes Secrets simulation.
            File.Delete(Path.Combine(currentDir, "TestKey2"));
        }

    }
}
