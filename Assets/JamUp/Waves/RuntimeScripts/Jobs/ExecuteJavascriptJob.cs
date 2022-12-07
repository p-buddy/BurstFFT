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
            private GCHandle handle { get; }
            private NativeArray<byte> ByteCode { get; }
            private NativeList<CreateEntity.PackedFrame> FrameData { get; }
            private NativeList<int> FrameDataOffsets { get; }
            private NativeList<Animatable<WaveState>> WaveStates { get; }
            private NativeList<int> WaveStateOffsets { get; }
            private NativeList<float> RootFrequencies { get; }

            public Builder(string code, GCHandle handle, Allocator allocator = Allocator.Persistent)
            {
                this.handle = handle;
                byte[] bytes = Encoding.GetBytes(code);
                ByteCode = new NativeArray<byte>(bytes, allocator);
                FrameData = new NativeList<CreateEntity.PackedFrame>(allocator);
                FrameDataOffsets = new NativeList<int>(allocator);
                WaveStates = new NativeList<Animatable<WaveState>>(allocator);
                WaveStateOffsets = new NativeList<int>(allocator);
                RootFrequencies = new NativeList<float>(allocator);
            }

            public ExecuteJavascriptJob MakeJob() => new()
            {
                APIHandle = handle,
                CodeBytes = ByteCode,
                FrameData = FrameData,
                FrameDataOffsets = FrameDataOffsets,
                WaveStates = WaveStates,
                WaveStateOffsets = WaveStateOffsets,
                RootFrequencies = RootFrequencies
            };

            public void Dispose(JobHandle dependency)
            {
                ByteCode.Dispose(dependency);
                FrameData.Dispose(dependency);
                FrameDataOffsets.Dispose(dependency);
                WaveStates.Dispose(dependency);
                WaveStateOffsets.Dispose(dependency);
                RootFrequencies.Dispose(dependency);
            }

            public void Dispose() => Dispose(default);
        }
        
        public static readonly Encoding Encoding = Encoding.UTF8;
        public GCHandle APIHandle;
        public NativeList<CreateEntity.PackedFrame> FrameData;
        public NativeList<int> FrameDataOffsets;
        public NativeList<Animatable<WaveState>> WaveStates;
        public NativeList<int> WaveStateOffsets;
        public NativeList<float> RootFrequencies;

        public NativeArray<byte> CodeBytes;

        public void Execute()
        {
            Stopwatch watch = new Stopwatch();
            ThreadSafeAPI api = (ThreadSafeAPI)APIHandle.Target;
            api.Reset();
            
            watch.Start();
            string code = Encoding.GetString(CodeBytes.ToArray());

            watch.Start();
            ExecutionContext context = JsRunner.GetExecutionContext();
            watch.Stop();
            UnityEngine.Debug.Log(watch.ElapsedMilliseconds);
            
            watch.Start();
            context.ApplyAPI(api);
            watch.Stop();
            UnityEngine.Debug.Log(watch.ElapsedMilliseconds);
            
            watch.Start();
            context.Execute(code);
            watch.Stop();
            UnityEngine.Debug.Log(watch.ElapsedMilliseconds);
            /*
            foreach (Signal signal in api.Signals)
            {
                ProcessFrame(in signal);
            }*/
        }
        
        private void ProcessFrame(in Signal signal)
        {
            int frameCount = signal.Frames.Count;
            int totalWaveCount = 0;
            
            FrameData.SetCapacity(FrameData.Length + frameCount);
            
            for (int index = 0; index < frameCount; index++)
            {
                KeyFrame frame = signal.Frames[index];
                FrameData.Add(CreateEntity.PackedFrame.Pack(in frame));
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