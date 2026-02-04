using System.Collections.Generic;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Objects.Doors.DoorDeconstruction.DeconstructionMethods
{
	[System.Serializable]
	public class WelderDeconstruction : IDeconstructionMethod
	{
		[SerializeField] private float deconstructTime = 4.5f;

		public bool CanInteract(ConstructibleDoor door, HandApply interaction, NetworkSide side)
		{
			return Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder);
		}

		public void ServerPerform(ConstructibleDoor door, HandApply interaction)
		{
			// Server-side action: destroy the door using a welder.
			ToolUtils.ServerUseToolWithActionMessages(interaction, deconstructTime,
				$"You start to deconstruct apart the {door.gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} starts to deconstruct the {door.gameObject.ExpensiveName()}...",
				$"You finished deconstructing the {door.gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} finished deconstructing the {door.gameObject.ExpensiveName()}.",
				() =>
				{
					// Use the ConstructibleDoor's destruction flow to spawn assembly
					door.WhenDestroyed(new DestructionInfo(DamageType.Brute, door.GetComponent<Integrity>()));
				});
		}

		public string HoverTip(ConstructibleDoor door)
		{
			return null;
		}

		public List<TextColor> InteractionStrings(ConstructibleDoor door)
		{
			return new();
		}
	}
}