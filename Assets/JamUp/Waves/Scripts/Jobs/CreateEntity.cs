using JamUp.Waves.Scripts.API;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace JamUp.Waves.Scripts
{
    [BurstCompile]
    public struct CreateEntity : IJob
    {
        private const int MaxWaveCount = 10;
        private const float AttackTime = 0.1f;
        private const float ReleaseTime = 0.01f;

        [ReadOnly] public NativeArray<Entity> ExistingEntities;

        [ReadOnly] public NativeArray<PackedFrame> PackedFrames;

        [ReadOnly] public NativeArray<Animatable<WaveState>> Waves;

        public float TimeNow;

        public int Index;

        public EntityCommandBuffer ECB;

        public EntityArchetype EntityArchetype;

        [ReadOnly] public ComponentDataFromEntity<CurrentTimeFrame> TimeFrameForEntity;
        [ReadOnly] public ComponentDataFromEntity<CurrentWaveCount> WaveCountForEntity;
        [ReadOnly] public ComponentDataFromEntity<CurrentProjection> ProjectionForEntity;
        [ReadOnly] public ComponentDataFromEntity<CurrentSignalLength> SignalLengthForEntity;
        [ReadOnly] public ComponentDataFromEntity<CurrentSampleRate> SampleRateForEntity;
        [ReadOnly] public ComponentDataFromEntity<CurrentThickness> ThicknessForEntity;
        [ReadOnly] public BufferFromEntity<CurrentWavesElement> WavesForEntity;
        [ReadOnly] public BufferFromEntity<CurrentWaveAxes> AxesForEntity;

        // Assigned in Execute
        private bool useExisting;
        private Entity entity;
        private float interpolant;

        public void Execute()
        {
            useExisting = Index < ExistingEntities.Length;
            entity = useExisting ? ExistingEntities[Index] : ECB.CreateEntity(EntityArchetype);
            interpolant = useExisting ? TimeFrameForEntity[entity].Interpolant(TimeNow) : 0f;

            if (useExisting)
            {
                ECB.RemoveComponent<UpdateRequired>(entity);
                NativeArray<CurrentWavesElement> currentWaves = WavesForEntity[entity].AsNativeArray();
                NativeArray<CurrentWaveAxes> currentWaveAxes = AxesForEntity[entity].AsNativeArray();

                for (int i = 0; i < currentWaves.Length; i++)
                {
                    AllWavesElement lerp = AllWavesElement.FromLerp(interpolant, currentWaves[i], currentWaveAxes[i]);
                    ECB.AppendToBuffer(entity, lerp);
                }
            }
            else
            {
                Init<CurrentWavesElement>(MaxWaveCount);
            }

            int frameCount = PackedFrames.Length;
            int elementsToAdd = frameCount + 2; // all frames, plus 'sustain' and 'release'
            int capacity = 1 + elementsToAdd; // prepend one frame for 'attack'

            Init<DurationElement>(capacity).Append(new DurationElement(AttackTime));
            Init<WaveCountElement>(capacity)
                .Append(new WaveCountElement
                {
                    Value = useExisting ? WaveCountForEntity[entity].Value : 0
                });

            Init<ProjectionElement>(capacity).Append(First<ProjectionElement, CurrentProjection>(ProjectionForEntity));
            Init<SignalLengthElement>(capacity)
                .Append(First<SignalLengthElement, CurrentSignalLength>(SignalLengthForEntity));
            Init<SampleRateElement>(capacity).Append(First<SampleRateElement, CurrentSampleRate>(SampleRateForEntity));
            Init<ThicknessElement>(capacity).Append(First<ThicknessElement, CurrentThickness>(ThicknessForEntity));

            CommandBufferForEntity entityCommandBuffer = new()
            {
                CommandBuffer = ECB,
                Entity = entity
            };

            int accumulatedWaveCounts = 0;
            for (int index = 0; index < elementsToAdd; index++)
            {
                bool isRelease = index >= frameCount;
                bool isFinal = index > frameCount;
                PackedFrame frame = !isRelease
                    ? PackedFrames[index]
                    : isFinal
                        ? PackedFrames[frameCount - 1] // nothing, modifications will be to waves
                        : PackedFrames[frameCount - 1].DefaultAnimations(ReleaseTime);

                int waveCount = frame.WaveCount;
                entityCommandBuffer.Append(new WaveCountElement { Value = waveCount });
                entityCommandBuffer.Append(new DurationElement(frame.Duration));

                entityCommandBuffer.AppendAnimationElement<ProjectionElement>(frame.ProjectionType);
                entityCommandBuffer.AppendAnimationElement<SignalLengthElement>(frame.SignalLength);
                entityCommandBuffer.AppendAnimationElement<SampleRateElement>(frame.SampleRate);
                entityCommandBuffer.AppendAnimationElement<ThicknessElement>(frame.Thickness);

                entityCommandBuffer.AppendWaveElements(Waves, accumulatedWaveCounts, waveCount);

                accumulatedWaveCounts += waveCount;
            }
        }

        private BufferHelper<TBufferElement> Init<TBufferElement>(int capacity)
            where TBufferElement : struct, IBufferElementData
        {
            ECB.SetBuffer<TBufferElement>(entity).EnsureCapacity(capacity);
            return new BufferHelper<TBufferElement>
            {
                CBfE = new()
                {
                    Entity = entity,
                    CommandBuffer = ECB,
                }
            };
        }

        private TBufferElement First<TBufferElement, TComponent>(ComponentDataFromEntity<TComponent> dataFromEntity)
            where TBufferElement : struct, IBufferElementData, IValueSettable<float>, IAnimatableSettable
            where TComponent : struct, IComponentData, IValuable<Animation<float>>
        {
            float value = useExisting ? dataFromEntity[entity].Value.Lerp(interpolant) : default;
            return new TBufferElement
            {
                Value = value,
                AnimationCurve = AnimationCurve.Linear
            };
        }
        
        public struct PackedFrame
        {
            public float Duration;
            public int WaveCount;
            public Animatable<float> ProjectionType;
            public Animatable<float> SampleRate;
            public Animatable<float> SignalLength;
            public Animatable<float> Thickness;

            public PackedFrame DefaultAnimations(float duration)
            {
                Duration = duration;
                ProjectionType = new Animatable<float>(ProjectionType.Value);
                SampleRate = new Animatable<float>(SampleRate.Value);
                SignalLength = new Animatable<float>(SignalLength.Value);
                Thickness = new Animatable<float>(Thickness.Value);
                return this;
            }
            
            public static PackedFrame Pack(in KeyFrame frame) => new()
            {
                Duration = frame.Duration,
                WaveCount = frame.Waves.Length,
                ProjectionType = new Animatable<float>((int)frame.ProjectionType.Value, frame.ProjectionType.AnimationCurve),
                SampleRate = new Animatable<float>(frame.SampleRate.Value, frame.SampleRate.AnimationCurve),
                SignalLength = frame.SignalLength,
                Thickness = frame.Thickness
            };
        };
        
        private struct CommandBufferForEntity
        {
            public EntityCommandBuffer CommandBuffer;
            public Entity Entity;
            public void Append<TBufferElement>(TBufferElement element) where TBufferElement : struct, IBufferElementData
                => CommandBuffer.AppendToBuffer(Entity, element);

            public void AppendAnimationElement<TBufferElement>(Animatable<float> property)
                where TBufferElement : struct, IBufferElementData, IAnimatableSettable, IValueSettable<float> 
                => Append(new TBufferElement { Value = property.Value, AnimationCurve = property.AnimationCurve });

            public void AppendWaveElements(NativeArray<Animatable<WaveState>> waves, int index, int count)
            {
                for (int i = index; i < count; i++)
                {
                    Animatable<WaveState> waveState = waves[i];
                    Append(waveState.Value.AsWaveElement(waveState.AnimationCurve));
                }
            }
        }

        private struct BufferHelper<TBufferElement> where TBufferElement : struct, IBufferElementData
        {
            public CommandBufferForEntity CBfE;
            public void Append(TBufferElement element) => CBfE.Append(element);
        }
    }
}
