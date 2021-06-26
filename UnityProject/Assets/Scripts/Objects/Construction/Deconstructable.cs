using System;
using UnityEngine;

namespace Objects.Construction
{
	/// <summary>
	/// Destroys the object when deconstructed using a set tool type
	/// </summary>
	public class Deconstructable : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[SerializeField]
		private ItemTrait deconstructToolTrait = null;

		[SerializeField]
		private float deconstructTime = 3f;

		private Integrity integrity;

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, deconstructToolTrait)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, deconstructTime,
				$"You start to deconstruct the {gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} starts to deconstruct the {gameObject.ExpensiveName()}...",
				$"You deconstruct the {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} deconstructs the {gameObject.ExpensiveName()}'.",
				() =>
				{
					integrity.ForceDestroy();
				});
		}
	}
}
