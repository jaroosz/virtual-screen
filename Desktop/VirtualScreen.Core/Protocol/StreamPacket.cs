namespace VirtualScreen.Core.Protocol;

public enum PacketType : byte
{
    VideoFrame = 1,
    ConnectionRequest = 3,
    ConnectionResponse = 4,
    Heartbeat = 5
}

public enum CursorType : byte
{
    Hidden = 0,
    Arrow = 1,
    IBeam = 2,
    Hand = 3,
    Wait = 4,
    ResizeNS = 5,
    ResizeEW = 6,
    ResizeNWSE = 7,
    ResizeNESW = 8,
    Cross = 9
}

public class StreamPacket
{
    private const int HeaderSize = 50;

    public PacketType Type { get; set; }
    public uint SequenceNumber { get; set; }
    public long Timestamp { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public ushort FragmentIndex { get; set; }
    public ushort TotalFragments { get; set; }
    public uint FrameNumber { get; set; }
    public short CursorX { get; set; }
    public short CursorY { get; set; }
    public CursorType CursorType { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();

    // Header layout (50 bytes):
    // [0]      Type           (1)
    // [1-4]    SequenceNumber (4)
    // [5-12]   Timestamp      (8)
    // [13-16]  Width          (4)
    // [17-20]  Height         (4)
    // [21-22]  FragmentIndex  (2)
    // [23-24]  TotalFragments (2)
    // [25-28]  FrameNumber    (4)
    // [29-30]  CursorX        (2)
    // [31-32]  CursorY        (2)
    // [33]     CursorType     (1)
    // [34-49]  padding        (16)

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
        writer.Write(CursorX);
        writer.Write(CursorY);
        writer.Write((byte)CursorType);

        var paddingNeeded = HeaderSize - (int)ms.Position;
        if (paddingNeeded > 0)
            writer.Write(new byte[paddingNeeded]);

        writer.Write(Payload);
        return ms.ToArray();
    }

    public static StreamPacket? FromBytes(byte[] data)
    {
        if (data.Length < HeaderSize) return null;

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
                FrameNumber = reader.ReadUInt32(),
                CursorX = reader.ReadInt16(),
                CursorY = reader.ReadInt16(),
                CursorType = (CursorType)reader.ReadByte()
            };

            ms.Seek(HeaderSize, SeekOrigin.Begin);

            var payloadSize = data.Length - HeaderSize;
            if (payloadSize > 0)
                packet.Payload = reader.ReadBytes(payloadSize);

            return packet;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Packet parse error: {ex.Message}");
            return null;
        }
    }
}