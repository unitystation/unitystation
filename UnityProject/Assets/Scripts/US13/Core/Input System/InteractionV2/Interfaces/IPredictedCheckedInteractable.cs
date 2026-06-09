using US13.Core.Input_System.InteractionV2.Interactions.Internal;

namespace US13.Core.Input_System.InteractionV2.Interfaces
{
	/// <summary>
	/// Indicates an interactable component which can perform client prediction and also has custom
	/// WillInteract logic.
	/// </summary>
	public interface IPredictedCheckedInteractable<T> : IPredictedInteractable<T>, ICheckedInteractable<T>
		where T : Interaction
	{
	}
}
