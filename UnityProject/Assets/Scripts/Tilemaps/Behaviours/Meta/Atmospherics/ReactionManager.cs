using System;
using System.Collections.Concurrent;
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
	public float n = 20;

	private GameObject fireLight = null;

	private Dictionary<Vector3Int, GameObject> fireLightDictionary = new Dictionary<Vector3Int, GameObject>();

	public Dictionary<Vector3Int, HashSet<Gas>> fogTiles = new Dictionary<Vector3Int, HashSet<Gas>>();

	public ConcurrentDictionary<Vector3Int, HashSet<GasReactions>> reactions = new ConcurrentDictionary<Vector3Int, HashSet<GasReactions>>();

	private static readonly int FIRE_FX_Z = -2;

	private TileChangeManager tileChangeManager;
	private MetaDataLayer metaDataLayer;
	private Matrix matrix;

	private Dictionary<Vector3Int, MetaDataNode> hotspots;
	private UniqueQueue<MetaDataNode> winds;

	private UniqueQueue<FogEffect> addFog; //List of tiles to add chemcial fx to
	private UniqueQueue<FogEffect> removeFog; //List of tiles to remove the chemical fx from

	private UniqueQueue<ReactionData> addReaction; //List of tiles to add chemcial fx to

	private List<Hotspot> hotspotsToAdd;
	private List<Vector3Int> hotspotsToRemove;
	private TilemapDamage[] tilemapDamages;

	private float timePassed;
	private float timePassed2;

	/// <summary>
	/// reused when applying exposures to lots of tiles to avoid creating GC from
	/// lambdas.
	/// </summary>
	private ApplyExposure applyExposure = new ApplyExposure();

	private void Awake()
	{
		tileChangeManager = GetComponentInParent<TileChangeManager>();
		metaDataLayer = GetComponent<MetaDataLayer>();
		matrix = GetComponent<Matrix>();

		hotspots = new Dictionary<Vector3Int, MetaDataNode>();
		winds = new UniqueQueue<MetaDataNode>();

		addFog = new UniqueQueue<FogEffect>();
		removeFog = new UniqueQueue<FogEffect>();

		addReaction = new UniqueQueue<ReactionData>();

		hotspotsToRemove = new List<Vector3Int>();
		hotspotsToAdd = new List<Hotspot>();
		tilemapDamages = GetComponentsInChildren<TilemapDamage>();
	}

	private void Start()
	{
		fireLight = AtmosManager.Instance.fireLight;
	}

	private void Update()
	{
		timePassed += Time.deltaTime;
		timePassed2 += Time.deltaTime;

		#region wind

		Profiler.BeginSample("Wind");

		int count = winds.Count;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				if (winds.TryDequeue(out var windyNode))
				{
					foreach (var pushable in matrix.Get<PushPull>(windyNode.Position, true))
					{
						float correctedForce = windyNode.WindForce / (int) pushable.Pushable.Size;
						if (correctedForce >= AtmosConstants.MinPushForce)
						{
							if (pushable.Pushable.IsTileSnap)
							{
								byte pushes = (byte) Mathf.Clamp((int) correctedForce / 10, 1, 10);
								for (byte j = 0; j < pushes; j++)
								{
									//converting push to world coords because winddirection is in local coords
									pushable.QueuePush((transform.rotation * windyNode.WindDirection.To3Int()).To2Int(),
										Random.Range((float) (correctedForce * 0.8), correctedForce));
								}
							}
							else
							{
								pushable.Pushable.Nudge(new NudgeInfo
								{
									OriginPos = pushable.Pushable.ServerPosition,
									Trajectory = (Vector2) windyNode.WindDirection,
									SpinMode = SpinMode.None,
									SpinMultiplier = 1,
									InitialSpeed = correctedForce,
								});
							}
						}
					}
					windyNode.WindForce = (windyNode.WindForce * (n - 1) / n);
					if (windyNode.WindForce >= 0.5f)
					{
						winds.Enqueue(windyNode);
					}
					else
					{
						windyNode.WindForce = 0;
						windyNode.WindDirection = Vector2Int.zero;
					}

				}
			}
		}

		timePassed2 = 0;

		Profiler.EndSample();

		#endregion

		if (timePassed < 0.5)
		{
			return;
		}


		//process the current hotspots, potentially adding new ones and removing ones that have expired.
		//(but we actually perform the add / remove after this loop so we don't concurrently modify the dict)
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
								ExposeHotspot(node.Neighbors[i].Position, node.GasMix.Temperature * 0.85f,
									node.GasMix.Volume / 4);
							}
						}
					}
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
		//perform the actual logic that needs to happen for adding / removing hotspots that have been
		//queued up to be added / removed
		foreach (var addedHotspot in hotspotsToAdd)
		{
			if (!hotspots.ContainsKey(addedHotspot.node.Position) &&
			    // only process the addition if it hasn't already been done, which
			    // could happen if multiple things try to add a hotspot to the same tile
			    addedHotspot.node.Hotspot == null)
			{
				addedHotspot.node.Hotspot = addedHotspot;
				hotspots.Add(addedHotspot.node.Position, addedHotspot.node);
				tileChangeManager.UpdateTile(
					new Vector3Int(addedHotspot.node.Position.x, addedHotspot.node.Position.y, FIRE_FX_Z),
					TileType.Effects, "Fire");

				if(fireLightDictionary.ContainsKey(addedHotspot.node.Position)) continue;

				var fireLightSpawn = Spawn.ServerPrefab(fireLight, addedHotspot.node.Position, transform);

				fireLightDictionary.Add(addedHotspot.node.Position, fireLightSpawn.GameObject);
			}
		}

		foreach (var removedHotspot in hotspotsToRemove)
		{
			if (hotspots.TryGetValue(removedHotspot, out var affectedNode) &&
			    // only process the removal if it hasn't already been done, which
			    // could happen if multiple things try to remove a hotspot to the same tile)
			    affectedNode.HasHotspot)
			{
				affectedNode.Hotspot = null;
				tileChangeManager.RemoveTile(
					new Vector3Int(affectedNode.Position.x, affectedNode.Position.y, FIRE_FX_Z),
					LayerType.Effects, false);
				hotspots.Remove(removedHotspot);

				if(!fireLightDictionary.ContainsKey(affectedNode.Position)) continue;

				var fireObject = fireLightDictionary[affectedNode.Position];

				if (fireObject != null)
				{
					Despawn.ServerSingle(fireLightDictionary[affectedNode.Position]);
				}

				fireLightDictionary.Remove(affectedNode.Position);
			}
		}

		hotspotsToAdd.Clear();
		hotspotsToRemove.Clear();
		Profiler.EndSample();

		Profiler.BeginSample("GasReactions");

		int gasReactionCount = addReaction.Count;
		if (gasReactionCount > 0)
		{
			for (int i = gasReactionCount; i >= 0; i--)
			{
				if (addReaction.TryDequeue(out var addReactionNode))
				{
					var gasMix = addReactionNode.metaDataNode.GasMix;

					addReactionNode.gasReaction.Reaction.React(ref gasMix, addReactionNode.metaDataNode.Position);

					if (reactions.TryGetValue(addReactionNode.metaDataNode.Position, out var gasHashSet) && gasHashSet.Count == 1)
					{
						reactions.TryRemove(addReactionNode.metaDataNode.Position, out var value);
						continue;
					}

					reactions[addReactionNode.metaDataNode.Position].Remove(addReactionNode.gasReaction);
				}
			}
		}

		Profiler.EndSample();

		#region TileOverlays

		Profiler.BeginSample("FogModifyAdd");
		//Here we check to see if chemical fog fx needs to be applied, and if so, add them. If not, we remove them
		int addFogCount = addFog.Count;
		if (addFogCount > 0)
		{
			for (int i = 0; i < addFogCount; i++)
			{
				if (addFog.TryDequeue(out var addFogNode))
				{
					if (fogTiles.ContainsKey(addFogNode.metaDataNode.Position))
					{
						if (fogTiles[addFogNode.metaDataNode.Position].Contains(addFogNode.gas)) continue;

						fogTiles[addFogNode.metaDataNode.Position].Add(addFogNode.gas); //Add it to fogTiles
					}
					else
					{
						fogTiles.Add(addFogNode.metaDataNode.Position, new HashSet<Gas>{addFogNode.gas}); //Add it to fogTiles
					}

					tileChangeManager.UpdateTile(
						new Vector3Int(addFogNode.metaDataNode.Position.x, addFogNode.metaDataNode.Position.y, addFogNode.gas.OverlayIndex),
						TileType.Effects, addFogNode.gas.TileName);
				}
			}
		}

		Profiler.EndSample();
		Profiler.BeginSample("FogModifyRemove");

		//Similar to above, but for removing chemical fog fx
		int removeFogCount = removeFog.Count;
		if (removeFogCount > 0)
		{
			for (int i = 0; i < removeFogCount; i++)
			{
				if (removeFog.TryDequeue(out var removeFogNode))
				{
					if (!fogTiles.ContainsKey(removeFogNode.metaDataNode.Position)) continue;

					if (!fogTiles[removeFogNode.metaDataNode.Position].Contains(removeFogNode.gas)) continue;

					tileChangeManager.RemoveTile(
						new Vector3Int(removeFogNode.metaDataNode.Position.x, removeFogNode.metaDataNode.Position.y, removeFogNode.gas.OverlayIndex),
						LayerType.Effects, false);

					if (fogTiles[removeFogNode.metaDataNode.Position].Count == 1)
					{
						fogTiles.Remove(removeFogNode.metaDataNode.Position);
						continue;
					}

					fogTiles[removeFogNode.metaDataNode.Position].Remove(removeFogNode.gas);
				}
			}
		}

		Profiler.EndSample();

		#endregion

		timePassed = 0;
	}

	/// Same as ExposeHotspot but allows providing a world position and handles the conversion
	public void ExposeHotspotWorldPosition(Vector2Int tileWorldPosition, float temperature, float volume)
	{
		ExposeHotspot(MatrixManager.WorldToLocalInt(tileWorldPosition.To3Int(), MatrixManager.Get(matrix)), temperature,
			volume);
	}

	private void RemoveHotspot(MetaDataNode node)
	{
		//removal will be processed later in update
		hotspotsToRemove.Add(node.Position);
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

			if (gasMix.GetMoles(Gas.Plasma) > 0.5 && gasMix.GetMoles(Gas.Oxygen) > 0.5 &&
			    temperature > Reactions.PlasmaMaintainFire)
			{
				// igniting
				//addition will be done later in Update
				hotspotsToAdd.Add(new Hotspot(node, temperature, volume * 25));
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


		//update fire exposure, reusing it to avoid creating GC.
		applyExposure.Update(isSideExposure, hotspots[hotspotPosition], hotspotWorldPosition, atLocalPosition,
			atWorldPosition);
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
			matrix.ForEachRegisterTileSafe(applyExposure, atLocalPosition, true);
			//expose the tiles there
			foreach (var tilemapDamage in tilemapDamages)
			{
				tilemapDamage.OnExposed(applyExposure.FireExposure);
			}

			Profiler.EndSample();
		}
		else
		{
			Profiler.BeginSample("DirectExposure");
			//direct exposure logic
			matrix.ForEachRegisterTileSafe(applyExposure, atLocalPosition, true);
			//expose the tiles
			foreach (var tilemapDamage in tilemapDamages)
			{
				tilemapDamage.OnExposed(applyExposure.FireExposure);
			}

			Profiler.EndSample();
		}
	}

	public void AddWindEvent(MetaDataNode node, Vector2Int windDirection, float pressureDifference)
	{
		if (node != MetaDataNode.None && pressureDifference > AtmosConstants.MinWindForce
		                              && windDirection != Vector2Int.zero)
		{
			if (node.WindForce == 0)
			{
				node.WindForce = pressureDifference;
				node.WindDirection = windDirection;
				winds.Enqueue(node);
			}
			else
			{
				if (n == 0)
				{
					n = 1;
				}

				node.WindForce = (node.WindForce * (n - 1) / n) + pressureDifference / n;
			}
		}
	}

	//Add tile to add fog effect queue
	//Being called by AtmosSimulation
	public void AddFogEvent(FogEffect node)
	{
		addFog.Enqueue(node);
	}

	//Add tile to remove fog effect queue
	//Being called by AtmosSimulation
	public void RemoveFogEvent(FogEffect node)
	{
		removeFog.Enqueue(node);
	}

	//Add tile to add reaction effect queue
	//Being called by AtmosSimulation
	public void AddReactionEvent(ReactionData node)
	{
		if (reactions.TryGetValue(node.metaDataNode.Position, out var gasHastSet))
		{
			if(gasHastSet.Contains(node.gasReaction)) return;

			gasHastSet.Add(node.gasReaction);
		}
		else
		{
			reactions.TryAdd(node.metaDataNode.Position, new HashSet<GasReactions>{node.gasReaction});
		}

		addReaction.Enqueue(node);
	}

	/// <summary>
	/// So we can avoid GC caused by creating lambdas, we create one instance of this and re-use it when
	/// applying a fire exposure to multiple objects
	/// </summary>
	private class ApplyExposure : IRegisterTileAction
	{
		private FireExposure fireExposure = new FireExposure();
		public FireExposure FireExposure => fireExposure;
		private bool isSideExposure;

		/// <summary>
		/// Modify this exposure to be for a different node / tile
		/// </summary>
		/// <param name="hotspotNode"></param>
		/// <param name="hotspotWorldPosition"></param>
		/// <param name="atLocalPosition"></param>
		/// <param name="atWorldPosition"></param>
		public void Update(bool isSideExposure, MetaDataNode hotspotNode, Vector3Int hotspotWorldPosition,
			Vector3Int atLocalPosition, Vector3Int atWorldPosition)
		{
			this.isSideExposure = isSideExposure;
			FireExposure.Update(hotspotNode, hotspotWorldPosition, atLocalPosition, atWorldPosition);
		}

		public void Invoke(RegisterTile registerTile)
		{
			if (isSideExposure)
			{
				if (registerTile.IsAtmosPassable(FireExposure.HotspotLocalPosition, true))
				{
					registerTile.OnExposed(FireExposure);
				}
			}
			else
			{
				registerTile.OnExposed(FireExposure);
			}
		}
	}

	public class FogEffect
	{
		public MetaDataNode metaDataNode;
		public Gas gas;
	}

	public class ReactionData
	{
		public MetaDataNode metaDataNode;
		public GasReactions gasReaction;
	}
}