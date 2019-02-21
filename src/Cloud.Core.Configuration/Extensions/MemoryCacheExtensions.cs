namespace Microsoft.Extensions.Caching.Memory
{
    using System;

    public static class MemoryCacheExtensions
    {
        /// <summary>
        /// Gets the or build.
        /// </summary>
        /// <typeparam name="T">Type of cached object.</typeparam>
        /// <param name="cache">The cache to search.</param>
        /// <param name="key">The key of the object to find.</param>
        /// <param name="buildAction">The build action - executes when building item (when not in cache).</param>
        /// <param name="expiryTime">The expiry duration of the cached object.</param>
        /// <returns>Found object T.</returns>
        public static T GetOrBuild<T>(this IMemoryCache cache, object key, Func<T> buildAction, TimeSpan expiryTime)
        {
            // Get the object from cache.
            cache.TryGetValue<T>(key, out var cacheEntry);

            // If not found in cache, build.
            if (cacheEntry == null)
            {
                cacheEntry = buildAction();

                // Set cache options.
                var cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(expiryTime);

                // Save data in cache.
                cache.Set(key, cacheEntry, cacheEntryOptions);
            }

            // Return cached object.
            return cacheEntry;
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="cache">The cache to search.</param>
        /// <param name="key">The key of the object to find.</param>
        /// <returns><c>true</c> if [contains] [the specified key]; otherwise, <c>false</c>.</returns>
        public static bool Contains(this IMemoryCache cache, object key)
        {
            return cache.Get(key) != null;
        }
    }
}
