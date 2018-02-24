using UnityEngine.Networking;

namespace InputControl
{
	public abstract class ObjectTrigger : NetworkBehaviour
	{
		public abstract void Trigger(bool state);
	}
}