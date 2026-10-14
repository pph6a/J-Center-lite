using JCenterLite.Core.Models;
using JCenterLite.Core.Protocol;
using Xunit;

namespace JCenterLite.Core.Tests;

public sealed class ProtocolTests
{
    [Fact]
    public void BrightnessFrameMatchesCapturedProtocolShape()
    {
        var frame = JCenterCommandBuilder.Brightness(100);

        Assert.Equal(9, frame.Length);
        Assert.Equal(0xAA, frame[0]);
        Assert.Equal(0xF5, frame[1]);
        Assert.Equal(4, frame[2]);
        Assert.Equal(0, frame[3]);
        Assert.Equal(0x01, frame[4]);
        Assert.Equal(0x07, frame[5]);
        Assert.Equal(0x00, frame[6]);
        Assert.Equal(100, frame[7]);
        Assert.Equal(Sum(frame, frame.Length - 1), frame[^1]);
    }

    [Fact]
    public void TelemetryFrameIsSeventyEightBytesAndChecksumValid()
    {
        var telemetry = new FanTelemetry
        {
            SurfaceTemperatureDeciCelsius = 430,
            CoreTemperatureDeciCelsius = 760,
            PackageTemperatureDeciCelsius = 430,
            CpuFanRpm = 3007,
            RamTotalGb = 63,
            GpuTemperatureCelsius = 51,
        };

        var frame = JCenterTelemetryCodec.Build(telemetry);

        Assert.Equal(78, frame.Length);
        Assert.Equal(38, frame[2] | (frame[3] << 8));
        Assert.Equal(0x01, frame[4]);
        Assert.Equal(JCenterCommands.FanTelemetry, frame[5]);
        Assert.Equal(430, frame[6] | (frame[7] << 8));
        Assert.Equal(760, frame[8] | (frame[9] << 8));
        Assert.Equal(Sum(frame, frame.Length - 1), frame[^1]);
    }

    [Fact]
    public void CustomThemeHeaderAndChunksUseObservedCommand()
    {
        var header = JCenterCustomThemeBuilder.BuildHeader("background.mp4");
        Assert.Equal(42, header[2] | (header[3] << 8));
        Assert.Equal(3, header[4]);
        Assert.Equal(JCenterCustomThemeBuilder.CustomThemeCommand, header[5]);

        var elements = new[]
        {
            new ThemeElementModel { Kind = ThemeElementKind.Time, Unit = "", X = 100, Y = 120, Scale = 2.0, Color = "#FFFFFFFF" },
            new ThemeElementModel { Kind = ThemeElementKind.CpuTemperature, Unit = "°C", X = 40, Y = 40, Scale = 3.5, Color = "#FF00AA" },
        };

        var chunks = JCenterCustomThemeBuilder.BuildChunks(elements);
        Assert.Single(chunks);
        Assert.Equal(5 + elements.Length * 17, chunks[0][2] | (chunks[0][3] << 8));
        Assert.Equal(9 + elements.Length * 17, chunks[0].Length);
        Assert.Equal(Sum(chunks[0], chunks[0].Length - 1), chunks[0][^1]);
    }

    [Fact]
    public void FrameParserRoundTrips()
    {
        var frame = JCenterCommandBuilder.QueryAmbientNoise();
        Assert.True(JCenterFrame.TryParse(frame, out var parsed));
        Assert.Equal(JCenterCommands.AmbientNoise, parsed.Command);
        Assert.Equal(1, parsed.Sequence);
        Assert.True(parsed.ChecksumValid);
    }

    private static byte Sum(byte[] data, int length)
    {
        byte sum = 0;
        for (var i = 0; i < length; i++)
        {
            sum += data[i];
        }

        return sum;
    }
}
