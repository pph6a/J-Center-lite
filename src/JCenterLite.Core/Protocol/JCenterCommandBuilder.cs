namespace JCenterLite.Core.Protocol;

public enum PlayMode : byte
{
    Loop = 0,
    Random = 1,
    Off = 2,
}

public enum ScreenPower : byte
{
    Off = 0,
    On = 1,
}

public static class JCenterCommandBuilder
{
    public static byte[] Brightness(byte percent) =>
        JCenterFrame.Build(0x07, new byte[] { 0x00, percent }, 4);

    public static byte[] Screen(ScreenPower power) =>
        JCenterFrame.BuildShort(JCenterCommands.ScreenPower, new byte[] { (byte)power });

    public static byte[] VideoLoop(bool enabled) =>
        JCenterFrame.BuildShort(JCenterCommands.VideoLoop, new byte[] { enabled ? (byte)1 : (byte)0 });

    public static byte[] VideoRandom(bool enabled) =>
        JCenterFrame.BuildShort(JCenterCommands.VideoRandom, new byte[] { enabled ? (byte)1 : (byte)0 });

    public static byte[] Language(byte languageIndex) =>
        JCenterFrame.BuildShort(JCenterCommands.Language, new byte[] { languageIndex });

    public static byte[] Rotation(byte angle) =>
        JCenterFrame.BuildShort(JCenterCommands.RotationAngle, new byte[] { angle });

    public static byte[] SetAngle(byte angle) =>
        JCenterFrame.BuildShort(JCenterCommands.SetAngle, new byte[] { angle });

    public static byte[] QueryTerminalVersion() =>
        JCenterFrame.BuildShort(JCenterCommands.TerminalVersion, new byte[] { 0x00 });

    public static byte[] QueryAmbientNoise() =>
        JCenterFrame.BuildShort(JCenterCommands.AmbientNoise, new byte[] { 0x00 });

    public static byte[] SelectMedia(byte slot, string fileName)
    {
        var name = FixedAscii(fileName, 32);
        var payload = new byte[1 + name.Length];
        payload[0] = slot;
        name.CopyTo(payload.AsSpan(1));
        return JCenterFrame.Build(JCenterCommands.MediaSelect, payload, 35);
    }

    public static byte[] SetBackgroundImage(byte type, ushort showData, string unit, string fileName)
    {
        var unitBytes = FixedAscii(unit, 3);
        var nameBytes = FixedAscii(fileName, 32);
        var payload = new byte[1 + 2 + unitBytes.Length + nameBytes.Length];
        payload[0] = type;
        payload[1] = (byte)(showData & 0xFF);
        payload[2] = (byte)(showData >> 8);
        unitBytes.CopyTo(payload.AsSpan(3));
        nameBytes.CopyTo(payload.AsSpan(3 + unitBytes.Length));
        return JCenterFrame.Build(JCenterCommands.BackgroundImage, payload, 41);
    }

    public static byte[] SetThemeImage(byte themeIndex, byte type, ushort showData, string unit, string fileName)
    {
        var unitBytes = FixedAscii(unit, 3);
        var nameBytes = FixedAscii(fileName, 32);
        var payload = new byte[4 + unitBytes.Length + nameBytes.Length];
        payload[0] = themeIndex;
        payload[1] = type;
        payload[2] = (byte)(showData & 0xFF);
        payload[3] = (byte)(showData >> 8);
        unitBytes.CopyTo(payload.AsSpan(4));
        nameBytes.CopyTo(payload.AsSpan(4 + unitBytes.Length));
        return JCenterFrame.Build(JCenterCommands.ThemeImage, payload, 53);
    }

    public static byte[] DisplayClock(bool visible, string fileName = "")
    {
        var nameBytes = FixedAscii(fileName, 32);
        var payload = new byte[1 + nameBytes.Length];
        payload[0] = visible ? (byte)0 : (byte)1;
        nameBytes.CopyTo(payload.AsSpan(1));
        return JCenterFrame.Build(JCenterCommands.DisplayTime, payload, 55);
    }

    private static byte[] FixedAscii(string text, int length)
    {
        var bytes = new byte[length];
        var source = System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty);
        Array.Copy(source, bytes, Math.Min(source.Length, length));
        return bytes;
    }
}
