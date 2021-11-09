using System;
using System.Diagnostics;

namespace JamUp.TestUtility
{
    public struct TimeProfiler : IDisposable
    {
        public readonly struct ReadOut
        {
            public double Seconds { get; }
            public double Milliseconds { get; }
            public double Nanoseconds { get; }

            public ReadOut(Stopwatch stopwatch)
            {
                double ticks = stopwatch.ElapsedTicks;
                Seconds = ticks / Stopwatch.Frequency;
                Milliseconds = (ticks / Stopwatch.Frequency) * 1000;
                Nanoseconds = (ticks/ Stopwatch.Frequency) * 1000000000;
            }
        }
        
        private Stopwatch stopwatch;
        private Action<ReadOut> onStop;
        
        public TimeProfiler(Action<ReadOut> onStop)
        {
            stopwatch = new Stopwatch();
            this.onStop = onStop;
            Start();
        }

        public void Start()
        {
            stopwatch ??= new Stopwatch();
            stopwatch.Start();
        }

        public void Stop()
        {
            stopwatch.Stop();
            onStop.Invoke(new ReadOut(stopwatch));
        }
        
        public void Dispose()
        {
            Stop();            
        }
    }
}