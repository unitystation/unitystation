using System;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Mirror;
using Player.Movement;
using ScriptableObjects;
using Tiles;
using UnityEngine;

namespace Systems.Antagonists
{
	public class AlienPlayer : NetworkBehaviour
	{
		[SerializeField]
		private SpriteHandler mainSpriteHandler;

		[SerializeField]
		private SpriteHandler mainBackSpriteHandler;

		[SerializeField]
		private List<AlienTypeDataSO> typesToChoose = new List<AlienTypeDataSO>();

		[SerializeField]
		private AlienTypes startingAlienType = AlienTypes.Larva1;

		[SerializeField]
		private List<LayerTile> weedTiles = new List<LayerTile>();

		//Used to generate names
		private static int alienCount;

		//Used to generate Queen names
		private static int queenCount;

		//Current alien data SO
		private AlienTypeDataSO currentData;

		[SyncVar]
		private AlienMode currentAlienMode;
		public AlienTypes CurrentAlienType => currentData.AlienType;

		//Plasma value (increase by being on weeds)
		[SyncVar]
		private int currentPlasma;
		public int CurrentPlasma => currentPlasma;
		public float CurrentPlasmaPercentage => (currentPlasma / (float)currentData.MaxPlasma) * 100;

		private LivingHealthMasterBase livingHealthMasterBase;
		public LivingHealthMasterBase LivingHealthMasterBase => livingHealthMasterBase;

		private PlayerScript playerScript;
		private Rotatable rotatable;

		public RegisterPlayer RegisterPlayer => playerScript.registerTile;

		[SyncVar]
		private bool isDead;
		public bool IsDead => isDead;

		private bool onWeeds;

		#region LifeCycle

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			livingHealthMasterBase = GetComponent<LivingHealthMasterBase>();
			rotatable = GetComponent<Rotatable>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(OnUpdate, 1f);
			livingHealthMasterBase.OnConsciousStateChangeServer.AddListener(OnConsciousHealthChange);
			rotatable.OnRotationChange.AddListener(OnRotation);
			playerScript.registerTile.OnLyingDownChangeEvent.AddListener(OnLyingDownChange);
			playerScript.PlayerSync.MovementStateEventServer.AddListener(OnMovementTypeChange);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
			livingHealthMasterBase.OnConsciousStateChangeServer.RemoveListener(OnConsciousHealthChange);
			rotatable.OnRotationChange.RemoveListener(OnRotation);
			playerScript.registerTile.OnLyingDownChangeEvent.RemoveListener(OnLyingDownChange);
			playerScript.PlayerSync.MovementStateEventServer.RemoveListener(OnMovementTypeChange);
		}

		private void Start()
		{
			SetNewPlayer(startingAlienType);
		}

		public override void OnStartLocalPlayer()
		{
			if(isLocalPlayer == false) return;

			UIManager.Instance.panelHudBottomController.AlienUI.SetUp(this);
		}

		#endregion

		#region Setup

		[Server]
		public void SetNewPlayer(AlienTypes newAlien)
		{
			if(isServer == false) return;

			Evolve(newAlien);

			if (currentData.AlienType == AlienTypes.Queen)
			{
				queenCount++;
				playerScript.playerName = $"{currentData.Name} {queenCount:D3}";
				return;
			}

			alienCount++;
			playerScript.playerName = $"{currentData.Name} {alienCount:D3}";
		}

		[ContextMenu("To Queen")]
		public void ToQueen()
		{
			SetNewPlayer(AlienTypes.Queen);
		}

		#endregion

		#region Update

		private void OnUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(currentData == null) return;

			//Dead...
			if(livingHealthMasterBase.IsDead) return;

			WeedCheck();

			PlasmaCheck();

			TryHeal();

			LarvaUpdate();
		}

		#endregion

		#region Larva

		private int growth;

		private void LarvaUpdate()
		{
			if (CurrentAlienType != AlienTypes.Larva1 && CurrentAlienType != AlienTypes.Larva2) return;

			if(currentPlasma <= 0) return;

			//If we are larva 1 or two then we need to continue growing to mature
			growth++;

			if(growth <= 100) return;
			growth = 0;

			Evolve(CurrentAlienType == AlienTypes.Larva1 ? AlienTypes.Larva2 : AlienTypes.Larva3);
		}

		#endregion

		#region Evolution

		private void Evolve(AlienTypes newAlien)
		{
			if (livingHealthMasterBase.IsDead)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You are dead, you cannot evolve!");
				return;
			}

			if (livingHealthMasterBase.ConsciousState == ConsciousState.UNCONSCIOUS)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You are unconscious, you cannot evolve!");
				return;
			}

			var typeFound = typesToChoose.Where(a => a.AlienType == newAlien).ToArray();
			if (typeFound.Length <= 0)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Unable to evolve to {newAlien.ToString()}");
				Logger.LogError($"Could not find alien type: {newAlien.ToString()} in data list!");
				return;
			}

			currentData = typeFound[0];

			ChangeAlienState(AlienMode.Normal);

			playerScript.weaponNetworkActions.SetNewDamageValues(currentData.AttackSpeed,
				currentData.AttackDamage, currentData.DamageType, currentData.ChanceToHit);
		}

		#endregion

		#region Plasma

		private void PlasmaCheck()
		{
			//Don't need to check if full
			if(currentPlasma == currentData.MaxPlasma) return;

			//Need to be on weeds
			if(onWeeds == false) return;

			var change = currentPlasma + currentData.PlasmaGainRate;

			change = Mathf.Clamp(change, 0, currentData.MaxPlasma);

			currentPlasma = change;
		}

		#endregion

		#region Healh

		private void OnConsciousHealthChange(ConsciousState oldState, ConsciousState newState)
		{
			if (newState == ConsciousState.DEAD)
			{
				OnDeath();
				return;
			}

			if(newState == ConsciousState.UNCONSCIOUS) return;

			ChangeAlienState(AlienMode.Unconscious);
		}

		private void TryHeal()
		{
			if(livingHealthMasterBase.HealthPercentage().Approx(100)) return;

			//Sleeping heals twice as fast, but costs twice as much
			var healCost = currentData.HealPlasmaCost * (currentAlienMode == AlienMode.Sleep ? 2 : 1);

			if(currentPlasma < healCost) return;
			currentPlasma -= healCost;

			var healAmount = currentData.HealAmount * (currentAlienMode == AlienMode.Sleep ? 2 : 1);

			livingHealthMasterBase.HealDamageOnAll(null, healAmount, DamageType.Brute);
		}

		#endregion

		#region Death

		private void OnDeath()
		{
			if(isDead) return;
			isDead = true;

			ChangeAlienState(AlienMode.Dead);

			//TODO say on alien chat they've died!

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);
		}

		#endregion

		#region Sleep

		private void OnLyingDownChange(bool isLyingDown)
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(livingHealthMasterBase.IsDead) return;

			if (isLyingDown)
			{
				ChangeAlienState(AlienMode.Sleep);
				return;
			}

			OnMovementTypeChange(playerScript.PlayerSync.CurrentMovementType == MovementType.Running);
		}

		#endregion

		#region Movement

		private void OnMovementTypeChange(bool isRunning)
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(livingHealthMasterBase.IsDead) return;

			if (playerScript.registerTile.IsLayingDown) return;

			ChangeAlienState(isRunning ? AlienMode.Running : AlienMode.Normal);
		}

		#endregion

		#region Weeds

		private void WeedCheck()
		{
			onWeeds = IsOnWeeds();
		}

		private bool IsOnWeeds()
		{
			var tileThere = RegisterPlayer.Matrix.MetaTileMap.GetAllTilesByType<ConnectedTileV2>(RegisterPlayer.LocalPositionServer, LayerType.Floors).ToList();

			if(tileThere.Count == 0) return false;

			var onWeedTile = false;
			foreach (var tileOn in tileThere)
			{
				foreach (var weedTile in weedTiles)
				{
					if (weedTile != tileOn) continue;

					onWeedTile = true;
					break;
				}

				if(onWeedTile) break;
			}

			return onWeedTile;
		}

		#endregion

		#region Sprites

		private void ChangeAlienSprite(AlienMode newSprite)
		{
			mainBackSpriteHandler.PushClear();

			switch (newSprite)
			{
				case AlienMode.Normal:
					SetSpriteSO(currentData.Normal);
					return;
				case AlienMode.Dead:
					SetSpriteSO(currentData.Dead);
					return;
				case AlienMode.Pounce:
					SetSpriteSO(currentData.Pounce);
					return;
				case AlienMode.Sleep:
					SetSpriteSO(currentData.Sleep);
					return;
				case AlienMode.Unconscious:
					SetSpriteSO(currentData.Unconscious);
					return;
				case AlienMode.Running:
					SetSpriteSO(currentData.Running);
					return;
				case AlienMode.Crawling:
					SetSpriteSO(currentData.Front, true);
					return;
			}
		}

		private void SetSpriteSO(SpriteDataSO newSprite, bool doBack = false)
		{
			if (newSprite == null)
			{
				//Don't have custom sprite just use normal
				mainSpriteHandler.SetSpriteSO(currentData.Normal);
				return;
			}

			mainSpriteHandler.SetSpriteSO(newSprite);

			if(doBack == false) return;

			mainBackSpriteHandler.SetSpriteSO(currentData.Back);
		}

		private void OnRotation(OrientationEnum newRotation)
		{
			int spriteVariant = 0;
			switch (newRotation)
			{
				case OrientationEnum.Up_By0:
					spriteVariant = 1;
					break;
				case OrientationEnum.Right_By270:
					spriteVariant = 2;
					break;
				case OrientationEnum.Down_By180:
					spriteVariant = 0;
					break;
				case OrientationEnum.Left_By90:
					spriteVariant = 3;
					break;
			}

			mainSpriteHandler.ChangeSpriteVariant(spriteVariant, false);
			mainBackSpriteHandler.ChangeSpriteVariant(spriteVariant, false);
		}

		#endregion

		#region Misc

		private void ChangeAlienState(AlienMode newState)
		{
			currentAlienMode = newState;
			ChangeAlienSprite(currentAlienMode);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStatics()
		{
			alienCount = 0;
			queenCount = 0;
		}

		#endregion

		public enum AlienMode
		{
			Normal,
			Dead,
			Pounce,
			Sleep,
			Unconscious,
			Running,
			Crawling
		}

		public enum AlienTypes
		{
			//Three larva stages
			Larva1,
			Larva2,
			Larva3,

			Hunter,
			Sentinel,
			Praetorian,
			Drone,

			//God Save the Queen!
			Queen
		}
	}
}