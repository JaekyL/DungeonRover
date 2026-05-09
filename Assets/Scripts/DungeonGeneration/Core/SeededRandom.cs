using System;
using System.Collections.Generic;

namespace DungeonGeneration.Core
{
    /// <summary>
    /// Deterministic seeded random number generator.
    /// </summary>
    public class SeededRandom
    {
        public int Seed { get; }
        private readonly Random _random;
        public SeededRandom(int seed)
        {
            Seed = seed;
            _random = new Random(seed);
        }
        public int Next() => _random.Next();
        public int Next(int max) => _random.Next(max);
        public int Next(int min, int max) => _random.Next(min, max);
        public float NextFloat() => (float)_random.NextDouble();
        public float NextFloat(float min, float max) => min + (max - min) * NextFloat();
        public double NextDouble() => _random.NextDouble();
        public bool NextBool(float probability = 0.5f) => NextFloat() < probability;
        public T Choose<T>(IList<T> list)
        {
            if (list == null || list.Count == 0)
                throw new ArgumentException("List cannot be null or empty");
            return list[Next(list.Count)];
        }
        public T ChooseWeighted<T>(IList<T> items, IList<float> weights)
        {
            if (items.Count != weights.Count)
                throw new ArgumentException("Items and weights must have same count");
            float total = 0f;
            foreach (var w in weights) total += w;
            float roll = NextFloat() * total;
            float cumulative = 0f;
            for (int i = 0; i < items.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return items[i];
            }
            return items[items.Count - 1];
        }
        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        public SeededRandom Fork(string salt)
        {
            return new SeededRandom(Seed ^ salt.GetHashCode());
        }
    }
}
