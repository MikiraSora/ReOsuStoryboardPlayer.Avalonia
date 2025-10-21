using System;
using System.Collections.Generic;
using System.Text;

namespace NVorbis
{
    internal static class ArrayEx
    {
        public static T[] CopyPart<T>(T[] soruce, int offset, int length)
        {
            var newArr = new T[length];
            Array.Copy(soruce, offset, newArr, 0, length);
            return newArr;
        }
    }
}
