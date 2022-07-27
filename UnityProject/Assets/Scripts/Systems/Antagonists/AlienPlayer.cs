using System;
using System.Collections.Generic;
using System.Linq;
using Alien;
using Core.Chat;
using HealthV2;
using Items.Others;
using Messages.Server.LocalGuiMessages;
using Mirror;
using Objects;
using Player.Movement;
using ScriptableObjects;
using Systems.GhostRoles;
using Tiles;
using UI.Action;
using UnityEngine;
using Weapons.Projectiles;

namespace Systems.Antagonists
{
	public class AlienPlayer : NetworkBehaviour, IServerActionGUIMulti, ICooldown, IServerLifecycle, IOnPlayerTransfer,
		IOnPlayerRejoin
	{
		[Header("Sprite Stuff")]
		[SerializeField]
		private SpriteHandler mainSpriteHandler;

		[SerializeField]
		private SpriteHandler mainBackSpriteHandler;

		[SerializeField]
		private GameObject alienLight;

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

		[SerializeField]
		private ActionData acidSpitAction = null;

		[SerializeField]
		private ActionData neurotoxinSpitAction = null;

		[SerializeField]
		private ActionData acidPoolAction = null;

		[SerializeField]
		private ActionData sharePlasmaAction = null;

		//TODO combine these somehow?
		[SerializeField]
		private ActionData resinWallAction = null;

		[SerializeField]
		private ActionData nestAction = null;

		#endregion

		#region Prefabs

		[Header("Prefab and Tiles")]
		[SerializeField]
		private List<LayerTile> weedTiles = new List<LayerTile>();

		[SerializeField]
		private LayerTile resinWallTile = null;

		[SerializeField]
		private GameObject eggPrefab = null;

		[SerializeField]
		private GameObject weedPrefab = null;

		[SerializeField]
		private GameObject nestPrefab = null;

		[SerializeField]
		private GameObject acidSpitProjectilePrefab = null;

		[SerializeField]
		private GameObject neurotoxicSpitProjectilePrefab = null;

		[SerializeField]
		private GameObject acidPoolPrefab = null;

		#endregion

		//TODO should probably reset on round start?
		//Used to generate names
		private static int alienCount;

		//Used to generate Queen names
		private static int queenCount;

		//Only one alive queen allowed
		private static bool queenInHive;

		//Current alien data SO
		private AlienTypeDataSO currentData;
		public AlienTypeDataSO CurrentData => currentData;

		[SyncVar]
		private AlienMode currentAlienMode;
		public AlienMode CurrentAlienMode => currentAlienMode;

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

		private LayerMask playerMask;

		public RegisterPlayer RegisterPlayer => playerScript.registerTile;

		[SyncVar]
		private bool isDead;
		public bool IsDead => isDead;

		private bool onWeeds;

		private int nameNumber = -1;

		public float DefaultTime => 5f;

		private CooldownInstance hissCooldown;
		public CooldownInstance HissCooldown => hissCooldown;

		private CooldownInstance projectileCooldown;
		public CooldownInstance ProjectileCooldown => projectileCooldown;

		private CooldownInstance queenAnnounceCooldown;
		public CooldownInstance QueenAnnounceCooldown => queenAnnounceCooldown;

		private CooldownInstance sharePlasmaCooldown;

		private AlienMouseInputController mouseInputController;

		private List<ActionData> toggles = new List<ActionData>();

		public bool IsLarva => currentData.AlienType is AlienTypes.Larva1 or AlienTypes.Larva2 or AlienTypes.Larva3;

		//0 if theres a player in body
		private uint createdRoleKey;

		private float disconnectTime;

		private CharacterSheet characterSheet;

		#region LifeCycle

		private void Awake()
		{
			playerScript = GetComponent<PlayerScript>();
			livingHealthMasterBase = GetComponent<LivingHealthMasterBase>();
			rotatable = GetComponent<Rotatable>();
			cooldowns = GetComponent<HasCooldowns>();
			mouseInputController = GetComponent<AlienMouseInputController>();

			playerMask = LayerMask.GetMask("Players");

			hissCooldown = new CooldownInstance
			{
				defaultTime = 5f
			};

			projectileCooldown = new CooldownInstance
			{
				defaultTime = 4f
			};

			queenAnnounceCooldown = new CooldownInstance
			{
				defaultTime = 3f
			};

			sharePlasmaCooldown = new CooldownInstance
			{
				defaultTime = 3f
			};

			characterSheet = new CharacterSheet()
			{
				PlayerPronoun = PlayerPronoun.They_them
			};
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

		public void OnSpawnServer(SpawnInfo info)
		{
			SetNewPlayer(startingAlienType);

			//This triggers the spawning of the alien body parts
			playerScript.playerSprites.OnCharacterSettingsChange(characterSheet);
		}

		public override void OnStartLocalPlayer()
		{
			if(isLocalPlayer == false) return;

			UIManager.Instance.panelHudBottomController.AlienUI.SetUp(this);

			alienLight.SetActive(true);

			ResetActions();
		}

		public override void OnStopLocalPlayer()
		{
			alienLight.SetActive(false);

			RemoveOldActions();
		}

		#endregion

		#region Setup

		[Server]
		public void SetNewPlayer(AlienTypes newAlien)
		{
			if(isServer == false) return;

			Evolve(newAlien, false);

			if (currentData.AlienType == AlienTypes.Queen)
			{
				queenCount++;
				nameNumber = queenCount;
				queenInHive = true;
			}
			else
			{
				alienCount++;
				nameNumber = alienCount;
			}

			SetName(true, currentData);

			if (CurrentAlienType is AlienTypes.Larva1 or AlienTypes.Larva2 or AlienTypes.Larva3)
			{
				Chat.AddExamineMsgFromServer(gameObject, "You are an alien larva. Hide from danger until you can evolve. Use :a to communicate with the hivemind.");
				return;
			}

			Chat.AddExamineMsgFromServer(gameObject, $"You are a {currentAlienType.ToString()}. Use :a to communicate with the hivemind.");
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

			GrowthUpdate();

			DisconnectCheck();
		}

		#endregion

		#region Growth

		[SyncVar]
		private int growth;

		private void GrowthUpdate()
		{
			if(currentPlasma <= 0) return;

			if (growth < currentData.MaxGrowth)
			{
				growth++;
				return;
			}

			Chat.AddExamineMsgFromServer(gameObject, "You are fully grown!");

			//At max growth (100) is early larva then auto evolve, otherwise add Action Button
			if (CurrentAlienType is AlienTypes.Larva1 or AlienTypes.Larva2)
			{
				Evolve(CurrentAlienType == AlienTypes.Larva1 ? AlienTypes.Larva2 : AlienTypes.Larva3);
			}

			if (CurrentAlienType != AlienTypes.Larva3 && connectionToClient != null) return;

			RpcOpenEvolveMenu();
		}

		[TargetRpc]
		private void RpcOpenEvolveMenu()
		{
			UIManager.Instance.panelHudBottomController.AlienUI.OpenEvolveMenu();
		}

		#endregion

		#region Queen

		[Command]
		public void CmdQueenAnnounce(string message)
		{
			if(OnCoolDown(NetworkSide.Server, queenAnnounceCooldown)) return;
			StartCoolDown(NetworkSide.Server, queenAnnounceCooldown);

			//Remove tags
			message = Chat.StripTags(message);

			//TODO play sound for all aliens

			Chat.AddChatMsgToChatServer(playerScript.PlayerInfo, message, ChatChannel.Alien, Loudness.MEGAPHONE);
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

			Chat.AddActionMsgToChat(gameObject, "You begin to evolve!",
				$"{playerScript.playerName} begins to twist and contort!");

			var old = currentData;
			currentData = typeFound[0];
			actionData = currentData.ActionData;
			currentAlienType = currentData.AlienType;
			growth = 0;
			currentPlasma = currentData.InitialPlasma;

			SetUpNewHealthValues();

			ChangeAlienMode(AlienMode.Normal);

			playerScript.weaponNetworkActions.SetNewDamageValues(currentData.AttackSpeed,
				currentData.AttackDamage, currentData.DamageType, currentData.ChanceToHit);

			Chat.AddExamineMsgFromServer(gameObject, $"You evolve into a {currentData.Name}!");

			if(changeName == false) return;

			SetName(false, old);
		}

		private void SetName(bool newlyJoined, AlienTypeDataSO old)
		{
			if (newlyJoined == false)
			{
				SpawnBannerMessage.Send(gameObject, currentData.Name, null, Color.green, Color.black, false);
			}

			//Set new name
			if (currentData.AlienType == AlienTypes.Queen)
			{
				playerScript.playerName = $"{currentData.Name} {nameNumber}";
				Chat.AddChatMsgToChatServer($"A new queen: {playerScript.playerName} has joined the hive, rejoice!",
					ChatChannel.Alien, Loudness.SCREAMING);
			}
			else
			{
				if (newlyJoined)
				{
					playerScript.playerName = $"{currentData.Name} {nameNumber:D3}";
					Chat.AddChatMsgToChatServer($"{playerScript.playerName} has joined the hive, rejoice!",
						ChatChannel.Alien, Loudness.LOUD);
				}
				else
				{
					playerScript.playerName = $"{currentData.Name} {nameNumber:D3}";
					Chat.AddChatMsgToChatServer($"{old.Name} {nameNumber:D3} has evolved into a {currentData.Name}!",
						ChatChannel.Alien, Loudness.LOUD);
				}
			}
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

			if (newData.AlienType == AlienTypes.Queen && queenInHive)
			{
				Chat.AddExamineMsgFromServer(gameObject, "There is already a queen in the hive!");
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

			TryAddPlasma(currentPlasma + currentData.PlasmaGainRate);
		}

		[Server]
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

		[Client]
		private bool TryRemovePlasmaClient(int toRemove, bool doMessage = true)
		{
			if (currentPlasma < toRemove)
			{
				if (doMessage)
				{
					Chat.AddExamineMsgToClient($"Not enough plasma! You are missing {toRemove - currentPlasma}");
				}

				return false;
			}

			return true;
		}

		[Server]
		private void TryAddPlasma(int toAdd)
		{
			if (currentPlasma + toAdd > currentData.MaxPlasma)
			{
				currentPlasma = currentData.MaxPlasma;
				return;
			}

			currentPlasma += toAdd;
		}

		private void SharePlasma()
		{
			if (OnCoolDown(NetworkSide.Server, sharePlasmaCooldown))
			{
				Chat.AddExamineMsgFromServer(gameObject, "You are still recovering from the last plasma share!");
				return;
			}

			StartCoolDown(NetworkSide.Server, sharePlasmaCooldown);

			var alienInRange = Physics2D.OverlapCircleAll(
				RegisterPlayer.ObjectPhysics.Component.OfficialPosition, 3f, playerMask)
				.Where(x => x.gameObject.GetComponent<AlienPlayer>() != null).ToArray();

			//Greater than one as we need more than ourself
			if (alienInRange.Length <= 1)
			{
				Chat.AddExamineMsgFromServer(gameObject, "No sisters in range!");
				return;
			}

			if (currentPlasma == 0)
			{
				Chat.AddExamineMsgFromServer(gameObject, "No plasma to share!");
				return;
			}

			//Radius of 3 only... do we need to do a wall check?

			//Divide plasma up
			var plasmaToShare = (int)Mathf.Ceil(currentPlasma / (float) alienInRange.Length);

			if (plasmaToShare == 0)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Not enough plasma to share between {alienInRange.Length - 1} sisters!");
				return;
			}

			var message = $"{playerScript.playerName} shared {plasmaToShare} plasma with you!";

			foreach (var alienCollider in alienInRange)
			{
				if(alienCollider.gameObject.TryGetComponent<AlienPlayer>(out var alien) == false) continue;

				//Dont add to ourself
				if(alien == this) continue;

				//Add the share to the alien
				alien.TryAddPlasma(plasmaToShare);

				Chat.AddExamineMsgFromServer(alien.gameObject, message);
			}

			//Set ours to the remaining share
			currentPlasma = plasmaToShare;

			Chat.AddExamineMsgFromServer(gameObject, $"You shared {plasmaToShare} to {alienInRange.Length - 1} sisters!");
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

			if (currentPlasma == 0) return;

			//Sleeping heals twice as fast, but costs twice as much
			var healCost = currentData.HealPlasmaCost * (currentAlienMode == AlienMode.Sleep ? 2 : 1);

			var healAmount = currentData.HealAmount * (currentAlienMode == AlienMode.Sleep ? 2 : 1);

			if (currentPlasma < healCost)
			{
				//If less than needed just do the percentage available
				healAmount *= (currentPlasma / (float)healCost);
				currentPlasma = 0;
			}
			else
			{
				currentPlasma -= healCost;
			}

			livingHealthMasterBase.HealDamageOnAll(null, healAmount, DamageType.Brute);
		}

		private void SetUpNewHealthValues()
		{
			var bodyParts = livingHealthMasterBase.SurfaceBodyParts;

			foreach (var bodyPart in bodyParts)
			{
				bodyPart.SelfArmor = currentData.BodyPartArmor;

				//TODO when limb is separated out into Arm and Leg redo this
				if(bodyPart.TryGetComponent<Limb>(out var limb) == false) continue;

				limb.SetNewSpeeds(currentData.RunningLegSpeed, currentData.WalkingLegSpeed, limb.CrawlingSpeed);
			}
		}

		#endregion

		#region Death

		private void OnDeath()
		{
			if(isDead) return;
			isDead = true;

			RemoveGhostRole();

			RpcRemoveActions();

			ChangeAlienMode(AlienMode.Dead);

			if (currentAlienType == AlienTypes.Queen)
			{
				OnQueenDeath();
			}
			else
			{
				Chat.AddChatMsgToChatServer($"{gameObject.ExpensiveName()} has died!", ChatChannel.Alien, Loudness.MEGAPHONE);
			}

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, OnUpdate);

			//Force player into ghost
			PlayerSpawn.ServerSpawnGhost(playerScript.mind);

			//Set to null so can't reenter
			playerScript.mind.body = null;
		}

		private void OnQueenDeath()
		{
			var queens = FindObjectsOfType<AlienPlayer>().Where(x => x.isDead == false &&
			                                                         x.currentAlienType == AlienTypes.Queen).ToArray();

			queenInHive = queens.Any();

			var queenString = queenInHive ? "\nBut we have a new leader!" : "\nWe need a new queen or the hive will surely perish!";

			Chat.AddChatMsgToChatServer($"{gameObject.ExpensiveName()} has died!{queenString}", ChatChannel.Alien, Loudness.MEGAPHONE);

			//TODO play scream for all xenos?
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

		private const int WeedPlasmaCost = 50;

		private void PlantWeeds()
		{
			if(TryRemovePlasma(WeedPlasmaCost) == false) return;

			if(ValidateBuild("plant weeds") == false) return;

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
			if(ValidateBuild("plant weeds") == false) return;

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
				default:
					Logger.LogError($"Unexpected case: {newSprite.ToString()}");
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
				default:
					Logger.LogError($"Unexpected case: {newRotation.ToString()}");
					return;
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

			//Acid Spit
			if (data == acidSpitAction)
			{
				if (mouseInputController.CurrentClick == AlienMouseInputController.AlienClicks.AcidSpit)
				{
					//Toggle Off
					mouseInputController.SetClickType(AlienMouseInputController.AlienClicks.None);
					UpdateButtonSprite(data, 0);
					return;
				}

				//Toggled On
				mouseInputController.SetClickType(AlienMouseInputController.AlienClicks.AcidSpit);
				UpdateButtonSprite(data, 1);
				UnToggleOthers(data);
				return;
			}

			//Neurotoxin Spit
			if (data == neurotoxinSpitAction)
			{
				if (mouseInputController.CurrentClick == AlienMouseInputController.AlienClicks.NeurotoxinSpit)
				{
					//Toggle Off
					mouseInputController.SetClickType(AlienMouseInputController.AlienClicks.None);
					UpdateButtonSprite(data, 0);
					return;
				}

				//Toggled On
				mouseInputController.SetClickType(AlienMouseInputController.AlienClicks.NeurotoxinSpit);
				UpdateButtonSprite(data, 1);
				UnToggleOthers(data);
				return;
			}

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
				if(OnCoolDown(NetworkSide.Client, hissCooldown)) return;
				StartCoolDown(NetworkSide.Client, hissCooldown);

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
			}
		}

		public void CallActionServer(ActionData data, PlayerInfo playerInfo)
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

			//Resin wall
			if (data == resinWallAction)
			{
				BuildWall();
				return;
			}

			//Nest
			if (data == nestAction)
			{
				BuildNest();
				return;
			}

			//Share plasma
			if (data == sharePlasmaAction)
			{
				SharePlasma();
				return;
			}

			//Acid pool
			if (data == acidPoolAction)
			{
				MakeAcidPool();
			}
		}

		private bool HasActionData(ActionData data)
		{
			if (actionData == null) return false;

			return actionData.Contains(data);
		}

		private void ResetActions()
		{
			RemoveOldActions();
			AddNewActions();
		}

		private void RemoveOldActions()
		{
			if(actionData == null) return;

			toggles = new List<ActionData>();

			if(isLocalPlayer == false) return;

			foreach (var action in actionData)
			{
				UIActionManager.Hide(this, action);
			}
		}

		private void AddNewActions()
		{
			if(currentData == null) return;

			foreach (var action in currentData.ActionData)
			{
				if(action.IsToggle == false) continue;

				toggles.Add(action);
			}

			if(isLocalPlayer == false) return;

			foreach (var action in currentData.ActionData)
			{
				UIActionManager.Show(this, action);
			}
		}

		[Client]
		//Note this assumes all toggles are mutually exclusive on this alien
		private void UnToggleOthers(ActionData keep)
		{
			foreach (var toggle in toggles)
			{
				if(toggle == keep) continue;

				UIActionManager.ToggleLocal(this, toggle, false);
			}
		}

		[TargetRpc]
		private void RpcRemoveActions()
		{
			RemoveOldActions();
		}

		private void UpdateButtonSprite(ActionData actionDataForSprite, int location)
		{
			UIActionManager.SetSprite(this, actionDataForSprite, location);
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

		private const int EggPlasmaCost = 75;

		private void LayEggs()
		{
			if(TryRemovePlasma(EggPlasmaCost) == false) return;

			if(ValidateBuild("lay an egg") == false) return;

			if (onWeeds == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, "Eggs will need a soft resin floor!");
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
			if(ValidateBuild("lay an egg") == false) return;

			var spawn = Spawn.ServerPrefab(eggPrefab, RegisterPlayer.ObjectPhysics.Component.OfficialPosition);

			if(spawn.Successful == false) return;

			Chat.AddActionMsgToChat(gameObject, $"You lay a new egg",
				$"{gameObject.ExpensiveName()} lays an egg!");
		}

		#endregion

		#region Wall, Nest, Acid Pool

		private const int ResinWallCost = 25;

		private const int NestCost = 25;

		private const int AcidPoolCost = 25;

		private void BuildWall()
		{
			if(ValidateBuild("secrete resin") == false) return;

			if(WallValidate(out _, out _) == false) return;

			if(TryRemovePlasma(ResinWallCost) == false) return;

			Chat.AddActionMsgToChat(gameObject, $"You start to secrete out resin",
				$"{gameObject.ExpensiveName()} starts to secrete out resin!");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction);

			StandardProgressAction.Create(
				cfg,
				() => FinishBuildingWall()
			).ServerStartProgress(ActionTarget.Object(RegisterPlayer), 6, gameObject);
		}

		private bool WallValidate(out Vector3Int directionFacing, out MatrixInfo matrixThere)
		{
			//Build wall in the direction we are facing
			directionFacing = rotatable.CurrentDirection.ToLocalVector3Int();

			//Convert both coords to locals of the
			var worldOrigin = RegisterPlayer.ObjectPhysics.Component.OfficialPosition.RoundToInt();
			var worldTarget = directionFacing + worldOrigin;

			matrixThere = MatrixManager.AtPoint(worldTarget, true, RegisterPlayer.Matrix.MatrixInfo);

			var passable = MatrixManager.IsPassableAtAllMatrices(worldOrigin, worldTarget, true, includingPlayers: false,
				matrixOrigin: RegisterPlayer.Matrix.MatrixInfo, matrixTarget: matrixThere);

			if (passable == false)
			{
				Chat.AddExamineMsgFromServer(gameObject, "The area is too crowded to secrete resin there!");
				return false;
			}

			return true;
		}

		private void FinishBuildingWall()
		{
			if(ValidateBuild("secrete resin") == false) return;

			if(WallValidate(out Vector3Int directionFacing, out MatrixInfo matrixThere) == false) return;

			Chat.AddActionMsgToChat(gameObject, $"You secrete out a resin wall",
				$"{gameObject.ExpensiveName()} secretes out a resin wall!");

			//Add resin wall
			matrixThere.MetaTileMap.SetTile(directionFacing + RegisterPlayer.LocalPositionServer, resinWallTile);
		}

		private void BuildNest()
		{
			if(ValidateBuild("secrete resin") == false) return;

			if(TryRemovePlasma(NestCost) == false) return;

			var nests = RegisterPlayer.Matrix.GetFirst<BuckleInteract>(RegisterPlayer.LocalPositionServer, true);

			if (nests != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "There is already a nest here, try somewhere else!");
				return;
			}

			Chat.AddActionMsgToChat(gameObject, $"You start to secrete out resin",
				$"{gameObject.ExpensiveName()} starts to secrete out resin!");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction);

			StandardProgressAction.Create(
				cfg,
				() => FinishBuildingNest()
			).ServerStartProgress(ActionTarget.Object(RegisterPlayer), 6, gameObject);
		}

		private void FinishBuildingNest()
		{
			if(ValidateBuild("secrete resin") == false) return;

			var spawn = Spawn.ServerPrefab(nestPrefab, RegisterPlayer.ObjectPhysics.Component.OfficialPosition);

			if(spawn.Successful == false) return;

			Chat.AddActionMsgToChat(gameObject, $"You secrete out a resin nest",
				$"{gameObject.ExpensiveName()} secretes out a resin nest!");
		}

		private void MakeAcidPool()
		{
			if(ValidateBuild("acid pool") == false) return;

			if(TryRemovePlasma(AcidPoolCost) == false) return;

			var directionFacing = rotatable.CurrentDirection.ToLocalVector3Int();

			//Convert both coords to locals of the
			var worldOrigin = RegisterPlayer.ObjectPhysics.Component.OfficialPosition.RoundToInt();
			var worldTarget = directionFacing + worldOrigin;

			var matrixThere = MatrixManager.AtPoint(worldTarget, true, RegisterPlayer.Matrix.MatrixInfo);

			var acidPools = matrixThere.Matrix.GetFirst<AcidPool>(RegisterPlayer.LocalPositionServer + directionFacing, true);

			if (acidPools != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "There is already an acid pool here, try somewhere else!");
				return;
			}

			Chat.AddActionMsgToChat(gameObject, $"You start to throw up acid",
				$"{gameObject.ExpensiveName()} starts throw up acid!");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Construction);

			StandardProgressAction.Create(
				cfg,
				() => FinishAcidPool()
			).ServerStartProgress(ActionTarget.Object(RegisterPlayer), 5, gameObject);
		}

		private void FinishAcidPool()
		{
			if(ValidateBuild("secrete resin") == false) return;

			var directionFacing = rotatable.CurrentDirection.ToLocalVector3Int();

			//Convert both coords to locals of the
			var worldOrigin = RegisterPlayer.ObjectPhysics.Component.OfficialPosition.RoundToInt();
			var worldTarget = directionFacing + worldOrigin;

			var matrixThere = MatrixManager.AtPoint(worldTarget, true, RegisterPlayer.Matrix.MatrixInfo);

			var acidPools = matrixThere.Matrix.GetFirst<AcidPool>(RegisterPlayer.LocalPositionServer + directionFacing, true);

			if (acidPools != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, "There is already an acid pool here, try somewhere else!");
				return;
			}

			var spawn = Spawn.ServerPrefab(acidPoolPrefab, worldTarget);

			if(spawn.Successful == false) return;

			Chat.AddActionMsgToChat(gameObject, $"You throw up a pool of acid",
				$"{gameObject.ExpensiveName()} throws up a pool of acid!");
		}

		#endregion

		#region Misc

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ClearStatics()
		{
			alienCount = 0;
			queenCount = 0;
			queenInHive = false;
		}

		public bool OnCoolDown(NetworkSide side, CooldownInstance cooldown)
		{
			return cooldowns.IsOn(CooldownID.Asset(cooldown, side));
		}

		public void StartCoolDown(NetworkSide side, CooldownInstance cooldown)
		{
			cooldowns.TryStart(cooldown, side);
		}

		private bool ValidateBuild(string action)
		{
			if (currentAlienMode == AlienMode.Sleep)
			{
				Chat.AddExamineMsg(gameObject, $"You cannot {action}, you are asleep!");
				return false;
			}

			if (currentAlienMode == AlienMode.Dead)
			{
				Chat.AddExamineMsg(gameObject, $"You cannot {action}, you are dead!");
				return false;
			}

			if (currentAlienMode == AlienMode.Unconscious)
			{
				Chat.AddExamineMsg(gameObject, $"You cannot {action}, you are unconscious");
				return false;
			}

			if (playerScript.PlayerSync.ContainedInContainer != null)
			{
				Chat.AddExamineMsgFromServer(gameObject, $"Cannot {action} in here!");
				return false;
			}

			return true;
		}

		#endregion

		#region Hiss

		[Command]
		public void CmdHiss()
		{
			if(OnCoolDown(NetworkSide.Server, hissCooldown)) return;
			StartCoolDown(NetworkSide.Server, hissCooldown);

			Hiss();
		}

		[Server]
		private void Hiss()
		{
			EmoteActionManager.DoEmote("hiss", gameObject);
		}

		#endregion

		#region Projectiles

		[Client]
		public void ClientTryAcidSpit(AimApply aimApply)
		{
			if(TryRemovePlasmaClient(currentData.AcidSpitCost) == false) return;

			if (OnCoolDown(NetworkSide.Client, ProjectileCooldown))
			{
				Chat.AddExamineMsgToClient("Your spit glands need recharging!");
				return;
			}
			StartCoolDown(NetworkSide.Client, ProjectileCooldown);

			CmdShootAcidSpit(aimApply.TargetVector, aimApply.TargetBodyPart);
		}

		[Command]
		public void CmdShootAcidSpit(Vector2 targetVector, BodyPartType targetZone)
		{
			if(TryRemovePlasma(currentData.AcidSpitCost) == false) return;

			if(ValidateProjectile() == false) return;

			if(OnCoolDown(NetworkSide.Server, projectileCooldown)) return;
			StartCoolDown(NetworkSide.Server, projectileCooldown);

			//TODO sound effect

			ProjectileManager.InstantiateAndShoot(acidSpitProjectilePrefab, targetVector, gameObject,
				null, targetZone);
		}

		[Client]
		public void ClientTryNeurotoxinSpit(AimApply aimApply)
		{
			if(TryRemovePlasmaClient(currentData.NeurotoxinSpitCost) == false) return;

			if (OnCoolDown(NetworkSide.Client, ProjectileCooldown))
			{
				Chat.AddExamineMsgToClient("Your neurotoxin glands need recharging!");
				return;
			}
			StartCoolDown(NetworkSide.Client, ProjectileCooldown);

			CmdShootNeurotoxinSpit(aimApply.TargetVector, aimApply.TargetBodyPart);
		}

		[Command]
		private void CmdShootNeurotoxinSpit(Vector2 targetVector, BodyPartType targetZone)
		{
			if(TryRemovePlasma(currentData.NeurotoxinSpitCost) == false) return;

			if(ValidateProjectile() == false) return;

			if(OnCoolDown(NetworkSide.Server, projectileCooldown)) return;
			StartCoolDown(NetworkSide.Server, projectileCooldown);

			//TODO sound effect

			ProjectileManager.InstantiateAndShoot(neurotoxicSpitProjectilePrefab, targetVector, gameObject,
				null, targetZone);
		}

		public bool ValidateProjectile()
		{
			if (currentAlienMode == AlienMode.Sleep)
			{
				Chat.AddExamineMsg(gameObject, "You cannot spit, you are asleep!");
				return false;
			}

			if (currentAlienMode == AlienMode.Dead)
			{
				Chat.AddExamineMsg(gameObject, "You cannot spit, you are dead!");
				return false;
			}

			if (currentAlienMode == AlienMode.Unconscious)
			{
				Chat.AddExamineMsg(gameObject, "You cannot spit, you are unconscious");
				return false;
			}

			return true;
		}

		#endregion

		#region GhostRole / Disconnect

		//Make ghost role after after 120 seconds after disconnect
		private const float DisconnectMaxTime = 120f;

		private PlayerInfo playerTookOver;

		private void DisconnectCheck()
		{
			if (connectionToClient != null)
			{
				disconnectTime = 0;
				return;
			}

			//Not 0 means ghost role already set up
			if(createdRoleKey != 0) return;

			disconnectTime += 1;
			if (disconnectTime < DisconnectMaxTime) return;
			disconnectTime = 0;

			SetUpGhostRole();
		}

		private void SetUpGhostRole()
		{
			if (createdRoleKey != 0)
			{
				GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
			}

			//Remove current player
			if (playerScript.mind != null)
			{
				if (playerScript.mind.GetCurrentMob().OrNull()?.GetComponent<PlayerScript>().IsGhost == false)
				{
					//Force player current into ghost
					PlayerSpawn.ServerSpawnGhost(playerScript.mind);
				}
			}

			createdRoleKey = GhostRoleManager.Instance.ServerCreateRole(currentData.GhostRoleData);
			var role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];
			role.OnPlayerAdded += OnSpawnFromGhostRole;
		}

		private void RemoveGhostRole()
		{
			if(createdRoleKey == 0) return;

			var role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];
			role.OnPlayerAdded -= OnSpawnFromGhostRole;

			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);

			createdRoleKey = 0;
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			RemoveGhostRole();
		}

		private void OnSpawnFromGhostRole(PlayerInfo player)
		{
			//Sanity check
			if(createdRoleKey == 0) return;

			playerTookOver = player;

			//Transfer player chosen into body
			PlayerSpawn.ServerTransferPlayerToNewBody(player.Connection, player.Script.mind, gameObject,
				Event.PlayerSpawned, null, true);

			//Remove the player so they can join again once they die
			GhostRoleManager.Instance.ServerRemoveWaitingPlayer(createdRoleKey, player);

			//GhostRoleManager will remove role don't need to call RemoveGhostRole
			createdRoleKey = 0;

			//PlayerTookOver only needs to be set for ServerTransferPlayerToNewBody as OnPlayerTransfer is triggered
			//During it
			playerTookOver = null;
		}

		public void OnPlayerTransfer()
		{
			//Block role remove if this transfered player was the one how got the ghost role
			//OnPlayerTransfer is still needed due to admin A ghosting which should remove the role on transfer
			if(playerTookOver != null && playerTookOver == playerScript.PlayerInfo) return;

			RemoveGhostRole();
		}

		public void OnPlayerRejoin()
		{
			RemoveGhostRole();
		}

		//Called after larva spawned, in case it was a disconnected player
		public void DoConnectCheck()
		{
			if(connectionToClient != null) return;

			if(createdRoleKey != 0) return;

			SetUpGhostRole();
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