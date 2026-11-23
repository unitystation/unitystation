using System;
using System.IO;
using NUnit.Framework;
using SecureStuff;

namespace Tests
{
	public class SafeAudioFilesystemTests
	{
		[Test]
		public void ReadsRejectTraversalAndSiblingPrefixes()
		{
			string root = AccessFile.AudioCacheRoot;
			Assert.Throws<UnauthorizedAccessException>(() => AccessFile.AudioFile.Exists(Path.Combine(root, "..", "outside.bundle")));
			Assert.Throws<UnauthorizedAccessException>(() => AccessFile.AudioFile.Exists(root + "-outside/file.bundle"));
			Assert.Throws<UnauthorizedAccessException>(() => AccessFile.AudioFile.Exists("relative.bundle"));
			Assert.Throws<UnauthorizedAccessException>(() => AccessFile.AudioFile.Exists(Path.Combine(root, "sound.bundle:payload")));
		}

		[Test]
		public void ServerPacksAreReadOnlyThroughTheRuntimeApi()
		{
			string path = Path.Combine(AccessFile.AudioServerRoot, "unchanged.bundle");
			Assert.Throws<UnauthorizedAccessException>(() => AccessFile.AudioFile.CreateNew(path));
			Assert.Throws<UnauthorizedAccessException>(() => AccessFile.AudioFile.Delete(path));
			Assert.Throws<UnauthorizedAccessException>(() => AccessFile.AudioFile.Move(path, Path.Combine(AccessFile.AudioCacheRoot, "unused.bundle")));
		}

		[Test]
		public void MissingFilesReportSecureFilesystemErrors()
		{
			string file = AccessFile.AudioFile.CachePath(Guid.NewGuid().ToString("N") + ".bundle");
			Assert.Throws<SafeFilesystemException>(() => AccessFile.AudioFile.OpenRead(file));
		}

		[Test]
		public void CacheStreamsRespectReadLimitsAndMoves()
		{
			AccessFile.AudioDirectory.CreateDirectory(AccessFile.AudioCacheRoot);
			string first = Path.Combine(AccessFile.AudioCacheRoot, Guid.NewGuid().ToString("N") + ".part");
			string second = Path.Combine(AccessFile.AudioCacheRoot, Guid.NewGuid().ToString("N") + ".bundle");
			try
			{
				using (var stream = AccessFile.AudioFile.CreateNew(first)) stream.Write(new byte[] { 1, 2, 3 }, 0, 3);
				Assert.Throws<SafeFilesystemException>(() => AccessFile.AudioFile.ReadAllBytes(first, 2));
				CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, AccessFile.AudioFile.ReadAllBytes(first, 3));
				AccessFile.AudioFile.Move(first, second);
				Assert.IsFalse(AccessFile.AudioFile.Exists(first));
				Assert.AreEqual(3, AccessFile.AudioFile.GetInfo(second).Length);
				Assert.Throws<ArgumentException>(() => AccessFile.AudioDirectory.GetFilesInfo(AccessFile.AudioCacheRoot, "../*"));
			}
			finally
			{
				AccessFile.AudioFile.Delete(first);
				AccessFile.AudioFile.Delete(second);
			}
		}
	}
}
