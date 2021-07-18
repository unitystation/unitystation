using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;
using TileManagement;

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
		public GameObject FireLightPrefab => fireLight;

		private Dictionary<Vector3Int, GameObject> fireLightDictionary = new Dictionary<Vector3Int, GameObject>();

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
			for (int i = hotspotsToAdd.Count - 1; i >= 0; i--)
			{
				var addedHotspot = hotspotsToAdd[i];
				if (!hotspots.ContainsKey(addedHotspot.node.Position) &&
				    // only process the addition if it hasn't already been done, which
				    // could happen if multiple things try to add a hotspot to the same tile
				    addedHotspot.node.Hotspot == null)
				{
					addedHotspot.node.Hotspot = addedHotspot;
					hotspots.TryAdd(addedHotspot.node.Position, addedHotspot.node);
					addedHotspot.OnCreation();
				}
			}

			for (int i = hotspotsToRemove.Count - 1; i >= 0; i--)
			{
				var removedHotspot = hotspotsToRemove[i];
				if (hotspots.TryGetValue(removedHotspot, out var affectedNode) &&
				    // only process the removal if it hasn't already been done, which
				    // could happen if multiple things try to remove a hotspot to the same tile)
				    affectedNode.HasHotspot)
				{
					affectedNode.Hotspot.OnRemove();
					affectedNode.Hotspot = null;
					hotspots.TryRemove(removedHotspot, out var value);
				}
			}

			hotspotsToAdd.Clear();
			hotspotsToRemove.Clear();

			//Do hotspot checks every 0.5 seconds
			timePassed += Time.deltaTime;
			if (timePassed < 0.5)
			{
				Profiler.EndSample();
				return;
			}
			timePassed = 0;

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

			reactionTick++;
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
			if (reactionTick <= 3)
			{
				return;
			}

			reactionTick = 0;

			//process the current hotspots, removing ones that can't sustain anymore.
			//(but we actually perform the add / remove after this loop so we don't concurrently modify the dict)
			foreach (MetaDataNode node in hotspots.Values)
			{
				if (IsAllowedHotSpot(node))
				{
					node.Hotspot.Process();
				}
				else
				{
					RemoveHotspot(node);
				}
			}
		}

		/// <summary>
		/// Same as ExposeHotspot but allows providing a world position and handles the conversion
		/// </summary>
		/// <param name="tileWorldPosition">World position</param>
		/// <param name="exposeTemperature">The temperature the hotspot will think it is for validation, leave to -1 for
		/// the current gas mix temperature</param>
		/// <param name="changeTemp">Will change temperature of tile to the exposeTemperature if this temperature
		/// is greater than the current gas mix temperature when true and only if the tile is on fire</param>
		public void ExposeHotspotWorldPosition(Vector2Int tileWorldPosition, float exposeTemperature = -1f, bool changeTemp = false)
		{
			ExposeHotspot(MatrixManager.WorldToLocalInt(tileWorldPosition.To3Int(), MatrixManager.Get(matrix)),
				exposeTemperature, changeTemp);
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

		/// <summary>
		/// Creates a hotspot if the tile is hot enough
		/// </summary>
		/// <param name="localPosition">Local position on the matrix</param>
		/// <param name="exposeTemperature">The temperature the hotspot will think it is for validation, leave to -1 for
		/// the current gas mix temperature</param>
		/// <param name="changeTemp">Will change temperature of tile to the exposeTemperature if this temperature
		/// is greater than the current gas mix temperature when true and only if the tile is on fire</param>
		/// <param name="doExposure">Do exposure on the tiles, do not do for non main thread calls</param>
		public void ExposeHotspot(Vector3Int localPosition, float exposeTemperature = -1f, bool changeTemp = false, bool doExposure = true)
		{
			//Try add new hotspot if we dont already have one or is null
			if (hotspots.TryGetValue(localPosition, out var hotspot) == false || hotspot.HasHotspot == false)
			{
				Profiler.BeginSample("MarkForAddition");

				InternalTryAddHotspot(localPosition, exposeTemperature);

				Profiler.EndSample();

				//Return here as InternalTryAddHotspot only adds it to the queue for new hotspots
				//So hotspot wont be available for next section yet
				return;
			}

			//If we are already a hotspot try to increase temperature if allowed to
			if (changeTemp && hotspot.GasMix.Temperature < exposeTemperature)
			{
				hotspot.GasMix.SetTemperature(exposeTemperature);
			}

			//Only do expose if allowed, prevents thread errors when being called off of main thread
			if(doExposure == false) return;

			//If we already have hotspot expose everything on this tile
			Expose(localPosition, localPosition);

			//Expose impassable things on the adjacent tiles too
			Expose(localPosition, localPosition + Vector3Int.right);
			Expose(localPosition, localPosition + Vector3Int.left);
			Expose(localPosition, localPosition + Vector3Int.up);
			Expose(localPosition, localPosition + Vector3Int.down);
		}

		private void InternalTryAddHotspot(Vector3Int localPosition, float exposeTemperature = -1f)
		{
			MetaDataNode node = metaDataLayer.Get(localPosition, false);

			if(IsAllowedHotSpot(node, exposeTemperature) == false) return;

			//Igniting
			//Addition will be done later in Update
			hotspotsToAdd.Add(new Hotspot(node));
		}

		private bool IsAllowedHotSpot(MetaDataNode node, float exposeTemperature = -1f)
		{
			//Only need to check stuff which has nodes as we are checking gas contents afterwards
			if(node == null) return false;

			GasMix gasMix = node.GasMix;

			if (exposeTemperature < 0)
			{
				exposeTemperature = gasMix.Temperature;
			}

			//Minimum temperature requirement
			if(exposeTemperature < AtmosDefines.FIRE_MINIMUM_TEMPERATURE_TO_EXIST) return false;

			//Minimum oxygen requirement
			if(gasMix.GetMoles(Gas.Oxygen) < 0.5f) return false;

			//Minimum plasma/tritium requirement
			if (gasMix.GetMoles(Gas.Plasma) < 0.5f && gasMix.GetMoles(Gas.Tritium) < 0.5f)
			{
				return false;
			}

			//Passed all checks, this position is allowed a hotspot
			return true;
		}

		private void Expose(Vector3Int hotspotPosition, Vector3Int atLocalPosition)
		{
			try
			{
				InternalExpose(hotspotPosition, atLocalPosition);
			}
			catch (Exception e)
			{
				Logger.LogError(e.ToString());
			}
		}

		private void InternalExpose(Vector3Int hotspotPosition, Vector3Int atLocalPosition)
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
				matrix.ServerObjects.InvokeOnObjects(applyExposure, atLocalPosition);
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
				matrix.ServerObjects.InvokeOnObjects(applyExposure, atLocalPosition);
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
