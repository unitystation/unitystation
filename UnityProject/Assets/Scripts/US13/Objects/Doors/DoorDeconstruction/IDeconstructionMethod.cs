using System.Collections.Generic;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.UI.Systems.Tooltips.HoverTooltips;

namespace US13.Objects.Doors.DoorDeconstruction
{
	public interface IDeconstructionMethod
	{
		// Check whether this method should allow interaction.
		bool CanInteract(ConstructibleDoor door, HandApply interaction, NetworkSide side);

		// Called on the server to perform the action.
		void ServerPerform(ConstructibleDoor door, HandApply interaction);

		// Provide hover tip text (single line or multi-line) relevant to this method.
		string HoverTip(ConstructibleDoor door);

		// Provide interaction strings to display in tooltip UI.
		List<TextColor> InteractionStrings(ConstructibleDoor door);
	}
}
