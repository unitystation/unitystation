using System;
using System.Collections.Generic;
using Clothing;
using UnityEngine;
using Doors;
using Systems.Mob;
using Random = UnityEngine.Random;
using AddressableReferences;
using HealthV2;
using Messages.Server.SoundMessages;
using UnityEngine.Serialization;


namespace Systems.MobAIs
{
	public class FaceHuggerAI : GenericHostileAI, ICheckedInteractable<HandApply>, IServerSpawn
	{
		[SerializeField]
		[Tooltip("If true, this hugger won't be counted for the cap Queens use for lying eggs.")]
		private bool ignoreInQueenCount = false;
		[FormerlySerializedAs("Bite")] [SerializeField]
		private AddressableAudioSource bite = null;
		[SerializeField] private GameObject maskObject = null;
		private MobMeleeAction mobMeleeAction;

		protected override void Awake()
		{
			base.Awake();
			simpleAnimal = GetComponent<SimpleAnimal>();
		}

		public override void OnEnable()
		{
			base.OnEnable();
			mobMeleeAction = gameObject.GetComponent<MobMeleeAction>();
		}

		/// <summary>
		/// Looks around and tries to find players to target
		/// </summary>
		/// <returns>Gameobject of the first player it found</returns>
		protected override GameObject SearchForTarget()
		{
			var hits = coneOfSight.GetObjectsInSight(hitMask, LayerTypeSelection.Walls , directional.CurrentDirection.Vector, 10f, 20);
			if (hits.Count == 0)
			{
				return null;
			}

			foreach (var coll in hits)
			{
				if (coll.GameObject == null) continue;

				if (coll.GameObject.layer == playersLayer)
				{
					return coll.GameObject;
				}
			}

			return null;
		}

		/// <summary>
		/// What happens when the mob dies or is unconscious
		/// </summary>
		protected override void HandleDeathOrUnconscious()
		{
			base.HandleDeathOrUnconscious();

			if (ignoreInQueenCount == false)
			{
				XenoQueenAI.RemoveFacehuggerFromCount();
			}
		}

		private void TryFacehug(Vector3 dir, LivingHealthMasterBase player)
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

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: 1f);
			SoundManager.PlayNetworkedAtPos(bite, player.gameObject.RegisterTile().WorldPositionServer,
				audioSourceParameters, true, player.gameObject);

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

		public override void ActOnLiving(Vector3 dir, LivingHealthMasterBase healthBehaviour)
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

		public override void OnSpawnServer(SpawnInfo info)
		{
			if (ignoreInQueenCount == false)
			{
				XenoQueenAI.AddFacehuggerToCount();
			}
			base.OnSpawnServer(info);
			ResetBehaviours();
			BeginSearch();
		}

		private readonly List<NamedSlot> faceSlots = new List<NamedSlot>()
		{
			NamedSlot.eyes,
			NamedSlot.head,
			NamedSlot.mask
		};
	}
}
