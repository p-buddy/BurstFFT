using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using JamUp.Waves.RuntimeScripts.API;
using pbuddy.TypeScriptingUtility.RuntimeScripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace JamUp.Waves.RuntimeScripts
{
    public struct ExecuteJavascriptJob: IJob /*, IThreadAPIData*/
    {
        public readonly struct Builder /*: IThreadAPIData, IDisposable*/
        {
            public ManagedResource<ThreadSafeAPI> api { get; }
            private NativeArray<byte> ByteCode { get; }
            public NativeList<CreateEntities.PackedFrame> FrameData { get; }
            public NativeList<int> FrameDataOffsets { get; }
            public NativeList<Animatable<WaveState>> WaveStates { get; }
            public NativeList<int> WaveStateOffsets { get; }
            public NativeList<float> RootFrequencies { get; }
            public NativeArray<float> ExecutionTime { get; }

            public Builder(string code, in ManagedResource<ThreadSafeAPI> api, Allocator allocator = Allocator.Persistent)
            {
                this.api = api;
                byte[] bytes = Encoding.GetBytes(code);
                ByteCode = new NativeArray<byte>(bytes, allocator);
                FrameData = new NativeList<CreateEntities.PackedFrame>(allocator);
                FrameDataOffsets = new NativeList<int>(allocator);
                WaveStates = new NativeList<Animatable<WaveState>>(allocator);
                WaveStateOffsets = new NativeList<int>(allocator);
                RootFrequencies = new NativeList<float>(allocator);
                ExecutionTime = new NativeArray<float>(1, allocator);
            }

            public ExecuteJavascriptJob MakeJob() => new()
            {
                API = api,
                CodeBytes = ByteCode,
                FrameData = FrameData,
                FrameDataOffsets = FrameDataOffsets,
                WaveStates = WaveStates,
                WaveStateOffsets = WaveStateOffsets,
                RootFrequencies = RootFrequencies,
                ExecutionTime = ExecutionTime
            };

            public void Dispose(JobHandle dependency)
            {
                ByteCode.Dispose(dependency);
                FrameData.Dispose(dependency);
                FrameDataOffsets.Dispose(dependency);
                WaveStates.Dispose(dependency);
                WaveStateOffsets.Dispose(dependency);
                RootFrequencies.Dispose(dependency);
                ExecutionTime.Dispose(dependency);
            }

            public void Dispose() => Dispose(default);
        }
        
        public static readonly Encoding Encoding = Encoding.UTF8;
        public ManagedResource<ThreadSafeAPI> API;
        public NativeList<CreateEntities.PackedFrame> FrameData;
        public NativeList<int> FrameDataOffsets;
        public NativeList<Animatable<WaveState>> WaveStates;
        public NativeList<int> WaveStateOffsets;
        public NativeList<float> RootFrequencies;
        
        public NativeArray<byte> CodeBytes;
        public NativeArray<float> ExecutionTime;

        public void Execute()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            
            ThreadSafeAPI api = API.Object;
            string code = Encoding.GetString(CodeBytes.ToArray());
            JsRunner.ExecuteString(code, context => context.ApplyAPI(api));
            foreach (Signal signal in api.Signals)
            {
                ProcessFrame(in signal);
            }
            api.Reset();
            watch.Stop();
            ExecutionTime[0] = watch.ElapsedMilliseconds / 1000f;
        }
        
        private void ProcessFrame(in Signal signal)
        {
            int frameCount = signal.Frames.Count;
            int totalWaveCount = 0;
            
            FrameData.SetCapacity(FrameData.Length + frameCount);
            
            for (int index = 0; index < frameCount; index++)
            {
                KeyFrame frame = signal.Frames[index];
                FrameData.Add(CreateEntities.PackedFrame.Pack(in frame));
                totalWaveCount += frame.Waves.Length;
            }
            
            FrameDataOffsets.Add(frameCount);
            WaveStateOffsets.Add(totalWaveCount);
            
            WaveStates.SetCapacity(WaveStates.Length + totalWaveCount);
            
            for (int index = 0; index < frameCount; index++)
            {
                Animatable<WaveState>[] states = signal.Frames[index].Waves;
                foreach (Animatable<WaveState> wave in states)
                {
                    WaveStates.Add(wave);
                }
            }
            
            RootFrequencies.Add(signal.RootFrequency);
        }
    }
}