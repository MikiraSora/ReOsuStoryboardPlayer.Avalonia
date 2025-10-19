using NAudio.Wave;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer;

public class PrependSilenceProvider : ISampleProvider
{
    private readonly int silenceSamples;
    private readonly ISampleProvider source;
    private int position;

    public PrependSilenceProvider(ISampleProvider source, double leadInSeconds)
    {
        this.source = source;
        silenceSamples = (int) (source.WaveFormat.SampleRate * source.WaveFormat.Channels * leadInSeconds);
        WaveFormat = source.WaveFormat;
    }

    public WaveFormat WaveFormat { get; }

    public int Read(float[] buffer, int offset, int count)
    {
        var samplesWritten = 0;

        while (position < silenceSamples && samplesWritten < count)
        {
            buffer[offset + samplesWritten++] = 0f;
            position++;
        }

        if (samplesWritten < count)
        {
            var read = source.Read(buffer, offset + samplesWritten, count - samplesWritten);
            position += read;
            samplesWritten += read;
        }

        return samplesWritten;
    }
}