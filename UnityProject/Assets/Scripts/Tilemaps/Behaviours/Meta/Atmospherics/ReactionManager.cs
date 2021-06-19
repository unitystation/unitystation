using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

namespace Systems.Atmospherics
{
	/// <summary>
	/// Handles performing the various reactions that can occur in atmospherics simulation.
	/// </summary>
	public class ReactionManager : MonoBehaviour
	{
		private float RollingAverageN = 20;
		private float PushMultiplier = 5;

		private GameObject fireLight = null;

		private Dictionary<Vector3Int, GameObject> fireLightDictionary = new Dictionary<Vector3Int, GameObject>();

		private Dictionary<Vector3Int, HashSet<Gas>> fogTiles = new Dictionary<Vector3Int, HashSet<Gas>>();
		public Dictionary<Vector3Int, HashSet<Gas>> FogTiles => fogTiles;

		public ConcurrentDictionary<Vector3Int, HashSet<GasReactions>> reactions =
			new ConcurrentDictionary<Vector3Int, HashSet<GasReactions>>();
		public ConcurrentDictionary<Vector3Int, HashSet<GasReactions>> Reactions => reactions;

		private static readonly int FIRE_FX_Z = -2;

		private TileChangeManager tileChangeManager;
		public TileChangeManager TileChangeManager => tileChangeManager;
		private MetaDataLayer metaDataLayer;
		private Matrix matrix;

		private ConcurrentDictionary<Vector3Int, MetaDataNode> hotspots;
		private ThreadSafeList<MetaDataNode> winds;
		private GenericDelegate<MetaDataNode> windsNodeDelegator;

		private List<Hotspot> hotspotsToAdd;
		private List<Vector3Int> hotspotsToRemove;
		private TilemapDamage[] tilemapDamages;

		private float timePassed;
		private int reactionTick;

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

			hotspots = new ConcurrentDictionary<Vector3Int, MetaDataNode>();
			winds =  new ThreadSafeList<MetaDataNode>();
			windsNodeDelegator = ProcessWindNodes;

			hotspotsToRemove = new List<Vector3Int>();
			hotspotsToAdd = new List<Hotspot>();
			tilemapDamages = GetComponentsInChildren<TilemapDamage>();
		}

		private void Start()
		{
			fireLight = AtmosManager.Instance.fireLight;
			AtmosThread.reactionManagerList.Add(this);
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			Profiler.BeginSample("Wind");
			winds.Iterate(windsNodeDelegator);
			Profiler.EndSample();

			Profiler.BeginSample("HotspotModify");
			//perform the actual logic that needs to happen for adding / removing hotspots that have been
			//queued up to be added / removed
			lock (hotspotsToAdd)
			{
				foreach (var addedHotspot in hotspotsToAdd)
				{
					if (!hotspots.ContainsKey(addedHotspot.node.Position) &&
						// only process the addition if it hasn't already been done, which
						// could happen if multiple things try to add a hotspot to the same tile
						addedHotspot.node.Hotspot == null)
					{
						addedHotspot.node.Hotspot = addedHotspot;
						hotspots.TryAdd(addedHotspot.node.Position, addedHotspot.node);
						tileChangeManager.AddOverlay(
							new Vector3Int(addedHotspot.node.Position.x, addedHotspot.node.Position.y, FIRE_FX_Z),
							TileType.Effects, "Fire");

						if (fireLightDictionary.ContainsKey(addedHotspot.node.Position)) continue;

						var fireLightSpawn = Spawn.ServerPrefab(fireLight, addedHotspot.node.Position, transform);

						fireLightDictionary.Add(addedHotspot.node.Position, fireLightSpawn.GameObject);
					}
				}

			}
			lock (hotspotsToRemove)
			{
				foreach (var removedHotspot in hotspotsToRemove)
				{
					if (hotspots.TryGetValue(removedHotspot, out var affectedNode) &&
						// only process the removal if it hasn't already been done, which
						// could happen if multiple things try to remove a hotspot to the same tile)
						affectedNode.HasHotspot)
					{
						affectedNode.Hotspot = null;
						tileChangeManager.RemoveOverlaysOfName(
							new Vector3Int(affectedNode.Position.x, affectedNode.Position.y, FIRE_FX_Z),
							LayerType.Effects, "Fire");
						hotspots.TryRemove(removedHotspot, out var value);

						if (!fireLightDictionary.ContainsKey(affectedNode.Position)) continue;

						var fireObject = fireLightDictionary[affectedNode.Position];

						if (fireObject != null)
						{
							_ = Despawn.ServerSingle(fireLightDictionary[affectedNode.Position]);
						}

						fireLightDictionary.Remove(affectedNode.Position);
					}
				}
			}

			hotspotsToAdd.Clear();
			hotspotsToRemove.Clear();

			timePassed += Time.deltaTime;
			if (timePassed < 0.5)
			{
				Profiler.EndSample();
				return;
			}

			timePassed = 0;
			reactionTick++;

			//hotspot spread to adjacent tiles and damage
			foreach (MetaDataNode node in hotspots.Values)
			{
				foreach (var neighbor in node.Neighbors)
				{
					if (neighbor != null)
					{
						ExposeHotspot(neighbor.Position);
					}
				}
			}

			Profiler.EndSample();
		}

		private void ProcessWindNodes(MetaDataNode windyNode)
		{
			foreach (var pushable in matrix.Get<PushPull>(windyNode.Position, true))
			{
				float correctedForce = (windyNode.WindForce * PushMultiplier) / (int)pushable.Pushable.Size;
				if (correctedForce >= AtmosConstants.MinPushForce)
				{
					if (pushable.Pushable.IsTileSnap)
					{
						byte pushes = (byte)Mathf.Clamp((int)correctedForce / 10, 1, 10);
						for (byte j = 0; j < pushes; j++)
						{
							//converting push to world coords because winddirection is in local coords
							pushable.QueuePush((transform.rotation * windyNode.WindDirection.To3Int()).To2Int(),
								Random.Range((float)(correctedForce * 0.8), correctedForce));
						}
					}
					else
					{
						pushable.Pushable.Nudge(new NudgeInfo
						{
							OriginPos = pushable.Pushable.ServerPosition,
							Trajectory = (Vector2)windyNode.WindDirection,
							SpinMode = SpinMode.None,
							SpinMultiplier = 1,
							InitialSpeed = correctedForce,
						});
					}
				}
			}

			windyNode.WindForce = (windyNode.WindForce * ((RollingAverageN - 1) / RollingAverageN));
			if (windyNode.WindForce < 0.5f * (1f / PushMultiplier))
			{
				winds.Remove(windyNode);
				windyNode.WindForce = 0;
				windyNode.WindDirection = Vector2Int.zero;
			}
		}

		public void DoTick()
		{
			if (reactionTick == 0)
			{
				return;
			}

			reactionTick--;

			//process the current hotspots, removing ones that can't sustain anymore.
			//(but we actually perform the add / remove after this loop so we don't concurrently modify the dict)
			foreach (MetaDataNode node in hotspots.Values)
			{
				if (node.Hotspot != null)
				{
					if (PlasmaFireReaction.CanHoldHotspot(node.GasMix))
					{
						node.Hotspot.Process();
					}
					else
					{
						RemoveHotspot(node);
					}
				}
			}
		}

		/// Same as ExposeHotspot but allows providing a world position and handles the conversion
		public void ExposeHotspotWorldPosition(Vector2Int tileWorldPosition)
		{
			ExposeHotspot(MatrixManager.WorldToLocalInt(tileWorldPosition.To3Int(), MatrixManager.Get(matrix)));
		}

		public void RemoveHotspot(MetaDataNode node)
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

		public void ExposeHotspot(Vector3Int localPosition)
		{
			if (!hotspots.ContainsKey(localPosition) || hotspots[localPosition].Hotspot == null)
			{
				Profiler.BeginSample("MarkForAddition");
				MetaDataNode node = metaDataLayer.Get(localPosition);
				GasMix gasMix = node.GasMix;

				if (PlasmaFireReaction.CanHoldHotspot(gasMix))
				{
					// igniting
					//addition will be done later in Update
					hotspotsToAdd.Add(new Hotspot(node));
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
				winds.AddIfMissing(node);

				node.WindForce = (node.WindForce * ((RollingAverageN - 1) / RollingAverageN)) +
				                 pressureDifference / RollingAverageN;
				node.WindDirection = windDirection;
			}
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
	}
}
