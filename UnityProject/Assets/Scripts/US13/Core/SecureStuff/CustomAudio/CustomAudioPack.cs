using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SecureStuff;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using AudioFile = SecureStuff.AccessFile.AudioFile;
using Object = UnityEngine.Object;

namespace US13.Core.SecureStuff.CustomAudio
{
	public static class CustomAudioPack
	{
		public const string LocalTestCataloguePrefix = "streaming-assets://AudioModdingTest/";

		public static IEnumerable<string> EnumerateAudioAddresses(IEnumerable<IResourceLocator> locators)
		{
			// Scan built maps only; editor settings locators may invoke AssetDatabase on missing assets.
			return locators.OfType<ResourceLocationMap>().Distinct().SelectMany(locator => locator.Keys.OfType<string>().Where(key =>
				!Guid.TryParse(key, out _) && locator.Locate(key, typeof(GameObject), out var locations) &&
				locations.Any(location => location.PrimaryKey == key))).Distinct();
		}

		public const long MaxTotalBytes = 256L * 1024 * 1024;
		private static readonly Dictionary<string, HashSet<string>> Validated = new();

		public static string HashFile(string path)
		{
			return Hex(AudioFile.HashSha256(path));
		}

		public static string Identity(CustomAudioOffer offer)
		{
			string content = offer.Sha256 + "\n" + offer.Size.ToString(CultureInfo.InvariantCulture) + "\n" +
			                 string.Join("\n",
				                 offer.Entries.OrderBy(entry => entry.Address, StringComparer.Ordinal)
					                 .Select(entry => entry.Address + "\n" + entry.AssetName));
			using (var hash = SHA256.Create())
			{
				return Hex(hash.ComputeHash(Encoding.UTF8.GetBytes(content)));
			}
		}

		private static string Hex(byte[] data)
		{
			return BitConverter.ToString(data).Replace("-", "").ToLowerInvariant();
		}

		public static bool IsHash(string value)
		{
			return value != null && value.Length == 64 &&
			       value.All(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
		}

		public static void ValidateOffer(CustomAudioOffer offer)
		{
			if (offer == null || !IsHash(offer.Id) || !IsHash(offer.Sha256) || offer.Size <= 0 ||
			    offer.Size > AudioBundlePreflight.MaxBundleBytes ||
			    offer.Entries == null || offer.Entries.Count == 0)
			{
				throw new FormatException("Invalid custom audio offer.");
			}
			var addresses = new HashSet<string>(StringComparer.Ordinal);
			foreach (CustomAudioEntry entry in offer.Entries)
			{
				if (entry == null || !SafeName(entry.Address, 256) || !SafeName(entry.AssetName, 512) ||
				    !entry.AssetName.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
				    !entry.AssetName.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
				    entry.AssetName.Split('/').Contains("..") || !addresses.Add(entry.Address))
				{
					throw new FormatException("Invalid/duplicate audio asset address.");
				}
			}
			if (Identity(offer) != offer.Id)
			{
				throw new FormatException("Audio offer identity does not match its contents.");
			}
		}

		private static bool SafeName(string value, int max)
		{
			return !string.IsNullOrWhiteSpace(value) && value.Length <= max &&
			       !value.Any(char.IsControl) && !value.Contains("\\") && !value.Contains(":") &&
			       !value.StartsWith("/", StringComparison.Ordinal);
		}

		public static void ValidateOffers(List<CustomAudioOffer> offers)
		{
			if (offers == null) throw new FormatException("Missing custom audio offers.");
			long size = 0;
			var addresses = new HashSet<string>(StringComparer.Ordinal);
			var ids = new HashSet<string>();
			foreach (CustomAudioOffer offer in offers)
			{
				ValidateOffer(offer);
				size += offer.Size;
				if (size > MaxTotalBytes || !ids.Add(offer.Id))
					throw new FormatException("Custom audio transfer limit exceeded.");
				foreach (CustomAudioEntry entry in offer.Entries)
					if (!addresses.Add(entry.Address))
						throw new FormatException("Ambiguous custom audio address.");
			}
		}

		/// <summary>
		/// Raw type preflight runs before AssetBundle.LoadFromFile or any native object deserialization.
		/// </summary>
		public static async Task<IResourceLocator> ValidateAndCreateLocatorAsync(CustomAudioOffer offer, string file, Func<bool> current)
		{
			AccessFile.InitialiseAudioPaths();
			ValidateOffer(offer);
			bool unchanged = await Task.Run(() => AudioFile.GetInfo(file).Length == offer.Size && HashFile(file) == offer.Sha256);
			if (!unchanged || !current())
			{
				throw new FormatException("Audio bundle hash mismatch or stale session.");
			}
			if (!Validated.TryGetValue(offer.Sha256, out HashSet<string> prefabs))
			{
				await Task.Run(() => AudioBundlePreflight.Validate(file));
				if (!current()) throw new OperationCanceledException();
				AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(file);
				while (!request.isDone) await UniTask.NextFrame();
				AssetBundle bundle = request.assetBundle;
				if (bundle == null) throw new FormatException("Unity could not open the validated audio bundle.");
				try
				{
					if (bundle.isStreamedSceneAssetBundle)
					{
						throw new FormatException("Scene bundles are not allowed.");
					}
					AssetBundleRequest assets = bundle.LoadAllAssetsAsync();
					while (!assets.isDone)
					{
						await UniTask.NextFrame();
					}
					foreach (Object asset in assets.allAssets)
					{
						if (asset == null) throw new FormatException("Missing audio asset.");
						if (asset is AudioClip clip)
						{
							if (clip.loadType != AudioClipLoadType.Streaming || clip.length <= 0 ||
							    clip.length > 1800 || clip.channels > 8 || clip.frequency > 192000)
								throw new FormatException("Audio clip exceeds playback limits.");
						}
						else if (asset is GameObject o)
						{
							ValidatePrefab(o);
						}
						else
						{
							throw new FormatException("Disallowed asset type: " + asset.GetType().Name);
						}
					}

					prefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (string name in bundle.GetAllAssetNames())
					{
						var prefab = bundle.LoadAsset<GameObject>(name);
						if (prefab == null)
							throw new FormatException("Named bundle assets must be audio prefabs.");
						ValidatePrefab(prefab);
						prefabs.Add(name);
					}

					if (Validated.Count >= 128) Validated.Clear();
					Validated[offer.Sha256] = prefabs;
				}
				finally
				{
					bundle.Unload(true);
				}
			}

			if (current() == false)
			{
				throw new OperationCanceledException();
			}

			foreach (CustomAudioEntry entry in offer.Entries)
			{
				if (prefabs.Contains(entry.AssetName) == false)
				{
					throw new FormatException("Offered prefab is not present in the validated bundle.");
				}
			}

			return CreateLocator(offer, file);
		}

		// Also used for the dedicated server's own trusted files. Clients must use ValidateAndCreateLocatorAsync.
		internal static IResourceLocator CreateLocator(CustomAudioOffer offer, string file)
		{
			var locator = new ResourceLocationMap("MirrorAudio_" + offer.Id);
			var bundleLocation = new ResourceLocationBase("bundle/" + offer.Sha256, file,
				typeof(AssetBundleProvider).FullName, typeof(IAssetBundleResource))
			{
				Data = new AssetBundleRequestOptions
				{
					BundleName = offer.Sha256,
					BundleSize = offer.Size,
					Timeout = 30
				}
			};
			foreach (CustomAudioEntry entry in offer.Entries)
			{
				locator.Add(
					entry.Address,
					new ResourceLocationBase(entry.Address, entry.AssetName, typeof(BundledAssetProvider).FullName,
						typeof(GameObject),
						bundleLocation)
					);
			}
			return locator;
		}

		private static void ValidatePrefab(GameObject prefab)
		{
			if (prefab.transform.childCount != 0 || prefab.GetComponent<AudioSource>() == null)
			{
				throw new FormatException("Audio prefabs must contain a single root AudioSource.");
			}

			foreach (Component component in prefab.GetComponents<Component>())
			{
				if (component == null || (component.GetType() != typeof(UnityEngine.Transform) && component.GetType() != typeof(AudioSource)))
				{
					throw new FormatException("Scripts and non-audio components are not allowed.");
				}
			}

			var source = prefab.GetComponent<AudioSource>();
			if (source.clip == null || source.outputAudioMixerGroup != null || prefab.GetComponents<AudioSource>().Length != 1)
			{
				throw new FormatException("Audio prefab has missing/external references.");
			}
		}
	}
}
