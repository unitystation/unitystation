using UnityEngine;

namespace Player
{
	/// <summary>
	/// Consume food and drink from floor
	/// </summary>
	public class ConsumeFromFloor : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (interaction.HandObject != null) return false;

			if (interaction.TargetObject == null) return false;

			if (interaction.TargetObject.GetComponent<Consumable>() == null) return false;

			if (DefaultWillInteract.Default(interaction, side,
				    Validations.CheckState(x => x.CanConsumeFromFloor)) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.TargetObject.TryGetComponent<Consumable>(out var consumable) == false) return;

			consumable.TryConsume(interaction.Performer);
		}
	}
}