using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AudioFile = SecureStuff.AccessFile.AudioFile;

namespace US13.Core.SecureStuff.CustomAudio
{
	/// <summary>Checks UnityFS and serialized object types in managed code, before Unity deserializes anything.</summary>
	public static class AudioBundlePreflight
	{
		public const int MaxBundleBytes = 128 * 1024 * 1024;
		public const int MaxExpandedBytes = 256 * 1024 * 1024;
		private static readonly HashSet<int> AllowedClasses = new() { 1, 4, 82, 83, 142 };

		public static void Validate(string path)
		{
			Validate(AudioFile.ReadAllBytes(path, MaxBundleBytes));
		}

		public static void Validate(byte[] bytes)
		{
			if (bytes == null || bytes.Length > MaxBundleBytes)
			{
				throw new FormatException("Audio bundle size limit exceeded.");
			}

			var reader = new Reader(bytes, false);
			if (reader.String() != "UnityFS")
			{
				throw new FormatException("Only UnityFS audio bundles are accepted.");
			}

			uint version = reader.U32();
			if (version < 6 || version > 8)
			{
				throw new FormatException("Unsupported UnityFS format.");
			}
			reader.String();
			reader.String(); // ?
			if (reader.I64() != bytes.Length)
			{
				throw new FormatException("Bundle length does not match its header.");
			}
			int compressedInfo = reader.Size(4 * 1024 * 1024);
			int expandedInfo = reader.Size(4 * 1024 * 1024);
			uint flags = reader.U32();

			if (version >= 7)
			{
				reader.Align(16);
			}

			int headerEnd = reader.Position;
			int infoStart = (flags & 0x80) != 0 ? bytes.Length - compressedInfo : headerEnd;
			reader.Position = infoStart;
			var info = new Reader(Expand(reader.Bytes(compressedInfo), expandedInfo, flags & 0x3f), false);
			info.Skip(16);
			int count = info.Count(65536);
			var blocks = new List<(int expanded, int compressed, uint flags)>();
			long total = 0;
			for (var i = 0; i < count; i++)
			{
				int expanded = info.Size(MaxExpandedBytes);
				int compressed = info.Size(MaxBundleBytes);
				uint compression = info.U16();
				total += expanded;
				if (total > MaxExpandedBytes) throw new FormatException("Expanded audio bundle is too large.");
				blocks.Add((expanded, compressed, compression));
			}

			int nodes = info.Count((info.Length - info.Position) / 21);
			var entries = new List<(int offset, int size, bool serialized, string name)>();
			var names = new HashSet<string>(StringComparer.Ordinal);
			for (var i = 0; i < nodes; i++)
			{
				long offset = info.I64(), size = info.I64();
				uint nodeFlags = info.U32();
				string name = info.String();
				bool serialized = (nodeFlags & 4) != 0;
				string suffix = serialized ? "" :
					name.EndsWith(".resS", StringComparison.Ordinal) ? ".resS" :
					name.EndsWith(".resource", StringComparison.Ordinal) ? ".resource" : null;
				string stem = suffix == null ? "" : name.Substring(0, name.Length - suffix.Length);
				if (suffix == null || !stem.StartsWith("CAB-", StringComparison.Ordinal) ||
				    stem.Length != 36 || stem.Substring(4).Any(c => !Uri.IsHexDigit(c)) || !names.Add(name))
				{
					throw new FormatException("Bundle contains an unexpected file.");
				}
				if (offset < 0 || size < 0 || offset > total || size > total - offset)
				{
					throw new FormatException("Invalid bundle file bounds.");
				}
				entries.Add(((int)offset, (int)size, serialized, name));
			}

			if (info.Position != info.Length || entries.Count(e => e.serialized) != 1)
			{
				throw new FormatException("Invalid bundle directory.");
			}

			reader.Position = (flags & 0x80) != 0 ? headerEnd : infoStart + compressedInfo;
			if ((flags & 0x200) != 0)
			{
				reader.Align(16);
			}

			var data = new byte[(int)total];
			var written = 0;
			foreach ((int expanded, int compressed, uint flags) block in blocks)
			{
				if ((flags & 0x80) != 0 && reader.Position + (long)block.compressed > infoStart)
				{
					throw new FormatException("Bundle blocks overlap their directory.");
				}
				byte[] part = Expand(reader.Bytes(block.compressed), block.expanded, block.flags & 0x3f);
				Buffer.BlockCopy(part, 0, data, written, part.Length);
				written += part.Length;
			}

			int expectedEnd = (flags & 0x80) != 0 ? infoStart : bytes.Length;
			if (reader.Position != expectedEnd)
			{
				throw new FormatException("Unexpected trailing bundle bytes.");
			}

			var hasAudio = false;
			foreach ((int offset, int size, bool serialized, string name) entry in entries.Where(e => e.serialized))
			{
				var serialized = new byte[entry.size];
				Buffer.BlockCopy(data, entry.offset, serialized, 0, entry.size);
				hasAudio |= ValidateSerializedFile(serialized, entry.name,
					entries.Where(e => !e.serialized).ToDictionary(e => e.name, e => e.size));
			}

			if (!hasAudio)
			{
				throw new FormatException("Audio bundle contains no AudioClip objects.");
			}
		}

		private static bool ValidateSerializedFile(byte[] data, string archive, Dictionary<string, int> resources)
		{
			var r = new Reader(data, false);
			uint metadata = r.U32();
			long fileSize = r.U32();
			uint version = r.U32();
			long dataOffset = r.U32();
			bool little = r.Byte() == 0;
			r.Skip(3);

			if (version < 17 || version > 22)
			{
				throw new FormatException("Unsupported serialized file format.");
			}
			if (version >= 22)
			{
				metadata = r.U32();
				fileSize = r.I64();
				dataOffset = r.I64();
				r.Skip(8);
			}

			if (fileSize != data.Length || dataOffset < r.Position || dataOffset > data.Length || metadata > dataOffset)
			{
				throw new FormatException("Invalid serialized file bounds.");
			}

			r.Little = little;
			r.String();
			r.U32();
			bool trees = r.Byte() != 0;
			if (!trees)
			{
				throw new FormatException("Audio bundles must retain their type trees.");
			}

			int typeCount = r.Count((int)((dataOffset - r.Position) / 23));
			var types = new int[typeCount];

			for (var i = 0; i < typeCount; i++)
			{
				int type = r.I32();
				if (!AllowedClasses.Contains(type))
					throw new FormatException("Disallowed serialized class ID: " + type);
				types[i] = type;
				r.Byte();
				if (r.I16() != -1) throw new FormatException("Script type references are not allowed.");
				r.Skip(16);
				if (trees)
				{
					int treeNodes = r.Count(65536), strings = r.Count(4 * 1024 * 1024);
					byte[] tree = r.Bytes(checked(treeNodes * (version >= 19 ? 32 : 24)));
					byte[] names = r.Bytes(strings);
					if (type == 83) ValidateAudioTree(tree, names, little, version >= 19);
				}

				if (version >= 21) r.Skip(checked(r.Count(1024) * 4));
			}

			int objects = r.Count((int)((dataOffset - r.Position) / (version >= 22 ? 24 : 20)));
			var hasAudio = false;
			var ranges = new List<(long start, long end)>();
			for (var i = 0; i < objects; i++)
			{
				r.Align(4);
				r.I64();
				long start = version >= 22 ? r.I64() : r.U32();
				uint size = r.U32();
				int type = r.I32();
				if (type < 0 || type >= types.Length || start < 0 || start > fileSize - dataOffset || size > fileSize - dataOffset - start)
				{
					throw new FormatException("Invalid serialized object.");
				}
				int classId = types[type];

				// The bundle's asset table grows with its entry count; its containing file bounds it.
				if (classId != 142 && size > (classId == 83 ? 16384 : 65536))
				{
					throw new FormatException("Serialized audio object size limit exceeded.");
				}
				ranges.Add((start, start + size));
				hasAudio |= types[type] == 83;
				if (types[type] == 83)
				{
					var clipData = new byte[size];
					Buffer.BlockCopy(data, checked((int)(dataOffset + start)), clipData, 0, (int)size);
					ValidateClip(clipData, little, archive, resources);
				}
			}

			ranges.Sort((left, right) => left.start.CompareTo(right.start));
			long previousEnd = 0;
			foreach ((long start, long end) range in ranges)
			{
				if (range.start < previousEnd)
				{
					throw new FormatException("Serialized audio objects overlap.");
				}
				previousEnd = range.end;
			}

			// No script identifiers, external object references, or managed-reference types.
			if (r.Count(0) != 0 || r.Count(0) != 0)
			{
				throw new FormatException("External/script references are not allowed.");
			}
			if (version >= 20 && r.Count(0) != 0)
			{
				throw new FormatException("Managed references are not allowed.");
			}

			r.String();
			if (r.Position > dataOffset) throw new FormatException("Serialized metadata overlaps object data.");
			return hasAudio;
		}

		private static void ValidateClip(byte[] data, bool little, string archive, Dictionary<string, int> resources)
		{
			var r = new Reader(data, little);
			r.AlignedString();
			int loadType = r.I32(), channels = r.I32(), frequency = r.I32(), bits = r.I32();
			var length = BitConverter.ToSingle(BitConverter.GetBytes(r.U32()), 0);
			if (loadType != 2 || channels < 1 || channels > 8 || frequency < 8000 || frequency > 192000 ||
			    bits < 1 || bits > 32 || float.IsNaN(length) || length <= 0 || length > 1800)
			{
				throw new FormatException("CustomAudio clips must use streaming import and bounded playback settings.");
			}
			r.Byte();
			r.Align(4);
			r.I32();
			r.Byte();
			r.Byte();
			r.Byte();
			r.Align(4);
			string source = r.AlignedString();
			long offset = r.I64(), size = r.I64();
			int compression = r.I32();
			string name = source.Substring(source.LastIndexOf('/') + 1);
			if (!resources.TryGetValue(name, out int available) ||
			    (source != "archive:/" + archive + "/" + name && source != "archive:/" + name && source != name) ||
			    offset < 0 || size <= 0 || offset > available || size > available - offset || compression < 0 ||
			    compression > 3)
			{
				throw new FormatException("Audio streaming resources must reside inside the verified bundle.");
			}
		}

		private static void ValidateAudioTree(byte[] data, byte[] names, bool little, bool large)
		{
			var r = new Reader(data, little);
			var signature = new StringBuilder();

			string Name(uint offset)
			{
				if ((offset & 0x80000000) != 0)
					switch (offset & 0x7fffffff)
					{
						case 49: return "Array";
						case 55: return "Base";
						case 76: return "bool";
						case 81: return "char";
						case 106: return "data";
						case 161: return "float";
						case 222: return "int";
						case 427: return "m_Name";
						case 795: return "size";
						case 814: return "SInt64";
						case 840: return "string";
						case 921: return "UInt64";
						case 1152: return "FileSize";
						default: throw new FormatException("Unexpected AudioClip type tree string.");
					}

				var strings = new Reader(names, little);
				strings.Position = checked((int)offset);
				return strings.String();
			}

			while (r.Position < r.Length)
			{
				r.U16();
				int level = r.Byte();
				int flags = r.Byte();
				string type = Name(r.U32()), name = Name(r.U32());
				int size = r.I32();
				r.I32();
				int meta = r.I32();
				if (large) r.I64();
				signature.Append(level).Append(':').Append(type).Append(':').Append(name).Append(':').Append(size)
					.Append(':').Append(flags).Append(':').Append(meta & 0x4000).Append('|');
			}

			const string expected =
				"0:AudioClip:Base:-1:0:0|1:string:m_Name:-1:0:0|2:Array:Array:-1:1:16384|3:int:size:4:0:0|3:char:data:1:0:0|" +
				"1:int:m_LoadType:4:0:0|1:int:m_Channels:4:0:0|1:int:m_Frequency:4:0:0|1:int:m_BitsPerSample:4:0:0|1:float:m_Length:4:0:0|" +
				"1:bool:m_IsTrackerFormat:1:0:0|1:bool:m_Ambisonic:1:0:16384|1:int:m_SubsoundIndex:4:0:0|" +
				"1:bool:m_PreloadAudioData:1:0:0|1:bool:m_LoadInBackground:1:0:0|1:bool:m_Legacy3D:1:0:16384|" +
				"1:StreamedResource:m_Resource:-1:0:0|2:string:m_Source:-1:0:0|3:Array:Array:-1:1:16384|4:int:size:4:0:0|4:char:data:1:0:0|" +
				"2:FileSize:m_Offset:8:0:0|2:UInt64:m_Size:8:0:0|1:int:m_CompressionFormat:4:0:16384|";
			if (signature.ToString() != expected)
				throw new FormatException("Unsupported AudioClip serialization layout.");
		}

		private static byte[] Expand(byte[] source, int size, uint compression)
		{
			if (compression == 0)
			{
				if (source.Length != size) throw new FormatException("Invalid uncompressed block.");
				return source;
			}

			if (compression != 2 && compression != 3)
				throw new FormatException("Only uncompressed/LZ4 audio bundles are accepted.");
			var output = new byte[size];
			int input = 0, written = 0;
			while (input < source.Length)
			{
				int token = source[input++], literal = token >> 4;
				if (literal == 15) literal = checked(literal + Extra(source, ref input));
				if (literal > source.Length - input || literal > size - written)
					throw new FormatException("Invalid LZ4 literal.");
				Buffer.BlockCopy(source, input, output, written, literal);
				input += literal;
				written += literal;
				if (input == source.Length) break;
				if (input + 2 > source.Length) throw new FormatException("Truncated LZ4 offset.");
				int distance = source[input++] | (source[input++] << 8);
				if (distance == 0 || distance > written) throw new FormatException("Invalid LZ4 offset.");
				int match = token & 15;
				if (match == 15) match = checked(match + Extra(source, ref input));
				match = checked(match + 4);
				if (match > size - written) throw new FormatException("LZ4 output limit exceeded.");
				for (var i = 0; i < match; i++)
				{
					output[written] = output[written - distance];
					written++;
				}
			}

			if (written != size) throw new FormatException("Invalid LZ4 output length.");
			return output;
		}

		private static int Extra(byte[] bytes, ref int offset)
		{
			int total = 0, value;
			do
			{
				if (offset == bytes.Length) throw new FormatException("Truncated LZ4 length.");
				value = bytes[offset++];
				total = checked(total + value);
			} while (value == 255);

			return total;
		}

		private sealed class Reader
		{
			private readonly byte[] data;
			private int position;
			public bool Little;
			public int Length => data.Length;

			public int Position
			{
				get => position;
				set
				{
					if (value < 0 || value > data.Length) throw new FormatException("Invalid data offset.");
					position = value;
				}
			}

			public Reader(byte[] value, bool little)
			{
				data = value;
				Little = little;
			}

			public byte Byte()
			{
				if (position == data.Length) throw new FormatException("Truncated data.");
				return data[position++];
			}

			public ushort U16()
			{
				uint a = Byte(), b = Byte();
				return (ushort)(Little ? a | (b << 8) : (a << 8) | b);
			}

			public short I16()
			{
				return unchecked((short)U16());
			}

			public uint U32()
			{
				uint a = U16(), b = U16();
				return Little ? a | (b << 16) : (a << 16) | b;
			}

			public int I32()
			{
				return unchecked((int)U32());
			}

			public long I64()
			{
				ulong a = U32(), b = U32();
				return unchecked((long)(Little ? a | (b << 32) : (a << 32) | b));
			}

			public int Count(int max)
			{
				int value = I32();
				if (value < 0 || value > max) throw new FormatException("Metadata count limit exceeded.");
				return value;
			}

			public int Size(int max)
			{
				uint value = U32();
				if (value > max) throw new FormatException("Block size limit exceeded.");
				return (int)value;
			}

			public void Skip(int size)
			{
				if (size < 0 || size > data.Length - position) throw new FormatException("Truncated data.");
				position += size;
			}

			public void Align(int alignment)
			{
				Position = checked((position + alignment - 1) / alignment * alignment);
			}

			public byte[] Bytes(int size)
			{
				int start = position;
				Skip(size);
				var result = new byte[size];
				Buffer.BlockCopy(data, start, result, 0, size);
				return result;
			}

			public string String()
			{
				int start = position;
				while (Byte() != 0)
					if (position - start > 4096)
						throw new FormatException("String limit exceeded.");
				return Encoding.UTF8.GetString(data, start, position - start - 1);
			}

			public string AlignedString()
			{
				int size = Count(4096);
				string value = Encoding.UTF8.GetString(Bytes(size));
				Align(4);
				return value;
			}
		}
	}
}