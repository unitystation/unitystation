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
	public class AlienPlayer : NetworkBehaviour, IServerActionGUIMulti, ICooldown
	{
		[Header("Sprite Stuff")]
		[SerializeField]
		private SpriteHandler mainSpriteHandler;

		[SerializeField]
		private SpriteHandler mainBackSpriteHandler;

		[Header("Alien ScriptableObjects")]
		[SerializeField]
		private List<AlienTypeDataSO> typesToChoose = new List<AlienTypeDataSO>();
		public List<AlienTypeDataSO> TypesToChoose => typesToChoose;

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

		[SerializeField]
		private ActionData evolveAction = null;

		[SerializeField]
		private ActionData hissAction = null;

		[SerializeField]
		private ActionData hiveAction = null;

		[SerializeField]
		private ActionData queenAnnounceAction = null;

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

		//TODO should probably reset on round start?
		//Used to generate names
		private static int alienCount;

		//Used to generate Queen names
		private static int queenCount;

		//Current alien data SO
		private AlienTypeDataSO currentData;
		public AlienTypeDataSO CurrentData => currentData;

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
		private HasCooldowns cooldowns;

		public RegisterPlayer RegisterPlayer => playerScript.registerTile;

		[SyncVar]
		private bool isDead;
		public bool IsDead => isDead;

		private bool onWeeds;

		private int nameNumber = -1;

		public float DefaultTime => 5f;

		#region LifeCycle

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			livingHealthMasterBase = GetComponent<LivingHealthMasterBase>();
			rotatable = GetComponent<Rotatable>();
			cooldowns = GetComponent<HasCooldowns>();
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

			Evolve(newAlien, false);

			currentPlasma = 100;

			if (currentData.AlienType == AlienTypes.Queen)
			{
				queenCount++;
				nameNumber = queenCount;
				playerScript.playerName = $"{currentData.Name} {queenCount}";
			}
			else
			{
				alienCount++;
				nameNumber = alienCount;
				playerScript.playerName = $"{currentData.Name} {alienCount:D3}";
			}
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

		[SyncVar]
		private int growth;

		private void LarvaUpdate()
		{
			if(currentPlasma <= 0) return;

			if (growth < currentData.MaxGrowth)
			{
				growth++;
				return;
			}

			//At max growth (100) is early larva then auto evolve, otherwise add Action Button
			if (CurrentAlienType is AlienTypes.Larva1 or AlienTypes.Larva2)
			{
				Evolve(CurrentAlienType == AlienTypes.Larva1 ? AlienTypes.Larva2 : AlienTypes.Larva3);
			}
		}

		#endregion

		#region Queen

		[Command]
		public void CmdQueenAnnounce(string message)
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

			//Remove tags
			message = Chat.StripTags(message);

			//TODO play sound for all aliens

			Chat.AddChatMsgToChat(playerScript.PlayerInfo, message, ChatChannel.Alien, Loudness.MEGAPHONE);
		}

		#endregion

		#region Evolution

		private void Evolve(AlienTypes newAlien, bool changeName = true)
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
			growth = 0;
			currentPlasma = 0;

			ChangeAlienMode(AlienMode.Normal);

			playerScript.weaponNetworkActions.SetNewDamageValues(currentData.AttackSpeed,
				currentData.AttackDamage, currentData.DamageType, currentData.ChanceToHit);

			Chat.AddExamineMsgFromServer(gameObject, $"You evolve into a {currentData.Name}!");

			//TODO display spawn banner?

			if(changeName == false) return;

			//Set new name
			if (currentData.AlienType == AlienTypes.Queen)
			{
				playerScript.playerName = $"{currentData.Name} {nameNumber}";
				return;
			}

			playerScript.playerName = $"{currentData.Name} {nameNumber:D3}";
		}

		[Command]
		public void CmdEvolve(AlienTypes newType)
		{
			if (growth < currentData.MaxGrowth)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You need to mature more!");
				return;
			}

			var typeFound = typesToChoose.Where(a => a.AlienType == newType).ToArray();
			if (typeFound.Length <= 0)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Unable to evolve to {newType.ToString()}");
				Logger.LogError($"Could not find alien type: {newType.ToString()} in data list!");
				return;
			}

			var newData = typeFound[0];

			if (newData.EvolvedFrom.HasFlag(currentAlienType) == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You cannot evolve into that alien type!");
				return;
			}

			Evolve(newData.AlienType);
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
			//CLIENT SIDE//
			if(HasActionData(data) == false) return;

			//Open evolve window
			if (data == evolveAction)
			{
				if (growth < currentData.MaxGrowth)
				{
					Chat.AddExamineMsgToClient("You need to mature more first!");
					return;
				}

				UIManager.Instance.panelHudBottomController.AlienUI.OpenEvolveMenu();
				return;
			}

			//Hiss
			if (data == hissAction)
			{
				if(OnCoolDown(NetworkSide.Client)) return;
				StartCoolDown(NetworkSide.Client);

				CmdHiss();
				return;
			}

			//Hive
			if (data == hiveAction)
			{
				UIManager.Instance.panelHudBottomController.AlienUI.OpenHiveMenu();
				return;
			}

			//Queen Announce
			if (data == queenAnnounceAction)
			{
				UIManager.Instance.panelHudBottomController.AlienUI.OpenQueenAnnounceMenu();
				return;
			}
		}

		public void CallActionServer(ActionData data, PlayerInfo sentByPlayer)
		{
			//SERVER SIDE//
			if(HasActionData(data) == false) return;

			//Plant weeds
			if (data == plantWeeds)
			{
				PlantWeeds();
				return;
			}

			//Lay eggs
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
			if(actionData == null) return;
			if(isLocalPlayer == false) return;

			foreach (var action in actionData)
			{
				UIActionManager.Hide(this, action);
			}
		}

		private void AddNewActions()
		{
			if(currentData == null) return;
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

		public bool OnCoolDown(NetworkSide side)
		{
			return cooldowns.IsOn(CooldownID.Asset(this, side));
		}

		public void StartCoolDown(NetworkSide side)
		{
			cooldowns.TryStart(this, side);
		}

		#endregion

		#region Hiss

		[Command]
		public void CmdHiss()
		{
			if(OnCoolDown(NetworkSide.Server)) return;
			StartCoolDown(NetworkSide.Server);

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

		[Flags]
		public enum AlienTypes
		{
			None = 0,
			//Three larva stages
			Larva1 = 1 << 0,
			Larva2 = 1 << 1,
			Larva3 = 1 << 2,

			Hunter = 1 << 3,
			Sentinel = 1 << 4,
			Praetorian = 1 << 5,
			Drone = 1 << 6,

			//God Save the Queen!
			Queen = 1 << 7
		}
	}
}