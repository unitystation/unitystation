using System.Linq;
using Systems.CraftingV2;
using Systems.GhostRoles;
using AddressableReferences;
using HealthV2;
using Managers;
using Player.Language;
using ScriptableObjects;
using UnityEngine;

namespace Objects
{
	public class AshwalkerNest : MonoBehaviour, IServerLifecycle, IExaminable
	{
		[SerializeField]
		private GhostRoleData ghostRole = null;

		[SerializeField]
		private PlayerHealthData ashwalkerRaceData = null;

		[SerializeField]
		private CraftingRecipeList ashwalkerCraftingRecipesList = null;

		[SerializeField]
		private AddressableAudioSource consumeSound = null;

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

		private bool wasMapped;

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
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
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

		private void EatBody(LivingHealthMasterBase healthMasterBase)
		{
			SoundManager.PlayNetworkedAtPos(consumeSound, registerTile.WorldPositionServer, sourceObj: gameObject);

			if (healthMasterBase.playerScript.OrNull()?.mind?.occupation.OrNull()?.JobType == JobType.ASHWALKER
			    && healthMasterBase.playerScript.OrNull()?.mind?.IsGhosting == false)
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

			Chat.AddLocalMsgToChat($"Serrated tendrils eagerly pull {healthMasterBase.gameObject.ExpensiveName()} to the {gameObject.ExpensiveName()}, tearing the body apart as its blood seeps over the eggs.", gameObject);

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

		private void SetSprite()
		{
			spriteHandler.ChangeSprite(ashwalkerEggs > 0 ? 1 : 0);
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
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
		}

		//Would use only IServerSpawn, but that is called before the ghost role manager which wipes the list at RoundStart...
		private void OnRoundRestart()
		{
			SetSprite();

			createdRoleKey = GhostRoleManager.Instance.ServerCreateRole(ghostRole);
			var role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];

			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKey, 1, ashwalkerEggs, -1);

			role.OnPlayerAdded += OnSpawnPlayer;

			EventManager.RemoveHandler(Event.LavalandFirstEntered, OnRoundRestart);
		}

		private void OnSpawnPlayer(PlayerInfo player)
		{
			SpawnAshwalker(player);
		}

		private void SpawnAshwalker(PlayerInfo player, bool costEgg = true)
		{
			//Since this is being called from an Action<> this could be null.
			if (this == null || gameObject == null)
			{
				//Remove the player from all roles (as createdRoleKey will Error)
				GhostRoleManager.Instance.ServerRemoveWaitingPlayer(player);
				Logger.LogError("Ghost role spawn called on null ashwalker, was the role not removed on destruction?");
				return;
			}

			var characterSettings = player.Script.characterSettings;

			if (characterSettings == null)
			{
				characterSettings = new CharacterSheet();
			}

			//TODO this replaces their old race, character settings needs a refactor to have them per body
			characterSettings.Species = ashwalkerRaceData.name;
			characterSettings.SerialisedExternalCustom?.Clear();

			//Give random lizard name
			characterSettings.Name = StringManager.GetRandomLizardName(characterSettings.GetGender());

			//Respawn the player
			player.Script.playerNetworkActions.ServerRespawnPlayerSpecial("Ashwalker", registerTile.WorldPositionServer);

			//Wipe crafting recipes and add Ashwalker ones
			var crafting = player.Script.PlayerCrafting;
			crafting.ForgetAllRecipes();
			foreach (var recipe in ashwalkerCraftingRecipesList.CraftingRecipes)
			{
				crafting.LearnRecipe(recipe);
			}

			if (costEgg)
			{
				ashwalkerEggs--;
				SetSprite();
				Chat.AddLocalMsgToChat("An egg hatches in the nest!", gameObject);
			}
			else
			{
				Chat.AddLocalMsgToChat("An creature emerges from the nest. Glory to the Necropolis!", gameObject);
			}

			//Decrease the remaining roles
			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKey, 1, ashwalkerEggs, -1);

			//Remove the player so they can join again once they die
			GhostRoleManager.Instance.ServerRemoveWaitingPlayer(createdRoleKey, player);

			Chat.AddExamineMsg(player.GameObject, "You have been pulled back from beyond the grave, with a new body and renewed purpose. Glory to the Necropolis!");

			//Ashwalkers cant speak or understand common
			player.Script.MobLanguages.RemoveLanguage(LanguageManager.Common, true);
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