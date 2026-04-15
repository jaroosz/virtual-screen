namespace VirtualScreen.Core.Protocol;

public enum PacketType : byte
{
    VideoFrame = 1,
    VideoFrameFragment = 2,
    ConnectionRequest = 3,
    ConnectionResponse = 4,
    Heartbeat = 5
}

public class StreamPacket
{
    private const int MaxUdpSize = 60000;
    private const int HeaderSize = 50;

    public PacketType Type { get; set; }
    public uint SequenceNumber { get; set; }
    public long Timestamp { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public ushort FragmentIndex { get; set; }
    public ushort TotalFragments { get; set; }
    public uint FrameNumber { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    // Header layout (50 bytes):
    // [0]      Type          (1)
    // [1-4]    SequenceNumber(4)
    // [5-12]   Timestamp     (8)
    // [13-16]  Width         (4)
    // [17-20]  Height        (4)
    // [21-22]  FragmentIndex (2)
    // [23-24]  TotalFragments(2)
    // [25-28]  FrameNumber   (4)
    // [29-49]  padding       (21)

    // serialize to byte array for UDP
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write((byte)Type);
        writer.Write(SequenceNumber);
        writer.Write(Timestamp);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(FragmentIndex);
        writer.Write(TotalFragments);
        writer.Write(FrameNumber);

        var paddingNeeded = HeaderSize - (int)ms.Position;
        if (paddingNeeded > 0)
        {
            writer.Write(new byte[paddingNeeded]);
        }

        writer.Write(Payload);
        return ms.ToArray();
    }

    public static StreamPacket? FromBytes(byte[] data)
    {
        if (data.Length < HeaderSize)
            return null;

        try
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            var packet = new StreamPacket
            {
                Type = (PacketType)reader.ReadByte(),
                SequenceNumber = reader.ReadUInt32(),
                Timestamp = reader.ReadInt64(),
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
                FragmentIndex = reader.ReadUInt16(),
                TotalFragments = reader.ReadUInt16(),
                FrameNumber = reader.ReadUInt32()
            };

            ms.Seek(HeaderSize, SeekOrigin.Begin);

            var payloadSize = data.Length - HeaderSize;
            if (payloadSize > 0)
            {
                packet.Payload = reader.ReadBytes(payloadSize);
            }

            return packet;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Packet parse error: {ex.Message}");
            return null;
        }
    }

    public static List<StreamPacket> CreateFragments(
        byte[] imageData,
        int width,
        int height,
        uint sequenceNumber,
        uint frameNumber = 0)
    {
        const int maxPayloadSize = MaxUdpSize - HeaderSize;
        var fragments = new List<StreamPacket>();
        var totalFragments = (ushort)Math.Ceiling((double)imageData.Length / maxPayloadSize);
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        for (ushort i = 0; i < totalFragments; i++)
        {
            var offset = i * maxPayloadSize;
            var length = Math.Min(maxPayloadSize, imageData.Length - offset);
            var payload = new byte[length];
            Array.Copy(imageData, offset, payload, 0, length);

            fragments.Add(new StreamPacket
            {
                Type = totalFragments > 1 ? PacketType.VideoFrameFragment : PacketType.VideoFrame,
                SequenceNumber = sequenceNumber,
                Timestamp = ts,
                Width = width,
                Height = height,
                FragmentIndex = i,
                TotalFragments = totalFragments,
                FrameNumber = frameNumber,
                Payload = payload
            });
        }

        return fragments;
    }
}
