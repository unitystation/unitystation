using Mirror;
using Player.Movement;
using UI.Action;
using UnityEngine;

namespace Items.Others
{
	public class ItemMagBoots : NetworkBehaviour, IServerInventoryMove, IMovementEffect
	{
		[Tooltip("The speed debuff to apply to run speed.")]
		[SerializeField]
		private float runSpeedDebuff = -1.5f;

		private SpriteHandler spriteHandler;
		private ItemAttributesV2 itemAttributesV2;
		private Pickupable pickupable;
		private MovementSynchronisation playerMove;
		private ItemActionButton actionButton;

		[SyncVar(hook = nameof(SyncClientState))]
		private bool isOn = false;

		public float RunningSpeedModifier => runSpeedDebuff;

		public float WalkingSpeedModifier => 0;


		public float CrawlingSpeedModifier => 0;


		private enum SpriteState
		{
			Off = 0,
			On = 1
		}

		#region Lifecycle

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			itemAttributesV2 = GetComponent<ItemAttributesV2>();
			actionButton = GetComponent<ItemActionButton>();
			pickupable.RefreshUISlotImage();
		}

		private void OnEnable()
		{
			actionButton.ServerActionClicked += ToggleState;
		}

		private void OnDisable()
		{
			actionButton.ServerActionClicked -= ToggleState;
		}

		#endregion Lifecycle

		public void OnInventoryMoveServer(InventoryMove info)
		{
			if (info.ToPlayer != null)
			{
				playerMove = info.ToPlayer.PlayerScript.playerMove;
			}
			else if (info.FromPlayer != null)
			{
				playerMove = info.FromPlayer.PlayerScript.playerMove;
			}

			if (isOn)
			{
				ToggleOff();
			}
		}

		private void ToggleState()
		{
			if (isOn)
			{
				ToggleOff();
			}
			else
			{
				ToggleOn();
			}
		}

		private void ToggleOn()
		{
			isOn = true;
			ApplyEffect();
			spriteHandler.SetCatalogueIndexSprite((int) SpriteState.On);
			pickupable.RefreshUISlotImage();
		}

		private void ToggleOff()
		{
			isOn = false;
			RemoveEffect();
			spriteHandler.SetCatalogueIndexSprite((int) SpriteState.Off);
			pickupable.RefreshUISlotImage();
		}

		private void SyncClientState(bool OldState, bool NewState)
		{
			if (OldState)
			{
				RemoveEffect();
			}

			if (NewState)
			{
				ApplyEffect();
			}
		}

		private void ApplyEffect()
		{
			itemAttributesV2.AddTrait(CommonTraits.Instance.NoSlip);
			if (isServer)
			{
				playerMove.AddModifier(this);
				playerMove.CanBeWindPushed = false;
				playerMove.HasOwnGravity = true;
			}


		}

		private void RemoveEffect()
		{
			itemAttributesV2.RemoveTrait(CommonTraits.Instance.NoSlip);
			if (isServer)
			{
				playerMove.RemoveModifier(this);
				playerMove.CanBeWindPushed = true;
				playerMove.HasOwnGravity = false;
			}
		}
	}
}
