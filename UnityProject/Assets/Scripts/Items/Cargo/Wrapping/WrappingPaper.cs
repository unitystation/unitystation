using System;
using UnityEngine;

namespace Items.Cargo.Wrapping
{
	public class WrappingPaper : MonoBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		[SerializeField][Tooltip("What kind of paper will be used to wrap. Determines the package appearance.")]
		private WrapType wrapType;
		public WrapType WrapType => wrapType;
		private Stackable stackable;
		public int PaperAmount => stackable.Amount;
		private Pickupable pickupable;
		public ItemSlot ItemSlot => pickupable.ItemSlot;

		private void Awake()
		{
			stackable = GetComponent<Stackable>();
			pickupable = GetComponent<Pickupable>();
		}

		private bool CommonWillInteract(TargetedInteraction interaction, NetworkSide side)
		{
			return interaction.TargetObject != gameObject &&
			       interaction.Intent == Intent.Help &&
			       interaction.TargetObject.TryGetComponent(out WrappableBase _);
		}

		#region Clicked object in the world

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			       CommonWillInteract(interaction, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			interaction.TargetObject.GetComponent<WrappableBase>().TryWrap(interaction.Performer, this);
		}

		#endregion

		#region Clicked object in inventory

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			       CommonWillInteract(interaction, side);
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			interaction.TargetObject.GetComponent<WrappableBase>().TryWrap(interaction.Performer, this);
		}

		#endregion

	}

	public enum WrapType
	{
		Normal,
		Festive
	}
}