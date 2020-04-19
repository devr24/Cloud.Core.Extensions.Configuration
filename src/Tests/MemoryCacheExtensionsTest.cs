using System;
using Cloud.Core.Testing;
using Xunit;
using System.Threading;
using Cloud.Core.Testing.Lorem;
using Microsoft.Extensions.Caching.Memory;

namespace Cloud.Core.Configuration.Tests
{
    [IsUnit]
    public class MemoryCacheExtensionsTest
    {
        [Fact]
        public void Test_MemoryCache_GetOrBuild()
        {
            IMemoryCache cache = new MemoryCache(new MemoryCacheOptions());

            var cacheMe = Lorem.GetParagraph();

            Assert.False(cache.Contains("key"));
            var cachedItem = cache.GetOrBuild("key", () => cacheMe, TimeSpan.FromSeconds(2));

            Assert.True(cache.Contains("key"));
            Assert.Equal(cachedItem, cacheMe);

            Thread.Sleep(3000);
            Assert.True(cache.Get("key") == null);
        }
    }
}
