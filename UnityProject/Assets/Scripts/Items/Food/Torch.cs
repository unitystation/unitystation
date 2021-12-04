using System;
using Mirror;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;
using Items;

namespace items
{
    /// <summary>
    /// Lightable objects that can be used as lighters
    /// </summary>
    public class Torch : NetworkBehaviour, ICheckedInteractable<HandApply>,
        ICheckedInteractable<InventoryApply>, IServerDespawn
    {
		private const int DEFAULT_SPRITE = 0;
		private const int LIT_SPRITE = 1;

		[SerializeField]
		[Tooltip("Object to spawn after torch burnt")]
		private GameObject burntOutPrefab = null;

		[SerializeField]
		[Tooltip("Time after torch will destroy and spawn burnt remains")]
		private float burnTimeSeconds = 30;

		[SerializeField]
		private ItemTrait LightableSurface = null;

		[SyncVar]
		private bool isLit = false;

		private SpriteHandler spriteHandler;
		private FireSource fireSource;
		private Pickupable pickupable;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			fireSource = GetComponent<FireSource>();
			pickupable = GetComponent<Pickupable>();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			ServerChangeLit(false);
		}

		#region Interactions

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			// standard validation for interaction and harm intent
			if (!DefaultWillInteract.Default(interaction, side))
			{
				return false;
			}

			ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
			return attr.HasTrait(LightableSurface) && interaction.Intent == Intent.Harm;
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			// standard validation for interaction
			if (DefaultWillInteract.Default(interaction, side) == false || !interaction.TargetObject)
			{
				return false;
			}

			ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
			return attr.HasTrait(LightableSurface) && interaction.Intent == Intent.Harm;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			LightByObject();
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			LightByObject();
		}

		#endregion

		private void ServerChangeLit(bool isLitNow)
		{
			if (spriteHandler)
			{
				var newSpriteID = isLitNow ? LIT_SPRITE : DEFAULT_SPRITE;
				spriteHandler.ChangeSprite(newSpriteID);
			}

			// toggle flame from match
			if (fireSource)
			{
				fireSource.IsBurning = isLitNow;
			}

			if (isLitNow)
			{
				StartCoroutine(FireRoutine());
			}
		}

		private void LightByObject()
		{
			if (!isLit)
			{
				ServerChangeLit(true);
			}
		}

		private void Burn()
		{
			var worldPos = gameObject.AssumedWorldPosServer();
			var tr = gameObject.transform.parent;
			var rotation = RandomUtils.RandomRotation2D();

			// Print burn out message if in players inventory
			if (pickupable && pickupable.ItemSlot != null)
			{
				var player = pickupable.ItemSlot.Player;
				if (player)
				{
					Chat.AddExamineMsgFromServer(player.gameObject,
						$"Your {gameObject.ExpensiveName()} goes out.");
				}
			}

			_ = Despawn.ServerSingle(gameObject);
			Spawn.ServerPrefab(burntOutPrefab, worldPos, tr, rotation);
		}

		private IEnumerator FireRoutine()
		{
			// wait until match will burn
			yield return new WaitForSeconds(burnTimeSeconds);
			// despawn match and spawn burn
			Burn();
		}
	}
}
