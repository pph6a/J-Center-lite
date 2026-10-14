namespace JCenterLite.Core.Protocol;

/// <summary>
/// Command identifiers observed in the shipped J-Center client and confirmed
/// against the display firmware with read-only queries.
/// </summary>
public static class JCenterCommands
{
    public const byte FanTelemetry = 0x15;
    public const byte MediaSelect = 0x1F;
    public const byte BackgroundImage = 0x22;
    public const byte ScreenPower = 0x24;
    public const byte VideoLoop = 0x26;
    public const byte VideoRandom = 0x27;
    public const byte TerminalVersion = 0x2E;
    public const byte AmbientNoise = 0x2F;
    public const byte Language = 0x30;
    public const byte RotationAngle = 0x32;
    public const byte ThemeImage = 0x35;
    public const byte DisplayTime = 0x37;
    public const byte SetAngle = 0x38;
}
