using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SecureStuff
{
	/// <summary>An I/O failure reported through the secure filesystem boundary.</summary>
	public sealed class SafeFilesystemException : Exception
	{
		public SafeFilesystemException(string message) : base(message) { }
		internal SafeFilesystemException(IOException cause) : base(cause.Message, cause) { }
	}

	public static partial class AccessFile
	{
		private static T AudioIo<T>(Func<T> operation)
		{
			try { return operation(); }
			catch (IOException exception) { throw new SafeFilesystemException(exception); }
		}

		private static void AudioIo(Action operation) => AudioIo(() => { operation(); return true; });

		/// <summary>A stream opened under the audio filesystem policy; exposes no raw filesystem handle.</summary>
		public sealed class AudioStream : IDisposable
		{
			private readonly FileStream stream;
			internal AudioStream(FileStream stream) { this.stream = stream; }
			public long Length => AudioIo(() => stream.Length);
			public long Position { get => AudioIo(() => stream.Position); set => AudioIo(() => { stream.Position = value; }); }
			public int Read(byte[] bytes, int offset, int count) => AudioIo(() => stream.Read(bytes, offset, count));
			public int ReadByte() => AudioIo(() => stream.ReadByte());
			public void Write(byte[] bytes, int offset, int count) => AudioIo(() => stream.Write(bytes, offset, count));
			internal byte[] HashSha256() => AudioIo(() =>
			{
				using (var hash = System.Security.Cryptography.SHA256.Create()) return hash.ComputeHash(stream);
			});
			public void Dispose() => AudioIo(() => stream.Dispose());
		}

		private sealed class AudioRoots
		{
			public readonly string Server, Cache;
			public AudioRoots(string server, string cache) { Server = server; Cache = cache; }
		}

		private static volatile AudioRoots audioRoots;

		/// <summary>Capture Unity paths on the main thread before starting background file work.</summary>
		public static void InitialiseAudioPaths()
		{
			if (audioRoots != null) return;
			audioRoots = new AudioRoots(
				Path.GetFullPath(Path.Combine(Application.streamingAssetsPath, "CustomAudio")),
				Path.GetFullPath(Path.Combine(Application.persistentDataPath, "CustomAudioCache")));
		}

		public static string AudioServerRoot { get { InitialiseAudioPaths(); return audioRoots.Server; } }
		public static string AudioCacheRoot { get { InitialiseAudioPaths(); return audioRoots.Cache; } }

		private static string ValidateAudioPath(string path, bool write = false)
		{
			InitialiseAudioPaths();
			if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
				throw new UnauthorizedAccessException("Audio filesystem paths must be absolute.");
			string full = Path.GetFullPath(path);
			string relative = full.Substring(Path.GetPathRoot(full).Length);
			if (relative.Contains(":") || relative.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
				.Any(part => part.EndsWith(".", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal)))
				throw new UnauthorizedAccessException("Audio filesystem ambiguous paths and alternate data streams are not permitted.");
			var comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			bool InRoot(string root) => string.Equals(full, root, comparison) ||
				full.StartsWith(root + Path.DirectorySeparatorChar, comparison);
			if (!(write ? InRoot(audioRoots.Cache) : InRoot(audioRoots.Server) || InRoot(audioRoots.Cache)))
				throw new UnauthorizedAccessException("Audio file access is outside its permitted folders.");
			// Check existing ancestors too: a symlink at or above either root must not escape the policy.
			for (string current = full; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
			{
				try
				{
					if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
						throw new UnauthorizedAccessException("Audio filesystem symlinks are not permitted.");
				}
				catch (FileNotFoundException) { }
				catch (DirectoryNotFoundException) { }
			}
			return full;
		}

		/// <summary>Binary audio I/O constrained to CustomAudio (read) and CustomAudioCache (read/write).</summary>
		public static class AudioFile
		{
			public static bool Exists(string path) => AudioIo(() => File.Exists(ValidateAudioPath(path)));
			public static AudioFileInfo GetInfo(string path) => AudioIo(() => new AudioFileInfo(ValidateAudioPath(path)));
			public static AudioStream OpenRead(string path) => AudioIo(() => new AudioStream(new FileStream(ValidateAudioPath(path), FileMode.Open, FileAccess.Read, FileShare.Read)));
			public static AudioStream CreateNew(string path) => AudioIo(() => new AudioStream(new FileStream(ValidateAudioPath(path, true), FileMode.CreateNew, FileAccess.Write, FileShare.None)));
			public static DateTime GetLastWriteTimeUtc(string path) => AudioIo(() => File.GetLastWriteTimeUtc(ValidateAudioPath(path)));
			public static void Delete(string path) => AudioIo(() => File.Delete(ValidateAudioPath(path, true)));
			public static void Move(string source, string destination) => AudioIo(() => File.Move(ValidateAudioPath(source, true), ValidateAudioPath(destination, true)));
			public static void Copy(string source, string destination) => AudioIo(() => File.Copy(ValidateAudioPath(source), ValidateAudioPath(destination, true)));
			public static byte[] HashSha256(string path) { using (var stream = OpenRead(path)) return stream.HashSha256(); }
			private static void ValidateFilename(string name)
			{
				if (string.IsNullOrEmpty(name) || name == "." || name == ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0)
					throw new UnauthorizedAccessException("Invalid audio filename.");
			}
			public static string CachePath(string name)
			{
				ValidateFilename(name);
				return AudioIo(() => ValidateAudioPath(Path.Combine(AudioCacheRoot, name), true));
			}
			public static string SiblingPath(string path, string name)
			{
				ValidateFilename(name);
				return AudioIo(() => ValidateAudioPath(Path.Combine(Path.GetDirectoryName(ValidateAudioPath(path)), name)));
			}

			public static byte[] ReadAllBytes(string path, int maxBytes)
				=> AudioIo(() => ReadBounded(path, maxBytes));

			private static byte[] ReadBounded(string path, int maxBytes)
			{
				if (maxBytes < 0) throw new ArgumentOutOfRangeException(nameof(maxBytes));
				using (var stream = OpenRead(path))
				{
					if (stream.Length <= 0 || stream.Length > maxBytes) throw new SafeFilesystemException("Audio file size limit exceeded.");
					var data = new byte[(int)stream.Length];
					int offset = 0;
					while (offset < data.Length)
					{
						int read = stream.Read(data, offset, data.Length - offset);
						if (read == 0) throw new EndOfStreamException();
						offset += read;
					}
					if (stream.ReadByte() != -1) throw new SafeFilesystemException("Audio file changed while reading.");
					return data;
				}
			}

			public static string ReadAllText(string path, int maxBytes) => Encoding.UTF8.GetString(ReadAllBytes(path, maxBytes)).TrimStart('\uFEFF');
		}

		// Snapshots expose metadata without handing callers an unchecked FileInfo/Delete capability.
		public sealed class AudioFileInfo
		{
			public string FullName { get; }
			public string Name => Path.GetFileName(FullName);
			public string NameWithoutExtension => Path.GetFileNameWithoutExtension(FullName);
			public long Length { get; }
			public DateTime LastWriteTimeUtc { get; }
			internal AudioFileInfo(string path)
			{
				var info = new FileInfo(path);
				FullName = info.FullName; Length = info.Length; LastWriteTimeUtc = info.LastWriteTimeUtc;
			}
			public void Delete() => AudioFile.Delete(FullName);
		}

		public static class AudioDirectory
		{
			public static bool Exists(string path) => AudioIo(() => Directory.Exists(ValidateAudioPath(path)));
			public static void CreateDirectory(string path) => AudioIo(() => Directory.CreateDirectory(ValidateAudioPath(path, true)));
			public static string[] GetFiles(string path) => AudioIo(() => Directory.GetFiles(ValidateAudioPath(path)).Select(file => ValidateAudioPath(file)).ToArray());
			public static string[] GetDirectories(string path) => AudioIo(() => Directory.GetDirectories(ValidateAudioPath(path)).Select(directory => ValidateAudioPath(directory)).ToArray());
			public static AudioFileInfo[] GetFilesInfo(string path, string pattern)
			{
				if (string.IsNullOrEmpty(pattern) || pattern.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || pattern.Contains(".."))
					throw new ArgumentException("Invalid audio file search pattern.", nameof(pattern));
				return AudioIo(() => Directory.GetFiles(ValidateAudioPath(path), pattern).Select(AudioFile.GetInfo).ToArray());
			}
		}
	}
}
