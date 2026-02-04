using System.Collections.Generic;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Objects.Doors.DoorDeconstruction.DeconstructionMethods
{
	[System.Serializable]
	public class AirlockDeconstruction : IDeconstructionMethod
	{
		[SerializeField] private float deconstructTime = 2.5f;

		public bool CanInteract(ConstructibleDoor door, HandApply interaction, NetworkSide side)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Crowbar) == false) return false;
			// Check door state: must be welded, bolted and not powered
			return door.IsWeldedShut() && door.HasBoltsDown() == false && door.HasPower() == false;
		}

		public void ServerPerform(ConstructibleDoor door, HandApply interaction)
		{
			// Server-side action: perform a timed deconstruction and destroy the door
			ToolUtils.ServerUseToolWithActionMessages(interaction, deconstructTime,
				$"You start to pry apart the {door.gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} starts to pry apart the {door.gameObject.ExpensiveName()}...",
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
			return $"Use a <b>crowbar</b> to pry this door apart. Time: {deconstructTime}s";
		}

		public List<TextColor> InteractionStrings(ConstructibleDoor door)
		{
			var tips = new List<TextColor>();
			if (PlayerManager.LocalPlayerScript == null) return tips;

			var items = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetHandSlots();
			foreach (var slot in items)
			{
				if (Validations.HasItemTrait(slot.ItemObject, CommonTraits.Instance.Crowbar))
				{
					tips.Add(new TextColor { Text = $"Pry apart the door with a <b>crowbar</b> ({deconstructTime}s)", Color = Color.green });
				}
			}

			return tips;
		}
	}
}
