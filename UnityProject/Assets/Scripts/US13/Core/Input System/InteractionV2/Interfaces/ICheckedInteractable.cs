
using US13.Core.Input_System.InteractionV2.Interactions.Internal;

namespace US13.Core.Input_System.InteractionV2.Interfaces
{
	/// <summary>
	/// Indicates an Interactable Component which has custom, non-default WillInteract logic. This is the recommended
	/// interface to implement for most interactable components.
	/// </summary>
	public interface ICheckedInteractable<T> : ICheckable<T>, IInteractable<T>
		where T : Interaction
	{

	}
}
