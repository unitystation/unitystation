#if !NO_UNITY
using System;
using System.Collections.Generic;
using FullSerializer.Internal.DirectConverters;
using UnityEngine;

namespace FullSerializer
{
	partial class fsConverterRegistrar
	{
		public static Bounds_DirectConverter Register_Bounds_DirectConverter;
	}
}

namespace FullSerializer.Internal.DirectConverters
{
	public class Bounds_DirectConverter : fsDirectConverter<Bounds>
	{
		protected override fsResult DoSerialize(Bounds model, Dictionary<string, fsData> serialized)
		{
			fsResult result = fsResult.Success;

			result += SerializeMember(serialized, null, "center", model.center);
			result += SerializeMember(serialized, null, "size", model.size);

			return result;
		}

		protected override fsResult DoDeserialize(Dictionary<string, fsData> data, ref Bounds model)
		{
			fsResult result = fsResult.Success;

			Vector3 t0 = model.center;
			result += DeserializeMember(data, null, "center", out t0);
			model.center = t0;

			Vector3 t1 = model.size;
			result += DeserializeMember(data, null, "size", out t1);
			model.size = t1;

			return result;
		}

		public override object CreateInstance(fsData data, Type storageType)
		{
			return new Bounds();
		}
	}
}
#endif