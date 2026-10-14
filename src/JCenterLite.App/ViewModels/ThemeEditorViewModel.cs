using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JCenterLite.Core.Models;
using JCenterLite.Core.Services;

namespace JCenterLite.App.ViewModels;

public sealed partial class ThemeEditorViewModel : ObservableObject
{
    private readonly ThemeStore _store = new();
    private readonly ThemeLibrary _library;

    public ThemeEditorViewModel(ThemeLibrary library)
    {
        _library = library;
        foreach (var theme in library.Themes)
        {
            Themes.Add(theme);
        }

        SelectedTheme = Themes.FirstOrDefault();
    }

    public ThemeEditorViewModel()
        : this(ThemeLibrary.CreateDefault())
    {
    }

    public ObservableCollection<ThemeDefinition> Themes { get; } = new();

    public IReadOnlyList<ThemeElementKind> ElementKinds { get; } = Enum.GetValues<ThemeElementKind>();

    [ObservableProperty]
    private ThemeDefinition? _selectedTheme;

    [ObservableProperty]
    private ThemeElementModel? _selectedElement;

    [ObservableProperty]
    private ThemeElementKind _newElementKind = ThemeElementKind.CpuTemperature;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public event EventHandler<ThemeDefinition>? ThemeChanged;

    [RelayCommand]
    private void AddTheme()
    {
        var theme = new ThemeDefinition { Name = $"自定义主题 {Themes.Count + 1}" };
        theme.Elements.Add(CreateElement(ThemeElementKind.CpuTemperature));
        Themes.Add(theme);
        _library.Themes.Add(theme);
        SelectedTheme = theme;
        ThemeChanged?.Invoke(this, theme);
    }

    [RelayCommand]
    private void DeleteTheme()
    {
        if (SelectedTheme is null)
        {
            return;
        }

        _library.Themes.Remove(SelectedTheme);
        Themes.Remove(SelectedTheme);
        SelectedTheme = Themes.FirstOrDefault();
        ThemeChanged?.Invoke(this, SelectedTheme!);
    }

    [RelayCommand]
    private void AddElement()
    {
        if (SelectedTheme is null)
        {
            AddTheme();
        }

        var element = CreateElement(NewElementKind);
        SelectedTheme!.Elements.Add(element);
        SelectedElement = element;
        ThemeChanged?.Invoke(this, SelectedTheme);
    }

    [RelayCommand]
    private void DeleteElement()
    {
        if (SelectedTheme is null || SelectedElement is null)
        {
            return;
        }

        SelectedTheme.Elements.Remove(SelectedElement);
        SelectedElement = null;
        ThemeChanged?.Invoke(this, SelectedTheme);
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (SelectedTheme is not null)
        {
            SelectedTheme.UpdatedAt = DateTimeOffset.Now;
        }

        await _store.SaveAsync(_library).ConfigureAwait(true);
        StatusMessage = "已保存主题库";
    }

    public void Reload(ThemeLibrary library)
    {
        _library.Themes.Clear();
        _library.Themes.AddRange(library.Themes);
        Themes.Clear();
        foreach (var theme in library.Themes)
        {
            Themes.Add(theme);
        }

        SelectedTheme = Themes.FirstOrDefault();
        ThemeChanged?.Invoke(this, SelectedTheme!);
    }

    public ThemeElementModel CreateElement(ThemeElementKind kind)
    {
        return new ThemeElementModel
        {
            Kind = kind,
            Text = kind.ToString(),
            Unit = kind switch
            {
                ThemeElementKind.CpuTemperature or ThemeElementKind.GpuTemperature or ThemeElementKind.HddTemperature => "°C",
                ThemeElementKind.CpuFrequency or ThemeElementKind.GpuFrequency => "MHz",
                ThemeElementKind.CpuUsage or ThemeElementKind.GpuUsage or ThemeElementKind.RamUsage or ThemeElementKind.HddUsage => "%",
                ThemeElementKind.CpuFanRpm => "RPM",
                ThemeElementKind.LanUpload or ThemeElementKind.LanDownload => "KB/s",
                _ => string.Empty,
            },
            X = 40,
            Y = 40,
            FontSize = 40,
        };
    }

    partial void OnSelectedElementChanged(ThemeElementModel? value) => OnPropertyChanged(nameof(HasSelectedElement));

    public bool HasSelectedElement => SelectedElement is not null;
}
