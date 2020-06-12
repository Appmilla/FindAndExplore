using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;

namespace FindAndExplore.Core.DynamicData
{
    public static class CacheExtensions
    {
        public static void UpdateCache<TObject, TKey>(
            this SourceCache<TObject, TKey> sourceCache,
            IEnumerable<TObject> updatedResults,
            Func<TObject, TKey> keySelector)
        {
            if (sourceCache == null) throw new ArgumentNullException(nameof(sourceCache));

            if (updatedResults == null) throw new ArgumentNullException(nameof(updatedResults));

            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

            var cacheItems = updatedResults.ToList();
            if (!cacheItems.Any()) return;

            sourceCache.Edit(innerCache =>
            {
                var updatedKeys = cacheItems.Select(keySelector);

                var keysToRemove = innerCache.Keys.Where(key => !updatedKeys.Contains(key)).ToList();

                if (keysToRemove.Any()) innerCache.Remove(keysToRemove);

                innerCache.AddOrUpdate(cacheItems);
            });
        }
    }
}
