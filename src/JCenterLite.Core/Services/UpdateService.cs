using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace JCenterLite.Core.Services;

public sealed record ReleaseInfo(string TagName, string HtmlUrl, string? Body, bool Prerelease, DateTimeOffset? PublishedAt);

public sealed class UpdateService
{
    private readonly HttpClient _http;
    private readonly string _repository;

    public UpdateService(HttpClient http, string repository)
    {
        _http = http;
        _repository = repository;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("J-Center-Lite/1.0");
    }

    public async Task<ReleaseInfo?> GetLatestAsync(bool includePrerelease, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"https://api.github.com/repos/{_repository}/releases/latest";
            var release = await _http.GetFromJsonAsync<GitHubRelease>(url, cancellationToken).ConfigureAwait(false);
            if (release is null)
            {
                return null;
            }

            if (!includePrerelease && release.Prerelease)
            {
                return null;
            }

            return new ReleaseInfo(release.TagName, release.HtmlUrl, release.Body, release.Prerelease, release.PublishedAt);
        }
        catch
        {
            return null;
        }
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; init; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; init; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset? PublishedAt { get; init; }
    }
}
