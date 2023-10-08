using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Logs;
using Messages.Server;
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
		private float RollingAverageN = 50;
		private float PushMultiplier = 4;

		private GameObject fireLight = null;
		public GameObject FireLightPrefab => fireLight;

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

		private HashSet<MetaDataNode> activeWindEffectSpots = new HashSet<MetaDataNode>(30);

		private HashSet<MetaDataNode> windEffectsToRemove = new HashSet<MetaDataNode>(30);

		private List<WindEffectData> windEffectNodes = new List<WindEffectData>(30);

		private const float WindParticleBlockTime = 3f;


		public enum WindStrength
		{
			SOUND_ONLY = 3, //TODO : Add wind noise.
			WEAK = 6, //Tile changes
			STRONG = 9, //Garbage room/pipes wind
			SPACE_VACUUM = 12 //Broken window or open airlock to the vast vacuum of space.
		}

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
			fireLight = AtmosManager.Instance.FireLight;
			AtmosManager.Instance.reactionManagerList.Add(this);
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

		public struct WindEffectData
		{
			public uint MatrixObject;
			public Vector3	LocalPosition;
			public Vector2	TargetVector;

			public WindEffectData(uint matrixNetId, Vector3 localPosition, Vector2 targetVector)
			{
				MatrixObject = matrixNetId;
				LocalPosition = localPosition;
				TargetVector = targetVector;
			}
		}

		private void UpdateMe()
		{
			Profiler.BeginSample("Wind Effect Time Check");

			windEffectsToRemove.Clear();
			foreach (var metaDataNode in activeWindEffectSpots)
			{
				metaDataNode.windEffectTime -= Time.deltaTime;
				if(metaDataNode.windEffectTime > 0) continue;
				windEffectsToRemove.Add(metaDataNode);
			}

			foreach (var metaDataNode in windEffectsToRemove)
			{
				activeWindEffectSpots.Remove(metaDataNode);
			}

			Profiler.EndSample();

			Profiler.BeginSample("Wind");

			windEffectNodes.Clear();

			winds.Iterate(windsNodeDelegator);

			PlayWindEffect.SendToAll(windEffectNodes);

			Profiler.EndSample();

			Profiler.BeginSample("HotspotModify");
			//perform the actual logic that needs to happen for adding / removing hotspots that have been
			//queued up to be added / removed
			for (int i = hotspotsToAdd.Count - 1; i >= 0; i--)
			{
				var addedHotspot = hotspotsToAdd[i];
				if (!hotspots.ContainsKey(addedHotspot.node.LocalPosition) &&
				    // only process the addition if it hasn't already been done, which
				    // could happen if multiple things try to add a hotspot to the same tile
				    addedHotspot.node.Hotspot == null)
				{
					addedHotspot.node.Hotspot = addedHotspot;
					hotspots.TryAdd(addedHotspot.node.LocalPosition, addedHotspot.node);
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
					hotspots.TryRemove(removedHotspot, out _);
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
				ExposeHotspot(node.LocalPosition);
			}

			reactionTick++;
			Profiler.EndSample();
		}

		private void ProcessWindNodes(MetaDataNode windyNode)
		{
			windyNode.WindData[(int) PushType.Wind] = (Vector2) windyNode.WindDirection * (windyNode.WindForce);

			var registerTiles = matrix.GetRegisterTile(windyNode.LocalPosition, true);
			for (int i = 0; i < registerTiles.Count; i++)
			{
				var registerTile = registerTiles[i];

				//Quicker to get all RegisterTiles and grab the cached PushPull component from it than to get it manually using Get<>
				if (registerTile.ObjectPhysics.HasComponent == false) continue;
				if (registerTile.ObjectPhysics.Component.Intangible) continue;
				var pushable = registerTile.ObjectPhysics.Component;

				float correctedForce = (windyNode.WindForce ) / (int) pushable.GetSize();

				correctedForce = Mathf.Clamp(correctedForce, 0, 30);

				if (pushable.CanBeWindPushed)
				{
					pushable.NewtonianPush(registerTile.transform.rotation * (Vector2)windyNode.WindDirection, Random.Range((float)(correctedForce * 0.8), correctedForce),  spinFactor: Random.Range(1, 150));
				}


				if (pushable.stickyMovement && windyNode.WindForce > (int)WindStrength.STRONG && pushable.CanBeWindPushed )
				{
					if (windyNode.WindForce * 0.15f > 0.25f)
					{
						pushable.NewtonianPush(windyNode.WindDirection, windyNode.WindForce * 0.05f,
							windyNode.WindForce * 0.05f, spinFactor: Random.Range(20, 150));
					}
				}
			}

			windyNode.WindForce = (windyNode.WindForce * ((RollingAverageN - 1) / RollingAverageN));
			if (windyNode.WindForce < 0.25f)
			{
				winds.Remove(windyNode);
				windyNode.WindForce = 0;
				windyNode.WindDirection = Vector2Int.zero;
				windyNode.WindData[(int) PushType.Wind] = Vector2.zero;
				return;
			}

			if(activeWindEffectSpots.Contains(windyNode)) return;
			windyNode.windEffectTime = WindParticleBlockTime;
			activeWindEffectSpots.Add(windyNode);

			windEffectNodes.Add(new WindEffectData(windyNode.PositionMatrix.NetworkedMatrix.MatrixSync.netId,
				windyNode.LocalPosition, windyNode.WindDirection));
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
			hotspotsToRemove.Add(node.LocalPosition);
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
			if(node.Exists == false) return;

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
				Loggy.LogError(e.ToString());
			}
		}

		private void InternalExpose(Vector3Int hotspotPosition, Vector3Int atLocalPosition)
		{
			Profiler.BeginSample("ExposureInit");

			if (hotspots.ContainsKey(hotspotPosition) == false)
			{
				Loggy.LogError("Hotspot position key was not found in the hotspots dictionary", Category.Atmos);
				return;
			}

			var isSideExposure = hotspotPosition != atLocalPosition;

			Profiler.EndSample();

			if (isSideExposure)
			{
				//Side exposure logic
				SideExposure(hotspotPosition, atLocalPosition);
				return;
			}

			//Direct exposure logic
			DirectExposure(hotspotPosition, atLocalPosition);
		}

		private void DirectExposure(Vector3Int hotspotPosition, Vector3Int atLocalPosition)
		{
			Profiler.BeginSample("DirectExposure");

			//Calculate world position
			var hotspotWorldPosition = MatrixManager.LocalToWorldInt(hotspotPosition, matrix.MatrixInfo);
			var atWorldPosition = MatrixManager.LocalToWorldInt(atLocalPosition, matrix.MatrixInfo);

			//Update fire exposure, reusing it to avoid creating GC.
			applyExposure.Update(false, hotspots[hotspotPosition], hotspotWorldPosition, atLocalPosition,
				atWorldPosition);

			matrix.ServerObjects.InvokeOnObjects(applyExposure, atLocalPosition);

			//Expose the tiles
			foreach (var tilemapDamage in tilemapDamages)
			{
				tilemapDamage.OnExposed(applyExposure.FireExposure);
			}

			Profiler.EndSample();
		}

		private void SideExposure(Vector3Int hotspotPosition, Vector3Int atLocalPosition)
		{
			Profiler.BeginSample("SideExposure");

			//Already exposed by a different hotspot
			if (hotspots.ContainsKey(atLocalPosition))
			{
				Profiler.EndSample();
				return;
			}

			var metadata = metaDataLayer.Get(atLocalPosition);
			if (metadata.IsOccupied == false)
			{
				//Atmos can pass here, so no need to check side exposure (nothing to brush up against)
				Profiler.EndSample();
				return;
			}

			//Calculate world position
			var hotspotWorldPosition = MatrixManager.LocalToWorldInt(hotspotPosition, matrix.MatrixInfo);
			var atWorldPosition = MatrixManager.LocalToWorldInt(atLocalPosition, matrix.MatrixInfo);

			//Update fire exposure, reusing it to avoid creating GC.
			applyExposure.Update(false, hotspots[hotspotPosition], hotspotWorldPosition, atLocalPosition,
				atWorldPosition);

			//Only expose to atmos impassable objects, since those are the things the flames would actually brush up against
			matrix.ServerObjects.InvokeOnObjects(applyExposure, atLocalPosition);

			//Expose the tiles there
			foreach (var tilemapDamage in tilemapDamages)
			{
				tilemapDamage.OnExposed(applyExposure.FireExposure);
			}

			Profiler.EndSample();
		}


		public void AddWindEvent(MetaDataNode node, Vector2Int windDirection, float pressureDifference)
		{
			if (node != MetaDataNode.None && pressureDifference > AtmosConstants.MinWindForce
										  && windDirection != Vector2Int.zero)
			{
				winds.AddIfMissing(node);

				node.WindForce = (node.WindForce * ((RollingAverageN - 1) / RollingAverageN)) +
				                 (pressureDifference  * PushMultiplier) / RollingAverageN;
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
