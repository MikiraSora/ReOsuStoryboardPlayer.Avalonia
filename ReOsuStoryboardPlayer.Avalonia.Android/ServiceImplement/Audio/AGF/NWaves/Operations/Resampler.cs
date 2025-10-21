using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NWaves.Filters.Base;
using NWaves.Filters.Fda;
using NWaves.Signals;
using FastMath;

namespace NWaves.Operations
{
    /// <summary>
    /// Represents signal resampler (sampling rate converter).
    /// </summary>
    public class Resampler
    {
        private static int s_SincLookupSize = 16384;
        private static int s_SincLookupHalfSize = s_SincLookupSize / 2;
        private static float[] s_SincLookup = new float[s_SincLookupSize];

        static Resampler()
        {
            InitSincCache();
        }
        
        private static void InitSincCache()
        {
            for (int i = 0; i < s_SincLookup.Length; i++)
            {
                float x = (i - s_SincLookupHalfSize) / (float)s_SincLookupHalfSize * RESAMPLE_WINDOW_SIZE;
                float t = (float)FastTrig.PI * x;
                s_SincLookup[i] = Math.Abs(x) > 1e-20 ? FastTrig.Sin(t) / t : 1.0f;
            }
        }

        public static float FastSinc(float x)
        {
            int idx = (int) ((x / RESAMPLE_WINDOW_SIZE + 1f) * (s_SincLookupHalfSize - 1));
            float val = s_SincLookup[idx];
            return val;
        }

        /// <summary>
        /// Gets or sets the order of lowpass anti-aliasing FIR filter 
        /// that will be created automatically if the filter is not specified explicitly. 
        /// By default, 101.
        /// </summary>
        public int MinResamplingFilterOrder { get; set; } = 101;

        /// <summary>
        /// Does interpolation of <paramref name="signal"/> followed by lowpass filtering.
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <param name="factor">Interpolation factor (e.g. factor=2 if 8000 Hz -> 16000 Hz)</param>
        /// <param name="filter">Lowpass anti-aliasing filter</param>
        public DiscreteSignal Interpolate(DiscreteSignal signal, int factor, FirFilter filter = null)
        {
            if (factor == 1)
            {
                return signal.Copy();
            }

            var output = new float[signal.Length * factor];

            var pos = 0;
            for (var i = 0; i < signal.Length; i++)
            {
                output[pos] = factor * signal[i];
                pos += factor;
            }

            var lpFilter = filter;

            if (filter is null)
            {
                var filterSize = factor > MinResamplingFilterOrder / 2 ?
                                 2 * factor + 1 :
                                 MinResamplingFilterOrder;

                lpFilter = new FirFilter(DesignFilter.FirWinLp(filterSize, 0.5f / factor));
            }

            return lpFilter.ApplyTo(new DiscreteSignal(signal.SamplingRate * factor, output));
        }

        /// <summary>
        /// Does decimation of <paramref name="signal"/> preceded by lowpass filtering.
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <param name="factor">Decimation factor (e.g. factor=2 if 16000 Hz -> 8000 Hz)</param>
        /// <param name="filter">Lowpass anti-aliasing filter</param>
        public DiscreteSignal Decimate(DiscreteSignal signal, int factor, FirFilter filter = null)
        {
            if (factor == 1)
            {
                return signal.Copy();
            }

            var filterSize = factor > MinResamplingFilterOrder / 2 ?
                             2 * factor + 1 :
                             MinResamplingFilterOrder;

            if (filter is null)
            {
                var lpFilter = new FirFilter(DesignFilter.FirWinLp(filterSize, 0.5f / factor));

                signal = lpFilter.ApplyTo(signal);
            }

            var output = new float[signal.Length / factor];

            var pos = 0;
            for (var i = 0; i < output.Length; i++)
            {
                output[i] = signal[pos];
                pos += factor;
            }

            return new DiscreteSignal(signal.SamplingRate / factor, output);
        }

        private const int RESAMPLE_WINDOW_SIZE = 15;
        /// <summary>
        /// Does band-limited resampling of <paramref name="signal"/>.
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <param name="newSamplingRate">Desired sampling rate</param>
        /// <param name="filter">Lowpass anti-aliasing filter</param>
        /// <param name="order">Order</param>
        public DiscreteSignal Resample(DiscreteSignal signal,
                                       int newSamplingRate,
                                       FirFilter filter = null,
                                       uint parallelThread = 1)
        {
            if (signal.SamplingRate == newSamplingRate)
            {
                return signal.Copy();
            }

            double g = (double)newSamplingRate / signal.SamplingRate;

            var input = signal.Samples;
            var output = new float[(int)(input.Length * g)];
            var times = new int[(int)(input.Length * g)];

            if (g < 1 && filter is null)
            {
                filter = new FirFilter(DesignFilter.FirWinLp(MinResamplingFilterOrder, g / 2));

                input = filter.ApplyTo(signal).Samples;
            }

            double step = 1 / g;

            var min = 0f;
            var max = 0f;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void calc(int n)
            {
                double x = n * step;

                for (var i = -RESAMPLE_WINDOW_SIZE; i < RESAMPLE_WINDOW_SIZE; i++)
                {
                    var j = (int)Math.Floor(x) - i;

                    if (j < 0 || j >= input.Length)
                    {
                        continue;
                    }

                    double t = x - j;
                    float w = 0.5f * (1.0f + FastTrig.Cos((float)(t / RESAMPLE_WINDOW_SIZE * FastTrig.PI)));    // Hann window
                    float sinc = FastSinc((float)t);                           // Sinc function
                    output[n] += w * sinc * input[j];

                    times[n] += 1;
                    min = Math.Min(min, (float)t);
                    max = Math.Max(max, (float)t);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void ave(int n)
            {
                //todo 虽然能修复刺音问题但为什们能修复呢小编也不知道呢但既然这样就可以修复那就不用多管闲事了.jpg
                output[n] = Math.Max(-0.9999999f, Math.Min(0.9999999f, output[n]));

                //output[n] = Math.Max(-1f, Math.Min(1f, output[n]));
            }

            if (parallelThread == 1)
            {
                var len = output.Length;
                for (int n = 0; n < len; n++)
                    calc(n);
                for (int n = 0; n < len; n++)
                    ave(n);
            }
            else
            {
                if (parallelThread == 0)
                {
                    ThreadPool.GetAvailableThreads(out var workerThreads, out _);
                    parallelThread = (uint)workerThreads;
                }

                Parallel.For(0, output.Length, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = (int)parallelThread,
                }, calc);
                //var b1 = output.Where(x => x > 1).ToArray();
               // var b2 = output.Where(x => x < -1).ToArray();
                Parallel.For(0, output.Length, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = (int)parallelThread,
                }, ave);
                //var c1 = output.Where(x => x > 1).ToArray();
                //var c2 = output.Where(x => x < -1).ToArray();
            }

            return new DiscreteSignal(newSamplingRate, output);
        }

        /// <summary>
        /// Does simple resampling of <paramref name="signal"/> (as the combination of interpolation and decimation).
        /// </summary>
        /// <param name="signal">Input signal</param>
        /// <param name="up">Interpolation factor</param>
        /// <param name="down">Decimation factor</param>
        /// <param name="filter">Lowpass anti-aliasing filter</param>
        public DiscreteSignal ResampleUpDown(DiscreteSignal signal, int up, int down, FirFilter filter = null)
        {
            if (up == down)
            {
                return signal.Copy();
            }

            var newSamplingRate = signal.SamplingRate * up / down;

            if (up > 20 && down > 20)
            {
                return Resample(signal, newSamplingRate, filter);
            }

            var output = new float[signal.Length * up];

            var pos = 0;
            for (var i = 0; i < signal.Length; i++)
            {
                output[pos] = up * signal[i];
                pos += up;
            }

            var lpFilter = filter;

            if (filter is null)
            {
                var factor = Math.Max(up, down);
                var filterSize = factor > MinResamplingFilterOrder / 2 ?
                                 8 * factor + 1 :
                                 MinResamplingFilterOrder;

                lpFilter = new FirFilter(DesignFilter.FirWinLp(filterSize, 0.5f / factor));
            }

            var upsampled = lpFilter.ApplyTo(new DiscreteSignal(signal.SamplingRate * up, output));

            output = new float[upsampled.Length / down];

            pos = 0;
            for (var i = 0; i < output.Length; i++)
            {
                output[i] = upsampled[pos];
                pos += down;
            }

            return new DiscreteSignal(newSamplingRate, output);
        }
    }
}
