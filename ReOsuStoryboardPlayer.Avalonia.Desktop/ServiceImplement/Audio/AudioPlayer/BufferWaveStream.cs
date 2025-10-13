﻿using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer
{
    internal class BufferWaveStream : WaveStream, IWaveProvider, ISampleProvider
    {
        private readonly byte[] waveBuffer;
        private readonly WaveFormat format;

        public BufferWaveStream(byte[] buffer, WaveFormat format)
        {
            waveBuffer = buffer;
            this.format = format;
        }

        public override WaveFormat WaveFormat => format;

        public override long Length => waveBuffer.LongLength;

        public override long Position { get; set; } = 0;

        public override int Read(byte[] buffer, int offset, int count)
        {
            var beforePosition = Position;
            for (int i = 0; i < count && Position < waveBuffer.Length; i++)
                buffer[offset + i] = waveBuffer[Position++];
            return (int)(Position - beforePosition);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var floatBuffer = MemoryMarshal.Cast<byte, float>(waveBuffer);

            var floatPosition = (int)(Position / sizeof(float));
            var floatLength = waveBuffer.Length / sizeof(float);

            var beforePosition = floatPosition;
            for (int i = 0; i < count && floatPosition < floatLength; i++)
                buffer[offset + i] = floatBuffer[floatPosition++];
            var read = floatPosition - beforePosition;
            Position = floatPosition * sizeof(float);
            return read;
        }
    }
}
