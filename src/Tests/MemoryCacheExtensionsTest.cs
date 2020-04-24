using System;
using Cloud.Core.Testing;
using Xunit;
using System.Threading;
using Cloud.Core.Testing.Lorem;
using Microsoft.Extensions.Caching.Memory;

namespace Cloud.Core.Extensions.Configuration.Tests
{
    [IsUnit]
    public class MemoryCacheExtensionsTest
    {
        /// <summary>Ensure the build method is called when the get does not return a value.</summary>
        [Fact]
        public void Test_MemoryCache_GetOrBuild()
        {
            // Arrange
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());
            var cacheMe = Lorem.GetParagraph();
            Assert.False(cache.Contains("key"));

            // Act
            var cachedItem = cache.GetOrBuild("key", () => cacheMe, TimeSpan.FromSeconds(2));

            // Assert
            Assert.True(cache.Contains("key"));
            Assert.Equal(cachedItem, cacheMe);
            Thread.Sleep(3000);
            Assert.True(cache.Get("key") == null);
        }
    }
}
