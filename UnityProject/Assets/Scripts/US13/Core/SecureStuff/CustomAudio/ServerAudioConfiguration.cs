using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace US13.Core.SecureStuff.CustomAudio
{
	/// <summary>Local audio policy, separate from server connectivity configuration and network manifests.</summary>
	[Serializable]
	public class ServerAudioConfiguration
	{
		public const string FileName = "audio-manifest.json";
		public ServerAudioManifest AudioManifest = new();
		public bool EnableLocalAudio;

		public static ServerAudioConfiguration Parse(string json)
		{
			if (string.IsNullOrWhiteSpace(json) || json.Length > 131072)
				throw new JsonSerializationException("Invalid audio configuration size.");
			var config = JsonConvert.DeserializeObject<ServerAudioConfiguration>(json,
				new JsonSerializerSettings { MaxDepth = 16, TypeNameHandling = TypeNameHandling.None });
			if (config == null) throw new JsonSerializationException("Audio configuration cannot be null.");
			if (config.AudioManifest != null) ServerAudioManifest.Validate(config.AudioManifest);
			return config;
		}

		public static ServerAudioConfiguration FromLegacy(string json)
		{
			var root = JObject.Parse(json);
			var audio = new JObject();
			if (root.TryGetValue("AudioManifest", out var manifest)) audio["AudioManifest"] = manifest.DeepClone();
			audio["EnableLocalAudio"] = (root[CustomAudioPack.LocalAudioEnabledKey] ?? root[CustomAudioPack.LegacyLocalAudioEnabledKey])?.DeepClone() ?? new JValue(false);
			return Parse(audio.ToString(Formatting.None));
		}

		public static ServerAudioConfiguration Load()
		{
			if (global::SecureStuff.AccessFile.Exists(FileName))
				return Parse(global::SecureStuff.AccessFile.Load(FileName));
			return global::SecureStuff.AccessFile.Exists("config.json")
				? FromLegacy(global::SecureStuff.AccessFile.Load("config.json"))
				: new ServerAudioConfiguration();
		}

		public static JObject LoadDocument()
		{
			if (!global::SecureStuff.AccessFile.Exists(FileName)) return JObject.FromObject(Load());
			string json = global::SecureStuff.AccessFile.Load(FileName);
			Parse(json);
			return JObject.Parse(json);
		}

		public static void SaveDocument(JObject document)
		{
			var stored = (JObject)document.DeepClone();
			if (stored[CustomAudioPack.LocalAudioEnabledKey] == null && stored.TryGetValue(CustomAudioPack.LegacyLocalAudioEnabledKey, out var legacy))
				stored[CustomAudioPack.LocalAudioEnabledKey] = legacy.DeepClone();
			stored.Remove(CustomAudioPack.LegacyLocalAudioEnabledKey);
			Parse(stored.ToString(Formatting.None));
			global::SecureStuff.AccessFile.Save(FileName, stored.ToString());
		}
	}
}
