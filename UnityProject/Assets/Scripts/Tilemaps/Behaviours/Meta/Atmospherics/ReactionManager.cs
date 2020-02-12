using System;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Objects;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using UnityEngine.Profiling;
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
	private List<Hotspot> hotspotsToAdd;
	private List<Vector3Int> hotspotsToRemove;
	private TilemapDamage[] tilemapDamages;

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
		hotspotsToRemove = new List<Vector3Int>();
		hotspotsToAdd = new List<Hotspot>();
		tilemapDamages = GetComponentsInChildren<TilemapDamage>();
	}

	private void Update()
	{
		timePassed += Time.deltaTime;
		timePassed2 += Time.deltaTime;

		Profiler.BeginSample("Wind");
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
										//converting push to world coords because winddirection is in local coords
										pushable.QueuePush((transform.rotation * windyNode.WindDirection.To3Int()).To2Int(), Random.Range( ( float ) ( correctedForce * 0.8 ), correctedForce ) );
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
		Profiler.EndSample();

		if (timePassed < 0.5)
		{
			return;
		}


		//process the current hotspots, potentially adding new ones and removing ones that have expired.
		//(but we actually perform the add / remove in a separate pass so we don't concurrentlyu modify the dict
		foreach (MetaDataNode node in hotspots.Values)
		{
			if (node.Hotspot != null)
			{
				if (node.Hotspot.Process())
				{
					if (node.Hotspot.Volume > 0.95 * node.GasMix.Volume)
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
					Profiler.BeginSample("MarkForRemoval");
					RemoveHotspot(node);
					Profiler.EndSample();
				}
			}
		}


		Profiler.BeginSample("HotspotModify");
		//process additions and removals to the dictionary
		foreach (var addedHotspot in hotspotsToAdd)
		{
			if (!hotspots.ContainsKey(addedHotspot.node.Position))
			{
				hotspots.Add(addedHotspot.node.Position, addedHotspot.node);
			}

		}
		foreach (var removedHotspot in hotspotsToRemove)
		{
			if (hotspots.ContainsKey(removedHotspot))
			{
				hotspots.Remove(removedHotspot);
			}
		}
		hotspotsToAdd.Clear();
		hotspotsToRemove.Clear();
		Profiler.EndSample();

		timePassed = 0;
	}

	/// Same as ExposeHotspot but allows providing a world position and handles the conversion
	public void ExposeHotspotWorldPosition(Vector2Int tileWorldPosition, float temperature, float volume)
	{
		ExposeHotspot(MatrixManager.WorldToLocalInt(tileWorldPosition.To3Int(), MatrixManager.Get(matrix)), temperature, volume);
	}

	private void RemoveHotspot(MetaDataNode node)
	{
		node.Hotspot = null;
		//removal will be done later in update
		hotspotsToRemove.Add(node.Position);
		tileChangeManager.RemoveTile(node.Position, LayerType.Effects);
	}

	public void ExtinguishHotspot(Vector3Int localPosition)
	{
		if (hotspots.ContainsKey(localPosition) && hotspots[localPosition].Hotspot != null)
		{
			RemoveHotspot(hotspots[localPosition].Hotspot.node);
		}
	}

	public void ExposeHotspot(Vector3Int localPosition, float temperature, float volume)
	{
		if (hotspots.ContainsKey(localPosition) && hotspots[localPosition].Hotspot != null)
		{
			// TODO soh?
			hotspots[localPosition].Hotspot.UpdateValues(volume * 25, temperature);
		}
		else
		{
			Profiler.BeginSample("MarkForAddition");
			MetaDataNode node = metaDataLayer.Get(localPosition);
			GasMix gasMix = node.GasMix;

			if (gasMix.GetMoles(Gas.Plasma) > 0.5 && gasMix.GetMoles(Gas.Oxygen) > 0.5 && temperature > Reactions.PlasmaMaintainFire)
			{

				// igniting
				Hotspot hotspot = new Hotspot(node, temperature, volume * 25);
				node.Hotspot = hotspot;
				//addition will be done later in Update
				hotspotsToAdd.Add(hotspot);
			}
			Profiler.EndSample();
		}

		if (hotspots.ContainsKey(localPosition) && hotspots[localPosition].Hotspot != null)
		{
			//expose everything on this tile
			Expose(localPosition, localPosition);

			//expose impassable things on the adjacent tile
			Expose(localPosition, localPosition + Vector3Int.right);
			Expose(localPosition, localPosition + Vector3Int.left);
			Expose(localPosition, localPosition + Vector3Int.up);
			Expose(localPosition, localPosition + Vector3Int.down);
		}
	}

	private void Expose(Vector3Int hotspotPosition, Vector3Int atLocalPosition)
	{
		Profiler.BeginSample("ExposureInit");
		var isSideExposure = hotspotPosition != atLocalPosition;
		//calculate world position
		var hotspotWorldPosition = MatrixManager.LocalToWorldInt(hotspotPosition, MatrixManager.Get(matrix));
		var atWorldPosition = MatrixManager.LocalToWorldInt(atLocalPosition, MatrixManager.Get(matrix));

		if (!hotspots.ContainsKey(hotspotPosition))
		{
			Logger.LogError("Hotspot position key was not found in the hotspots dictionary", Category.Atmos);
			return;
		}


		var exposure = FireExposure.FromMetaDataNode(hotspots[hotspotPosition], hotspotWorldPosition.To2Int(), atLocalPosition.To2Int(), atWorldPosition.To2Int());
		Profiler.EndSample();

		if (isSideExposure)
		{
			Profiler.BeginSample("SideExposure");
			//side exposure logic

			//already exposed by a different hotspot
			if (hotspots.ContainsKey(atLocalPosition))
			{
				Profiler.EndSample();
				return;
			}

			var metadata = metaDataLayer.Get(atLocalPosition);
			if (!metadata.IsOccupied)
			{
				//atmos can pass here, so no need to check side exposure (nothing to brush up against)
				Profiler.EndSample();
				return;
			}

			//only expose to atmos impassable objects, since those are the things the flames would
			//actually brush up against
			var regTiles = matrix.Get<RegisterTile>(atLocalPosition, true);
			foreach (var regTile in regTiles)
			{
				if (!regTile.IsAtmosPassable(exposure.HotspotLocalPosition.To3Int(), true))
				{
					var exposable = regTile.GetComponent<IFireExposable>();
					exposable.OnExposed(exposure);
				}
			}

			//expose the tiles there
			foreach (var tilemapDamage in tilemapDamages)
			{
				tilemapDamage.OnExposed(exposure);
			}
			Profiler.EndSample();
		}
		else
		{
			Profiler.BeginSample("DirectExposure");
			//direct exposure logic
			var fireExposables = matrix.Get<IFireExposable>(atLocalPosition, true);
			foreach (var exposable in fireExposables)
			{
				exposable.OnExposed(exposure);
			}
			//expose the tiles
			foreach (var tilemapDamage in tilemapDamages)
			{
				tilemapDamage.OnExposed(exposure);
			}
			Profiler.EndSample();
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
		}
	}
}