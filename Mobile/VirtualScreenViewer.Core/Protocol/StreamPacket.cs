namespace VirtualScreenViewer.Core.Protocol;

public enum PacketType : byte
{
    VideFrame = 1,
    ConnectionRequest = 2,
    ConnectionResponse = 3,
    Heartbeat = 4
}

public class StreamPacket
{
    public PacketType Type { get; set; }
    public uint SequenceNumber { get; set; }
    public long Timestamp { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public byte[] PayLoad { get; set; } = Array.Empty<byte>();

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
        writer.Write(PayLoad.Length);
        writer.Write(PayLoad);

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
                PayLoad = reader.ReadBytes(reader.ReadInt32())
            };
        }
        catch
        {
            return null;
        }
    }
}
