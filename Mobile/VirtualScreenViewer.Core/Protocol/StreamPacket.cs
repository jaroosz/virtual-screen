namespace VirtualScreenViewer.Core.Protocol;

public enum PacketType : byte
{
    VideoFrame = 1,
    ConnectionRequest = 2,
    ConnectionResponse = 3,
    Heartbeat = 4,
    VideoFrameFragment = 5
}

public class StreamPacket
{
    private const int MaxUdpSize = 60000;

    public PacketType Type { get; set; }
    public uint SequenceNumber { get; set; }
    public long Timestamp { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public ushort FragmentIndex { get; set; }
    public ushort TotalFragments { get; set; }

    public byte[] Payload { get; set; } = Array.Empty<byte>();

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
        writer.Write(Payload.Length);
        writer.Write(Payload);

        return ms.ToArray();
    }

    public static StreamPacket? FromBytes(byte[] data)
    {
        try
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            return new StreamPacket
            {
                Type = (PacketType)reader.ReadByte(),
                SequenceNumber = reader.ReadUInt32(),
                Timestamp = reader.ReadInt64(),
                Width = reader.ReadInt32(),
                Height = reader.ReadInt32(),
                FragmentIndex = reader.ReadUInt16(),
                TotalFragments = reader.ReadUInt16(),
                Payload = reader.ReadBytes(reader.ReadInt32())
            };
        }
        catch
        {
            return null;
        }
    }

    public static List<StreamPacket> CreateFragments(
        byte[] imageData,
        int width,
        int height,
        uint sequenceNumber)
    {
        const int maxPayloadSize = MaxUdpSize - 100; // Odstęp na nagłówki
        var fragments = new List<StreamPacket>();
        var totalFragments = (ushort)Math.Ceiling((double)imageData.Length / maxPayloadSize);

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
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Width = width,
                Height = height,
                FragmentIndex = i,
                TotalFragments = totalFragments,
                Payload = payload
            });
        }

        return fragments;
    }
}
