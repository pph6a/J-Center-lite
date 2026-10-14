using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JCenterLite.Core.Models;

public enum ThemeElementKind
{
    CpuTemperature = 0,
    CpuFrequency = 1,
    CpuUsage = 2,
    CpuVoltage = 3,
    CpuPower = 4,
    GpuTemperature = 5,
    GpuUsage = 6,
    GpuFrequency = 7,
    RamTotal = 8,
    RamUsed = 9,
    RamFree = 10,
    RamUsage = 11,
    CpuFanRpm = 12,
    HddTemperature = 13,
    HddTotal = 14,
    HddUsed = 15,
    HddFree = 16,
    HddUsage = 17,
    LanUpload = 18,
    LanDownload = 19,
    Time = 20,
    Date = 21,
    Weekday = 22,
    CpuModel = 23,
    GpuModel = 24,
    FreeText = 25,
}

/// <summary>
/// A single element of a theme. The class is observable so the editor can keep the
/// element list, the property panel and the 480x480 canvas in sync while typing or dragging.
/// </summary>
public sealed class ThemeElementModel : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private ThemeElementKind _kind = ThemeElementKind.CpuTemperature;
    private string _text = "CPU";
    private string _unit = "°C";
    private string _color = "#FFFFFFFF";
    private double _x = 40;
    private double _y = 40;
    private double _scale = 1.0;
    private double _rotation;
    private double _fontSize = 40;
    private int _zIndex;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => Set(ref _id, value);
    }

    public ThemeElementKind Kind
    {
        get => _kind;
        set => Set(ref _kind, value);
    }

    public string Text
    {
        get => _text;
        set => Set(ref _text, value);
    }

    public string Unit
    {
        get => _unit;
        set => Set(ref _unit, value);
    }

    public string Color
    {
        get => _color;
        set => Set(ref _color, value);
    }

    public double X
    {
        get => _x;
        set => Set(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => Set(ref _y, value);
    }

    public double Scale
    {
        get => _scale;
        set => Set(ref _scale, value);
    }

    public double Rotation
    {
        get => _rotation;
        set => Set(ref _rotation, value);
    }

    public double FontSize
    {
        get => _fontSize;
        set => Set(ref _fontSize, value);
    }

    public int ZIndex
    {
        get => _zIndex;
        set => Set(ref _zIndex, value);
    }

    public ThemeElementModel Clone() => new()
    {
        Kind = Kind,
        Text = Text,
        Unit = Unit,
        Color = Color,
        X = X,
        Y = Y,
        Scale = Scale,
        Rotation = Rotation,
        FontSize = FontSize,
        ZIndex = ZIndex,
    };

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ThemeDefinition : INotifyPropertyChanged
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "My theme";
    private string _backgroundFile = string.Empty;
    private string _thumbnailFile = string.Empty;
    private bool _backgroundIsVideo;
    private string _backgroundColor = "#FF0B1220";
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => Set(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value);
    }

    public string BackgroundFile
    {
        get => _backgroundFile;
        set => Set(ref _backgroundFile, value);
    }

    public string ThumbnailFile
    {
        get => _thumbnailFile;
        set => Set(ref _thumbnailFile, value);
    }

    public bool BackgroundIsVideo
    {
        get => _backgroundIsVideo;
        set => Set(ref _backgroundIsVideo, value);
    }

    public string BackgroundColor
    {
        get => _backgroundColor;
        set => Set(ref _backgroundColor, value);
    }

    public List<ThemeElementModel> Elements { get; set; } = new();

    public DateTimeOffset UpdatedAt
    {
        get => _updatedAt;
        set => Set(ref _updatedAt, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class ThemeLibrary
{
    public List<ThemeDefinition> Themes { get; set; } = new();

    public static ThemeLibrary CreateDefault()
    {
        return new ThemeLibrary
        {
            Themes =
            {
                new ThemeDefinition
                {
                    Name = "极简时钟",
                    Elements =
                    {
                        new ThemeElementModel { Kind = ThemeElementKind.Time, Text = "12:00", Unit = string.Empty, X = 120, Y = 90, FontSize = 72, ZIndex = 2 },
                        new ThemeElementModel { Kind = ThemeElementKind.Date, Text = "2026-10-14", Unit = string.Empty, X = 140, Y = 190, FontSize = 24, ZIndex = 1 },
                    },
                },
                new ThemeDefinition
                {
                    Name = "温度仪表盘",
                    Elements =
                    {
                        new ThemeElementModel { Kind = ThemeElementKind.CpuTemperature, Text = "CPU", X = 60, Y = 80, FontSize = 56, ZIndex = 1 },
                        new ThemeElementModel { Kind = ThemeElementKind.GpuTemperature, Text = "GPU", X = 300, Y = 80, FontSize = 56, ZIndex = 2 },
                        new ThemeElementModel { Kind = ThemeElementKind.CpuFanRpm, Text = "FAN", Unit = "RPM", X = 60, Y = 260, FontSize = 40, ZIndex = 3 },
                    },
                },
            },
        };
    }
}
