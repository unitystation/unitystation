using System;
using System.Collections.Generic;
using Tilemaps.Behaviours.Meta.Utils;

namespace Atmospherics
{
	public static class AtmosUtils
	{
		public const float MinimumPressure = 0.01f;

		public static void SetEmpty(params MetaDataNode[] nodes)
		{
			foreach (MetaDataNode node in nodes)
			{
				SetGasMix(node, GasMixUtils.Space);
			}
		}

		public static void SetAir(MetaDataNode node)
		{
			SetGasMix(node, GasMixUtils.Air);
		}

		public static void SetGasMix(MetaDataNode node, GasMix gasMix)
		{
			node.Atmos = gasMix;
		}

		public static void Equalize(List<MetaDataNode> nodes)
		{
			List<MetaDataNode> targetNodes = new List<MetaDataNode>();

			bool isSpace = false;

			GasMix gasMix = GasMixUtils.Space;

			foreach (MetaDataNode node in nodes)
			{
				if (node.IsSpace)
				{
					isSpace = true;
					break;
				}

				gasMix += node.Atmos;

				if (node.IsRoom)
				{
					targetNodes.Add(node);
				}
				else
				{
					SetEmpty(node);
				}
			}

			if (isSpace)
			{
				SetEmpty(nodes.ToArray());
			}
			else
			{
				gasMix /= targetNodes.Count;

				// Copy to other nodes
				for (int i = 0; i < targetNodes.Count; i++)
				{
					targetNodes[i].Atmos = gasMix;
				}
			}
		}
	}
}