using System;
using System.Linq;
using UnityEngine;


namespace Objects.Kitchen
{
	/// <summary>
	/// Allows Griddle to be interacted with. Player can put food on the griddle to cook it.
	/// </summary>
	[RequireComponent(typeof(Griddle))]
	public class InteractableGriddle : MonoBehaviour, ICheckedInteractable<PositionalHandApply>,
			IRightClickable, ICheckedInteractable<ContextMenuApply>, IDisposable
	{

		[SerializeField]
		[Tooltip("The GameObject in this hierarchy that contains the SpriteClickRegion component defining the griddle's table.")]
		private SpriteClickRegion tableRegion = default;

		[SerializeField]
		[Tooltip("The GameObject in this hierarchy that contains the SpriteClickRegion component defining the griddle's power knob.")]
		private SpriteClickRegion powerRegion = default;

		private Griddle griddle;

		private void Start()
		{
			griddle = GetComponent<Griddle>();
		}

		void OnDestroy()
		{
			this.Dispose();
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
			if (tableRegion.Contains(interaction.WorldPositionTarget))
			{
				griddle.RequestDropItem(interaction);
			}
			else if (powerRegion.Contains(interaction.WorldPositionTarget))
			{
				griddle.RequestToggleActive();
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
					griddle.RequestToggleActive();
					break;
			}
		}

		#endregion Interaction-ContextMenu

		public void Dispose()
		{
			griddle.OrNull()?.Dispose();
		}
	}
}
