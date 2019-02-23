using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Objects;
using UnityEngine;

/// <summary>
/// Handles performing the various reactions that can occur in atmospherics simulation.
/// </summary>
public class ReactionManager : MonoBehaviour
{
	private TileChangeManager tileChangeManager;
	private MetaDataLayer metaDataLayer;
	private Matrix matrix;

	private Dictionary<Vector3Int, MetaDataNode> hotspots;

	private float timePassed;

	private void Awake()
	{
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		metaDataLayer = GetComponent<MetaDataLayer>();
		matrix = GetComponent<Matrix>();

		hotspots = new Dictionary<Vector3Int, MetaDataNode>();
	}

	private void Update()
	{
		timePassed += Time.deltaTime;

		if (timePassed < 0.5)
		{
			return;
		}

		timePassed = 0;

		foreach (MetaDataNode node in hotspots.Values.ToArray())
		{
			if (node.Hotspot != null)
			{
				if (node.Hotspot.Process())
				{
					if (node.Hotspot.Volume > 0.95 * node.GasMix.Volume && node.Hotspot.Temperature > Reactions.FIRE_MINIMUM_TEMPERATURE_TO_SPREAD)
					{
						for (var i = 0; i < node.Neighbors.Length; i++)
						{
							MetaDataNode neighbor = node.Neighbors[i];

							if (neighbor != null)
							{
								ExposeHotspot(node.Neighbors[i].Position, node.GasMix.Temperature * 0.85f, node.GasMix.Volume / 4);
							}
						}
					}

					tileChangeManager.UpdateTile(node.Position, TileType.Effects, "Fire");
				}
				else
				{
					node.Hotspot = null;
					hotspots.Remove(node.Position);
					tileChangeManager.RemoveTile(node.Position, LayerType.Effects);
				}
			}
		}
	}

	public void ExposeHotspot(Vector3Int position, float temperature, float volume)
	{
		if (hotspots.ContainsKey(position) && hotspots[position].Hotspot != null)
		{
			// TODO soh?
			hotspots[position].Hotspot.UpdateValues(volume * 25, temperature);
		}
		else
		{
			MetaDataNode node = metaDataLayer.Get(position);
			GasMix gasMix = node.GasMix;

			if (gasMix.GetMoles(Gas.Plasma) > 0.5 && gasMix.GetMoles(Gas.Oxygen) > 0.5 && temperature > Reactions.PLASMA_MINIMUM_BURN_TEMPERATURE)
			{
				// igniting
				Hotspot hotspot = new Hotspot(node, temperature, volume * 25);
				node.Hotspot = hotspot;
				hotspots[position] = node;
			}
		}

		if (hotspots.ContainsKey(position) && hotspots[position].Hotspot != null)
		{
			List<LivingHealthBehaviour> healths = matrix.Get<LivingHealthBehaviour>(position);

			foreach (LivingHealthBehaviour health in healths)
			{
				health.ApplyDamage(null, 1, DamageType.Burn);
			}
		}
	}
}