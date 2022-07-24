using System;
using System.Collections.Generic;
using System.Linq;
using Alien;
using Core.Chat;
using HealthV2;
using Mirror;
using Player.Movement;
using ScriptableObjects;
using Tiles;
using UI.Action;
using UnityEngine;

namespace Systems.Antagonists
{
	public class AlienPlayer : NetworkBehaviour, IServerActionGUIMulti
	{
		[Header("Sprite Stuff")]
		[SerializeField]
		private SpriteHandler mainSpriteHandler;

		[SerializeField]
		private SpriteHandler mainBackSpriteHandler;

		[Header("Alien ScriptableObjects")]
		[SerializeField]
		private List<AlienTypeDataSO> typesToChoose = new List<AlienTypeDataSO>();

		[SerializeField]
		private AlienTypes startingAlienType = AlienTypes.Larva1;

		private List<ActionData> actionData = new List<ActionData>();

		public List<ActionData> ActionData => actionData;

		#region ActionData

		[Header("Action Data")]
		[SerializeField]
		private ActionData plantWeeds = null;

		[SerializeField]
		private ActionData layEggs = null;

		#endregion

		#region Prefabs

		[Header("Prefab and Tiles")]
		[SerializeField]
		private List<LayerTile> weedTiles = new List<LayerTile>();

		[SerializeField]
		private GameObject eggPrefab = null;

		[SerializeField]
		private GameObject weedPrefab = null;

		[SerializeField]
		private GameObject spitProjectilePrefab = null;

		#endregion

		//Used to generate names
		private static int alienCount;

		//Used to generate Queen names
		private static int queenCount;

		//Current alien data SO
		private AlienTypeDataSO currentData;

		[SyncVar]
		private AlienMode currentAlienMode;

		[SyncVar(hook = nameof(SyncAlienType))]
		private AlienTypes currentAlienType;
		public AlienTypes CurrentAlienType => currentAlienType;

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

			currentPlasma = 100;

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
			actionData = currentData.ActionData;
			currentAlienType = currentData.AlienType;

			ChangeAlienMode(AlienMode.Normal);

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

		private bool TryRemovePlasma(int toRemove, bool doMessage = true)
		{
			if (currentPlasma < toRemove)
			{
				if (doMessage)
				{
					Chat.AddExamineMsgFromServer(gameObject, $"Not enough plasma! You are missing {toRemove - currentPlasma}");
				}

				return false;
			}

			currentPlasma -= toRemove;

			return true;
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

			ChangeAlienMode(AlienMode.Unconscious);
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

			RpcRemoveActions();

			ChangeAlienMode(AlienMode.Dead);

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
				ChangeAlienMode(AlienMode.Sleep);
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

			ChangeAlienMode(isRunning ? AlienMode.Running : AlienMode.Normal);
		}

		#endregion

		#region Weeds

		private void WeedCheck()
		{
			onWeeds = IsOnWeeds();
		}

		private bool IsOnWeeds()
		{
			var localPos = RegisterPlayer.LocalPositionServer;

			var tileThere = RegisterPlayer.Matrix.MetaTileMap.GetAllTilesByType<ConnectedTileV2>(localPos, LayerType.Floors).ToList();

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

		private const int WeedPlasmaCost = 25;

		private void PlantWeeds()
		{
			if (RegisterPlayer.ObjectPhysics.Component.ContainedInContainer != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You cannot plant weeds from in here!");
				return;
			}

			if(TryRemovePlasma(WeedPlasmaCost) == false) return;

			var weeds = RegisterPlayer.Matrix.GetFirst<AlienWeeds>(RegisterPlayer.LocalPositionServer, true);

			if (weeds != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "There is already a weed node growing here, try somewhere else!");
				return;
			}

			Chat.AddActionMsgToChat(gameObject, $"You start to plant weeds",
				$"{gameObject.ExpensiveName()} starts planting weeds");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction);

			StandardProgressAction.Create(
				cfg,
				() => FinishPlantWeeds()
			).ServerStartProgress(ActionTarget.Object(RegisterPlayer), 5, gameObject);
		}

		private void FinishPlantWeeds()
		{
			if (RegisterPlayer.ObjectPhysics.Component.ContainedInContainer != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You cannot plant weeds from in here!");
				return;
			}

			var spawn = Spawn.ServerPrefab(weedPrefab, RegisterPlayer.ObjectPhysics.Component.OfficialPosition);

			if(spawn.Successful == false) return;

			Chat.AddActionMsgToChat(gameObject, $"You plant new weeds",
				$"{gameObject.ExpensiveName()} plants weeds");
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

		#region Action Button Interactions

		public void CallActionClient(ActionData data)
		{
			if(HasActionData(data) == false) return;

			//Any clientside stuff??
		}

		public void CallActionServer(ActionData data, PlayerInfo sentByPlayer)
		{
			if(HasActionData(data) == false) return;

			if (data == plantWeeds)
			{
				PlantWeeds();
				return;
			}

			if (data == layEggs)
			{
				LayEggs();
				return;
			}
		}

		private bool HasActionData(ActionData data)
		{
			if (actionData == null) return false;

			return actionData.Contains(data);
		}

		private void RemoveOldActions()
		{
			if(currentData == null) return;
			if(isLocalPlayer == false) return;

			foreach (var action in currentData.ActionData)
			{
				UIActionManager.Hide(this, action);
			}
		}

		private void AddNewActions()
		{
			if(actionData == null) return;
			if(isLocalPlayer == false) return;

			foreach (var action in currentData.ActionData)
			{
				UIActionManager.Show(this, action);
			}
		}

		[TargetRpc]
		private void RpcRemoveActions()
		{
			RemoveOldActions();
		}

		#endregion

		#region Alien Mode

		private void ChangeAlienMode(AlienMode newState)
		{
			currentAlienMode = newState;
			ChangeAlienSprite(currentAlienMode);
		}

		#endregion

		#region Alien Type

		//Client and server
		private void SyncAlienType(AlienTypes oldType, AlienTypes newType)
		{
			currentAlienType = newType;

			var typeFound = typesToChoose.Where(a => a.AlienType == newType).ToArray();
			if (typeFound.Length <= 0)
			{
				if (isLocalPlayer)
				{
					Chat.AddExamineMsgFromServer(gameObject, $"Unable to evolve to {newType.ToString()}");
				}

				Logger.LogError($"Could not find alien type: {newType.ToString()} in data list!");
				return;
			}

			currentData = typeFound[0];

			RemoveOldActions();

			actionData = currentData.ActionData;

			AddNewActions();
		}

		#endregion

		#region Lay Eggs

		private const int EggPlasmaCost = 20;

		private void LayEggs()
		{
			if (onWeeds == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Eggs will need a soft resin floor!");
				return;
			}

			if(TryRemovePlasma(EggPlasmaCost) == false) return;

			if (RegisterPlayer.ObjectPhysics.Component.ContainedInContainer != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You cannot lay an egg in here!");
				return;
			}

			var eggs = RegisterPlayer.Matrix.GetFirst<AlienEggCycle>(RegisterPlayer.LocalPositionServer, true);

			if (eggs != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "There is already an egg here, try somewhere else!");
				return;
			}

			Chat.AddActionMsgToChat(gameObject, $"You start to lay an egg",
				$"{gameObject.ExpensiveName()} starts laying an egg!");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction);

			StandardProgressAction.Create(
				cfg,
				() => FinishLayingEggs()
			).ServerStartProgress(ActionTarget.Object(RegisterPlayer), 6, gameObject);
		}

		private void FinishLayingEggs()
		{
			if (RegisterPlayer.ObjectPhysics.Component.ContainedInContainer != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You cannot lay an egg in here!");
				return;
			}

			var spawn = Spawn.ServerPrefab(eggPrefab, RegisterPlayer.ObjectPhysics.Component.OfficialPosition);

			if(spawn.Successful == false) return;

			Chat.AddActionMsgToChat(gameObject, $"You lay a new egg",
				$"{gameObject.ExpensiveName()} lays an egg!");
		}

		#endregion

		#region Misc

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStatics()
		{
			alienCount = 0;
			queenCount = 0;
		}

		#endregion

		#region Hiss

		[Command]
		public void CmdHiss()
		{
			Hiss();
		}

		[Server]
		private void Hiss()
		{
			EmoteActionManager.DoEmote("hiss", gameObject);
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