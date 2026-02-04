using System.Collections.Generic;
using System.Text;
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
			return null;
		}

		public List<TextColor> InteractionStrings(ConstructibleDoor door)
		{
			var tips = new List<TextColor>();
			StringBuilder tipBuilder = new StringBuilder();
			tipBuilder.AppendLine("----");
			tipBuilder.AppendLine("To deconstruct this airlock door:");
			tipBuilder.AppendLine("1. Ensure the door is <b>welded shut</b>.");
			tipBuilder.AppendLine("2. Ensure the door's <b>bolts are retracted</b>.");
			tipBuilder.AppendLine("3. Ensure the door has <b>no power</b>.");
			tipBuilder.AppendLine("4. Use a <b>crowbar</b> to pry the door apart.");
			tipBuilder.AppendLine("----");
			tips.Add(new TextColor { Text = tipBuilder.ToString(), Color = Color.yellow });
			return tips;
		}
	}
}
