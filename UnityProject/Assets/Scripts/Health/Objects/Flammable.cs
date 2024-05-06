using System;
using System.Collections.Generic;
using Effects.Overlays;
using HealthV2;
using Logs;
using Mirror;
using ScriptableObjects;
using Systems.Atmospherics;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using UnityEngine.Profiling;
using Util.Independent.FluentRichText;
using Random = UnityEngine.Random;

namespace Health.Objects
{
	[RequireComponent(typeof(Integrity))]
	[RequireComponent(typeof(RegisterTile))]
	public class Flammable : NetworkBehaviour, IServerSpawn, IRightClickable, IHoverTooltip, IExaminable
	{
		private Integrity integrity;
		public Integrity Integrity => integrity;
		private RegisterTile registerTile;

		[SyncVar(hook = nameof(SyncOnFire))]
		private int fireStacks = 0;
		private BurningOverlay burningObjectOverlay;

		public bool IsOnFire => fireStacks > 0;

		private static GameObject SMALL_BURNING_PREFAB;
		private static GameObject LARGE_BURNING_PREFAB;
		[SerializeField] private GameObject fireParticlePrefab;
		private ParticleSystem fireParticle;

		private static OverlayTile SMALL_ASH;
		private static OverlayTile LARGE_ASH;

		// damage incurred each tick while an object is on fire
		private static float BURNING_DAMAGE_PER_STACK = 0.08f;
		private static float HOT_IN_KELVIN = 750f;
		private static readonly float BURN_RATE = 1.25f;

		private bool isLarge = false;

		[SerializeField] private float minimumFireDamageForStack = 12;
		[SerializeField, SyncVar] private float chanceToSpread = 20;
		[SerializeField] private int maxStacks = 20;

		[SyncVar] private long lastBurnStackTickTime = 0;
		private const long BURN_STACK_TICK_INTERVAL = 60 * TimeSpan.TicksPerSecond; // 60 seconds in ticks

		private bool isUpdating = false;

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
			integrity.OnDestruction.AddListener(ExtingushFireAndDestroy);
			integrity.OnApplyDamage.AddListener(OnDamageReceived);
			EnsureInit();
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer && isUpdating)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdateBurn);
			}
		}

		private void EnsureInit()
		{
			if (SMALL_BURNING_PREFAB == null)
			{
				SMALL_BURNING_PREFAB = CommonPrefabs.Instance.BurningSmall;
				LARGE_BURNING_PREFAB = CommonPrefabs.Instance.BurningLarge;
			}

			if (SMALL_ASH == null)
			{
				SMALL_ASH = TileManager.GetTile(TileType.Effects, "SmallAsh") as OverlayTile;
				LARGE_ASH = TileManager.GetTile(TileType.Effects, "LargeAsh") as OverlayTile;
			}
			//this is just a guess - large items can't be picked up
			isLarge = GetComponent<Pickupable>() == null;
			if (integrity.Resistances.Flammable)
			{
				ToggleOverlay(false);
			}
		}

		public override void OnStartClient()
		{
			SyncOnFire(fireStacks, fireStacks);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (info.SpawnType == SpawnType.Clone)
			{
				//cloned
				var clonedIntegrity = info.ClonedFrom.GetComponent<Flammable>();
				SyncOnFire(fireStacks, clonedIntegrity.fireStacks);
			}
			else
			{
				ToggleOverlay(false);
				SyncOnFire(fireStacks, 0);
			}
		}

		private void PeriodicUpdateBurn()
		{
			//Instantly stop burning if there's no oxygen at this location
			MetaDataNode node = registerTile.Matrix.MetaDataLayer.Get(registerTile.LocalPositionServer);
			if (node?.GasMixLocal.GetMoles(Gas.Oxygen) < 1)
			{
				SyncOnFire(fireStacks, 0);
				return;
			}

			integrity.ApplyDamage(BURNING_DAMAGE_PER_STACK * fireStacks, AttackType.Fire, DamageType.Burn);
			node?.GasMixLocal.AddGas(Gas.Smoke, BURNING_DAMAGE_PER_STACK * 75, Kelvin.FromC(100f));

			long currentTickTime = DateTime.UtcNow.Ticks;
			long timeElapsed = currentTickTime - lastBurnStackTickTime;
			ToggleOverlay(true);
			if (timeElapsed >= BURN_STACK_TICK_INTERVAL)
			{
				lastBurnStackTickTime = currentTickTime;
				// we only do this every 60 seconds to avoid constant GC stutters when there's hundreds of objects on fire.
				CreateHotSpot(registerTile.LocalPosition, HOT_IN_KELVIN + fireStacks);
				SyncOnFire(fireStacks, fireStacks - 1);
				Spread();
			}
		}

		private void Spread()
		{
			if (DMMath.Prob(chanceToSpread) == false) return;
			var flammables = MatrixManager.GetAdjacent<Flammable>(gameObject.AssumedWorldPosServer().CutToInt(), true);
			var healths = MatrixManager.GetAdjacent<LivingHealthMasterBase>(gameObject.AssumedWorldPosServer().CutToInt(), true);
			foreach (var flammable in flammables)
			{
				if (flammable.gameObject == gameObject) continue;
				if (flammable.Integrity.Resistances.Flammable == false) continue;
				flammable.SyncOnFire(flammable.fireStacks, flammable.fireStacks + Random.Range(1, 4));
			}
			foreach (var health in healths)
			{
				health.ChangeFireStacks(health.FireStacks + Random.Range(1, 5));
			}
		}

		private void SyncOnFire(int oldStacks, int newStacks)
		{
			fireStacks = Mathf.Clamp(newStacks, 0, maxStacks);
			ToggleProcessing(isUpdating, IsOnFire);
		}

		private void ToggleOverlay(bool state)
		{
			if (burningObjectOverlay == null)
			{
				burningObjectOverlay = Instantiate(isLarge ? LARGE_BURNING_PREFAB : SMALL_BURNING_PREFAB, transform).GetComponent<BurningOverlay>();
			}
			if (burningObjectOverlay == null)
			{
				Loggy.LogError("[Flammable/ToggleOverlay] - Failed to instantiate burning object overlay");
				return;
			}
			burningObjectOverlay.enabled = state;
			if (state)
			{
				burningObjectOverlay.Burn();
			}
			else
			{
				burningObjectOverlay.StopBurning();
			}
			HandleFireParticles();
		}

		private void HandleFireParticles()
		{
			if (fireParticlePrefab == null) return;
			if (fireStacks == maxStacks)
			{
				if (fireParticle == null)
				{
					fireParticle = Instantiate(fireParticlePrefab, transform).GetComponent<ParticleSystem>();
				}
				fireParticle.Play();
				fireParticle.SetActive(true);
			}
			else
			{
				if (fireParticle != null)
				{
					fireParticle.Stop();
					fireParticle.SetActive(false);
				}
			}
		}

		private void ToggleProcessing(bool oldState, bool newState)
		{
			if (CustomNetworkManager.IsServer == false) return;
			isUpdating = newState;
			if (newState == true && oldState == false)
			{
				UpdateManager.Add(PeriodicUpdateBurn, BURN_RATE);
				Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()} catches on fire!".Color(Color.red), gameObject);
				registerTile = gameObject.RegisterTile();
				CreateHotSpot(registerTile.LocalPosition, HOT_IN_KELVIN + fireStacks);
				ToggleOverlay(true);
				return;
			}
			if (oldState == true && newState == false)
			{
				UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdateBurn);
				Chat.AddLocalMsgToChat($"The {gameObject.ExpensiveName()} is no longer on fire..", gameObject);
				ToggleOverlay(false);
			}
		}

		public void ExtingushFireAndDestroy()
		{
			if (IsOnFire == false) return;
			DefaultBurnUp();
		}

		[Server]
		private void DefaultBurnUp()
		{
			Profiler.BeginSample("DefaultBurnUp");
			registerTile = gameObject.RegisterTile();
			if (LARGE_ASH == null || SMALL_ASH == null)
			{
				Loggy.LogError("[Flammable/DefaultBurnUp] - HEY SHITASS, Failed to find burning object overlay");
				return;
			}
			else
			{
				registerTile.TileChangeManager.MetaTileMap.AddOverlay(registerTile.LocalPosition, isLarge ? LARGE_ASH : SMALL_ASH);
			}
			Chat.AddLocalDestroyMsgToChat(gameObject.ExpensiveName(), " burnt to ash.", gameObject);
			Loggy.LogTraceFormat("{0} burning up, onfire is {1} (burningObject enabled {2})", Category.Health, name, this.fireStacks, burningObjectOverlay?.enabled);
			Profiler.EndSample();
		}

		public void OnDamageReceived(DamageInfo info)
		{
			if (integrity.Resistances.Flammable == false) return;
			if (info.Damage < minimumFireDamageForStack) return;
			if (info.DamageType == DamageType.Burn || info.AttackType == AttackType.Fire)
			{
				fireStacks += 1;
			}
		}

		public void AddFireStacks(int amount)
		{
			SyncOnFire(fireStacks, fireStacks + amount);
		}

		/// <summary>
		/// EXPENSIVE, DO NOT SPAM THIS EVERY FRAME.
		/// creates a hotspot for a given position (duh) which spreads around to nearby tiles by default, temprature can be defined for that spot.
		/// </summary>
		/// <param name="tilePos">I.e: registerTile.LocalPosition</param>
		/// <param name="fireHotspotTemperature">In kelvin, 310 is 36.85'c.</param>
		/// <param name="changeTemperatureOnHotspot">True by default, causes temperature to change for that spot.</param>
		public static void CreateHotSpot(Vector3Int tilePos, float fireHotspotTemperature, bool changeTemperatureOnHotspot = true)
		{
			var reactionManager = MatrixManager.AtPoint(tilePos, true).ReactionManager;
			reactionManager.ExposeHotspotWorldPosition(tilePos.To2Int(), fireHotspotTemperature, changeTemperatureOnHotspot);
		}

		private void DebugAddStacks()
		{
			SyncOnFire(fireStacks, fireStacks + 20);
		}

		private void ResetFireStacks()
		{
			SyncOnFire(fireStacks, 0);
		}

		private void DebugMakeItAlwaysSpread()
		{
			chanceToSpread = 100;
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken) || KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold) == false)
			{
				return null;
			}

			if (IsOnFire)
			{
				return RightClickableResult.Create()
					.AddAdminElement("[Debug] - Set firestacks to 0", ResetFireStacks)
					.AddAdminElement("[Debug] - Set fire spread chance to 100%", DebugMakeItAlwaysSpread);
			}
			else
			{
				return RightClickableResult.Create()
					.AddAdminElement("[Debug] - Add 20 fire stacks", DebugAddStacks);
			}
		}

		public string HoverTip()
		{
			if (IsOnFire == false) return null;
			if (string.IsNullOrEmpty(PlayerList.Instance.AdminToken) == false && KeyboardInputManager.Instance.CheckKeyAction(KeyAction.ShowAdminOptions, KeyboardInputManager.KeyEventType.Hold))
			{
				long currentTickTime = DateTime.UtcNow.Ticks;
				long timeElapsed = currentTickTime - lastBurnStackTickTime;
				return $"Firestacks: {fireStacks}\n last tick: {lastBurnStackTickTime}\n timeElapsed: {timeElapsed}\n spreadChance: {chanceToSpread}%".Color(RichTextColor.Yellow);
			}
			return "It's on fire!".Color(Color.red).Bold();
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			if (IsOnFire == false) return null;
			return new List<TextColor> { new TextColor() { Text = "Find and use a fire extingusher!", Color = Color.red } };
		}

		public string Examine(Vector3 worldPos = default)
		{
			if (IsOnFire == false) return null;
			return "It's on fire!".Color(Color.red).Bold();
		}
	}
}
