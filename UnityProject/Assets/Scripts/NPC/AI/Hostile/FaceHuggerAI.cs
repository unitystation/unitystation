using System;
using System.Collections.Generic;
using Clothing;
using NPC.AI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NPC
{
	public class FaceHuggerAI : MobAI, ICheckedInteractable<HandApply>, IServerSpawn
	{
		[SerializeField] private List<string> deathSounds = new List<string>();
		[SerializeField] private List<string> randomSound = new List<string>();
		[Tooltip("Amount of time to wait between each random sound. Decreasing this value could affect performance!")]
		[SerializeField]
		private int playRandomSoundTimer = 3;
		[SerializeField]
		[Range(0,100)]
		private int randomSoundProbability = 20;
		[SerializeField] private float searchTickRate = 0.5f;
		private float movementTickRate = 1f;
		private float moveWaitTime = 0f;
		private float searchWaitTime = 0f;
		private bool deathSoundPlayed = false;
		[SerializeField] private MobStatus currentStatus;
		public MobStatus CurrentStatus => currentStatus;

		[SerializeField] private GameObject maskObject = null;

		private LayerMask hitMask;
		private int playersLayer;
		private MobMeleeAction mobMeleeAction;
		private ConeOfSight coneOfSight;
		private SimpleAnimal simpleAnimal;

		protected override void Awake()
		{
			base.Awake();
			simpleAnimal = GetComponent<SimpleAnimal>();
		}

		public override void OnEnable()
		{
			base.OnEnable();
			mobMeleeAction = gameObject.GetComponent<MobMeleeAction>();
			hitMask = LayerMask.GetMask("Walls", "Players");
			playersLayer = LayerMask.NameToLayer("Players");
			coneOfSight = GetComponent<ConeOfSight>();
			PlayRandomSound();
		}

		protected override void AIStartServer()
		{
			movementTickRate = Random.Range(1f, 3f);
			BeginSearch();
		}

		/// <summary>
		/// Declare the current state of the mob as Searching.
		/// </summary>
		private void BeginSearch()
		{
			searchWaitTime = 0f;
			currentStatus = MobStatus.Searching;
		}

		/// <summary>
		/// Declare the current state of the mob as attacking.
		/// </summary>
		/// <param name="target">Gameobject that this mob will target to attack</param>
		private void BeginAttack(GameObject target)
		{
			currentStatus = MobStatus.Attacking;
			FollowTarget(target.transform);
		}

		protected override void ResetBehaviours()
		{
			base.ResetBehaviours();
			mobFollow.followTarget = null;
			currentStatus = MobStatus.None;
			searchWaitTime = 0f;
		}

		private void MonitorIdleness()
		{
			if (!mobMeleeAction.performingDecision && mobMeleeAction.followTarget == null)
			{
				BeginSearch();
			}
		}

		/// <summary>
		/// Looks around and tries to find players to target
		/// </summary>
		/// <returns>Gameobject of the first player it found</returns>
		protected virtual GameObject SearchForTarget()
		{
			var hits = coneOfSight.GetObjectsInSight(hitMask, dirSprites.CurrentFacingDirection, 10f, 20);
			if (hits.Count == 0)
			{
				return null;
			}

			foreach (Collider2D coll in hits)
			{
				if (coll.gameObject.layer == playersLayer)
				{
					return coll.gameObject;
				}
			}

			return null;
		}

		/// <summary>
		/// Makes the mob move to a random direction
		/// </summary>
		protected virtual void DoRandomMove()
		{
			var nudgeDir = GetNudgeDirFromInt(Random.Range(0, 8));
			if (registerObject.Matrix.IsSpaceAt(registerObject.LocalPosition + nudgeDir.To3Int(), true))
			{
				for (int i = 0; i < 8; i++)
				{
					var testDir = GetNudgeDirFromInt(i);
					var checkTile = registerObject.LocalPosition + testDir.To3Int();
					if (registerObject.Matrix.IsSpaceAt(checkTile, true))
					{
						continue;
					}
					if (registerObject.Matrix.IsPassableAt(checkTile, true))
					{
						nudgeDir = testDir;
						break;
					}
					else
					{
						if (!registerObject.Matrix.GetFirst<DoorController>(checkTile, true))
						{
							continue;
						}
						nudgeDir = testDir;
						break;
					}
				}
			}

			NudgeInDirection(nudgeDir);
			movementTickRate = Random.Range(1f, 3f);
		}

		protected virtual void PlayRandomSound(bool force = false)
		{
			if (IsDead || IsUnconscious || randomSound.Count <= 0)
			{
				return;
			}

			if (!force)
			{
				if (!DMMath.Prob(randomSoundProbability))
				{
					return;
				}
			}

			SoundManager.PlayNetworkedAtPos(
				randomSound.PickRandom(),
				transform.position,
				Random.Range(0.9f, 1.1f),
				sourceObj: gameObject);

			Invoke(nameof(PlayRandomSound), playRandomSoundTimer);
		}

		/// <summary>
		/// What happens when the mob dies or is unconscious
		/// </summary>
		protected virtual void HandleDeathOrUnconscious()
		{
			if (!IsDead || deathSoundPlayed || deathSounds.Count <= 0) return;
			deathSoundPlayed = true;
			SoundManager.PlayNetworkedAtPos(
				deathSounds.PickRandom(),
				transform.position,
				Random.Range(0.9f, 1.1f),
				sourceObj: gameObject);
			XenoQueenAI.CurrentHuggerAmt -= 1;
		}

		/// <summary>
		/// What happens if the mob is searching
		/// </summary>
		protected virtual void HandleSearch()
		{
			moveWaitTime += Time.deltaTime;
			if (moveWaitTime >= movementTickRate)
			{
				moveWaitTime = 0f;
				DoRandomMove();
			}

			searchWaitTime += Time.deltaTime;
			if (!(searchWaitTime >= searchTickRate)) return;
			searchWaitTime = 0f;
			var findTarget = SearchForTarget();
			if (findTarget != null)
			{
				BeginAttack(findTarget);
			}
			else
			{
				BeginSearch();
			}
		}

		protected override void UpdateMe()
		{
			base.UpdateMe();

			if (!isServer) return;

			if (IsDead || IsUnconscious)
			{
				HandleDeathOrUnconscious();
				return;
			}

			switch (currentStatus)
			{
				case MobStatus.Searching:
					HandleSearch();
					return;
				case MobStatus.Attacking:
					MonitorIdleness();
					break;
				case MobStatus.None:
					MonitorIdleness();
					break;
				default:
					MonitorIdleness();
					break;
			}
		}

		protected override void OnAttackReceived(GameObject damagedBy = null)
		{
			if (damagedBy == null)
			{
				StartFleeing(damagedBy);
				return;
			}

			if (health.OverallHealth < -20f)
			{
				//30% chance the mob decides to flee:
				if (Random.value < 0.3f)
				{
					StartFleeing(damagedBy, 5f);
					return;
				}
			}

			if (damagedBy != mobMeleeAction.followTarget)
			{
				//80% chance the mob decides to attack the new attacker
				if (Random.value < 0.8f)
				{
					var playerScript = damagedBy.GetComponent<PlayerScript>();
					if (playerScript != null)
					{
						BeginAttack(damagedBy);
						return;
					}
				}
			}
		}

		public override void LocalChatReceived(ChatEvent chatEvent)
		{
			if (chatEvent.originator == null) return;

			if (currentStatus != MobStatus.Searching && currentStatus != MobStatus.None) return;

			//face towards the origin:
			var dir = (chatEvent.originator.transform.position - transform.position).normalized;
			dirSprites.ChangeDirection(dir);

			//Then scan to see if anyone is there:
			var findTarget = SearchForTarget();
			if (findTarget != null)
			{
				BeginAttack(findTarget);
			}
		}

		private void TryFacehug(Vector3 dir, LivingHealthBehaviour player)
		{
			var playerInventory = player.gameObject.GetComponent<PlayerScript>()?.Equipment;

			if (playerInventory == null)
			{
				return;
			}

			string verb;
			bool success;

			if (HasAntihuggerItem(playerInventory))
			{
				verb = "tried to hug";
				success = false;
			}
			else
			{
				verb = "hugged";
				success = true;
			}

			mobMeleeAction.ServerDoLerpAnimation(dir);

			Chat.AddAttackMsgToChat(
				gameObject,
				player.gameObject,
				BodyPartType.Head,
				null,
				verb);

			SoundManager.PlayNetworkedAtPos(
				"bite",
				player.gameObject.RegisterTile().WorldPositionServer,
				1f,
				true,
				player.gameObject);

			if (success)
			{
				RegisterPlayer registerPlayer = player.gameObject.GetComponent<RegisterPlayer>();
				Facehug(playerInventory, registerPlayer);
			}

		}

		private void Facehug(Equipment playerInventory, RegisterPlayer player)
		{
			var result = Spawn.ServerPrefab(maskObject);
			var mask = result.GameObject;

			Inventory.ServerAdd(
				mask,
				playerInventory.ItemStorage.GetNamedItemSlot(NamedSlot.mask),
				ReplacementStrategy.DespawnOther);

			Despawn.ServerSingle(gameObject);
		}

		/// <summary>
		/// Check the player inventory for an item in head, mask or eyes slots with
		/// Antifacehugger trait. It also drops all items that doesn't have the trait.
		/// </summary>
		/// <param name="equipment"></param>
		/// <returns>True if the player is protected against huggers, false it not</returns>
		private bool HasAntihuggerItem(Equipment equipment)
		{
			bool antiHugger = false;

			foreach (var slot in faceSlots)
			{
				var item = equipment.ItemStorage.GetNamedItemSlot(slot)?.Item;
				if (item == null || item.gameObject == null)
				{
					continue;
				}

				if (!Validations.HasItemTrait(item.gameObject, CommonTraits.Instance.AntiFacehugger))
				{
					Inventory.ServerDrop(equipment.ItemStorage.GetNamedItemSlot(slot));
				}
				else
				{
					var integrity = item.gameObject.GetComponent<Integrity>();
					if (integrity != null)
					{
						// Your protection might break!
						integrity.ApplyDamage(7.5f, AttackType.Melee, DamageType.Brute);
					}
					antiHugger = true;
				}
			}
			return antiHugger;
		}

		public override void ActOnLiving(Vector3 dir, LivingHealthBehaviour healthBehaviour)
		{
			TryFacehug(dir, healthBehaviour);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side)
			       && (interaction.HandObject == null
			           || (interaction.Intent == Intent.Help || interaction.Intent == Intent.Grab));
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			var handSlot = interaction.HandSlot;

			var result = Spawn.ServerPrefab(maskObject);
			var mask = result.GameObject;

			if (IsDead || IsUnconscious)
			{
				mask.GetComponent<FacehuggerImpregnation>().KillHugger();
			}

			Inventory.ServerAdd(mask, handSlot, ReplacementStrategy.DropOther);

			Despawn.ServerSingle(gameObject);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			//FIXME This shouldn't be called by client yet it seems it is
			if (!isServer)
			{
				return;
			}

			XenoQueenAI.CurrentHuggerAmt++;
			dirSprites.SetToNPCLayer();
			registerObject.Passable = false;
			simpleAnimal.SetDeadState(false);
			ResetBehaviours();
			BeginSearch();
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);
			dirSprites.SetToBodyLayer();
			deathSoundPlayed = false;
			registerObject.Passable = true;
		}

		private readonly List<NamedSlot> faceSlots = new List<NamedSlot>()
		{
			NamedSlot.eyes,
			NamedSlot.head,
			NamedSlot.mask
		};

		public enum MobStatus
		{
			None,
			Searching,
			Attacking
		}
	}
}