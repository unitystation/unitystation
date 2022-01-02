using System;
using System.Collections.Generic;
using System.Linq;
using Systems.CraftingV2;
using Systems.GhostRoles;
using HealthV2;
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

		//Nest eats bodies to allow for more ashwalkers
		private int ashwalkerRoles = 3;

		//30 Seconds between eating next body
		private int timeBetweenEgg = 30;

		private uint createdRoleKey;

		private int timer = 0;

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
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PeriodicUpdate);
			integrity.OnWillDestroyServer.RemoveListener(OnDestruction);
		}

		#endregion

		private void PeriodicUpdate()
		{
			if (timer > 0)
			{
				timer--;
				return;
			}

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
		}

		private void EatMobBody(LivingHealthBehaviour mobHealth)
		{
			mobHealth.Harvest();
			IncreaseEgg();
		}

		private void EatBody(LivingHealthMasterBase playerHealth)
		{
			playerHealth.Gib();
			IncreaseEgg();
		}

		private void IncreaseEgg()
		{
			timer = timeBetweenEgg;
			ashwalkerRoles++;
			SetSprite();

			Chat.AddLocalMsgToChat("An egg matures in the nest", gameObject);
		}

		private void DecreaseEgg()
		{
			ashwalkerRoles--;
			SetSprite();

			Chat.AddLocalMsgToChat("An egg hatches in the nest", gameObject);
		}

		private void SetSprite()
		{
			spriteHandler.ChangeSprite(ashwalkerRoles > 0 ? 1 : 0);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			SetSprite();

			createdRoleKey = GhostRoleManager.Instance.ServerCreateRole(ghostRole);
			GhostRoleServer role = GhostRoleManager.Instance.serverAvailableRoles[createdRoleKey];

			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKey, 1, ashwalkerRoles, -1);

			role.OnPlayerAdded += OnSpawnPlayer;
		}

		private void OnSpawnPlayer(ConnectedPlayer player)
		{
			var newCharacterSettings = player.CharacterSettings;

			//TODO this replaces their old race, charactersettings needs a refactor to have them per body
			newCharacterSettings.Species = ashwalkerRaceData.name;

			//TODO change player name

			player.Script.playerNetworkActions.ServerRespawnPlayerSpecial("Ashwalker");

			//Wipe crafting recipes and add ashwalker ones
			var crafting = player.Script.PlayerCrafting;
			crafting.ForgetAllRecipes();
			foreach (var recipe in ashwalkerCraftingRecipesList.CraftingRecipes)
			{
				crafting.LearnRecipe(recipe);
			}

			DecreaseEgg();
			GhostRoleManager.Instance.ServerUpdateRole(createdRoleKey, 1, ashwalkerRoles, -1);
		}

		private void OnDestruction(DestructionInfo info)
		{
			GhostRoleManager.Instance.ServerRemoveRole(createdRoleKey);
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"There {(ashwalkerRoles == 1 ? "is" : "are")} {ashwalkerRoles} egg{(ashwalkerRoles == 1 ? "" : "s")} in the nest";
		}
	}
}