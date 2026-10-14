using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JCenterLite.App.Helpers;
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

    public IReadOnlyList<ElementKindOption> ElementKinds { get; } = ElementKindInfo.All;

    [ObservableProperty]
    private ThemeDefinition? _selectedTheme;

    [ObservableProperty]
    private ThemeElementModel? _selectedElement;

    [ObservableProperty]
    private ElementKindOption _newElementKind = ElementKindInfo.All[0];

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>Raised when the element list of the selected theme changed structurally.</summary>
    public event EventHandler<ThemeDefinition>? ThemeChanged;

    /// <summary>Raised when an element property changed; the canvas only needs a repaint.</summary>
    public event EventHandler? ElementChanged;

    public bool HasSelectedElement => SelectedElement is not null;

    public bool HasSelectedTheme => SelectedTheme is not null;

    /// <summary>Index into <see cref="ElementKinds"/> for the selected element's type.</summary>
    public int SelectedElementKindIndex
    {
        get => SelectedElement is null ? -1 : (int)SelectedElement.Kind;
        set
        {
            if (SelectedElement is null || value < 0 || value >= ElementKinds.Count)
            {
                return;
            }

            var option = ElementKinds[value];
            var element = SelectedElement;
            if (option.Kind == element.Kind)
            {
                return;
            }

            var previousText = ElementKindInfo.DefaultText(element.Kind);
            var previousUnit = ElementKindInfo.DefaultUnit(element.Kind);
            element.Kind = option.Kind;

            // Only replace captions that still hold the previous kind's default.
            if (string.Equals(element.Text, previousText, StringComparison.Ordinal))
            {
                element.Text = ElementKindInfo.DefaultText(option.Kind);
            }

            if (string.Equals(element.Unit, previousUnit, StringComparison.Ordinal))
            {
                element.Unit = ElementKindInfo.DefaultUnit(option.Kind);
            }

            OnPropertyChanged();
            StatusMessage = $"元素类型：{option.Label}";
        }
    }

    [RelayCommand]
    private void AddTheme()
    {
        var theme = new ThemeDefinition { Name = $"自定义主题 {Themes.Count + 1}" };
        var element = CreateElement(ThemeElementKind.Time);
        element.X = 160;
        element.Y = 200;
        element.FontSize = 64;
        theme.Elements.Add(element);

        Themes.Add(theme);
        _library.Themes.Add(theme);
        SelectedTheme = theme;
        SelectedElement = element;
        ThemeChanged?.Invoke(this, theme);
    }

    [RelayCommand]
    private void DuplicateTheme()
    {
        if (SelectedTheme is null)
        {
            return;
        }

        var copy = new ThemeDefinition
        {
            Name = $"{SelectedTheme.Name} 副本",
            BackgroundFile = SelectedTheme.BackgroundFile,
            ThumbnailFile = SelectedTheme.ThumbnailFile,
            BackgroundIsVideo = SelectedTheme.BackgroundIsVideo,
            BackgroundColor = SelectedTheme.BackgroundColor,
            Elements = new ObservableCollection<ThemeElementModel>(SelectedTheme.Elements.Select(element => element.Clone())),
        };

        Themes.Add(copy);
        _library.Themes.Add(copy);
        SelectedTheme = copy;
        ThemeChanged?.Invoke(this, copy);
        StatusMessage = $"已复制主题：{copy.Name}";
    }

    [RelayCommand]
    private void DeleteTheme()
    {
        if (SelectedTheme is null)
        {
            return;
        }

        var removed = SelectedTheme;
        _library.Themes.Remove(removed);
        Themes.Remove(removed);
        SelectedTheme = Themes.FirstOrDefault();
        SelectedElement = null;
        ThemeChanged?.Invoke(this, SelectedTheme!);
        StatusMessage = $"已删除主题：{removed.Name}";
    }

    [RelayCommand]
    private void AddElement()
    {
        if (SelectedTheme is null)
        {
            AddTheme();
            return;
        }

        var element = CreateElement(NewElementKind.Kind);
        var index = SelectedTheme.Elements.Count;
        element.X = Math.Clamp(40 + index % 6 * 28, 0, 380);
        element.Y = Math.Clamp(40 + index % 6 * 28, 0, 380);
        element.ZIndex = index + 1;

        SelectedTheme.Elements.Add(element);
        SelectedElement = element;
        ThemeChanged?.Invoke(this, SelectedTheme);
        StatusMessage = $"已添加元素：{ElementKindInfo.Label(element.Kind)}";
    }

    [RelayCommand]
    private void DuplicateElement()
    {
        if (SelectedTheme is null || SelectedElement is null)
        {
            return;
        }

        var copy = SelectedElement.Clone();
        copy.X = Math.Clamp(copy.X + 16, 0, 420);
        copy.Y = Math.Clamp(copy.Y + 16, 0, 420);
        copy.ZIndex = SelectedTheme.Elements.Count + 1;

        SelectedTheme.Elements.Add(copy);
        SelectedElement = copy;
        ThemeChanged?.Invoke(this, SelectedTheme);
    }

    [RelayCommand]
    private void DeleteElement()
    {
        if (SelectedTheme is null || SelectedElement is null)
        {
            return;
        }

        var index = SelectedTheme.Elements.IndexOf(SelectedElement);
        SelectedTheme.Elements.Remove(SelectedElement);
        SelectedElement = SelectedTheme.Elements.Count == 0
            ? null
            : SelectedTheme.Elements[Math.Clamp(index - 1, 0, SelectedTheme.Elements.Count - 1)];
        ThemeChanged?.Invoke(this, SelectedTheme);
    }

    [RelayCommand]
    private void RaiseElement() => MoveElement(1);

    [RelayCommand]
    private void LowerElement() => MoveElement(-1);

    /// <summary>Moves the selected element one step in the draw order and renumbers ZIndex.</summary>
    private void MoveElement(int delta)
    {
        if (SelectedTheme is null || SelectedElement is null)
        {
            return;
        }

        var ordered = SelectedTheme.Elements.OrderBy(element => element.ZIndex).ToList();
        var index = ordered.IndexOf(SelectedElement);
        var target = Math.Clamp(index + delta, 0, ordered.Count - 1);
        if (index == target)
        {
            return;
        }

        ordered.RemoveAt(index);
        ordered.Insert(target, SelectedElement);
        for (var i = 0; i < ordered.Count; i++)
        {
            ordered[i].ZIndex = i;
        }

        ElementChanged?.Invoke(this, EventArgs.Empty);
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

        SelectedElement = null;
        SelectedTheme = Themes.FirstOrDefault();
        ThemeChanged?.Invoke(this, SelectedTheme!);
    }

    public ThemeElementModel CreateElement(ThemeElementKind kind) => new()
    {
        Kind = kind,
        Text = ElementKindInfo.DefaultText(kind),
        Unit = ElementKindInfo.DefaultUnit(kind),
        X = 40,
        Y = 40,
        FontSize = 40,
    };

    partial void OnSelectedElementChanged(ThemeElementModel? oldValue, ThemeElementModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.PropertyChanged -= OnElementPropertyChanged;
        }

        if (newValue is not null)
        {
            newValue.PropertyChanged += OnElementPropertyChanged;
        }

        OnPropertyChanged(nameof(HasSelectedElement));
        OnPropertyChanged(nameof(SelectedElementKindIndex));
    }

    partial void OnSelectedThemeChanged(ThemeDefinition? value) => OnPropertyChanged(nameof(HasSelectedTheme));

    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ThemeElementModel.Kind))
        {
            OnPropertyChanged(nameof(SelectedElementKindIndex));
        }

        ElementChanged?.Invoke(this, EventArgs.Empty);
    }
}
