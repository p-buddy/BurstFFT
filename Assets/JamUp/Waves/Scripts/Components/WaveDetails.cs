using Unity.Entities;
using Unity.Mathematics;

namespace JamUp.Waves.Scripts
{
    public struct WaveDetails: IComponentData, IAnimatable
    {
        public int2 NumberOfWaves { get; }
        
        public int StartingNumberOfWaves => NumberOfWaves.x;
        public int EndingNumberOfWaves => NumberOfWaves.y;
        
        public AnimationCurve Animation { get; }

        public WaveDetails(int startingCount, int endingCount, AnimationCurve animation)
        {
            NumberOfWaves = new int2(startingCount, endingCount);
            Animation = animation;
        }
    }
}