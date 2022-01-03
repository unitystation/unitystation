using System;
using System.Collections.Generic;
using System.Linq;
using Systems.CraftingV2;
using Systems.GhostRoles;
using AddressableReferences;
using HealthV2;
using Messages.Server.GhostRoles;
using ScriptableObjects;
using UnityEngine;

namespace Objects
{
	public class AshwalkerNest : MonoBehaviour, IServerSpawn, IExaminable
	{
		[SerializeField]
		private GhostRoleData ghostRole = null;

		[SerializeField]
		private PlayerHealthData ashwalkerRaceData = null;

		[SerializeField]
		private CraftingRecipeList ashwalkerCraftingRecipesList = null;

		[SerializeField]
		private AddressableAudioSource consumeSound = null;

		//Amount of meat in nest
		private int meat = 0;

		//Meat cost of new eggs
		private int meatCost = 2;

		//Nest eats bodies to allow for more ashwalkers eggs
		private int ashwalkerEggs = 3;

		//30 Seconds between eating next body
		private int timeBetweenEating = 30;

		private int eatingTimer = 0;

		private uint createdRoleKey;

		private Integrity integrity;
		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;

		private static Vector3Int[] directions = new []
		{
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(1, -1, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, -1, 0),
			new Vector3Int(-1, 0, 0),
			new Vector3Int(-1, 1, 0)
		};

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
			EventManager.AddHandler(Event.RoundStarted, OnRoundRestart);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
			integrity.OnWillDestroyServer.RemoveListener(OnDestruction);
			EventManager.RemoveHandler(Event.RoundStarted, OnRoundRestart);
		}

		#endregion

		private void PeriodicUpdate()
		{
			if (eatingTimer > 0)
			{
				eatingTimer--;

				if (eatingTimer == 5)
				{
					Chat.AddLocalMsgToChat("The nest reaches out and searches for food", gameObject);
				}

				return;
			}

			eatingTimer = timeBetweenEating;

			foreach (var direction in directions)
			{
				//TODO remove once mobs use new health
				var oldHealth = registerTile.Matrix.Get<LivingHealthBehaviour>
					(registerTile.LocalPositionServer + direction, ObjectType.Object, true).ToList();

				if (oldHealth.Count > 0)
				{
					var mob = oldHealth[0];
					if(mob.IsDead == false) continue;

					EatMobBody(mob);
					return;
				}

				//TODO change ObjectType.Player after old health removed
				var newHealth = registerTile.Matrix.Get<LivingHealthMasterBase>
					(registerTile.LocalPositionServer + direction, ObjectType.Player, true).ToList();

				if (newHealth.Count > 0)
				{
					var health = newHealth[0];
					if(health.IsDead == false) continue;

					EatBody(health);
					return;
				}
			}

			Chat.AddLocalMsgToChat("The nest gurgles in displeasure, there was no food to eat", gameObject);
		}

		private void EatMobBody(LivingHealthBehaviour mobHealth)
		{
			mobHealth.Harvest();
			IncreaseMeat();

			Chat.AddLocalMsgToChat($"Serrated tendrils eagerly pull {mobHealth.gameObject.ExpensiveName()} to the {gameObject.ExpensiveName()}, tearing the body apart as its blood seeps over the eggs.", gameObject);
		}

		private void EatBody(LivingHealthMasterBase playerHealth)
		{
			SoundManager.PlayNetworkedAtPos(consumeSound, registerTile.WorldPositionServer, sourceObj: gameObject);

			if (playerHealth.playerScript.OrNull()?.mind?.occupation.OrNull()?.JobType == JobType.ASHWALKER
			    && playerHealth.playerScript.OrNull()?.mind?.IsGhosting == false)
			{
				Chat.AddActionMsgToChat(playerHealth.gameObject,
					$"Your body has been returned to the nest. You are being remade anew, and will awaken shortly. Your memories will remain intact in your new body, as your soul is being salvaged",
					$"Serrated tendrils carefully pull {playerHealth.gameObject.ExpensiveName()} to the {gameObject.ExpensiveName()}, absorbing the body and creating it anew.");

				//If dead ashwalker in body respawn without cost
				OnSpawnPlayer(playerHealth.playerScript.connectedPlayer);
				playerHealth.Gib();
				return;
			}

			playerHealth.Gib();

			Chat.AddLocalMsgToChat($"Serrated tendrils eagerly pull {playerHealth.gameObject.ExpensiveName()} to the {gameObject.ExpensiveName()}, tearing the body apart as its blood seeps over the eggs.", gameObject);

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
			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKey, 1, ashwalkerEggs, -1);

			Chat.AddLocalMsgToChat("One of the eggs swells to an unnatural size and tumbles free. It's ready to hatch!", gameObject);
		}

		private void DecreaseEgg()
		{
			ashwalkerEggs--;
			SetSprite();

			Chat.AddLocalMsgToChat("An egg hatches in the nest!", gameObject);

			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKey, 1, ashwalkerEggs, -1);
		}

		private void SetSprite()
		{
			spriteHandler.ChangeSprite(ashwalkerEggs > 0 ? 1 : 0);
		}

		//Used for when admin or in round spawned
		public void OnSpawnServer(SpawnInfo info)
		{
			if(GameManager.Instance.CurrentRoundState != RoundState.Started) return;

			OnRoundRestart();
		}

		//Would use only IServerSpawn, but that is called before the ghost role manager which wipes the list at RoundStart...
		private void OnRoundRestart()
		{
			SetSprite();

			createdRoleKey = GhostRoleManager.Instance.ServerCreateRole(ghostRole);
			var role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];

			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKey, 1, ashwalkerEggs, -1);

			role.OnPlayerAdded += OnSpawnPlayer;
		}

		private void OnSpawnPlayer(ConnectedPlayer player)
		{
			var newCharacterSettings = player.Script.characterSettings;

			if (newCharacterSettings == null)
			{
				newCharacterSettings = new CharacterSettings();
				newCharacterSettings.Name = "Slither";
			}

			//TODO this replaces their old race, charactersettings needs a refactor to have them per body
			newCharacterSettings.Species = ashwalkerRaceData.name;

			//TODO change player name

			player.Script.playerNetworkActions.ServerRespawnPlayerSpecial("Ashwalker", registerTile.WorldPositionServer);

			//Wipe crafting recipes and add ashwalker ones
			var crafting = player.Script.PlayerCrafting;
			crafting.ForgetAllRecipes();
			foreach (var recipe in ashwalkerCraftingRecipesList.CraftingRecipes)
			{
				crafting.LearnRecipe(recipe);
			}

			DecreaseEgg();

			GhostRoleManager.Instance.ServerRemoveWaitingPlayer(createdRoleKey, player);

			Chat.AddExamineMsg(player.GameObject, "You have been pulled back from beyond the grave, with a new body and renewed purpose. Glory to the Necropolis!");
		}

		private void OnDestruction(DestructionInfo info)
		{
			Chat.AddLocalMsgToChat("As the nest dies, all the eggs explode. There will be no more ashwalkers today", gameObject);
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"There {(ashwalkerEggs == 1 ? "is" : "are")} {ashwalkerEggs} egg{(ashwalkerEggs == 1 ? "" : "s")} in the nest.";
		}
	}
}