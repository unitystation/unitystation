/// <summary>
/// Indicates an interactable component which can perform client prediction and also has custom
/// WillInteract logic.
/// </summary>
public interface IPredictedCheckedInteractable<T> : IPredictedInteractable<T>, ICheckedInteractable<T>
	where T : Interaction
{
}
