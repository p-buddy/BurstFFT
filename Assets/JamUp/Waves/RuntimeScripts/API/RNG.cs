using UnityEngine;
using Random = System.Random;

namespace JamUp.Waves.RuntimeScripts.API
{
    public readonly struct RNG
    {
        private readonly Random random;
        
        public RNG(int seed)
        {
            random = new Random(seed);
        }

        public int NextInt(int maxValue) => random.Next(maxValue);
    }
}