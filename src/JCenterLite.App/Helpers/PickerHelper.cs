using Windows.Storage;
using Windows.Storage.Pickers;

namespace JCenterLite.App.Helpers;

public static class PickerHelper
{
    public static async Task<StorageFile?> PickFileAsync(nint windowHandle, params string[] extensions)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        };

        foreach (var extension in extensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
        return await picker.PickSingleFileAsync();
    }

    public static async Task<StorageFile?> PickExecutableAsync(nint windowHandle)
    {
        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
        };
        picker.FileTypeFilter.Add(".exe");
        WinRT.Interop.InitializeWithWindow.Initialize(picker, windowHandle);
        return await picker.PickSingleFileAsync();
    }
}
