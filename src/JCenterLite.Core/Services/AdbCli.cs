using System.Diagnostics;
using System.Text;
using JCenterLite.Core.Models;

namespace JCenterLite.Core.Services;

/// <summary>
/// Thin wrapper around a user-provided adb.exe. The original client ships
/// Google's platform-tools; this project deliberately does not redistribute
/// them, so the path is discovered locally or configured in settings.
/// </summary>
public sealed class AdbCli
{
    private readonly Func<string> _pathProvider;

    public AdbCli(Func<string> pathProvider)
    {
        _pathProvider = pathProvider;
    }

    public string ResolveAdbPath()
    {
        var configured = _pathProvider();
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
        {
            return configured;
        }

        var candidates = new List<string>();
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            candidates.Add(Path.Combine(dir.Trim(), "adb.exe"));
            candidates.Add(Path.Combine(dir.Trim(), "platform-tools", "adb.exe"));
        }

        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk", "platform-tools", "adb.exe"));
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "scoop", "apps", "adb", "current", "adb.exe"));
        candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "horiemtech", "J-Center", "ADB", "adb.exe"));

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    public async Task<(int ExitCode, string Output, string Error)> RunAsync(string arguments, CancellationToken cancellationToken = default)
    {
        var adb = ResolveAdbPath();
        if (string.IsNullOrEmpty(adb))
        {
            return (-1, string.Empty, "adb.exe not found. Configure it in Settings.");
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = adb,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            },
        };

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        return (process.ExitCode, await outputTask.ConfigureAwait(false), await errorTask.ConfigureAwait(false));
    }

    public async Task<DeviceStatus> ProbeAsync(CancellationToken cancellationToken = default)
    {
        var adb = ResolveAdbPath();
        if (string.IsNullOrEmpty(adb))
        {
            return new DeviceStatus { AdbAvailable = false, Message = "adb.exe not found" };
        }

        var result = await RunAsync("devices -l", cancellationToken).ConfigureAwait(false);
        var lines = result.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Older adb releases print space-padded columns (not tabs):
            // HRBDFUN          device product:... model:... device:... transport_id:1
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var state = parts[1];
            if (!state.Equals("device", StringComparison.OrdinalIgnoreCase))
            {
                return new DeviceStatus { AdbAvailable = true, AdbPath = adb, Message = state };
            }

            string Find(string prefix) => parts.FirstOrDefault(part => part.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..] ?? string.Empty;
            var model = Find("model:");
            var product = Find("product:");
            var device = Find("device:");

            return new DeviceStatus
            {
                AdbAvailable = true,
                DeviceConnected = true,
                DeviceSerial = parts[0],
                DeviceModel = string.IsNullOrWhiteSpace(model) ? (string.IsNullOrWhiteSpace(product) ? device : product) : model,
                AdbPath = adb,
                Message = "Connected",
            };
        }

        return new DeviceStatus { AdbAvailable = true, AdbPath = adb, Message = "No device" };
    }

    public Task<(int ExitCode, string Output, string Error)> ForwardAsync(int localPort, int devicePort, CancellationToken cancellationToken = default) =>
        RunAsync($"forward tcp:{localPort} tcp:{devicePort}", cancellationToken);

    public async Task<bool> IsForwardedAsync(int localPort, int devicePort, CancellationToken cancellationToken = default)
    {
        var result = await RunAsync("forward --list", cancellationToken).ConfigureAwait(false);
        var needle = $"tcp:{localPort} tcp:{devicePort}";
        return result.Output.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    public Task<(int ExitCode, string Output, string Error)> RemoveForwardAsync(int localPort, CancellationToken cancellationToken = default) =>
        RunAsync($"forward --remove tcp:{localPort}", cancellationToken);

    public Task<(int ExitCode, string Output, string Error)> PushAsync(string localFile, string remoteFile, CancellationToken cancellationToken = default) =>
        RunAsync($"push \"{localFile}\" \"{remoteFile}\"", cancellationToken);

    public Task<(int ExitCode, string Output, string Error)> ShellAsync(string command, CancellationToken cancellationToken = default) =>
        RunAsync($"shell {command}", cancellationToken);
}
