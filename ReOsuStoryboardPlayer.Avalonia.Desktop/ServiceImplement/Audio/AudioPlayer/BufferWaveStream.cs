using System.Runtime.InteropServices;
using NAudio.Wave;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer;

internal class BufferWaveStream : WaveStream, IWaveProvider, ISampleProvider
{
    private readonly WaveFormat format;
    private readonly byte[] waveBuffer;

    public BufferWaveStream(byte[] buffer, WaveFormat format)
    {
        waveBuffer = buffer;
        this.format = format;
    }

    public override long Length => waveBuffer.LongLength;

    public override long Position { get; set; }

    public int Read(float[] buffer, int offset, int count)
    {
        var floatBuffer = MemoryMarshal.Cast<byte, float>(waveBuffer);

        var floatPosition = (int) (Position / sizeof(float));
        var floatLength = waveBuffer.Length / sizeof(float);

        var beforePosition = floatPosition;
        for (var i = 0; i < count && floatPosition < floatLength; i++)
            buffer[offset + i] = floatBuffer[floatPosition++];
        var read = floatPosition - beforePosition;
        Position = floatPosition * sizeof(float);
        return read;
    }

    public override WaveFormat WaveFormat => format;

    public override int Read(byte[] buffer, int offset, int count)
    {
        var beforePosition = Position;
        for (var i = 0; i < count && Position < waveBuffer.Length; i++)
            buffer[offset + i] = waveBuffer[Position++];
        return (int) (Position - beforePosition);
    }
}