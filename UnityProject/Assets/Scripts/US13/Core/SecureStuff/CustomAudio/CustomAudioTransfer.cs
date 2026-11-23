using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Logs;
using Mirror;
using Newtonsoft.Json;
using SecureStuff;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using AudioFile = SecureStuff.AccessFile.AudioFile;
using AudioDirectory = SecureStuff.AccessFile.AudioDirectory;
using AudioFileInfo = SecureStuff.AccessFile.AudioFileInfo;

namespace US13.Core.SecureStuff.CustomAudio
{
	public static class CustomAudioTransfer
	{
		public const int ChunkBytes = 16 * 1024;
		public const int WindowChunks = 4;
		private const long CacheLimit = 512L * 1024 * 1024;

		private sealed class Source
		{
			public CustomAudioOffer Offer;
			public string Path;
		}

		private sealed class Sending
		{
			public string Id, Token;
			public long Offset, Budget;
		}

		private sealed class Receiving
		{
			public CustomAudioOffer Offer;
			public string Token, Temp;
			public NetworkConnection Connection;
			public AccessFile.AudioStream Stream;
			public long Offset, RequestedEnd;
			public float LastProgress;
			public readonly TaskCompletionSource<bool> Completion = new();
		}

		private static readonly Dictionary<string, Source> Sources = new();
		private static readonly Dictionary<NetworkConnection, Sending> Senders = new();
		private static readonly SemaphoreSlim Preparing = new(1, 1);
		private static readonly HashSet<string> ProtectedHashes = new();
		private static Receiving receiving;
		private static int epoch;
		private static string sourceStamp;
		private static bool missingRootReported;
		private static List<CustomAudioOffer> serverOffers = new();
		public static string CacheRoot => AccessFile.AudioCacheRoot;

		// A headless Mirror host has a local client, but still must not deserialize client-platform bundles.
		public static bool IsDedicatedServer => Application.isBatchMode;

		/// <summary>
		/// Register the server's own trusted pack metadata without initializing Addressables or loading bundles.
		/// </summary>
		public static Dictionary<string, IResourceLocator> DiscoverServerLocators()
		{
			var locators = new Dictionary<string, IResourceLocator>();
			foreach (var offer in DiscoverServerPacks())
			{
				locators["mirror-audio://" + offer.Id] = CustomAudioPack.CreateLocator(offer, Sources[offer.Id].Path);
			}
			return locators;
		}

		/// <summary>Only the server reads its own catalogue; clients never deserialize its provider/type metadata.</summary>
		public static List<CustomAudioOffer> DiscoverServerPacks()
		{
			string root = AccessFile.AudioServerRoot;
			if (!AudioDirectory.Exists(root))
			{
				if (!missingRootReported) Loggy.Info($"Custom audio folder not found: {root}. No custom packs will be offered.", Category.Addressables);
				missingRootReported = true;
				Sources.Clear();
				serverOffers.Clear();
				sourceStamp = null;
				return new List<CustomAudioOffer>();
			}
			missingRootReported = false;

			List<string> files = SafeFiles(root).ToList();
			string stamp = string.Join("|",
				files.OrderBy(path => path, StringComparer.Ordinal).Select(path =>
					path + ":" + AudioFile.GetInfo(path).Length + ":" + AudioFile.GetLastWriteTimeUtc(path).Ticks));
			if (stamp == sourceStamp) return serverOffers.ToList();
			Loggy.Info($"Scanning custom audio packs in {root}.", Category.Addressables);
			var sources = new Dictionary<string, Source>();
			foreach (string indexPath in files.Where(path => AudioFile.GetInfo(path).Name.Equals(CustomAudioPackage.FileName, StringComparison.OrdinalIgnoreCase)))
			{
				var package = CustomAudioPackage.Parse(AudioFile.ReadAllText(indexPath, 4 * 1024 * 1024));
				foreach (var bundle in package.Bundles)
				{
					string file = FindBundleFile(indexPath, bundle.FileName, files);
					var source = CreateSource(file, bundle.Entries);
					sources[source.Offer.Id] = source;
				}
			}
			foreach (string catalogPath in files.Where(path =>
				         AudioFile.GetInfo(path).Name.StartsWith("catalog", StringComparison.OrdinalIgnoreCase) &&
				         path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
			{
				if (files.Contains(AudioFile.SiblingPath(catalogPath, CustomAudioPackage.FileName), StringComparer.OrdinalIgnoreCase)) continue;
#if ENABLE_JSON_CATALOG
				if (AudioFile.GetInfo(catalogPath).Length > 4 * 1024 * 1024)
					throw new FormatException("Custom audio catalogue is too large.");
				var data = JsonUtility.FromJson<ContentCatalogData>(AudioFile.ReadAllText(catalogPath, 4 * 1024 * 1024));
				if (data == null) throw new FormatException("Invalid CustomAudio catalogue.");
				ResourceLocationMap locator = data.CreateLocator();
				var grouped = new Dictionary<string, List<CustomAudioEntry>>();
				foreach (string address in CustomAudioPack.EnumerateAudioAddresses(new[] { locator }))
				{
					locator.Locate(address, typeof(GameObject), out IList<IResourceLocation> locations);
					if (locations.Count != 1)
						throw new FormatException("Custom audio addresses must identify one prefab.");
					IResourceLocation location = locations[0];
					var dependencies = new HashSet<IResourceLocation>();
					CollectBundles(location, dependencies, new HashSet<IResourceLocation>());
					if (dependencies.Count != 1)
						throw new FormatException("Custom audio prefabs must have one self-contained bundle.");
					string internalId = dependencies.Single().InternalId.Replace('\\', '/');
					string filename = internalId.Substring(internalId.LastIndexOf('/') + 1);
					if (!filename.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase))
						throw new FormatException("Invalid custom audio bundle filename.");
					string bundle = FindBundleFile(catalogPath, filename, files);
					if (!grouped.TryGetValue(bundle, out List<CustomAudioEntry> entries))
						grouped[bundle] = entries = new List<CustomAudioEntry>();
					entries.Add(new CustomAudioEntry { Address = address, AssetName = location.InternalId });
				}

				foreach (KeyValuePair<string, List<CustomAudioEntry>> group in grouped)
				{
					var source = CreateSource(group.Key, group.Value);
					sources[source.Offer.Id] = source;
				}
#else
				throw new FormatException($"Pack at {catalogPath} has no {CustomAudioPackage.FileName}. Rebuild it with the Lobby Music Pack tool so this server can read its metadata independently of Unity's catalogue format.");
#endif
			}

			List<CustomAudioOffer> offers = sources.Values.Select(source => source.Offer)
				.OrderBy(offer => offer.Id, StringComparer.Ordinal).ToList();
			CustomAudioPack.ValidateOffers(offers);
			// Prevent an offer from exceeding Mirror's ordinary reliable message budget.
			if (Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(offers)) > 32768)
				throw new FormatException("Custom audio metadata exceeds 32 KB.");
			Sources.Clear();
			foreach (KeyValuePair<string, Source> pair in sources) Sources.Add(pair.Key, pair.Value);
			serverOffers = offers;
			sourceStamp = stamp;
			if (offers.Count == 0 && files.Any(path => path.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase)))
				Loggy.Warning($"No custom audio addresses found in {root}. Copy each pack's {CustomAudioPackage.FileName} (or compatible catalog*.json) alongside its .bundle files.", Category.Addressables);
			else
				Loggy.Info($"Custom audio discovered in {root}: {offers.Count} bundles, {offers.Sum(offer => offer.Entries.Count)} addresses, {offers.Sum(offer => offer.Size)} bytes.", Category.Addressables);
			return offers.ToList();
		}

		private static string FindBundleFile(string metadata, string filename, List<string> files)
		{
			string candidate = AudioFile.SiblingPath(metadata, filename);
			if (files.Contains(candidate, StringComparer.Ordinal)) return candidate;
			var matches = files.Where(path => string.Equals(path, candidate, StringComparison.OrdinalIgnoreCase)).ToList();
			if (matches.Count != 1) throw new FormatException($"Custom audio bundle '{filename}' is missing or ambiguous beside {metadata}.");
			return matches[0];
		}

		private static Source CreateSource(string file, List<CustomAudioEntry> entries)
		{
			var info = AudioFile.GetInfo(file);
			if (info.Length > AudioBundlePreflight.MaxBundleBytes) throw new FormatException("Custom audio bundle is too large.");
			var offer = new CustomAudioOffer { Sha256 = CustomAudioPack.HashFile(file), Size = info.Length, Entries = entries };
			offer.Id = CustomAudioPack.Identity(offer);
			CustomAudioPack.ValidateOffer(offer);
			return new Source { Offer = offer, Path = file };
		}

		private static IEnumerable<string> SafeFiles(string root)
		{
			var directories = new Stack<(string path, int depth)>();
			directories.Push((root, 0));
			while (directories.Count > 0)
			{
				(string path, int depth) directory = directories.Pop();
				foreach (string path in AudioDirectory.GetFiles(directory.path))
				{
					yield return path;
				}

				foreach (string path in AudioDirectory.GetDirectories(directory.path))
				{
					if (directory.depth >= 3)
						throw new FormatException("CustomAudio folder nesting limit exceeded.");
					directories.Push((path, directory.depth + 1));
				}
			}
		}

		private static void CollectBundles(IResourceLocation location, HashSet<IResourceLocation> bundles,
			HashSet<IResourceLocation> visited)
		{
			if (!visited.Add(location)) return;
			if (location.ResourceType == typeof(IAssetBundleResource)) bundles.Add(location);
			if (location.HasDependencies)
				foreach (IResourceLocation dependency in location.Dependencies)
					CollectBundles(dependency, bundles, visited);
		}

		public static void Serve(NetworkConnection connection, string id, string token, long offset, int windowBytes = ChunkBytes * WindowChunks)
		{
			if (connection == null || !connection.isAuthenticated || !CustomAudioPack.IsHash(id) || token == null ||
			    token.Length != 32 ||
			    !token.All(Uri.IsHexDigit)) return;
			if (offset == -1)
			{
				if (Senders.TryGetValue(connection, out Sending canceled) && canceled.Id == id &&
				    canceled.Token == token)
				{
					canceled.Id = null;
					canceled.Token = null;
					canceled.Offset = 0;
				}

				return;
			}

			if (!Sources.TryGetValue(id, out Source source))
			{
				CustomAudioChunkMessage.SendTo(connection, new CustomAudioChunkMessage.NetMessage { Id = id, Token = token, Error = true });
				return;
			}
			if (windowBytes <= 0 || windowBytes > ChunkBytes * WindowChunks) return;
			if (offset >= 0 && offset < source.Offer.Size && windowBytes < Math.Min(1024L, source.Offer.Size - offset)) return;
			if (!Senders.TryGetValue(connection, out Sending state))
				Senders[connection] = state = new Sending { Budget = CustomAudioPack.MaxTotalBytes };
			bool previousComplete = state.Id == null || !Sources.TryGetValue(state.Id, out Source previous) ||
			                        state.Offset == previous.Offer.Size;
			if (offset == 0 && previousComplete && (state.Id != id || state.Token != token))
			{
				state.Id = id;
				state.Token = token;
				state.Offset = 0;
			}

			if (state.Id != id || state.Token != token || state.Offset != offset || offset < 0 ||
			    offset >= source.Offer.Size || state.Budget <= 0) return;
			try
			{
				int chunkSize = GetTransportChunkSize();
				if (offset == 0) Loggy.Info($"Serving custom audio {id}: {source.Offer.Size} bytes, chunks up to {chunkSize} bytes.", Category.Addressables);
				using (AccessFile.AudioStream file = AudioFile.OpenRead(source.Path))
				{
					if (file.Length != source.Offer.Size)
						throw new SafeFilesystemException("Custom audio changed during transfer.");
					file.Position = offset;
					long end = Math.Min(file.Length, offset + windowBytes);
					while (state.Offset < end)
					{
						var size = (int)Math.Min(chunkSize, end - state.Offset);
						if (size > state.Budget)
							throw new SafeFilesystemException("Custom audio connection transfer budget exceeded.");
						var bytes = new byte[size];
						var read = 0;
						while (read < size)
						{
							int n = file.Read(bytes, read, size - read);
							if (n == 0) throw new SafeFilesystemException("Unexpected end of audio file.");
							read += n;
						}

						CustomAudioChunkMessage.SendTo(connection,
							new CustomAudioChunkMessage.NetMessage
								{ Id = id, Token = token, Offset = state.Offset, Data = bytes });
						state.Offset += size;
						state.Budget -= size;
					}
					if (state.Offset == file.Length) Loggy.Info($"Custom audio {id} transfer sent completely.", Category.Addressables);
				}
			}
			catch (SafeFilesystemException exception)
			{
				Loggy.Warning("Custom audio transfer stopped: " + exception.Message, Category.Addressables);
				CustomAudioChunkMessage.SendTo(connection,
					new CustomAudioChunkMessage.NetMessage { Id = id, Token = token, Error = true });
			}
		}

		public static void ForgetServerClient(NetworkConnection connection)
		{
			Senders.Remove(connection);
		}

		public static void ResetServer()
		{
			Sources.Clear();
			Senders.Clear();
			serverOffers.Clear();
			sourceStamp = null;
		}

		public static void CancelClient()
		{
			epoch++;
			FailReceiving(new OperationCanceledException("Audio session ended."));
			ProtectedHashes.Clear();
		}

		private static void FailReceiving(Exception exception)
		{
			Receiving state = receiving;
			receiving = null;
			if (state == null) return;
			if (NetworkClient.isConnected && ReferenceEquals(state.Connection, NetworkClient.connection))
				try
				{
					CustomAudioRequestMessage.Send(new CustomAudioRequestMessage.NetMessage
						{ Id = state.Offer.Id, Token = state.Token, Offset = -1 });
				}
				catch (InvalidOperationException)
				{
				}

			try
			{
				state.Stream.Dispose();
			}
			catch (SafeFilesystemException)
			{
			}

			try
			{
				if (AudioFile.Exists(state.Temp)) AudioFile.Delete(state.Temp);
			}
			catch (SafeFilesystemException)
			{
			}

			state.Completion.TrySetException(exception);
		}

		public static void Receive(CustomAudioChunkMessage.NetMessage message)
		{
			Receiving state = receiving;
			if (state == null || !ReferenceEquals(state.Connection, NetworkClient.connection) ||
			    message.Id != state.Offer.Id || message.Token != state.Token) return;
			try
			{
				if (message.Error) throw new InvalidOperationException($"Server could not provide custom audio {message.Id}. Check the server's pack discovery and transfer logs.");
				if (message.Data == null || message.Data.Length == 0 ||
				    message.Data.Length > ChunkBytes ||
				    message.Offset != state.Offset || message.Data.Length > state.RequestedEnd - state.Offset)
					throw new FormatException("Invalid custom audio transfer chunk.");
				state.Stream.Write(message.Data, 0, message.Data.Length);
				state.Offset += message.Data.Length;
				state.LastProgress = Time.realtimeSinceStartup;
				if (state.Offset == state.Offer.Size)
				{
					state.Stream.Dispose();
					receiving = null;
					state.Completion.TrySetResult(true);
				}
				else if (state.Offset == state.RequestedEnd)
				{
					RequestWindow(state);
				}
			}
			catch (Exception exception)
			{
				FailReceiving(exception);
			}
		}

		private static void RequestWindow(Receiving state)
		{
			state.RequestedEnd = Math.Min(state.Offer.Size, state.Offset + (long)GetTransportChunkSize() * WindowChunks);
			CustomAudioRequestMessage.Send(new CustomAudioRequestMessage.NetMessage
				{ Id = state.Offer.Id, Token = state.Token, Offset = state.Offset, WindowBytes = (int)(state.RequestedEnd - state.Offset) });
		}

		private static int GetTransportChunkSize()
		{
			// Reserve room for the strings, offset, byte-array length and error flag in the Mirror message.
			int available = NetworkMessages.MaxContentSize(0) - 256;
			if (available < 256) throw new SafeFilesystemException("Transport cannot fit a custom audio message.");
			return Math.Min(ChunkBytes, available);
		}

		public static async Task<Dictionary<string, IResourceLocator>> PrepareAsync(List<CustomAudioOffer> offers)
		{
			CustomAudioPack.ValidateOffers(offers);
			if (offers.Count == 0) return new Dictionary<string, IResourceLocator>();
			int session = epoch;
			NetworkConnectionToServer connection = NetworkClient.connection;

			bool Current()
			{
				return session == epoch && (NetworkServer.active ||
				                            (NetworkClient.isConnected &&
				                             ReferenceEquals(connection, NetworkClient.connection)));
			}

			await Preparing.WaitAsync();
			try
			{
				if (!Current()) throw new OperationCanceledException();
				var locators = new Dictionary<string, IResourceLocator>();
				if (IsDedicatedServer)
				{
					// A dedicated server distributes client-platform bundles without opening them in Unity.
					// Only its own discovered files qualify; every receiving client still performs full validation.
					foreach (CustomAudioOffer offer in offers)
					{
						if (!Sources.TryGetValue(offer.Id, out Source source))
							throw new FormatException("Unknown local server audio pack.");
						locators["mirror-audio://" + offer.Id] = CustomAudioPack.CreateLocator(offer, source.Path);
					}

					return locators;
				}

				AudioDirectory.CreateDirectory(CacheRoot);
				foreach (AudioFileInfo partial in AudioDirectory.GetFilesInfo(CacheRoot, "*.part"))
				{
					if (partial.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-1) ||
					    !Guid.TryParseExact(partial.NameWithoutExtension, "N", out _)) continue;
					try
					{
						partial.Delete();
					}
					catch (SafeFilesystemException)
					{
					}
				}

				var keep = new HashSet<string>(offers.Select(offer => offer.Sha256));
				foreach (CustomAudioOffer offer in offers)
				{
					if (!Current()) throw new OperationCanceledException();
					string file = AudioFile.CachePath(offer.Sha256 + ".bundle");
					bool cached = AudioFile.Exists(file) && await Task.Run(() =>
						AudioFile.GetInfo(file).Length == offer.Size && CustomAudioPack.HashFile(file) == offer.Sha256);
					Loggy.Info($"Custom audio {offer.Id}: {(cached ? "using cached bundle" : "requesting bundle")} ({offer.Size} bytes).", Category.Addressables);
					if (!cached)
					{
						if (AudioFile.Exists(file)) AudioFile.Delete(file);
						MakeCacheRoom(offer.Size, keep);
						string temp = AudioFile.CachePath(Guid.NewGuid().ToString("N") + ".part");
						try
						{
							if (NetworkServer.active && Sources.TryGetValue(offer.Id, out Source source))
							{
								AudioFile.Copy(source.Path, temp);
							}
							else
							{
								var state = new Receiving
								{
									Offer = offer, Token = Guid.NewGuid().ToString("N"), Temp = temp,
									Connection = connection,
									Stream = AudioFile.CreateNew(temp),
									LastProgress = Time.realtimeSinceStartup
								};
								try
								{
									receiving = state;
									RequestWindow(state);
									while (!state.Completion.Task.IsCompleted)
									{
										if (!Current()) FailReceiving(new OperationCanceledException());
										else if (Time.realtimeSinceStartup - state.LastProgress > 30f)
											FailReceiving(new TimeoutException("Custom audio download timed out."));
										await UniTask.NextFrame();
									}

									await state.Completion.Task;
								}
								finally
								{
									if (ReferenceEquals(receiving, state))
									{
										FailReceiving(new OperationCanceledException());
										_ = state.Completion.Task.Exception;
									}
								}
							}

							if (!Current() || !await Task.Run(() =>
								    AudioFile.GetInfo(temp).Length == offer.Size &&
								    CustomAudioPack.HashFile(temp) == offer.Sha256))
								throw new FormatException("Custom audio download hash mismatch.");
							try
							{
								AudioFile.Move(temp, file);
							}
							catch (SafeFilesystemException)
							{
								// Another local client process may have completed the same content hash.
								if (!AudioFile.Exists(file) || CustomAudioPack.HashFile(file) != offer.Sha256) throw;
							}
						}
						finally
						{
							if (AudioFile.Exists(temp)) AudioFile.Delete(temp);
						}
					}

					try
					{
						locators["mirror-audio://" + offer.Id] =
							await CustomAudioPack.ValidateAndCreateLocatorAsync(offer, file, Current);
					}
					catch
					{
						if (AudioFile.Exists(file)) AudioFile.Delete(file);
						throw;
					}

					ProtectedHashes.Add(offer.Sha256);
					Loggy.Info($"Custom audio {offer.Id} validated: {offer.Entries.Count} addresses ready.", Category.Addressables);
				}

				IList<IResourceProvider> providers =
					UnityEngine.AddressableAssets.Addressables.ResourceManager.ResourceProviders;
				if (!providers.Any(provider => provider.ProviderId == typeof(AssetBundleProvider).FullName))
					providers.Add(new AssetBundleProvider());
				if (!providers.Any(provider => provider.ProviderId == typeof(BundledAssetProvider).FullName))
					providers.Add(new BundledAssetProvider());
				return locators;
			}
			finally
			{
				Preparing.Release();
			}
		}

		private static void MakeCacheRoom(long size, HashSet<string> keep)
		{
			AudioFileInfo[] files = AudioDirectory.GetFilesInfo(CacheRoot, "*.bundle");
			long used = files.Sum(file => file.Length) +
			            AudioDirectory.GetFilesInfo(CacheRoot, "*.part").Sum(file => file.Length);
			foreach (AudioFileInfo file in files.OrderBy(file => file.LastWriteTimeUtc))
			{
				if (used + size <= CacheLimit) break;
				string hash = file.NameWithoutExtension;
				if (!CustomAudioPack.IsHash(hash) || keep.Contains(hash) || ProtectedHashes.Contains(hash)) continue;
				long length = file.Length;
				try
				{
					file.Delete();
					used -= length;
				}
				catch (SafeFilesystemException)
				{
				}
			}

			if (used + size > CacheLimit) throw new SafeFilesystemException("Custom audio cache budget exceeded.");
		}
	}
}
