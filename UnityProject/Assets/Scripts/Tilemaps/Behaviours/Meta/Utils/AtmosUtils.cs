using System;
using System.Collections.Generic;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;

namespace Atmospherics
{
	public static class AtmosUtils
	{
		public const float MinimumPressure = 0.00001f;

		public static void SetEmpty(IEnumerable<MetaDataNode> nodes)
		{
			foreach (MetaDataNode node in nodes)
			{
				SetEmpty(node);
			}
		}

		public static void SetEmpty(MetaDataNode node)
		{
			SetGasMix(node, GasMixUtils.Space);
		}

		public static void SetAir(MetaDataNode node)
		{
			SetGasMix(node, GasMixUtils.Air);
		}

		public static void SetGasMix(MetaDataNode node, GasMix gasMix)
		{
			node.Atmos = gasMix;
		}

		public static bool IsPressureChanged(MetaDataNode node)
		{
			foreach (MetaDataNode neighbor in node.GetNeighbors())
			{
				if (Mathf.Abs(node.Atmos.Pressure - neighbor.Atmos.Pressure) > MinimumPressure)
				{
					return true;
				}
			}

			return false;
		}

	}
}