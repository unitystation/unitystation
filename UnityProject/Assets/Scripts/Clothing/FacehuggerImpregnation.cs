using System;
using System.Collections;
using System.Threading.Tasks;
using Items;
using Mirror;
using UnityEngine;
using Systems.Clothing;
using HealthV2;

namespace Clothing
{
	[RequireComponent(typeof(ClothingV2))]
	public class FacehuggerImpregnation : NetworkBehaviour, IServerInventoryMove, IClientInventoryMove
	{
		[Tooltip("Is this a toy? Won't impregnate the wearer")][SerializeField]
		private bool isToy = false;

		[Tooltip("Time it takes to successfully insert the larvae inside the victim")] [SerializeField]
		private float coitusTime = 10;

		[Tooltip("Time it takes for the larvae to 'birth'")] [SerializeField]
		private int pregnancyTime = 300;

		[Tooltip("Reference to facehugger gameObject so we can spawn it")] [SerializeField]
		private GameObject facehugger = null;

		[Tooltip("Reference to larvae gameObject so we can spawn it")] [SerializeField]
		private GameObject larvae = null;

		private bool isAlive = true;
		private ClothingV2 clothingV2;
		private ItemAttributesV2 itemAttributesV2;
		private SpriteHandler spriteHandler;

		private void Awake()
		{
			clothingV2 = GetComponent<ClothingV2>();
			itemAttributesV2 = GetComponent<ItemAttributesV2>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
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
			await Task.Delay(TimeSpan.FromSeconds(pregnancyTime));
			//TODO check if the larvae was removed from stomach
			player.ApplyDamageToBodyPart(
				gameObject,
				200,
				AttackType.Internal,
				DamageType.Brute,
				BodyPartType.Chest);

			Spawn.ServerPrefab(larvae, player.gameObject.RegisterTile().WorldPositionServer);
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
			    || playerScript.playerNetworkActions == null
			    || playerScript.playerHealth == null)
			{
				return;
			}

			if (info.ClientInventoryMoveType == ClientInventoryMoveType.Added
				&& playerScript.DynamicItemStorage.InventoryHasObjectInCategory(gameObject, NamedSlot.mask))
			{
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.crit);
			}
			else if (info.ClientInventoryMoveType == ClientInventoryMoveType.Removed
				&& playerScript.DynamicItemStorage.InventoryHasObjectInCategory(gameObject, NamedSlot.mask) == false)
			{
				UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.normal);
			}
		}
	}
}
