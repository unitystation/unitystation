using Mirror;

/// <summary>
/// Base Component for an object that can trigger other objects to do stuff.
/// </summary>
public abstract class ObjectTrigger : NetworkBehaviour
{
	public abstract void Trigger(bool iState);
}
