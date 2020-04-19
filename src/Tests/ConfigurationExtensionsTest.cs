using System;
using System.Collections.Generic;
using Cloud.Core.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Cloud.Core.Configuration.Tests
{
    [IsUnit]
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
            File.WriteAllText(Path.Combine(currentDir, "appsettings.json"), "{\"TestKey1\":\"testVal1\", \"TestKey2\": { \"TestKey3\":\"testVal3\" } }");

            // Method 2 - Kubernetes Secrets simulation.
            File.WriteAllText(Path.Combine(currentDir, "TestKey2"), "testVal2");
        }

        [Fact]
        public void Test_ConfigBuilder_GetValue()
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("testKey", "testVal")
                });
            
            // Act
            var lookupResult = configBuilder.GetValue<string>("testKey");
            
            // Assert
            lookupResult.Should().Be("testVal");
        }

        [Fact]
        public void Test_UseDefaultConfigs()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("appsettings.json", Directory.GetCurrentDirectory())
                .Build();

            var settings = configBuilder.Get<TestSettings>();

            Assert.True(configBuilder.GetAllSettings().Count(s => s.Key == "TestKey1")  == 1);
            Assert.NotNull(settings);
            Assert.NotNull(settings.TestKey1);
            Assert.NotNull(settings.TestKey2);
        }

        [Fact]
        public void Test_TryGetValueFromConfig()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("appsettings.json", Directory.GetCurrentDirectory())
                .Build();

            Assert.True(configBuilder.TryGetValue("TestKey1", out string value));

            Assert.True(value == "testVal1");
        }

        [Fact]
        public void Test_FailToGetValueFromConfigWithWrongKey()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("appsettings.json", Directory.GetCurrentDirectory())
                .Build();

            Assert.False(configBuilder.TryGetValue("WrongKey", out string value));

            Assert.True(value == null);
        }


        [Fact]
        public void Test_FailToGetValueFromConfigWithNullKey()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("appsettings.json", Directory.GetCurrentDirectory())
                .Build();

            Assert.False(configBuilder.TryGetValue(null, out string value));

            Assert.True(value == null);
        }

        [Fact]
        public void Test_UseDefaultConfigs_WrongPaths()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("madeUpSettings.json", "madeUpDir")
                .Build();

            var settings = configBuilder.Get<TestSettings>();

            Assert.NotNull(settings);
            Assert.Null(settings.TestKey1);
            Assert.Null(settings.TestKey2);
        }

        [Fact]
        public void Test_UseDefaultConfigs_NoPaths()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs(null)
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

        [Fact]
        public void Test_ConfigBuilder_AsString()
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddValue("testKey", "testVal");

            // Act
            var str = configBuilder.GetAllSettingsAsString();

            // Assert
            str.Length.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Test_ConfigBuilder_AddValueExtension()
        {
            // Arrange
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddValue("testKey", "testVal");

            // Act
            var lookupResult = configBuilder.GetValue<string>("testKey");

            // Assert
            lookupResult.Should().Be("testVal");
        }

        [Fact]
        public void Test_ConfigBuilder_AddValuesExtension()
        {
            // Arrange
            var configs = new List<KeyValuePair<string, string>> { 
                new KeyValuePair<string, string>("testKey", "testVal"),
                new KeyValuePair<string, string>("testKey1", "testVal1"),
                new KeyValuePair<string, string>("testKey2", "testVal2"),
            };
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddValues(configs);

            // Act
            var lookupResult = configBuilder.GetValue<string>("testKey");
            var lookupResult1 = configBuilder.GetValue<string>("testKey1");
            var lookupResult2 = configBuilder.GetValue<string>("testKey2");

            // Assert
            lookupResult.Should().Be("testVal");
            lookupResult1.Should().Be("testVal1");
            lookupResult2.Should().Be("testVal2");
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
