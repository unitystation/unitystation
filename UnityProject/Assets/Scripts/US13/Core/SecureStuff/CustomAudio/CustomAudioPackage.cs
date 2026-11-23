using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace US13.Core.SecureStuff.CustomAudio
{
	/// <summary>Portable server-side pack index. Clients independently validate the offered bundle bytes.</summary>
	[Serializable]
	public class CustomAudioPackage
	{
		public const string FileName = "audio-pack.json";
		public List<CustomAudioPackageBundle> Bundles = new();

		public static CustomAudioPackage Parse(string json)
		{
			if (string.IsNullOrWhiteSpace(json) || json.Length > 4 * 1024 * 1024)
				throw new FormatException("Invalid custom audio package index size.");
			var package = JsonConvert.DeserializeObject<CustomAudioPackage>(json,
				new JsonSerializerSettings { MaxDepth = 16, TypeNameHandling = TypeNameHandling.None });
			if (package?.Bundles == null || package.Bundles.Count == 0)
				throw new FormatException("Custom audio package has no bundles.");
			foreach (var bundle in package.Bundles)
			{
				if (bundle == null || string.IsNullOrWhiteSpace(bundle.FileName) || bundle.FileName.Length > 255 ||
				    !bundle.FileName.EndsWith(".bundle", StringComparison.OrdinalIgnoreCase) ||
				    bundle.FileName.Any(char.IsControl) || bundle.FileName.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 ||
				    bundle.Entries == null || bundle.Entries.Count == 0 || bundle.Entries.Any(entry => entry == null))
					throw new FormatException("Invalid custom audio package bundle entry.");
			}
			return package;
		}
	}

	[Serializable]
	public class CustomAudioPackageBundle
	{
		public string FileName;
		public List<CustomAudioEntry> Entries = new();
	}
}
