using System;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Objects;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Handles performing the various reactions that can occur in atmospherics simulation.
/// </summary>
public class ReactionManager : MonoBehaviour
{
	private TileChangeManager tileChangeManager;
	private MetaDataLayer metaDataLayer;
	private Matrix matrix;

	private Dictionary<Vector3Int, MetaDataNode> hotspots;
	private UniqueQueue<MetaDataNode> winds;

	private float timePassed;
	private float timePassed2;
	private static readonly string LogAddingWindyNode = "Adding windy node {0}, dir={1}, force={2}";

	private void Awake()
	{
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		metaDataLayer = GetComponent<MetaDataLayer>();
		matrix = GetComponent<Matrix>();

		hotspots = new Dictionary<Vector3Int, MetaDataNode>();
		winds = new UniqueQueue<MetaDataNode>();
	}

	private void Update()
	{
		timePassed += Time.deltaTime;
		timePassed2 += Time.deltaTime;

		if ( timePassed2 >= 0.1 )
		{
			int count = winds.Count;
			if ( count > 0 )
			{
				for ( int i = 0; i < count; i++ )
				{
					if ( winds.TryDequeue( out var windyNode ) )
					{
						foreach ( var pushable in matrix.Get<PushPull>( windyNode.Position, true ) )
						{
							float correctedForce = windyNode.WindForce / ( int ) pushable.Pushable.Size;
							if ( correctedForce >= AtmosConstants.MinPushForce )
							{
								if ( pushable.Pushable.IsTileSnap )
								{
									byte pushes = (byte)Mathf.Clamp((int)correctedForce / 10, 1, 10);
									for ( byte j = 0; j < pushes; j++ )
									{
										pushable.QueuePush( windyNode.WindDirection, Random.Range( ( float ) ( correctedForce * 0.8 ), correctedForce ) );
									}
								} else
								{
									pushable.Pushable.Nudge( new NudgeInfo
									{
										OriginPos = pushable.Pushable.ServerPosition,
										Trajectory = (Vector2)windyNode.WindDirection,
										SpinMode = SpinMode.None,
										SpinMultiplier = 1,
										InitialSpeed = correctedForce,
									} );
								}
							}
						}

						windyNode.WindForce = 0;
						windyNode.WindDirection = Vector2Int.zero;
					}
				}
			}

			timePassed2 = 0;
		}

		if (timePassed < 0.5)
		{
			return;
		}

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

		timePassed = 0;
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
			var healths = matrix.Get<LivingHealthBehaviour>(position, true);
			foreach (LivingHealthBehaviour health in healths)
			{
				health.ApplyDamage(null, 1, DamageType.Burn);
			}
		}
	}

	public void AddWindEvent( MetaDataNode node, Vector2Int windDirection, float pressureDifference )
	{
		if ( node != MetaDataNode.None && pressureDifference > AtmosConstants.MinWindForce
		                               && windDirection != Vector2Int.zero )
		{
			node.WindForce = pressureDifference;
			node.WindDirection = windDirection;
			winds.Enqueue( node );
			Logger.LogTraceFormat( LogAddingWindyNode, Category.Atmos, node.Position.To2Int(), windDirection, pressureDifference );
		}
	}
}