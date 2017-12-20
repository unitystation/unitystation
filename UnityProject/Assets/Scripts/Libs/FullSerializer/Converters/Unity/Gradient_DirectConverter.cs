#if !NO_UNITY
using System;
using System.Collections.Generic;
using FullSerializer.Internal.DirectConverters;
using UnityEngine;

namespace FullSerializer
{
	partial class fsConverterRegistrar
	{
		public static Gradient_DirectConverter Register_Gradient_DirectConverter;
	}
}

namespace FullSerializer.Internal.DirectConverters
{
	public class Gradient_DirectConverter : fsDirectConverter<Gradient>
	{
		protected override fsResult DoSerialize(Gradient model, Dictionary<string, fsData> serialized)
		{
			fsResult result = fsResult.Success;

			result += SerializeMember(serialized, null, "alphaKeys", model.alphaKeys);
			result += SerializeMember(serialized, null, "colorKeys", model.colorKeys);

			return result;
		}

		protected override fsResult DoDeserialize(Dictionary<string, fsData> data, ref Gradient model)
		{
			fsResult result = fsResult.Success;

			GradientAlphaKey[] t0 = model.alphaKeys;
			result += DeserializeMember(data, null, "alphaKeys", out t0);
			model.alphaKeys = t0;

			GradientColorKey[] t1 = model.colorKeys;
			result += DeserializeMember(data, null, "colorKeys", out t1);
			model.colorKeys = t1;

			return result;
		}

		public override object CreateInstance(fsData data, Type storageType)
		{
			return new Gradient();
		}
	}
}
#endif