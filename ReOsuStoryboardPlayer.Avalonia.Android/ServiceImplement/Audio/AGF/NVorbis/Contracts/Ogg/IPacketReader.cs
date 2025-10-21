using System;

namespace NVorbis.Contracts.Ogg
{
    interface IPacketReader
    {
        byte[] GetPacketData(int pagePacketIndex);

        void InvalidatePacketCache(IPacket packet);
    }
}
