#if !NO_UNITY
using System;
using System.Collections.Generic;
using FullSerializer.Internal.DirectConverters;
using UnityEngine;

namespace FullSerializer
{
	partial class fsConverterRegistrar
	{
		public static RectOffset_DirectConverter Register_RectOffset_DirectConverter;
	}
}

namespace FullSerializer.Internal.DirectConverters
{
	public class RectOffset_DirectConverter : fsDirectConverter<RectOffset>
	{
		protected override fsResult DoSerialize(RectOffset model, Dictionary<string, fsData> serialized)
		{
			fsResult result = fsResult.Success;

			result += SerializeMember(serialized, null, "bottom", model.bottom);
			result += SerializeMember(serialized, null, "left", model.left);
			result += SerializeMember(serialized, null, "right", model.right);
			result += SerializeMember(serialized, null, "top", model.top);

			return result;
		}

		protected override fsResult DoDeserialize(Dictionary<string, fsData> data, ref RectOffset model)
		{
			fsResult result = fsResult.Success;

			int t0 = model.bottom;
			result += DeserializeMember(data, null, "bottom", out t0);
			model.bottom = t0;

			int t2 = model.left;
			result += DeserializeMember(data, null, "left", out t2);
			model.left = t2;

			int t3 = model.right;
			result += DeserializeMember(data, null, "right", out t3);
			model.right = t3;

			int t4 = model.top;
			result += DeserializeMember(data, null, "top", out t4);
			model.top = t4;

			return result;
		}

		public override object CreateInstance(fsData data, Type storageType)
		{
			return new RectOffset();
		}
	}
}
#endif