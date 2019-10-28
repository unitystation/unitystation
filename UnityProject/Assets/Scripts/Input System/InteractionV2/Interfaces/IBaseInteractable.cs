
/// <summary>
/// Base for all interactable component interfaces. It's basically a marker interface (usually something
/// best avoided) which is only used
/// because of C#'s lack of default interface methods.
/// </summary>
public interface IBaseInteractable<T>
	where T : Interaction
{
}
