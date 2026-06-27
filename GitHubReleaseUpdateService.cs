using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Resizer3
{
    internal sealed record GitHubReleaseUpdate(
        Version Version,
        string TagName,
        Uri DownloadUrl,
        string? Digest,
        Uri? Sha256DownloadUrl);

    internal static class GitHubReleaseUpdateService
    {
        // Set before the first public GitHub Release; blank keeps startup update checks disabled.
        internal const string GitHubOwner = "rockenrooster";
        internal const string GitHubRepo = "Resizer3";
        internal const string ReleaseAssetName = "Resizer3.exe";

        private static readonly HttpClient Http = new();
        private static readonly Regex Sha256Regex = new(@"\b[0-9a-fA-F]{64}\b", RegexOptions.Compiled);

        internal static async Task<GitHubReleaseUpdate?> CheckLatestAsync(Version localVersion, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(GitHubOwner))
                throw new InvalidOperationException("GitHubOwner is not configured.");

            var latestUri = new Uri($"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest");
            using var request = CreateRequest(latestUri, githubApi: true);
            using var response = await Http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            JsonElement root = document.RootElement;

            string tagName = GetString(root, "tag_name") ?? throw new InvalidOperationException("Latest release has no tag_name.");
            Version releaseVersion = ParseTagVersion(tagName);
            if (releaseVersion <= localVersion)
                return null;

            if (!root.TryGetProperty("assets", out JsonElement assets) || assets.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Latest release has no assets.");

            Uri? downloadUrl = null;
            Uri? sha256DownloadUrl = null;
            string? digest = null;

            foreach (JsonElement asset in assets.EnumerateArray())
            {
                string? name = GetString(asset, "name");
                string? url = GetString(asset, "browser_download_url");
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url))
                    continue;

                if (name.Equals(ReleaseAssetName, StringComparison.Ordinal))
                {
                    downloadUrl = new Uri(url);
                    digest = GetString(asset, "digest");
                }
                else if (name.Equals(ReleaseAssetName + ".sha256", StringComparison.Ordinal))
                {
                    sha256DownloadUrl = new Uri(url);
                }
            }

            if (downloadUrl is null)
                throw new InvalidOperationException($"Latest release does not contain {ReleaseAssetName}.");

            return new GitHubReleaseUpdate(releaseVersion, tagName, downloadUrl, digest, sha256DownloadUrl);
        }

        internal static async Task DownloadAndVerifyAsync(
            GitHubReleaseUpdate update,
            string destinationPath,
            Version localVersion,
            CancellationToken cancellationToken = default)
        {
            await DownloadFileAsync(update.DownloadUrl, destinationPath, cancellationToken);

            var file = new FileInfo(destinationPath);
            if (!file.Exists || file.Length == 0)
                throw new InvalidOperationException("Downloaded update is empty.");

            Version downloadedVersion = GetFileVersion(destinationPath);
            if (downloadedVersion < update.Version || downloadedVersion <= localVersion)
                throw new InvalidOperationException($"Downloaded version {downloadedVersion} does not match release {update.Version}.");

            string? expectedSha256 = NormalizeGitHubDigest(update.Digest);
            if (expectedSha256 is null && update.Sha256DownloadUrl is not null)
                expectedSha256 = await DownloadSha256Async(update.Sha256DownloadUrl, cancellationToken);

            if (expectedSha256 is not null)
                VerifySha256(destinationPath, expectedSha256);
        }

        private static async Task DownloadFileAsync(Uri uri, string destinationPath, CancellationToken cancellationToken)
        {
            using var request = CreateRequest(uri, githubApi: false);
            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 128 * 1024, useAsync: true);
            await source.CopyToAsync(destination, cancellationToken);
        }

        private static async Task<string?> DownloadSha256Async(Uri uri, CancellationToken cancellationToken)
        {
            using var request = CreateRequest(uri, githubApi: false);
            using var response = await Http.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            return NormalizeSha256(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        private static void VerifySha256(string path, string expectedSha256)
        {
            using var stream = File.OpenRead(path);
            string actualSha256 = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
            if (!actualSha256.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Downloaded update failed SHA256 verification.");
        }

        private static HttpRequestMessage CreateRequest(Uri uri, bool githubApi)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Resizer3", "1.0"));
            if (githubApi)
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return request;
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static Version ParseTagVersion(string tagName)
        {
            string value = tagName.Trim();
            if (value.StartsWith('v') || value.StartsWith('V'))
                value = value[1..];

            if (Version.TryParse(value, out Version? version))
                return version;

            throw new FormatException($"Release tag {tagName} is not a version.");
        }

        private static Version GetFileVersion(string path)
        {
            string? value = FileVersionInfo.GetVersionInfo(path).FileVersion;
            return Version.TryParse(value, out Version? version)
                ? version
                : new Version(0, 0, 0, 0);
        }

        private static string? NormalizeGitHubDigest(string? digest)
        {
            return digest?.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase) == true
                ? NormalizeSha256(digest)
                : null;
        }

        private static string? NormalizeSha256(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            Match match = Sha256Regex.Match(value);
            return match.Success ? match.Value.ToLowerInvariant() : null;
        }
    }
}
