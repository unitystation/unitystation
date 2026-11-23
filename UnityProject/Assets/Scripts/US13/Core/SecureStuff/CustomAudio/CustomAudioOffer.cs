using System;
using System.Collections.Generic;

namespace US13.Core.SecureStuff.CustomAudio
{
	[Serializable]
	public class CustomAudioOffer
	{
		public string Id;
		public string Sha256;
		public long Size;
		public List<CustomAudioEntry> Entries = new();
	}
}