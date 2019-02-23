// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharingService.Data
{
    internal class MemoryAnchorCache : IAnchorKeyCache
    {
        /// <summary>
        /// The entry cache options.
        /// </summary>
        private static readonly MemoryCacheEntryOptions entryCacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromHours(48),
        };

        /// <summary>
        /// The memory cache.
        /// </summary>
        private readonly MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());

        /// <summary>
        /// The anchor numbering index.
        /// </summary>
        private long anchorNumberIndex = -1;

        /// <summary>
        /// Determines whether the cache contains the specified anchor identifier.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <returns>A <see cref="Task{System.Boolean}" /> containing true if the identifier is found; otherwise false.</returns>
        public Task<bool> ContainsAsync(long anchorId)
        {
            return Task.FromResult(this.memoryCache.TryGetValue(anchorId, out _));
        }

        /// <summary>
        /// Gets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorId">The anchor identifier.</param>
        /// <exception cref="KeyNotFoundException"></exception>
        /// <returns>The anchor key.</returns>
        public Task<string> GetAnchorKeyAsync(long anchorId)
        {
            if (this.memoryCache.TryGetValue(anchorId, out string anchorKey))
            {
                return Task.FromResult(anchorKey);
            }

            return Task.FromException<string>(new KeyNotFoundException($"The {nameof(anchorId)} {anchorId} could not be found."));
        }

        /// <summary>
        /// Gets the last anchor key asynchronously.
        /// </summary>
        /// <returns>The anchor key.</returns>
        public Task<string> GetLastAnchorKeyAsync()
        {
            if (this.anchorNumberIndex >= 0 && this.memoryCache.TryGetValue(this.anchorNumberIndex, out string anchorKey))
            {
                return Task.FromResult(anchorKey);
            }

            return Task.FromResult<string>(null);
        }

        /// <summary>
        /// Sets the anchor key asynchronously.
        /// </summary>
        /// <param name="anchorKey">The anchor key.</param>
        /// <returns>An <see cref="Task{System.Int64}" /> representing the anchor identifier.</returns>
        public Task<long> SetAnchorKeyAsync(string anchorKey)
        {
            if (this.anchorNumberIndex == long.MaxValue)
            {
                // Reset the anchor number index.
                this.anchorNumberIndex = -1;
            }

            long newAnchorNumberIndex = ++this.anchorNumberIndex;
            this.memoryCache.Set(newAnchorNumberIndex, anchorKey, entryCacheOptions);

            return Task.FromResult(newAnchorNumberIndex);
        }
    }
}
