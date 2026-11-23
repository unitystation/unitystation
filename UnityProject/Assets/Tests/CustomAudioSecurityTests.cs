using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using US13.Core.SecureStuff.CustomAudio;

namespace Tests
{
	public class CustomAudioSecurityTests
	{
		[Test]
		public void AudioSecurityAndTransportAreOwnedBySecureStuff()
		{
			var secureAssembly = typeof(SecureStuff.AccessFile).Assembly;
			Assert.AreEqual(secureAssembly, typeof(AudioBundlePreflight).Assembly);
			Assert.AreEqual(secureAssembly, typeof(CustomAudioPack).Assembly);
			Assert.AreEqual(secureAssembly, typeof(CustomAudioTransfer).Assembly);
			Assert.AreEqual(secureAssembly, typeof(ServerAudioManifest).Assembly);
			Assert.AreEqual(secureAssembly, typeof(CustomAudioRequestMessage).Assembly);
			Assert.AreEqual(secureAssembly, typeof(CustomAudioChunkMessage).Assembly);
		}

		private static CustomAudioOffer Offer()
		{
			var offer = new CustomAudioOffer
			{
				Sha256 = new string('a', 64), Size = 1024,
				Entries = new List<CustomAudioEntry> { new CustomAudioEntry { Address = "custom/music", AssetName = "Assets/Music.prefab" } }
			};
			offer.Id = CustomAudioPack.Identity(offer);
			return offer;
		}

		[Test]
		public void SecureManifestReaderSupportsBothWireFormatsAndRejectsInvalidContent()
		{
			var modern = ServerAudioManifest.ReadMessage("{\"LobbyTracks\":[\"custom/lobby\"]}", null);
			CollectionAssert.AreEqual(new[] { "custom/lobby" }, modern.LobbyTracks);
			var legacy = ServerAudioManifest.ReadMessage(null, "[\"https://example.org/catalog.json\"]");
			Assert.AreEqual("https://example.org/catalog.json", legacy.Catalogues[0]);
			Assert.Throws<ArgumentNullException>(() => ServerAudioManifest.ReadMessage("null", null));
			Assert.Throws<ArgumentException>(() => ServerAudioManifest.ReadMessage("{\"LobbyTracks\":[null]}", null));
			Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => ServerAudioManifest.ReadMessage(null, null));
		}

		[Test]
		public void PortablePackageIndexDoesNotRequireUnityCatalogueTypes()
		{
			var package = CustomAudioPackage.Parse("{\"Bundles\":[{\"FileName\":\"lobby.bundle\",\"Entries\":[{\"Address\":\"music/lobby\",\"AssetName\":\"Assets/Music.prefab\"}]}]}");
			Assert.AreEqual("lobby.bundle", package.Bundles[0].FileName);
			Assert.AreEqual("music/lobby", package.Bundles[0].Entries[0].Address);
		}

		[TestCase("../lobby.bundle")]
		[TestCase("C:/lobby.bundle")]
		[TestCase("folder/lobby.bundle")]
		[TestCase("code.dll")]
		public void PortablePackageIndexRejectsInvalidFilenames(string filename)
		{
			string json = Newtonsoft.Json.JsonConvert.SerializeObject(new CustomAudioPackage
			{
				Bundles = new List<CustomAudioPackageBundle>
				{
					new CustomAudioPackageBundle { FileName = filename, Entries = new List<CustomAudioEntry> { new CustomAudioEntry { Address = "music/lobby", AssetName = "Assets/Music.prefab" } } }
				}
			});
			Assert.Throws<FormatException>(() => CustomAudioPackage.Parse(json));
		}

		[Test]
		public void OfferIdentityDetectsMetadataTampering()
		{
			var offer = Offer();
			Assert.DoesNotThrow(() => CustomAudioPack.ValidateOffer(offer));
			offer.Entries[0].AssetName = "Assets/Other.prefab";
			Assert.Throws<FormatException>(() => CustomAudioPack.ValidateOffer(offer));
		}

		[Test]
		public void OfferRejectsPathAndSizeAttacks()
		{
			var offer = Offer();
			offer.Sha256 = "../../outside";
			Assert.Throws<FormatException>(() => CustomAudioPack.ValidateOffer(offer));
			offer = Offer(); offer.Size = AudioBundlePreflight.MaxBundleBytes + 1L;
			Assert.Throws<FormatException>(() => CustomAudioPack.ValidateOffer(offer));
			offer = Offer(); offer.Entries[0].AssetName = "Assets/../Outside.prefab"; offer.Id = CustomAudioPack.Identity(offer);
			Assert.Throws<FormatException>(() => CustomAudioPack.ValidateOffer(offer));
		}

		[Test]
		public void DuplicateAddressesAcrossPacksAreRejected()
		{
			var first = Offer(); var second = Offer();
			second.Sha256 = new string('b', 64); second.Id = CustomAudioPack.Identity(second);
			Assert.Throws<FormatException>(() => CustomAudioPack.ValidateOffers(new List<CustomAudioOffer> { first, second }));
		}

		[TestCase(114)] // MonoBehaviour / ScriptableObject
		[TestCase(115)] // MonoScript
		[TestCase(49)]  // TextAsset, including disguised DLL bytes
		[TestCase(43)]  // Mesh
		[TestCase(28)]  // Texture2D
		[TestCase(241)] // AudioMixerController
		public void SerializedTypesAreRejectedBeforeNativeLoading(int classId)
		{
			var exception = Assert.Throws<FormatException>(() => AudioBundlePreflight.Validate(BundleWithType(classId)));
			StringAssert.Contains("Disallowed serialized class ID", exception.Message);
		}

		[Test]
		public void OffersAllowMoreThanPreviousEntryAndBundleCounts()
		{
			var offers = new List<CustomAudioOffer>();
			for (int pack = 0; pack < 17; pack++)
			{
				var offer = Offer();
				offer.Entries = Enumerable.Range(0, 129).Select(index => new CustomAudioEntry
				{
					Address = "custom/" + pack + "/" + index,
					AssetName = "Assets/Track" + index + ".prefab"
				}).ToList();
				offer.Id = CustomAudioPack.Identity(offer);
				offers.Add(offer);
			}
			Assert.DoesNotThrow(() => CustomAudioPack.ValidateOffers(offers));
		}

		[Test]
		public void ArbitraryFilesAndTruncatedBundlesAreRejected()
		{
			Assert.Throws<FormatException>(() => AudioBundlePreflight.Validate(Encoding.ASCII.GetBytes("MZ executable payload")));
			var bytes = BundleWithType(115);
			Assert.Throws<FormatException>(() => AudioBundlePreflight.Validate(bytes.Take(bytes.Length - 1).ToArray()));
		}

		private static byte[] BundleWithType(int classId)
		{
			byte[] serialized;
			using (var stream = new MemoryStream())
			using (var w = new BinaryWriter(stream))
			{
				// Serialized file v22: big-endian header, little-endian metadata.
				w.Write(new byte[48]);
				w.Write(Encoding.ASCII.GetBytes("6000.2.10f1\0"));
				w.Write(13); w.Write((byte)1); w.Write(1); w.Write(classId);
				w.Write(new byte[64]); // Enough metadata bytes for one serialized type record.
				serialized = stream.ToArray();
				using (var header = new MemoryStream(serialized))
				using (var h = new BinaryWriter(header))
				{
					Big32(h, (uint)(serialized.Length - 48)); Big32(h, (uint)serialized.Length); Big32(h, 22); Big32(h, (uint)serialized.Length);
					h.Write(new byte[4]); Big32(h, (uint)(serialized.Length - 48)); Big64(h, serialized.Length); Big64(h, serialized.Length); Big64(h, 0);
				}
			}
			byte[] info;
			using (var stream = new MemoryStream())
			using (var w = new BinaryWriter(stream))
			{
				w.Write(new byte[16]); Big32(w, 1); Big32(w, (uint)serialized.Length); Big32(w, (uint)serialized.Length); w.Write(new byte[2]);
				Big32(w, 1); Big64(w, 0); Big64(w, serialized.Length); Big32(w, 4);
				w.Write(Encoding.ASCII.GetBytes("CAB-" + new string('a', 32) + "\0"));
				info = stream.ToArray();
			}
			using (var stream = new MemoryStream())
			using (var w = new BinaryWriter(stream))
			{
				w.Write(Encoding.ASCII.GetBytes("UnityFS\0")); Big32(w, 6); w.Write(Encoding.ASCII.GetBytes("6\0test\0"));
				long position = stream.Position;
				Big64(w, 0); Big32(w, (uint)info.Length); Big32(w, (uint)info.Length); Big32(w, 0);
				w.Write(info); w.Write(serialized);
				long length = stream.Length; stream.Position = position; Big64(w, length);
				return stream.ToArray();
			}
		}

		private static void Big32(BinaryWriter writer, uint value) => writer.Write(new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value });
		private static void Big64(BinaryWriter writer, long value) { Big32(writer, (uint)((ulong)value >> 32)); Big32(writer, (uint)value); }
	}
}
