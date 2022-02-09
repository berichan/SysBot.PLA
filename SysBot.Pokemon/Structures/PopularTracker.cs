using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon
{
    public class PopularTracker<T> : Dictionary<T, int>
    {
        public string Description { get; }

        public PopularTracker(string description)
            :base()
        {
            Description = description;
        }

        public void AddEntryCount(T value, int toAdd = 1)
        {
            if (ContainsKey(value))
                this[value] = this[value] + toAdd;
            else
                Add(value, 1);
        }

        public Dictionary<T, int> GetMostPopular(int truncateCount)
        {
            var ordered = this
                .OrderByDescending(x => x.Value)
                .Take(truncateCount)
                .ToDictionary(x => x.Key, x => x.Value);

            return ordered;
        }
    }
}
