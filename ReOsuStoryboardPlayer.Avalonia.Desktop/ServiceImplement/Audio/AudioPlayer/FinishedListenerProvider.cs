using Microsoft.VisualBasic;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReOsuStoryboardPlayer.Avalonia.Desktop.ServiceImplement.Audio.AudioPlayer
{
    internal class FinishedListenerProvider : ISampleProvider
    {
        private ISampleProvider provider;
        public ISampleProvider Provider => provider;
        public WaveFormat WaveFormat => provider.WaveFormat;
        private bool enableEventFire;

        public event Action OnReturnEmptySamples;

        public FinishedListenerProvider(ISampleProvider provider)
        {
            this.provider = provider;
        }

        public void StartListen()
        {
            enableEventFire = true;
        }

        public void StopListen()
        {
            enableEventFire = false;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var read = Provider.Read(buffer, offset, count);
            if (read < count && enableEventFire)
                OnReturnEmptySamples?.Invoke();

            if (read < count)
                Array.Clear(buffer, offset + read, count - read);

            read = count;
            return read;
        }
    }
}
