// This was created on the basis of this pull request
// https://github.com/SoftwareGuy/Ignorance/pull/25
using System;

namespace Mirror
{
    public interface ISegmentTransport
    {
        bool ServerSend(int connectionId, int channelId, ArraySegment<byte> data);
        bool ClientSend(int channelId, ArraySegment<byte> data);
    }
}
