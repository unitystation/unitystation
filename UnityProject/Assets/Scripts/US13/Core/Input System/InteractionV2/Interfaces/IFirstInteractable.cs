
using US13.Core.Input_System.InteractionV2.Interactions.Internal;

namespace US13.Core.Input_System.InteractionV2.Interfaces
{
	/// <summary>
	/// Indicates an Interactable Component on a target object that should be checked BEFORE
	/// hand-item interactions. Use this when a target object needs to claim certain interactions
	/// with higher priority than whatever the player is holding.
	/// </summary>
	public interface IFirstInteractable<T> : ICheckedInteractable<T>
		where T : Interaction
	{
	}
}
