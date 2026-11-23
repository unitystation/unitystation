using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace US13.Core.SecureStuff.CustomAudio
{
	/// <summary
	/// >Data-only server audio configuration. Addresses must be unique across packs.
	/// </summary>
	[Serializable]
	public class ServerAudioManifest
	{
		public int Revision;
		public string Platform;
		public List<string> Catalogues = new();
		public Dictionary<string, List<string>> PlatformCatalogues = new();
		public Dictionary<string, string> Overrides = new();
		public List<string> LobbyTracks = new();
		public bool ReplaceLobbyPlaylist;
		public List<CustomAudioOffer> CustomAudio = new();

		public static void Validate(ServerAudioManifest manifest)
		{
			if (manifest == null)
			{
				throw new ArgumentNullException(nameof(manifest));
			}
			CustomAudioPack.ValidateOffers(manifest.CustomAudio);
			if (manifest.PlatformCatalogues == null || manifest.PlatformCatalogues.Count > 16 ||
			    manifest.PlatformCatalogues.Values.Any(value => value == null || value.Count > 32))
			{
				throw new ArgumentException("Invalid platform audio catalogues.");
			}
			if (manifest.Catalogues == null || manifest.Overrides == null || manifest.LobbyTracks == null)
			{
				throw new ArgumentException("Audio manifest collections cannot be null.");
			}
			if (manifest.Catalogues.Count > 32)
			{
				throw new ArgumentException("Audio manifest exceeds content limits.");
			}
			if (manifest.Catalogues.Concat(manifest.PlatformCatalogues.Values.SelectMany(value => value))
			    .Concat(manifest.Overrides.Keys).Concat(manifest.Overrides.Values)
			    .Concat(manifest.LobbyTracks)
			    .Any(value => string.IsNullOrWhiteSpace(value) || value.Length > 2048))
			{
				throw new ArgumentException("Audio manifest contains an invalid address.");
			}
			if (manifest.Overrides.Values.Any(manifest.Overrides.ContainsKey))
			{
				throw new ArgumentException("Audio overrides must target physical addresses, not other overrides.");
			}
		}

		public static ServerAudioManifest ReadMessage(string audioManifest, string serialiseCatalogues)
		{
			if (audioManifest?.Length > 131072 || serialiseCatalogues?.Length > 65536)
			{
				throw new JsonSerializationException("Server audio metadata size limit exceeded.");
			}

			ServerAudioManifest manifest;
			if (string.IsNullOrWhiteSpace(audioManifest) == false)
			{
				manifest = JsonConvert.DeserializeObject<ServerAudioManifest>(audioManifest, new JsonSerializerSettings
				{
					MaxDepth = 16,
					TypeNameHandling = TypeNameHandling.None
				});
			}
			else if (string.IsNullOrWhiteSpace(serialiseCatalogues) == false)
			{
				manifest = new ServerAudioManifest
				{
					Catalogues = JsonConvert.DeserializeObject<List<string>>(serialiseCatalogues,
						new JsonSerializerSettings
						{
							MaxDepth = 8,
							TypeNameHandling = TypeNameHandling.None
						})
				};
			}
			else
			{
				throw new JsonSerializationException("Server audio message contains no manifest or catalogue list.");
			}

			Validate(manifest);
			return manifest;
		}

	}
}
