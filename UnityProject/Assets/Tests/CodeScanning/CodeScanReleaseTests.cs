using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Tests
{
	public class CodeScanReleaseTests
	{
		private string cacheDirectory;

		[SetUp]
		public void SetUp()
		{
			cacheDirectory = Path.Combine(Path.GetTempPath(), "CodeScanReleaseTests-" + Guid.NewGuid().ToString("N"));
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(cacheDirectory)) Directory.Delete(cacheDirectory, true);
		}

		[TestCase("win-x64", "CodeScan.exe")]
		[TestCase("linux-x64", "CodeScan")]
		[TestCase("osx-x64", "CodeScan")]
		public void DownloadsMissingToolAndReusesUnchangedRelease(string runtimeIdentifier, string executableName)
		{
			var server = new ReleaseServer { RuntimeIdentifier = runtimeIdentifier, Archive = CreateArchive(runtimeIdentifier) };
			using (var client = new HttpClient(server))
			{
				var executable = CodeScanRelease.EnsureAvailable(cacheDirectory, runtimeIdentifier, client);
				Assert.That(executable, Is.EqualTo(Path.Combine(cacheDirectory, runtimeIdentifier, runtimeIdentifier, executableName)));
				Assert.That(File.ReadAllText(executable), Is.EqualTo("original"));
				Assert.That(File.Exists(Path.Combine(cacheDirectory, runtimeIdentifier, "FilesToMoveToManaged.json")), Is.True);
				Assert.That(CodeScanRelease.EnsureAvailable(cacheDirectory, runtimeIdentifier, client), Is.EqualTo(executable));
				Assert.That(server.ReleaseChecks, Is.EqualTo(2));
				Assert.That(server.Downloads, Is.EqualTo(1));
			}
		}

		[TestCase("build-2-1", 2, "2026-11-19T00:00:00Z")]
		[TestCase("build-1-1", 2, "2026-11-18T00:00:00Z")]
		[TestCase("build-1-1", 1, "2026-11-19T00:00:00Z")]
		public void DownloadsChangedReleaseOrReplacedAsset(string tag, long assetId, string updatedAt)
		{
			var server = new ReleaseServer();
			using (var client = new HttpClient(server))
			{
				var executable = CodeScanRelease.EnsureAvailable(cacheDirectory, "win-x64", client);
				server.Tag = tag;
				server.AssetId = assetId;
				server.UpdatedAt = updatedAt;
				server.Archive = CreateArchive("win-x64", "updated");
				CodeScanRelease.EnsureAvailable(cacheDirectory, "win-x64", client);
				Assert.That(File.ReadAllText(executable), Is.EqualTo("updated"));
				Assert.That(server.Downloads, Is.EqualTo(2));
				Assert.That(Directory.GetDirectories(cacheDirectory).Length, Is.EqualTo(1));
			}
		}

		[TestCase("offline")]
		[TestCase("missing asset")]
		[TestCase("download error")]
		[TestCase("invalid zip")]
		[TestCase("incomplete zip")]
		public void FailedUpdatePreservesCachedToolAndVersion(string failure)
		{
			var server = new ReleaseServer();
			using (var client = new HttpClient(server))
			{
				var executable = CodeScanRelease.EnsureAvailable(cacheDirectory, "win-x64", client);
				var versionPath = Path.Combine(cacheDirectory, "win-x64", "release.txt");
				var originalVersion = File.ReadAllText(versionPath);
				server.Tag = "build-2-1";
				SetFailure(server, failure);
				Assert.That(CodeScanRelease.EnsureAvailable(cacheDirectory, "win-x64", client), Is.EqualTo(executable));
				Assert.That(File.ReadAllText(executable), Is.EqualTo("original"));
				Assert.That(File.ReadAllText(versionPath), Is.EqualTo(originalVersion));
				Assert.That(Directory.GetDirectories(cacheDirectory).Length, Is.EqualTo(1));
			}
		}

		[TestCase("offline")]
		[TestCase("missing asset")]
		[TestCase("download error")]
		[TestCase("invalid zip")]
		[TestCase("incomplete zip")]
		public void FailedInitialInstallReportsDownloadFailure(string failure)
		{
			var server = new ReleaseServer();
			SetFailure(server, failure);
			using (var client = new HttpClient(server))
			{
				var exception = Assert.Throws<InvalidOperationException>(() =>
					CodeScanRelease.EnsureAvailable(cacheDirectory, "win-x64", client));
				Assert.That(exception.Message, Does.Contain("https://github.com/unitystation/codescan/releases"));
				Assert.That(File.Exists(Path.Combine(cacheDirectory, "win-x64", "release.txt")), Is.False);
			}
		}

		[Test]
		public void RepairsIncompleteCachedInstallation()
		{
			var server = new ReleaseServer();
			using (var client = new HttpClient(server))
			{
				var executable = CodeScanRelease.EnsureAvailable(cacheDirectory, "win-x64", client);
				var assemblyPath = Path.Combine(Path.GetDirectoryName(executable), "CodeScan.dll");
				File.Delete(assemblyPath);
				CodeScanRelease.EnsureAvailable(cacheDirectory, "win-x64", client);
				Assert.That(File.Exists(assemblyPath), Is.True);
				Assert.That(server.Downloads, Is.EqualTo(2));
			}
		}

		private static void SetFailure(ReleaseServer server, string failure)
		{
			server.Offline = failure == "offline";
			server.MissingAsset = failure == "missing asset";
			server.DownloadError = failure == "download error";
			if (failure == "invalid zip") server.Archive = new byte[] { 1, 2, 3 };
			if (failure == "incomplete zip") server.Archive = CreateArchive("linux-x64");
		}

		private static byte[] CreateArchive(string runtimeIdentifier, string executableContent = "original")
		{
			using (var stream = new MemoryStream())
			{
				using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
				{
					var executableName = runtimeIdentifier == "win-x64" ? "CodeScan.exe" : "CodeScan";
					foreach (var name in new[] { runtimeIdentifier + "/" + executableName, runtimeIdentifier + "/CodeScan.dll", "FilesToMoveToManaged.json" })
					{
						using (var writer = new StreamWriter(archive.CreateEntry(name).Open())) writer.Write(executableContent);
					}
				}
				return stream.ToArray();
			}
		}

		private sealed class ReleaseServer : HttpMessageHandler
		{
			internal string RuntimeIdentifier = "win-x64";
			internal string Tag = "build-1-1";
			internal long AssetId = 1;
			internal string UpdatedAt = "2026-11-18T00:00:00Z";
			internal byte[] Archive = CreateArchive("win-x64");
			internal bool Offline;
			internal bool MissingAsset;
			internal bool DownloadError;
			internal int ReleaseChecks;
			internal int Downloads;

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				if (Offline) throw new HttpRequestException("GitHub is unavailable.");
				if (request.RequestUri.Host == "api.github.com")
				{
					ReleaseChecks++;
					Assert.That(request.RequestUri.AbsolutePath, Is.EqualTo("/repos/unitystation/codescan/releases/latest"));
					Assert.That(request.Headers.UserAgent.ToString(), Is.Not.Empty);
					Assert.That(request.Headers.Accept.ToString(), Does.Contain("application/vnd.github+json"));
					return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
					{
						Content = new StringContent(JsonConvert.SerializeObject(new
						{
							tag_name = Tag,
							assets = MissingAsset ? new object[0] : new object[]
							{
								new { id = AssetId, name = RuntimeIdentifier + ".zip", updated_at = UpdatedAt,
									browser_download_url = "https://github.com/unitystation/codescan/releases/download/" + Tag + "/" + RuntimeIdentifier + ".zip" }
							}
						}))
					});
				}
				Downloads++;
				if (DownloadError) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadGateway));
				return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Archive) });
			}
		}
	}
}
