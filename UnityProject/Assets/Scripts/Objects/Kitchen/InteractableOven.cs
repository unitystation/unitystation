using System;
using System.Linq;
using UnityEngine;


namespace Objects.Kitchen
{
	/// <summary>
	/// Allows Oven to be interacted with. Player can put food in the oven to cook it.
	/// The oven can be interacted with to, for example, check the remaining time.
	/// </summary>
	[RequireComponent(typeof(Oven))]
	public class InteractableOven : MonoBehaviour, IExaminable, ICheckedInteractable<PositionalHandApply>,
			IRightClickable, ICheckedInteractable<ContextMenuApply>
	{
		const int TIMER_INCREMENT = 5; // Seconds

		[SerializeField]
		[Tooltip("The GameObject in this hierarchy that contains the SpriteClickRegion component defining the oven's door.")]
		private SpriteClickRegion doorRegion = default;

		[SerializeField]
		[Tooltip("The GameObject in this hierarchy that contains the SpriteClickRegion component defining the oven's power button.")]
		private SpriteClickRegion powerRegion = default;

		private Oven oven;

		private void Start()
		{
			oven = GetComponent<Oven>();
		}

		public string Examine(Vector3 worldPos = default)
		{
			var contents = oven.HasContents
					? string.Join(", ", oven.Slots.Where(slot => slot.IsOccupied).Select(slot => $"<b>{slot.ItemObject.ExpensiveName()}</b>"))
					: "<b>nothing</b>";

			return $"The oven is currently <b>{oven.CurrentState.StateMsgForExamine}</b>. " +
					$"You see {contents} inside. " +
					$"There {(oven.StorageSize == 1 ? "is <b>one</b> slot" : $"are <b>{oven.StorageSize}</b> slots")} available.";
		}

		#region Interaction-PositionalHandApply

		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return (Validations.HasItemTrait(interaction, CommonTraits.Instance.Screwdriver) 
				|| Validations.HasItemTrait(interaction, CommonTraits.Instance.Crowbar)) == false;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			if (doorRegion.Contains(interaction.WorldPositionTarget))
			{
				oven.RequestDoorInteraction(interaction);
			}
			else if (powerRegion.Contains(interaction.WorldPositionTarget))
			{
				oven.RequestToggleActive();
			}
		}

		#endregion Interaction-PositionalHandApply

		#region Interaction-ContextMenu

		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			var activateInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "ToggleActive");
			if (!WillInteract(activateInteraction, NetworkSide.Client)) return result;
			result.AddElement("Activate", () => ContextMenuOptionClicked(activateInteraction));

			var ejectInteraction = ContextMenuApply.ByLocalPlayer(gameObject, "ToggleDoor");
			result.AddElement("Toggle Door", () => ContextMenuOptionClicked(ejectInteraction));

			return result;
		}

		private void ContextMenuOptionClicked(ContextMenuApply interaction)
		{
			InteractionUtils.RequestInteract(interaction, this);
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			switch (interaction.RequestedOption)
			{
				case "ToggleActive":
					oven.RequestToggleActive();
					break;
				case "ToggleDoor":
					oven.RequestDoorInteraction();
					break;
			}
		}

		#endregion Interaction-ContextMenu
	}
}
