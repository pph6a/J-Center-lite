using JCenterLite.Core.Models;

namespace JCenterLite.Core.Protocol;

public static class JCenterCustomThemeBuilder
{
    public const byte CustomThemeCommand = 0x39;
    public const byte ProjectThemeCommand = 0x35;
    public const int ChunkSize = 8;

    public static byte[] BuildHeader(string fileName)
    {
        var name = FixedUtf8(fileName, 32);
        var payload = new byte[3 + name.Length + 4];
        payload[0] = 1;
        payload[1] = 1;
        payload[2] = 3;
        name.CopyTo(payload.AsSpan(3));
        return JCenterFrame.Build(CustomThemeCommand, payload, 42, sequence: 3);
    }

    public static IReadOnlyList<byte[]> BuildChunks(IEnumerable<ThemeElementModel> elements)
    {
        var list = elements.ToList();
        var frames = new List<byte[]>(Math.Max(1, (list.Count + ChunkSize - 1) / ChunkSize));
        for (var offset = 0; offset < list.Count; offset += ChunkSize)
        {
            var chunk = list.Skip(offset).Take(ChunkSize).ToList();
            var payload = new byte[2 + chunk.Count * 17];
            payload[0] = (byte)chunk.Count;
            payload[1] = 0;
            var cursor = 2;
            foreach (var element in chunk)
            {
                payload[cursor++] = 2; // text element
                payload[cursor++] = (byte)element.Kind;
                var unit = FixedUtf8(element.Unit, 3);
                unit.CopyTo(payload.AsSpan(cursor));
                cursor += 3;
                WriteU16(payload, ref cursor, (ushort)Math.Clamp(element.X, 0, 480));
                WriteU16(payload, ref cursor, (ushort)Math.Clamp(element.Y, 0, 480));
                WriteU16(payload, ref cursor, (ushort)Math.Clamp(element.Scale * 14, 0, ushort.MaxValue));
                var color = FixedUtf8(element.Color.Replace("#", string.Empty, StringComparison.Ordinal), 6);
                color.CopyTo(payload.AsSpan(cursor));
                cursor += 6;
            }

            var constant = (ushort)(5 + chunk.Count * 17);
            frames.Add(JCenterFrame.Build(CustomThemeCommand, payload, constant, sequence: 3));
        }

        return frames;
    }

    public static byte[] BuildProjectTheme(byte projectIndex, IReadOnlyList<(byte DataType, ushort Value, string Text)> items, string fileName, string background, string foreground, ushort angle)
    {
        var file = FixedUtf8(fileName, 32);
        var bg = FixedUtf8(background, 6);
        var fg = FixedUtf8(foreground, 6);
        var payload = new byte[4 + items.Count * 6 + file.Length + bg.Length + fg.Length + 2];
        payload[0] = (byte)items.Count;
        payload[1] = projectIndex;
        var cursor = 2;
        foreach (var item in items)
        {
            payload[cursor++] = item.DataType;
            WriteU16(payload, ref cursor, item.Value);
            var text = FixedUtf8(item.Text, 3);
            text.CopyTo(payload.AsSpan(cursor));
            cursor += 3;
        }

        file.CopyTo(payload.AsSpan(cursor));
        cursor += file.Length;
        bg.CopyTo(payload.AsSpan(cursor));
        cursor += bg.Length;
        fg.CopyTo(payload.AsSpan(cursor));
        cursor += fg.Length;
        WriteU16(payload, ref cursor, angle);

        var constant = (ushort)(80 + items.Count * 6);
        return JCenterFrame.Build(ProjectThemeCommand, payload, constant);
    }

    private static byte[] FixedUtf8(string text, int length)
    {
        var bytes = new byte[length];
        var source = System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty);
        Array.Copy(source, bytes, Math.Min(source.Length, length));
        return bytes;
    }

    private static void WriteU16(byte[] buffer, ref int offset, ushort value)
    {
        buffer[offset++] = (byte)(value & 0xFF);
        buffer[offset++] = (byte)(value >> 8);
    }
}
