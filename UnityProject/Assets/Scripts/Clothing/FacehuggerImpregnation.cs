using System;
using System.Collections;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

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
		private RegisterPlayer registerPlayer;
		private ClothingV2 clothingV2;
		private ItemAttributesV2 itemAttributesV2;

		private void Awake()
		{
			clothingV2 = GetComponent<ClothingV2>();
			itemAttributesV2 = GetComponent<ItemAttributesV2>();
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
			clothingV2.ServerChangeVariant(ClothingV2.ClothingVariantType.Tucked);
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
			if (!isAlive || isToy)
			{
				return;
			}

			StartCoroutine(Release());
		}

		private IEnumerator Coitus(PlayerHealth player)
		{
			yield return WaitFor.Seconds(coitusTime);
			Pregnancy(player);
			yield return WaitFor.EndOfFrame;
		}

		private async Task Pregnancy(PlayerHealth player)
		{
			KillHugger();
			await Task.Delay(TimeSpan.FromSeconds(pregnancyTime));
			//TODO check if the larvae was removed from stomach
			player.ApplyDamageToBodypart(
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
			Despawn.ServerSingle(gameObject);
			yield return WaitFor.EndOfFrame;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
			{
				registerPlayer = info.ToRootPlayer;

				if (registerPlayer != null && info.ToSlot.NamedSlot == NamedSlot.mask)
				{
					OnWearing(registerPlayer);
				}
			}

			if (info.FromSlot != null & info.FromSlot?.NamedSlot != null & info.ToSlot != null)
			{
				registerPlayer = info.FromRootPlayer;

				if (registerPlayer != null && info.FromSlot.NamedSlot == NamedSlot.mask)
				{
					OnTakingOff();
				}
			}
			else if (info.FromSlot != null & info.ToSlot == null)
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

			switch (info.ClientInventoryMoveType)
			{
				case ClientInventoryMoveType.Added
					when playerScript.playerNetworkActions.GetActiveItemInSlot(NamedSlot.mask)?.gameObject ==
					     gameObject:
					UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.crit);
					break;
				case ClientInventoryMoveType.Removed
					when playerScript.playerNetworkActions.GetActiveItemInSlot(NamedSlot.mask)?.gameObject !=
					     gameObject:
					UIManager.PlayerHealthUI.heartMonitor.overlayCrits.SetState(OverlayState.normal);
					break;
			}
		}
	}
}
