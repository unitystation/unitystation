using System;
using System.Collections;
using System.Threading.Tasks;
using Items;
using Mirror;
using UnityEngine;
using Systems.Clothing;
using HealthV2;
using Systems.Antagonists;
using Systems.MobAIs;

namespace Clothing
{
	[RequireComponent(typeof(ClothingV2))]
	public class FacehuggerImpregnation : NetworkBehaviour, IServerInventoryMove, IClientInventoryMove, ICheckedInteractable<PositionalHandApply>
	{
		[Tooltip("Is this a toy? Won't impregnate the wearer")][SerializeField]
		private bool isToy = false;

		[Tooltip("Time it takes to successfully insert the larvae inside the victim")] [SerializeField]
		private float coitusTime = 10;

		[Tooltip("Reference to facehugger gameObject so we can spawn it")] [SerializeField]
		private GameObject facehugger = null;

		[Tooltip("Reference to larvae organ gameObject so we can spawn it")] [SerializeField]
		private GameObject larvae = null;

		private bool isAlive = true;
		private ClothingV2 clothingV2;
		private ItemAttributesV2 itemAttributesV2;
		private SpriteHandler spriteHandler;

		private CooldownInstance alienTryHuggerCooldown = new CooldownInstance(2f);

		private Pickupable pickupable;

		private void Awake()
		{
			clothingV2 = GetComponent<ClothingV2>();
			itemAttributesV2 = GetComponent<ItemAttributesV2>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			pickupable = GetComponent<Pickupable>();
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			if (!isToy) return;

			itemAttributesV2.ServerSetArticleName("Toy Facehugger");
			itemAttributesV2.ServerSetArticleDescription("Still pretty scary!");
		}

		public void KillHugger()
		{
			isAlive = false;
			spriteHandler.ChangeSprite(1);
			clothingV2.ChangeSprite(1);
			itemAttributesV2.ServerSetArticleDescription("It is not moving anymore.");
		}

		private void OnTakingOff()
		{
			if (!isAlive || isToy)
			{
				return;
			}

			StopAllCoroutines();
		}

		private void OnWearing(RegisterPlayer player)
		{
			if (!isAlive || isToy)
			{
				return;
			}

			player.ServerStun(coitusTime, true);
			StartCoroutine(Coitus(player.PlayerScript.playerHealth));
		}

		private void OnReleasing()
		{
			// check if gameObject is active because gameObject needs to be active to StartCoroutine
			if (!isAlive || isToy || !gameObject.activeInHierarchy)
			{
				return;
			}

			StartCoroutine(Release());
		}

		private IEnumerator Coitus(PlayerHealthV2 player)
		{
			yield return WaitFor.Seconds(coitusTime);
			_ = Pregnancy(player);
			yield return WaitFor.EndOfFrame;
		}

		private async Task Pregnancy(PlayerHealthV2 player)
		{
			KillHugger();

			GameObject embryo = Spawn.ServerPrefab(larvae, SpawnDestination.At(gameObject), 1).GameObject;

			if (player.GetStomachs().Count == 0) return;

			player.GetStomachs()[0].RelatedPart.OrganStorage.ServerTryAdd(embryo);
		}

		private IEnumerator Release()
		{
			//TODO wait until the object's velocity is 0 instead of a fixed amount of time!
			yield return WaitFor.Seconds(0.6f);
			Spawn.ServerPrefab(facehugger, gameObject.transform.position);
			_ = Despawn.ServerSingle(gameObject);
			yield return WaitFor.EndOfFrame;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (this.gameObject != info.MovedObject.gameObject) return;

			RegisterPlayer registerPlayer;

			if (info.ToSlot != null && info.ToSlot?.NamedSlot != null)
			{
				registerPlayer = info.ToRootPlayer;

				if (registerPlayer != null && info.ToSlot.NamedSlot == NamedSlot.mask)
				{
					OnWearing(registerPlayer);
				}
			}

			if (info.FromSlot != null && info.FromSlot?.NamedSlot != null && info.ToSlot != null)
			{
				registerPlayer = info.FromRootPlayer;

				if (registerPlayer != null && info.FromSlot.NamedSlot == NamedSlot.mask)
				{
					OnTakingOff();
				}
			}
			else if (info.FromSlot != null && info.ToSlot == null)
			{
				OnReleasing();
			}
		}

		public void OnInventoryMoveClient(ClientInventoryMove info)
		{
			var playerScript = PlayerManager.LocalPlayerScript;
			if ((CustomNetworkManager.Instance._isServer && GameData.IsHeadlessServer)
			    || playerScript == null
			    || playerScript.PlayerNetworkActions == null
			    || playerScript.playerHealth == null)
			{
				return;
			}

			//Aliens don't go into crit
			if(playerScript.PlayerType == PlayerTypes.Alien) return;

			if (info.ClientInventoryMoveType == ClientInventoryMoveType.Added
				&& playerScript.DynamicItemStorage.InventoryHasObjectInCategory(gameObject, NamedSlot.mask))
			{
				OverlayCrits.Instance.SetState(OverlayState.crit);
			}
			else if (info.ClientInventoryMoveType == ClientInventoryMoveType.Removed
				&& playerScript.DynamicItemStorage.InventoryHasObjectInCategory(gameObject, NamedSlot.mask) == false)
			{
				OverlayCrits.Instance.SetState(OverlayState.normal);
			}
		}

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (interaction.HandObject != gameObject) return false;

			if (interaction.TargetObject == null) return false;

			if (interaction.TargetObject == interaction.Performer) return false;

			if (interaction.TargetObject.TryGetComponent<PlayerScript>(out var playerScript) == false) return false;

			if (playerScript.PlayerType != PlayerTypes.Normal) return false;

			if (DefaultWillInteract.Default(interaction, side, PlayerTypes.Alien) == false) return false;

			if (side == NetworkSide.Client)
			{
				if (Cooldowns.TryStartClient(interaction, alienTryHuggerCooldown) == false)
				{
					Chat.AddExamineMsgToClient("The facehugger needs time to recuperate from its failure!");
					return false;
				}
			}

			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if(Cooldowns.TryStartServer(interaction, alienTryHuggerCooldown) == false) return;

			//Alien clicking on layer with face hugger in hand
			if (interaction.TargetObject.TryGetComponent<PlayerScript>(out var playerScript) == false) return;

			//If not laying down small chance to hug and check for anti hugger items
			bool success = (playerScript.RegisterPlayer.IsLayingDown == false && DMMath.Prob(80)
			                || FaceHugAction.HasAntihuggerItem(playerScript.Equipment)) == false;

			interaction.PerformerPlayerScript.WeaponNetworkActions.RpcMeleeAttackLerp(interaction.TargetVector, gameObject);

			Chat.AddActionMsgToChat(interaction.Performer, success == false ?
					$"You fail to attach the {gameObject.ExpensiveName()} to {playerScript.visibleName}"
					: $"You attach the {gameObject.ExpensiveName()} to {playerScript.visibleName}",
				success == false ? $"{interaction.Performer.ExpensiveName()} attempted to attach a {gameObject.ExpensiveName()} to {playerScript.visibleName} but failed!"
					: $"{interaction.Performer.ExpensiveName()} attaches a {gameObject.ExpensiveName()} to {playerScript.visibleName}!");

			if(success == false) return;

			foreach (var itemSlot in playerScript.Equipment.ItemStorage.GetNamedItemSlots(NamedSlot.mask))
			{
				Inventory.ServerTransfer(pickupable.ItemSlot, itemSlot, ReplacementStrategy.DropOther);
				break;
			}
		}
	}
}
