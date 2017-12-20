#if !NO_UNITY
using System;
using System.Collections.Generic;
using FullSerializer.Internal.DirectConverters;
using UnityEngine;

namespace FullSerializer
{
	partial class fsConverterRegistrar
	{
		public static Rect_DirectConverter Register_Rect_DirectConverter;
	}
}

namespace FullSerializer.Internal.DirectConverters
{
	public class Rect_DirectConverter : fsDirectConverter<Rect>
	{
		protected override fsResult DoSerialize(Rect model, Dictionary<string, fsData> serialized)
		{
			fsResult result = fsResult.Success;

			result += SerializeMember(serialized, null, "xMin", model.xMin);
			result += SerializeMember(serialized, null, "yMin", model.yMin);
			result += SerializeMember(serialized, null, "xMax", model.xMax);
			result += SerializeMember(serialized, null, "yMax", model.yMax);

			return result;
		}

		protected override fsResult DoDeserialize(Dictionary<string, fsData> data, ref Rect model)
		{
			fsResult result = fsResult.Success;

			float t0 = model.xMin;
			result += DeserializeMember(data, null, "xMin", out t0);
			model.xMin = t0;

			float t1 = model.yMin;
			result += DeserializeMember(data, null, "yMin", out t1);
			model.yMin = t1;

			float t2 = model.xMax;
			result += DeserializeMember(data, null, "xMax", out t2);
			model.xMax = t2;

			float t3 = model.yMax;
			result += DeserializeMember(data, null, "yMax", out t3);
			model.yMax = t3;

			return result;
		}

		public override object CreateInstance(fsData data, Type storageType)
		{
			return new Rect();
		}
	}
}
#endif