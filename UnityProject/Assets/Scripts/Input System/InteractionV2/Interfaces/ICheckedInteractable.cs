
/// <summary>
/// Indicates an Interactable Component which has custom, non-default WillInteract logic. This is the recommended
/// interface to implement for most interactable components.
/// </summary>
public interface ICheckedInteractable<T> : IInteractable<T>, ICheckable<T>
	where T : Interaction
{

}
