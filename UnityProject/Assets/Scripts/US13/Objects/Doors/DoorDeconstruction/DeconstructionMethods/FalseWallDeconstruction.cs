using System.Collections.Generic;
using UnityEngine;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Lifecycle;
using US13.Items.Tool;
using US13.Items.Traits;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;

namespace US13.Objects.Doors.DoorDeconstruction.DeconstructionMethods
{
	[System.Serializable]
	public class FalseWallDeconstruction : IDeconstructionMethod
	{
		[SerializeField] private float deconstructTime = 4.5f;

		public bool CanInteract(ConstructibleDoor door, HandApply interaction, NetworkSide side)
		{
			if (door.CheckWeld() == false) return false;
			return Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Screwdriver);
		}

		public void ServerPerform(ConstructibleDoor door, HandApply interaction)
		{
			// Server-side action: destroy the false wall using a screwdriver.
			ToolUtils.ServerUseToolWithActionMessages(interaction, deconstructTime,
				$"You start to deconstruct apart the {door.gameObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} starts to deconstruct the {door.gameObject.ExpensiveName()}...",
				$"You finished deconstructing the {door.gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} finished deconstructing the {door.gameObject.ExpensiveName()}.",
				() =>
				{
					//TODO: Spawn grider for false-walls? Check what thing people are expected to have after removing false walls.
					_ = Despawn.ServerSingle(door.gameObject);
				});
		}

		public string HoverTip(ConstructibleDoor door)
		{
			return "<b>Weld</b> this false wall, then use a <b>screwdriver</b> to remove this false wall.";
		}

		public List<TextColor> InteractionStrings(ConstructibleDoor door)
		{
			return new();
		}
	}
}