using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using UnityEngine;

namespace Tests
{
	internal static class CodeScanRelease
	{
		private const string LatestReleaseUrl = "https://api.github.com/repos/unitystation/codescan/releases/latest";
		private const string ReleasesUrl = "https://github.com/unitystation/codescan/releases";

		internal static string EnsureAvailable(string cacheDirectory, string runtimeIdentifier)
		{
			using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) })
			{
				return EnsureAvailable(cacheDirectory, runtimeIdentifier, client);
			}
		}

		// Accept a client so release checks and failed downloads can be tested without GitHub.
		internal static string EnsureAvailable(string cacheDirectory, string runtimeIdentifier, HttpClient client)
		{
			var installDirectory = Path.Combine(cacheDirectory, runtimeIdentifier);
			var executableName = runtimeIdentifier == "win-x64" ? "CodeScan.exe" : "CodeScan";
			var executablePath = Path.Combine(installDirectory, runtimeIdentifier, executableName);
			var stagingDirectory = Path.Combine(cacheDirectory, ".download-" + Guid.NewGuid().ToString("N"));
			var backupDirectory = Path.Combine(cacheDirectory, ".previous-" + Guid.NewGuid().ToString("N"));

			try
			{
				Release release;
				using (var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl))
				{
					request.Headers.UserAgent.ParseAdd("unitystation-CodeScan");
					request.Headers.Accept.ParseAdd("application/vnd.github+json");
					using (var response = client.SendAsync(request).GetAwaiter().GetResult())
					{
						response.EnsureSuccessStatusCode();
						release = JsonConvert.DeserializeObject<Release>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
					}
				}

				var asset = release?.Assets?.SingleOrDefault(candidate => candidate.Name == runtimeIdentifier + ".zip");
				if (asset == null || string.IsNullOrWhiteSpace(asset.DownloadUrl) || string.IsNullOrWhiteSpace(release.Tag))
				{
					throw new InvalidDataException($"The latest CodeScan release has no {runtimeIdentifier}.zip asset.");
				}

				// Include asset identity and modification time to detect a replaced archive under the same tag.
				var version = $"{release.Tag}/{asset.Id}/{asset.UpdatedAt}";
				var versionPath = Path.Combine(installDirectory, "release.txt");
				if (IsInstalled(installDirectory, runtimeIdentifier, executableName) &&
				    File.Exists(versionPath) && File.ReadAllText(versionPath) == version)
				{
					return executablePath;
				}

				Directory.CreateDirectory(stagingDirectory);
				var archivePath = Path.Combine(stagingDirectory, "release.zip");
				using (var response = client.GetAsync(asset.DownloadUrl).GetAwaiter().GetResult())
				{
					response.EnsureSuccessStatusCode();
					using (var input = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
					using (var output = File.Create(archivePath))
					{
						input.CopyTo(output);
					}
				}

				var extractedDirectory = Path.Combine(stagingDirectory, "extracted");
				ZipFile.ExtractToDirectory(archivePath, extractedDirectory);
				if (IsInstalled(extractedDirectory, runtimeIdentifier, executableName) == false)
				{
					throw new InvalidDataException("The CodeScan archive is missing its executable or FilesToMoveToManaged.json.");
				}
				File.WriteAllText(Path.Combine(extractedDirectory, "release.txt"), version);

				// Only replace the cached tool after the entire download has been extracted and validated.
				if (Directory.Exists(installDirectory)) MoveDirectory(installDirectory, backupDirectory);
				try
				{
					MoveDirectory(extractedDirectory, installDirectory);
				}
				catch
				{
					if (Directory.Exists(backupDirectory)) MoveDirectory(backupDirectory, installDirectory);
					throw;
				}

				Debug.Log($"Installed CodeScan {release.Tag} ({runtimeIdentifier}) in {installDirectory}.");
				return executablePath;
			}
			catch (Exception exception) when (exception is HttpRequestException || exception is System.Threading.Tasks.TaskCanceledException ||
			                                  exception is IOException || exception is InvalidDataException ||
			                                  exception is JsonException || exception is InvalidOperationException)
			{
				if (IsInstalled(installDirectory, runtimeIdentifier, executableName))
				{
					Debug.LogWarning($"Unable to update CodeScan; using the cached tool at {executablePath}. {exception.Message}");
					return executablePath;
				}

				throw new InvalidOperationException($"CodeScan is not installed and could not be downloaded from {ReleasesUrl}.", exception);
			}
			finally
			{
				if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, true);
				if (Directory.Exists(backupDirectory) && IsInstalled(installDirectory, runtimeIdentifier, executableName))
				{
					Directory.Delete(backupDirectory, true);
				}
			}
		}

		private static void MoveDirectory(string source, string destination)
		{
			// Windows virus scanners can briefly hold newly extracted executables open.
			for (var attempt = 0; ; attempt++)
			{
				try
				{
					Directory.Move(source, destination);
					return;
				}
				catch (IOException) when (attempt < 4)
				{
					Thread.Sleep(200);
				}
			}
		}

		private static bool IsInstalled(string directory, string runtimeIdentifier, string executableName)
		{
			return File.Exists(Path.Combine(directory, runtimeIdentifier, executableName)) &&
			       File.Exists(Path.Combine(directory, runtimeIdentifier, "CodeScan.dll")) &&
			       File.Exists(Path.Combine(directory, "FilesToMoveToManaged.json"));
		}

		private sealed class Release
		{
			[JsonProperty("tag_name")] public string Tag { get; set; }
			[JsonProperty("assets")] public List<Asset> Assets { get; set; }
		}

		private sealed class Asset
		{
			[JsonProperty("id")] public long Id { get; set; }
			[JsonProperty("name")] public string Name { get; set; }
			[JsonProperty("updated_at")] public string UpdatedAt { get; set; }
			[JsonProperty("browser_download_url")] public string DownloadUrl { get; set; }
		}
	}
}
