using System;
using System.Collections.Generic;
using Cloud.Core.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Cloud.Core.Extensions.Configuration.Tests
{
    [IsUnit]
    public class ConfigurationExtensionsTest : IDisposable
    {
        public ConfigurationExtensionsTest()
        {
            var currentDir = Directory.GetCurrentDirectory();

            // Method 1 - app settings.
            File.WriteAllText(Path.Combine(currentDir, "appsettings.json"), "{\"TestKey1\":\"testVal1\", \"TestKey2\": { \"TestKey3\":\"testVal3\" } }");

            // Method 2 - Kubernetes Secrets simulation.
            File.WriteAllText(Path.Combine(currentDir, "TestKey2"), "testVal2");
        }

        /// <summary>Ensure GetValue on the builder, returns the value as expected.</summary>
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

        /// <summary>Ensure using default configs loads all expected values.</summary>
        [Fact]
        public void Test_ConfigBuilder_UseDefaultConfigs()
        {
            // Arrange
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("appsettings.json", Directory.GetCurrentDirectory())
                .Build();
            
            var settings = configBuilder.Get<TestSettings>();
            
            // Act/Assert
            Assert.True(configBuilder.GetAllSettings().Count(s => s.Key == "TestKey1")  == 1);
            Assert.NotNull(settings);
            Assert.NotNull(settings.TestKey1);
            Assert.NotNull(settings.TestKey2);
        }

        /// <summary>Ensure TryGet returns expected value.</summary>
        [Fact]
        public void Test_ConfigBuilder_TryGetValueFromConfig()
        {
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("appsettings.json", Directory.GetCurrentDirectory())
                .Build();

            // Act/Assert
            Assert.True(configBuilder.TryGetValue("TestKey1", out string value));
            Assert.True(value == "testVal1");
        }

        /// <summary>Ensure wrong keys dont return values.</summary>
        [Fact]
        public void Test_ConfigBuilder_FailToGetValueFromConfigWithWrongKey()
        {
            // Arrange
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("appsettings.json", Directory.GetCurrentDirectory())
                .Build();

            // Act/Assert
            Assert.False(configBuilder.TryGetValue("WrongKey", out string value));
            Assert.True(value == null);
        }

        /// <summary>Ensure trying to get a value using a null key returns false for TryGet.</summary>
        [Fact]
        public void Test_ConfigBuilder_FailToGetValueFromConfigWithNullKey()
        {
            // Arrange
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("appsettings.json", Directory.GetCurrentDirectory())
                .Build();

            // Act/Assert
            Assert.False(configBuilder.TryGetValue(null, out string value));
            Assert.True(value == null);
        }

        /// <summary>Ensure Use Default Builder with wrong paths still sets up the config with the right path.</summary>
        [Fact]
        public void Test_ConfigBuilder_UseDefaultConfigsWrongPaths()
        {
            // Arrange
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs("madeUpSettings.json", "madeUpDir")
                .Build();

            // Act
            var settings = configBuilder.Get<TestSettings>();

            // Assert
            Assert.NotNull(settings);
            Assert.Null(settings.TestKey1);
            Assert.Null(settings.TestKey2);
        }

        /// <summary>Ensure using default config with no extra paths sets everything up as expected.</summary>
        [Fact]
        public void Test_ConfigBuilder_UseDefaultConfigsNoPaths()
        {
            // Arrange
            IConfiguration configBuilder = new ConfigurationBuilder()
                .UseDefaultConfigs(null)
                .Build();

            // Act
            var configString = configBuilder.GetAllSettingsAsString();
            var configSkipped = configBuilder.GetAllSettingsAsString(new[] {typeof(EnvironmentVariablesConfigurationProvider)});
            var settings = configBuilder.Get<TestSettings>();

            // Assert
            Assert.True(configSkipped.Length != configString.Length);
            configString.Length.Should().BeGreaterThan(0);
            Assert.NotNull(settings);
            Assert.Null(settings.TestKey1);
            Assert.Null(settings.TestKey2);
        }

        /// <summary>Ensure builder converts to string as expected.</summary>
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

        /// <summary>Ensure add value, adds as expected.</summary>
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

        /// <summary>Ensure add multiple values, adds as expected.</summary>
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

        private class TestSettings
        {
            public string TestKey1 { get; set; }
            public string TestKey2 { get; set; }
        }

    }
}
