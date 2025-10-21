//https://gist.github.com/jcdickinson/1933489
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace FastMath
{
    public class FastTrig
    {
        public const double PI = Math.PI;
        
        // Care when going over 512:
        // 512 (perfect NT page size) = 93ms.
        // 16384 = 90ms.
        // This contradicted what I just said - so I will leave it at 16384.
        private const int MaxCircleAngle = 16384; 

        private const int HalfMaxCircleAngle = MaxCircleAngle / 2;
        private const int QuarterMaxCircleAngle = MaxCircleAngle / 4;
        private const int MaskMaxCircleAngle = MaxCircleAngle - 1;
        private const double PiOverHalfCircleAngle = Math.PI / HalfMaxCircleAngle;
        private const float HalfMaxCircleOverPiSingle = HalfMaxCircleAngle / (float)Math.PI;
            
        private static readonly float[] _singleLookup = new float[MaxCircleAngle];

        static FastTrig()
        {
            for (var i = 0; i < MaxCircleAngle; i++)
            {
                _singleLookup[i] = (float)Math.Sin(i * PiOverHalfCircleAngle);
            }
        }

        public static float Cos(float radians)
        {
            var i = (int)(radians * HalfMaxCircleOverPiSingle);
            if (i < 0)
            {
                return _singleLookup[(QuarterMaxCircleAngle - i) & MaskMaxCircleAngle];
            }
            else
            {
                return _singleLookup[(QuarterMaxCircleAngle + i) & MaskMaxCircleAngle];
            }
        }

        public static float Sin(float radians)
        {
            var i = (int)(radians * HalfMaxCircleOverPiSingle);
            if (i < 0)
            {
                return _singleLookup[MaxCircleAngle - ((-i) & MaskMaxCircleAngle)];
            }
            else
            {
                return _singleLookup[i & MaskMaxCircleAngle];
            }
        }
    }
}