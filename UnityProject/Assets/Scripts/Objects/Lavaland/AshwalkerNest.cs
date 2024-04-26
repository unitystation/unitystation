using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Character;
using Systems.CraftingV2;
using Systems.GhostRoles;
using AddressableReferences;
using HealthV2;
using Items;
using Logs;
using Managers;
using Mirror;
using ScriptableObjects;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects
{
	public class AshwalkerNest : NetworkBehaviour, IServerLifecycle, IExaminable, IHoverTooltip
	{
		[FormerlySerializedAs("ghostRole")] [SerializeField]
		private GhostRoleData ashwalkerGhostRole = null;
		[SerializeField]
		private GhostRoleData priestGhostRole = null;

		[SerializeField]
		private PlayerHealthData ashwalkerRaceData = null;

		[SerializeField]
		private PlayerHealthData tieflingRaceData = null;

		[SerializeField]
		private CraftingRecipeList ashwalkerCraftingRecipesList = null;

		[SerializeField]
		private AddressableAudioSource consumeSound = null;

		[SerializeField]
		private ItemTrait edibleTraitForTheNest;

		//Meat cost of new eggs
		[SerializeField]
		private int meatCost = 2;

		//30 Seconds between eating next body
		[SerializeField]
		private int timeBetweenEating = 30;

		//Amount of meat in nest
		private int meat = 0;

		//Nest eats bodies to allow for more ashwalkers eggs
		private int ashwalkerEggs = 3;

		private int eatingTimer = 0;

		private uint createdRoleKeyAshwalker;
		private uint createdRoleKeyPriest;

		private Integrity integrity;
		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;

		private bool wasMapped;

		[SyncVar] private long timeSinceLastSearch = DateTime.Now.Ticks;

		#region Life Cycle

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void OnEnable()
		{
			UpdateManager.Add(PeriodicUpdate, 1f);
			integrity.OnWillDestroyServer.AddListener(OnDestruction);
			EventManager.AddHandler(Event.LavalandFirstEntered, OnRoundRestart);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
			integrity.OnWillDestroyServer.RemoveListener(OnDestruction);
			EventManager.RemoveHandler(Event.LavalandFirstEntered, OnRoundRestart);

			//Just in case remove the role here too
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKeyAshwalker);
		}

		#endregion

		private void PeriodicUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if (eatingTimer > 0)
			{
				eatingTimer--;
				if (eatingTimer == 5)
				{
					Chat.AddActionMsgToChat(gameObject, "The nest reaches out and searches for food.");
				}
				return;
			}

			eatingTimer = timeBetweenEating;
			if (SearchAndEatupBodies() == false)
			{
				Chat.AddActionMsgToChat(gameObject, "The nest gurgles in displeasure, there was no food to eat.");
			}

			timeSinceLastSearch = DateTime.Now.Ticks;
		}

		private bool SearchAndEatupBodies()
		{
			// LivingHealthBehavior is obselte, don't search for it pls. Only search for MasterBase.
			var creatures = MatrixManager.GetAdjacent<LivingHealthMasterBase>(gameObject.AssumedWorldPosServer().CutToInt(), true);
			var organs = MatrixManager.GetAdjacent<ItemAttributesV2>(gameObject.AssumedWorldPosServer().CutToInt(), true);
			bool ate = false;
			string smallMeatMsg = $"Serrated tendrils eagerly pull nearby food from the {gameObject.ExpensiveName()}";
			bool willBeSatisifed = DMMath.Prob(5);
			if (willBeSatisifed is false) smallMeatMsg += ", but its hunger is never satiated.";

			//(Max): HEY THERE, IF YOU ARE TRYING TO UPDATE THIS TO BALANCE OUT HOW THE NEST GENERATES 1-3 MORE MEAT PRODUCE AFTER GIBBING A PLAYER
			//DO NOT CHANGE IT. THERE IS ANOTHER BALANCE UPDATE ALREADY IN THE WORKS FOR MEAT LIFECYCLES AND IT WILL BE PUSHED IN THE FORGE PR.
			//LEAVE THIS AS IS FOR THE TIME BEING!!
			foreach (var item in organs)
			{
				if (item!= null && item.GetTraits().Contains(edibleTraitForTheNest) == false) continue;
				_ = Despawn.ServerSingle(item.gameObject);
				ate = true;
			}

			if (ate)
			{
				Chat.AddActionMsgToChat(gameObject, smallMeatMsg);
				if (willBeSatisifed) IncreaseMeat();
			}

			foreach (var creature in creatures)
			{
				if (creature.IsDead == false) continue;
				if (creature.InitialSpecies == ashwalkerRaceData || creature.InitialSpecies == tieflingRaceData)
				{
					Chat.AddActionMsgToChat(gameObject,
						$"The nest grumples violently as it first tries snatching up {creature.playerScript.visibleName}, " +
						$"but it puts them down as it notices that they're a {creature.InitialSpecies.name}");
					continue;
				}
				EatBody(creature);
				ate = true;
			}

			return ate;
		}

		private void EatBody(LivingHealthMasterBase healthMasterBase)
		{
			SoundManager.PlayNetworkedAtPos(consumeSound, registerTile.WorldPositionServer, sourceObj: gameObject);

			if (healthMasterBase.playerScript.OrNull()?.Mind?.occupation.OrNull()?.JobType == JobType.ASHWALKER
			    && healthMasterBase.playerScript.OrNull()?.Mind?.IsGhosting == false)
			{
				Chat.AddActionMsgToChat(healthMasterBase.gameObject,
					$"Your body has been returned to the nest. You are being remade anew, and will awaken shortly. Your memories will remain intact in your new body, as your soul is being salvaged",
					$"Serrated tendrils carefully pull {healthMasterBase.gameObject.ExpensiveName()} to the {gameObject.ExpensiveName()}, absorbing the body and creating it anew.");

				//If dead ashwalker in body respawn without cost
				SpawnAshwalker(healthMasterBase.playerScript.PlayerInfo, false);
				healthMasterBase.OnGib();
				return;
			}

			healthMasterBase.OnGib();
			Chat.AddActionMsgToChat(gameObject, $"Serrated tendrils eagerly pull {healthMasterBase.gameObject.ExpensiveName()} to " +
												$"the {gameObject.ExpensiveName()}, tearing the body apart as its blood seeps over the eggs.");

			IncreaseMeat();
		}

		private void IncreaseMeat(int meatIncrease = 1)
		{
			meat += meatIncrease;

			//Restore 5% integrity
			integrity.RestoreIntegrity(integrity.initialIntegrity * 0.05f);

			if(meat < meatCost) return;
			meat -= meatCost;

			//Increase eggs
			ashwalkerEggs++;
			SetSprite();
			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKeyAshwalker, 1, ashwalkerEggs, -1);

			Chat.AddActionMsgToChat(gameObject, "One of the eggs swells to an unnatural size and tumbles free. It's ready to hatch!");
		}

		private void SetSprite()
		{
			spriteHandler.SetCatalogueIndexSprite(ashwalkerEggs > 0 ? 1 : 0);
		}

		//Used for when admin or in round spawned
		public void OnSpawnServer(SpawnInfo info)
		{
			wasMapped = info.SpawnType == SpawnType.Mapped;
			if(wasMapped) return;

			OnRoundRestart();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKeyAshwalker);
		}

		//Would use only IServerSpawn, but that is called before the ghost role manager which wipes the list at RoundStart...
		private void OnRoundRestart()
		{
			SetSprite();

			createdRoleKeyAshwalker = GhostRoleManager.Instance.ServerCreateRole(ashwalkerGhostRole);
			createdRoleKeyPriest = GhostRoleManager.Instance.ServerCreateRole(priestGhostRole);
			var role1 = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKeyAshwalker];
			var role2 = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKeyPriest];

			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKeyAshwalker, 1, ashwalkerEggs, -1);
			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKeyPriest, 1, 1, -1);

			role2.OnPlayerAdded += OnSpawnPlayerPriest;
			role1.OnPlayerAdded += OnSpawnPlayer;

			EventManager.RemoveHandler(Event.LavalandFirstEntered, OnRoundRestart);
		}

		private void OnSpawnPlayer(PlayerInfo player)
		{
			SpawnAshwalker(player);
		}

		private void OnSpawnPlayerPriest(PlayerInfo player)
		{
			if (RemovePlayer(player) == false) return;
			var characterSettings = GenerateWalkerSheet(ashwalkerRaceData);
			var Ashwalker = PlayerSpawn.NewSpawnCharacterV2(
				SOAdminJobsList.Instance.GetByName("Ashwalker Priest")
				,characterSettings);
			PlayerSpawn.TransferAccountToSpawnedMind(player,Ashwalker);
			Ashwalker.Body.playerMove.AppearAtWorldPositionServer(registerTile.WorldPosition);
			// Priests don't need their crafting recpies wiped.
			var crafting = player.Mind.CurrentPlayScript.PlayerCrafting;
			foreach (var recipe in ashwalkerCraftingRecipesList.CraftingRecipes)
			{
				crafting.LearnRecipe(recipe);
			}
			//Decrease the remaining roles
			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKeyAshwalker, 1, ashwalkerEggs, -1);

			//Remove the player so they can join again once they die
			GhostRoleManager.Instance.ServerRemoveWaitingPlayer(createdRoleKeyPriest, player);

			Chat.AddExamineMsg(player.GameObject, "You have been risen from the hell fires, with a new body and renewed purpose. Glory to the Necropolis!");

			//Priests cant speak common, but can understand it.
			player.Mind.CurrentPlayScript.MobLanguages.RemoveLanguage(LanguageManager.Common);
		}

		private bool RemovePlayer(PlayerInfo player)
		{
			if (this == null || gameObject == null)
			{
				//Remove the player from all roles (as createdRoleKey will Error)
				GhostRoleManager.Instance.ServerRemoveWaitingPlayer(player);
				Loggy.LogError("Ghost role spawn called on null ashwalker, was the role not removed on destruction?");
				return false;
			}
			return true;
		}

		private CharacterSheet GenerateWalkerSheet(PlayerHealthData race)
		{
			var characterSettings = CharacterSheet.GenerateRandomCharacter();
			characterSettings.Species = race.name;
			characterSettings.SerialisedExternalCustom?.Clear();
			characterSettings.SkinTone = CharacterSheet.GetRandomSkinTone(race);
			characterSettings.Name = StringManager.GetRandomLizardName(characterSettings.GetGender());
			return characterSettings;
		}

		private void SpawnAshwalker(PlayerInfo player, bool costEgg = true)
		{
			//Since this is being called from an Action<> this could be null.
			if (RemovePlayer(player) == false) return;
			var characterSettings = GenerateWalkerSheet(ashwalkerRaceData);
			var Ashwalker = PlayerSpawn.NewSpawnCharacterV2(
				SOAdminJobsList.Instance.GetByName("Ashwalker")
				,characterSettings);
			PlayerSpawn.TransferAccountToSpawnedMind(player,Ashwalker);
			Ashwalker.Body.playerMove.AppearAtWorldPositionServer(registerTile.WorldPosition);

			//Wipe crafting recipes and add Ashwalker ones
			var crafting = player.Mind.CurrentPlayScript.PlayerCrafting;
			crafting.ForgetAllRecipes();
			foreach (var recipe in ashwalkerCraftingRecipesList.CraftingRecipes)
			{
				crafting.LearnRecipe(recipe);
			}

			if (costEgg)
			{
				ashwalkerEggs--;
				SetSprite();
				Chat.AddActionMsgToChat(gameObject, "An egg hatches in the nest!");
			}
			else
			{
				Chat.AddActionMsgToChat(gameObject, "A creature emerges from the nest. Glory to the Necropolis!");
			}

			//Decrease the remaining roles
			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKeyAshwalker, 1, ashwalkerEggs, -1);

			//Remove the player so they can join again once they die
			GhostRoleManager.Instance.ServerRemoveWaitingPlayer(createdRoleKeyAshwalker, player);

			Chat.AddExamineMsg(player.GameObject, "You have been pulled back from beyond the grave, with a new body and renewed purpose. Glory to the Necropolis!");

			//Ashwalkers cant speak or understand common
			player.Mind.CurrentPlayScript.MobLanguages.RemoveLanguage(LanguageManager.Common, true);
		}

		private void OnDestruction(DestructionInfo info)
		{
			Chat.AddActionMsgToChat(gameObject, "As the nest dies, all the eggs explode. There will be no more ashwalkers today!");
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKeyAshwalker);
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var msg = $"There {(ashwalkerEggs == 1 ? "is" : "are")} {ashwalkerEggs} egg{(ashwalkerEggs == 1 ? "" : "s")} in the nest.";
			long currentTimeTicks = DateTime.Now.Ticks;
			long elapsedTicks = currentTimeTicks - timeSinceLastSearch;
			double elapsedSeconds = (double)elapsedTicks / TimeSpan.TicksPerSecond;
			msg += "\n\n" + $"it's been {elapsedSeconds} seconds since its last meal.";
			return msg;
		}

		public string HoverTip()
		{
			return Examine();
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
			List<TextColor> textColors = new List<TextColor>
			{
				new TextColor() {Text = "Leave dead bodies or uncooked meat near the nest to feed it.", Color = Color.red}
			};
			return textColors;
		}
	}
}