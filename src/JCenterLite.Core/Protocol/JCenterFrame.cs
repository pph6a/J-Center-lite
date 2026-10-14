namespace JCenterLite.Core.Protocol;

/// <summary>
/// Frame layout used by the QiaoSiBo 480x480 display firmware:
/// AA F5 | count (LE16) | sequence | command | payload | checksum.
/// The checksum is the low byte of the sum of every preceding byte.
/// </summary>
public static class JCenterFrame
{
    public const byte Magic0 = 0xAA;
    public const byte Magic1 = 0xF5;
    public const byte Sequence = 0x01;

    public static byte[] Build(byte command, ReadOnlySpan<byte> payload, ushort wordCount)
        => Build(command, payload, wordCount, Sequence);

    public static byte[] Build(byte command, ReadOnlySpan<byte> payload, ushort wordCount, byte sequence)
    {
        var body = new byte[2 + 1 + 1 + payload.Length + 1];
        body[0] = (byte)(wordCount & 0xFF);
        body[1] = (byte)(wordCount >> 8);
        body[2] = sequence;
        body[3] = command;
        payload.CopyTo(body.AsSpan(4));

        var frame = new byte[2 + body.Length];
        frame[0] = Magic0;
        frame[1] = Magic1;
        body.CopyTo(frame.AsSpan(2));

        byte sum = 0;
        for (var i = 0; i < frame.Length - 1; i++)
        {
            sum += frame[i];
        }

        frame[^1] = sum;
        return frame;
    }

    public static byte[] BuildShort(byte command, ReadOnlySpan<byte> payload) => Build(command, payload, 4);

    public static bool TryParse(ReadOnlySpan<byte> data, out ParsedFrame frame)
    {
        frame = default;
        if (data.Length < 7 || data[0] != Magic0 || data[1] != Magic1)
        {
            return false;
        }

        var count = (ushort)(data[2] | (data[3] << 8));
        var declaredLength = 2 + count * 2;
        var length = Math.Min(declaredLength, data.Length);
        var checksum = data[length - 1];
        byte sum = 0;
        for (var i = 0; i < length - 1; i++)
        {
            sum += data[i];
        }

        frame = new ParsedFrame(count, data[4], data[5], data.Slice(6, Math.Max(0, length - 7)).ToArray(), checksum == sum);
        return true;
    }

    public readonly record struct ParsedFrame(ushort WordCount, byte Sequence, byte Command, byte[] Payload, bool ChecksumValid);
}
