using Newtonsoft.Json;
using SysBot.Pokemon.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms
{
    public class UpdateChecker
    {
        private const string RepositoryOwner = "";
        private const string RepositoryName = "";

        // Reuse HttpClient to prevent socket exhaustion and memory leaks
        // HttpClient is thread-safe and should be reused
        private static readonly HttpClient _sharedClient = CreateGitHubClient();

        private static HttpClient CreateGitHubClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout for slow connections
            client.DefaultRequestHeaders.Add("User-Agent", "PokeBot");
            // No auth token needed for public repo
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            return client;
        }

        public static async Task<(bool UpdateAvailable, bool UpdateRequired, string NewVersion)> CheckForUpdatesAsync(bool forceShow = false)
        {
            try
            {
                ReleaseInfo? latestRelease = await FetchLatestReleaseAsync();
                if (latestRelease == null)
                {
                    if (forceShow)
                    {
                        MessageBox.Show("Failed to fetch release information. Please check your internet connection.",
                            "Update Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return (false, false, string.Empty);
                }

                bool updateAvailable = latestRelease.TagName != PokeBot.Version;
                bool updateRequired = !latestRelease.Prerelease && IsUpdateRequired(latestRelease.Body ?? string.Empty);
                string newVersion = latestRelease.TagName ?? string.Empty;

                if (forceShow)
                {
                    var updateForm = new UpdateForm(updateRequired, newVersion, updateAvailable);
                    updateForm.ShowDialog();
                }

                return (updateAvailable, updateRequired, newVersion);
            }
            catch (Exception ex)
            {
                if (forceShow)
                {
                    MessageBox.Show($"Error checking for updates: {ex.Message}",
                        "Update Check Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return (false, false, string.Empty);
            }
        }

        public static async Task<string> FetchChangelogAsync()
        {
            try
            {
                ReleaseInfo? latestRelease = await FetchLatestReleaseAsync();
                return latestRelease?.Body ?? "Failed to fetch the latest release information.";
            }
            catch (Exception ex)
            {
                return $"Error fetching changelog: {ex.Message}";
            }
        }

        public static async Task<string?> FetchDownloadUrlAsync()
        {
            try
            {
                ReleaseInfo? latestRelease = await FetchLatestReleaseAsync();
                if (latestRelease?.Assets == null || !latestRelease.Assets.Any())
                {
                    Console.WriteLine("No assets found in the release");
                    return null;
                }

                var exeAsset = latestRelease.Assets
                    .FirstOrDefault(a => a.Name?.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) == true);

                if (exeAsset == null)
                {
                    Console.WriteLine("No .exe asset found in the release");
                    return null;
                }

                // For public repos, use browser_download_url directly
                if (string.IsNullOrEmpty(exeAsset.BrowserDownloadUrl))
                {
                    Console.WriteLine("Download URL is empty");
                    return null;
                }

                Console.WriteLine($"Found download URL: {exeAsset.BrowserDownloadUrl}");
                return exeAsset.BrowserDownloadUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching download URL: {ex.Message}");
                return null;
            }
        }

        private static async Task<ReleaseInfo?> FetchLatestReleaseAsync()
        {
            const int maxRetries = 3;
            Exception? lastException = null;

            for (int retry = 0; retry < maxRetries; retry++)
            {
                if (retry > 0)
                {
                    // Wait before retry (exponential backoff)
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retry)));
                    Console.WriteLine($"Retrying fetch attempt {retry + 1}/{maxRetries}...");
                }

                // Use shared HttpClient instance to prevent memory leaks
                try
                {
                    string releasesUrl = $"https://api.github.com/repos/{RepositoryOwner}/{RepositoryName}/releases/latest";
                    Console.WriteLine($"Fetching from URL: {releasesUrl}");

                    HttpResponseMessage response = await _sharedClient.GetAsync(releasesUrl);
                    string responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"GitHub API Error: {response.StatusCode} - {responseContent}");
                        lastException = new HttpRequestException($"GitHub API returned {response.StatusCode}");
                        continue; // Try again
                    }

                    var releaseInfo = JsonConvert.DeserializeObject<ReleaseInfo>(responseContent);
                    if (releaseInfo == null)
                    {
                        Console.WriteLine("Failed to deserialize release info");
                        lastException = new InvalidOperationException("Failed to deserialize release info");
                        continue; // Try again
                    }

                    Console.WriteLine($"Successfully fetched release info. Tag: {releaseInfo.TagName}");
                    return releaseInfo;
                }
                catch (TaskCanceledException ex)
                {
                    Console.WriteLine($"Request timed out on attempt {retry + 1}: {ex.Message}");
                    lastException = ex;
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"Network error on attempt {retry + 1}: {ex.Message}");
                    lastException = ex;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error on attempt {retry + 1}: {ex.Message}");
                    lastException = ex;
                }
            }

            // All retries failed
            Console.WriteLine($"Failed to fetch release info after {maxRetries} attempts");
            if (lastException != null)
                Console.WriteLine($"Last error: {lastException.Message}");

            return null;
        }

        private static bool IsUpdateRequired(string changelogBody)
        {
            return !string.IsNullOrWhiteSpace(changelogBody) &&
                   changelogBody.Contains("Required = Yes", StringComparison.OrdinalIgnoreCase);
        }

        private class ReleaseInfo
        {
            [JsonProperty("tag_name")]
            public string? TagName { get; set; }

            [JsonProperty("prerelease")]
            public bool Prerelease { get; set; }

            [JsonProperty("assets")]
            public List<AssetInfo>? Assets { get; set; }

            [JsonProperty("body")]
            public string? Body { get; set; }
        }

        private class AssetInfo
        {
            [JsonProperty("name")]
            public string? Name { get; set; }

            [JsonProperty("url")]
            public string? Url { get; set; }

            [JsonProperty("browser_download_url")]
            public string? BrowserDownloadUrl { get; set; }
        }
    }
}